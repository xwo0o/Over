using UnityEngine;
using Mirror;

/// <summary>
/// 详细的攻击伤害调试工具
/// 用于追踪攻击伤害的完整计算过程
/// </summary>
public class DetailedAttackDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDetailedLog = true;
    public KeyCode debugKey = KeyCode.F7;
    
    void Update()
    {
        if (enableDetailedLog && Input.GetKeyDown(debugKey))
        {
            LogDetailedAttackInfo();
        }
    }
    
    /// <summary>
    /// 记录详细的攻击信息
    /// </summary>
    void LogDetailedAttackInfo()
    {
        Debug.Log("========== 详细攻击伤害调试信息 ==========");
        
        // 1. 检查CharacterDatabase中的原始数据
        Debug.Log("1. CharacterDatabase中的角色数据:");
        if (CharacterDatabase.Instance != null)
        {
            var allCharacters = CharacterDatabase.Instance.GetAllCharacters();
            foreach (var charData in allCharacters)
            {
                Debug.Log($"  角色ID: {charData.id}, 名称: {charData.name}, 攻击力: {charData.attack}, 血量: {charData.health}");
            }
        }
        else
        {
            Debug.LogWarning("  CharacterDatabase实例为空");
        }
        
        // 2. 检查EnemyDatabase中的原始数据
        Debug.Log("\n2. EnemyDatabase中的敌人数据:");
        if (EnemyDatabase.GetInstance() != null)
        {
            // 由于EnemyDatabase没有GetAllEnemies方法，我们只能手动检查
            Debug.Log("  SmallEnemy - 血量: 270, 攻击力: 50");
            Debug.Log("  BigEnemy - 血量: 400, 攻击力: 60");
        }
        else
        {
            Debug.LogWarning("  EnemyDatabase实例为空");
        }
        
        // 3. 检查所有玩家角色的实际值
        Debug.Log("\n3. 玩家角色的实际属性:");
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            Debug.Log($"  玩家 {player.netId} (本地: {player.isLocalPlayer}):");
            
            CharacterStats stats = player.GetComponentInChildren<CharacterStats>();
            if (stats != null)
            {
                Debug.Log($"    角色ID: {stats.characterId}");
                Debug.Log($"    实际攻击力: {stats.attack}");
                Debug.Log($"    当前血量: {stats.currentHealth}/{stats.maxHealth}");
                Debug.Log($"    移动速度: {stats.moveSpeed}");
                
                // 验证数据是否匹配
                if (CharacterDatabase.Instance != null)
                {
                    CharacterData charData = CharacterDatabase.Instance.GetCharacter(stats.characterId);
                    if (charData != null)
                    {
                        bool attackMatches = stats.attack == charData.attack;
                        bool healthMatches = stats.maxHealth == charData.health;
                        bool speedMatches = stats.moveSpeed == charData.speed;
                        
                        Debug.Log($"    数据匹配情况:");
                        Debug.Log($"      攻击力: {(attackMatches ? "✓" : "✗")} (实际: {stats.attack}, 配置: {charData.attack})");
                        Debug.Log($"      血量: {(healthMatches ? "✓" : "✗")} (实际: {stats.maxHealth}, 配置: {charData.health})");
                        Debug.Log($"      速度: {(speedMatches ? "✓" : "✗")} (实际: {stats.moveSpeed}, 配置: {charData.speed})");
                        
                        if (!attackMatches || !healthMatches || !speedMatches)
                        {
                            Debug.LogWarning($"    ⚠️ 数据不匹配！角色 {stats.characterId} 的属性未正确初始化");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"    ⚠️ 无法找到角色ID {stats.characterId} 的配置数据");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"    ⚠️ 未找到CharacterStats组件");
            }
            
            // 检查PlayerInputHandler的攻击参数
            PlayerInputHandler inputHandler = player.GetComponent<PlayerInputHandler>();
            if (inputHandler != null)
            {
                Debug.Log($"    攻击范围: {inputHandler.attackRange}米");
                Debug.Log($"    攻击角度: {inputHandler.attackAngle}度");
            }
            else
            {
                Debug.LogWarning($"    ⚠️ 未找到PlayerInputHandler组件");
            }
        }
        
        // 4. 检查所有敌人的实际值
        Debug.Log("\n4. 敌人的实际属性:");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"  找到 {enemies.Length} 个敌人");
        
        foreach (GameObject enemy in enemies)
        {
            EnemyHealthManager enemyHealth = enemy.GetComponent<EnemyHealthManager>();
            if (enemyHealth != null)
            {
                Debug.Log($"  敌人 {enemy.name}:");
                Debug.Log($"    类型: {enemyHealth.enemyType}");
                Debug.Log($"    实际血量: {enemyHealth.currentHealth}/{enemyHealth.maxHealth}");
                
                // 验证数据是否匹配
                if (EnemyDatabase.GetInstance() != null)
                {
                    EnemyData enemyData = EnemyDatabase.GetInstance().GetEnemy(enemyHealth.enemyType);
                    if (enemyData != null)
                    {
                        bool healthMatches = enemyHealth.maxHealth == enemyData.health;
                        
                        Debug.Log($"    数据匹配情况:");
                        Debug.Log($"      血量: {(healthMatches ? "✓" : "✗")} (实际: {enemyHealth.maxHealth}, 配置: {enemyData.health})");
                        
                        if (!healthMatches)
                        {
                            Debug.LogWarning($"    ⚠️ 数据不匹配！敌人 {enemyHealth.enemyType} 的血量未正确初始化");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"    ⚠️ 无法找到敌人类型 {enemyHealth.enemyType} 的配置数据");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"  ⚠️ 敌人 {enemy.name} 未找到EnemyHealthManager组件");
            }
        }
        
        // 5. 模拟攻击计算
        Debug.Log("\n5. 模拟攻击计算:");
        foreach (NetworkPlayer player in players)
        {
            CharacterStats playerStats = player.GetComponentInChildren<CharacterStats>();
            if (playerStats != null && playerStats.attack > 0)
            {
                Debug.Log($"  玩家 {playerStats.characterId} (攻击力: {playerStats.attack}):");
                
                foreach (GameObject enemy in enemies)
                {
                    EnemyHealthManager enemyHealth = enemy.GetComponent<EnemyHealthManager>();
                    if (enemyHealth != null && enemyHealth.maxHealth > 0)
                    {
                        int damage = playerStats.attack;
                        int remainingHealth = enemyHealth.maxHealth - damage;
                        int hitsToKill = Mathf.CeilToInt((float)enemyHealth.maxHealth / damage);
                        
                        Debug.Log($"    对 {enemyHealth.enemyType} (血量: {enemyHealth.maxHealth}):");
                        Debug.Log($"      伤害: {damage}, 剩余血量: {remainingHealth}, 需要攻击次数: {hitsToKill}");
                        
                        if (hitsToKill == 1)
                        {
                            Debug.LogWarning($"      ⚠️ 警告：{playerStats.characterId} 可以一下击杀 {enemyHealth.enemyType}！");
                        }
                    }
                }
            }
        }
        
        Debug.Log("========== 调试信息结束 ==========");
    }
}