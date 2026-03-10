using UnityEngine;
using Mirror;

/// <summary>
/// 敌人AI配置修复工具
/// 在运行时自动修复敌人AI的配置问题
/// </summary>
public class EnemyAIConfigFixer : MonoBehaviour
{
    [Header("修复设置")]
    public float targetAttackCooldown = 3f;
    public float targetAttackDistance = 3f;
    public bool autoFixOnStart = true;

    void Start()
    {
        if (autoFixOnStart)
        {
            FixAllEnemyAIControllers();
        }
    }

    [ContextMenu("修复所有敌人AI配置")]
    public void FixAllEnemyAIControllers()
    {
        Debug.Log("[EnemyAIConfigFixer] 开始修复所有敌人AI控制器...");

        EnemyAIController[] controllers = FindObjectsOfType<EnemyAIController>();
        int fixedCooldownCount = 0;
        int fixedDistanceCount = 0;

        foreach (EnemyAIController controller in controllers)
        {
            bool needsFix = false;
            string fixInfo = "";

            if (controller.attackCooldown != targetAttackCooldown)
            {
                float oldValue = controller.attackCooldown;
                controller.attackCooldown = targetAttackCooldown;
                fixedCooldownCount++;
                needsFix = true;
                fixInfo += $"攻击冷却时间: {oldValue}秒 -> {targetAttackCooldown}秒\n";
            }

            if (controller.attackDistance != targetAttackDistance)
            {
                float oldValue = controller.attackDistance;
                controller.attackDistance = targetAttackDistance;
                fixedDistanceCount++;
                needsFix = true;
                fixInfo += $"攻击距离: {oldValue}米 -> {targetAttackDistance}米\n";
            }

            if (needsFix)
            {
                Debug.Log($"[EnemyAIConfigFixer] 已修复 {controller.gameObject.name}:\n{fixInfo}");
            }
        }

        Debug.Log($"[EnemyAIConfigFixer] 修复完成: 共修复{fixedCooldownCount}个攻击冷却时间，{fixedDistanceCount}个攻击距离");
    }

    [ContextMenu("显示当前配置")]
    public void ShowCurrentConfiguration()
    {
        Debug.Log("[EnemyAIConfigFixer] 当前敌人AI配置:");

        EnemyAIController[] controllers = FindObjectsOfType<EnemyAIController>();

        foreach (EnemyAIController controller in controllers)
        {
            Debug.Log($"[EnemyAIConfigFixer] {controller.gameObject.name}:");
            Debug.Log($"  - 敌人类型: {controller.enemyType}");
            Debug.Log($"  - 攻击冷却时间: {controller.attackCooldown}秒");
            Debug.Log($"  - 攻击距离: {controller.attackDistance}米");
            Debug.Log($"  - 检测半径: {controller.detectionRadius}米");
            Debug.Log($"  - 巡逻等待时间: {controller.patrolWaitTime}秒");
        }
    }
}