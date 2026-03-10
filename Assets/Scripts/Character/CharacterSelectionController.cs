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
    public string[] characterIds = { "01", "02", "03", "04" };
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

        // 订阅事件
        EventBus.Instance.Subscribe(GameEvents.CHARACTER_SELECTION_CONFIRMED, OnSelectionConfirmed);
    }

    void RegisterNetworkEvents()
    {
        if (NetworkConnectionManager.Instance != null)
        {
            if (NetworkConnectionManager.Instance.IsConnected)
            {
                StartCoroutine(FindLocalNetworkPlayer());
            }
            else
            {
                NetworkConnectionManager.Instance.OnConnected += OnNetworkConnected;
            }
        }
        
        NetworkPlayer.OnPlayerInitialized += OnNetworkPlayerInitialized;
    }

    void OnNetworkConnected()
    {
        StartCoroutine(FindLocalNetworkPlayer());
    }

    void OnNetworkPlayerInitialized(NetworkPlayer player)
    {
        if (player.isLocalPlayer && localNetworkPlayer == null)
        {
            localNetworkPlayer = player;
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
        int maxWaitForNetwork = 30;
        int networkWaitCount = 0;
        
        while (networkWaitCount < maxWaitForNetwork)
        {
            NetworkManager networkManager = NetworkManager.singleton;
            if (networkManager != null && NetworkClient.isConnected)
            {
                break;
            }
            
            networkWaitCount++;
            yield return new WaitForSeconds(1f);
        }
        
        if (networkWaitCount >= maxWaitForNetwork)
        {
            yield break;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        int maxAttempts = 30;
        int currentAttempt = 0;

        while (localNetworkPlayer == null && currentAttempt < maxAttempts)
        {
            currentAttempt++;
            NetworkIdentity[] networkIdentities = FindObjectsOfType<NetworkIdentity>();
            
            foreach (NetworkIdentity identity in networkIdentities)
            {
                if (identity.isLocalPlayer)
                {
                    localNetworkPlayer = identity.GetComponent<NetworkPlayer>();
                    if (localNetworkPlayer != null)
                    {
                        // 等待NetworkPlayer完成初始化
                        int maxInitWait = 30;
                        int initWaitCount = 0;
                        
                        while (!localNetworkPlayer.IsInitialized && initWaitCount < maxInitWait)
                        {
                            initWaitCount++;
                            yield return new WaitForSeconds(0.2f);
                        }
                        
                        yield break;
                    }
                }
            }
            
            if (localNetworkPlayer == null)
            {
                yield return new WaitForSeconds(0.3f);
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
        if (localNetworkPlayer == null)
        {
            return;
        }

        if (!NetworkClient.isConnected)
        {
            return;
        }

        localNetworkPlayer.CmdSelectCharacter(selectedCharacterId);
        StartCoroutine(VerifyCharacterDataUpload());
    }

    IEnumerator VerifyCharacterDataUpload()
    {
        int maxWaitTime = 5;
        float elapsedTime = 0f;
        
        while (elapsedTime < maxWaitTime)
        {
            if (localNetworkPlayer != null && !string.IsNullOrEmpty(localNetworkPlayer.selectedCharacterId))
            {
                if (localNetworkPlayer.selectedCharacterId == selectedCharacterId)
                {
                    yield break;
                }
            }
            
            elapsedTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
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

            StartCoroutine(ShowNetworkModeSelectionWithCooldown());
        }
    }

    IEnumerator ShowNetworkModeSelectionWithCooldown()
    {
        yield return new WaitForSeconds(networkModeCooldownTime);

        // 通过View设置UI状态
        if (characterView != null)
        {
            characterView.SetControlHintText("← → 选择模式 | Enter 确认");
        }

        if (NetworkModeSelectionUIController.Instance != null)
        {
            NetworkModeSelectionUIController.Instance.SetNetworkModeSelectionEnabled(true);
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
        float elapsedTime = 0f;
        float checkInterval = 0.2f;
        
        while (!IsNetworkPlayerReady() && elapsedTime < maxWaitTime)
        {
            elapsedTime += checkInterval;
            yield return new WaitForSeconds(checkInterval);
        }
    }
}
