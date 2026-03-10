using UnityEngine;

/// <summary>
/// 可伤害接口 - 所有可以被攻击的目标都需要实现此接口
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    void TakeDamage(float damage);

    /// <summary>
    /// 受到伤害（带攻击者信息）
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="attackerNetId">攻击者的NetworkId</param>
    void TakeDamage(float damage, uint attackerNetId);
}
