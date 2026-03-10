using UnityEngine;
using Mirror;

/// <summary>
/// 敌人血条系统测试脚本
/// 用于验证EnemyHealthManager是否正常工作
/// </summary>
public class EnemyHealthTest : MonoBehaviour
{
    [SerializeField]
    private GameObject enemyPrefab;
    
    [SerializeField]
    private int testEnemyCount = 3;
    
    [SerializeField]
    private GameObject healthBarPrefab;
    
    void Start()
    {
        if (NetworkServer.active && enemyPrefab != null && healthBarPrefab != null)
        {
            Debug.Log("[EnemyHealthTest] 开始测试敌人血条系统...");
            
            // 生成测试敌人
            for (int i = 0; i < testEnemyCount; i++)
            {
                Vector3 spawnPos = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                
                // 添加EnemyHealthManager组件
                EnemyHealthManager healthManager = enemy.GetComponent<EnemyHealthManager>();
                if (healthManager == null)
                {
                    healthManager = enemy.AddComponent<EnemyHealthManager>();
                    healthManager.healthBarPrefab = healthBarPrefab;
                }
                else
                {
                    healthManager.healthBarPrefab = healthBarPrefab;
                }
                
                // 设置敌人类型
                EnemyAIController enemyController = enemy.GetComponent<EnemyAIController>();
                if (enemyController != null)
                {
                    enemyController.enemyType = i % 2 == 0 ? "Goblin" : "Orc";
                }
                
                // 在服务器上生成敌人
                NetworkServer.Spawn(enemy);
            }
            
            Debug.Log("[EnemyHealthTest] 测试敌人生成完成，共生成" + testEnemyCount + "个敌人");
        }
    }
}