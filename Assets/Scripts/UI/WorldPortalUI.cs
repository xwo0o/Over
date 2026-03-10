using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using NetworkCore;

public class WorldPortalUI : MonoBehaviour
{
    #region UI元素引用

    [Header("UI面板")]
    public GameObject panel;

    [Header("输入框")]
    public TMPro.TMP_InputField ipInputField;
    public TMPro.TMP_InputField portInputField;

    [Header("按钮")]
    public UnityEngine.UI.Button connectButton;
    public UnityEngine.UI.Button cancelButton;

    [Header("状态显示")]
    public TMPro.TMP_Text statusText;

    #endregion

    #region 私有变量

    private bool isPanelOpen = false;
    private bool isConnecting = false;

    #endregion

    #region 单例模式

    public static WorldPortalUI Instance { get; private set; }

    #endregion

    #region Unity 生命周期

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    void Start()
    {
        SetupButtons();
        SetupInputFields();
        SubscribeToEvents();
        SetDefaultValues();
        UpdateStatus("等待连接...");
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region 初始化方法

    private void SetupButtons()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
            cancelButton.interactable = false;
        }
    }

    private void SetupInputFields()
    {
        if (ipInputField != null)
        {
            ipInputField.onValueChanged.AddListener(OnInputChanged);
        }

        if (portInputField != null)
        {
            portInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            portInputField.characterLimit = 5;
            portInputField.onValueChanged.AddListener(OnInputChanged);
        }
    }

    private void SetDefaultValues()
    {
        if (ipInputField != null && string.IsNullOrEmpty(ipInputField.text))
        {
            ipInputField.text = "127.0.0.1";
        }

        if (portInputField != null && string.IsNullOrEmpty(portInputField.text))
        {
            portInputField.text = "7777";
        }
    }

    private void SubscribeToEvents()
    {
        if (WorldPortalManager.Instance != null)
        {
            WorldPortalManager.Instance.OnWorldSwitchStarted += OnWorldSwitchStarted;
            WorldPortalManager.Instance.OnWorldSwitchCompleted += OnWorldSwitchCompleted;
            WorldPortalManager.Instance.OnWorldSwitchFailed += OnWorldSwitchFailed;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (WorldPortalManager.Instance != null)
        {
            WorldPortalManager.Instance.OnWorldSwitchStarted -= OnWorldSwitchStarted;
            WorldPortalManager.Instance.OnWorldSwitchCompleted -= OnWorldSwitchCompleted;
            WorldPortalManager.Instance.OnWorldSwitchFailed -= OnWorldSwitchFailed;
        }
    }

    #endregion

    #region 面板显示/隐藏

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

    public void OpenPanel()
    {
        if (panel == null) return;

        panel.SetActive(true);
        isPanelOpen = true;
        ResetUIState();
    }

    public void ClosePanel()
    {
        if (panel == null) return;

        panel.SetActive(false);
        isPanelOpen = false;
    }

    #endregion

    #region 按钮事件处理

    private void OnConnectButtonClicked()
    {
        if (!ValidateInputs())
        {
            return;
        }

        string ip = ipInputField.text.Trim();
        int port = int.Parse(portInputField.text);

        if (WorldPortalManager.Instance != null)
        {
            WorldPortalManager.Instance.SwitchWorld(ip, port);
        }
    }

    private void OnCancelButtonClicked()
    {
        if (WorldPortalManager.Instance != null)
        {
            WorldPortalManager.Instance.CancelSwitchWorld();
        }
    }

    #endregion

    #region 输入验证

    private void OnInputChanged(string value)
    {
        ValidateInputs();
    }

    private bool ValidateInputs()
    {
        bool isValid = true;

        if (ipInputField == null || string.IsNullOrEmpty(ipInputField.text.Trim()))
        {
            UpdateStatus("请输入IP地址");
            isValid = false;
        }
        else if (!IsValidIPAddress(ipInputField.text.Trim()))
        {
            UpdateStatus("IP地址格式不正确");
            isValid = false;
        }
        else if (portInputField == null || string.IsNullOrEmpty(portInputField.text))
        {
            UpdateStatus("请输入端口号");
            isValid = false;
        }
        else if (!int.TryParse(portInputField.text, out int port) || port < 1 || port > 65535)
        {
            UpdateStatus("端口号必须在1-65535之间");
            isValid = false;
        }
        else
        {
            UpdateStatus("输入有效");
        }

        if (connectButton != null)
        {
            connectButton.interactable = isValid && !isConnecting;
        }

        return isValid;
    }

    private bool IsValidIPAddress(string ip)
    {
        if (ip == "localhost" || ip == "Localhost" || ip == "LOCALHOST")
        {
            return true;
        }

        string[] parts = ip.Split('.');
        if (parts.Length != 4) return false;

        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int num) || num < 0 || num > 255)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region 状态更新

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ResetUIState()
    {
        isConnecting = false;
        SetInputFieldsInteractable(true);
        SetButtonsInteractable(true, false);
        ValidateInputs();
    }

    private void SetInputFieldsInteractable(bool interactable)
    {
        if (ipInputField != null)
        {
            ipInputField.interactable = interactable;
        }

        if (portInputField != null)
        {
            portInputField.interactable = interactable;
        }
    }

    private void SetButtonsInteractable(bool connectInteractable, bool cancelInteractable)
    {
        if (connectButton != null)
        {
            connectButton.interactable = connectInteractable;
        }

        if (cancelButton != null)
        {
            cancelButton.interactable = cancelInteractable;
        }
    }

    #endregion

    #region WorldPortalManager 事件回调

    private void OnWorldSwitchStarted()
    {
        isConnecting = true;
        UpdateStatus("正在切换世界...");
        SetInputFieldsInteractable(false);
        SetButtonsInteractable(false, true);
    }

    private void OnWorldSwitchCompleted()
    {
        isConnecting = false;
        UpdateStatus("世界切换成功！");
        SetInputFieldsInteractable(true);
        SetButtonsInteractable(true, false);
        ClosePanel();
    }

    private void OnWorldSwitchFailed(string errorMessage)
    {
        isConnecting = false;
        UpdateStatus($"切换失败: {errorMessage}");
        SetInputFieldsInteractable(true);
        SetButtonsInteractable(true, false);
    }

    #endregion
}
