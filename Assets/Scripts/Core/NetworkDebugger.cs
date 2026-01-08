using UnityEngine;
using Mirror;
using System.Linq;

public class NetworkDebugger : MonoBehaviour
{
    public static NetworkDebugger Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Debug.Log("[网络调试] NetworkDebugger已启动");
        CheckNetworkSetup();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            PrintNetworkStatus();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            CheckCharacterDatabase();
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CheckSpawnPoints();
        }
    }

    void CheckNetworkSetup()
    {
        Debug.Log("[网络调试] 检查网络设置...");
        
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[网络调试] NetworkManager未找到！");
            return;
        }

        GameNetworkManager manager = NetworkManager.singleton as GameNetworkManager;
        if (manager == null)
        {
            Debug.LogError("[网络调试] NetworkManager不是GameNetworkManager类型！");
            return;
        }

        Debug.Log($"[网络调试] NetworkManager类型: {manager.GetType().Name}");
        Debug.Log($"[网络调试] 当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"[网络调试] 服务器状态: {NetworkServer.active}");
        Debug.Log($"[网络调试] 客户端状态: {NetworkClient.active}");
        Debug.Log($"[网络调试] 已连接客户端数: {NetworkServer.connections.Count}");
    }

    void PrintNetworkStatus()
    {
        Debug.Log("========== 网络状态 ==========");
        Debug.Log($"服务器活跃: {NetworkServer.active}");
        Debug.Log($"客户端活跃: {NetworkClient.active}");
        Debug.Log($"主机模式: {NetworkServer.activeHost}");
        
        if (NetworkManager.singleton != null)
        {
            Debug.Log($"当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"在线玩家数: {NetworkServer.connections.Count}");
            
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && conn.identity != null)
                {
                    Debug.Log($"玩家: {conn.identity.netId}, 位置: {conn.identity.transform.position}");
                    NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
                    if (player != null)
                    {
                        Debug.Log($"  选择的角色ID: {player.selectedCharacterId}");
                    }
                }
            }
        }
        Debug.Log("=============================");
    }

    void CheckCharacterDatabase()
    {
        Debug.Log("========== 角色数据库检查 ==========");
        
        if (CharacterDatabase.Instance == null)
        {
            Debug.LogError("[角色数据库] CharacterDatabase单例未初始化！");
            Debug.LogError("[角色数据库] 请确保Character场景中有CharacterDatabase组件");
            return;
        }

        Debug.Log("[角色数据库] CharacterDatabase已初始化");
        
        var allCharacters = CharacterDatabase.Instance.GetAllCharacters();
        Debug.Log($"[角色数据库] 总共 {allCharacters.Count()} 个角色:");
        
        foreach (var character in allCharacters)
        {
            Debug.Log($"  - ID: {character.id}, 名称: {character.name}, 生命值: {character.health}, 速度: {character.speed}");
        }
        
        CharacterData scout = CharacterDatabase.Instance.GetCharacter("Scout");
        if (scout != null)
        {
            Debug.Log("[角色数据库] Scout角色数据验证通过");
        }
        else
        {
            Debug.LogError("[角色数据库] Scout角色数据未找到！");
        }
        Debug.Log("=================================");
    }

    void CheckSpawnPoints()
    {
        Debug.Log("========== 生成点检查 ==========");
        
        GameObject[] yingdiObjects = GameObject.FindGameObjectsWithTag("yingdi");
        Debug.Log($"[生成点] 找到 {yingdiObjects.Length} 个'yingdi'标签的对象:");
        
        foreach (GameObject obj in yingdiObjects)
        {
            Debug.Log($"  - 名称: {obj.name}, 位置: {obj.transform.position}");
            
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                Bounds bounds = col.bounds;
                Debug.Log($"    碰撞体边界: min={bounds.min}, max={bounds.max}");
            }
        }
        
        if (yingdiObjects.Length == 0)
        {
            Debug.LogWarning("[生成点] 未找到'yingdi'标签的对象！");
            Debug.LogWarning("[生成点] 角色将生成在 Vector3.zero 位置");
        }
        Debug.Log("===============================");
    }

    public void LogPlayerSpawn(NetworkPlayer player, Vector3 position, string characterId)
    {
        Debug.Log($"[玩家生成] 玩家 {player.netId} 已生成");
        Debug.Log($"[玩家生成] 位置: {position}");
        Debug.Log($"[玩家生成] 角色ID: {characterId}");
    }

    public void LogCharacterDataApplied(CharacterStats stats, CharacterData data)
    {
        Debug.Log($"[角色数据] 已应用角色数据到 {stats.name}");
        Debug.Log($"[角色数据] 最大生命值: {stats.maxHealth}");
        Debug.Log($"[角色数据] 攻击力: {stats.attack}");
        Debug.Log($"[角色数据] 移动速度: {stats.moveSpeed}");
    }
}
