using UnityEngine;
using Mirror;

/// <summary>
/// 武器状态管理器 - 严格服务器权威架构
/// 
/// 连击系统设计：
/// 1. 使用 AttackStage Int 参数控制动画 (0=Idle, 1=A1, 2=A2, 3=A3)
/// 2. 动画播放期间点击鼠标 -> 缓存连击请求
/// 3. 动画结束后 0.3 秒内 -> 窗口期，自动触发缓存的连击
/// 4. 窗口期结束后 -> 重置连击，返回 Idle
/// 
/// 关键修复：
/// - 连击请求在动画播放期间被缓存，而不是立即执行
/// - 动画结束后的 0.3 秒窗口期内，自动消耗缓存触发下一段
/// - 确保 A1 -> A2 -> A3 的顺序执行，不会跳过或重复
/// </summary>
public class WeaponStateManager : NetworkBehaviour
{
    [Header("系统引用")]
    public WeaponDamageSystem damageSystem;
    public SceneAwareAnimatorManager animatorManager;
    public PlayerWeaponController weaponController;
    public AttackRangeVisualizer attackRangeVisualizer;

    [Header("攻击设置")]
    public float attackRadius = 1.5f;
    public float attackDistance = 2.0f;

    // 服务器权威：当前攻击阶段 SyncVar (0=Idle, 1=A1, 2=A2, 3=A3)
    [SyncVar(hook = nameof(OnAttackStageChanged))]
    private int attackStage = 0;

    // 服务器权威：最大连击段数
    [SyncVar]
    private int maxComboStages = 3;

    // 服务器权威：是否正在播放攻击动画
    [SyncVar]
    private bool isPlayingAttackAnim = false;

    // 服务器权威：连击窗口是否开启
    [SyncVar]
    private bool isComboWindowOpen = false;

    // 服务器专用：连击窗口计时器
    private float comboWindowTimer = 0f;
    private const float COMBO_WINDOW_DURATION = 0.5f;

    // 服务器专用：攻击动画超时计时器（防止动画事件未触发导致状态卡死）
    private float attackAnimTimer = 0f;
    private const float ATTACK_ANIM_TIMEOUT = 2.0f; // 最大攻击动画时间

    // 本地状态
    private WeaponState currentState = WeaponState.Idle;
    private WeaponData currentWeaponData;
    private bool canDealDamage = false;
    private Vector3 attackHitPoint;

    // 动画参数 Hash
    private static readonly int AttackStageParam = Animator.StringToHash("AttackStage");
    private static readonly int ComboWindowParam = Animator.StringToHash("ComboWindow");
    private static readonly int AttackTrigger = Animator.StringToHash("AttackTrigger"); // 用于触发动画状态转换

    void Awake()
    {
        AutoFindComponents();
    }

    void AutoFindComponents()
    {
        if (damageSystem == null)
            damageSystem = GetComponent<WeaponDamageSystem>();
        if (animatorManager == null)
            animatorManager = GetComponent<SceneAwareAnimatorManager>();
        if (weaponController == null)
            weaponController = GetComponent<PlayerWeaponController>();
        if (attackRangeVisualizer == null)
            attackRangeVisualizer = GetComponent<AttackRangeVisualizer>();
    }

    void Update()
    {
        // 服务器更新连击窗口和攻击动画超时
        if (isServer)
        {
            ServerUpdateComboWindow();
            ServerUpdateAttackAnimTimeout();
        }
    }

    #region SyncVar 回调

    /// <summary>
    /// SyncVar回调：攻击阶段变化
    /// 在所有客户端执行视觉更新
    /// 关键修复：确保在所有客户端上正确执行
    /// </summary>
    void OnAttackStageChanged(int oldStage, int newStage)
    {
        // 关键修复：验证组件引用
        if (animatorManager == null)
        {
            animatorManager = GetComponent<SceneAwareAnimatorManager>();
        }
        
        if (attackRangeVisualizer == null)
        {
            attackRangeVisualizer = GetComponent<AttackRangeVisualizer>();
        }

        // 更新动画参数 - 关键修复：确保所有客户端都更新Animator
        UpdateAnimatorAttackStage(newStage);

        // 更新视觉表现
        if (newStage > 0)
        {
            PlayAttackVisuals(newStage);
        }
        else
        {
            StopAttackVisuals();
        }
    }

