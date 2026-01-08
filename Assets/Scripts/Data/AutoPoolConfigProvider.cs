using UnityEngine;
using System.IO;

/// <summary>
/// 自动化对象池配置提供者
/// </summary>
public class AutoPoolConfigProvider : MonoBehaviour
{
    public static AutoPoolConfigProvider Instance { get; private set; }

    public AutoObjectPoolConfig Config { get; private set; }

    /// <summary>配置文件路径</summary>
    private string configPath = Path.Combine(Application.streamingAssetsPath, "AutoObjectPoolConfig.json");

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
        
        // 加载配置
        LoadConfig();
        
        // 如果配置不存在，创建默认配置
        if (Config == null)
        {
            CreateDefaultConfig();
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(configPath))
            {
                Debug.LogWarning($"AutoPoolConfigProvider: 配置文件不存在，将创建默认配置: {configPath}");
                return;
            }

            string json = File.ReadAllText(configPath);
            AutoObjectPoolConfigWrapper wrapper = JsonUtility.FromJson<AutoObjectPoolConfigWrapper>(json);
            if (wrapper != null && wrapper.config != null)
            {
                Config = wrapper.config;
                Debug.Log($"AutoPoolConfigProvider: 成功加载配置，版本: {Config.version}，对象池数量: {Config.poolItems.Length}");
            }
            else
            {
                Debug.LogError("AutoPoolConfigProvider: 配置文件格式错误");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoPoolConfigProvider: 加载配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 创建默认配置
    /// </summary>
    private void CreateDefaultConfig()
    {
        try
        {
            // 创建默认配置
            AutoObjectPoolConfig defaultConfig = new AutoObjectPoolConfig();
            defaultConfig.version = "1.0";
            
            // 添加默认对象池配置项
            defaultConfig.poolItems = new AutoObjectPoolConfigItem[]
            {
                new AutoObjectPoolConfigItem()
                {
                    poolId = "SmallEnemy",
                    objectType = "Enemy",
                    addressablePath = "SmallEnemy",
                    initialCapacity = 10,
                    updateThreshold = 5
                },
                new AutoObjectPoolConfigItem()
                {
                    poolId = "BigEnemy",
                    objectType = "Enemy",
                    addressablePath = "BigEnemy",
                    initialCapacity = 5,
                    updateThreshold = 3
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
                },
                new AutoObjectPoolConfigItem()
                {
                    poolId = "Apple",
                    objectType = "Resource",
                    addressablePath = "Apple",
                    initialCapacity = 15,
                    updateThreshold = 10
                },
                new AutoObjectPoolConfigItem()
                {
                    poolId = "Pear",
                    objectType = "Resource",
                    addressablePath = "Pear",
                    initialCapacity = 15,
                    updateThreshold = 10
                }
            };
            
            // 保存配置
            SaveConfig(defaultConfig);
            
            Debug.Log("AutoPoolConfigProvider: 已创建默认配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoPoolConfigProvider: 创建默认配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    /// <param name="config">要保存的配置</param>
    public void SaveConfig(AutoObjectPoolConfig config)
    {
        try
        {
            // 创建配置包装器
            AutoObjectPoolConfigWrapper wrapper = new AutoObjectPoolConfigWrapper();
            wrapper.config = config;
            
            // 序列化到JSON
            string json = JsonUtility.ToJson(wrapper, true);
            
            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            
            // 保存到文件
            File.WriteAllText(configPath, json);
            
            // 更新当前配置
            Config = config;
            
            Debug.Log($"AutoPoolConfigProvider: 配置已保存到: {configPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AutoPoolConfigProvider: 保存配置失败: {e.Message}");
        }
    }
}