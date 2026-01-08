using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;
using NetworkCore;

public class NetworkModeSelectionController : MVCController
{
    [Header("MVC组件")]
    public NetworkModeSelectionModel model;
    public NetworkModeSelectionView view;
    
    [Header("角色选择控制器")]
    public CharacterSelectionController characterSelectionController;

    [Header("场景设置")]
    [Tooltip("是否在Character场景中使用（只保存数据，不启动网络）")]
    public bool isCharacterScene = false;

    [Header("场景切换设置")]
    [Tooltip("在Character场景保存数据后是否自动切换到GameScene")]
    public bool autoLoadGameScene = true;
    [Tooltip("场景切换前的延迟时间（秒）")]
    public float sceneSwitchDelay = 0.5f;

    void Start()
    {
        // 自动检测当前场景
        string currentScene = SceneManager.GetActiveScene().name;
        isCharacterScene = (currentScene == "Character");
        Debug.Log($"[NetworkModeSelectionController] 自动检测场景: {currentScene}, isCharacterScene: {isCharacterScene}");

        if (characterSelectionController == null)
        {
            characterSelectionController = FindObjectOfType<CharacterSelectionController>();
            if (characterSelectionController != null)
            {
                Debug.Log("[NetworkModeSelectionController] 自动找到CharacterSelectionController");
            }
            else
            {
                Debug.LogWarning("[NetworkModeSelectionController] 未找到CharacterSelectionController，请在Inspector中手动设置");
            }
        }

        // 初始化MVC组件
        if (model == null)
        {
            if (NetworkModeSelectionModel.Instance == null)
            {
                model = new NetworkModeSelectionModel();
            }
            else
            {
                model = NetworkModeSelectionModel.Instance;
            }
        }

        if (view == null)
        {
            view = FindObjectOfType<NetworkModeSelectionView>();
        }

        // 订阅事件
        Debug.Log("[NetworkModeSelectionController] 订阅网络模式选择事件");
        EventBus.Instance.Subscribe(GameEvents.NETWORK_MODE_SELECTED, OnNetworkModeSelected);
    }

    void OnDestroy()
    {
        // 取消订阅事件
        EventBus.Instance.Unsubscribe(GameEvents.NETWORK_MODE_SELECTED, OnNetworkModeSelected);
    }

    void OnNetworkModeSelected(object mode)
    {
        Debug.Log($"[NetworkModeSelectionController] 收到网络模式选择事件: {mode}");
        
        if (mode is NetworkMode networkMode)
        {
            if (networkMode == NetworkMode.Host)
            {
                Debug.Log("[NetworkModeSelectionController] 处理主机模式选择");
                OnHostModeSelected();
            }
            else if (networkMode == NetworkMode.Client)
            {
                Debug.Log("[NetworkModeSelectionController] 处理客户端模式选择");
                OnClientModeSelected();
            }
        }
    }

    public void OnHostModeSelected()
    {
        // 验证角色是否已选择
        if (characterSelectionController == null || string.IsNullOrEmpty(PlayerSelectionData.SelectedCharacterId))
        {
            Debug.LogError("[NetworkModeSelectionController] 未选择角色，无法启动主机模式");
            return;
        }

        model.SelectHostMode();

        if (view != null)
        {
            view.UpdateButtonSelectionVisuals(view.hostModeButton, true);
            view.UpdateButtonSelectionVisuals(view.clientModeButton, false);
        }

        StartNetworkMode(NetworkMode.Host);
    }

    public void OnClientModeSelected()
    {
        // 验证角色是否已选择
        if (characterSelectionController == null || string.IsNullOrEmpty(PlayerSelectionData.SelectedCharacterId))
        {
            Debug.LogError("[NetworkModeSelectionController] 未选择角色，无法启动客户端模式");
            return;
        }

        model.SelectClientMode();

        if (view != null)
        {
            view.UpdateButtonSelectionVisuals(view.hostModeButton, false);
            view.UpdateButtonSelectionVisuals(view.clientModeButton, true);
        }

        StartNetworkMode(NetworkMode.Client);
    }

    void StartNetworkMode(NetworkMode mode)
    {
        // 自动检测场景
        string currentScene = SceneManager.GetActiveScene().name;
        isCharacterScene = (currentScene == "Character");

        // 检查NetworkManager（仅在非Character场景中）
        if (NetworkManager.singleton == null && !isCharacterScene)
        {
            Debug.LogError($"[NetworkModeSelectionController] NetworkManager未找到 (当前场景: {currentScene})");
            return;
        }

        Debug.Log($"[NetworkModeSelectionController] 启动{mode}模式 (当前场景: {currentScene})");
        
        // 如果在Character场景中，只保存数据，不启动网络
        if (isCharacterScene)
        {
            Debug.Log("[NetworkModeSelectionController] Character场景模式：只保存数据，不启动网络");
            
            // 自动切换到GameScene
            if (autoLoadGameScene)
            {
                StartCoroutine(LoadGameSceneWithDelay(sceneSwitchDelay));
            }
            return;
        }
        
        // 在GameScene中启动网络
        if (mode == NetworkMode.Host)
        {
            NetworkManager.singleton.StartHost();
        }
        else if (mode == NetworkMode.Client)
        {
            NetworkManager.singleton.networkAddress = "localhost";
            NetworkManager.singleton.StartClient();
        }
        
        StartCoroutine(SendCharacterSelectionAfterNetworkReady());
    }