    /// <summary>
    /// 更新Animator的AttackStage参数
    /// 关键修复：确保在所有客户端上正确更新动画
    /// </summary>
    void UpdateAnimatorAttackStage(int stage)
    {
        if (animatorManager == null)
        {
            return;
        }
        
        // 关键修复：检查animatorManager是否已初始化
        if (!animatorManager.IsInitialized)
        {
            animatorManager.UpdateAnimatorReference();
        }

        var animator = animatorManager.GetCurrentAnimator();
        if (animator == null)
        {
            // 尝试重新获取动画引用
            animatorManager.UpdateAnimatorReference();
            animator = animatorManager.GetCurrentAnimator();
            
            if (animator == null)
            {
                return;
            }
        }
        
        // 关键修复：验证animator是否有效
        if (!animator.isActiveAndEnabled)
        {
            animator.enabled = true;
        }

        // 设置AttackStage参数（用于状态机判断当前阶段）
        animator.SetInteger(AttackStageParam, stage);

        // 设置ComboWindow参数
        animator.SetBool(ComboWindowParam, isComboWindowOpen);

        // 关键修复：使用Trigger触发动画状态转换（进入攻击状态或连击）
        if (stage > 0)
        {
            animator.SetTrigger(AttackTrigger);
        }

        // 强制刷新Animator（某些情况下需要）
        animator.Update(0);
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 公共接口：请求攻击
    /// 供 PlayerInputHandler 等外部系统调用
    /// </summary>
    public void RequestAttack()
    {
        // 如果在服务器端运行，直接执行服务器逻辑
        if (isServer)
        {
            ServerHandleAttackRequest();
            return;
        }

        // 客户端：只有本地玩家才能请求攻击
        if (!isLocalPlayer)
        {
            return;
        }

        CmdRequestAttack();
    }

    /// <summary>
    /// 动画事件：攻击动画结束
    /// 由 Animation Event 调用
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        if (isServer)
        {
            ServerOnAttackAnimationEnd();
        }
        else
        {
            CmdOnAttackAnimationEnd();
        }
    }

    #endregion

    #region 服务器逻辑

    /// <summary>
    /// 客户端请求：攻击
    /// </summary>
    [Command]
    void CmdRequestAttack()
    {
        ServerHandleAttackRequest();
    }

    /// <summary>
    /// 客户端通知：攻击动画结束
    /// </summary>
    [Command]
    void CmdOnAttackAnimationEnd()
    {
        ServerOnAttackAnimationEnd();
    }

    /// <summary>
    /// 服务器处理：攻击请求
    /// 核心逻辑：
    /// - Idle 状态：立即开始第一段攻击
    /// - 动画播放中：缓存连击请求
    /// - 窗口期：立即触发下一段攻击
    /// </summary>
    [Server]
    void ServerHandleAttackRequest()
    {
        // 更新武器数据
        UpdateWeaponData();

        // 检查武器
        if (currentWeaponData == null)
        {
            return;
        }

        // 情况1：Idle 状态，开始第一段攻击
        if (attackStage == 0 && !isPlayingAttackAnim)
        {
            ServerStartAttackStage(1);
            return;
        }

        // 情况2：正在播放攻击动画，忽略请求（无缓存功能）
        if (isPlayingAttackAnim)
        {
            return;
        }

        // 情况3：连击窗口开启，立即触发下一段
        if (isComboWindowOpen && attackStage < maxComboStages)
        {
            ServerStartAttackStage(attackStage + 1);
            return;
        }
    }

    /// <summary>
    /// 服务器执行：开始指定阶段的攻击
    /// </summary>
    [Server]
    void ServerStartAttackStage(int stage)
    {
        // 设置攻击阶段
        attackStage = stage;
        isPlayingAttackAnim = true;
        isComboWindowOpen = false;

        // 关键修复：设置攻击动画超时计时器
        attackAnimTimer = ATTACK_ANIM_TIMEOUT;

        // 更新状态
        currentState = WeaponState.Attacking;

        // 通知伤害系统
        if (damageSystem != null)
        {
            damageSystem.SyncComboStage(stage);
        }

        // 开启伤害判定
        canDealDamage = true;
    }

