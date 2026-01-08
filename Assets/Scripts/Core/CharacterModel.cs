using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 角色数据模型 - 纯数据类，不继承MonoBehaviour
/// </summary>
[System.Serializable]
public class CharacterDataModel
{
    public string id;
    public string name;
    public string description;
    public int health;
    public float speed;
    public int attack;
    public string addressableKey;
    public string idleAnimation;
    public string specialAbility;
    public float specialValue;
}

/// <summary>
/// 角色数据集合
/// </summary>
[System.Serializable]
public class CharacterDataCollection
{
    public List<CharacterDataModel> CharacterDatas;
}

/// <summary>
/// 角色模型管理器 - 负责角色数据的加载和管理
/// </summary>
public class CharacterModel : MVCModel<CharacterModel>
{
    private readonly Dictionary<string, CharacterDataModel> characters = new Dictionary<string, CharacterDataModel>();

    void Awake()
    {
        InitializeSingleton();
        Load();
    }

    void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "CharacterData.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("[CharacterModel] 角色数据文件不存在: " + path);
            return;
        }
        
        string json = File.ReadAllText(path);
        CharacterDataCollection collection = JsonUtility.FromJson<CharacterDataCollection>(json);
        
        if (collection != null && collection.CharacterDatas != null)
        {
            foreach (var data in collection.CharacterDatas)
            {
                if (!string.IsNullOrEmpty(data.id))
                {
                    characters[data.id] = data;
                }
            }
            Debug.Log($"[CharacterModel] 加载了 {characters.Count} 个角色数据");
        }
    }

    public CharacterDataModel GetCharacter(string id)
    {
        return characters.ContainsKey(id) ? characters[id] : null;
    }
    
    public bool ContainsCharacter(string id)
    {
        return characters.ContainsKey(id);
    }
    
    public List<string> GetAllCharacterIds()
    {
        List<string> ids = new List<string>();
        foreach (var kvp in characters)
        {
            ids.Add(kvp.Key);
        }
        return ids;
    }
}