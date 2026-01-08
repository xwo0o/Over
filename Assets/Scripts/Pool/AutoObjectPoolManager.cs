using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Reflection;
using Mirror;
using Core;

/// <summary>
/// 自动化对象池管理器
/// </summary>
public class AutoObjectPoolManager : NetworkBehaviour
{
    public static AutoObjectPoolManager Instance { get; private set; }

    /// <summary>对象池配置</summary>
    private AutoObjectPoolConfig config;

    /// <summary>对象池字典</summary>
    private Dictionary<string, AutoObjectPool> pools = new Dictionary<string, AutoObjectPool>();

    /// <summary>资源加载句柄字典</summary>
    private Dictionary<string, AsyncOperationHandle<GameObject>> assetHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    /// <summary>配置提供者</summary>
    private AutoPoolConfigProvider configProvider;

    /// <summary>定时检查间隔（秒）</summary>
    private const float CHECK_INTERVAL = 30f;
    
    /// <summary>对象池根对象，用于统一挂载所有对象池中的对象</summary>
    private GameObject poolRoot;
    
    /// <summary>活跃对象根对象，用于统一挂载所有活跃对象</summary>
    public GameObject ActiveRoot { get; private set; }
    
    /// <summary>非活跃对象根对象，用于统一挂载所有非活跃对象</summary>
    public GameObject InactiveRoot { get; private set; }
    
    /// <summary>资源加载完成标志字典</summary>
    private Dictionary<string, bool> resourceLoadStatus = new Dictionary<string, bool>();
    
    /// <summary>所有资源是否已加载完成</summary>
    public bool AllResourcesLoaded { get; private set; } = false;

    private void Awake()
    {
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 创建统一的对象池根对象
        CreatePoolRoots();
    }
    
    /// <summary>
    /// 创建对象池根对象
    /// </summary>
    private void CreatePoolRoots()
    {
        // 创建对象池总根对象
        poolRoot = new GameObject("ObjectPoolRoot");
        poolRoot.transform.SetParent(transform);
        poolRoot.transform.localPosition = Vector3.zero;
        poolRoot.transform.localRotation = Quaternion.identity;
        
        // 创建活跃对象根对象
        ActiveRoot = new GameObject("ActiveObjects");
        ActiveRoot.transform.SetParent(poolRoot.transform);
        ActiveRoot.transform.localPosition = Vector3.zero;
        ActiveRoot.transform.localRotation = Quaternion.identity;
        
        // 创建非活跃对象根对象
        InactiveRoot = new GameObject("InactiveObjects");
        InactiveRoot.transform.SetParent(poolRoot.transform);
        InactiveRoot.transform.localPosition = Vector3.zero;
        InactiveRoot.transform.localRotation = Quaternion.identity;
        
        Debug.Log("AutoObjectPoolManager: 已创建对象池根对象");
    }

    private bool hasInitialized = false;
    
    private void Start()
    {
        // 加载配置
        LoadConfig();
        
        // 启动定时检查
        StartCoroutine(ScheduledCheckCoroutine());
        
        // 延迟检查网络状态，确保网络服务器完全启动后再初始化对象池
        StartCoroutine(DelayedNetworkCheck());
    }
    
    /// <summary>
    /// 延迟检查网络状态，确保网络服务器完全启动后再初始化对象池
    /// </summary>
    private IEnumerator DelayedNetworkCheck()
    {
        // 等待几帧，确保网络系统已完全启动
        yield return null;
        yield return null;
        yield return null;
        
        // 检查网络状态
        CheckNetworkStatus();
    }
    
    private void Update()
    {
        // 只在未初始化时检查网络状态，避免持续输出日志
        if (!hasInitialized && NetworkServer.active)
        {
            CheckNetworkStatus();
        }
    }
    
    /// <summary>
    /// 检查网络状态，确保在网络服务器激活时初始化对象池
    /// </summary>
    private void CheckNetworkStatus()
    {
        // 只有在网络服务器激活的情况下才初始化对象池
        if (NetworkServer.active)
        {
            if (!hasInitialized)
            {
                hasInitialized = true;
                Debug.Log("AutoObjectPoolManager: 网络服务器已激活，开始初始化对象池");
            }
            
            if (config == null)
            {
                LoadConfig();
            }
            else if (pools.Count == 0)
            {
                InitializePools();
            }
        }
    }

