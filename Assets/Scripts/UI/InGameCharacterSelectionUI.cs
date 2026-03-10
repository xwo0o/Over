using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Mirror;

/// <summary>
/// 游戏内角色选择UI - 按Q键打开，使用左右按钮切换角色，支持无限循环
/// </summary>
public class InGameCharacterSelectionUI : MonoBehaviour
{
    [Header("UI面板")]
    public GameObject selectionPanel;
    public RawImage characterRawImage;      // 显示3D模型的RawImage
    public TextMeshProUGUI characterIdText; // 角色ID文本（TMP）
    
    [Header("切换按钮")]
    public Button leftButton;               // 左切换按钮 (<)
    public Button rightButton;              // 右切换按钮 (>)
    
    [Header("操作按钮")]
    public Button confirmButton;            // 确认更换按钮
    public Button closeButton;              // 关闭按钮
    
    [Header("按键设置")]
    public KeyCode toggleKey = KeyCode.Q;
    
    private int currentIndex = 0;           // 当前预览索引
    private string currentCharacterId = ""; // 当前实际使用的角色ID
    private bool isPanelOpen = false;
    private NetworkPlayer localPlayer;
    private string[] characterIds = { "01", "02", "03", "04" };
    
    public static InGameCharacterSelectionUI Instance { get; private set; }
    
    void Awake()
    {
        Debug.Log("[InGameCharacterSelectionUI] Awake 被调用");
        
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[InGameCharacterSelectionUI] 已存在实例，销毁重复实例");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[InGameCharacterSelectionUI] 单例实例已创建");
        
