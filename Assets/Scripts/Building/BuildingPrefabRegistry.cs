using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuildingPrefabEntry
{
    public string buildingId;
    public GameObject prefab;
}

public class BuildingPrefabRegistry : MonoBehaviour
{
    public static BuildingPrefabRegistry Instance { get; private set; }

    public List<BuildingPrefabEntry> entries = new List<BuildingPrefabEntry>();

    readonly Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        map.Clear();
        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.buildingId) && e.prefab != null)
            {
                map[e.buildingId] = e.prefab;
            }
        }
    }

    public GameObject GetPrefab(string buildingId)
    {
        GameObject prefab;
        map.TryGetValue(buildingId, out prefab);
        return prefab;
    }
}
