using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class ResourceData
{
    public string resourceId;
    public string name;
    public string description;
    public string type;
    public int maxStack;
    public string addressableKey;
    public string spriteAddressableKey;
    public int healthRestore;
}

[System.Serializable]
class ResourceDataCollection
{
    public List<ResourceData> ResourceDatas;
}

public class ResourceDatabase : MonoBehaviour
{
    public static ResourceDatabase Instance { get; private set; }

    private readonly Dictionary<string, ResourceData> resources = new Dictionary<string, ResourceData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "ResourceData.json");
        
        if (!File.Exists(path))
        {
            return;
        }
        
        string json = File.ReadAllText(path);
        
        ResourceDataCollection collection = JsonUtility.FromJson<ResourceDataCollection>(json);
        if (collection == null)
        {
            return;
        }
        
        if (collection.ResourceDatas == null)
        {
            return;
        }
        
        foreach (var data in collection.ResourceDatas)
        {
            if (!string.IsNullOrEmpty(data.resourceId))
            {
                resources[data.resourceId] = data;
            }
            else
            {
            }
        }
        
        // 通知所有UI更新资源消耗文本
        UpdateAllResourceCostTexts();
    }
    
    // 通知所有UI更新资源消耗文本
    private void UpdateAllResourceCostTexts()
    {
        BuildingSlotUI.UpdateAllResourceCostTexts();
        
        // 注意：BuildingUIController中的consumptionText无法更新，因为没有保存与BuildingData的关联
        // 推荐使用BuildingSlotUI组件来管理UI元素
    }

    public ResourceData GetResource(string id)
    {
        ResourceData data;
        resources.TryGetValue(id, out data);
        
        if (data == null)
        {
        }
        else
        {
        }
        
        return data;
    }
}
