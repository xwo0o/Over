using UnityEngine;

/// <summary>
/// 武器动画事件接收器 - 挂在玩家身上，供Animation Event调用
/// </summary>
public class WeaponAnimEvents : MonoBehaviour
{
    [Header("系统引用")]
    [Tooltip("武器状态管理器")]
    public WeaponStateManager weaponStateManager;
    [Tooltip("武器伤害系统")]
    public WeaponDamageSystem damageSystem;
    [Tooltip("Animator组件")]
    public Animator animator;

    // Animator参数Hash（性能优化）
    private static readonly int ComboWindow = Animator.StringToHash("ComboWindow");

    void Awake()
    {
        // 自动查找组件（优先从自身，然后查找父物体，最后查找子物体）
        if (weaponStateManager == null)
            weaponStateManager = GetComponent<WeaponStateManager>() 
                ?? GetComponentInParent<WeaponStateManager>() 
                ?? GetComponentInChildren<WeaponStateManager>();
        if (damageSystem == null)
            damageSystem = GetComponent<WeaponDamageSystem>() 
                ?? GetComponentInParent<WeaponDamageSystem>() 
                ?? GetComponentInChildren<WeaponDamageSystem>();
        if (animator == null)
            animator = GetComponent<Animator>() 
                ?? GetComponentInParent<Animator>() 
                ?? GetComponentInChildren<Animator>();
        
        Debug.Log($"[WeaponAnimEvents] Awake - weaponStateManager={(weaponStateManager != null)}, damageSystem={(damageSystem != null)}, animator={(animator != null)}");
    }

    /// <summary>
    /// 动画事件：开启连击窗口（挂在A1和A2动画的comboWindowStart时间点）
    /// 例如：阔刀在0.15s触发
    /// </summary>
    public void AE_OpenWindow()
    {
        // 设置Animator参数（用于Animator状态机判断）
        if (animator != null)
        {
            animator.SetBool(ComboWindow, true);
        }

        // 通知伤害系统开启连击窗口（用于代码逻辑判断）
        if (damageSystem != null)
        {
            damageSystem.OpenComboWindow();
        }

        Debug.Log("[WeaponAnimEvents] 连击窗口开启");
    }

    /// <summary>
    /// 动画事件：关闭连击窗口（挂在A1和A2动画的comboWindowEnd时间点）
    /// 例如：阔刀在0.6s触发
    /// </summary>
    public void AE_CloseWindow()
    {
        // 设置Animator参数
        if (animator != null)
        {
            animator.SetBool(ComboWindow, false);
        }

        // 通知伤害系统关闭连击窗口
        if (damageSystem != null)
        {
            damageSystem.CloseComboWindow();
        }

        Debug.Log("[WeaponAnimEvents] 连击窗口关闭");
    }

    /// <summary>
    /// 动画事件：开启伤害判定（挂在攻击动画的伤害判定开始帧）
    /// </summary>
    public void AE_EnableDamage()
    {
        if (weaponStateManager != null)
        {
            weaponStateManager.EnableDamage();
        }

        Debug.Log("[WeaponAnimEvents] 伤害判定开启");
    }

    /// <summary>
    /// 动画事件：关闭伤害判定（挂在攻击动画的伤害判定结束帧）
    /// </summary>
    public void AE_DisableDamage()
    {
        if (weaponStateManager != null)
        {
            weaponStateManager.DisableDamage();
        }

        Debug.Log("[WeaponAnimEvents] 伤害判定关闭");
    }

    /// <summary>
    /// 动画事件：执行伤害判定（挂在攻击动画的具体伤害帧）
    /// </summary>
    /// <param name="hitIndex">
    /// 伤害段数索引：
    /// - 阔刀/双刀：每段攻击使用 AE_Hit(0)，因为每段只有1次伤害
    /// - 斧头：使用 AE_Hit(0)、AE_Hit(1)、AE_Hit(2) 实现单次动画3次伤害
    /// </param>
    public void AE_Hit(int hitIndex)
    {
        Debug.Log($"[WeaponAnimEvents] 伤害判定: 第{hitIndex + 1}击");

        // 调用状态管理器的伤害判定
        if (weaponStateManager != null)
        {
            weaponStateManager.PerformDamageCheck();
        }
    }

    /// <summary>
    /// 动画事件：开始新的攻击段（A1、A2、A3每一段开始时调用）
    /// </summary>
    public void AE_StartAttackStage()
    {
        Debug.Log("[WeaponAnimEvents] 开始新的攻击段");

        // 通知伤害系统开始新的攻击段（重置伤害判定状态）
        if (damageSystem != null)
        {
            damageSystem.StartAttack();
        }
    }

    /// <summary>
    /// 动画事件：攻击结束（挂在攻击动画的最后一帧）
    /// 关键：通知状态管理器动画已结束，开启连击窗口
    /// </summary>
    public void EndAttack()
    {
        Debug.Log("[WeaponAnimEvents] 攻击动画结束事件触发");

        // 调用新的动画结束处理方法
        if (weaponStateManager != null)
        {
            weaponStateManager.OnAttackAnimationEnd();
        }
    }

    /// <summary>
    /// 动画事件：武器切换完成
    /// </summary>
    public void AE_EquipComplete()
    {
        Debug.Log("[WeaponAnimEvents] 武器切换完成");
    }

    /// <summary>
    /// 动画事件：播放武器音效
    /// </summary>
    /// <param name="soundType">音效类型：0=挥砍声, 1=击中声, 2=特殊音效</param>
    public void AE_PlaySound(int soundType)
    {
        // 可以在这里实现音效播放
        Debug.Log($"[WeaponAnimEvents] 播放音效: 类型{soundType}");
    }

    /// <summary>
    /// 动画事件：生成特效
    /// </summary>
    /// <param name="effectType">特效类型</param>
    public void AE_SpawnEffect(int effectType)
    {
        // 可以在这里实现特效生成
        Debug.Log($"[WeaponAnimEvents] 生成特效: 类型{effectType}");
    }
}