    private void OnDestroy()
    {
        // 释放所有资源
        ReleaseAllResources();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            // 查找或创建配置提供者
            configProvider = FindObjectOfType<AutoPoolConfigProvider>();
            if (configProvider == null)
            {
                GameObject providerObj = new GameObject("AutoPoolConfigProvider");
                configProvider = providerObj.AddComponent<AutoPoolConfigProvider>();
                Debug.Log("AutoObjectPoolManager: 创建了新的AutoPoolConfigProvider实例");
            }

            // 等待一帧确保配置已加载
            StartCoroutine(WaitForConfigLoaded());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoObjectPoolManager: 加载配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 等待配置加载完成
    /// </summary>
    private IEnumerator WaitForConfigLoaded()
    {
        // 等待配置提供者初始化
        yield return null;
        
        int maxAttempts = 5;
        int attempts = 0;
        float waitTime = 0.5f;
        
        while (attempts < maxAttempts)
        {
            if (configProvider != null && configProvider.Config != null)
            {
                config = configProvider.Config;
                Debug.Log($"AutoObjectPoolManager: 成功加载配置，版本: {config.version}，对象池数量: {config.poolItems.Length}");
                
                // 配置加载完成后初始化对象池，无论hasInitialized状态
                InitializePools();
                yield break;
            }
            else
            {
                attempts++;
                Debug.LogWarning($"AutoObjectPoolManager: 第 {attempts}/{maxAttempts} 次尝试获取配置失败，等待 {waitTime} 秒后重试");
                yield return new WaitForSeconds(waitTime);
            }
        }
        
        if (config == null)
        {
            Debug.LogError("AutoObjectPoolManager: 多次尝试后仍无法获取配置");
            // 尝试手动创建配置
            CreateManualConfig();
        }
    }
    
    /// <summary>
    /// 手动创建配置
    /// </summary>
    private void CreateManualConfig()
    {
        Debug.Log("AutoObjectPoolManager: 尝试手动创建配置");
        
        try
        {
            // 创建临时配置
            AutoObjectPoolConfig tempConfig = new AutoObjectPoolConfig();
            tempConfig.version = "1.0";
            
            // 添加默认对象池配置项
            tempConfig.poolItems = new AutoObjectPoolConfigItem[]
            {
                new AutoObjectPoolConfigItem()
                {
                    poolId = "SmallEnemy",
                    objectType = "Enemy",
                    addressablePath = "SmallEnemy",
                    initialCapacity = 30,
                    updateThreshold = 10,
                    maxCapacity = 40
                },
                new AutoObjectPoolConfigItem()
                {
                    poolId = "BigEnemy",
                    objectType = "Enemy",
                    addressablePath = "BigEnemy",
                    initialCapacity = 20,
                    updateThreshold = 10,
                    maxCapacity = 30
                },
                new AutoObjectPoolConfigItem()
                {
                    poolId = "Wood",
                    objectType = "Resource",
                    addressablePath = "Wood",
                    initialCapacity = 15,
                    updateThreshold = 10
                },
                new AutoObjectPoolConfigItem()
                {
                    poolId = "Stone",
                    objectType = "Resource",
                    addressablePath = "Stone",
                    initialCapacity = 15,
                    updateThreshold = 10
                }
            };
            
            // 使用临时配置初始化对象池
            config = tempConfig;
            InitializePools();
            
            Debug.Log("AutoObjectPoolManager: 手动创建配置成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoObjectPoolManager: 手动创建配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePools()
    {
        if (config == null || config.poolItems == null)
        {
            Debug.LogError("AutoObjectPoolManager: 配置无效，无法初始化对象池");
            return;
        }
        
        // 确保只在服务器端初始化对象池
        if (!NetworkServer.active)
        {
            Debug.LogWarning("AutoObjectPoolManager: 不在服务器端，跳过对象池初始化");
            return;
        }

        // 从ResourceDatabase自动同步资源类型到对象池配置
        SyncResourceTypesFromDatabase();

        foreach (AutoObjectPoolConfigItem item in config.poolItems)
        {
            if (string.IsNullOrEmpty(item.poolId) || string.IsNullOrEmpty(item.addressablePath))
            {
                Debug.LogError($"AutoObjectPoolManager: 配置项无效，poolId或addressablePath为空");
                continue;
            }

            if (pools.ContainsKey(item.poolId))
            {
                Debug.LogWarning($"AutoObjectPoolManager: 对象池ID已存在: {item.poolId}");
                continue;
            }

            // 创建对象池
            AutoObjectPool pool = new AutoObjectPool(item, this);
            pools.Add(item.poolId, pool);
            
            Debug.Log($"AutoObjectPoolManager: 初始化对象池 - ID: {item.poolId}, 类型: {item.objectType}, 初始容量: {item.initialCapacity}, 更新阈值: {item.updateThreshold}");
        }
        
        Debug.Log($"AutoObjectPoolManager: 对象池初始化完成，共创建 {pools.Count} 个对象池");
        
        // 对象池初始化完成后，生成敌人
        StartCoroutine(SpawnInitialEnemies());
    }
    
    /// <summary>
    /// 生成初始敌人
    /// </summary>
    private IEnumerator SpawnInitialEnemies()
    {
        // 等待几帧，确保对象池完全初始化
        yield return null;
        yield return null;
        
        // 遍历所有对象池，为敌人类型生成初始敌人
        foreach (var item in config.poolItems)
        {
            if (item.objectType == "Enemy")
            {
                // 生成初始容量的敌人
                for (int i = 0; i < item.initialCapacity; i++)
                {
                    GameObject enemy = GetObject(item.poolId);
                    if (enemy != null)
                    {
                        Debug.Log($"AutoObjectPoolManager: 已生成初始敌人 {i + 1}/{item.initialCapacity}，池ID: {item.poolId}");
                    }
                    else
                    {
                        Debug.LogError($"AutoObjectPoolManager: 生成初始敌人失败，池ID: {item.poolId}");
                    }
                    
                    // 每生成一个敌人等待一帧，避免卡顿
                    if (i % 5 == 0)
                    {
                        yield return null;
                    }
                }
            }
        }
        
        Debug.Log("AutoObjectPoolManager: 初始敌人生成完成");
    }

    /// <summary>
    /// 从ResourceDatabase同步资源类型到对象池配置
    /// </summary>
    private void SyncResourceTypesFromDatabase()
    {
        // 等待ResourceDatabase初始化完成
        StartCoroutine(WaitForResourceDatabaseAndSync());
    }

    /// <summary>
    /// 等待ResourceDatabase初始化完成并同步资源类型
    /// </summary>
    private IEnumerator WaitForResourceDatabaseAndSync()
    {
        // 等待ResourceDatabase初始化
        int maxAttempts = 10;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            if (ResourceDatabase.Instance != null)
            {
                break;
            }
            attempts++;
            yield return new WaitForSeconds(0.5f);
        }
        
        if (ResourceDatabase.Instance == null)
        {
            Debug.LogWarning("AutoObjectPoolManager: ResourceDatabase未初始化，跳过资源类型同步");
            yield break;
        }

        // 获取当前配置中的资源类型池ID集合
        HashSet<string> existingResourcePoolIds = new HashSet<string>();
        foreach (var item in config.poolItems)
        {
            if (item.objectType == "Resource")
            {
                existingResourcePoolIds.Add(item.poolId);
            }
        }

        // 获取ResourceDatabase中的所有资源类型
        var allResources = new Dictionary<string, ResourceData>();
        // 通过反射获取ResourceDatabase的私有字段resources
        var resourcesField = typeof(ResourceDatabase).GetField("resources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (resourcesField != null)
        {
            var resources = resourcesField.GetValue(ResourceDatabase.Instance) as System.Collections.Generic.Dictionary<string, ResourceData>;
            if (resources != null)
            {
                foreach (var kvp in resources)
                {
                    allResources[kvp.Key] = kvp.Value;
                }
            }
        }

        // 为ResourceDatabase中存在但配置中不存在的资源类型创建对象池配置
        List<AutoObjectPoolConfigItem> newItems = new List<AutoObjectPoolConfigItem>(config.poolItems);
        int addedCount = 0;

        foreach (var kvp in allResources)
        {
            string resourceId = kvp.Key;
            ResourceData resourceData = kvp.Value;

            // 如果资源类型不在现有配置中，则添加
            if (!existingResourcePoolIds.Contains(resourceId))
            {
                AutoObjectPoolConfigItem newItem = new AutoObjectPoolConfigItem()
                {
                    poolId = resourceId,
                    objectType = "Resource",
                    addressablePath = resourceData.addressableKey,
                    initialCapacity = 15,
                    updateThreshold = 10
                };
                
                newItems.Add(newItem);
                existingResourcePoolIds.Add(resourceId);
                addedCount++;
                
                Debug.Log($"AutoObjectPoolManager: 自动添加资源类型到对象池配置 - ID: {resourceId}, 名称: {resourceData.name}, addressableKey: {resourceData.addressableKey}");
            }
        }

        // 如果有新增的资源类型，更新配置
        if (addedCount > 0)
        {
            config.poolItems = newItems.ToArray();
            Debug.Log($"AutoObjectPoolManager: 已从ResourceDatabase同步 {addedCount} 个资源类型到对象池配置，总资源类型数量: {existingResourcePoolIds.Count}");
        }
        else
        {
            Debug.Log($"AutoObjectPoolManager: 对象池配置已与ResourceDatabase同步，无需更新，总资源类型数量: {existingResourcePoolIds.Count}");
        }
    }

    /// <summary>
    /// 定时检查协程
    /// </summary>
    private IEnumerator ScheduledCheckCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(CHECK_INTERVAL);
            CheckAllPools();
        }
    }

    /// <summary>
    /// 检查所有对象池
    /// </summary>
    private void CheckAllPools()
    {
        // 确保只在服务器端执行检查
        if (!NetworkServer.active)
        {
            return;
        }
        
        foreach (var pair in pools)
        {
            pair.Value.CheckCapacity();
        }
    }

    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <param name="position">位置</param>
    /// <param name="rotation">旋转</param>
    /// <returns>获取到的游戏对象</returns>
    [Server]
    public GameObject GetObject(string poolId, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
    {
        // 确保只在服务器端执行
        if (!NetworkServer.active)
        {
            Debug.LogWarning($"AutoObjectPoolManager: 不在服务器端，无法从池 {poolId} 获取对象");
            return null;
        }
        
        if (!pools.TryGetValue(poolId, out AutoObjectPool pool))
        {
            Debug.LogError($"AutoObjectPoolManager: 未找到对象池: {poolId}");
            return null;
        }

        // 如果是资源类型对象，使用随机位置生成策略
        if (config != null && IsResourcePool(poolId))
        {
            // 为资源对象生成随机位置，避免在角色出生点附近生成造成碰撞
            position = GenerateRandomResourcePosition();
            rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0); // 随机Y轴旋转
        }
        
        // 如果是敌人类型对象，使用随机位置生成策略
        if (config != null && IsEnemyPool(poolId))
        {
            // 为敌人对象生成随机位置，避开yingdi区域
            position = GenerateRandomEnemyPosition();
            rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0); // 随机Y轴旋转
        }
        
        GameObject obj = pool.GetObject();
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            // 将活跃对象移动到活跃根对象下
            obj.transform.SetParent(ActiveRoot.transform, true);
            
            // 检查对象是否有NetworkIdentity组件，如果有则在网络中激活
            NetworkIdentity netIdentity = obj.GetComponent<NetworkIdentity>();
            if (netIdentity != null)
            {
                // 确保对象在网络中被正确处理
                if (!netIdentity.gameObject.activeSelf)
                {
                    netIdentity.gameObject.SetActive(true);
                }
                
                // 如果对象尚未在网络中生成（netId为0表示未生成），则进行网络生成
                if (netIdentity.netId == 0)
                {
                    NetworkServer.Spawn(obj);
                    Debug.Log($"AutoObjectPoolManager: 对象 {obj.name} 已在网络中生成，netId: {netIdentity.netId}");
                    
                    // 关键修复：只有在有真正的客户端连接时才通过ClientRpc同步父对象关系到所有客户端
                    // 主机启动时没有客户端连接，调用ClientRpc会导致空引用异常
                    // 检查是否有真正的客户端连接（排除主机本身）
                    int clientConnectionCount = NetworkServer.connections.Count;
                    bool hasRealClients = false;
                    
                    // 在主机模式下，connections.Count可能包含主机自己的连接
                    // 我们需要检查是否有真正的远程客户端连接
                    foreach (var connectionPair in NetworkServer.connections)
                    {
                        // connectionPair.Value 是 NetworkConnectionToClient 对象
                        var connection = connectionPair.Value;
                        // 如果连接不为空且不是本地主机连接，则认为是真正的客户端连接
                        // 通过比较连接对象与 NetworkServer.localConnection 来判断是否是本地主机
                        if (connection != null && connection != NetworkServer.localConnection)
                        {
                            hasRealClients = true;
                            Debug.Log($"AutoObjectPoolManager: 检测到真正的客户端连接 - connectionId: {connection.connectionId}");
                            break;
                        }
                    }
                    
                    if (hasRealClients)
                    {
                        Debug.Log($"AutoObjectPoolManager: 检测到 {clientConnectionCount} 个连接，其中有真正的客户端，调用RpcSetObjectParent");
                        RpcSetObjectParent(netIdentity.netId);
                    }
                    else
                    {
                        Debug.Log($"AutoObjectPoolManager: 没有检测到真正的客户端连接（总连接数: {clientConnectionCount}），跳过RpcSetObjectParent调用");
                    }
                }
                else
                {
                    // 对象已经生成，只需更新位置和旋转
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    
                    // 通知所有客户端重新显示此对象
                    RpcShowObject(netIdentity.netId, position, rotation);
                }
            }
            else
            {
                Debug.LogWarning($"AutoObjectPoolManager: 对象 {obj.name} 没有NetworkIdentity组件，无法进行网络同步");
            }
        }
        else
        {
            Debug.LogWarning($"AutoObjectPoolManager: 从池 {poolId} 获取对象失败");
        }

        return obj;
    }

