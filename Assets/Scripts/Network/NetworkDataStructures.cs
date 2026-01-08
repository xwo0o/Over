using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkCore
{
    public enum NetworkRequestType
{
    Unknown,
    CharacterSelection,
    PlayerMovement,
    PlayerAttack,
    PlayerAction,
    GameStateChange,
    PlayerJoin,
    PlayerLeave,
    ChatMessage,
    ResourceCollection,
    BuildingPlacement,
    GameStateSync,
    PlayerStateSync,
    Custom
}

public enum NetworkResponseCode
{
    Unknown,
    Success,
    Failed,
    NotFound,
    InvalidData,
    Unauthorized,
    ServerError,
    Timeout,
    Custom
}

[Serializable]
public class NetworkRequest
{
    public string requestId;
    public NetworkRequestType requestType;
    public string senderId;
    public float timestamp;
    public string jsonData;

    public NetworkRequest()
    {
        requestId = Guid.NewGuid().ToString();
        timestamp = Time.time;
        jsonData = "{}";
    }

    public NetworkRequest(NetworkRequestType type) : this()
    {
        requestType = type;
    }

    public void SetData(string key, object value)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict == null)
        {
            dataDict = new DataDictionary();
        }
        dataDict.SetData(key, value);
        jsonData = JsonUtility.ToJson(dataDict);
    }

    public T GetData<T>(string key, T defaultValue = default(T))
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict != null && dataDict.ContainsKey(key))
        {
            return dataDict.GetData<T>(key, defaultValue);
        }
        return defaultValue;
    }

    public bool TryGetData<T>(string key, out T value)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict != null && dataDict.ContainsKey(key))
        {
            value = dataDict.GetData<T>(key, default(T));
            return true;
        }
        value = default(T);
        return false;
    }

    public bool ContainsKey(string key)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        return dataDict != null && dataDict.ContainsKey(key);
    }
}

[Serializable]
public class NetworkResponse
{
    public string requestId;
    public NetworkResponseCode responseCode;
    public string message;
    public string jsonData;
    public float timestamp;

    public NetworkResponse()
    {
        this.requestId = "";
        this.responseCode = NetworkResponseCode.Success;
        this.message = "";
        this.timestamp = Time.time;
        this.jsonData = "{}";
    }

    public NetworkResponse(string requestId, NetworkResponseCode code, string message = "")
    {
        this.requestId = requestId;
        this.responseCode = code;
        this.message = message;
        this.timestamp = Time.time;
        this.jsonData = "{}";
    }

    public void SetData(string key, object value)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict == null)
        {
            dataDict = new DataDictionary();
        }
        dataDict.SetData(key, value);
        jsonData = JsonUtility.ToJson(dataDict);
    }

    public T GetData<T>(string key, T defaultValue = default(T))
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict != null && dataDict.ContainsKey(key))
        {
            return dataDict.GetData<T>(key, defaultValue);
        }
        return defaultValue;
    }

    public bool TryGetData<T>(string key, out T value)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict != null && dataDict.ContainsKey(key))
        {
            value = dataDict.GetData<T>(key, default(T));
            return true;
        }
        value = default(T);
        return false;
    }

    public bool ContainsKey(string key)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        return dataDict != null && dataDict.ContainsKey(key);
    }

    public bool IsSuccess()
    {
        return responseCode == NetworkResponseCode.Success;
    }
}

[Serializable]
public class NetworkError
{
    public NetworkErrorType errorType;
    public string errorMessage;
    public string stackTrace;
    public string requestId;
    public DateTime timestamp;
    public string jsonData;

    public NetworkError()
    {
        timestamp = DateTime.Now;
        jsonData = "{}";
    }

    public NetworkError(NetworkErrorType type, string message) : this()
    {
        errorType = type;
        errorMessage = message;
    }

    public void SetAdditionalData(string key, object value)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict == null)
        {
            dataDict = new DataDictionary();
        }
        dataDict.SetData(key, value);
        jsonData = JsonUtility.ToJson(dataDict);
    }

    public T GetAdditionalData<T>(string key, T defaultValue = default(T))
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict != null && dataDict.ContainsKey(key))
        {
            return dataDict.GetData<T>(key, defaultValue);
        }
        return defaultValue;
    }

    public override string ToString()
    {
        return $"[{errorType}] {errorMessage} (时间: {timestamp:yyyy-MM-dd HH:mm:ss})";
    }
}

