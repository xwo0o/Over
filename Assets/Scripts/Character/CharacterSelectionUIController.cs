using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;
using NetworkCore;

/// <summary>
/// 角色选择视图 - 负责显示角色选择界面和处理用户输入
/// 根据MVC架构，此组件只负责UI显示和用户输入事件，业务逻辑由Controller处理
/// </summary>
public class CharacterSelectionUIController : MonoBehaviour
{
    [Header("角色选择UI")]
    public GameObject characterSelectionPanel;
    public Text characterNameText;
    public Text characterDescriptionText;
    public Text characterStatsText;
    public Text controlHintText;
    public Button confirmSelectionButton;

    [Header("角色预览")]
    public CharacterPreviewManager previewManager;

    [Header("网络模式选择")]
    public NetworkModeSelectionUIController networkModeSelectionController;

    [Header("操作间隔设置")]
    public float networkModeCooldownTime = 2f;

    private string[] characterIds = { "Scout", "Architect", "Guardian" };
    private int currentCharacterIndex = 0;
    private string selectedCharacterId;
    private bool isCharacterSelectionEnabled = true;

    public string SelectedCharacterId => selectedCharacterId;

    void Start()
    {
        // 初始化UI显示
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(true);
        }

        // 设置确认按钮事件监听器
        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.onClick.AddListener(OnConfirmSelection);
        }

        // 设置控制提示文本
        if (controlHintText != null)
        {
            controlHintText.text = "← → 切换角色 | Enter 确认选择";
        }

        isCharacterSelectionEnabled = true;

        // 订阅事件
        EventBus.Instance.Subscribe(GameEvents.CHARACTER_SELECTED, OnCharacterSelected);
        EventBus.Instance.Subscribe(GameEvents.CHARACTER_SELECTION_CONFIRMED, OnCharacterSelectionConfirmed);
    }

    void OnDestroy()
    {
        // 取消订阅事件
        EventBus.Instance.Unsubscribe(GameEvents.CHARACTER_SELECTED, OnCharacterSelected);
        EventBus.Instance.Unsubscribe(GameEvents.CHARACTER_SELECTION_CONFIRMED, OnCharacterSelectionConfirmed);
    }

    void Update()
    {
        if (!isCharacterSelectionEnabled)
            return;

        // 处理用户输入事件
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
            OnConfirmSelection();
        }
    }

    /// <summary>
    /// 更新角色显示
    /// </summary>
    /// <param name="characterId">要显示的角色ID</param>
    public void UpdateCharacterDisplay(string characterId)
    {
        if (CharacterDatabase.Instance == null)
            return;

        CharacterData data = CharacterDatabase.Instance.GetCharacter(characterId);
        if (data == null)
            return;

        if (characterNameText != null)
        {
            characterNameText.text = data.name;
        }

        if (characterDescriptionText != null)
        {
            characterDescriptionText.text = $"特殊能力: {data.specialAbility}";
        }

        if (characterStatsText != null)
        {
            characterStatsText.text = $"血量: {data.health}\n攻击: {data.attack}\n速度: {data.speed}\n特殊值: {data.specialValue}";
        }

        if (previewManager != null)
        {
            previewManager.LoadCharacterModel(characterId);
        }
    }

    /// <summary>
    /// 设置角色选择按钮的交互状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetCharacterSelectionInteractable(bool interactable)
    {
        isCharacterSelectionEnabled = interactable;
    }

    /// <summary>
    /// 设置确认按钮的交互状态
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetConfirmButtonInteractable(bool interactable)
    {
        if (confirmSelectionButton != null)
        {
            confirmSelectionButton.interactable = interactable;
        }
    }

    /// <summary>
    /// 设置控制提示文本
    /// </summary>
    /// <param name="text">提示文本</param>
    public void SetControlHintText(string text)
    {
        if (controlHintText != null)
        {
            controlHintText.text = text;
        }
    }

    /// <summary>
    /// 设置角色选择面板的激活状态
    /// </summary>
    /// <param name="active">是否激活</param>
    public void SetCharacterSelectionPanelActive(bool active)
    {
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(active);
        }
    }

    /// <summary>
    /// 确认选择角色
    /// </summary>
    void OnConfirmSelection()
    {
        Debug.Log($"[CharacterSelectionUIController] 用户确认选择角色: {selectedCharacterId}");

        // 通过EventBus发布角色选择确认事件
        EventBus.Instance.Publish(GameEvents.CHARACTER_SELECTION_CONFIRMED, selectedCharacterId);
    }

    /// <summary>
    /// 处理角色选择事件
    /// </summary>
    /// <param name="data">角色选择数据，-1表示上一个角色，1表示下一个角色，字符串表示具体角色ID</param>
    void OnCharacterSelected(object data)
    {
        if (data is int direction)
        {
            if (direction == -1)
            {
                // 选择上一个角色
                currentCharacterIndex--;
                if (currentCharacterIndex < 0)
                {
                    currentCharacterIndex = characterIds.Length - 1;
                }
            }
            else if (direction == 1)
            {
                // 选择下一个角色
                currentCharacterIndex++;
                if (currentCharacterIndex >= characterIds.Length)
                {
                    currentCharacterIndex = 0;
                }
            }

            selectedCharacterId = characterIds[currentCharacterIndex];
            UpdateCharacterDisplay(selectedCharacterId);
        }
        else if (data is string characterId)
        {
            // 直接选择特定角色
            for (int i = 0; i < characterIds.Length; i++)
            {
                if (characterIds[i] == characterId)
                {
                    currentCharacterIndex = i;
                    selectedCharacterId = characterId;
                    UpdateCharacterDisplay(selectedCharacterId);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 处理角色选择确认事件
    /// </summary>
    /// <param name="data">选择的角色ID</param>
    void OnCharacterSelectionConfirmed(object data)
    {
        if (data is string characterId)
        {
            selectedCharacterId = characterId;
            
            // 保存角色ID到PlayerSelectionData
            PlayerSelectionData.SelectedCharacterId = selectedCharacterId;
            Debug.Log($"[CharacterSelectionUIController] 已保存角色ID到PlayerSelectionData: {selectedCharacterId}");
            
            isCharacterSelectionEnabled = false;
            SetConfirmButtonInteractable(false);
            SetControlHintText($"请等待 {networkModeCooldownTime} 秒后选择网络模式...");

            if (networkModeSelectionController != null)
            {
                networkModeSelectionController.ShowNetworkModeSelection();
                networkModeSelectionController.SetNetworkModeSelectionEnabled(false);
            }
            else
            {
                Debug.LogWarning("[CharacterSelectionUIController] NetworkModeSelectionUIController未设置，无法显示网络模式选择界面");
            }

            StartCoroutine(ShowNetworkModeSelectionWithCooldown());
        }
    }

    /// <summary>
    /// 显示网络模式选择界面并带冷却时间
    /// </summary>
    /// <returns>协程</returns>
    IEnumerator ShowNetworkModeSelectionWithCooldown()
    {
        Debug.Log($"[CharacterSelectionUIController] 开始 {networkModeCooldownTime} 秒冷却倒计时");

        yield return new WaitForSeconds(networkModeCooldownTime);

        Debug.Log($"[CharacterSelectionUIController] 冷却时间结束，启用网络模式选择");

        SetControlHintText("← → 选择模式 | Enter 确认");

        if (networkModeSelectionController != null)
        {
            networkModeSelectionController.SetNetworkModeSelectionEnabled(true);
        }
        else
        {
            Debug.LogWarning("[CharacterSelectionUIController] NetworkModeSelectionUIController未设置，无法启用网络模式选择界面");
        }
    }
}
