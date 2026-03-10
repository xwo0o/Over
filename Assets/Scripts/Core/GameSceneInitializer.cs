using UnityEngine;

[DefaultExecutionOrder(-200)]
public class GameSceneInitializer : MonoBehaviour
{
    [Header("数据库组件引用")]
    [SerializeField]
    private EnemyDatabase enemyDatabase;

    [SerializeField]
    private CharacterDatabase characterDatabase;

    [SerializeField]
    private ResourceDatabase resourceDatabase;

    [SerializeField]
    private BuildingDataManager buildingDataManager;

    void Awake()
    {
        InitializeDatabases();
    }

    private void InitializeDatabases()
    {
        EnsureEnemyDatabase();
        EnsureCharacterDatabase();
        EnsureResourceDatabase();
        EnsureBuildingDataManager();

        Debug.Log("[GameSceneInitializer] 游戏场景数据库初始化完成");
    }

    private void EnsureEnemyDatabase()
    {
        if (enemyDatabase == null)
        {
            GameObject dbObj = GameObject.Find("EnemyDatabase");
            if (dbObj == null)
            {
                dbObj = new GameObject("EnemyDatabase");
                enemyDatabase = dbObj.AddComponent<EnemyDatabase>();
                Debug.Log("[GameSceneInitializer] 已创建EnemyDatabase组件");
            }
            else
            {
                enemyDatabase = dbObj.GetComponent<EnemyDatabase>();
                if (enemyDatabase == null)
                {
                    enemyDatabase = dbObj.AddComponent<EnemyDatabase>();
                    Debug.Log("[GameSceneInitializer] 已为EnemyDatabase对象添加EnemyDatabase组件");
                }
                else
                {
                    Debug.Log("[GameSceneInitializer] EnemyDatabase已存在");
                }
            }
        }
    }

    private void EnsureCharacterDatabase()
    {
        if (characterDatabase == null)
        {
            GameObject dbObj = GameObject.Find("CharacterDatabase");
            if (dbObj == null)
            {
                dbObj = new GameObject("CharacterDatabase");
                characterDatabase = dbObj.AddComponent<CharacterDatabase>();
                Debug.Log("[GameSceneInitializer] 已创建CharacterDatabase组件");
            }
            else
            {
                characterDatabase = dbObj.GetComponent<CharacterDatabase>();
                if (characterDatabase == null)
                {
                    characterDatabase = dbObj.AddComponent<CharacterDatabase>();
                    Debug.Log("[GameSceneInitializer] 已为CharacterDatabase对象添加CharacterDatabase组件");
                }
                else
                {
                    Debug.Log("[GameSceneInitializer] CharacterDatabase已存在");
                }
            }
        }
    }

    private void EnsureResourceDatabase()
    {
        if (resourceDatabase == null)
        {
            GameObject dbObj = GameObject.Find("ResourceDatabase");
            if (dbObj == null)
            {
                dbObj = new GameObject("ResourceDatabase");
                resourceDatabase = dbObj.AddComponent<ResourceDatabase>();
                Debug.Log("[GameSceneInitializer] 已创建ResourceDatabase组件");
            }
            else
            {
                resourceDatabase = dbObj.GetComponent<ResourceDatabase>();
                if (resourceDatabase == null)
                {
                    resourceDatabase = dbObj.AddComponent<ResourceDatabase>();
                    Debug.Log("[GameSceneInitializer] 已为ResourceDatabase对象添加ResourceDatabase组件");
                }
                else
                {
                    Debug.Log("[GameSceneInitializer] ResourceDatabase已存在");
                }
            }
        }
    }

    private void EnsureBuildingDataManager()
    {
        if (buildingDataManager == null)
        {
            GameObject dbObj = GameObject.Find("BuildingDataManager");
            if (dbObj == null)
            {
                dbObj = new GameObject("BuildingDataManager");
                buildingDataManager = dbObj.AddComponent<BuildingDataManager>();
                Debug.Log("[GameSceneInitializer] 已创建BuildingDataManager组件");
            }
            else
            {
                buildingDataManager = dbObj.GetComponent<BuildingDataManager>();
                if (buildingDataManager == null)
                {
                    buildingDataManager = dbObj.AddComponent<BuildingDataManager>();
                    Debug.Log("[GameSceneInitializer] 已为BuildingDataManager对象添加BuildingDataManager组件");
                }
                else
                {
                    Debug.Log("[GameSceneInitializer] BuildingDataManager已存在");
                }
            }
        }
    }
}
