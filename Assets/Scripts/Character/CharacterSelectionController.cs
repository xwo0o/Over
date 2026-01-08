using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using Mirror;
using NetworkCore;

/// <summary>
/// 角色选择控制器 - 负责角色选择的业务逻辑
/// 根据MVC架构，此组件处理角色选择的业务逻辑，通过EventBus与View组件通信
/// </summary>
public class CharacterSelectionController : MVCController
{
    [Header("MVC组件")]
    public CharacterSelectionUIController characterView; // 使用重构后的UI Controller作为View
    
    [Header("角色预览")]
    public CharacterPreviewManager previewManager;
    
    [Header("选择设置")]
    public string[] characterIds = { "Scout", "Architect", "Guardian" };
    public float networkModeCooldownTime = 2f;
    
    private int currentCharacterIndex = 0;
    private string selectedCharacterId;
    private bool isCharacterSelectionEnabled = true;
    private NetworkPlayer localNetworkPlayer;

    void Start()
    {
        if (characterView == null)
        {
            characterView = FindObjectOfType<CharacterSelectionUIController>();
        }

        currentCharacterIndex = 0;
        selectedCharacterId = characterIds[0];
        
        // 通过View更新UI显示
        if (characterView != null)
        {
            characterView.UpdateCharacterDisplay(selectedCharacterId);
        }

        isCharacterSelectionEnabled = true;

        if (NetworkManager.singleton != null)
        {
            RegisterNetworkEvents();
        }
        else
        {
            Debug.Log("[CharacterSelectionController] Character场景不注册网络事件，等待GameScene加载");
        }

        // 订阅事件
        EventBus.Instance.Subscribe(GameEvents.CHARACTER_SELECTION_CONFIRMED, OnSelectionConfirmed);
    }

    void RegisterNetworkEvents()
    {
        if (NetworkConnectionManager.Instance != null)
        {
            Debug.Log("[CharacterSelectionController] 注册网络事件监听");
            
            if (NetworkConnectionManager.Instance.IsConnected)
            {
                Debug.Log("[CharacterSelectionController] 网络已连接，立即开始查找NetworkPlayer");
                StartCoroutine(FindLocalNetworkPlayer());
            }
            else
            {
                Debug.Log("[CharacterSelectionController] 网络未连接，等待连接事件");
                NetworkConnectionManager.Instance.OnConnected += OnNetworkConnected;
            }
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionController] NetworkConnectionManager未找到，无法注册网络事件");
        }
        
