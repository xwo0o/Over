using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// 敌人AI配置验证工具
/// 用于检查并修复敌人AI控制器的配置问题
/// </summary>
public class EnemyAIConfigValidator : EditorWindow
{
    [MenuItem("Tools/Enemy/验证AI配置")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAIConfigValidator>("敌人AI配置验证");
    }

    void OnGUI()
    {
        GUILayout.Label("敌人AI配置验证工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("验证所有敌人AI配置"))
        {
            ValidateAllEnemyAIControllers();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("修复攻击冷却时间为3秒"))
        {
            FixAttackCooldown();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("显示当前配置"))
        {
            ShowCurrentConfiguration();
        }
    }

    void ValidateAllEnemyAIControllers()
    {
        Debug.Log("[EnemyAIConfigValidator] 开始验证所有敌人AI控制器...");

        EnemyAIController[] controllers = GameObject.FindObjectsOfType<EnemyAIController>();
        int totalControllers = controllers.Length;
        int issuesFound = 0;

        foreach (EnemyAIController controller in controllers)
        {
            bool hasIssues = false;
            string issues = "";

            if (controller.attackCooldown != 3f)
            {
                hasIssues = true;
                issuesFound++;
                issues += $"攻击冷却时间异常: {controller.attackCooldown}秒 (应为3秒)\n";
            }

            if (controller.attackDistance != 3f)
            {
                hasIssues = true;
                issuesFound++;
                issues += $"攻击距离异常: {controller.attackDistance}米 (应为3米)\n";
            }

            if (string.IsNullOrEmpty(controller.enemyType))
            {
                hasIssues = true;
                issuesFound++;
                issues += $"敌人类型为空\n";
            }

            if (hasIssues)
            {
                Debug.LogWarning($"[EnemyAIConfigValidator] 发现问题 - {controller.gameObject.name}:\n{issues}");
            }
            else
            {
                Debug.Log($"[EnemyAIConfigValidator] 配置正常 - {controller.gameObject.name}");
            }
        }

        Debug.Log($"[EnemyAIConfigValidator] 验证完成: 共{totalControllers}个敌人AI控制器，发现{issuesFound}个问题");
    }

    void FixAttackCooldown()
    {
        Debug.Log("[EnemyAIConfigValidator] 开始修复攻击冷却时间...");

        EnemyAIController[] controllers = GameObject.FindObjectsOfType<EnemyAIController>();
        int fixedCount = 0;

        foreach (EnemyAIController controller in controllers)
        {
            if (controller.attackCooldown != 3f)
            {
                float oldValue = controller.attackCooldown;
                controller.attackCooldown = 3f;
                EditorUtility.SetDirty(controller);
                fixedCount++;
                Debug.Log($"[EnemyAIConfigValidator] 已修复 {controller.gameObject.name}: {oldValue}秒 -> 3秒");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyAIConfigValidator] 修复完成: 共修复{fixedCount}个敌人AI控制器");
    }

    void ShowCurrentConfiguration()
    {
        Debug.Log("[EnemyAIConfigValidator] 当前敌人AI配置:");

        EnemyAIController[] controllers = GameObject.FindObjectsOfType<EnemyAIController>();

        foreach (EnemyAIController controller in controllers)
        {
            Debug.Log($"[EnemyAIConfigValidator] {controller.gameObject.name}:");
            Debug.Log($"  - 敌人类型: {controller.enemyType}");
            Debug.Log($"  - 攻击冷却时间: {controller.attackCooldown}秒");
            Debug.Log($"  - 攻击距离: {controller.attackDistance}米");
            Debug.Log($"  - 检测半径: {controller.detectionRadius}米");
            Debug.Log($"  - 巡逻等待时间: {controller.patrolWaitTime}秒");
        }
    }
}