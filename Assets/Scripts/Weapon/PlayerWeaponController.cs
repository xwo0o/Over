using UnityEngine;
using Mirror;

/// <summary>
/// 玩家武器控制器 - 严格服务器权威架构
/// 
/// 设计原则：
/// 1. 客户端只发送输入请求，不执行任何逻辑
/// 2. 服务器验证并执行所有武器操作
/// 3. 使用SyncVar同步状态，所有客户端被动接收更新
/// 4. 每个玩家独立维护自己的武器状态
/// </summary>
public class PlayerWeaponController : NetworkBehaviour
{
    [Header("组件引用")]
    public WeaponAttachmentSystem attachmentSystem;
    public SceneAwareAnimatorManager animatorManager;
    public PreviewWeaponController previewWeaponController;

    [Header("输入设置")]
    public float scrollSensitivity = 0.1f;

    // 服务器权威：只有服务器能修改，自动同步到所有客户端
    [SyncVar(hook = nameof(OnWeaponIdChanged))]
    private int currentWeaponId = -1;
    
    // 关键修复：同步当前槽位索引，确保空格子也能正确显示
    [SyncVar(hook = nameof(OnSlotIndexChanged))]
    private int currentSlotIndex = 0;

    // 客户端预测：本地显示的槽位索引（不与服务器同步）
    private int localSlotIndex = 0;
    private const int MAX_SLOTS = 5;

    void Awake()
    {
        AutoFindComponents();
    }

    void AutoFindComponents()
    {
        if (attachmentSystem == null)
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
        if (animatorManager == null)
            animatorManager = GetComponent<SceneAwareAnimatorManager>();
        if (previewWeaponController == null)
            previewWeaponController = GetComponent<PreviewWeaponController>();
    }

    void Update()
    {
        // 严格检查：只有本地玩家且是拥有此对象的客户端才能处理输入
        if (!isLocalPlayer)
        {
            return;
        }

        // 只有客户端发送输入，不执行任何逻辑
        HandleLocalInput();
    }