    /// <summary>
    /// 客户端RPC：设置对象的父对象为客户端的AutoObjectPoolManager的activeRoot
    /// </summary>
    [ClientRpc]
    public void RpcSetObjectParent(uint objectNetId)
    {
        try
        {
            // 关键修复：确保NetworkClient系统已完全初始化
            if (!NetworkClient.active)
            {
                Debug.LogWarning($"AutoObjectPoolManager: NetworkClient未激活，跳过设置对象父对象 - objectNetId: {objectNetId}");
                return;
            }
            
            // 确保NetworkClient.spawned字典已初始化
            if (NetworkClient.spawned == null)
            {
                Debug.LogWarning($"AutoObjectPoolManager: NetworkClient.spawned未初始化，跳过设置对象父对象 - objectNetId: {objectNetId}");
                return;
            }
            
            // 在客户端上查找对应的网络对象
            if (NetworkClient.spawned.TryGetValue(objectNetId, out NetworkIdentity netIdentity))
            {
                // 确保网络对象不为空
                if (netIdentity == null)
                {
                    Debug.LogWarning($"AutoObjectPoolManager: 网络对象为空 - objectNetId: {objectNetId}");
                    return;
                }
                
                // 查找客户端的AutoObjectPoolManager实例
                AutoObjectPoolManager clientPoolManager = AutoObjectPoolManager.Instance;
                if (clientPoolManager != null && clientPoolManager.ActiveRoot != null)
                {
                    // 确保ActiveRoot的transform不为空
                    if (clientPoolManager.ActiveRoot.transform == null)
                    {
                        Debug.LogWarning($"AutoObjectPoolManager: ActiveRoot.transform为空 - objectNetId: {objectNetId}");
                        return;
                    }
                    
                    // 设置父对象关系
                    netIdentity.transform.SetParent(clientPoolManager.ActiveRoot.transform);
                    Debug.Log($"AutoObjectPoolManager: 客户端设置对象父对象 - objectNetId: {objectNetId}, parent: {clientPoolManager.ActiveRoot.name}");
                }
                else
                {
                    Debug.LogWarning($"AutoObjectPoolManager: 客户端AutoObjectPoolManager或ActiveRoot未找到 - objectNetId: {objectNetId}");
                }
            }
            else
            {
                Debug.LogWarning($"AutoObjectPoolManager: 客户端无法找到网络对象 - objectNetId: {objectNetId}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AutoObjectPoolManager: RpcSetObjectParent异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 通知所有客户端隐藏指定的网络对象
    /// </summary>
    /// <param name="objectNetId">要隐藏的网络对象的netId</param>
    [ClientRpc]
    public void RpcHideObject(uint objectNetId)
    {
        try
        {
            // 只在客户端上执行（主机也会执行，但这是安全的）
            if (isServer)
            {
                return;
            }

            // 根据netId查找网络对象
            NetworkIdentity foundObject = null;
            if (NetworkClient.spawned.TryGetValue(objectNetId, out foundObject))
            {
                // 在客户端上隐藏对象
                foundObject.gameObject.SetActive(false);
                Debug.Log($"AutoObjectPoolManager: 客户端已隐藏对象 - netId: {objectNetId}, 名称: {foundObject.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"AutoObjectPoolManager: 客户端无法找到网络对象 - objectNetId: {objectNetId}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AutoObjectPoolManager: RpcHideObject异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 通知所有客户端显示指定的网络对象
    /// </summary>
    /// <param name="objectNetId">要显示的网络对象的netId</param>
    /// <param name="position">对象的位置</param>
    /// <param name="rotation">对象的旋转</param>
    [ClientRpc]
    public void RpcShowObject(uint objectNetId, Vector3 position, Quaternion rotation)
    {
        try
        {
            // 只在客户端上执行（主机也会执行，但这是安全的）
            if (isServer)
            {
                return;
            }

            // 根据netId查找网络对象
            NetworkIdentity foundObject = null;
            if (NetworkClient.spawned.TryGetValue(objectNetId, out foundObject))
            {
                // 在客户端上显示对象
                foundObject.gameObject.SetActive(true);
                foundObject.transform.position = position;
                foundObject.transform.rotation = rotation;
                Debug.Log($"AutoObjectPoolManager: 客户端已显示对象 - netId: {objectNetId}, 名称: {foundObject.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"AutoObjectPoolManager: 客户端无法找到网络对象 - objectNetId: {objectNetId}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AutoObjectPoolManager: RpcShowObject异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 将对象归还到对象池
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <param name="obj">要归还的游戏对象</param>
    [Server]
    public void ReturnObject(string poolId, GameObject obj)
    {
        // 确保只在服务器端执行
        if (!NetworkServer.active)
        {
            Debug.LogWarning($"AutoObjectPoolManager: 不在服务器端，无法将对象归还到池 {poolId}");
            return;
        }
        
        if (obj == null)
        {
            Debug.LogWarning($"AutoObjectPoolManager: 归还的对象为null");
            return;
        }

        if (string.IsNullOrEmpty(poolId))
        {
            Debug.LogWarning($"AutoObjectPoolManager: 对象池ID为空");
            Destroy(obj);
            return;
        }

        if (pools == null)
        {
            Debug.LogError($"AutoObjectPoolManager: pools字典为null");
            Destroy(obj);
            return;
        }

        if (!pools.TryGetValue(poolId, out AutoObjectPool pool))
        {
            Debug.LogError($"AutoObjectPoolManager: 未找到对象池: {poolId}");
            Destroy(obj);
            return;
        }

        if (pool == null)
        {
            Debug.LogError($"AutoObjectPoolManager: 对象池为null: {poolId}");
            Destroy(obj);
            return;
        }

        if (InactiveRoot == null || InactiveRoot.transform == null)
        {
            Debug.LogError($"AutoObjectPoolManager: InactiveRoot或其transform为null");
            Destroy(obj);
            return;
        }

        // 检查对象是否有NetworkIdentity组件
        NetworkIdentity netIdentity = obj.GetComponent<NetworkIdentity>();
        if (netIdentity != null)
        {
            // 检查网络对象是否仍然有效
            if (!netIdentity.isServer)
            {
                Debug.LogWarning($"AutoObjectPoolManager: 网络对象不在服务器端，跳过RPC通知 - netId: {netIdentity.netId}");
            }
            // 检查网络对象是否已经被unspawned（netId为0表示未被spawned）
            else if (netIdentity.netId == 0)
            {
                Debug.LogWarning($"AutoObjectPoolManager: 网络对象已被unspawned，跳过RPC通知 - netId: {netIdentity.netId}");
            }
            // 检查网络服务器是否仍然活跃
            else if (!NetworkServer.active)
            {
                Debug.LogWarning($"AutoObjectPoolManager: 网络服务器已停止，跳过RPC通知 - netId: {netIdentity.netId}");
            }
            // 检查是否有连接的客户端
            else if (NetworkServer.connections.Count == 0)
            {
                Debug.Log($"AutoObjectPoolManager: 没有连接的客户端，跳过RPC通知 - netId: {netIdentity.netId}");
            }
            else
            {
                try
                {
                    // 通知所有客户端隐藏此对象
                    RpcHideObject(netIdentity.netId);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"AutoObjectPoolManager: 调用RpcHideObject时发生异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                }
            }
        }

        // 将非活跃对象移动到非活跃根对象下
        try
        {
            obj.transform.SetParent(InactiveRoot.transform, true);
            pool.ReturnObject(obj);
            Debug.Log($"AutoObjectPoolManager: 对象已归还到池 {poolId}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AutoObjectPoolManager: 返回对象到池时发生异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            Destroy(obj);
        }
    }

    /// <summary>
    /// 手动清理对象池
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    [Server]
    public void ClearPool(string poolId)
    {
        // 确保只在服务器端执行
        if (!NetworkServer.active)
        {
            Debug.LogWarning($"AutoObjectPoolManager: 不在服务器端，无法清理对象池 {poolId}");
            return;
        }
        
        if (!pools.TryGetValue(poolId, out AutoObjectPool pool))
        {
            Debug.LogError($"AutoObjectPoolManager: 未找到对象池: {poolId}");
            return;
        }

        pool.Clear();
    }

    /// <summary>
    /// 手动清理所有对象池
    /// </summary>
    [Server]
    public void ClearAllPools()
    {
        // 确保只在服务器端执行
        if (!NetworkServer.active)
        {
            Debug.LogWarning("AutoObjectPoolManager: 不在服务器端，无法清理所有对象池");
            return;
        }
        
        foreach (var pair in pools)
        {
            pair.Value.Clear();
        }
    }

    /// <summary>
    /// 手动触发所有对象池的容量检查（用于手动优化）
    /// </summary>
    [Server]
    public void ManualCapacityCheck()
    {
        // 确保只在服务器端执行
        if (!NetworkServer.active)
        {
            Debug.LogWarning("AutoObjectPoolManager: 不在服务器端，无法执行手动容量检查");
            return;
        }
        
        Debug.Log("AutoObjectPoolManager: 手动触发对象池容量检查");
        
        // 遍历所有对象池进行容量检查
        foreach (var pool in pools.Values)
        {
            if (pool != null)
            {
                try
                {
                    pool.CheckCapacity();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"AutoObjectPoolManager: 手动检查对象池时发生异常: {e.Message}");
                }
            }
        }
        
        Debug.Log("AutoObjectPoolManager: 手动对象池容量检查完成");
    }

    /// <summary>
    /// 获取所有对象池的统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public string GetAllPoolStatistics()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 对象池统计信息 ===");
        
        foreach (var pool in pools.Values)
        {
            if (pool != null)
            {
                sb.AppendLine(pool.GetStatistics());
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// 加载Addressable资源
    /// </summary>
    /// <param name="addressablePath">资源路径</param>
    /// <param name="callback">加载完成回调</param>
    public void LoadAsset(string addressablePath, System.Action<GameObject> callback)
    {
        if (string.IsNullOrEmpty(addressablePath))
        {
            Debug.LogError("AutoObjectPoolManager: 资源路径为空");
            callback?.Invoke(null);
            return;
        }

        try
        {
            // 检查是否已加载
            if (assetHandles.TryGetValue(addressablePath, out AsyncOperationHandle<GameObject> handle))
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(handle.Result);
                    return;
                }
            }

            // 加载资源
            handle = Addressables.LoadAssetAsync<GameObject>(addressablePath);
            assetHandles[addressablePath] = handle;

            handle.Completed += (operation) =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(operation.Result);
                }
                else
                {
                    Debug.LogError($"AutoObjectPoolManager: 加载资源失败: {addressablePath}, 错误: {operation.OperationException}");
                    callback?.Invoke(null);
                }
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoObjectPoolManager: 加载资源时发生异常: {addressablePath}, 异常: {e.Message}");
            callback?.Invoke(null);
        }
    }

    /// <summary>
    /// 获取非活跃对象根对象
    /// </summary>
    /// <returns>非活跃对象根对象</returns>
    public GameObject GetInactiveRoot()
    {
        return InactiveRoot;
    }
    
    /// <summary>
    /// 获取活跃对象根对象
    /// </summary>
    /// <returns>活跃对象根对象</returns>
    public GameObject GetActiveRoot()
    {
        return ActiveRoot;
    }
    
    /// <summary>
    /// 获取指定池的可用对象数量
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <returns>可用对象数量</returns>
    public int GetAvailableCount(string poolId)
    {
        if (pools.TryGetValue(poolId, out AutoObjectPool pool))
        {
            return pool.GetAvailableCount();
        }
        return 0;
    }
    
    /// <summary>
    /// 获取指定池的活跃对象数量
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <returns>活跃对象数量</returns>
    public int GetActiveCount(string poolId)
    {
        if (pools.TryGetValue(poolId, out AutoObjectPool pool))
        {
            return pool.GetActiveCount();
        }
        return 0;
    }
    
    /// <summary>
    /// 获取对象池的最大容量
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <returns>最大容量</returns>
    public int GetMaxCapacity(string poolId)
    {
        if (pools.TryGetValue(poolId, out AutoObjectPool pool))
        {
            return pool.GetMaxCapacity();
        }
        return 0;
    }
    
    /// <summary>
    /// 释放所有资源
    /// </summary>
    private void ReleaseAllResources()
    {
        // 释放所有对象池
        foreach (var pair in pools)
        {
            pair.Value.Clear();
        }
        pools.Clear();

        // 释放所有资源加载句柄
        foreach (var handle in assetHandles.Values)
        {
            Addressables.Release(handle);
        }
        assetHandles.Clear();

        // 销毁根对象
        if (poolRoot != null)
        {
            Destroy(poolRoot);
        }

        Debug.Log("AutoObjectPoolManager: 已释放所有资源");
    }
    
    /// <summary>
    /// 判断是否为资源类型的对象池
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <returns>如果是资源类型返回true，否则返回false</returns>
    private bool IsResourcePool(string poolId)
    {
        if (config == null || config.poolItems == null) return false;
        
        foreach (var item in config.poolItems)
        {
            if (item.poolId == poolId)
            {
                return item.objectType == "Resource";
            }
        }
        return false;
    }
    
    /// <summary>
    /// 判断是否为敌人类型的对象池
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    /// <returns>如果是敌人类型返回true，否则返回false</returns>
    private bool IsEnemyPool(string poolId)
    {
        if (config == null || config.poolItems == null) return false;
        
        foreach (var item in config.poolItems)
        {
            if (item.poolId == poolId)
            {
                return item.objectType == "Enemy";
            }
        }
        return false;
    }
    
    /// <summary>
    /// 生成随机资源位置，避免在角色出生点附近生成
    /// </summary>
    /// <returns>随机生成的位置</returns>
    private Vector3 GenerateRandomResourcePosition()
    {
        // 使用SpawnPositionHelper在Scene地形上生成随机位置，避开yingdi区域
        // 这样资源会在整个Scene地形上均匀分布，而不是集中在某个区域
        Vector3 randomPosition = SpawnPositionHelper.GetRandomPositionOnSceneTerrain(10f, 50);
        
        if (randomPosition != Vector3.zero)
        {
            Debug.Log($"AutoObjectPoolManager: 在Scene地形上生成资源位置: {randomPosition}");
        }
        else
        {
            Debug.LogWarning($"AutoObjectPoolManager: 未能找到有效的资源生成位置，使用默认位置");
        }
        
        return randomPosition;
    }
    
    /// <summary>
    /// 生成敌人的随机位置
    /// </summary>
    /// <returns>敌人的随机位置</returns>
    private Vector3 GenerateRandomEnemyPosition()
    {
        // 使用SpawnPositionHelper在Scene地形上生成随机位置，避开yingdi区域
        return SpawnPositionHelper.GetRandomPositionOnSceneTerrain(30f, 50);
    }
    
    /// <summary>
    /// 标记资源加载完成
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    public void MarkResourceLoaded(string poolId)
    {
        if (!resourceLoadStatus.ContainsKey(poolId))
        {
            resourceLoadStatus[poolId] = true;
            Debug.Log($"AutoObjectPoolManager: 资源加载完成 - 池ID: {poolId}, 已加载: {resourceLoadStatus.Count}/{config?.poolItems?.Length}");
            
            // 检查是否所有资源都已加载完成
            CheckAllResourcesLoaded();
        }
    }
    
    /// <summary>
    /// 标记资源加载失败
    /// </summary>
    /// <param name="poolId">对象池ID</param>
    public void MarkResourceLoadFailed(string poolId)
    {
        if (!resourceLoadStatus.ContainsKey(poolId))
        {
            resourceLoadStatus[poolId] = false;
            Debug.LogError($"AutoObjectPoolManager: 资源加载失败 - 池ID: {poolId}");
            
            // 即使加载失败，也要检查是否所有资源都已处理（成功或失败）
            CheckAllResourcesLoaded();
        }
    }
    
    /// <summary>
    /// 检查所有资源是否都已加载完成
    /// </summary>
    private void CheckAllResourcesLoaded()
    {
        if (config == null || config.poolItems == null)
        {
            return;
        }
        
        // 检查是否所有配置的资源都已成功加载
        bool allLoaded = true;
        foreach (var item in config.poolItems)
        {
            // 只有当poolId存在且值为true（加载成功）时才算完成
            if (!resourceLoadStatus.ContainsKey(item.poolId) || !resourceLoadStatus[item.poolId])
            {
                allLoaded = false;
                break;
            }
        }
        
        if (allLoaded && !AllResourcesLoaded)
        {
            AllResourcesLoaded = true;
            Debug.Log($"AutoObjectPoolManager: 所有资源加载完成！已加载: {resourceLoadStatus.Count}/{config.poolItems.Length}");
            
            // 通知ResourceSpawner可以开始生成资源
            NotifyResourceSpawnerReady();
        }
    }
    
    /// <summary>
    /// 通知ResourceSpawner资源已准备好
    /// </summary>
    private void NotifyResourceSpawnerReady()
    {
        ResourceSpawner spawner = FindObjectOfType<ResourceSpawner>();
        if (spawner != null)
        {
            Debug.Log("AutoObjectPoolManager: 通知ResourceSpawner资源已准备好");
            spawner.OnResourcesReady();
        }
        else
        {
            Debug.LogWarning("AutoObjectPoolManager: 未找到ResourceSpawner");
        }
    }
}

/// <summary>
/// 自动化对象池
/// </summary>
public class AutoObjectPool
{
    /// <summary>对象池配置项</summary>
    private AutoObjectPoolConfigItem configItem;

    /// <summary>对象池管理器</summary>
    private AutoObjectPoolManager manager;

    /// <summary>可用对象队列</summary>
    private Queue<GameObject> availableObjects = new Queue<GameObject>();

    /// <summary>活跃对象列表</summary>
    private List<GameObject> activeObjects = new List<GameObject>();

    /// <summary>已加载的预制体</summary>
    private GameObject prefab;

    /// <summary>最后检查时间</summary>
    private float lastCheckTime = 0f;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configItem">配置项</param>
    /// <param name="manager">对象池管理器</param>
    public AutoObjectPool(AutoObjectPoolConfigItem configItem, AutoObjectPoolManager manager)
    {
        this.configItem = configItem;
        this.manager = manager;
        
        // 初始化对象池
        Initialize();
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void Initialize()
    {
        // 加载资源并初始化对象
        manager.LoadAsset(configItem.addressablePath, (loadedPrefab) =>
        {
            if (loadedPrefab == null)
            {
                Debug.LogError($"AutoObjectPool: 加载预制体失败: {configItem.addressablePath}");
                manager.MarkResourceLoadFailed(configItem.poolId);
                return;
            }

            prefab = loadedPrefab;
            
            // 通知管理器资源加载完成
            manager.MarkResourceLoaded(configItem.poolId);
            
            // 生成初始容量的对象并全部放入可用队列中（不激活）
            int initialCapacity = CalculateDynamicInitialCapacity();
            GenerateInitialObjects(initialCapacity);
        });
    }

    /// <summary>
    /// 计算动态初始容量
    /// </summary>
    /// <returns>动态计算的初始容量</returns>
    private int CalculateDynamicInitialCapacity()
    {
        // 使用配置的初始容量
        return configItem.initialCapacity;
    }

    /// <summary>
    /// 生成指定数量的对象
    /// </summary>
    /// <param name="count">对象数量</param>
    /// <param name="forceRegen">是否强制重新生成</param>
    private void GenerateObjects(int count, bool forceRegen = false)
    {
        if (prefab == null)
        {
            Debug.LogError($"AutoObjectPool: 预制体未加载，无法生成对象");
            return;
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                // 根据对象类型决定生成位置
                Vector3 spawnPosition;
                Quaternion spawnRotation;
                
                if (configItem.objectType == "Resource")
                {
                    // 对于资源对象，使用随机位置生成，避免在中心区域生成
                    spawnPosition = GenerateRandomResourcePosition();
                    // 随机Y轴旋转，使资源看起来方向不同
                    spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                }
                else
                {
                    // 对于非资源对象（如敌人），使用预制体的原始位置
                    spawnPosition = prefab.transform.position;
                    spawnRotation = prefab.transform.rotation;
                }
                
                GameObject obj = GameObject.Instantiate(prefab, spawnPosition, spawnRotation);
                obj.SetActive(true);
                // 将新生成的对象添加到活跃根对象下
                obj.transform.SetParent(manager.ActiveRoot.transform, true);
                activeObjects.Add(obj);
            }
            Debug.Log($"AutoObjectPool: 已生成 {count} 个对象并激活，池ID: {configItem.poolId}, 当前活跃对象数: {activeObjects.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoObjectPool: 生成对象时发生异常: {configItem.poolId}, 异常: {e.Message}");
        }
    }

    /// <summary>
    /// 生成初始对象并放入可用队列
    /// </summary>
    /// <param name="count">对象数量</param>
    private void GenerateInitialObjects(int count)
    {
        if (prefab == null)
        {
            Debug.LogError($"AutoObjectPool: 预制体未加载，无法生成初始对象");
            return;
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                GameObject obj;
                
                if (configItem.objectType == "Enemy")
                {
                    // 对于敌人类型，生成在原点并保持非激活状态
                    // 敌人将通过GetObject方法在网络服务器启动后被正确初始化和激活
                    Vector3 spawnPosition = Vector3.zero;
                    Quaternion spawnRotation = Quaternion.identity;
                    obj = GameObject.Instantiate(prefab, spawnPosition, spawnRotation);
                    obj.SetActive(false);
                    // 将初始对象添加到非活跃根对象下
                    obj.transform.SetParent(manager.InactiveRoot.transform, false);
                    // 添加到可用队列
                    availableObjects.Enqueue(obj);
                    Debug.Log($"AutoObjectPool: 已生成敌人对象并放入可用队列，池ID: {configItem.poolId}");
                }
                else if (configItem.objectType == "Resource")
                {
                    // 对于资源类型，生成在原点并保持非激活状态
                    // 资源将通过GetObject方法在网络服务器启动后被正确初始化和激活
                    Vector3 spawnPosition = Vector3.zero;
                    Quaternion spawnRotation = Quaternion.identity;
                    obj = GameObject.Instantiate(prefab, spawnPosition, spawnRotation);
                    obj.SetActive(false);
                    // 将初始对象添加到非活跃根对象下
                    obj.transform.SetParent(manager.InactiveRoot.transform, false);
                    // 添加到可用队列
                    availableObjects.Enqueue(obj);
                    Debug.Log($"AutoObjectPool: 已生成资源对象并放入可用队列，池ID: {configItem.poolId}");
                }
                else
                {
                    // 对于其他类型，生成在原点并保持非激活状态
                    Vector3 spawnPosition = Vector3.zero;
                    Quaternion spawnRotation = Quaternion.identity;
                    obj = GameObject.Instantiate(prefab, spawnPosition, spawnRotation);
                    obj.SetActive(false);
                    // 将初始对象添加到非活跃根对象下
                    obj.transform.SetParent(manager.InactiveRoot.transform, false);
                    // 添加到可用队列
                    availableObjects.Enqueue(obj);
                }
            }
            
            Debug.Log($"AutoObjectPool: 已生成 {count} 个初始对象并放入可用队列，池ID: {configItem.poolId}, 当前可用对象数: {availableObjects.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoObjectPool: 生成初始对象时发生异常: {configItem.poolId}, 异常: {e.Message}");
        }
    }

    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    /// <returns>获取到的游戏对象</returns>
    public GameObject GetObject()
    {
        Debug.Log($"AutoObjectPool: 尝试从池 {configItem.poolId} 获取对象，当前活跃对象数: {activeObjects.Count}，可用对象数: {availableObjects.Count}");
        
        // 优先从可用队列获取对象
        if (availableObjects.Count > 0)
        {
            GameObject obj = availableObjects.Dequeue();
            activeObjects.Add(obj);
            obj.SetActive(true);
            Debug.Log($"AutoObjectPool: 从池 {configItem.poolId} 获取已存在的对象，当前活跃对象数: {activeObjects.Count}");
            return obj;
        }
        
        // 如果没有可用对象且未达到最大容量限制，生成新对象
        if (activeObjects.Count < configItem.maxCapacity)
        {
            GenerateObjects(1);
            
            // 返回刚生成的对象
            if (activeObjects.Count > 0)
            {
                GameObject obj = activeObjects[activeObjects.Count - 1];
                Debug.Log($"AutoObjectPool: 从池 {configItem.poolId} 成功获取新对象，当前活跃对象数: {activeObjects.Count}");
                return obj;
            }
        }
        
        Debug.LogWarning($"AutoObjectPool: 对象池 {configItem.poolId} 已达到最大容量或无法获取对象");
        return null;
    }

    /// <summary>回收对象计数</summary>
    private int recycledCount = 0;
    
    /// <summary>
    /// 将对象归还到对象池
    /// </summary>
    /// <param name="obj">要归还的游戏对象</param>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning($"AutoObjectPool: 归还的对象为null");
            return;
        }

        // 检查对象是否属于当前对象池
        if (!activeObjects.Contains(obj))
        {
            Debug.LogWarning($"AutoObjectPool: 对象不属于当前对象池: {configItem.poolId}");
            GameObject.Destroy(obj);
            return;
        }

        try
        {
            // 在处理对象之前，调用对象池状态重置方法
            EnemyHealthManager healthManager = obj.GetComponent<EnemyHealthManager>();
            if (healthManager != null)
            {
                healthManager.ResetPoolState();
            }
            
            EnemyAIController aiController = obj.GetComponent<EnemyAIController>();
            if (aiController != null)
            {
                aiController.ResetPoolState();
            }
            
            // 从活跃列表移除
            activeObjects.Remove(obj);
            
            // 重置对象状态
            obj.SetActive(false);
            obj.transform.SetParent(manager.InactiveRoot.transform, false); // 移动到非活跃根对象下
            obj.transform.position = Vector3.zero; // 重置位置
            obj.transform.rotation = Quaternion.identity; // 重置旋转
            
            // 添加到可用队列
            availableObjects.Enqueue(obj);
            
            // 增加回收计数
            recycledCount++;
            
            Debug.Log($"AutoObjectPool: 对象已归还到池，池ID: {configItem.poolId}，当前活跃对象数: {activeObjects.Count}，可用对象数: {availableObjects.Count}，回收计数: {recycledCount}");
            
            // 检查回收计数是否超过阈值
            if (recycledCount >= configItem.updateThreshold)
            {
                Debug.Log($"AutoObjectPool: 回收计数超过阈值 {configItem.updateThreshold}，重新生成所有对象，池ID: {configItem.poolId}");
                
                // 重置回收计数
                recycledCount = 0;
                
                // 重新生成所有对象
                RegenerateAllObjects();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoObjectPool: 归还对象时发生异常: {configItem.poolId}, 异常: {e.Message}");
            // 如果出现异常，仍然尝试销毁对象
            if (activeObjects.Contains(obj))
            {
                activeObjects.Remove(obj);
            }
            GameObject.Destroy(obj);
        }
    }
    
    /// <summary>
    /// 重新生成所有对象
    /// </summary>
    private void RegenerateAllObjects()
    {
        // 清空当前所有对象
        foreach (GameObject obj in availableObjects)
        {
            GameObject.Destroy(obj);
        }
        availableObjects.Clear();

        foreach (GameObject obj in activeObjects)
        {
            // 如果有活跃对象，也要销毁它们
            GameObject.Destroy(obj);
        }
        activeObjects.Clear();
        
        // 重新生成初始容量的对象并放入可用队列
        GenerateInitialObjects(configItem.initialCapacity);
        
        Debug.Log($"AutoObjectPool: 已重新生成所有对象，池ID: {configItem.poolId}, 当前可用对象数: {availableObjects.Count}");
    }

    /// <summary>
    /// 检查容量并自动调整（简化版，仅用于兼容性）
    /// </summary>
    public void CheckCapacity()
    {
        // 根据新需求，不再需要定期检查和调整容量
        // 保留此方法仅用于兼容性
    }

    /// <summary>
    /// 如果空闲对象过多，则缩减对象池大小（简化版，仅用于兼容性）
    /// </summary>
    private void ShrinkIfNecessary()
    {
        // 根据新需求，不再需要缩减对象池大小
        // 保留此方法仅用于兼容性
    }

    /// <summary>
    /// 获取对象池统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public string GetStatistics()
    {
        int currentTotal = activeObjects.Count; // 不再有可用对象队列
        return $"对象池 {configItem.poolId}: 活跃={activeObjects.Count}, 总数={currentTotal}, 初始容量={configItem.initialCapacity}, 更新阈值={configItem.updateThreshold}";
    }

    /// <summary>
    /// 获取可用对象数量
    /// </summary>
    /// <returns>可用对象数量</returns>
    public int GetAvailableCount()
    {
        return availableObjects.Count;
    }
    
    /// <summary>
    /// 获取活跃对象数量
    /// </summary>
    /// <returns>活跃对象数量</returns>
    public int GetActiveCount()
    {
        return activeObjects.Count;
    }
    
    /// <summary>
    /// 获取最大容量
    /// </summary>
    /// <returns>最大容量</returns>
    public int GetMaxCapacity()
    {
        return configItem.maxCapacity;
    }
    
    /// <summary>
    /// 清理对象池
    /// </summary>
    public void Clear()
    {
        // 销毁所有可用对象
        foreach (GameObject obj in availableObjects)
        {
            GameObject.Destroy(obj);
        }
        availableObjects.Clear();

        // 销毁所有活跃对象
        foreach (GameObject obj in activeObjects)
        {
            GameObject.Destroy(obj);
        }
        activeObjects.Clear();

        Debug.Log($"AutoObjectPool: 已清理对象池: {configItem.poolId}");
    }
    
    /// <summary>
    /// 生成随机资源位置，用于资源对象的随机分布
    /// </summary>
    /// <returns>随机生成的位置</returns>
    private Vector3 GenerateRandomResourcePosition()
    {
        // 定义场景范围，避免在中心区域（0,0,0附近）生成资源
        // 假设场景范围为-50到50之间，但避开中心±10的区域
        float x, z;
        
        // 随机选择在正负区间生成
        if (Random.value > 0.5f)
        {
            // 在正区间生成 (10 到 50)
            x = Random.Range(10f, 50f);
        }
        else
        {
            // 在负区间生成 (-50 到 -10)
            x = Random.Range(-50f, -10f);
        }
        
        if (Random.value > 0.5f)
        {
            // 在正区间生成 (10 到 50)
            z = Random.Range(10f, 50f);
        }
        else
        {
            // 在负区间生成 (-50 到 -10)
            z = Random.Range(-50f, -10f);
        }
        
        // Y轴使用0作为基础高度，可以根据需要调整
        float y = 0f;
        
        Vector3 randomPosition = new Vector3(x, y, z);
        
        return randomPosition;
    }
}