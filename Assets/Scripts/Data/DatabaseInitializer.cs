using UnityEngine;

/// <summary>
/// 自动初始化单例数据库
/// 确保所有数据库单例在游戏开始时正确创建
/// </summary>
public class DatabaseInitializer : MonoBehaviour
{
    [Header("数据库配置")]
    [Tooltip("是否自动创建WeaponDatabase")]
    public bool createWeaponDatabase = true;
    
    [Tooltip("是否自动创建ResourceDatabase")]
    public bool createResourceDatabase = true;
    
    [Tooltip("是否自动创建EnemyDatabase")]
    public bool createEnemyDatabase = true;
    
    [Tooltip("是否自动创建CharacterDatabase")]
    public bool createCharacterDatabase = true;

    void Awake()
    {
        InitializeDatabases();
    }

    void InitializeDatabases()
    {
        Debug.Log("[DatabaseInitializer] 开始初始化数据库");

        if (createWeaponDatabase)
        {
            InitializeDatabase<WeaponDatabase>("WeaponDatabase");
        }

        if (createResourceDatabase)
        {
            InitializeDatabase<ResourceDatabase>("ResourceDatabase");
        }

        if (createEnemyDatabase)
        {
            InitializeDatabase<EnemyDatabase>("EnemyDatabase");
        }

        if (createCharacterDatabase)
        {
            InitializeDatabase<CharacterDatabase>("CharacterDatabase");
        }

        Debug.Log("[DatabaseInitializer] 数据库初始化完成");
    }

    void InitializeDatabase<T>(string databaseName) where T : MonoBehaviour
    {
        var existing = FindObjectOfType<T>();
        if (existing != null)
        {
            Debug.Log($"[DatabaseInitializer] {databaseName} 已存在，跳过创建");
            return;
        }

        GameObject dbObj = new GameObject(databaseName);
        dbObj.AddComponent<T>();
        DontDestroyOnLoad(dbObj);
        Debug.Log($"[DatabaseInitializer] 自动创建 {databaseName}");
    }
}
