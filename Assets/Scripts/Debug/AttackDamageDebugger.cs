using UnityEngine;
using Mirror;

/// <summary>
/// 攻击伤害调试工具
/// 用于检查角色攻击力和敌人血量的实际值
/// </summary>
public class AttackDamageDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugLog = true;
    public KeyCode debugKey = KeyCode.F8;
    
    void Update()
    {
        if (enableDebugLog && Input.GetKeyDown(debugKey))
        {
            LogAttackDamageInfo();
        }
    }
    
    /// <summary>
    /// 记录攻击伤害相关信息
    /// </summary>
    void LogAttackDamageInfo()
    {
        Debug.Log("=== 攻击伤害调试信息 ===");
        
        // 检查所有玩家角色
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            Debug.Log($"玩家 {player.netId} (本地: {player.isLocalPlayer}):");
            
            CharacterStats stats = player.GetComponentInChildren<CharacterStats>();
            if (stats != null)
            {
                Debug.Log($"  角色ID: {stats.characterId}");
                Debug.Log($"  攻击力: {stats.attack}");
                Debug.Log($"  当前血量: {stats.currentHealth}/{stats.maxHealth}");
                Debug.Log($"  移动速度: {stats.moveSpeed}");
                
                // 检查CharacterData中的原始数据
                if (CharacterDatabase.Instance != null)
                {
                    CharacterData charData = CharacterDatabase.Instance.GetCharacter(stats.characterId);
                    if (charData != null)
                    {
                        Debug.Log($"  配置攻击力: {charData.attack}");
                        Debug.Log($"  数据匹配: {(stats.attack == charData.attack ? "是" : "否")}");
                    }
                    else
                    {
                        Debug.LogWarning($"  无法找到角色ID {stats.characterId} 的配置数据");
                    }
                }
                else
                {
                    Debug.LogWarning("  CharacterDatabase实例为空");
                }
            }
            else
            {
                Debug.LogWarning("  未找到CharacterStats组件");
            }
        }
        
        // 检查所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"找到 {enemies.Length} 个敌人:");
        
        foreach (GameObject enemy in enemies)
        {
            EnemyHealthManager enemyHealth = enemy.GetComponent<EnemyHealthManager>();
            if (enemyHealth != null)
            {
                Debug.Log($"  敌人 {enemy.name}:");
                Debug.Log($"    类型: {enemyHealth.enemyType}");
                Debug.Log($"    血量: {enemyHealth.currentHealth}/{enemyHealth.maxHealth}");
                
                // 检查EnemyData中的原始数据
                if (EnemyDatabase.GetInstance() != null)
                {
                    EnemyData enemyData = EnemyDatabase.GetInstance().GetEnemy(enemyHealth.enemyType);
                    if (enemyData != null)
                    {
                        Debug.Log($"    配置血量: {enemyData.health}");
                        Debug.Log($"    数据匹配: {(enemyHealth.maxHealth == enemyData.health ? "是" : "否")}");
                    }
                    else
                    {
                        Debug.LogWarning($"    无法找到敌人类型 {enemyHealth.enemyType} 的配置数据");
                    }
                }
                else
                {
                    Debug.LogWarning("    EnemyDatabase实例为空");
                }
            }
            else
            {
                Debug.LogWarning($"  敌人 {enemy.name} 未找到EnemyHealthManager组件");
            }
        }
        
        Debug.Log("=== 调试信息结束 ===");
    }
}