    /// <summary>
    /// 处理本地输入 - 客户端只发送请求
    /// 关键修复：预览UI切换是即时的、无限制的，武器切换是后台的
    /// </summary>
    void HandleLocalInput()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) < scrollSensitivity) return;

        int newIndex = localSlotIndex;
        if (scrollDelta > 0)
        {
            newIndex = (localSlotIndex - 1 + MAX_SLOTS) % MAX_SLOTS;
        }
        else if (scrollDelta < 0)
        {
            newIndex = (localSlotIndex + 1) % MAX_SLOTS;
        }

        if (newIndex != localSlotIndex)
        {
            // 关键修复：立即更新本地槽位索引
            localSlotIndex = newIndex;
            
            // 关键修复：立即更新预览UI选中状态（无限制，可以自由切换到任何格子）
            if (previewWeaponController != null && previewWeaponController.inventoryUI != null)
            {
                previewWeaponController.inventoryUI.SetSelectedSlot(newIndex);
            }
            
            // 发送武器切换请求到服务器（如果是武器则切换，不是武器则无效果）
            CmdRequestWeaponSwitch(newIndex);
        }
    }

    /// <summary>
    /// 客户端请求：切换武器
    /// 服务器验证并执行
    /// 关键修复：设置requiresAuthority = true确保只有拥有该对象的客户端才能调用
    /// </summary>
    [Command(requiresAuthority = true)]
    void CmdRequestWeaponSwitch(int slotIndex)
    {
        // 服务器验证槽位合法性
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS)
        {
            return;
        }

        // 服务器获取武器ID
        int weaponId = GetWeaponIdFromSlot(slotIndex);
        
        // 关键修复：验证武器合法性
        if (weaponId > 0 && !IsWeaponValid(weaponId))
        {
            return;
        }
        
        // 关键修复：服务器设置SyncVar，自动同步到所有客户端
        // 由于这是Command，currentWeaponId只会修改当前NetworkIdentity实例的值
        currentWeaponId = weaponId;
        
        // 关键修复：同步槽位索引，确保空格子也能正确显示
        currentSlotIndex = slotIndex;
    }

    /// <summary>
    /// 验证武器是否合法（玩家是否拥有该武器）
    /// 关键修复：直接从Inventory组件获取数据，不依赖previewWeaponController
    /// </summary>
    bool IsWeaponValid(int weaponId)
    {
        // 关键修复：优先直接从当前GameObject的Inventory组件获取
        Inventory inventory = GetComponent<Inventory>();
        
        // 如果直接获取失败，再尝试通过previewWeaponController获取（向后兼容）
        if (inventory == null)
        {
            if (previewWeaponController == null || previewWeaponController.inventoryUI == null)
                return false;

            var inventoryUI = previewWeaponController.inventoryUI;
            if (inventoryUI.targetInventory == null)
                return false;
            
            inventory = inventoryUI.targetInventory;
        }

        // 检查玩家背包中是否有该武器
        for (int i = 0; i < inventory.slots.Count; i++)
        {
            var slotData = inventory.slots[i];
            if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
                continue;

            if (WeaponDatabase.Instance != null)
            {
                WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(slotData.resourceId);
                if (weaponData != null && weaponData.weaponId == weaponId)
                {
                    return true; // 找到合法武器
                }
            }
        }

        return false; // 未找到该武器
    }

    /// <summary>
    /// SyncVar回调：武器ID变化时在所有客户端执行
    /// 只更新视觉表现，不执行逻辑
    /// </summary>
    void OnWeaponIdChanged(int oldId, int newId)
    {
        // 关键修复：如果oldId和newId相同，说明是初始化调用，不应该执行装备逻辑
        if (oldId == newId)
        {
            return;
        }
        
        // 关键修复：检查这个对象是否有效
        if (this == null || gameObject == null)
        {
            return;
        }

        // 关键修复：同步 localSlotIndex 以匹配服务器状态
        // 现在通过OnSlotIndexChanged处理，这里不再重复同步
        // if (isLocalPlayer)
        // {
        //     SyncLocalSlotIndex(newId);
        // }

        // 所有客户端（包括本地和其他玩家）更新武器显示
        if (newId > 0)
        {
            EquipWeaponVisual(newId);
        }
        else
        {
            // 关键修复：当weaponId <= 0时，检查是否有非武器资源需要挂载
            EquipResourceVisual();
        }
        
        // 关键修复：通知WeaponStateManager更新武器数据
        // 确保在客户端上也能获取到最新的武器数据，用于画线范围显示
        WeaponStateManager weaponStateManager = GetComponent<WeaponStateManager>();
        if (weaponStateManager != null)
        {
            weaponStateManager.RefreshWeaponData();
        }
    }
    
    /// <summary>
    /// SyncVar回调：槽位索引变化时在所有客户端执行
    /// 关键修复：确保空格子也能正确同步UI显示
    /// </summary>
    void OnSlotIndexChanged(int oldIndex, int newIndex)
    {
        // 关键修复：同步 localSlotIndex 以匹配服务器状态
        localSlotIndex = newIndex;
        
        // 关键修复：只更新本地玩家的UI，避免影响其他玩家
        // previewWeaponController和inventoryUI是全局单例，所有玩家共享
        if (isLocalPlayer && previewWeaponController != null && previewWeaponController.inventoryUI != null)
        {
            previewWeaponController.inventoryUI.SetSelectedSlot(newIndex);
        }
        
        // 关键修复：如果当前是资源（weaponId <= 0），重新挂载资源
        // 解决SyncVar回调顺序问题：OnSlotIndexChanged可能在OnWeaponIdChanged之后执行
        if (currentWeaponId <= 0)
        {
            EquipResourceVisual();
        }
    }

    /// <summary>
    /// 根据武器ID同步本地槽位索引
    /// 关键修复：同时更新InventoryUIController的选中状态
    /// </summary>
    void SyncLocalSlotIndex(int weaponId)
    {
        if (weaponId <= 0)
        {
            // 关键修复：当weaponId <= 0时（空格子/非武器），保持当前的localSlotIndex
            // 因为HandleLocalInput已经更新了localSlotIndex和UI选中状态
            
            // 确保UI选中状态与localSlotIndex一致（用于其他客户端同步）
            if (previewWeaponController != null && previewWeaponController.inventoryUI != null)
            {
                previewWeaponController.inventoryUI.SetSelectedSlot(localSlotIndex);
            }
            return;
        }

        // 遍历所有槽位，找到匹配的武器ID
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            int slotWeaponId = GetWeaponIdFromSlot(i);
            if (slotWeaponId == weaponId)
            {
                localSlotIndex = i;
                
                // 关键修复：同步更新InventoryUIController的选中状态
                if (previewWeaponController != null && previewWeaponController.inventoryUI != null)
                {
                    previewWeaponController.inventoryUI.SetSelectedSlot(i);
                }
                
                return;
            }
        }
    }

    /// <summary>
    /// 装备武器视觉表现 - 所有客户端执行
    /// 关键修复：确保只更新当前角色的武器显示
    /// </summary>
    void EquipWeaponVisual(int weaponId)
    {
        // 关键修复：确保attachmentSystem是当前GameObject上的组件
        if (attachmentSystem == null)
        {
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
        }
        
        // 关键修复：验证attachmentSystem是否属于当前GameObject
        if (attachmentSystem != null && attachmentSystem.gameObject != gameObject)
        {
            // 重新获取正确的attachmentSystem
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
        }
        
        // 检查attachmentSystem是否有效
        if (attachmentSystem == null)
        {
            return;
        }

        attachmentSystem.EquipWeapon(weaponId);
        UpdateAnimatorWeaponType(weaponId);
    }

    /// <summary>
    /// 卸下武器视觉表现 - 所有客户端执行
    /// </summary>
    void UnequipWeaponVisual()
    {
        // 关键修复：确保attachmentSystem是当前GameObject上的组件
        if (attachmentSystem == null)
        {
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
        }
        
        if (attachmentSystem != null)
        {
            // 关键修复：验证attachmentSystem是否属于当前GameObject
            if (attachmentSystem.gameObject != gameObject)
            {
                attachmentSystem = GetComponent<WeaponAttachmentSystem>();
            }
            
            attachmentSystem.UnequipWeapon();
        }

        UpdateAnimatorWeaponType(0);
    }

    /// <summary>
    /// 装备资源视觉表现（非武器资源）- 所有客户端执行
    /// 关键修复：支持非武器资源（木材、石材、食物等）在右手显示模型
    /// </summary>
    void EquipResourceVisual()
    {
        // 确保attachmentSystem是当前GameObject上的组件
        if (attachmentSystem == null)
        {
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
        }
        
        if (attachmentSystem == null)
        {
            return;
        }

        // 验证attachmentSystem是否属于当前GameObject
        if (attachmentSystem.gameObject != gameObject)
        {
            attachmentSystem = GetComponent<WeaponAttachmentSystem>();
        }

        // 关键修复：使用currentSlotIndex（SyncVar）替代localSlotIndex（本地变量）
        // 确保使用服务器同步的槽位索引，避免SyncVar回调顺序问题
        Inventory inventory = GetComponent<Inventory>();
        if (inventory == null)
        {
            attachmentSystem.UnequipWeapon();
            return;
        }

        if (currentSlotIndex < 0 || currentSlotIndex >= inventory.slots.Count)
        {
            attachmentSystem.UnequipWeapon();
            return;
        }

        var slotData = inventory.slots[currentSlotIndex];
        
        // 空格子，卸下武器/资源
        if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
        {
            attachmentSystem.UnequipWeapon();
            UpdateAnimatorWeaponType(0);
            return;
        }

        // 检查是否是武器（武器会通过WeaponDatabase找到）
        if (WeaponDatabase.Instance != null)
        {
            WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(slotData.resourceId);
            if (weaponData != null)
            {
                // 是武器，但weaponId <= 0的情况不应该发生
                attachmentSystem.UnequipWeapon();
                UpdateAnimatorWeaponType(0);
                return;
            }
        }

        // 非武器资源，从ResourceDatabase获取模型地址
        if (ResourceDatabase.Instance != null)
        {
            ResourceData resourceData = ResourceDatabase.Instance.GetResource(slotData.resourceId);
            if (resourceData != null && !string.IsNullOrEmpty(resourceData.addressableKey))
            {
                // 使用AttachWeapon挂载资源模型到右手骨骼Prop_R
                attachmentSystem.AttachWeapon(resourceData.addressableKey, "Prop_R");
                
                // 非武器资源不更新动画参数（保持默认状态）
                UpdateAnimatorWeaponType(0);
                return;
            }
        }

        // 无法挂载资源，卸下当前模型
        attachmentSystem.UnequipWeapon();
        UpdateAnimatorWeaponType(0);
    }

    /// <summary>
    /// 更新动画参数 - 所有客户端执行
    /// </summary>
    void UpdateAnimatorWeaponType(int weaponId)
    {
        if (animatorManager == null) return;

        var animators = animatorManager.GetCurrentAnimators();
        foreach (var animator in animators)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat("WeaponType", weaponId);
                animator.SetInteger("WeaponType_int", weaponId);
            }
        }
    }

    /// <summary>
    /// 获取当前武器ID（供其他系统查询）
    /// </summary>
    public int GetCurrentWeaponId()
    {
        return currentWeaponId;
    }

    /// <summary>
    /// 根据槽位获取武器ID - 服务器执行
    /// 关键修复：直接从Inventory组件获取数据，不依赖previewWeaponController
    /// </summary>
    int GetWeaponIdFromSlot(int slotIndex)
    {
        // 关键修复：优先直接从当前GameObject的Inventory组件获取
        // 这样可以确保服务器获取到正确的玩家背包数据
        Inventory inventory = GetComponent<Inventory>();
        
        // 如果直接获取失败，再尝试通过previewWeaponController获取（向后兼容）
        if (inventory == null)
        {
            if (previewWeaponController == null || previewWeaponController.inventoryUI == null)
                return -1;

            var inventoryUI = previewWeaponController.inventoryUI;
            if (inventoryUI.targetInventory == null)
                return -1;
            
            inventory = inventoryUI.targetInventory;
        }

        if (slotIndex < 0 || slotIndex >= inventory.slots.Count)
            return -1;

        var slotData = inventory.slots[slotIndex];
        if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
            return -1;

        if (WeaponDatabase.Instance != null)
        {
            WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(slotData.resourceId);
            if (weaponData != null)
            {
                return weaponData.weaponId;
            }
        }

        return -1;
    }
}
