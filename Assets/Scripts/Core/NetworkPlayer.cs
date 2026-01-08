using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(NetworkTransformReliable))]
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar]
    public string selectedCharacterId;

    [SyncVar(hook = nameof(OnIsRunningChanged))]
    private bool isRunning;
    
    [SyncVar(hook = nameof(OnIsAttackingChanged))]
    private bool isAttacking;
    
    [Header("网络同步设置")]
    [SerializeField]
    private NetworkTransformReliable networkTransform;
    
    [Header("动画设置")]
    [Tooltip("Animator在模型中的相对路径，例如：ModelParent/Scout/Body")]
    [SerializeField]
    private string animatorPath = "";
    
    public string AnimatorPath => animatorPath;
    
    private CharacterStats characterStats;
    private CharacterMovementController movementController;
    private Inventory inventory;
    private List<Animator> animators = new List<Animator>();
    private SceneAwareAnimatorManager sceneAnimatorManager;
    private CharacterModelManager characterModelManager;
    private bool lastIsRunningState = false;
    private bool lastIsAttackingState = false;
    private bool animatorsReady = false;

    [SyncVar(hook = nameof(OnModelLoadedChanged))]
    private bool modelLoaded = false;

    public bool IsInitialized { get; private set; }
    public static event Action<NetworkPlayer> OnPlayerInitialized;

    /// <summary>
    /// 初始化NetworkTransform组件，确保位置同步正常工作
    /// </summary>
    private void InitializeNetworkTransform()
    {
        networkTransform = GetComponent<NetworkTransformReliable>();
        if (networkTransform == null)
        {
            networkTransform = gameObject.AddComponent<NetworkTransformReliable>();
            Debug.Log($"[NetworkPlayer] 已添加NetworkTransformReliable组件");
        }
        
        networkTransform.syncDirection = Mirror.SyncDirection.ServerToClient;
        networkTransform.positionPrecision = 0.01f;
        networkTransform.rotationSensitivity = 0.01f;
        networkTransform.interpolatePosition = true;
        networkTransform.interpolateRotation = true;
        
        if (isLocalPlayer)
        {
            Debug.Log($"[NetworkPlayer] NetworkTransform已设置为服务器权威模式");
        }
        
        Debug.Log($"[NetworkPlayer] NetworkTransform初始化完成 - 玩家: {netId}");
    }

    public override void OnStartServer()
    {
        try
        {
            // 初始化NetworkTransform组件
            InitializeNetworkTransform();
            
            characterStats = GetComponentInChildren<CharacterStats>();
            movementController = GetComponentInChildren<CharacterMovementController>();
            
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                inventory = gameObject.AddComponent<Inventory>();
            }
            
            Debug.Log($"[NetworkPlayer] OnStartServer完成 - 玩家: {netId}, Inventory: {(inventory != null ? "已创建" : "未创建")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkPlayer] OnStartServer发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public override void OnStartClient()
    {
        Debug.Log($"[NetworkPlayer] OnStartClient - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
        
        try
        {
            // 初始化NetworkTransform组件
            InitializeNetworkTransform();
            
            // 在客户端上也初始化Inventory组件
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                inventory = gameObject.AddComponent<Inventory>();
                Debug.Log($"[NetworkPlayer] OnStartClient - 已创建Inventory组件");
            }
            
            characterStats = GetComponentInChildren<CharacterStats>();
            movementController = GetComponentInChildren<CharacterMovementController>();
            
            // 关键修复：如果这是远程玩家（非本地玩家），延迟检查模型是否已经加载
            // 等待模型实例化后再初始化动画器（处理客户端在主机之后启动的情况）
            if (!isLocalPlayer)
            {
                Debug.Log($"[NetworkPlayer] 远程玩家，启动延迟动画器初始化检查 - 玩家: {netId}, modelLoaded: {modelLoaded}");
                StartCoroutine(DelayedInitializeAnimatorsForRemotePlayer());
            }
            
            Debug.Log($"[NetworkPlayer] OnStartClient完成 - 玩家: {netId}, Inventory: {(inventory != null ? "已找到" : "未找到")}, modelLoaded: {modelLoaded}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkPlayer] OnStartClient发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[NetworkPlayer] OnStartLocalPlayer - 玩家: {netId}, this: {(this != null ? "不为null" : "为null")}");
        
        try
        {
            // 并行初始化非关键组件，提高初始化效率
            StartCoroutine(ParallelInitializeComponents());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkPlayer] OnStartLocalPlayer发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 并行初始化非关键组件
    /// </summary>
    private System.Collections.IEnumerator ParallelInitializeComponents()
    {
        Debug.Log($"[NetworkPlayer] 开始并行初始化非关键组件");
        
        bool sceneAnimatorInitialized = false;
        bool playerInputInitialized = false;
        bool characterModelInitialized = false;
        
        // 并行初始化SceneAwareAnimatorManager
        StartCoroutine(InitializeSceneAnimatorManagerAsync(() => sceneAnimatorInitialized = true));
        
        // 并行初始化PlayerInputHandler
        StartCoroutine(InitializePlayerInputHandlerAsync(() => playerInputInitialized = true));
        
        // 并行初始化CharacterModelManager
        StartCoroutine(InitializeCharacterModelManagerAsync(() => characterModelInitialized = true));
        
        // 等待PlayerInputHandler和CharacterModelManager初始化完成
        // SceneAwareAnimatorManager在游戏场景中会等待模型加载完成，所以不阻塞等待
        float maxWaitTime = 5f;
        float elapsedTime = 0f;
        float checkInterval = 0.1f;
        
        while ((!playerInputInitialized || !characterModelInitialized) && elapsedTime < maxWaitTime)
        {
            elapsedTime += checkInterval;
            
            if (Mathf.Approximately(elapsedTime % 1f, 0f))
            {
                Debug.Log($"[NetworkPlayer] 等待非关键组件初始化... 已等待: {elapsedTime:F1}秒, SceneAnimator: {sceneAnimatorInitialized}, PlayerInput: {playerInputInitialized}, CharacterModel: {characterModelInitialized}");
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
        
        if (playerInputInitialized && characterModelInitialized)
        {
            Debug.Log($"[NetworkPlayer] 关键组件初始化完成，等待时间: {elapsedTime:F1}秒");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 部分关键组件初始化超时，等待时间: {elapsedTime:F1}秒");
        }
        
        // 关键修改：在基本组件初始化完成后立即标记为已初始化
        // 不等待模型加载完成，避免超时问题
        MarkAsInitialized();
    }
    
    /// <summary>
    /// 异步初始化场景感知动画管理器
    /// </summary>
    private System.Collections.IEnumerator InitializeSceneAnimatorManagerAsync(System.Action onComplete)
    {
        Debug.Log($"[NetworkPlayer] 开始异步初始化SceneAwareAnimatorManager");
        
        sceneAnimatorManager = GetComponent<SceneAwareAnimatorManager>();
        if (sceneAnimatorManager == null)
        {
            sceneAnimatorManager = gameObject.AddComponent<SceneAwareAnimatorManager>();
        }
        
        // 订阅动画器初始化完成事件
        sceneAnimatorManager.OnAnimatorsInitialized += OnAnimatorsInitialized;
        
        sceneAnimatorManager.Initialize(this);
        
        Debug.Log($"[NetworkPlayer] SceneAwareAnimatorManager初始化完成");
        
        // 关键修改：立即调用onComplete，不等待动画器初始化完成
        // 因为在游戏场景中，动画器需要等待模型加载完成后才能初始化
        onComplete?.Invoke();
        
        yield return null;
    }
    
    /// <summary>
    /// 异步初始化PlayerInputHandler
    /// </summary>
    private System.Collections.IEnumerator InitializePlayerInputHandlerAsync(System.Action onComplete)
    {
        Debug.Log($"[NetworkPlayer] 开始异步初始化PlayerInputHandler");
        
        PlayerInputHandler input = GetComponent<PlayerInputHandler>();
        if (input == null)
        {
            Debug.Log($"[NetworkPlayer] 未找到PlayerInputHandler组件，创建新组件");
            input = gameObject.AddComponent<PlayerInputHandler>();
        }
        else
        {
            Debug.Log($"[NetworkPlayer] 找到已存在的PlayerInputHandler组件，使用现有组件");
        }
        
        input.Initialize(this);
        
        Debug.Log($"[NetworkPlayer] PlayerInputHandler初始化完成");
        onComplete?.Invoke();
        
        yield return null;
    }
    
    /// <summary>
    /// 异步初始化CharacterModelManager
    /// </summary>
    private System.Collections.IEnumerator InitializeCharacterModelManagerAsync(System.Action onComplete)
    {
        Debug.Log($"[NetworkPlayer] 开始异步初始化CharacterModelManager");
        
        characterModelManager = GetComponent<CharacterModelManager>();
        if (characterModelManager != null)
        {
            // 订阅模型加载完成事件
            characterModelManager.OnModelLoaded += OnCharacterModelLoaded;
            Debug.Log($"[NetworkPlayer] 已订阅CharacterModelManager.OnModelLoaded事件");
            
            // CharacterModelManager会自动监听角色ID变化并加载模型
            // 不需要在这里等待角色ID同步，避免循环依赖
            Debug.Log($"[NetworkPlayer] CharacterModelManager初始化完成（模型加载在后台异步进行）");
            onComplete?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 未找到CharacterModelManager组件");
            onComplete?.Invoke();
        }
        
        yield return null;
    }
    
    /// <summary>
    /// 客户端通知服务器模型已加载
    /// </summary>
    [Command]
    private void CmdNotifyModelLoaded()
    {
        Debug.Log($"[NetworkPlayer] 收到客户端模型加载通知，准备通知所有客户端 - 玩家: {netId}");
        
        // 关键修复：设置modelLoaded为true，这样SyncVar会同步到所有客户端
        // 让后加入的客户端能够检测到模型已加载
        modelLoaded = true;
        Debug.Log($"[NetworkPlayer] 已设置modelLoaded = true - 玩家: {netId}");
        
        // 服务器收到通知后，通过ClientRpc通知所有客户端
        RpcNotifyModelLoaded();
    }
    
    /// <summary>
    /// 角色模型加载完成回调
    /// </summary>
    /// <summary>
    /// 角色模型加载完成回调（仅在本地玩家上调用）
    /// </summary>
    private void OnCharacterModelLoaded(GameObject model)
    {
        Debug.Log($"[NetworkPlayer] 角色模型加载完成（本地玩家），准备初始化动画器 - 玩家: {netId}");
        
        // 在模型加载完成后初始化动画器
        StartCoroutine(InitializeAnimatorsAfterModelLoaded());
        
        // 通知服务器模型已加载，服务器会通过ClientRpc通知所有客户端
        CmdNotifyModelLoaded();
    }

    /// <summary>
    /// 角色模型同步完成回调（在所有客户端上调用）
    /// 当服务器端的角色模型加载完成后，通知所有客户端初始化动画器
    /// </summary>
    [ClientRpc]
    private void RpcNotifyModelLoaded()
    {
        Debug.Log($"[NetworkPlayer] 收到模型加载通知，准备初始化动画器 - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
        
        // 在所有客户端上初始化动画器
        StartCoroutine(InitializeAnimatorsAfterModelLoaded());
    }

    
    /// <summary>
    /// 在模型加载完成后初始化动画器
    /// </summary>
    /// <summary>
    /// 在模型加载完成后初始化动画器（在所有客户端上执行）
    /// </summary>
    /// <summary>
    /// 在模型加载完成后初始化动画器（在所有客户端上执行）
    /// </summary>
    private System.Collections.IEnumerator InitializeAnimatorsAfterModelLoaded()
    {
        Debug.Log($"[NetworkPlayer] 开始初始化动画器 - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
        
        // 等待一帧，确保模型完全初始化
        yield return null;
        
        // 查找模型中的Animator组件
        Animator[] foundAnimators = GetComponentsInChildren<Animator>(true);
        
        if (foundAnimators.Length > 0)
        {
            // 验证animator引用是否有效
            if (foundAnimators[0] != null && foundAnimators[0].runtimeAnimatorController != null)
            {
                Debug.Log($"[NetworkPlayer] 找到有效的Animator组件 - 玩家: {netId}, Animator: {foundAnimators[0].name}, Controller: {foundAnimators[0].runtimeAnimatorController.name}");
                
                // 更新animators列表
                this.animators = new List<Animator>(foundAnimators);
                
                // 标记动画器已就绪（关键修复：在所有客户端上设置）
                animatorsReady = true;
                
                // 同步当前跑步状态到所有动画器
                foreach (Animator anim in this.animators)
                {
                    if (anim != null)
                    {
                        anim.SetBool("IsRun", lastIsRunningState);
                    }
                }
                
                // 如果有缓存的攻击触发，现在应用
                if (lastIsAttackingState)
                {
                    foreach (Animator anim in this.animators)
                    {
                        if (anim != null)
                        {
                            anim.SetTrigger("Atk");
                        }
                    }
                    lastIsAttackingState = false;
                    Debug.Log($"[NetworkPlayer] 已应用缓存的攻击触发 - 玩家: {netId}");
                }
                
                Debug.Log($"[NetworkPlayer] 动画器初始化成功，已同步IsRun状态到 {this.animators.Count} 个动画器 - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayer] Animator组件存在但未设置RuntimeAnimatorController - 玩家: {netId}, Animator: {foundAnimators[0].name}");
            }
            
            // 如果有多个Animator，记录警告
            if (foundAnimators.Length > 1)
            {
                Debug.LogWarning($"[NetworkPlayer] 找到 {foundAnimators.Length} 个Animator组件 - 玩家: {netId}");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 模型中未找到Animator组件 - 玩家: {netId}");
        }
        
        Debug.Log($"[NetworkPlayer] 角色模型和动画器初始化完成 - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
    }
    
    /// <summary>
    /// 动画器初始化完成回调
    /// </summary>
    private void OnAnimatorsInitialized(List<Animator> animators)
    {
        this.animators = animators;
        Debug.Log($"[NetworkPlayer] 动画器初始化完成事件触发，获取到 {animators.Count} 个animator");
        
        // 同步当前跑步状态到所有动画器
        foreach (Animator anim in animators)
        {
            if (anim != null)
            {
                anim.SetBool("IsRun", isRunning);
                Debug.Log($"[NetworkPlayer] 已同步IsRun状态到 {anim.name}: {isRunning}");
            }
        }
    }

    private void MarkAsInitialized()
    {
        if (!IsInitialized)
        {
            IsInitialized = true;
            Debug.Log($"[NetworkPlayer] 玩家已完全初始化 - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
            OnPlayerInitialized?.Invoke(this);
        }
    }

    [Server]
    public void OnServerPlayerAdded()
    {
    }

    [Command]
    public void CmdSelectCharacter(string characterId)
    {
        Debug.Log($"[NetworkPlayer] 服务器收到角色选择命令: {characterId}");
        selectedCharacterId = characterId;
        Debug.Log($"[NetworkPlayer] 角色ID已设置: {selectedCharacterId}");
        GenerateCharacter();
    }

    [Server]
    void GenerateCharacter()
    {
        if (string.IsNullOrEmpty(selectedCharacterId))
        {
            Debug.LogWarning($"[NetworkPlayer] 角色ID为空，无法生成角色");
            return;
        }

        // 直接调用SpawnCharacterForPlayer，无论是否是临时占位符
        // 角色模型会通过CharacterModelManager加载到当前GameObject上
        GameNetworkManager networkManager = NetworkManager.singleton as GameNetworkManager;
        if (networkManager != null)
        {
            networkManager.SpawnCharacterForPlayer(this);
            Debug.Log($"[NetworkPlayer] 角色生成请求已发送: {selectedCharacterId}");
        }
        else
        {
            Debug.LogError($"[NetworkPlayer] GameNetworkManager未找到，无法生成角色");
        }
    }


    [Command]
    public void CmdSetMovementInput(Vector2 input)
    {
        if (movementController != null)
        {
            movementController.SetInput(input);
        }
    }

    [Command]
    public void CmdSetRunning(bool running)
    {
        if (isRunning != running)
        {
            isRunning = running;
            Debug.Log($"[NetworkPlayer] CmdSetRunning: {running} - 玩家: {netId}");
        }
    }
    
    /// <summary>
    /// 延迟初始化远程玩家的动画器（处理客户端在主机之后启动的情况）
    /// </summary>
    private System.Collections.IEnumerator DelayedInitializeAnimatorsForRemotePlayer()
    {
        Debug.Log($"[NetworkPlayer] 开始延迟初始化远程玩家动画器 - 玩家: {netId}");
        
        // 等待最多10秒，直到模型实例化完成
        float maxWaitTime = 10f;
        float elapsedTime = 0f;
        
        while (elapsedTime < maxWaitTime)
        {
            // 检查模型是否已加载且模型已实例化（通过检查是否有Animator组件）
            if (modelLoaded && !animatorsReady)
            {
                // 检查是否有Animator组件（模型已实例化）
                Animator[] foundAnimators = GetComponentsInChildren<Animator>(true);
                if (foundAnimators.Length > 0 && foundAnimators[0] != null && foundAnimators[0].runtimeAnimatorController != null)
                {
                    Debug.Log($"[NetworkPlayer] 检测到模型已实例化，开始初始化动画器 - 玩家: {netId}");
                    StartCoroutine(InitializeAnimatorsAfterModelLoaded());
                    yield break;
                }
            }
            
            // 每0.5秒检查一次
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
            
            Debug.Log($"[NetworkPlayer] 等待模型实例化... 已等待 {elapsedTime:F1}秒 - 玩家: {netId}, modelLoaded: {modelLoaded}, animatorsReady: {animatorsReady}");
        }
        
        Debug.LogWarning($"[NetworkPlayer] 延迟初始化动画器超时 - 玩家: {netId}, modelLoaded: {modelLoaded}, animatorsReady: {animatorsReady}");
    }

    /// <summary>
    /// 模型加载状态变化回调（在所有客户端上自动调用）
    /// </summary>
    private void OnModelLoadedChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[NetworkPlayer] OnModelLoadedChanged: {oldValue} -> {newValue} - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}");
        
        // 当模型加载完成时，在所有客户端上初始化动画器
        if (newValue && !animatorsReady)
        {
            Debug.Log($"[NetworkPlayer] 检测到模型已加载，开始初始化动画器 - 玩家: {netId}");
            StartCoroutine(InitializeAnimatorsAfterModelLoaded());
        }
    }

    /// <summary>
    /// isRunning状态改变时的回调（SyncVar hook）
    /// 在所有客户端上自动调用，确保动画同步
    /// </summary>
    /// <summary>
    /// isRunning状态改变时的回调（SyncVar hook）
    /// 在所有客户端上自动调用，确保动画同步
    /// </summary>
    private void OnIsRunningChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[NetworkPlayer] OnIsRunningChanged: {oldValue} -> {newValue} - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}, animatorsReady: {animatorsReady}, animators.Count: {animators.Count}");
        
        // 等待动画器初始化完成后再更新
        if (animatorsReady && animators.Count > 0)
        {
            foreach (Animator anim in animators)
            {
                if (anim != null && anim.isActiveAndEnabled)
                {
                    anim.SetBool("IsRun", newValue);
                }
            }
            lastIsRunningState = newValue;
            Debug.Log($"[NetworkPlayer] 已更新跑步动画 - 玩家: {netId}");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 动画器未就绪，缓存状态并等待动画器初始化 - 玩家: {netId}");
            // 缓存状态，等待动画器初始化完成后应用
            lastIsRunningState = newValue;
        }
    }

    [Command]
    public void CmdSetRotation(float rotationY)
    {
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }
    
    /// <summary>
    /// 等待动画器初始化完成后更新跑步动画
    /// </summary>


    public void UpdateAnimatorReference()
    {
        if (sceneAnimatorManager != null)
        {
            sceneAnimatorManager.UpdateAnimatorReference();
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] 场景感知动画管理器未初始化，使用传统方式更新动画引用");
            StartCoroutine(TraditionalUpdateAnimatorReferenceCoroutine());
        }
    }
    
    /// <summary>
    /// 传统方式更新动画引用的协程方法（备用方案）
    /// </summary>
    private System.Collections.IEnumerator TraditionalUpdateAnimatorReferenceCoroutine()
    {
        int maxAttempts = 30;
        int attempts = 0;
        
        Debug.Log($"[NetworkPlayer] 使用传统方式获取Animator引用，指定路径: {animatorPath}");
        
        while (animators.Count == 0 && attempts < maxAttempts)
        {
            Animator[] allAnimators = GetComponentsInChildren<Animator>(true);
            Debug.Log($"[NetworkPlayer] 找到{allAnimators.Length}个Animator组件");
            
            for (int i = 0; i < allAnimators.Length; i++)
            {
                Transform animTransform = allAnimators[i].transform;
                string path = animTransform.name;
                Transform parent = animTransform.parent;
                while (parent != null && parent != transform)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                Debug.Log($"[NetworkPlayer] Animator[{i}]: {path}");
            }
            
            // 添加所有找到的动画器
            animators.AddRange(allAnimators);
            
            Debug.Log($"[NetworkPlayer] 尝试获取Animator引用 (尝试 {attempts+1}/{maxAttempts}): 找到 {animators.Count} 个");
            
            if (animators.Count > 0)
            {
                // 同步当前跑步状态到所有动画器
                foreach (Animator anim in animators)
                {
                    anim.SetBool("IsRun", isRunning);
                }
                Debug.Log($"[NetworkPlayer] 已同步IsRun状态到所有动画器: {isRunning}");
                yield break;
            }
            
            attempts++;
            yield return new WaitForSeconds(0.2f);
        }
        
        if (animators.Count == 0)
        {
            Debug.LogError($"[NetworkPlayer] 多次尝试后仍无法获取Animator引用，已尝试{maxAttempts}次");
        }
    }

    private void Update()
    {
        // 确保NetworkPlayer已完全初始化后再执行Update逻辑
        if (!IsInitialized)
        {
            return;
        }
    }

    [Command]
    public void CmdTryCollectResource(uint resourceNetId)
    {
        if (NetworkServer.spawned.TryGetValue(resourceNetId, out NetworkIdentity identity))
        {
            ResourceNode node = identity.GetComponent<ResourceNode>();
            if (node != null)
            {
                node.ServerCollect(this);
            }
        }
    }

    [Command]
    public void CmdAttack()
    {
        Debug.Log($"[NetworkPlayer] CmdAttack被调用 - 玩家: {netId}, 动画器数量: {animators.Count}");
        
        // 设置攻击状态
        isAttacking = true;
        Debug.Log($"[NetworkPlayer] isAttacking已设置为true - 玩家: {netId}");
        
        // 初始化攻击周期，确保每次攻击只触发一次伤害
        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.InitializeAttackCycle();
            Debug.Log($"[NetworkPlayer] 攻击周期已初始化 - 玩家: {netId}");
            
            // 启动协程，在攻击动画结束后结束攻击周期
            StartCoroutine(EndAttackCycleAfterAnimation());
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] PlayerInputHandler为空，无法初始化攻击周期 - 玩家: {netId}");
        }
    }

    /// <summary>
    /// isAttacking状态改变时的回调（SyncVar hook）
    /// 在所有客户端上自动调用，确保攻击动画同步
    /// </summary>
    /// <summary>
    /// isAttacking状态改变时的回调（SyncVar hook）
    /// 在所有客户端上自动调用，确保攻击动画同步
    /// </summary>
    private void OnIsAttackingChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"[NetworkPlayer] OnIsAttackingChanged: {oldValue} -> {newValue} - 玩家: {netId}, isLocalPlayer: {isLocalPlayer}, animatorsReady: {animatorsReady}, animators.Count: {animators.Count}");
        
        // 只在从false变为true时触发攻击动画
        if (newValue && !oldValue)
        {
            // 等待动画器初始化完成后再触发
            if (animatorsReady && animators.Count > 0)
            {
                foreach (Animator anim in animators)
                {
                    if (anim != null && anim.isActiveAndEnabled)
                    {
                        anim.SetTrigger("Atk");
                    }
                }
                Debug.Log($"[NetworkPlayer] 已触发攻击动画 - 玩家: {netId}");
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayer] 动画器未就绪，缓存攻击触发并等待动画器初始化 - 玩家: {netId}");
                // 缓存攻击触发，等待动画器初始化完成后应用
                lastIsAttackingState = true;
            }
        }
    }
    
    /// <summary>
    /// 等待动画器初始化完成后播放攻击动画
    /// </summary>

    
    /// <summary>
    /// 在攻击动画结束后结束攻击周期的协程
    /// </summary>
    private System.Collections.IEnumerator EndAttackCycleAfterAnimation()
    {
        // 等待攻击动画完成（假设攻击动画持续时间为1.5秒）
        yield return new WaitForSeconds(1.5f);
        
        // 重置攻击状态
        isAttacking = false;
        Debug.Log($"[NetworkPlayer] 重置isAttacking为false - 玩家: {netId}");
        
        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
        {
            inputHandler.EndAttackCycle();
            Debug.Log($"[NetworkPlayer] 攻击周期已结束");
        }
    }
    
    /// <summary>
    /// 处理攻击命中事件的命令，在服务器上执行攻击检测和伤害应用
    /// </summary>
    [Command]
    public void CmdOnAttackHit()
    {
        Debug.Log($"[NetworkPlayer] CmdOnAttackHit被调用 - 玩家: {netId}");
        
        // 确保只在服务器上执行
        if (!isServer)
            return;
            
        // 获取PlayerInputHandler组件
        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
        {
            Debug.LogError($"[NetworkPlayer] 无法找到PlayerInputHandler组件");
            return;
        }
        
        // 获取玩家的CharacterStats组件
        CharacterStats playerStats = GetComponentInChildren<CharacterStats>();
        if (playerStats == null)
        {
            Debug.LogError($"[NetworkPlayer] 无法找到玩家的CharacterStats组件");
            return;
        }
        
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        
        // 检测攻击范围内的敌人
        Collider[] hits = Physics.OverlapSphere(origin, inputHandler.attackRange, LayerMask.GetMask("Enemy"));
        Debug.Log($"[NetworkPlayer] 服务器攻击检测 - 攻击范围: {inputHandler.attackRange}, 检测到敌人数量: {hits.Length}");
        
        foreach (var hit in hits)
        {
            Debug.Log($"[NetworkPlayer] 检测到碰撞对象: {hit.gameObject.name}, Tag: {hit.tag}, Layer: {LayerMask.LayerToName(hit.gameObject.layer)}");
            
            if (!hit.CompareTag("Enemy"))
            {
                Debug.Log($"[NetworkPlayer] 跳过非敌人对象: {hit.gameObject.name}, Tag: {hit.tag}");
                continue;
            }
            
            // 检查敌人是否在攻击角度范围内
            Vector3 dirToEnemy = (hit.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirToEnemy);
            Debug.Log($"[NetworkPlayer] 敌人角度: {angle}, 最大允许角度: {inputHandler.attackAngle * 0.5f}");
            
            if (angle > inputHandler.attackAngle * 0.5f)
            {
                Debug.Log($"[NetworkPlayer] 敌人 {hit.gameObject.name} 超出攻击角度范围，跳过");
                continue;
            }
            
            // 查找敌人的EnemyHealthManager组件
            EnemyHealthManager enemyHealthManager = hit.GetComponent<EnemyHealthManager>();
            if (enemyHealthManager == null)
            {
                // 尝试从父对象查找
                enemyHealthManager = hit.transform.parent?.GetComponent<EnemyHealthManager>();
            }
            if (enemyHealthManager == null)
            {
                // 尝试从子对象查找
                enemyHealthManager = hit.transform.GetComponentInChildren<EnemyHealthManager>();
            }
            
            if (enemyHealthManager != null)
            {
                Debug.Log($"[NetworkPlayer] 找到敌人 {hit.gameObject.name} 的EnemyHealthManager组件");
                
                // 应用伤害
                int oldHealth = enemyHealthManager.currentHealth;
                enemyHealthManager.ApplyDamage(playerStats.attack);
                int newHealth = enemyHealthManager.currentHealth;
                
                Debug.Log($"[NetworkPlayer] 服务器伤害应用完成 - 敌人 {hit.gameObject.name}: 旧血量: {oldHealth}, 伤害: {playerStats.attack}, 新血量: {newHealth}");
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayer] 无法找到敌人 {hit.gameObject.name} 的EnemyHealthManager组件，尝试使用CharacterStats作为备选");
                
                // 备用方案：使用CharacterStats组件
                CharacterStats enemyStats = hit.GetComponent<CharacterStats>();
                if (enemyStats == null)
                {
                    enemyStats = hit.transform.parent?.GetComponent<CharacterStats>();
                }
                if (enemyStats == null)
                {
                    enemyStats = hit.transform.GetComponentInChildren<CharacterStats>();
                }
                
                if (enemyStats != null)
                {
                    Debug.Log($"[NetworkPlayer] 使用备选方案：找到敌人 {hit.gameObject.name} 的CharacterStats组件");
                    int oldHealth = enemyStats.currentHealth;
                    enemyStats.ApplyDamage(playerStats.attack);
                    int newHealth = enemyStats.currentHealth;
                    Debug.Log($"[NetworkPlayer] 服务器伤害应用完成(备选方案) - 敌人 {hit.gameObject.name}: 旧血量: {oldHealth}, 伤害: {playerStats.attack}, 新血量: {newHealth}");
                }
                else
                {
                    Debug.LogError($"[NetworkPlayer] 无法找到敌人 {hit.gameObject.name} 的任何血量管理组件");
                }
            }
        }
    }

    public Inventory GetInventory()
    {
        return inventory;
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件，避免内存泄漏
        if (sceneAnimatorManager != null)
        {
            sceneAnimatorManager.OnAnimatorsInitialized -= OnAnimatorsInitialized;
        }
    }
}