        NetworkPlayer.OnPlayerInitialized += OnNetworkPlayerInitialized;
    }

    void OnNetworkConnected()
    {
        Debug.Log("[CharacterSelectionController] 收到网络连接事件，开始查找NetworkPlayer");
        StartCoroutine(FindLocalNetworkPlayer());
    }

    void OnNetworkPlayerInitialized(NetworkPlayer player)
    {
        if (player.isLocalPlayer && localNetworkPlayer == null)
        {
            localNetworkPlayer = player;
            Debug.Log($"[CharacterSelectionController] 通过事件收到NetworkPlayer初始化完成通知: {player.netId}");
        }
    }

    void OnDestroy()
    {
        if (NetworkConnectionManager.Instance != null)
        {
            NetworkConnectionManager.Instance.OnConnected -= OnNetworkConnected;
        }
        
        NetworkPlayer.OnPlayerInitialized -= OnNetworkPlayerInitialized;
        
        // 取消订阅事件
        EventBus.Instance.Unsubscribe(GameEvents.CHARACTER_SELECTION_CONFIRMED, OnSelectionConfirmed);
    }

    public IEnumerator FindLocalNetworkPlayer()
    {
        Debug.Log($"[CharacterSelectionController] 开始查找本地NetworkPlayer...");
        
        int maxWaitForNetwork = 30;
        int networkWaitCount = 0;
        
        while (networkWaitCount < maxWaitForNetwork)
        {
            NetworkManager networkManager = NetworkManager.singleton;
            if (networkManager != null && NetworkClient.isConnected)
            {
                Debug.Log($"[CharacterSelectionController] 网络服务已就绪，开始查找NetworkPlayer");
                break;
            }
            
            networkWaitCount++;
            Debug.LogWarning($"[CharacterSelectionController] 等待网络服务启动... ({networkWaitCount}/{maxWaitForNetwork})");
            yield return new WaitForSeconds(1f);
        }
        
        if (networkWaitCount >= maxWaitForNetwork)
        {
            Debug.LogError("[CharacterSelectionController] 等待网络服务启动超时，无法查找NetworkPlayer");
            Debug.LogError("[CharacterSelectionController] 请确保已启动主机模式或连接到服务器");
            yield break;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        int maxAttempts = 30;
        int currentAttempt = 0;

        while (localNetworkPlayer == null && currentAttempt < maxAttempts)
        {
            currentAttempt++;
            NetworkIdentity[] networkIdentities = FindObjectsOfType<NetworkIdentity>();
            Debug.Log($"[CharacterSelectionController] 第{currentAttempt}次查找，场景中NetworkIdentity数量: {networkIdentities.Length}");
            
            foreach (NetworkIdentity identity in networkIdentities)
            {
                Debug.Log($"[CharacterSelectionController] 检查NetworkIdentity: {identity.netId}, isLocalPlayer: {identity.isLocalPlayer}");
                
                if (identity.isLocalPlayer)
                {
                    localNetworkPlayer = identity.GetComponent<NetworkPlayer>();
                    if (localNetworkPlayer != null)
                    {
                        Debug.Log($"[CharacterSelectionController] 找到本地NetworkPlayer: {localNetworkPlayer.netId}, 尝试次数: {currentAttempt}");
                        Debug.Log($"[CharacterSelectionController] NetworkPlayer isLocalPlayer标记: {localNetworkPlayer.isLocalPlayer}");
                        
                        // 等待NetworkPlayer完成初始化
                        Debug.Log($"[CharacterSelectionController] 等待NetworkPlayer完成初始化...");
                        int maxInitWait = 30;
                        int initWaitCount = 0;
                        
                        while (!localNetworkPlayer.IsInitialized && initWaitCount < maxInitWait)
                        {
                            initWaitCount++;
                            Debug.Log($"[CharacterSelectionController] 等待NetworkPlayer初始化... ({initWaitCount}/{maxInitWait}), IsInitialized: {localNetworkPlayer.IsInitialized}");
                            yield return new WaitForSeconds(0.2f);
                        }
                        
                        if (localNetworkPlayer.IsInitialized)
                        {
                            Debug.Log($"[CharacterSelectionController] NetworkPlayer初始化完成，总等待时间: {currentAttempt * 0.3f + initWaitCount * 0.2f:F1}秒");
                            yield break;
                        }
                        else
                        {
                            Debug.LogWarning($"[CharacterSelectionController] NetworkPlayer初始化超时，但已找到NetworkPlayer对象");
                            yield break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterSelectionController] 找到本地NetworkIdentity，但未找到NetworkPlayer组件，netId: {identity.netId}");
                    }
                }
            }
            
            if (localNetworkPlayer == null)
            {
                Debug.LogWarning($"[CharacterSelectionController] 第{currentAttempt}次查找NetworkPlayer失败，0.3秒后重试...");
                yield return new WaitForSeconds(0.3f);
            }
        }

        if (localNetworkPlayer == null)
        {
            Debug.LogError("[CharacterSelectionController] 查找NetworkPlayer失败");
            Debug.LogError("[CharacterSelectionController] 请检查以下配置:");
            Debug.LogError("  1. NetworkManager的Player Prefab是否正确设置");
            Debug.LogError("  2. Player Prefab是否挂载了NetworkPlayer组件");
            Debug.LogError("  3. 网络连接是否正常建立");
            Debug.LogError("  4. 是否在正确的场景中启动了网络");
            
            // 尝试获取更多调试信息
            NetworkIdentity[] allIdentities = FindObjectsOfType<NetworkIdentity>();
            Debug.Log($"[CharacterSelectionController] 场景中所有NetworkIdentity: {allIdentities.Length}个");
            foreach (var identity in allIdentities)
            {
                Debug.Log($"[CharacterSelectionController] NetworkIdentity - netId: {identity.netId}, isLocalPlayer: {identity.isLocalPlayer}, GameObject: {identity.name}");
            }
        }
    }

    void Update()
    {
        if (!isCharacterSelectionEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // 通过EventBus发布角色选择变更事件
            EventBus.Instance.Publish(GameEvents.CHARACTER_SELECTED, -1); // -1 表示上一个角色
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // 通过EventBus发布角色选择变更事件
            EventBus.Instance.Publish(GameEvents.CHARACTER_SELECTED, 1); // 1 表示下一个角色
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // 通过EventBus发布角色选择确认事件
            EventBus.Instance.Publish(GameEvents.CHARACTER_SELECTION_CONFIRMED, selectedCharacterId);
        }
    }

    void OnConfirmSelection()
    {
        Debug.Log($"[CharacterSelectionController] 用户确认选择角色: {selectedCharacterId}");

        isCharacterSelectionEnabled = false;
        
        // 通过View设置UI状态
        if (characterView != null)
        {
            characterView.SetConfirmButtonInteractable(false);
        }

        // 保存选择的角色ID到PlayerSelectionData
        PlayerSelectionData.SelectedCharacterId = selectedCharacterId;

        // 发布角色选择确认事件
        EventBus.Instance.Publish(GameEvents.CHARACTER_SELECTION_CONFIRMED, selectedCharacterId);
    }

    public void SendCharacterSelectionToServer()
    {
        Debug.Log($"[CharacterSelectionController] 开始发送角色选择到服务器: {selectedCharacterId}");

        if (localNetworkPlayer == null)
        {
            Debug.LogError("[CharacterSelectionController] 未找到本地NetworkPlayer，无法发送角色选择");
            return;
        }

        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[CharacterSelectionController] 网络未连接，无法发送角色选择");
            return;
        }

        localNetworkPlayer.CmdSelectCharacter(selectedCharacterId);
        Debug.Log($"[CharacterSelectionController] 已将角色选择发送到服务器: {selectedCharacterId}");
        StartCoroutine(VerifyCharacterDataUpload());
    }

    IEnumerator VerifyCharacterDataUpload()
    {
        Debug.Log($"[CharacterSelectionController] 开始验证角色数据上传...");
        
        int maxWaitTime = 5;
        float elapsedTime = 0f;
        
        while (elapsedTime < maxWaitTime)
        {
            if (localNetworkPlayer != null && !string.IsNullOrEmpty(localNetworkPlayer.selectedCharacterId))
            {
                if (localNetworkPlayer.selectedCharacterId == selectedCharacterId)
                {
                    Debug.Log($"[CharacterSelectionController] 角色数据上传验证成功！服务器端角色ID: {localNetworkPlayer.selectedCharacterId}");
                    yield break;
                }
                else
                {
                    Debug.LogWarning($"[CharacterSelectionController] 角色ID不匹配！本地: {selectedCharacterId}, 服务器: {localNetworkPlayer.selectedCharacterId}");
                }
            }
            
            elapsedTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogError($"[CharacterSelectionController] 角色数据上传验证失败！等待{maxWaitTime}秒后仍未收到服务器确认");
        Debug.LogError($"[CharacterSelectionController] 本地选择: {selectedCharacterId}, 服务器端: {(localNetworkPlayer != null ? localNetworkPlayer.selectedCharacterId : "null")}");
    }

    void OnSelectionConfirmed(object data)
    {
        if (data is string characterId)
        {
            selectedCharacterId = characterId;

            isCharacterSelectionEnabled = false;
            
            // 通过View设置UI状态
            if (characterView != null)
            {
                characterView.SetConfirmButtonInteractable(false);
                characterView.SetControlHintText($"请等待 {networkModeCooldownTime} 秒后选择网络模式...");
            }

            if (NetworkModeSelectionUIController.Instance != null)
            {
                NetworkModeSelectionUIController.Instance.ShowNetworkModeSelection();
                NetworkModeSelectionUIController.Instance.SetNetworkModeSelectionEnabled(false);
            }
            else
            {
                Debug.LogWarning("[CharacterSelectionController] NetworkModeSelectionUIController未设置，无法显示网络模式选择界面");
            }

            StartCoroutine(ShowNetworkModeSelectionWithCooldown());
        }
    }

    IEnumerator ShowNetworkModeSelectionWithCooldown()
    {
        Debug.Log($"[CharacterSelectionController] 开始 {networkModeCooldownTime} 秒冷却倒计时");

        yield return new WaitForSeconds(networkModeCooldownTime);

        Debug.Log($"[CharacterSelectionController] 冷却时间结束，启用网络模式选择");

        // 通过View设置UI状态
        if (characterView != null)
        {
            characterView.SetControlHintText("← → 选择模式 | Enter 确认");
        }

        if (NetworkModeSelectionUIController.Instance != null)
        {
            NetworkModeSelectionUIController.Instance.SetNetworkModeSelectionEnabled(true);
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionController] NetworkModeSelectionUIController未设置，无法启用网络模式选择界面");
        }
    }

    public bool IsNetworkPlayerReady()
    {
        return localNetworkPlayer != null && 
               NetworkClient.isConnected && 
               localNetworkPlayer.isLocalPlayer &&
               localNetworkPlayer.IsInitialized;
    }

    public IEnumerator WaitForNetworkPlayerReady(float maxWaitTime = 15f)
    {
        Debug.Log($"[CharacterSelectionController] 等待NetworkPlayer就绪，最长等待时间: {maxWaitTime}秒");
        
        float elapsedTime = 0f;
        float checkInterval = 0.2f;
        
        while (!IsNetworkPlayerReady() && elapsedTime < maxWaitTime)
        {
            elapsedTime += checkInterval;
            
            if (Mathf.Approximately(elapsedTime % 1f, 0f))
            {
                Debug.Log($"[CharacterSelectionController] 等待NetworkPlayer就绪中... 已等待: {elapsedTime:F1}秒");
                Debug.Log($"[CharacterSelectionController] 当前状态 - localNetworkPlayer: {(localNetworkPlayer != null ? "存在" : "不存在")}, " +
                         $"NetworkClient.isConnected: {NetworkClient.isConnected}, " +
                         $"isLocalPlayer: {(localNetworkPlayer != null ? localNetworkPlayer.isLocalPlayer.ToString() : "N/A")}, " +
                         $"IsInitialized: {(localNetworkPlayer != null ? localNetworkPlayer.IsInitialized.ToString() : "N/A")}");
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
        
        if (IsNetworkPlayerReady())
        {
            Debug.Log($"[CharacterSelectionController] NetworkPlayer已就绪，等待时间: {elapsedTime:F1}秒");
        }
        else
        {
            Debug.LogError($"[CharacterSelectionController] 等待NetworkPlayer就绪超时，已等待: {elapsedTime:F1}秒");
            Debug.LogError($"[CharacterSelectionController] 最终状态 - localNetworkPlayer: {(localNetworkPlayer != null ? "存在" : "不存在")}, " +
                         $"NetworkClient.isConnected: {NetworkClient.isConnected}, " +
                         $"isLocalPlayer: {(localNetworkPlayer != null ? localNetworkPlayer.isLocalPlayer.ToString() : "N/A")}, " +
                         $"IsInitialized: {(localNetworkPlayer != null ? localNetworkPlayer.IsInitialized.ToString() : "N/A")}");
        }
    }
}