using Mirror;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 对象池管理器适配器（兼容旧系统，内部使用新的自动化对象池系统）
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class PoolEntry
    {
        public string key;
        public GameObject prefab;
        public int initialSize;
    }

    public List<PoolEntry> entries = new List<PoolEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 确保自动化对象池管理器存在
        EnsureAutoPoolManagerExists();
    }

    void Start()
    {
        if (NetworkServer.active)
        {
            // 旧系统初始化不再需要，由新系统自动处理
            Debug.Log("ObjectPoolManager: 旧系统初始化已废弃，由AutoObjectPoolManager自动处理");
        }
    }

    /// <summary>
    /// 确保自动化对象池管理器存在
    /// </summary>
    private void EnsureAutoPoolManagerExists()
    {
        if (AutoObjectPoolManager.Instance == null)
        {
            GameObject poolManagerObj = new GameObject("AutoObjectPoolManager");
            poolManagerObj.AddComponent<AutoObjectPoolManager>();
            poolManagerObj.AddComponent<AutoPoolConfigProvider>();
            Debug.Log("ObjectPoolManager: 已创建AutoObjectPoolManager实例");
        }
    }

    /// <summary>
    /// 从对象池获取对象（适配方法，内部使用新系统）
    /// </summary>
    [Server]
    public GameObject GetFromPool(string key, Vector3 position, Quaternion rotation)
    {
        // 使用新的自动化对象池系统获取对象
        return AutoObjectPoolManager.Instance.GetObject(key, position, rotation);
    }

    /// <summary>
    /// 将对象归还到对象池（适配方法，内部使用新系统）
    /// </summary>
    [Server]
    public void ReturnToPool(string key, GameObject obj)
    {
        // 使用新的自动化对象池系统归还对象
        AutoObjectPoolManager.Instance.ReturnObject(key, obj);
    }
}