    IEnumerator SendCharacterSelectionAfterNetworkReady()
    {
        Debug.Log("[NetworkModeSelectionController] 等待网络连接就绪...");
        
        int maxWaitTime = 10;
        float elapsedTime = 0f;
        
        while (!NetworkClient.isConnected && elapsedTime < maxWaitTime)
        {
            elapsedTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (!NetworkClient.isConnected)
        {
            Debug.LogError("[NetworkModeSelectionController] 网络连接超时");
            yield break;
        }
        
        Debug.Log("[NetworkModeSelectionController] 网络连接就绪，等待NetworkPlayer创建完成...");
        
        if (characterSelectionController == null)
        {
            Debug.LogError("[NetworkModeSelectionController] CharacterSelectionController未设置，无法等待NetworkPlayer就绪");
            yield break;
        }
        
        // 使用更长的超时时间
        yield return characterSelectionController.WaitForNetworkPlayerReady(20f);
        
        if (!characterSelectionController.IsNetworkPlayerReady())
        {
            Debug.LogError("[NetworkModeSelectionController] NetworkPlayer未就绪，无法发送角色选择");
            
            // 添加重试机制
            Debug.Log("[NetworkModeSelectionController] 尝试重新查找NetworkPlayer...");
            yield return characterSelectionController.StartCoroutine(characterSelectionController.FindLocalNetworkPlayer());
            
            if (!characterSelectionController.IsNetworkPlayerReady())
            {
                Debug.LogError("[NetworkModeSelectionController] 重试后仍然无法找到就绪的NetworkPlayer");
                yield break;
            }
        }
        
        Debug.Log("[NetworkModeSelectionController] NetworkPlayer已就绪，发送角色选择数据");
        
        characterSelectionController.SendCharacterSelectionToServer();
        
        yield return new WaitForSeconds(1f);
        
        LoadGameScene();
    }

    void LoadGameScene()
    {
        Debug.Log("[NetworkModeSelectionController] 加载GameScene场景");
        SceneManager.LoadScene("GameScene");
    }

    IEnumerator LoadGameSceneWithDelay(float delay)
    {
        Debug.Log($"[NetworkModeSelectionController] 将在{delay}秒后加载GameScene场景");
        
        // 验证保存的数据
        if (!PlayerSelectionData.IsDataSaved)
        {
            Debug.LogError("[NetworkModeSelectionController] 数据未保存，无法切换场景");
            yield break;
        }
        
        if (!PlayerSelectionData.IsValidData())
        {
            Debug.LogError("[NetworkModeSelectionController] 保存的数据无效，无法切换场景");
            Debug.LogError($"[NetworkModeSelectionController] 角色ID: {PlayerSelectionData.SelectedCharacterId}");
            Debug.LogError($"[NetworkModeSelectionController] 网络模式: {PlayerSelectionData.SelectedNetworkMode}");
            yield break;
        }
        
        Debug.Log($"[NetworkModeSelectionController] 数据验证通过:");
        Debug.Log($"  角色ID: {PlayerSelectionData.SelectedCharacterId}");
        Debug.Log($"  网络模式: {PlayerSelectionData.GetNetworkModeDescription(PlayerSelectionData.SelectedNetworkMode)}");
        Debug.Log($"  服务器地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");
        
        yield return new WaitForSeconds(delay);
        LoadGameScene();
    }

    public void ResetSelection()
    {
        model.ResetSelection();
        
        if (view != null)
        {
            view.UpdateButtonSelectionVisuals(view.hostModeButton, false);
            view.UpdateButtonSelectionVisuals(view.clientModeButton, false);
        }
    }

    public void ShowNetworkModeSelection()
    {
        if (view != null)
        {
            view.ShowNetworkModeSelection();
        }
    }

    public void HideNetworkModeSelection()
    {
        if (view != null)
        {
            view.HideNetworkModeSelection();
        }
    }

    public void SetNetworkModeSelectionEnabled(bool enabled)
    {
        if (view != null)
        {
            view.SetNetworkModeSelectionEnabled(enabled);
        }
    }

    public NetworkMode GetSelectedMode()
    {
        return model.GetCurrentSelection();
    }
}