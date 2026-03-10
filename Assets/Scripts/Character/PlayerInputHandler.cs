using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerInputHandler : NetworkBehaviour
{
    NetworkPlayer networkPlayer;
    ResourceNode currentNearbyResource;
    List<Animator> animators = new List<Animator>();
    CharacterStats stats;
    SceneAwareAnimatorManager sceneAnimatorManager;
    List<AnimationEventForwarder> animationEventForwarders = new List<AnimationEventForwarder>();
    CharacterMovementController movementController;

    [Header("旋转设置")]
    [Tooltip("鼠标旋转灵敏度")]
    public float mouseRotationSensitivity = 1f;

    private float currentRotationY;

    [Header("动画参数")]
    [Tooltip("速度平滑过渡系数")]
    public float speedBlendSpeed = 5f;

    private float currentSpeed;
    private float targetSpeed;
    
    // 关键调试：记录上一帧的跑步状态
    private bool wasRunningLastFrame = false;

    public void Initialize(NetworkPlayer player)
    {
        networkPlayer = player;
        stats = player.GetComponentInChildren<CharacterStats>();
        movementController = player.GetComponentInChildren<CharacterMovementController>();
        
        currentRotationY = player.transform.eulerAngles.y;
        
        // 创建或获取场景感知动画管理器
        sceneAnimatorManager = GetComponent<SceneAwareAnimatorManager>();
        if (sceneAnimatorManager == null)
        {
            sceneAnimatorManager = gameObject.AddComponent<SceneAwareAnimatorManager>();
        }
        
        // 订阅动画器初始化完成事件
        sceneAnimatorManager.OnAnimatorsInitialized += OnAnimatorsInitialized;
        
        // 初始化场景感知动画管理器
        sceneAnimatorManager.Initialize(player);
    }
    
    /// <summary>
    /// 动画器初始化完成回调
    /// </summary>
    private void OnAnimatorsInitialized(List<Animator> animators)
    {
        this.animators = animators;
        CollectAnimationEventForwarders();
    }
    
    /// <summary>
    /// 收集所有的AnimationEventForwarder组件
    /// </summary>
    private void CollectAnimationEventForwarders()
    {
        animationEventForwarders.Clear();
        AnimationEventForwarder[] forwarders = GetComponentsInChildren<AnimationEventForwarder>();
        animationEventForwarders.AddRange(forwarders);
    }

    public void UpdateAnimatorReference()
    {
        if (sceneAnimatorManager != null)
        {
            sceneAnimatorManager.UpdateAnimatorReference();
        }
    }

    void Update()
    {
        if (networkPlayer == null || !networkPlayer.isLocalPlayer)
            return;

        // 检查是否正在攻击，如果是则阻止移动输入
        WeaponStateManager weaponStateManager = GetComponent<WeaponStateManager>();
        bool isAttacking = weaponStateManager != null && 
                          (weaponStateManager.GetCurrentState() == WeaponState.Attacking ||
                           weaponStateManager.GetCurrentState() == WeaponState.ComboWindow);

        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 input = Vector2.zero;
        
        // 只有在非攻击状态下才响应移动输入
        if (!isAttacking)
        {
            input = new Vector2(h, v);
        }
        
        networkPlayer.CmdSetMovementInput(input);

        // 新方案：计算目标速度（输入强度 0-1 连续值）
        targetSpeed = input.magnitude;
        // 平滑过渡到目标速度（本地预测）
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * speedBlendSpeed);
        // 更新所有Animator的speed参数（本地立即更新）
        UpdateAnimatorSpeed(currentSpeed);

        // 新方案：直接发送speed值到服务器（0-1 连续值）
        // 替代原来的CmdSetRunning(bool)，直接同步连续的speed值
        networkPlayer.CmdSetSpeed(currentSpeed);

        // 处理鼠标旋转输入
        float mouseX = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseX) > 0.01f)
        {
            currentRotationY += mouseX * mouseRotationSensitivity;
            // 同步角色旋转到服务器
            networkPlayer.CmdSetRotation(currentRotationY);
        }

        // 采集资源
        if (Input.GetKeyDown(KeyCode.E) && currentNearbyResource != null)
        {
            NetworkIdentity identity = currentNearbyResource.GetComponent<NetworkIdentity>();
            if (identity != null)
            {
                networkPlayer.CmdTryCollectResource(identity.netId);
            }
        }

        // 攻击输入 - 严格服务器权威架构
        // 客户端只发送请求，服务器执行逻辑
        if (Input.GetMouseButtonDown(0))
        {
            // 检查是否点击在UI上
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 检查是否处于建筑模式
            BuildingUIController buildingUIController = FindObjectOfType<BuildingUIController>();
            if (buildingUIController != null && buildingUIController.IsInBuildingMode())
            {
                return;
            }

            // 直接调用 WeaponStateManager 的 Command 请求攻击
            // 关键修复：使用 GetComponentInChildren 因为 WeaponStateManager 可能在子对象上
            WeaponStateManager wsm = networkPlayer.GetComponentInChildren<WeaponStateManager>(true);
            if (wsm != null)
            {
                Debug.Log("[PlayerInputHandler] 调用 WeaponStateManager.RequestAttack");
                wsm.RequestAttack();
            }
            else
            {
                Debug.LogWarning("[PlayerInputHandler] 未找到 WeaponStateManager 组件");
            }
        }
        
        // 按Q键打开角色选择面板
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("[PlayerInputHandler] Q键被按下");
            
            // 检查是否有UI面板正在显示
            if (InGameCharacterSelectionUI.Instance != null)
            {
                Debug.Log("[PlayerInputHandler] 调用 TogglePanel");
                InGameCharacterSelectionUI.Instance.TogglePanel();
            }
            else
            {
                Debug.LogWarning("[PlayerInputHandler] InGameCharacterSelectionUI 实例为空，无法打开面板");
            }
        }

        // 右键消耗食物恢复血量
        HandleConsumeFoodInput();
    }

    /// <summary>
    /// 处理右键消耗食物输入
    /// 当预览UI选中食物资源时，点击右键消耗一个食物恢复血量
    /// </summary>
    void HandleConsumeFoodInput()
    {
        // 检测鼠标右键点击
        if (!Input.GetMouseButtonDown(1))
            return;

        // 获取InventoryUIController
        InventoryUIController inventoryUI = FindObjectOfType<InventoryUIController>();
        if (inventoryUI == null)
        {
            Debug.LogWarning("[PlayerInputHandler] HandleConsumeFoodInput：未找到InventoryUIController");
            return;
        }

        // 获取当前选中的槽位索引
        int selectedIndex = inventoryUI.currentSelectedIndex;
        
        // 获取玩家的Inventory
        Inventory inventory = networkPlayer.GetInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[PlayerInputHandler] HandleConsumeFoodInput：未找到Inventory组件");
            return;
        }

        // 检查选中槽位是否有物品
        if (selectedIndex < 0 || selectedIndex >= inventory.slots.Count)
        {
            Debug.LogWarning($"[PlayerInputHandler] HandleConsumeFoodInput：无效的槽位索引 {selectedIndex}");
            return;
        }

        var slot = inventory.slots[selectedIndex];
        if (string.IsNullOrEmpty(slot.resourceId) || slot.amount <= 0)
        {
            Debug.Log($"[PlayerInputHandler] HandleConsumeFoodInput：槽位[{selectedIndex}]为空");
            return;
        }

        // 从ResourceDatabase获取资源数据，检查是否为食物资源
        if (ResourceDatabase.Instance == null)
        {
            Debug.LogWarning("[PlayerInputHandler] HandleConsumeFoodInput：ResourceDatabase.Instance为null");
            return;
        }

        ResourceData resourceData = ResourceDatabase.Instance.GetResource(slot.resourceId);
        if (resourceData == null)
        {
            Debug.LogWarning($"[PlayerInputHandler] HandleConsumeFoodInput：未找到资源数据 {slot.resourceId}");
            return;
        }

        // 只有食物资源才响应右键
        if (resourceData.type != "食物资源")
        {
            Debug.Log($"[PlayerInputHandler] HandleConsumeFoodInput：槽位[{selectedIndex}]的物品 {slot.resourceId} 不是食物资源，类型为 {resourceData.type}，不响应右键");
            return;
        }

        // 发送消耗食物命令到服务器
        Debug.Log($"[PlayerInputHandler] 请求消耗食物 - 槽位[{selectedIndex}], 资源: {resourceData.name}");
        inventory.CmdConsumeFood(selectedIndex);
    }

    /// <summary>
    /// 更新所有Animator的speed参数
    /// </summary>
    void UpdateAnimatorSpeed(float speed)
    {
        foreach (var animator in animators)
        {
            if (animator != null)
            {
                animator.SetFloat("speed", speed);
            }
        }
    }

    public void SetNearbyResource(ResourceNode resource)
    {
        currentNearbyResource = resource;
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件，避免内存泄漏
        if (sceneAnimatorManager != null)
        {
            sceneAnimatorManager.OnAnimatorsInitialized -= OnAnimatorsInitialized;
        }
    }
}
