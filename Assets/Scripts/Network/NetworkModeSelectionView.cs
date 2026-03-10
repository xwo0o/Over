using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using NetworkCore;

public class NetworkModeSelectionView : MVCView
{
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

    private Button[] navigationButtons;
    private int currentFocusIndex = 0;
    private bool isEnabled = true;
    private CanvasGroup canvasGroup;
    private Coroutine transitionCoroutine;
    
    void Start()
    {
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
            Debug.Log("[NetworkModeSelectionView] 显示网络模式选择界面");
        }
    }

    public void HideNetworkModeSelection()
    {
        if (networkModePanel != null)
        {
            networkModePanel.SetActive(false);
            Debug.Log("[NetworkModeSelectionView] 隐藏网络模式选择界面");
        }
    }

    public void UpdateButtonSelectionVisuals(Button button, bool isSelected)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void UpdateButtonDisabledVisuals()
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
            Debug.Log("[NetworkModeSelectionView] 网络模式选择已启用");
        }
        else
        {
            transitionCoroutine = StartCoroutine(DisableWithAnimation());
            Debug.Log("[NetworkModeSelectionView] 网络模式选择已禁用");
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

    public bool IsEnabled()
    {
        return isEnabled;
    }

    public override void BindModel(object model)
    {
        // NetworkModeSelectionView不需要绑定特定模型
        // 但可以在这里处理模型数据的绑定
        Debug.Log("[NetworkModeSelectionView] 模型已绑定");
    }

    public override void UpdateView()
    {
        // 根据当前状态更新视图
        if (networkModePanel != null)
        {
            networkModePanel.SetActive(true);
        }
        
        Debug.Log("[NetworkModeSelectionView] 视图已更新");
    }
}