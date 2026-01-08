using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class CharacterData
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

[System.Serializable]
class CharacterDatabaseDataCollection
{
    public List<CharacterData> CharacterDatas;
}

public class CharacterDatabase : MonoBehaviour
{
    public static CharacterDatabase Instance { get; private set; }

    private readonly Dictionary<string, CharacterData> characters = new Dictionary<string, CharacterData>();

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
        string path = Path.Combine(Application.streamingAssetsPath, "CharacterData.json");
        if (!File.Exists(path))
            return;
        string json = File.ReadAllText(path);
        CharacterDatabaseDataCollection collection = JsonUtility.FromJson<CharacterDatabaseDataCollection>(json);
        if (collection != null && collection.CharacterDatas != null)
        {
            foreach (var data in collection.CharacterDatas)
            {
                if (!string.IsNullOrEmpty(data.id))
                {
                    characters[data.id] = data;
                }
            }
        }
    }

    public CharacterData GetCharacter(string id)
    {
        CharacterData data;
        characters.TryGetValue(id, out data);
        return data;
    }

    public IEnumerable<CharacterData> GetAllCharacters()
    {
        return characters.Values;
    }
}
