using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Mirror;

public class InventoryUIController : MonoBehaviour
{
    public Inventory targetInventory;
    public GameObject inventoryPanel;
    public GameObject previewPanel;
    public KeyCode toggleKey = KeyCode.Tab;

    private GameObject[] slotPanels;
    private GameObject[] previewSlotPanels;
    bool isVisible;
    private bool hasSubscribedToPlayerEvent = false;
    private bool isToggleBlocked = false; // 是否阻止切换背包
    
    // 添加资源缓存以提高性能
    private Dictionary<string, AsyncOperationHandle<Sprite>> spriteHandles = new Dictionary<string, AsyncOperationHandle<Sprite>>();
    
    [Header("滚轮选择设置")]
    [Tooltip("当前选中的预览槽索引 (0-4)")]
    public int currentSelectedIndex = 0;
    [Tooltip("选中状态的颜色")]
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);
    [Tooltip("正常状态的颜色")]
    public Color normalColor = Color.white;
    [Tooltip("选中状态的缩放")]
    public float selectedScale = 1.15f;
    [Tooltip("正常状态的缩放")]
    public float normalScale = 1f;
    [Tooltip("选择切换动画持续时间")]
    public float transitionDuration = 0.1f;
    
    // 选择变更事件
    public System.Action<int> OnPreviewSelectionChanged;

    void Awake()
    {
        InitializeSlotPanels();
        InitializePreviewSlotPanels();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
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
            
            for (int i = 1; i <= 5; i++)
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
            
            // 初始化高亮显示
            UpdateSlotHighlight();
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
        
        // 关键修复：禁用滚轮选择输入，由PlayerWeaponController处理
        // HandleScrollInput(); // 禁用，避免与PlayerWeaponController冲突
    }
    
    /// <summary>
    /// 处理鼠标滚轮输入进行物品选择
    /// </summary>
    void HandleScrollInput()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) < 0.001f) return;
        
        int newIndex = currentSelectedIndex;
        
        // 向上滚动 - 选择上一个
        if (scrollDelta > 0)
        {
            newIndex = (currentSelectedIndex - 1 + 5) % 5;
        }
        // 向下滚动 - 选择下一个
        else if (scrollDelta < 0)
        {
            newIndex = (currentSelectedIndex + 1) % 5;
        }
        
        // 索引变化时更新
        if (newIndex != currentSelectedIndex)
        {
            SetSelectedSlot(newIndex);
        }
    }
    
    /// <summary>
    /// 设置当前选中的槽位
    /// </summary>
    /// <param name="index">槽位索引 (0-4)</param>
    public void SetSelectedSlot(int index)
    {
        if (index < 0 || index >= 5) return;
        if (previewSlotPanels == null || previewSlotPanels.Length == 0) return;
        
        currentSelectedIndex = index;
        
        // 更新UI高亮显示
        UpdateSlotHighlight();
        
        // 触发选择变更事件
        OnPreviewSelectionChanged?.Invoke(index);
        
        Debug.Log($"[InventoryUIController] 选中槽位: {index}");
    }
    
    /// <summary>
    /// 更新预览槽的高亮显示
    /// </summary>
    void UpdateSlotHighlight()
    {
        if (previewSlotPanels == null) return;
        
        for (int i = 0; i < previewSlotPanels.Length && i < 5; i++)
        {
            if (previewSlotPanels[i] == null) continue;
            
            Transform panelTransform = previewSlotPanels[i].transform;
            Image bgImage = panelTransform.GetComponent<Image>();
            
            bool isSelected = (i == currentSelectedIndex);
            
            // 设置颜色
            if (bgImage != null)
            {
                bgImage.color = isSelected ? selectedColor : normalColor;
            }
            
            // 设置缩放 (使用LeanTween或DOTween可以实现平滑动画，这里用简单的方式)
            StopCoroutine("AnimateSlotScale");
            StartCoroutine(AnimateSlotScale(panelTransform, isSelected ? selectedScale : normalScale));
        }
    }
    
    /// <summary>
    /// 槽位缩放动画协程
    /// </summary>
    System.Collections.IEnumerator AnimateSlotScale(Transform target, float targetScale)
    {
        if (target == null) yield break;
        
        Vector3 startScale = target.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            // 使用平滑插值
            t = t * t * (3f - 2f * t);
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        
        if (target != null)
        {
            target.localScale = endScale;
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
                    // 订阅背包数据变化事件（SyncList网络同步回调）
                    targetInventory.slots.Callback += OnInventorySlotsChanged;
                    
                    // 订阅本地数据变化事件（解决主机模式下SyncList.Callback不触发的问题）
                    targetInventory.OnDataChanged += OnLocalDataChanged;
                    
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
    
    /// <summary>
    /// 本地数据变化时的回调（主机模式下使用）
    /// </summary>
    void OnLocalDataChanged()
    {
        Refresh();
        RefreshPreview();
        Debug.Log($"[InventoryUIController] 本地背包数据变化，刷新UI");
    }
    
    /// <summary>
    /// 背包数据变化时的回调
    /// </summary>
    void OnInventorySlotsChanged(SyncList<InventoryItem>.Operation op, int index, InventoryItem oldItem, InventoryItem newItem)
    {
        // 当背包数据变化时，刷新UI
        Refresh();
        RefreshPreview();
        Debug.Log($"[InventoryUIController] 背包数据变化 - 操作: {op}, 索引: {index}");
    }

    public void ToggleInventory()
    {
        isVisible = !isVisible;
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isVisible);
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
        
        // 取消订阅背包数据变化事件
        if (targetInventory != null)
        {
            targetInventory.slots.Callback -= OnInventorySlotsChanged;
            targetInventory.OnDataChanged -= OnLocalDataChanged;
        }
        
        if (hasSubscribedToPlayerEvent)
        {
            NetworkPlayer.OnPlayerInitialized -= OnPlayerInitialized;
            hasSubscribedToPlayerEvent = false;
        }
    }

    public void Refresh()
    {
        Debug.Log($"[InventoryUIController] Refresh被调用 - targetInventory: {(targetInventory != null ? "有" : "无")}, slotPanels: {(slotPanels != null ? slotPanels.Length : 0)}");
        
        if (targetInventory == null)
        {
            Debug.LogWarning("[InventoryUIController] Refresh: targetInventory为null");
            return;
        }
        
        if (slotPanels == null || slotPanels.Length == 0)
        {
            Debug.LogWarning("[InventoryUIController] Refresh: slotPanels为空");
            return;
        }
        
        if (ResourceDatabase.Instance == null)
        {
            Debug.LogWarning("[InventoryUIController] Refresh: ResourceDatabase.Instance为null");
            return;
        }
        
        if (WeaponDatabase.Instance == null)
        {
            Debug.LogWarning("[InventoryUIController] Refresh: WeaponDatabase.Instance为null");
        }
        else
        {
            Debug.Log($"[InventoryUIController] Refresh: WeaponDatabase已加载 {WeaponDatabase.Instance.GetAllWeapons().Count} 个武器");
        }

        int maxSlots = Mathf.Min(targetInventory.slots.Count, slotPanels.Length);
        Debug.Log($"[InventoryUIController] Refresh: 处理 {maxSlots} 个格子");
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
                Debug.Log($"[InventoryUIController] 处理格子 {i}: resourceId={slotData.resourceId}, amount={slotData.amount}");
                
                if (icon != null)
                {
                    icon.enabled = true;
                    string spriteKey = GetSpriteAddressableKey(slotData.resourceId);
                    Debug.Log($"[InventoryUIController] 格子 {i}: spriteKey={spriteKey}");
                    
                    if (!string.IsNullOrEmpty(spriteKey))
                    {
                        activeResourceKeys.Add(spriteKey);
                        
                        if (!spriteHandles.ContainsKey(spriteKey))
                        {
                            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);
                            spriteHandles[spriteKey] = handle;
                            
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
                            AsyncOperationHandle<Sprite> handle = spriteHandles[spriteKey];
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

        int previewSlotsCount = Mathf.Min(5, targetInventory.slots.Count, previewSlotPanels.Length);
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
                    string spriteKey = GetSpriteAddressableKey(slotData.resourceId);
                    
                    if (!string.IsNullOrEmpty(spriteKey))
                    {
                        activeResourceKeys.Add(spriteKey);
                        
                        if (!spriteHandles.ContainsKey(spriteKey))
                        {
                            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteKey);
                            spriteHandles[spriteKey] = handle;
                            
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
                            AsyncOperationHandle<Sprite> handle = spriteHandles[spriteKey];
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

    /// <summary>
    /// 获取资源的Sprite地址
    /// 优先从WeaponDatabase查找武器数据，如果没有则从ResourceDatabase查找
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>Sprite的AddressableKey，如果没有找到则返回null</returns>
    string GetSpriteAddressableKey(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId))
        {
            Debug.LogWarning("[InventoryUIController] GetSpriteAddressableKey: resourceId为空");
            return null;
        }

        // 首先尝试从WeaponDatabase获取武器数据
        if (WeaponDatabase.Instance != null)
        {
            WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(resourceId);
            if (weaponData != null)
            {
                Debug.Log($"[InventoryUIController] 从WeaponDatabase找到武器: {resourceId} -> {weaponData.weaponName}, sprite: {weaponData.spriteAddressableKey}");
                if (!string.IsNullOrEmpty(weaponData.spriteAddressableKey))
                {
                    return weaponData.spriteAddressableKey;
                }
            }
            else
            {
                Debug.LogWarning($"[InventoryUIController] WeaponDatabase中未找到武器: {resourceId}");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryUIController] WeaponDatabase.Instance为null");
        }

        // 如果没有找到武器数据，从ResourceDatabase获取
        if (ResourceDatabase.Instance != null)
        {
            ResourceData resourceData = ResourceDatabase.Instance.GetResource(resourceId);
            if (resourceData != null)
            {
                Debug.Log($"[InventoryUIController] 从ResourceDatabase找到资源: {resourceId} -> {resourceData.name}, sprite: {resourceData.spriteAddressableKey}");
                if (!string.IsNullOrEmpty(resourceData.spriteAddressableKey))
                {
                    return resourceData.spriteAddressableKey;
                }
            }
            else
            {
                Debug.LogWarning($"[InventoryUIController] ResourceDatabase中未找到资源: {resourceId}");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryUIController] ResourceDatabase.Instance为null");
        }

        return null;
    }
}
