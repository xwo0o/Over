using UnityEngine;
using Mirror;

/// <summary>
/// 动画事件转发器 - 严格服务器权威架构
/// 
/// 设计原则：
/// 1. 动画事件在所有客户端触发
/// 2. 但只有服务器执行游戏逻辑
/// 3. 客户端只处理视觉表现
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    private WeaponStateManager weaponStateManager;
    private NetworkBehaviour networkBehaviour;

    private void Start()
    {
        StartCoroutine(DelayedFindComponents());
    }

    private System.Collections.IEnumerator DelayedFindComponents()
    {
        yield return null;
        yield return null;
        yield return null;

        FindComponents();

        int attempts = 0;
        int maxAttempts = 5;

        while ((weaponStateManager == null || networkBehaviour == null) && attempts < maxAttempts)
        {
            yield return new WaitForSeconds(0.1f);
            FindComponents();
            attempts++;
        }
    }

    private void FindComponents()
    {
        // 查找 WeaponStateManager
        if (weaponStateManager == null)
        {
            weaponStateManager = FindParentComponent<WeaponStateManager>(transform);
        }

        // 查找 NetworkBehaviour（用于检查 isServer）
        if (networkBehaviour == null && weaponStateManager != null)
        {
            networkBehaviour = weaponStateManager.GetComponent<NetworkBehaviour>();
        }
    }

    private T FindParentComponent<T>(Transform current) where T : Component
    {
        if (current == null) return null;

        T component = current.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        if (current.parent != null)
        {
            return FindParentComponent<T>(current.parent);
        }

        return null;
    }

    /// <summary>
    /// 动画事件：攻击命中（AE_Hit）
    /// 关键修复：在所有客户端触发，由WeaponStateManager内部发送Command到服务器执行伤害判定
    /// </summary>
    public void AE_Hit(int hitIndex = 0)
    {
        if (weaponStateManager == null) return;

        // 关键修复：移除isServer检查，让所有客户端都能调用PerformDamageCheck
        // WeaponStateManager.PerformDamageCheck内部会发送Command到服务器
        if (networkBehaviour != null)
        {
            weaponStateManager.PerformDamageCheck();
        }
    }

    /// <summary>
    /// 兼容旧方法名
    /// </summary>
    public void OnAttackHit()
    {
        AE_Hit(0);
    }

    /// <summary>
    /// 动画事件：攻击开始
    /// 由动画触发，但逻辑在 WeaponStateManager 的 SyncVar hook 中处理
    /// </summary>
    public void StartAttack()
    {
        // 动画事件触发，但实际的攻击逻辑由 WeaponStateManager 的 SyncVar 控制
        // 这里不需要做任何事，因为攻击状态由服务器同步
    }

    /// <summary>
    /// 动画事件：攻击结束
    /// 只在服务器调用结束攻击
    /// </summary>
    public void EndAttack()
    {
        Debug.Log($"[AnimationEventForwarder] EndAttack 被调用 - isServer={networkBehaviour?.isServer}");

        if (weaponStateManager == null)
        {
            Debug.LogWarning("[AnimationEventForwarder] weaponStateManager 为空，无法结束攻击");
            return;
        }

        // 只有服务器调用结束攻击
        if (networkBehaviour != null && networkBehaviour.isServer)
        {
            // 关键修复：使用新的 OnAttackAnimationEnd 方法
            weaponStateManager.OnAttackAnimationEnd();
            Debug.Log("[AnimationEventForwarder] 已调用 OnAttackAnimationEnd");
        }
    }

    /// <summary>
    /// 动画事件：开启伤害判定
    /// 只在服务器执行
    /// </summary>
    public void EnableDamage()
    {
        if (weaponStateManager == null) return;

        if (networkBehaviour != null && networkBehaviour.isServer)
        {
            // 关键修复：使用新的 EnableDamage 方法
            weaponStateManager.EnableDamage();
        }
    }
}
