using Mirror;
using UnityEngine;
using Core;

public class EnemySpawner : NetworkBehaviour
{
    public string smallEnemyKey = "SmallEnemy";
    public string bigEnemyKey = "BigEnemy";

    float checkTimer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 对象池现在会在初始化时自动生成并激活敌人，无需在此处手动生成
        Debug.Log("EnemySpawner: 敌人生成器已启动，对象池将自动处理初始敌人生成");
    }

    [Server]
    void SpawnRandomEnemy()
    {
        if (PoolConfigProvider.Instance == null)
        {
            Debug.LogError("SpawnRandomEnemy: PoolConfigProvider.Instance 为空");
            return;
        }

        PoolConfig config = PoolConfigProvider.Instance.Config;
        if (config == null)
        {
            Debug.LogError("SpawnRandomEnemy: PoolConfig 为空");
            return;
        }

        Vector3 pos = GetRandomSpawnPosition(config.enemySpawnRange, config.campAvoidanceDistance);
        string key = Random.value > 0.5f ? smallEnemyKey : bigEnemyKey;
        
        GameObject enemy = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);
        if (enemy != null)
        {
            EnemyAIController aiController = enemy.GetComponent<EnemyAIController>();
            if (aiController != null)
            {
                aiController.enemyType = key;
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] 敌人对象上没有找到EnemyAIController组件，无法设置敌人类型");
            }
        }
        else
        {
            Debug.LogError($"EnemySpawner: 生成敌人失败，位置: {pos}, 类型: {key}");
        }
    }

    Vector3 GetRandomSpawnPosition(float range, float avoidDistance)
    {
        return SpawnPositionHelper.GetRandomPositionOnSceneTerrain(avoidDistance, 20);
    }

    [ServerCallback]
    void Update()
    {
        checkTimer += Time.deltaTime;
        if (checkTimer >= 30f)
        {
            checkTimer = 0f;
        }
    }
}
