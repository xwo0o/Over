using UnityEngine;

/// <summary>
/// 预览武器控制器 - 根据预览UI的选择在角色手臂上显示对应物品模型
/// </summary>
public class PreviewWeaponController : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("背包UI控制器")]
    public InventoryUIController inventoryUI;
    [Tooltip("武器挂载系统")]
    public WeaponAttachmentSystem weaponAttachment;
    
    [Header("设置")]
    [Tooltip("是否自动查找组件")]
    public bool autoFindComponents = true;
    
    void Start()
    {
        Debug.Log("[PreviewWeaponController] Start方法被调用");
        
        // 自动查找组件
        if (autoFindComponents)
        {
            FindComponents();
        }
        
        // 订阅选择变更事件
        if (inventoryUI != null)
        {
            inventoryUI.OnPreviewSelectionChanged += OnSelectionChanged;
        }
        else
        {
            Debug.LogWarning("[PreviewWeaponController] 未找到InventoryUIController引用");
        }
        
        // 延迟初始化，等待角色模型加载完成
        StartCoroutine(DelayedInitialize());
    }
    
    /// <summary>
    /// 延迟初始化，等待角色模型加载
    /// </summary>
    System.Collections.IEnumerator DelayedInitialize()
    {
        // 等待几帧，确保角色模型已经加载
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[PreviewWeaponController] DelayedInitialize - 开始延迟初始化");
        
        // 延迟初始化完成，等待武器挂载
        Debug.Log("[PreviewWeaponController] DelayedInitialize - 延迟初始化完成");
        
        // 初始化当前选中的武器
        if (inventoryUI != null)
        {
            Debug.Log($"[PreviewWeaponController] DelayedInitialize - 初始化武器显示，当前选中索引: {inventoryUI.currentSelectedIndex}");
            UpdateWeaponDisplay(inventoryUI.currentSelectedIndex);
        }
        else
        {
            Debug.LogWarning("[PreviewWeaponController] DelayedInitialize - inventoryUI为null");
        }
    }
    
    /// <summary>
    /// 自动查找所需组件
    /// </summary>
    void FindComponents()
    {
        Debug.Log("[PreviewWeaponController] FindComponents开始");
        
        // 查找InventoryUIController
        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUIController>();
            Debug.Log($"[PreviewWeaponController] InventoryUIController: {(inventoryUI != null ? "找到" : "未找到")}");
        }
        
        // 查找WeaponAttachmentSystem（可能在同一个物体上或在子物体上）
        if (weaponAttachment == null)
        {
            weaponAttachment = GetComponent<WeaponAttachmentSystem>();
            Debug.Log($"[PreviewWeaponController] GetComponent<WeaponAttachmentSystem>: {(weaponAttachment != null ? "找到" : "未找到")}");
            
            if (weaponAttachment == null)
            {
                weaponAttachment = GetComponentInChildren<WeaponAttachmentSystem>();
                Debug.Log($"[PreviewWeaponController] GetComponentInChildren<WeaponAttachmentSystem>: {(weaponAttachment != null ? "找到" : "未找到")}");
            }
        }
        
        // 如果找到了WeaponAttachmentSystem，记录日志
        if (weaponAttachment != null)
        {
            Debug.Log("[PreviewWeaponController] 已找到WeaponAttachmentSystem");
        }
        else
        {
            Debug.LogError("[PreviewWeaponController] 未能找到WeaponAttachmentSystem！");
        }
    }
    
    /// <summary>
    /// 选择变更回调
    /// </summary>
    /// <param name="slotIndex">选中的槽位索引</param>
    void OnSelectionChanged(int slotIndex)
    {
        // 关键修复：不再直接更新武器显示
        // 武器切换应该由PlayerWeaponController通过服务器权威机制处理
        // 这里只更新UI显示，不实际切换武器
        Debug.Log($"[PreviewWeaponController] OnSelectionChanged: slotIndex={slotIndex}，跳过直接武器切换");
        // UpdateWeaponDisplay(slotIndex); // 禁用直接武器切换
    }
    
    /// <summary>
    /// 更新武器显示
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    void UpdateWeaponDisplay(int slotIndex)
    {
        Debug.Log($"[PreviewWeaponController] UpdateWeaponDisplay开始 - slotIndex={slotIndex}");
        
        if (weaponAttachment == null)
        {
            Debug.LogWarning("[PreviewWeaponController] 未找到WeaponAttachmentSystem引用");
            return;
        }

        if (inventoryUI == null || inventoryUI.targetInventory == null)
        {
            Debug.LogWarning("[PreviewWeaponController] 背包数据未初始化");
            return;
        }

        // 检查索引有效性
        if (slotIndex < 0 || slotIndex >= inventoryUI.targetInventory.slots.Count)
        {
            Debug.LogWarning($"[PreviewWeaponController] 无效的槽位索引: {slotIndex}");
            return;
        }

        // 获取槽位数据
        var slotData = inventoryUI.targetInventory.slots[slotIndex];
        Debug.Log($"[PreviewWeaponController] 槽位 {slotIndex} 数据: resourceId={slotData.resourceId}, amount={slotData.amount}");

        // 空格子或数量为0，卸下武器
        if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
        {
            weaponAttachment.UnequipWeapon();
            Debug.Log($"[PreviewWeaponController] 槽位 {slotIndex} 为空，卸下武器");
            return;
        }

        // 检查数据库实例
        Debug.Log($"[PreviewWeaponController] ResourceDatabase.Instance={(ResourceDatabase.Instance != null ? "有" : "null")}, WeaponDatabase.Instance={(WeaponDatabase.Instance != null ? "有" : "null")}");

        // 优先使用武器数据库配置
        if (WeaponDatabase.Instance != null)
        {
            // 尝试通过资源ID查找对应的武器ID
            int weaponId = FindWeaponIdByResourceId(slotData.resourceId);
            Debug.Log($"[PreviewWeaponController] FindWeaponIdByResourceId({slotData.resourceId})返回: {weaponId}");
            
            if (weaponId > 0)
            {
                Debug.Log($"[PreviewWeaponController] 调用EquipWeapon({weaponId})");
                weaponAttachment.EquipWeapon(weaponId);
                return;
            }
        }

        // 回退到旧方式：直接使用资源的addressableKey
        ResourceData resourceData = ResourceDatabase.Instance?.GetResource(slotData.resourceId);
        if (resourceData == null)
        {
            Debug.LogWarning($"[PreviewWeaponController] 未找到资源数据: {slotData.resourceId}");
            weaponAttachment.UnequipWeapon();
            return;
        }

        if (string.IsNullOrEmpty(resourceData.addressableKey))
        {
            Debug.LogWarning($"[PreviewWeaponController] 资源 {slotData.resourceId} 没有配置addressableKey");
            weaponAttachment.UnequipWeapon();
            return;
        }

        // 挂载武器（旧版接口）
        Debug.Log($"[PreviewWeaponController] 使用旧版接口AttachWeapon({resourceData.addressableKey})");
        weaponAttachment.AttachWeapon(resourceData.addressableKey);
    }

    /// <summary>
    /// 根据资源ID查找对应的武器ID
    /// </summary>
    int FindWeaponIdByResourceId(string resourceId)
    {
        if (WeaponDatabase.Instance == null)
            return -1;

        // 使用WeaponDatabase的GetWeaponByResourceId方法
        WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(resourceId);
        if (weaponData != null)
        {
            return weaponData.weaponId;
        }

        return -1;
    }
    
    /// <summary>
    /// 手动刷新当前武器显示
    /// </summary>
    public void RefreshCurrentWeapon()
    {
        if (inventoryUI != null)
        {
            UpdateWeaponDisplay(inventoryUI.currentSelectedIndex);
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        if (inventoryUI != null)
        {
            inventoryUI.OnPreviewSelectionChanged -= OnSelectionChanged;
        }
    }
}
