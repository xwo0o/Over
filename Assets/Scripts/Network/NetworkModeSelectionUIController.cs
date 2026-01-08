using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;
using NetworkCore;

public class NetworkModeSelectionUIController : MonoBehaviour
{
    public static NetworkModeSelectionUIController Instance { get; private set; }
    
    [Header("UI组件")]
    public GameObject networkModePanel;
    public Button hostModeButton;
    public Button clientModeButton;

    [Header("按钮样式")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 1f);
    public Color selectedColor = new Color(0.5f, 0.8f, 1f);
    public Color focusColor = new Color(0.7f, 0.9f, 1f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("焦点设置")]
    public Button initialFocusedButton;

    [Header("动画设置")]
    public float transitionDuration = 0.3f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("场景设置")]
    [Tooltip("是否在Character场景中使用（只保存数据，不启动网络）")]
    public bool isCharacterScene = false;

    [Header("场景切换设置")]
    [Tooltip("在Character场景保存数据后是否自动切换到GameScene")]
    public bool autoLoadGameScene = true;
    [Tooltip("场景切换前的延迟时间（秒）")]
    public float sceneSwitchDelay = 0.5f;

    private Button[] navigationButtons;
    private int currentFocusIndex = 0;
    private bool isEnabled = true;
    private CanvasGroup canvasGroup;
    private Coroutine transitionCoroutine;
    private bool isAutoDetectingScene = true;

    void Start()
    {
        // 初始化单例
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[NetworkModeSelectionUIController] 已存在实例，销毁重复实例");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 自动检测当前场景
        if (isAutoDetectingScene)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            isCharacterScene = (currentScene == "Character");
            Debug.Log($"[NetworkModeSelectionUIController] 自动检测场景: {currentScene}, isCharacterScene: {isCharacterScene}");
        }

        if (networkModePanel != null)
        {
            networkModePanel.SetActive(false);
            
            canvasGroup = networkModePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = networkModePanel.AddComponent<CanvasGroup>();
            }
        }

        InitializeButtons();
        SetupNavigation();
        
        // 添加按钮点击事件监听器
        if (hostModeButton != null)
        {
            hostModeButton.onClick.AddListener(OnHostModeButtonClicked);
        }

        if (clientModeButton != null)
        {
            clientModeButton.onClick.AddListener(OnClientModeButtonClicked);
        }
    }

    void InitializeButtons()
    {
        navigationButtons = new Button[] { hostModeButton, clientModeButton };

        if (hostModeButton != null)
        {
            SetupButtonVisuals(hostModeButton);
        }

        if (clientModeButton != null)
        {
            SetupButtonVisuals(clientModeButton);
        }
    }

    void SetupButtonVisuals(Button button)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = selectedColor;
        colors.selectedColor = selectedColor;
        button.colors = colors;
    }

    void SetupNavigation()
    {
        if (hostModeButton != null && clientModeButton != null)
        {
            Navigation navigation = new Navigation();
            navigation.mode = Navigation.Mode.Explicit;

            navigation.selectOnUp = null;
            navigation.selectOnDown = null;
            navigation.selectOnLeft = clientModeButton;
            navigation.selectOnRight = clientModeButton;

            hostModeButton.navigation = navigation;

            navigation.selectOnLeft = hostModeButton;
            navigation.selectOnRight = hostModeButton;

            clientModeButton.navigation = navigation;
        }
    }

    void Update()
    {
        if (!networkModePanel.activeSelf || !isEnabled)
            return;

        HandleKeyboardNavigation();
    }

    void HandleKeyboardNavigation()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveFocus(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveFocus(1);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ActivateFocusedButton();
        }
    }

    void MoveFocus(int direction)
    {
        currentFocusIndex = (currentFocusIndex + direction + navigationButtons.Length) % navigationButtons.Length;
        UpdateFocus();
    }

    void UpdateFocus()
    {
        if (currentFocusIndex >= 0 && currentFocusIndex < navigationButtons.Length)
        {
            Button focusedButton = navigationButtons[currentFocusIndex];
            if (focusedButton != null)
            {
                focusedButton.Select();
                UpdateButtonFocusVisuals(focusedButton, true);
            }
        }
    }

    void UpdateButtonFocusVisuals(Button button, bool hasFocus)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = hasFocus ? focusColor : normalColor;
        }
    }

    void ActivateFocusedButton()
    {
        if (currentFocusIndex >= 0 && currentFocusIndex < navigationButtons.Length)
        {
            Button focusedButton = navigationButtons[currentFocusIndex];
            if (focusedButton != null)
            {
                focusedButton.onClick.Invoke();
            }
        }
    }

    public void ShowNetworkModeSelection()
    {
        if (networkModePanel != null)
        {
            networkModePanel.SetActive(true);
            currentFocusIndex = 0;
            UpdateFocus();
            Debug.Log("[NetworkModeSelectionUIController] 显示网络模式选择界面");
        }
    }

    public void HideNetworkModeSelection()
    {
        if (networkModePanel != null)
        {
            networkModePanel.SetActive(false);
            Debug.Log("[NetworkModeSelectionUIController] 隐藏网络模式选择界面");
        }
    }

    void OnHostModeButtonClicked()
    {
        UpdateButtonSelectionVisuals(hostModeButton, true);
        UpdateButtonSelectionVisuals(clientModeButton, false);
        Debug.Log("[NetworkModeSelectionUIController] 用户选择主机模式");

        // 验证角色是否已选择
        if (string.IsNullOrEmpty(PlayerSelectionData.SelectedCharacterId))
        {
            Debug.LogError("[NetworkModeSelectionUIController] 未选择角色，无法启动主机模式");
            return;
        }

        // 保存完整的玩家选择数据（包括角色ID、网络模式、服务器地址和端口）
        PlayerSelectionData.SavePlayerSelection(
            PlayerSelectionData.SelectedCharacterId,
            NetworkCore.NetworkMode.Host,
            "127.0.0.1",
            7777
        );
        Debug.Log($"[NetworkModeSelectionUIController] 已保存完整的玩家选择数据:");
        Debug.Log($"  角色ID: {PlayerSelectionData.SelectedCharacterId}");
        Debug.Log($"  网络模式: {PlayerSelectionData.GetNetworkModeDescription(PlayerSelectionData.SelectedNetworkMode)}");
        Debug.Log($"  服务器地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");

        // 通过EventBus发布网络模式选择事件
        EventBus.Instance.Publish(GameEvents.NETWORK_MODE_SELECTED, NetworkCore.NetworkMode.Host);

        // 如果在Character场景中，自动切换到GameScene
        if (isCharacterScene && autoLoadGameScene)
        {
            StartCoroutine(LoadGameSceneWithDelay(sceneSwitchDelay));
        }
    }

    void OnClientModeButtonClicked()
    {
        UpdateButtonSelectionVisuals(hostModeButton, false);
        UpdateButtonSelectionVisuals(clientModeButton, true);
        Debug.Log("[NetworkModeSelectionUIController] 用户选择客户端模式");

        // 验证角色是否已选择
        if (string.IsNullOrEmpty(PlayerSelectionData.SelectedCharacterId))
        {
            Debug.LogError("[NetworkModeSelectionUIController] 未选择角色，无法启动客户端模式");
            return;
        }

        // 保存完整的玩家选择数据（包括角色ID、网络模式、服务器地址和端口）
        PlayerSelectionData.SavePlayerSelection(
            PlayerSelectionData.SelectedCharacterId,
            NetworkCore.NetworkMode.Client,
            "127.0.0.1",
            7777
        );
        Debug.Log($"[NetworkModeSelectionUIController] 已保存完整的玩家选择数据:");
        Debug.Log($"  角色ID: {PlayerSelectionData.SelectedCharacterId}");
        Debug.Log($"  网络模式: {PlayerSelectionData.GetNetworkModeDescription(PlayerSelectionData.SelectedNetworkMode)}");
        Debug.Log($"  服务器地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");

        // 通过EventBus发布网络模式选择事件
        EventBus.Instance.Publish(GameEvents.NETWORK_MODE_SELECTED, NetworkCore.NetworkMode.Client);

        // 如果在Character场景中，自动切换到GameScene
        if (isCharacterScene && autoLoadGameScene)
        {
            StartCoroutine(LoadGameSceneWithDelay(sceneSwitchDelay));
        }
    }

    void UpdateButtonSelectionVisuals(Button button, bool isSelected)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedColor : normalColor;
        }
    }



    public void ResetSelection()
    {
        UpdateButtonSelectionVisuals(hostModeButton, false);
        UpdateButtonSelectionVisuals(clientModeButton, false);
    }

    public void SetNetworkModeSelectionEnabled(bool enabled)
    {
        if (isEnabled == enabled)
            return;

        isEnabled = enabled;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        if (enabled)
        {
            transitionCoroutine = StartCoroutine(EnableWithAnimation());
            Debug.Log("[NetworkModeSelectionUIController] 网络模式选择已启用");
        }
        else
        {
            transitionCoroutine = StartCoroutine(DisableWithAnimation());
            Debug.Log("[NetworkModeSelectionUIController] 网络模式选择已禁用");
        }
    }

    IEnumerator EnableWithAnimation()
    {
        if (canvasGroup == null)
        {
            isEnabled = true;
            EnableButtons(true);
            yield break;
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = 1f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float curveValue = transitionCurve.Evaluate(t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        EnableButtons(true);
    }

    IEnumerator DisableWithAnimation()
    {
        if (canvasGroup == null)
        {
            isEnabled = false;
            EnableButtons(false);
            yield break;
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = 0.5f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionDuration;
            float curveValue = transitionCurve.Evaluate(t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        EnableButtons(false);
        UpdateButtonDisabledVisuals();
    }

    void EnableButtons(bool enabled)
    {
        if (hostModeButton != null)
        {
            hostModeButton.interactable = enabled;
        }

        if (clientModeButton != null)
        {
            clientModeButton.interactable = enabled;
        }
    }

    void UpdateButtonDisabledVisuals()
    {
        if (hostModeButton != null)
        {
            Image buttonImage = hostModeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = disabledColor;
            }
        }

        if (clientModeButton != null)
        {
            Image buttonImage = clientModeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = disabledColor;
            }
        }
    }

    public bool IsEnabled()
    {
        return isEnabled;
    }

    /// <summary>
    /// 延迟加载GameScene场景
    /// </summary>
    IEnumerator LoadGameSceneWithDelay(float delay)
    {
        Debug.Log($"[NetworkModeSelectionUIController] 将在{delay}秒后加载GameScene场景");
        
        // 验证保存的数据
        if (!PlayerSelectionData.IsDataSaved)
        {
            Debug.LogError("[NetworkModeSelectionUIController] 数据未保存，无法切换场景");
            yield break;
        }
        
        if (!PlayerSelectionData.IsValidData())
        {
            Debug.LogError("[NetworkModeSelectionUIController] 保存的数据无效，无法切换场景");
            Debug.LogError($"[NetworkModeSelectionUIController] 角色ID: {PlayerSelectionData.SelectedCharacterId}");
            Debug.LogError($"[NetworkModeSelectionUIController] 网络模式: {PlayerSelectionData.SelectedNetworkMode}");
            yield break;
        }
        
        Debug.Log($"[NetworkModeSelectionUIController] 数据验证通过:");
        Debug.Log($"  角色ID: {PlayerSelectionData.SelectedCharacterId}");
        Debug.Log($"  网络模式: {PlayerSelectionData.GetNetworkModeDescription(PlayerSelectionData.SelectedNetworkMode)}");
        Debug.Log($"  服务器地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");
        
        yield return new WaitForSeconds(delay);
        LoadGameScene();
    }

    /// <summary>
    /// 加载GameScene场景
    /// </summary>
    void LoadGameScene()
    {
        Debug.Log("[NetworkModeSelectionUIController] 加载GameScene场景");
        SceneManager.LoadScene("GameScene");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[NetworkModeSelectionUIController] 清理单例引用");
        }
    }
}