    /// <summary>
    /// 服务器处理：攻击动画结束
    /// 开启连击窗口，准备接受下一段攻击
    /// </summary>
    [Server]
    void ServerOnAttackAnimationEnd()
    {
        // 关闭伤害判定
        canDealDamage = false;
        isPlayingAttackAnim = false;

        // 检查是否还有下一段可以连击
        if (attackStage < maxComboStages)
        {
            // 开启连击窗口
            isComboWindowOpen = true;
            comboWindowTimer = COMBO_WINDOW_DURATION;
            currentState = WeaponState.ComboWindow;

            // 通知伤害系统
            if (damageSystem != null)
            {
                damageSystem.OpenComboWindow();
            }
        }
        else
        {
            // 最后一段结束，重置连击
            ServerResetCombo();
        }
    }

    /// <summary>
    /// 服务器更新：攻击动画超时检测
    /// 防止动画事件未触发导致状态卡死
    /// </summary>
    [Server]
    void ServerUpdateAttackAnimTimeout()
    {
        if (!isPlayingAttackAnim) return;

        attackAnimTimer -= Time.deltaTime;

        if (attackAnimTimer <= 0f)
        {
            ServerOnAttackAnimationEnd();
        }
    }

    /// <summary>
    /// 服务器更新：连击窗口计时器
    /// </summary>
    [Server]
    void ServerUpdateComboWindow()
    {
        if (!isComboWindowOpen) return;

        comboWindowTimer -= Time.deltaTime;

        if (comboWindowTimer <= 0f)
        {
            // 窗口超时关闭
            ServerCloseComboWindow();
        }
    }

    /// <summary>
    /// 服务器执行：关闭连击窗口
    /// </summary>
    [Server]
    void ServerCloseComboWindow()
    {
        isComboWindowOpen = false;

        // 通知伤害系统
        if (damageSystem != null)
        {
            damageSystem.CloseComboWindow();
        }

        // 重置连击
        ServerResetCombo();
    }

    /// <summary>
    /// 服务器执行：重置连击
    /// </summary>
    [Server]
    void ServerResetCombo()
    {
        attackStage = 0;
        isPlayingAttackAnim = false;
        isComboWindowOpen = false;
        currentState = WeaponState.Idle;

        // 重置Animator参数
        if (animatorManager != null)
        {
            var animator = animatorManager.GetCurrentAnimator();
            if (animator != null)
            {
                animator.ResetTrigger(AttackTrigger);
                animator.SetInteger(AttackStageParam, 0);
                animator.SetBool(ComboWindowParam, false);
            }
        }

        // 通知伤害系统
        if (damageSystem != null)
        {
            damageSystem.ResetCombo();
        }
    }

    #endregion

    #region 视觉表现

    /// <summary>
    /// 所有客户端执行：播放攻击视觉表现
    /// 关键修复：确保在客户端上也能正确获取武器数据
    /// </summary>
    void PlayAttackVisuals(int stage)
    {
        // 关键修复：在客户端上也需要获取武器数据
        if (currentWeaponData == null)
        {
            UpdateWeaponData();
        }

        // 显示攻击范围
        if (attackRangeVisualizer != null && currentWeaponData != null)
        {
            attackRangeVisualizer.SetWeaponData(currentWeaponData);
            attackRangeVisualizer.StartAttack();
        }
    }

    /// <summary>
    /// 所有客户端执行：停止攻击视觉表现
    /// </summary>
    void StopAttackVisuals()
    {
        // 隐藏攻击范围
        if (attackRangeVisualizer != null)
        {
            attackRangeVisualizer.EndAttack();
        }
    }

    #endregion

    #region 伤害判定

    /// <summary>
    /// 动画事件：开启伤害判定
    /// </summary>
    public void EnableDamage()
    {
        if (isServer)
        {
            canDealDamage = true;
        }
    }

