using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

[System.Serializable]
public class BuildingResourceCost
{
    public string resourceId;
    public int amount;
}

[System.Serializable]
public class BuildingData
{
    public string buildingId;
    public string name;
    public string description;
    public int width;
    public int height;
    public List<BuildingResourceCost> resourceCostList;
    public string addressableKey;
    public string imageAddressableKey;
}

[System.Serializable]
public class BuildingDataContainer
{
    public List<BuildingData> BuildingDatas;
}

public class BuildingDataManager : MonoBehaviour
{
    public static BuildingDataManager Instance { get; private set; }
    
    private Dictionary<string, BuildingData> buildingDataMap = new Dictionary<string, BuildingData>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadBuildingData();
    }
    
    // 加载建筑配置数据
    void LoadBuildingData()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, "BuildingData.json");
        
        if (File.Exists(jsonPath))
        {
            string jsonContent = File.ReadAllText(jsonPath);
            BuildingDataContainer container = JsonConvert.DeserializeObject<BuildingDataContainer>(jsonContent);
            
            buildingDataMap.Clear();
            foreach (BuildingData data in container.BuildingDatas)
            {
                buildingDataMap[data.buildingId] = data;
            }
            
        }
        else
        {
        }
    }
    
    // 获取所有建筑数据
    public List<BuildingData> GetAllBuildingData()
    {
        return new List<BuildingData>(buildingDataMap.Values);
    }
    
    // 根据ID获取建筑数据
    public BuildingData GetBuildingData(string buildingId)
    {
        BuildingData data;
        buildingDataMap.TryGetValue(buildingId, out data);
        return data;
    }
    
    // 检查建筑是否存在
    public bool HasBuildingData(string buildingId)
    {
        return buildingDataMap.ContainsKey(buildingId);
    }
}