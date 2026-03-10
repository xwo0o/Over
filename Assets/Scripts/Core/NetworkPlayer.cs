using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(NetworkTransformReliable))]
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar]
    public string selectedCharacterId;

    // 新方案：直接同步speed值（0-1），而不是布尔值isRunning
    [SyncVar(hook = nameof(OnSpeedChanged))]
    private float syncedSpeed = 0f;
    
    // 注意：攻击状态已移到 WeaponStateManager 管理
    // 使用 WeaponStateManager 的 isAttacking SyncVar
    
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
    private bool lastIsAttackingState = false;
    private bool animatorsReady = false;

    [SyncVar(hook = nameof(OnModelLoadedChanged))]
    private bool modelLoaded = false;

    public bool IsInitialized { get; private set; }
    public static event Action<NetworkPlayer> OnPlayerInitialized;
    
    /// <summary>
    /// 获取当前角色的Animator列表
    /// </summary>
    public List<Animator> GetAnimators()
    {
        return animators;
    }

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
            // 关键修复：Inventory 组件已经在预制体上挂载，不需要动态添加
            // 动态添加的 NetworkBehaviour 组件无法正确同步 SyncList
            if (inventory == null)
            {
                Debug.LogError($"[NetworkPlayer] OnStartServer: Inventory组件未找到！请确保预制体上已挂载Inventory组件");
            }
            
            // 注意：不在这里直接调用 InitializeInventoryWeapons()
            // 因为 Inventory.OnStartServer() 可能还没有被调用，slots 还没有初始化
            // 使用 StartCoroutine 延迟初始化，确保 Inventory 先完成初始化
            StartCoroutine(DelayedInitializeInventoryWeapons());
            
            Debug.Log($"[NetworkPlayer] OnStartServer完成 - 玩家: {netId}, Inventory: {(inventory != null ? "已创建" : "未创建")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkPlayer] OnStartServer发生异常: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 延迟初始化背包武器，确保 Inventory 先完成初始化
    /// </summary>
    [Server]
    private System.Collections.IEnumerator DelayedInitializeInventoryWeapons()
    {
        // 等待一帧，确保所有 OnStartServer() 都已执行
        yield return null;
        
        // 再检查一下 slots 是否已初始化
        if (inventory != null && inventory.slots.Count == 0)
        {
            Debug.LogWarning($"[NetworkPlayer] Inventory.slots 仍未初始化，再等待一帧");
            yield return null;
        }
        
        InitializeInventoryWeapons();
    }
    
    /// <summary>
    /// 初始化背包武器数据
    /// 将武器放在2、3、4格子中（索引1、2、3）
    /// </summary>
    [Server]
    void InitializeInventoryWeapons()
    {
        Debug.Log($"[NetworkPlayer] InitializeInventoryWeapons开始 - inventory={(inventory != null ? "有" : "null")}, slots.Count={inventory?.slots?.Count ?? 0}");
        
        if (inventory == null)
        {
            Debug.LogError("[NetworkPlayer] 背包组件为空，无法初始化武器");
            return;
        }
        
        // 检查 slots 是否已初始化
        if (inventory.slots.Count == 0)
        {
            Debug.LogError($"[NetworkPlayer] Inventory.slots 未初始化，无法放置武器");
            return;
        }
        
        // 确保背包容量足够
        if (inventory.capacity < 4)
        {
            Debug.LogWarning("[NetworkPlayer] 背包容量不足，无法放置所有武器");
            return;
        }
        
        Debug.Log($"[NetworkPlayer] 当前slots状态 - 长度: {inventory.slots.Count}");
        for (int i = 0; i < inventory.slots.Count; i++)
        {
            Debug.Log($"[NetworkPlayer]   格子[{i}]: resourceId={inventory.slots[i].resourceId}, amount={inventory.slots[i].amount}");
        }
        
        // 格子2（索引1）：阔刀
        inventory.slots[1] = new InventoryItem { resourceId = "WGS", amount = 1 };
        
        // 格子3（索引2）：双刀
        inventory.slots[2] = new InventoryItem { resourceId = "WTD", amount = 1 };
        
        // 格子4（索引3）：斧头
        inventory.slots[3] = new InventoryItem { resourceId = "Axe", amount = 1 };
        
        // SyncList自动检测变化并同步，无需手动克隆
        
        Debug.Log("[NetworkPlayer] 背包武器初始化完成 - 格子1:阔刀, 格子2:双刀, 格子3:斧头");
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
            // 关键修复：Inventory 组件已经在预制体上挂载，不需要动态添加
            // 动态添加的 NetworkBehaviour 组件无法正确同步 SyncList
            if (inventory == null)
            {
                Debug.LogError($"[NetworkPlayer] OnStartClient: Inventory组件未找到！请确保预制体上已挂载Inventory组件");
            }
            
            characterStats = GetComponentInChildren<CharacterStats>();
            movementController = GetComponentInChildren<CharacterMovementController>();
            
            // 关键修复：如果这是远程玩家（非本地玩家），延迟检查模型是否已经加载
            // 等待模型实例化后再初始化动画器（处理客户端在主机之后启动的情况）
            if (!isLocalPlayer)
            {
                Debug.Log($"[NetworkPlayer] 远程玩家，启动延迟动画器初始化检查 - 玩家: {netId}, modelLoaded: {modelLoaded}");
                
                // 关键修复：在客户端上也需要初始化SceneAwareAnimatorManager
                // 确保WeaponStateManager能够获取到Animator
                sceneAnimatorManager = GetComponent<SceneAwareAnimatorManager>();
                if (sceneAnimatorManager == null)
                {
                    sceneAnimatorManager = gameObject.AddComponent<SceneAwareAnimatorManager>();
                    Debug.Log($"[NetworkPlayer] 远程玩家 - 已创建SceneAwareAnimatorManager组件");
                }
                
                // 订阅动画器初始化完成事件
                sceneAnimatorManager.OnAnimatorsInitialized += OnAnimatorsInitialized;
                
                // 初始化SceneAwareAnimatorManager
                sceneAnimatorManager.Initialize(this);
                Debug.Log($"[NetworkPlayer] 远程玩家 - SceneAwareAnimatorManager已初始化");
                
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
                
                // 新方案：同步当前syncedSpeed到所有动画器
                // 直接使用syncedSpeed（0-1连续值），而不是布尔值
                foreach (Animator anim in this.animators)
                {
                    if (anim != null)
                    {
                        anim.SetFloat("speed", syncedSpeed);
                    }
                }
                
                // 注意：动画触发由WeaponStateManager处理，这里不再处理缓存的攻击触发
                
                // 关键修复：更新SceneAwareAnimatorManager的动画引用
                // 确保WeaponStateManager能够通过SceneAwareAnimatorManager获取到Animator
                if (sceneAnimatorManager == null)
                {
                    sceneAnimatorManager = GetComponent<SceneAwareAnimatorManager>();
                }
                if (sceneAnimatorManager != null)
                {
                    sceneAnimatorManager.UpdateAnimatorReference();
                    Debug.Log($"[NetworkPlayer] 已更新SceneAwareAnimatorManager动画引用 - 玩家: {netId}");
                }
                else
                {
                    Debug.LogWarning($"[NetworkPlayer] sceneAnimatorManager为null，无法更新动画引用 - 玩家: {netId}");
                }
                
                Debug.Log($"[NetworkPlayer] 动画器初始化成功，已同步IsRun状态到 {this.animators.Count} 个动画器 - 玩家: {netId}, isLocalPlayer={isLocalPlayer}");
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
        
        // 新方案：直接使用syncedSpeed（0-1连续值）
        // 替代原来的lastIsRunningState布尔值
        float speedToApply = syncedSpeed;
        
        // 同步当前speed到所有动画器
        foreach (Animator anim in animators)
        {
            if (anim != null)
            {
                anim.SetFloat("speed", speedToApply);
                Debug.Log($"[NetworkPlayer] 已同步speed状态到 {anim.name}: {speedToApply}");
            }
        }
        
        // 标记动画器已就绪
        animatorsReady = true;
        Debug.Log($"[NetworkPlayer] 动画器已就绪，已应用speed状态: {speedToApply} - 玩家: {netId}");
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

    [Command]
    public void CmdSwitchCharacter(string newCharacterId)
    {
        Debug.Log($"[NetworkPlayer] 服务器收到角色切换命令: {selectedCharacterId} -> {newCharacterId}");
        
        if (string.IsNullOrEmpty(newCharacterId))
        {
            Debug.LogWarning($"[NetworkPlayer] 新角色ID为空，无法切换");
            return;
        }
        
        if (selectedCharacterId == newCharacterId)
        {
            Debug.Log($"[NetworkPlayer] 新角色与当前角色相同，无需切换");
            return;
        }
        
        // 更新角色ID
        selectedCharacterId = newCharacterId;
        Debug.Log($"[NetworkPlayer] 角色ID已切换为: {selectedCharacterId}");
        
        // 重新生成角色
        RegenerateCharacter();
        
        // 通知所有客户端角色已切换
        RpcOnCharacterSwitched(newCharacterId);
    }
    
    /// <summary>
    /// 角色切换回调（在所有客户端上调用）
    /// </summary>
    [ClientRpc]
    private void RpcOnCharacterSwitched(string newCharacterId)
    {
        Debug.Log($"[NetworkPlayer] 收到角色切换通知: {newCharacterId}");
        
        // 更新UI显示
        if (isLocalPlayer && InGameCharacterSelectionUI.Instance != null)
        {
            InGameCharacterSelectionUI.Instance.UpdateCurrentCharacterId(newCharacterId);
        }
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
    
    /// <summary>
    /// 重新生成角色（用于角色切换）
    /// </summary>
    [Server]
    void RegenerateCharacter()
    {
        if (string.IsNullOrEmpty(selectedCharacterId))
        {
            Debug.LogWarning($"[NetworkPlayer] 角色ID为空，无法重新生成角色");
            return;
        }
        
        // 关键修复：保存当前位置，避免切换角色时位置重置
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;
        Debug.Log($"[NetworkPlayer] 保存当前位置: {currentPosition}, 旋转: {currentRotation.eulerAngles}");
        
        // 通知CharacterModelManager重新加载模型
        CharacterModelManager modelManager = GetComponent<CharacterModelManager>();
        if (modelManager != null)
        {
            modelManager.UpdateModel(selectedCharacterId);
            Debug.Log($"[NetworkPlayer] 角色模型更新请求已发送: {selectedCharacterId}");
        }
        else
        {
            Debug.LogError($"[NetworkPlayer] CharacterModelManager未找到，无法更新角色模型");
        }
        
        // 更新角色属性
        GameNetworkManager networkManager = NetworkManager.singleton as GameNetworkManager;
        if (networkManager != null)
        {
            networkManager.SpawnCharacterForPlayer(this);
        }
        
        // 关键修复：恢复之前保存的位置和旋转
        transform.position = currentPosition;
        transform.rotation = currentRotation;
        Debug.Log($"[NetworkPlayer] 恢复位置: {currentPosition}, 旋转: {currentRotation.eulerAngles}");
    }


    [Command]
    public void CmdSetMovementInput(Vector2 input)
    {
        if (movementController != null)
        {
            movementController.SetInput(input);
        }
    }

    /// <summary>
    /// 新方案：直接同步speed值（0-1）
    /// 替代原来的CmdSetRunning，直接同步连续的speed值
    /// </summary>
    [Command]
    public void CmdSetSpeed(float speed)
    {
        // 限制speed范围在0-1
        speed = Mathf.Clamp01(speed);
        
        // 更新syncedSpeed，SyncVar会自动同步到所有客户端
        syncedSpeed = speed;
        
        // 优化：同步更新移动速度乘数
        // 让实际移动速度 = 最大速度 × speed参数
        if (movementController != null)
        {
            movementController.SetSpeedMultiplier(speed);
        }
    }
    
    /// <summary>
    /// speed值变化时的回调（SyncVar hook）
    /// 直接设置Animator的speed参数，无需协程过渡
    /// </summary>
    private void OnSpeedChanged(float oldValue, float newValue)
    {
        // 确保动画器就绪
        if (!animatorsReady || animators.Count == 0)
        {
            // 尝试从SceneAwareAnimatorManager获取Animator
            if (sceneAnimatorManager != null)
            {
                Animator animator = sceneAnimatorManager.GetCurrentAnimator();
                if (animator != null)
                {
                    animators.Add(animator);
                    animatorsReady = true;
                }
            }
        }
        
        // 直接设置Animator的speed参数
        // 新方案：直接同步，无需协程过渡，确保流畅
        if (animatorsReady && animators.Count > 0)
        {
            foreach (Animator anim in animators)
            {
                if (anim != null && anim.isActiveAndEnabled)
                {
                    anim.SetFloat("speed", newValue);
                }
            }
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
                // 新方案：同步当前syncedSpeed到所有动画器
                foreach (Animator anim in animators)
                {
                    anim.SetFloat("speed", syncedSpeed);
                }
                Debug.Log($"[NetworkPlayer] 已同步speed状态到所有动画器: {syncedSpeed}");
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

    // 注意：攻击逻辑已移到 WeaponStateManager.CmdRequestAttack
    // 使用 WeaponStateManager 的 SyncVar 系统同步攻击状态
    
    /// <summary>
    /// 等待动画器初始化完成后播放攻击动画
    /// </summary>

    

    
    /// <summary>
    /// 处理攻击命中事件的命令，在服务器上执行攻击检测和伤害应用
    /// 注意：实际的伤害检测逻辑由WeaponDamageSystem处理
    /// </summary>
    [Command]
    public void CmdOnAttackHit()
    {
        Debug.Log($"[NetworkPlayer] CmdOnAttackHit被调用 - 玩家: {netId}");
        
        // 确保只在服务器上执行
        if (!isServer)
            return;
        
        // 获取WeaponDamageSystem组件
        WeaponDamageSystem damageSystem = GetComponent<WeaponDamageSystem>();
        if (damageSystem != null)
        {
            // 让WeaponDamageSystem处理伤害检测
            damageSystem.PerformDamageCheck(Vector3.zero, 0f);
            Debug.Log($"[NetworkPlayer] 伤害检测已转发到WeaponDamageSystem - 玩家: {netId}");
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] WeaponDamageSystem为空，无法执行伤害检测 - 玩家: {netId}");
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
