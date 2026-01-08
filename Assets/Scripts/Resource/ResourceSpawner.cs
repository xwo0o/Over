using Mirror;
using UnityEngine;
using Core;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class ResourceSpawner : NetworkBehaviour
{
    public string woodKey = "Wood";
    public string stoneKey = "Stone";
    public string appleKey = "Apple";
    public string pearKey = "Pear";

    /// <summary>动态资源类型列表</summary>
    private List<string> dynamicResourceTypes = new List<string>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 从ResourceDatabase动态加载资源类型
        LoadDynamicResourceTypes();
        // 启动延迟生成协程，等待资源加载完成
        StartCoroutine(DelayedInitialSpawn());
    }
    
    /// <summary>
    /// 当AutoObjectPoolManager通知资源已准备好时调用
    /// </summary>
    public void OnResourcesReady()
    {
        Debug.Log("ResourceSpawner: 收到资源准备就绪通知，开始生成初始资源");
        StartCoroutine(DelayedInitialSpawn());
    }
    
    System.Collections.IEnumerator DelayedInitialSpawn()
    {
        // 等待对象池管理器初始化
        while (AutoObjectPoolManager.Instance == null)
        {
            Debug.Log("ResourceSpawner: 等待AutoObjectPoolManager初始化...");
            yield return new WaitForSeconds(0.5f);
        }
        
        // 等待所有资源加载完成，设置超时时间
        float maxWaitTime = 30f; // 最多等待30秒
        float elapsedTime = 0f;
        
        while (!AutoObjectPoolManager.Instance.AllResourcesLoaded && elapsedTime < maxWaitTime)
        {
            Debug.Log($"ResourceSpawner: 等待资源加载完成... ({elapsedTime:F1}s/{maxWaitTime}s)");
            yield return new WaitForSeconds(0.5f);
            elapsedTime += 0.5f;
        }
        
        if (!AutoObjectPoolManager.Instance.AllResourcesLoaded)
        {
            Debug.LogError("ResourceSpawner: 等待资源加载超时，部分资源可能加载失败，将尝试生成可用资源");
        }
        else
        {
            Debug.Log("ResourceSpawner: 所有资源已加载完成，开始生成初始资源");
        }
        
        InitialSpawn();
    }

    /// <summary>
    /// 从ResourceDatabase动态加载资源类型
    /// </summary>
    private void LoadDynamicResourceTypes()
    {
        dynamicResourceTypes.Clear();
        
        // 等待ResourceDatabase初始化完成
        int maxAttempts = 10;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            if (ResourceDatabase.Instance != null)
            {
                break;
            }
            attempts++;
            new WaitForSeconds(0.5f);
        }
        
        if (ResourceDatabase.Instance == null)
        {
            Debug.LogWarning("ResourceSpawner: ResourceDatabase未初始化，使用硬编码资源类型");
            dynamicResourceTypes.Add(woodKey);
            dynamicResourceTypes.Add(stoneKey);
            dynamicResourceTypes.Add(appleKey);
            dynamicResourceTypes.Add(pearKey);
            return;
        }

        // 通过反射获取ResourceDatabase的私有字段resources
        var resourcesField = typeof(ResourceDatabase).GetField("resources", BindingFlags.NonPublic | BindingFlags.Instance);
        if (resourcesField != null)
        {
            var resources = resourcesField.GetValue(ResourceDatabase.Instance) as Dictionary<string, ResourceData>;
            if (resources != null)
            {
                foreach (var kvp in resources)
                {
                    dynamicResourceTypes.Add(kvp.Key);
                    Debug.Log($"ResourceSpawner: 动态加载资源类型 - ID: {kvp.Key}, 名称: {kvp.Value.name}");
                }
                
                Debug.Log($"ResourceSpawner: 已从ResourceDatabase加载 {dynamicResourceTypes.Count} 个资源类型");
            }
            else
            {
                Debug.LogWarning("ResourceSpawner: 无法获取ResourceDatabase的resources字段，使用硬编码资源类型");
                dynamicResourceTypes.Add(woodKey);
                dynamicResourceTypes.Add(stoneKey);
                dynamicResourceTypes.Add(appleKey);
                dynamicResourceTypes.Add(pearKey);
            }
        }
        else
        {
            Debug.LogWarning("ResourceSpawner: 无法找到ResourceDatabase的resources字段，使用硬编码资源类型");
            dynamicResourceTypes.Add(woodKey);
            dynamicResourceTypes.Add(stoneKey);
            dynamicResourceTypes.Add(appleKey);
            dynamicResourceTypes.Add(pearKey);
        }
    }

    [Server]
    void InitialSpawn()
    {
        // Debug.Log("ResourceSpawner: 开始初始生成资源");
        
        if (PoolConfigProvider.Instance == null)
        {
            Debug.LogError("ResourceSpawner: PoolConfigProvider.Instance 为空");
            return;
        }
        
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("ResourceSpawner: ObjectPoolManager.Instance 为空");
            return;
        }
        
        if (AutoObjectPoolManager.Instance == null)
        {
            Debug.LogError("ResourceSpawner: AutoObjectPoolManager.Instance 为空");
            return;
        }
        
        PoolConfig config = PoolConfigProvider.Instance.Config;
        if (config == null)
        {
            Debug.LogError("ResourceSpawner: PoolConfig 为空");
            return;
        }

        // Debug.Log($"ResourceSpawner: 开始生成 {config.initialResourcePoolSize} 个初始资源");
        
        // 在生成前检查对象池状态
        // Debug.Log($"ResourceSpawner: 检查Wood池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Wood")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Wood")}");
        // Debug.Log($"ResourceSpawner: 检查Stone池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Stone")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Stone")}");
        // Debug.Log($"ResourceSpawner: 检查Apple池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Apple")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Apple")}");
        // Debug.Log($"ResourceSpawner: 检查Pear池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Pear")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Pear")}");
        
        for (int i = 0; i < config.initialResourcePoolSize; i++)
        {
            SpawnRandomResource();
        }
        
        // 在生成后检查对象池状态
        // Debug.Log($"ResourceSpawner: 生成完成后检查Wood池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Wood")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Wood")}");
        // Debug.Log($"ResourceSpawner: 生成完成后检查Stone池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Stone")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Stone")}");
        // Debug.Log($"ResourceSpawner: 生成完成后检查Apple池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Apple")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Apple")}");
        // Debug.Log($"ResourceSpawner: 生成完成后检查Pear池状态 - 可用对象数: {AutoObjectPoolManager.Instance.GetAvailableCount("Pear")}, 活跃对象数: {AutoObjectPoolManager.Instance.GetActiveCount("Pear")}");
        
        // Debug.Log("ResourceSpawner: 初始生成完成");
    }

    [Server]
    void SpawnRandomResource()
    {
        PoolConfig config = PoolConfigProvider.Instance.Config;
        if (config == null)
        {
            Debug.LogError("SpawnRandomResource: PoolConfig 为空");
            return;
        }
        
        Vector3 pos = SpawnPositionHelper.GetRandomPositionOnSceneTerrain(config.campAvoidanceDistance, 20);
        
        if (pos == Vector3.zero)
        {
            Debug.LogWarning("SpawnRandomResource: 未找到有效的生成位置，跳过本次生成");
            return;
        }
        
        // 使用动态资源类型列表
        if (dynamicResourceTypes.Count == 0)
        {
            Debug.LogWarning("SpawnRandomResource: 动态资源类型列表为空，使用硬编码资源类型");
            dynamicResourceTypes.Add(woodKey);
            dynamicResourceTypes.Add(stoneKey);
            dynamicResourceTypes.Add(appleKey);
            dynamicResourceTypes.Add(pearKey);
        }
        
        // 随机选择一个资源类型
        int randomIndex = Random.Range(0, dynamicResourceTypes.Count);
        string key = dynamicResourceTypes[randomIndex];
        
        GameObject resource = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);
        if (resource != null)
        {
            ResourceNode resourceNode = resource.GetComponent<ResourceNode>();
            if (resourceNode != null)
            {
                resourceNode.resourceId = key;
            }
            else
            {
                Debug.LogError($"ResourceSpawner: 资源对象上没有ResourceNode组件，位置: {pos}, 类型: {key}");
            }
        }
        else
        {
            Debug.LogError($"ResourceSpawner: 生成资源失败，位置: {pos}, 类型: {key}");
        }
    }
}
