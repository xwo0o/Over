using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class EnemyData
{
    public string enemyType;
    public string name;
    public int health;
    public int attackDamage;
    public float patrolSpeed;
    public float chaseSpeed;
    public float viewAngle;
    public float viewDistance;
    public string addressableKey;
    public string patrolAnimation;
    public string chaseAnimation;
    public string attackAnimation;
}

[System.Serializable]
class EnemyDataCollection
{
    public List<EnemyData> EnemyDatas;
}

public class EnemyDatabase : MonoBehaviour
{
    public static EnemyDatabase Instance { get; private set; }

    private readonly Dictionary<string, EnemyData> enemies = new Dictionary<string, EnemyData>();
    private static bool initialized = false;

    public static EnemyDatabase GetInstance()
    {
        if (Instance == null && !initialized)
        {
            InitializeInstance();
        }
        return Instance;
    }

    private static void InitializeInstance()
    {
        initialized = true;
        
        GameObject dbObj = GameObject.Find("EnemyDatabase");
        if (dbObj == null)
        {
            dbObj = new GameObject("EnemyDatabase");
            DontDestroyOnLoad(dbObj);
            Instance = dbObj.AddComponent<EnemyDatabase>();
            Debug.Log("[EnemyDatabase] 已自动创建EnemyDatabase实例");
        }
        else
        {
            EnemyDatabase db = dbObj.GetComponent<EnemyDatabase>();
            if (db == null)
            {
                Instance = dbObj.AddComponent<EnemyDatabase>();
                Debug.Log("[EnemyDatabase] 已为现有EnemyDatabase对象添加组件");
            }
            else
            {
                Instance = db;
                Debug.Log("[EnemyDatabase] 已找到现有EnemyDatabase实例");
            }
        }
    }

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
        string path = Path.Combine(Application.streamingAssetsPath, "EnemyData.json");
        Debug.Log("[EnemyDatabase] 尝试加载敌人数据文件: " + path);
        
        if (!File.Exists(path))
        {
            Debug.LogWarning("[EnemyDatabase] 敌人数据文件不存在: " + path);
            return;
        }
        
        string json = File.ReadAllText(path);
        Debug.Log("[EnemyDatabase] 敌人数据文件内容: " + json);
        
        EnemyDataCollection collection = JsonUtility.FromJson<EnemyDataCollection>(json);
        if (collection == null)
        {
            Debug.LogWarning("[EnemyDatabase] 无法解析敌人数据文件");
            return;
        }
        
        if (collection.EnemyDatas == null)
        {
            Debug.LogWarning("[EnemyDatabase] 敌人数据列表为空");
            return;
        }
        
        Debug.Log("[EnemyDatabase] 加载敌人数据数量: " + collection.EnemyDatas.Count);
        
        foreach (var data in collection.EnemyDatas)
        {
            if (!string.IsNullOrEmpty(data.enemyType))
            {
                enemies[data.enemyType] = data;
                Debug.Log("[EnemyDatabase] 加载敌人数据: " + data.enemyType + ", 血量: " + data.health);
            }
            else
            {
                Debug.LogWarning("[EnemyDatabase] 敌人类型为空，跳过");
            }
        }
        
        Debug.Log("[EnemyDatabase] 敌人数据加载完成，共加载 " + enemies.Count + " 种敌人数据");
    }

    public EnemyData GetEnemy(string enemyType)
    {
        EnemyData data;
        enemies.TryGetValue(enemyType, out data);
        return data;
    }
}
