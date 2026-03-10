using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 武器数据结构
/// </summary>
[System.Serializable]
public class WeaponData
{
    public int weaponId;
    public string resourceId;  // 背包资源ID，用于关联ResourceDatabase
    public string weaponName;
    public string weaponType;
    [System.Obsolete("使用GetModelKey()或modelAddressableKeys数组代替")]
    public string modelAddressableKey;
    public string[] modelAddressableKeys;  // 支持多模型（如双手武器左右手不同模型）
    public string spriteAddressableKey;
    public string[] boneNames;
    public int damage;
    public int comboStages;
    [System.Obsolete("使用代码控制的连击窗口机制，此字段不再使用")]
    public float comboWindowStart;
    [System.Obsolete("使用代码控制的连击窗口机制，此字段不再使用")]
    public float comboWindowEnd;
    public float attackRange = 2.0f;        // 攻击范围（扇形半径）
    public float attackAngle = 90f;         // 攻击角度（扇形角度）
    public string[] attackAnimationNames;
    public float[] comboDamageMultipliers;  // 每段攻击的伤害倍率
    public string description;
    
    /// <summary>
    /// 获取指定连击段的伤害倍率
    /// </summary>
    public float GetComboDamageMultiplier(int comboStage)
    {
        if (comboDamageMultipliers == null || comboDamageMultipliers.Length == 0)
            return 1.0f; // 默认倍率
        
        int index = Mathf.Clamp(comboStage - 1, 0, comboDamageMultipliers.Length - 1);
        return comboDamageMultipliers[index];
    }

    /// <summary>
    /// 获取指定索引的模型地址
    /// </summary>
    public string GetModelKey(int index = 0)
    {
        // 优先使用新的数组配置
        if (modelAddressableKeys != null && modelAddressableKeys.Length > index)
        {
            return modelAddressableKeys[index];
        }
        // 兼容旧配置
        if (!string.IsNullOrEmpty(modelAddressableKey))
        {
            return modelAddressableKey;
        }
        return null;
    }

    /// <summary>
    /// 获取所有模型地址
    /// </summary>
    public string[] GetAllModelKeys()
    {
        if (modelAddressableKeys != null && modelAddressableKeys.Length > 0)
        {
            return modelAddressableKeys;
        }
        if (!string.IsNullOrEmpty(modelAddressableKey))
        {
            return new string[] { modelAddressableKey };
        }
        return new string[0];
    }

    /// <summary>
    /// 是否为双手武器（需要多个模型）
    public bool IsDualWield => modelAddressableKeys != null && modelAddressableKeys.Length > 1;
}

/// <summary>
/// 武器数据集合
/// </summary>
[System.Serializable]
class WeaponDataCollection
{
    public List<WeaponData> Weapons;
}

/// <summary>
/// 武器数据库 - 管理所有武器配置数据
/// </summary>
public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }

    private Dictionary<int, WeaponData> weaponDict = new Dictionary<int, WeaponData>();
    private bool isInitialized = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadWeaponData();
    }

    /// <summary>
    /// 加载武器数据
    /// </summary>
    void LoadWeaponData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "WeaponData.json");
        Debug.Log($"[WeaponDatabase] 开始加载武器数据，路径: {path}");

        if (!File.Exists(path))
        {
            Debug.LogError($"[WeaponDatabase] 武器数据文件不存在: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        Debug.Log($"[WeaponDatabase] JSON内容长度: {json.Length}");
        
        WeaponDataCollection collection = JsonUtility.FromJson<WeaponDataCollection>(json);
        Debug.Log($"[WeaponDatabase] 解析结果: collection={(collection != null ? "有" : "null")}, Weapons={(collection != null && collection.Weapons != null ? $"有{collection.Weapons.Count}个" : "null")}");

        if (collection == null || collection.Weapons == null)
        {
            Debug.LogError("[WeaponDatabase] 武器数据解析失败");
            return;
        }

        foreach (var weapon in collection.Weapons)
        {
            weaponDict[weapon.weaponId] = weapon;
            Debug.Log($"[WeaponDatabase] 加载武器: {weapon.weaponName} (ID: {weapon.weaponId}, resourceId: {weapon.resourceId})");
        }

        isInitialized = true;
        Debug.Log($"[WeaponDatabase] 共加载 {weaponDict.Count} 把武器");
    }

    /// <summary>
    /// 根据武器ID获取武器数据
    /// </summary>
    public WeaponData GetWeapon(int weaponId)
    {
        if (weaponDict.TryGetValue(weaponId, out WeaponData weapon))
        {
            return weapon;
        }
        Debug.LogWarning($"[WeaponDatabase] 未找到武器ID: {weaponId}");
        return null;
    }

    /// <summary>
    /// 根据武器名称获取武器数据
    /// </summary>
    public WeaponData GetWeaponByName(string weaponName)
    {
        foreach (var weapon in weaponDict.Values)
        {
            if (weapon.weaponName == weaponName)
            {
                return weapon;
            }
        }
        Debug.LogWarning($"[WeaponDatabase] 未找到武器名称: {weaponName}");
        return null;
    }

    /// <summary>
    /// 获取所有武器数据
    /// </summary>
    public List<WeaponData> GetAllWeapons()
    {
        return new List<WeaponData>(weaponDict.Values);
    }

    /// <summary>
    /// 根据资源ID获取武器数据
    /// </summary>
    public WeaponData GetWeaponByResourceId(string resourceId)
    {
        if (string.IsNullOrEmpty(resourceId))
        {
            Debug.LogWarning("[WeaponDatabase] GetWeaponByResourceId: resourceId为空");
            return null;
        }

        Debug.Log($"[WeaponDatabase] GetWeaponByResourceId({resourceId}) - 武器数量: {weaponDict.Count}");
        
        foreach (var weapon in weaponDict.Values)
        {
            Debug.Log($"[WeaponDatabase] 检查武器: {weapon.weaponName}, resourceId={weapon.resourceId}, modelKeys={string.Join(",", weapon.GetAllModelKeys())}");
            
            // 优先匹配resourceId字段
            if (!string.IsNullOrEmpty(weapon.resourceId) && weapon.resourceId == resourceId)
            {
                Debug.Log($"[WeaponDatabase] 找到匹配: {weapon.weaponName} (通过resourceId)");
                return weapon;
            }
            // 如果没有resourceId，尝试匹配模型地址的第一个key
            string[] modelKeys = weapon.GetAllModelKeys();
            foreach (var key in modelKeys)
            {
                if (key == resourceId)
                {
                    Debug.Log($"[WeaponDatabase] 找到匹配: {weapon.weaponName} (通过modelKey)");
                    return weapon;
                }
            }
        }
        
        Debug.LogWarning($"[WeaponDatabase] 未找到resourceId为{resourceId}的武器");
        return null;
    }

    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;
}
