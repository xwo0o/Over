using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

public class InventoryUIController : MonoBehaviour
{
    public Inventory targetInventory;
    public GameObject inventoryPanel;
    public GameObject backgroundPanel;
    public GameObject previewPanel;
    public KeyCode toggleKey = KeyCode.Tab;

    private GameObject[] slotPanels;
    private GameObject[] previewSlotPanels;
    bool isVisible;
    private bool hasSubscribedToPlayerEvent = false;
    private bool isToggleBlocked = false; // 是否阻止切换背包
    
    // 添加资源缓存以提高性能
    private Dictionary<string, AsyncOperationHandle<Sprite>> spriteHandles = new Dictionary<string, AsyncOperationHandle<Sprite>>();

    void Awake()
    {
        InitializeSlotPanels();
        InitializePreviewSlotPanels();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }
        
        // 预览UI始终显示
        if (previewPanel != null)
        {
            previewPanel.SetActive(true);
        }
        else
        {
        }
        
        isVisible = false;
    }

    void Start()
    {
        // 订阅NetworkPlayer初始化事件
        if (!hasSubscribedToPlayerEvent)
        {
            NetworkPlayer.OnPlayerInitialized += OnPlayerInitialized;
            hasSubscribedToPlayerEvent = true;
    
        }
    }

    void InitializeSlotPanels()
    {
        if (inventoryPanel == null)
            return;

        List<GameObject> slots = new List<GameObject>();
        
        for (int i = 1; i <= 20; i++)
        {
            Transform panelTransform = inventoryPanel.transform.Find($"Panel ({i})");
            if (panelTransform != null)
            {
                slots.Add(panelTransform.gameObject);
                
                InventorySlotUI slotUI = panelTransform.gameObject.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                {
                    slotUI = panelTransform.gameObject.AddComponent<InventorySlotUI>();
                }
                slotUI.slotIndex = i - 1;
                slotUI.inventoryUIController = this;
            }
        }

        slotPanels = slots.ToArray();
    }

    void InitializePreviewSlotPanels()
    {
        try
        {
            
            if (previewPanel == null)
            {
                return;
            }

            
            for (int i = 0; i < previewPanel.transform.childCount; i++)
            {
                Transform child = previewPanel.transform.GetChild(i);
            }

            List<GameObject> previewSlots = new List<GameObject>();
            
            for (int i = 1; i <= 4; i++)
            {
                Transform panelTransform = previewPanel.transform.Find($"Panel ({i})");
                if (panelTransform != null)
                {
                    previewSlots.Add(panelTransform.gameObject);
                }
                else
                {
                }
            }

            previewSlotPanels = previewSlots.ToArray();
        }
        catch (System.Exception ex)
        {
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && !isToggleBlocked)
        {
            ToggleInventory();
        }
    }

    void OnPlayerInitialized(NetworkPlayer player)
    {
        try
        {
            
            if (player != null && player.isLocalPlayer)
            {
                
                targetInventory = player.GetInventory();
                
                if (targetInventory != null)
                {
                    Refresh();
                    RefreshPreview();
                }
                else
                {
                }
            }
            else
            {
            }
        }
        catch (System.Exception ex)
        {
        }
    }

    public void ToggleInventory()
    {
        isVisible = !isVisible;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isVisible);
        }
        
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(isVisible);
        }

        if (isVisible)
        {
            Refresh();
        }
    }

    /// <summary>
    /// 设置是否阻止背包切换
    /// </summary>
    /// <param name="blocked">是否阻止</param>
    public void SetToggleBlocked(bool blocked)
    {
        isToggleBlocked = blocked;
    }

    /// <summary>
    /// 获取背包当前是否可见
    /// </summary>
    /// <returns>背包是否可见</returns>
    public bool IsVisible()
    {
        return isVisible;
    }

    /// <summary>
    /// 显示背包（不切换状态）
    /// </summary>
    public void ShowInventory()
    {
        isVisible = true;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }

        Refresh();
    }

    private void CleanupSpriteHandles()
    {
        // 释放所有缓存的资源句柄
        foreach (var handle in spriteHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        spriteHandles.Clear();
    }
    
    void OnDestroy()
    {
        CleanupSpriteHandles();
        
        if (hasSubscribedToPlayerEvent)
        {
            NetworkPlayer.OnPlayerInitialized -= OnPlayerInitialized;
            hasSubscribedToPlayerEvent = false;
        }
    }

    public void Refresh()
    {
        if (targetInventory == null)
        {
            return;
        }
        
        if (slotPanels == null || slotPanels.Length == 0)
        {
            return;
        }
        
        if (ResourceDatabase.Instance == null)
        {
            return;
        }
        else
        {
        }

        int maxSlots = Mathf.Min(targetInventory.slots.Length, slotPanels.Length);
        HashSet<string> activeResourceKeys = new HashSet<string>();

        for (int i = 0; i < maxSlots; i++)
        {
            if (slotPanels[i] == null)
                continue;

            var slotData = targetInventory.slots[i];
            Image icon = slotPanels[i].transform.Find("Image")?.GetComponent<Image>();
            TextMeshProUGUI amountText = slotPanels[i].transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();

            if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
            {
                if (icon != null)
                {
                    icon.enabled = true;
                    icon.sprite = null;
                    icon.color = new Color(1, 1, 1, 0);
                }
                if (amountText != null) amountText.text = "";
            }
            else
            {
                
                if (icon != null)
                {
                    icon.enabled = true;
                    ResourceData data = ResourceDatabase.Instance?.GetResource(slotData.resourceId);
                    if (data != null && !string.IsNullOrEmpty(data.spriteAddressableKey))
                    {
                        activeResourceKeys.Add(data.spriteAddressableKey);
                        
                        if (!spriteHandles.ContainsKey(data.spriteAddressableKey))
                        {
                            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(data.spriteAddressableKey);
                            spriteHandles[data.spriteAddressableKey] = handle;
                            
                            handle.Completed += handleResult =>
                            {
                                if (icon != null && handleResult.Status == AsyncOperationStatus.Succeeded)
                                {
                                    icon.sprite = handleResult.Result;
                                    icon.color = Color.white;
                                }
                                else if (icon != null)
                                {
                                }
                            };
                        }
                        else
                        {
                            AsyncOperationHandle<Sprite> handle = spriteHandles[data.spriteAddressableKey];
                            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
                            {
                                icon.sprite = handle.Result;
                                icon.color = Color.white;
                            }
                            else
                            {
                            }
                        }
                    }
                    else
                    {
                    }
                }

                if (amountText != null)
                {
                    amountText.text = slotData.amount.ToString();
                }
            }
        }

        CleanupUnusedSpriteHandles(activeResourceKeys);
    }

    public void RefreshPreview()
    {
        if (targetInventory == null)
        {
            return;
        }
        
        if (previewSlotPanels == null || previewSlotPanels.Length == 0)
        {
            return;
        }
        
        if (ResourceDatabase.Instance == null)
        {
            return;
        }

        int previewSlotsCount = Mathf.Min(4, targetInventory.slots.Length, previewSlotPanels.Length);
        HashSet<string> activeResourceKeys = new HashSet<string>();

        for (int i = 0; i < previewSlotsCount; i++)
        {
            if (previewSlotPanels[i] == null)
            {
                continue;
            }
            
            var slotData = targetInventory.slots[i];
            Image icon = previewSlotPanels[i].transform.Find("Image")?.GetComponent<Image>();
            TextMeshProUGUI amountText = previewSlotPanels[i].transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            
            if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
            {
                if (icon != null)
                {
                    icon.enabled = true;
                    icon.sprite = null;
                    icon.color = new Color(1, 1, 1, 0);
                }
                if (amountText != null) amountText.text = "";
            }
            else
            {
                if (icon != null)
                {
                    icon.enabled = true;
                    icon.color = Color.white;
                    ResourceData data = ResourceDatabase.Instance?.GetResource(slotData.resourceId);
                    if (data != null && !string.IsNullOrEmpty(data.spriteAddressableKey))
                    {
                        activeResourceKeys.Add(data.spriteAddressableKey);
                        
                        if (!spriteHandles.ContainsKey(data.spriteAddressableKey))
                        {
                            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(data.spriteAddressableKey);
                            spriteHandles[data.spriteAddressableKey] = handle;
                            
                            int slotIndex = i;
                            Image capturedIcon = icon;
                            handle.Completed += handleResult =>
                            {
                                if (capturedIcon != null && handleResult.Status == AsyncOperationStatus.Succeeded)
                                {
                                    capturedIcon.sprite = handleResult.Result;
                                    capturedIcon.color = Color.white;
                                }
                                else if (capturedIcon != null)
                                {
                                }
                            };
                        }
                        else
                        {
                            AsyncOperationHandle<Sprite> handle = spriteHandles[data.spriteAddressableKey];
                            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
                            {
                                icon.sprite = handle.Result;
                                icon.color = Color.white;
                            }
                            else if (handle.IsDone && handle.Status != AsyncOperationStatus.Succeeded)
                            {
                            }
                            else
                            {
                                int slotIndex = i;
                                Image capturedIcon = icon;
                                handle.Completed += handleResult =>
                                {
                                    if (capturedIcon != null && handleResult.Status == AsyncOperationStatus.Succeeded)
                                    {
                                        capturedIcon.sprite = handleResult.Result;
                                        capturedIcon.color = Color.white;
                                    }
                                    else if (capturedIcon != null)
                                    {
                                    }
                                };
                            }
                        }
                    }
                    else
                    {
                    }
                }

                if (amountText != null)
                {
                    amountText.text = slotData.amount.ToString();
                }
            }
        }

        CleanupUnusedSpriteHandles(activeResourceKeys);
    }

    private void CleanupUnusedSpriteHandles(HashSet<string> activeKeys)
    {
        List<string> keysToRemove = new List<string>();
        foreach (var kvp in spriteHandles)
        {
            if (!activeKeys.Contains(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            if (spriteHandles.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                spriteHandles.Remove(key);
            }
        }
    }
    
    /// <summary>
    /// 通用显示方法，与ShowInventory功能相同
    /// </summary>
    public void Show()
    {
        ShowInventory();
    }
}
