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

    [Header("攻击参数")]
    [Tooltip("攻击范围，单位为米")]
    public float attackRange = 3.5f; // 默认攻击范围
    
    [Tooltip("攻击角度，单位为度")]
    public float attackAngle = 90f; // 默认攻击角度
    
    [Header("攻击周期管理")]
    [Tooltip("当前是否在攻击周期中（网络同步）")]
    [SyncVar]
    private bool isInAttackCycle = false;
    
    [Tooltip("当前攻击周期是否已经触发过伤害（网络同步）")]
    [SyncVar]
    private bool hasTriggeredDamage = false;

    /// <summary>
    /// 当前是否在攻击周期中（只读）
    /// </summary>
    public bool IsInAttackCycle => isInAttackCycle;

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
            // 现在场景动画管理器会通过事件更新动画器列表，这里不需要直接获取

        }
        else
        {

        }
    }

    void Update()
    {
        if (networkPlayer == null || !networkPlayer.isLocalPlayer)
            return;

        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 input = new Vector2(h, v);
        networkPlayer.CmdSetMovementInput(input);

        bool isRunning = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        networkPlayer.CmdSetRunning(isRunning);

        // 处理鼠标旋转输入
        float mouseX = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseX) > 0.01f)
        {
            currentRotationY += mouseX * mouseRotationSensitivity;
            // 同步角色旋转到服务器
            networkPlayer.CmdSetRotation(currentRotationY);
        }

        if (Input.GetKeyDown(KeyCode.E) && currentNearbyResource != null)
        {
            NetworkIdentity identity = currentNearbyResource.GetComponent<NetworkIdentity>();
            if (identity != null)
            {
                networkPlayer.CmdTryCollectResource(identity.netId);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {

                return;
            }
            
            // 检查是否处于建筑模式，如果是则忽略攻击输入
            BuildingUIController buildingUIController = FindObjectOfType<BuildingUIController>();
            if (buildingUIController != null && buildingUIController.IsInBuildingMode())
            {

                return;
            }
            

            networkPlayer.CmdAttack();
        }
    }

    /// <summary>
    /// 攻击命中事件
    /// 由AnimationEventForwarder触发，统一管理攻击周期，确保每次攻击只触发一次伤害
    /// </summary>
    public void OnAttackHit()
    {
        // 检查是否在攻击周期中
        if (!isInAttackCycle)
        {

            return;
        }
        
        // 检查当前攻击周期是否已经触发过伤害
        if (hasTriggeredDamage)
        {

            return;
        }
        
        // 标记已触发伤害
        hasTriggeredDamage = true;
        
        if (networkPlayer != null && networkPlayer.isLocalPlayer)
        {

            networkPlayer.CmdOnAttackHit();
        }
        else
        {

        }
    }

    public void SetNearbyResource(ResourceNode resource)
    {
        currentNearbyResource = resource;
    }
    
    /// <summary>
    /// 初始化攻击周期
    /// 确保每次攻击只触发一次伤害，并在所有客户端上同步显示攻击画线
    /// </summary>
    public void InitializeAttackCycle()
    {
        if (isServer)
        {
            // 服务器端直接设置状态
            isInAttackCycle = true;
            hasTriggeredDamage = false;

        }
        else if (isLocalPlayer)
        {
            // 客户端通过Command请求服务器初始化攻击周期
            CmdInitializeAttackCycle();
        }
    }
    
    /// <summary>
    /// 结束攻击周期
    /// 重置攻击状态，准备下一次攻击，并在所有客户端上同步隐藏攻击画线
    /// </summary>
    public void EndAttackCycle()
    {
        if (isServer)
        {
            // 服务器端直接设置状态
            isInAttackCycle = false;
            hasTriggeredDamage = false;

        }
        else if (isLocalPlayer)
        {
            // 客户端通过Command请求服务器结束攻击周期
            CmdEndAttackCycle();
        }
    }
    
    /// <summary>
    /// 服务器端命令：初始化攻击周期
    /// </summary>
    [Command]
    private void CmdInitializeAttackCycle()
    {
        isInAttackCycle = true;
        hasTriggeredDamage = false;

    }
    
    /// <summary>
    /// 服务器端命令：结束攻击周期
    /// </summary>
    [Command]
    private void CmdEndAttackCycle()
    {
        isInAttackCycle = false;
        hasTriggeredDamage = false;

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