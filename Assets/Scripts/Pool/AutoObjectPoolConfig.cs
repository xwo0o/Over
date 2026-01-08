using UnityEngine;

/// <summary>
/// 自动化对象池配置项
/// </summary>
[System.Serializable]
public class AutoObjectPoolConfigItem
{
    /// <summary>对象池ID</summary>
    public string poolId;
    
    /// <summary>对象类型</summary>
    public string objectType;
    
    /// <summary>Addressable资源路径</summary>
    public string addressablePath;
    
    /// <summary>对象池初始容量大小</summary>
    public int initialCapacity;
    
    /// <summary>更新对象池阈值</summary>
    public int updateThreshold;
    
    /// <summary>对象池最大容量</summary>
    public int maxCapacity = 50; // 默认最大容量为50
}

/// <summary>
/// 自动化对象池配置
/// </summary>
[System.Serializable]
public class AutoObjectPoolConfig
{
    /// <summary>配置版本</summary>
    public string version;
    
    /// <summary>对象池配置项列表</summary>
    public AutoObjectPoolConfigItem[] poolItems;
}

/// <summary>
/// 自动化对象池配置包装器（用于JSON序列化）
/// </summary>
[System.Serializable]
public class AutoObjectPoolConfigWrapper
{
    public AutoObjectPoolConfig config;
}