        // 初始隐藏面板
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
            Debug.Log("[InGameCharacterSelectionUI] 面板初始隐藏");
        }
        else
        {
            Debug.LogWarning("[InGameCharacterSelectionUI] selectionPanel 为空");
        }
    }
    
    void Start()
    {
        SetupButtons();
        FindLocalPlayer();
        
        // 等待CharacterPreviewPool加载完成后更新显示
        StartCoroutine(WaitForPreviewPool());
    }
    
    // Q键检测已移到 PlayerInputHandler，这里不再检测
    // void Update() { }
    
    /// <summary>
    /// 等待预览池加载完成
    /// </summary>
    IEnumerator WaitForPreviewPool()
    {
        // 等待CharacterPreviewPool初始化
        float maxWaitTime = 10f;
        float elapsedTime = 0f;
        
        while ((CharacterPreviewPool.Instance == null || !CharacterPreviewPool.Instance.IsLoaded()) 
               && elapsedTime < maxWaitTime)
        {
            elapsedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (CharacterPreviewPool.Instance != null && CharacterPreviewPool.Instance.IsLoaded())
        {
            // 同步当前显示的角色ID
            string displayedId = CharacterPreviewPool.Instance.GetCurrentDisplayedId();
            currentIndex = System.Array.IndexOf(characterIds, displayedId);
            if (currentIndex < 0) currentIndex = 0;
            
            UpdateDisplay();
            Debug.Log($"[InGameCharacterSelectionUI] 预览池加载完成，当前显示: {displayedId}");
        }
        else
        {
            Debug.LogWarning("[InGameCharacterSelectionUI] 等待预览池超时");
        }
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtons()
    {
        // 左切换按钮
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(OnLeftButtonClicked);
        }
        
        // 右切换按钮
        if (rightButton != null)
        {
            rightButton.onClick.AddListener(OnRightButtonClicked);
        }
        
        // 确认按钮
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        
        // 关闭按钮
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }
    
    /// <summary>
    /// 查找本地玩家
    /// </summary>
    void FindLocalPlayer()
    {
        StartCoroutine(FindLocalPlayerCoroutine());
    }
    
    IEnumerator FindLocalPlayerCoroutine()
    {
        float maxWaitTime = 10f;
        float elapsedTime = 0f;
        
        while (localPlayer == null && elapsedTime < maxWaitTime)
        {
            NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
            foreach (var player in players)
            {
                if (player.isLocalPlayer)
                {
                    localPlayer = player;
                    currentCharacterId = player.selectedCharacterId;
                    
                    // 同步当前索引
                    currentIndex = System.Array.IndexOf(characterIds, currentCharacterId);
                    if (currentIndex < 0) currentIndex = 0;
                    
                    UpdateDisplay();
                    Debug.Log($"[InGameCharacterSelectionUI] 找到本地玩家，当前角色: {currentCharacterId}");
                    yield break;
                }
            }
            
            elapsedTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (localPlayer == null)
        {
            Debug.LogWarning("[InGameCharacterSelectionUI] 未找到本地玩家");
        }
    }
    
    /// <summary>
    /// 左按钮点击 - 切换到上一个角色（无限循环）
    /// </summary>
    void OnLeftButtonClicked()
    {
        currentIndex--;
        
        // 无限循环：如果小于0，跳到最后一个
        if (currentIndex < 0)
        {
            currentIndex = characterIds.Length - 1;
        }
        
        // 更新预览显示
        string newCharacterId = characterIds[currentIndex];
        if (CharacterPreviewPool.Instance != null)
        {
            CharacterPreviewPool.Instance.ShowCharacter(newCharacterId);
        }
        
        UpdateDisplay();
        Debug.Log($"[InGameCharacterSelectionUI] 切换到上一个角色: {newCharacterId}");
    }
    
    /// <summary>
    /// 右按钮点击 - 切换到下一个角色（无限循环）
    /// </summary>
    void OnRightButtonClicked()
    {
        currentIndex++;
        
        // 无限循环：如果超出范围，跳到第一个
        if (currentIndex >= characterIds.Length)
        {
            currentIndex = 0;
        }
        
        // 更新预览显示
        string newCharacterId = characterIds[currentIndex];
        if (CharacterPreviewPool.Instance != null)
        {
            CharacterPreviewPool.Instance.ShowCharacter(newCharacterId);
        }
        
        UpdateDisplay();
        Debug.Log($"[InGameCharacterSelectionUI] 切换到下一个角色: {newCharacterId}");
    }
    
    /// <summary>
    /// 确认按钮点击
    /// </summary>
    void OnConfirmButtonClicked()
    {
        string selectedCharacterId = characterIds[currentIndex];
        
        if (string.IsNullOrEmpty(selectedCharacterId))
        {
            Debug.LogWarning("[InGameCharacterSelectionUI] 未选择角色");
            return;
        }
        
        if (selectedCharacterId == currentCharacterId)
        {
            Debug.Log("[InGameCharacterSelectionUI] 选择的角色与当前角色相同，无需切换");
            ClosePanel();
            return;
        }
        
        // 发送切换命令到服务器
        if (localPlayer != null)
        {
            localPlayer.CmdSwitchCharacter(selectedCharacterId);
            Debug.Log($"[InGameCharacterSelectionUI] 发送角色切换命令: {currentCharacterId} -> {selectedCharacterId}");
        }
        else
        {
            Debug.LogError("[InGameCharacterSelectionUI] 本地玩家为空，无法切换角色");
        }
        
        ClosePanel();
    }
    
    /// <summary>
    /// 切换面板显示状态
    /// </summary>
    public void TogglePanel()
    {
        if (isPanelOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }
    
    /// <summary>
    /// 打开面板
    /// </summary>
    public void OpenPanel()
    {
        if (selectionPanel == null) return;
        
        // 更新当前角色显示
        if (localPlayer != null)
        {
            currentCharacterId = localPlayer.selectedCharacterId;
            currentIndex = System.Array.IndexOf(characterIds, currentCharacterId);
            if (currentIndex < 0) currentIndex = 0;
            
            // 同步预览池显示
            if (CharacterPreviewPool.Instance != null)
            {
                CharacterPreviewPool.Instance.ShowCharacter(characterIds[currentIndex]);
            }
        }
        
        UpdateDisplay();
        
        selectionPanel.SetActive(true);
        isPanelOpen = true;
        
        Debug.Log("[InGameCharacterSelectionUI] 打开角色选择面板");
    }
    
    /// <summary>
    /// 关闭面板
    /// </summary>
    public void ClosePanel()
    {
        if (selectionPanel == null) return;
        
        selectionPanel.SetActive(false);
        isPanelOpen = false;
        
        Debug.Log("[InGameCharacterSelectionUI] 关闭角色选择面板");
    }
    
    /// <summary>
    /// 更新显示
    /// </summary>
    void UpdateDisplay()
    {
        string displayedId = characterIds[currentIndex];
        
        // 只更新角色ID文本
        if (characterIdText != null)
        {
            characterIdText.text = $"角色 {displayedId}";
        }
    }
    
    /// <summary>
    /// 更新当前角色ID（由NetworkPlayer调用）
    /// </summary>
    public void UpdateCurrentCharacterId(string characterId)
    {
        currentCharacterId = characterId;
        currentIndex = System.Array.IndexOf(characterIds, characterId);
        if (currentIndex < 0) currentIndex = 0;
        UpdateDisplay();
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
