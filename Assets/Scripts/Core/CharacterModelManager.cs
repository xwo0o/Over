using Mirror;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class CharacterModelManager : NetworkBehaviour
{
    [Header("模型设置")]
    [SerializeField]
    private Transform modelParent;

    [SerializeField]
    private float modelScale = 1f;

    private GameObject currentModel;
    private CharacterData currentCharacterData;
    private Dictionary<string, AsyncOperationHandle<GameObject>> loadedHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    private NetworkPlayer networkPlayer;
    private CharacterDatabase characterDatabase;
    private bool isLoadingModel = false;
    private string lastCharacterId = "";

    public event System.Action<GameObject> OnModelLoaded;

    private void Awake()
    {
        networkPlayer = GetComponent<NetworkPlayer>();
        characterDatabase = CharacterDatabase.Instance;

        if (modelParent == null)
        {
            modelParent = transform.Find("ModelParent");
            if (modelParent == null)
            {
                GameObject parentObj = new GameObject("ModelParent");
                parentObj.transform.SetParent(transform);
                parentObj.transform.localPosition = Vector3.zero;
                parentObj.transform.localRotation = Quaternion.identity;
                modelParent = parentObj.transform;
            }
        }
    }

    public override void OnStartServer()
    {
        StartCoroutine(WaitForCharacterIdAndLoadModel());
    }

    public override void OnStartClient()
    {
        Debug.Log($"[CharacterModelManager] OnStartClient - isLocalPlayer: {isLocalPlayer}, isServer: {isServer}");
        
        if (isLocalPlayer)
        {
            Debug.Log($"[CharacterModelManager] 本地玩家，开始监听角色ID变化");
            StartCoroutine(MonitorCharacterIdChanges());
        }
        else if (isServer)
        {
            Debug.Log($"[CharacterModelManager] 服务器端，等待角色ID同步");
            StartCoroutine(WaitForCharacterIdAndLoadModel());
        }
        else
        {
            Debug.Log($"[CharacterModelManager] 远程玩家（其他客户端的玩家），等待服务器同步角色ID");
            StartCoroutine(WaitForCharacterIdAndLoadModel());
        }
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[CharacterModelManager] OnStartLocalPlayer - 本地玩家，等待角色ID同步");
        StartCoroutine(MonitorCharacterIdChanges());
    }

    private System.Collections.IEnumerator MonitorCharacterIdChanges()
    {
        Debug.Log($"[CharacterModelManager] 开始监听角色ID变化");
        
        while (true)
        {
            if (!string.IsNullOrEmpty(networkPlayer.selectedCharacterId) && 
                networkPlayer.selectedCharacterId != lastCharacterId &&
                !isLoadingModel)
            {
                Debug.Log($"[CharacterModelManager] 检测到角色ID变化: '{lastCharacterId}' -> '{networkPlayer.selectedCharacterId}'");
                lastCharacterId = networkPlayer.selectedCharacterId;
                LoadCharacterModel(networkPlayer.selectedCharacterId);
            }
            
            // 优化：减少检查间隔，从0.1秒减少到0.05秒，提高响应速度
            yield return new WaitForSeconds(0.05f);
        }
    }

    private System.Collections.IEnumerator WaitForCharacterIdAndLoadModel()
    {
        Debug.Log($"[CharacterModelManager] 开始等待角色ID, 当前selectedCharacterId: '{networkPlayer.selectedCharacterId}'");
        
        int timeoutCount = 0;
        int maxTimeout = 300;
        
        while (string.IsNullOrEmpty(networkPlayer.selectedCharacterId))
        {
            timeoutCount++;
            if (timeoutCount >= maxTimeout)
            {
                Debug.LogWarning($"[CharacterModelManager] 等待角色ID超时（{maxTimeout}次），等待客户端发送角色选择命令");
                Debug.LogWarning($"[CharacterModelManager] 服务器端不会自动设置默认角色，必须等待客户端通过CmdSelectCharacter发送角色ID");
                break;
            }
            yield return new WaitForSeconds(0.05f);
        }

        if (!string.IsNullOrEmpty(networkPlayer.selectedCharacterId))
        {
            Debug.Log($"[CharacterModelManager] 角色ID已设置: {networkPlayer.selectedCharacterId}");
            LoadCharacterModel(networkPlayer.selectedCharacterId);
        }
        else
        {
            Debug.LogWarning($"[CharacterModelManager] 角色ID未设置，跳过模型加载，等待客户端发送角色选择命令");
        }
    }

    public void LoadCharacterModel(string characterId)
    {
        if (isLoadingModel)
        {
            Debug.LogWarning($"[CharacterModelManager] 正在加载模型，忽略重复请求: {characterId}");
            return;
        }
        
        if (characterDatabase == null)
        {
            Debug.LogError($"[CharacterModelManager] CharacterDatabase未初始化");
            return;
        }

        currentCharacterData = characterDatabase.GetCharacter(characterId);
        if (currentCharacterData == null)
        {
            Debug.LogError($"[CharacterModelManager] 未找到角色ID: {characterId}");
            return;
        }

        isLoadingModel = true;
        StartCoroutine(LoadModelAsync(currentCharacterData));
    }

    private System.Collections.IEnumerator LoadModelAsync(CharacterData characterData)
    {
        string addressableKey = characterData.addressableKey;
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError($"[CharacterModelManager] 角色{characterData.id}的addressableKey为空");
            isLoadingModel = false;
            yield break;
        }

        Debug.Log($"[CharacterModelManager] 开始加载角色模型: {addressableKey}");

        AsyncOperationHandle<GameObject> handle;

        if (loadedHandles.ContainsKey(addressableKey))
        {
            handle = loadedHandles[addressableKey];
        }
        else
        {
            handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            loadedHandles[addressableKey] = handle;
        }

        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            InstantiateModel(handle.Result);
        }
        else
        {
            Debug.LogError($"[CharacterModelManager] 加载角色模型失败: {addressableKey}, 错误: {handle.OperationException}");
            isLoadingModel = false;
        }
    }

    private void InstantiateModel(GameObject modelPrefab)
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel = Instantiate(modelPrefab, modelParent);
        // 确保模型相对于父对象的位置正确，避免Y轴偏移导致下落
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one * modelScale;
        
        // 关键修复：确保模型没有额外的Rigidbody组件导致物理问题
        Rigidbody modelRb = currentModel.GetComponentInChildren<Rigidbody>();
        if (modelRb != null)
        {
            // 如果模型有Rigidbody，设置为运动学以避免与CharacterController冲突
            modelRb.isKinematic = true;
            modelRb.useGravity = false;
        }

        // 添加动画事件转发器，确保动画事件能够被正确处理
        // 为所有带有Animator组件的子对象添加AnimationEventForwarder
        Animator[] animators = currentModel.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            // 检查是否已经添加了AnimationEventForwarder
            if (animator.GetComponent<AnimationEventForwarder>() == null)
            {
                AnimationEventForwarder eventForwarder = animator.gameObject.AddComponent<AnimationEventForwarder>();
                Debug.Log($"[CharacterModelManager] 已为{animator.gameObject.name}添加AnimationEventForwarder组件，实例ID: {eventForwarder.GetInstanceID()}");
            }
        }
        
        // 同时在根对象也添加一个，确保所有事件都能被处理
        if (currentModel.GetComponent<AnimationEventForwarder>() == null)
        {
            AnimationEventForwarder rootForwarder = currentModel.AddComponent<AnimationEventForwarder>();
            Debug.Log($"[CharacterModelManager] 已为角色模型根对象添加AnimationEventForwarder组件，实例ID: {rootForwarder.GetInstanceID()}");
        }

        Debug.Log($"[CharacterModelManager] 角色模型实例化成功: {currentCharacterData?.name}");
        
        // 标记模型加载完成
        isLoadingModel = false;
        
        // 触发模型加载完成事件
        OnModelLoaded?.Invoke(currentModel);
        
        // 延迟一帧后更新动画引用，确保模型完全初始化
        StartCoroutine(DelayedUpdateAnimatorReference());
    }
    
    /// <summary>
    /// 延迟更新动画引用
    /// </summary>
    private System.Collections.IEnumerator DelayedUpdateAnimatorReference()
    {
        // 等待一帧，确保模型完全初始化
        yield return null;
        
        if (networkPlayer != null)
        {
            // 更新场景感知动画管理器的引用
            SceneAwareAnimatorManager sceneAnimatorManager = networkPlayer.GetComponent<SceneAwareAnimatorManager>();
            if (sceneAnimatorManager != null)
            {
                sceneAnimatorManager.UpdateAnimatorReference();
                Debug.Log($"[CharacterModelManager] 场景感知动画管理器引用已更新");
            }
            
            // 更新网络玩家的动画引用
            networkPlayer.UpdateAnimatorReference();
            
            // 更新玩家输入处理器的动画引用
            PlayerInputHandler inputHandler = networkPlayer.GetComponent<PlayerInputHandler>();
            Debug.Log($"[CharacterModelManager] 找到PlayerInputHandler: {(inputHandler != null ? "是" : "否")}, 实例ID: {(inputHandler != null ? inputHandler.GetInstanceID() : "N/A")}");
            if (inputHandler != null)
            {
                inputHandler.UpdateAnimatorReference();
            }
            else
            {
                Debug.LogWarning("[CharacterModelManager] 未找到PlayerInputHandler组件！");
            }
        }
    }

    public void UpdateModel(string newCharacterId)
    {
        LoadCharacterModel(newCharacterId);
    }

    private void OnDestroy()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        foreach (var handle in loadedHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        loadedHandles.Clear();
    }
}