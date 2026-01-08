using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : NetworkManager
{
    public GameStateManager gameStateManager;
    public string characterSelectionScene = "Character";
    public string gameScene = "GameScene";

    [Header("角色生成设置")]
    public Transform spawnPoint;
    public string spawnTag = "yingdi";
    public bool useRandomSpawn = true;
    public float spawnRadius = 5f;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == characterSelectionScene)
        {
            GameObject playerObj = Instantiate(playerPrefab);
            NetworkServer.AddPlayerForConnection(conn, playerObj);
            NetworkPlayer player = playerObj.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                player.OnServerPlayerAdded();
            }
        }
        else if (currentScene == gameScene)
        {
            // 在GameScene中，直接创建playerPrefab
            // 角色模型会通过CharacterModelManager加载到玩家对象上
            Debug.Log($"[GameNetworkManager] GameScene中玩家 {conn.connectionId} 已连接，创建玩家对象");
            
            GameObject playerObj = Instantiate(playerPrefab);
            NetworkServer.AddPlayerForConnection(conn, playerObj);
            NetworkPlayer player = playerObj.GetComponent<NetworkPlayer>();
            
            if (player != null)
            {
                player.OnServerPlayerAdded();
                Debug.Log($"[GameNetworkManager] 玩家对象已创建，等待角色选择命令");
            }
        }
        else
        {
            base.OnServerAddPlayer(conn);
            if (conn.identity == null)
                return;
            NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                player.OnServerPlayerAdded();
            }
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
    }

    [Server]
    public void StartGame()
    {
        if (SceneManager.GetActiveScene().name != gameScene)
        {
            ServerChangeScene(gameScene);
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        if (sceneName == gameScene)
        {
            // 角色生成现在由角色选择命令触发，不再在这里自动生成
            // 等待客户端发送CmdSelectCharacter命令后再生成角色
            
            if (gameStateManager != null)
            {
                gameStateManager.SetPhase(GamePhase.InGame);
            }
        }
    }

    [Server]
    public void SpawnCharacterForPlayer(NetworkPlayer player)
    {
        Debug.Log($"[角色实例化] 开始为玩家 {player.netId} 生成角色");
        Debug.Log($"[角色实例化] 当前selectedCharacterId: '{player.selectedCharacterId}'");

        if (string.IsNullOrEmpty(player.selectedCharacterId))
        {
            Debug.LogWarning($"[角色实例化] 玩家 {player.netId} 未选择角色，等待客户端发送角色选择命令");
            Debug.LogWarning($"[角色实例化] 服务器端不会自动设置默认角色，必须等待客户端通过CmdSelectCharacter发送角色ID");
            return;
        }

        Debug.Log($"[角色实例化] 玩家 {player.netId} 已选择角色: {player.selectedCharacterId}");

        if (CharacterDatabase.Instance == null)
        {
            Debug.LogError($"[角色实例化] CharacterDatabase未初始化！无法生成角色");
            Debug.LogError($"[角色实例化] 请确保Character场景中有CharacterDatabase组件");
            return;
        }

        CharacterData data = CharacterDatabase.Instance.GetCharacter(player.selectedCharacterId);
        if (data == null)
        {
            Debug.LogError($"[角色实例化] 找不到角色数据: {player.selectedCharacterId}");
            return;
        }

        Debug.Log($"[角色实例化] 角色数据加载成功: {data.name} (ID: {data.id})");

        CharacterStats stats = player.GetComponentInChildren<CharacterStats>();
        if (stats != null)
        {
            stats.InitializeCharacterData(data);
            Debug.Log($"[角色实例化] 角色属性已设置 - 生命值: {data.health}, 攻击力: {data.attack}, 速度: {data.speed}");
        }
        else
        {
            Debug.LogWarning($"[角色实例化] 未找到CharacterStats组件");
        }

        CharacterMovementController movement = player.GetComponentInChildren<CharacterMovementController>();
        if (movement != null)
        {
            movement.moveSpeed = data.speed;
            Debug.Log($"[角色实例化] 移动速度已设置为: {data.speed}");
        }
        else
        {
            Debug.LogWarning($"[角色实例化] 未找到CharacterMovementController组件");
        }

        Vector3 spawnPos = Vector3.zero;

        if (spawnPoint != null)
        {
            if (useRandomSpawn)
            {
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                spawnPos = spawnPoint.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
                spawnPos.y = 1f;
                Debug.Log($"[角色实例化] 在生成点周围随机生成角色，位置: {spawnPos}");
            }
            else
            {
                spawnPos = spawnPoint.position;
                spawnPos.y = 1f;
                Debug.Log($"[角色实例化] 在指定生成点生成角色，位置: {spawnPos}");
            }
        }
        else
        {
            GameObject spawnObj = GameObject.FindGameObjectWithTag(spawnTag);
            if (spawnObj != null)
            {
                Collider col = spawnObj.GetComponent<Collider>();
                if (col != null && useRandomSpawn)
                {
                    Bounds bounds = col.bounds;
                    spawnPos = new Vector3(
                        Random.Range(bounds.min.x, bounds.max.x),
                        1f,
                        Random.Range(bounds.min.z, bounds.max.z)
                    );
                    Debug.Log($"[角色实例化] 在{spawnTag}标签对象边界内随机生成角色，位置: {spawnPos}");
                }
                else if (col != null)
                {
                    spawnPos = spawnObj.transform.position;
                    spawnPos.y = 1f;
                    Debug.Log($"[角色实例化] 在{spawnTag}标签对象位置生成角色，位置: {spawnPos}");
                }
                else
                {
                    if (useRandomSpawn)
                    {
                        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                        spawnPos = spawnObj.transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
                        spawnPos.y = 1f;
                        Debug.Log($"[角色实例化] 在{spawnTag}标签对象周围随机生成角色，位置: {spawnPos}");
                    }
                    else
                    {
                        spawnPos = spawnObj.transform.position;
                        spawnPos.y = 1f;
                        Debug.Log($"[角色实例化] 在{spawnTag}标签对象位置生成角色，位置: {spawnPos}");
                    }
                }
            }
            else
            {
                spawnPos = new Vector3(0f, 1f, 0f);
                Debug.LogWarning($"[角色实例化] 未找到生成点或'{spawnTag}'标签的对象，使用默认生成位置 Y=1");
            }
        }
        
        player.transform.position = spawnPos;
        Debug.Log($"[角色实例化] 玩家 {player.netId} 角色生成完成，最终位置: {player.transform.position}");

        if (NetworkDebugger.Instance != null)
        {
            NetworkDebugger.Instance.LogPlayerSpawn(player, spawnPos, player.selectedCharacterId);
            if (stats != null)
            {
                NetworkDebugger.Instance.LogCharacterDataApplied(stats, data);
            }
        }
    }
}
