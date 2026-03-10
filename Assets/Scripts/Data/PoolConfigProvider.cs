using UnityEngine;
using System.IO;

[System.Serializable]
public class PoolConfig
{
    public string smallEnemyPrefabKey;
    public string bigEnemyPrefabKey;
    public int initialEnemyPoolSize;
    public float enemySpawnRange;
    public float campAvoidanceDistance;
    public string woodPrefabKey;
    public string stonePrefabKey;
    public string applePrefabKey;
    public string pearPrefabKey;
    public int initialResourcePoolSize;
    public float resourceSpawnRange;
}

[System.Serializable]
class PoolConfigWrapper
{
    public PoolConfig poolConfig;
}

public class PoolConfigProvider : MonoBehaviour
{
    public static PoolConfigProvider Instance { get; private set; }

    public PoolConfig Config { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "PoolConfig.json");
        if (!File.Exists(path))
            return;
        string json = File.ReadAllText(path);
        PoolConfigWrapper wrapper = JsonUtility.FromJson<PoolConfigWrapper>(json);
        if (wrapper != null)
        {
            Config = wrapper.poolConfig;
        }
    }
}