[Serializable]
public class NetworkStats
{
    public float ping;
    public int totalBytesSent;
    public int totalBytesReceived;
    public float packetsPerSecond;
    public int connectionCount;

    public void Reset()
    {
        ping = 0f;
        totalBytesSent = 0;
        totalBytesReceived = 0;
        packetsPerSecond = 0f;
        connectionCount = 0;
    }
}

public enum NetworkState
{
    Disconnected,
    Connecting,
    Connected,
    Good,
    Fair,
    Poor,
    Reconnecting,
    Failed
}

[Serializable]
public class NetworkPlayerInfo
{
    public string playerId;
    public string characterId;
    public bool isLocalPlayer;
    public float ping;
    public Vector3 position;
    public Quaternion rotation;
    public int health;
    public int score;

    public NetworkPlayerInfo()
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        health = 100;
        score = 0;
    }
}

[Serializable]
public class NetworkMessage
{
    public string messageId;
    public string senderId;
    public string receiverId;
    public string messageType;
    public string jsonData;
    public float timestamp;

    public NetworkMessage()
    {
        messageId = Guid.NewGuid().ToString();
        timestamp = Time.time;
        jsonData = "{}";
    }

    public NetworkMessage(string type) : this()
    {
        messageType = type;
    }

    public void SetData(string key, object value)
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict == null)
        {
            dataDict = new DataDictionary();
        }
        dataDict.SetData(key, value);
        jsonData = JsonUtility.ToJson(dataDict);
    }

    public T GetData<T>(string key, T defaultValue = default(T))
    {
        var dataDict = JsonUtility.FromJson<DataDictionary>(jsonData);
        if (dataDict != null && dataDict.ContainsKey(key))
        {
            return dataDict.GetData<T>(key, defaultValue);
        }
        return defaultValue;
    }
}

[Serializable]
public class DataDictionary
{
    public List<DataEntry> entries = new List<DataEntry>();

    public void SetData(string key, object value)
    {
        var existingEntry = entries.Find(e => e.key == key);
        if (existingEntry != null)
        {
            existingEntry.value = value != null ? value.ToString() : "";
            existingEntry.type = value?.GetType().Name ?? "string";
        }
        else
        {
            entries.Add(new DataEntry { key = key, value = value != null ? value.ToString() : "", type = value?.GetType().Name ?? "string" });
        }
    }

    public T GetData<T>(string key, T defaultValue = default(T))
    {
        var entry = entries.Find(e => e.key == key);
        if (entry != null)
        {
            try
            {
                if (typeof(T) == typeof(int))
                {
                    if (int.TryParse(entry.value, out int intValue))
                    {
                        return (T)(object)intValue;
                    }
                    else
                    {
                        Debug.LogWarning($"[DataDictionary] 无法解析整数值: {entry.value}");
                        return defaultValue;
                    }
                }
                else if (typeof(T) == typeof(float))
                {
                    if (float.TryParse(entry.value, out float floatValue))
                    {
                        return (T)(object)floatValue;
                    }
                    else
                    {
                        Debug.LogWarning($"[DataDictionary] 无法解析浮点值: {entry.value}");
                        return defaultValue;
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(entry.value, out bool boolValue))
                    {
                        return (T)(object)boolValue;
                    }
                    else
                    {
                        Debug.LogWarning($"[DataDictionary] 无法解析布尔值: {entry.value}");
                        return defaultValue;
                    }
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)entry.value;
                }
                else if (typeof(T) == typeof(Vector3))
                {
                    return (T)(object)JsonUtility.FromJson<Vector3>(entry.value);
                }
                else if (typeof(T) == typeof(Quaternion))
                {
                    return (T)(object)JsonUtility.FromJson<Quaternion>(entry.value);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataDictionary] 解析数据时发生异常: {ex.Message}, 键: {key}, 值: {entry.value}");
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public bool ContainsKey(string key)
    {
        return entries.Exists(e => e.key == key);
    }
}

[Serializable]
public class DataEntry
{
    public string key;
    public string value;
    public string type;
}
}