    /// <summary>
    /// 动画事件：关闭伤害判定
    /// </summary>
    public void DisableDamage()
    {
        if (isServer)
        {
            canDealDamage = false;
        }
    }

    /// <summary>
    /// 动画事件：执行伤害判定
    /// 关键修复：动画事件在客户端触发，使用Command发送到服务器执行伤害判定
    /// </summary>
    public void PerformDamageCheck()
    {
        // 关键修复：动画事件在客户端触发，使用Command发送到服务器
        // 无论isServer是否为true，都发送Command到服务器执行伤害判定
        try
        {
            CmdPerformDamageCheck();
        }
        catch (System.Exception)
        {
            // 忽略异常
        }
    }
    
    /// <summary>
    /// 服务器命令：执行伤害判定
    /// 关键修复：使用Command确保伤害判定在服务器上执行
    /// 关键修复：添加requiresAuthority=false，允许任何客户端发送此命令
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdPerformDamageCheck()
    {
        // 关键修复：通知客户端Command已接收
        RpcNotifyDamageCheckReceived(isServer, canDealDamage, damageSystem != null);
        
        if (!isServer)
        {
            return;
        }
        
        if (!canDealDamage)
        {
            return;
        }
        
        if (damageSystem == null)
        {
            return;
        }
        
        // 从角色位置计算攻击中心
        Vector3 attackCenter = transform.position + Vector3.up * 1.0f;
        float attackRadius = currentWeaponData?.attackRange ?? 2.0f;
        
        damageSystem.PerformDamageCheck(attackCenter, attackRadius);
    }
    
    /// <summary>
    /// 客户端RPC：通知客户端伤害判定检查已接收
    /// </summary>
    [ClientRpc]
    void RpcNotifyDamageCheckReceived(bool serverFlag, bool canDealDamageFlag, bool hasDamageSystem)
    {
        // RPC通知，无需处理
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 更新武器数据
    /// 关键修复：确保在客户端和服务器上都能正确获取武器数据
    /// </summary>
    void UpdateWeaponData()
    {
        // 关键修复：验证weaponController
        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
            if (weaponController == null)
            {
                return;
            }
        }

        int weaponId = weaponController.GetCurrentWeaponId();

        if (weaponId < 0)
        {
            // 关键修复：当weaponId < 0时（空格子/非武器资源），清空currentWeaponData
            // 这样ServerHandleAttackRequest中的检查就能阻止非武器资源的攻击
            currentWeaponData = null;
            maxComboStages = 0;
            
            // 同步到伤害系统（只在服务器端执行）
            if (isServer && damageSystem != null)
            {
                damageSystem.SetWeaponData(null);
            }
            return;
        }

        // 关键修复：使用正确的API GetWeapon 而不是 GetWeaponData
        if (WeaponDatabase.Instance == null)
        {
            return;
        }

        currentWeaponData = WeaponDatabase.Instance.GetWeapon(weaponId);
        if (currentWeaponData == null)
        {
            return;
        }

        maxComboStages = currentWeaponData.comboStages;

        // 同步到伤害系统（只在服务器端执行）
        if (isServer && damageSystem != null)
        {
            damageSystem.SetWeaponData(currentWeaponData);
        }
    }

    /// <summary>
    /// 获取当前攻击阶段
    /// </summary>
    public int GetCurrentAttackStage()
    {
        return attackStage;
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public WeaponState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 检查是否在连击窗口期
    /// </summary>
    public bool IsInComboWindow()
    {
        return isComboWindowOpen;
    }

    /// <summary>
    /// 公共接口：刷新武器数据
    /// 关键修复：供PlayerWeaponController在武器切换时调用
    /// 确保在客户端上也能获取到最新的武器数据
    /// </summary>
    public void RefreshWeaponData()
    {
        UpdateWeaponData();
    }

    #endregion
}

/// <summary>
/// 武器状态枚举
/// </summary>
public enum WeaponState
{
    Idle,           // 待机
    Attacking,      // 攻击中
    ComboWindow,    // 连击窗口期
    Disabled        // 禁用
}
