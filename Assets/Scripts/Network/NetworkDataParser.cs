using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkCore
{
    public class NetworkDataParser : MonoBehaviour
    {
        private static NetworkDataParser instance;
        public static NetworkDataParser Instance => instance;

        private Dictionary<Type, Func<object, string>> serializers = new Dictionary<Type, Func<object, string>>();
        private Dictionary<Type, Func<string, object>> deserializers = new Dictionary<Type, Func<string, object>>();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterDefaultSerializers();
        }

        void RegisterDefaultSerializers()
        {
            RegisterSerializer(typeof(int), obj => obj.ToString());
            RegisterSerializer(typeof(float), obj => obj.ToString());
            RegisterSerializer(typeof(bool), obj => obj.ToString().ToLower());
            RegisterSerializer(typeof(string), obj => obj.ToString());
            RegisterSerializer(typeof(Vector3), SerializeVector3);
            RegisterSerializer(typeof(Vector2), SerializeVector2);
            RegisterSerializer(typeof(Quaternion), SerializeQuaternion);
            RegisterSerializer(typeof(int[]), SerializeIntArray);
            RegisterSerializer(typeof(float[]), SerializeFloatArray);
            RegisterSerializer(typeof(string[]), SerializeStringArray);

            RegisterDeserializer(typeof(int), str => int.Parse(str));
            RegisterDeserializer(typeof(float), str => float.Parse(str));
            RegisterDeserializer(typeof(bool), str => bool.Parse(str));
            RegisterDeserializer(typeof(string), str => str);
            RegisterDeserializer(typeof(Vector3), str => DeserializeVector3(str));
            RegisterDeserializer(typeof(Vector2), str => DeserializeVector2(str));
            RegisterDeserializer(typeof(Quaternion), str => DeserializeQuaternion(str));
            RegisterDeserializer(typeof(int[]), str => DeserializeIntArray(str));
            RegisterDeserializer(typeof(float[]), str => DeserializeFloatArray(str));
            RegisterDeserializer(typeof(string[]), str => DeserializeStringArray(str));
        }

        public void RegisterSerializer(Type type, Func<object, string> serializer)
        {
            if (serializers.ContainsKey(type))
            {
                serializers[type] = serializer;
            }
            else
            {
                serializers.Add(type, serializer);
            }
            Debug.Log($"[NetworkDataParser] 注册序列化器: {type.Name}");
        }

        public void RegisterDeserializer(Type type, Func<string, object> deserializer)
        {
            if (deserializers.ContainsKey(type))
            {
                deserializers[type] = deserializer;
            }
            else
            {
                deserializers.Add(type, deserializer);
            }
            Debug.Log($"[NetworkDataParser] 注册反序列化器: {type.Name}");
        }

        public string Serialize(object data)
        {
            if (data == null)
            {
                return null;
            }

            Type type = data.GetType();

            if (serializers.ContainsKey(type))
            {
                return serializers[type](data);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return SerializeUnityObject((UnityEngine.Object)data);
            }

            Debug.LogWarning($"[NetworkDataParser] 未找到序列化器: {type.Name}, 使用JSON序列化");
            return JsonUtility.ToJson(data);
        }

        public T Deserialize<T>(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return default(T);
            }

            Type type = typeof(T);

            if (deserializers.ContainsKey(type))
            {
                return (T)deserializers[type](data);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                UnityEngine.Object obj = DeserializeUnityObject(data, type);
                if (obj != null && typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)obj;
                }
                return default(T);
            }

            Debug.LogWarning($"[NetworkDataParser] 未找到反序列化器: {type.Name}, 使用JSON反序列化");
            return JsonUtility.FromJson<T>(data);
        }

        public object Deserialize(string data, Type type)
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }

            if (deserializers.ContainsKey(type))
            {
                return deserializers[type](data);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return DeserializeUnityObject(data, type);
            }

            Debug.LogWarning($"[NetworkDataParser] 未找到反序列化器: {type.Name}, 使用JSON反序列化");
            return JsonUtility.FromJson(data, type);
        }

        public string SerializeVector3(object obj)
        {
            Vector3 vec = (Vector3)obj;
            return $"{vec.x},{vec.y},{vec.z}";
        }

        public Vector3 DeserializeVector3(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return Vector3.zero;
            }
            
            string[] parts = str.Split(',');
            if (parts.Length == 3)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y) &&
                    float.TryParse(parts[2].Trim(), out float z))
                {
                    return new Vector3(x, y, z);
                }
                else
                {
                    Debug.LogWarning($"[NetworkDataParser] 无法解析Vector3: {str}, 部分值格式错误");
                    return Vector3.zero;
                }
            }
            Debug.LogWarning($"[NetworkDataParser] 无法解析Vector3: {str}, 格式不正确 (需要3个值，实际{parts.Length}个)");
            return Vector3.zero;
        }

        public string SerializeVector2(object obj)
        {
            Vector2 vec = (Vector2)obj;
            return $"{vec.x},{vec.y}";
        }

        public Vector2 DeserializeVector2(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return Vector2.zero;
            }
            
            string[] parts = str.Split(',');
            if (parts.Length == 2)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y))
                {
                    return new Vector2(x, y);
                }
                else
                {
                    Debug.LogWarning($"[NetworkDataParser] 无法解析Vector2: {str}, 部分值格式错误");
                    return Vector2.zero;
                }
            }
            Debug.LogWarning($"[NetworkDataParser] 无法解析Vector2: {str}, 格式不正确 (需要2个值，实际{parts.Length}个)");
            return Vector2.zero;
        }

        public string SerializeQuaternion(object obj)
        {
            Quaternion quat = (Quaternion)obj;
            return $"{quat.x},{quat.y},{quat.z},{quat.w}";
        }

        public Quaternion DeserializeQuaternion(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return Quaternion.identity;
            }
            
            string[] parts = str.Split(',');
            if (parts.Length == 4)
            {
                if (float.TryParse(parts[0].Trim(), out float x) &&
                    float.TryParse(parts[1].Trim(), out float y) &&
                    float.TryParse(parts[2].Trim(), out float z) &&
                    float.TryParse(parts[3].Trim(), out float w))
                {
                    return new Quaternion(x, y, z, w);
                }
                else
                {
                    Debug.LogWarning($"[NetworkDataParser] 无法解析Quaternion: {str}, 部分值格式错误");
                    return Quaternion.identity;
                }
            }
            Debug.LogWarning($"[NetworkDataParser] 无法解析Quaternion: {str}, 格式不正确 (需要4个值，实际{parts.Length}个)");
            return Quaternion.identity;
        }

        public string SerializeIntArray(object obj)
        {
            int[] array = (int[])obj;
            if (array.Length == 0)
                return "";
            
            // 由于int数组可以直接使用string.Join，我们保留原逻辑，但添加空数组检查
            return string.Join(",", array);
        }

        public int[] DeserializeIntArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new int[0];
            }
            string[] parts = str.Split(',');
            // 预估结果数组大小以减少内存分配
            int[] result = new int[parts.Length];
            int count = 0;
            
            for (int i = 0; i < parts.Length; i++)
            {
                // 检查空字符串或仅包含空白字符的字符串
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    continue; // 跳过空项
                }
                
                // 尝试解析整数，如果失败则跳过该项
                if (int.TryParse(parts[i].Trim(), out int value))
                {
                    if (count >= result.Length)
                    {
                        // 如果预分配的数组不够，扩展它
                        System.Array.Resize(ref result, result.Length * 2);
                    }
                    result[count] = value;
                    count++;
                }
                else
                {
                    Debug.LogWarning($"[NetworkDataParser] 无法解析整数: '{parts[i]}', 跳过该项");
                }
            }
            
            // 调整数组大小到实际使用的数量
            if (count < result.Length)
            {
                int[] finalResult = new int[count];
                System.Array.Copy(result, finalResult, count);
                return finalResult;
            }
            
            return result;
        }

        public string SerializeFloatArray(object obj)
        {
            float[] array = (float[])obj;
            if (array.Length == 0)
                return "";
            
            // 使用数组来存储格式化后的字符串，然后使用string.Join一次性连接
            string[] stringArray = new string[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                stringArray[i] = array[i].ToString("F2");
            }
            return string.Join(",", stringArray);
        }

        public float[] DeserializeFloatArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new float[0];
            }
            string[] parts = str.Split(',');
            // 预估结果数组大小以减少内存分配
            float[] result = new float[parts.Length];
            int count = 0;
            
            for (int i = 0; i < parts.Length; i++)
            {
                // 检查空字符串或仅包含空白字符的字符串
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    continue; // 跳过空项
                }
                
                // 尝试解析浮点数，如果失败则跳过该项
                if (float.TryParse(parts[i].Trim(), out float value))
                {
                    if (count >= result.Length)
                    {
                        // 如果预分配的数组不够，扩展它
                        System.Array.Resize(ref result, result.Length * 2);
                    }
                    result[count] = value;
                    count++;
                }
                else
                {
                    Debug.LogWarning($"[NetworkDataParser] 无法解析浮点数: '{parts[i]}', 跳过该项");
                }
            }
            
            // 调整数组大小到实际使用的数量
            if (count < result.Length)
            {
                float[] finalResult = new float[count];
                System.Array.Copy(result, finalResult, count);
                return finalResult;
            }
            
            return result;
        }

        public string SerializeStringArray(object obj)
        {
            string[] array = (string[])obj;
            if (array.Length == 0)
                return "";
            
            // 使用数组来存储转义后的字符串，然后使用string.Join一次性连接
            string[] escapedArray = new string[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                escapedArray[i] = EscapeString(array[i]);
            }
            return string.Join(",", escapedArray);
        }

        public string[] DeserializeStringArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new string[0];
            }
            
            // 预估结果数组大小以减少内存分配
            int estimatedSize = 1;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ',')
                    estimatedSize++;
            }
            string[] result = new string[estimatedSize];
            int count = 0;
            
            StringBuilder current = new StringBuilder();
            bool inEscape = false;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '\\' && !inEscape)
                {
                    inEscape = true;
                }
                else if (c == ',' && !inEscape)
                {
                    // 避免每次都调用ToString()，减少GC分配
                    string item = UnescapeString(current.ToString());
                    if (count >= result.Length)
                    {
                        System.Array.Resize(ref result, result.Length * 2);
                    }
                    result[count] = item;
                    current.Clear();
                    count++;
                }
                else
                {
                    current.Append(c);
                    inEscape = false;
                }
            }
            if (current.Length > 0)
            {
                string item = UnescapeString(current.ToString());
                if (count >= result.Length)
                {
                    System.Array.Resize(ref result, result.Length * 2);
                }
                result[count] = item;
                count++;
            }
            
            // 调整数组大小到实际使用的数量
            if (count < result.Length)
            {
                string[] finalResult = new string[count];
                System.Array.Copy(result, finalResult, count);
                return finalResult;
            }
            
            return result;
        }

        private string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\").Replace(",", "\\,");
        }

        private string UnescapeString(string str)
        {
            return str.Replace("\\,", ",").Replace("\\\\", "\\");
        }

        private string SerializeUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            return obj.GetInstanceID().ToString();
        }

        private T DeserializeUnityObject<T>(string data) where T : UnityEngine.Object
        {
            return (T)DeserializeUnityObject(data, typeof(T));
        }

        private UnityEngine.Object DeserializeUnityObject(string data, Type type)
        {
            if (data == "null")
            {
                return null;
            }

            int instanceID;
            if (int.TryParse(data, out instanceID))
            {
                // 注意：Unity中无法通过instanceID直接获取对象，这只是一个占位实现
                // 在实际项目中，可能需要使用其他方法来获取对象
                Debug.LogWarning($"[NetworkDataParser] 无法通过instanceID获取Unity对象: {instanceID}. Unity不支持直接通过instanceID获取对象。");
                return null;
            }

            return null;
        }

        public string SerializeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict)
        {
            if (dict == null || dict.Count == 0)
            {
                return "";
            }

            List<string> pairs = new List<string>();
            foreach (var kvp in dict)
            {
                string key = Serialize(kvp.Key);
                string value = Serialize(kvp.Value);
                pairs.Add($"{key}:{value}");
            }
            return string.Join("|", pairs);
        }

        public Dictionary<TKey, TValue> DeserializeDictionary<TKey, TValue>(string data)
        {
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();

            if (string.IsNullOrEmpty(data))
            {
                return result;
            }

            string[] pairs = data.Split('|');
            foreach (string pair in pairs)
            {
                if (string.IsNullOrEmpty(pair)) continue; // 跳过空项
                
                string[] parts = pair.Split(':');
                if (parts.Length >= 2)
                {
                    // 将第一部分作为键，其余部分（用冒号连接）作为值
                    string keyStr = parts[0];
                    string valueStr = string.Join(":", parts, 1, parts.Length - 1); // 从第二部分开始连接
                    
                    try
                    {
                        TKey key = Deserialize<TKey>(keyStr);
                        TValue value = Deserialize<TValue>(valueStr);
                        result[key] = value;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[NetworkDataParser] 反序列化字典项时出错: {ex.Message}, 键: {keyStr}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NetworkDataParser] 无法解析字典项: {pair}, 格式不正确 (需要至少2个部分，实际{parts.Length}个)");
                }
            }

            return result;
        }

        public string SerializeRequest(NetworkRequest request)
        {
            // 使用数组来构建字符串，减少字符串连接的GC分配
            string[] parts = {
                $"id:{request.requestId}",
                $"type:{request.requestType}",
                $"sender:{request.senderId}",
                $"time:{request.timestamp}",
                $"data:{request.jsonData}"
            };
            return string.Join(";", parts) + ";";
        }

        public NetworkRequest DeserializeRequest(string data)
        {
            NetworkRequest request = new NetworkRequest();

            if (string.IsNullOrEmpty(data))
            {
                return request;
            }

            string[] parts = data.Split(';');
            foreach (string part in parts)
            {
                string[] kvp = part.Split(':');
                if (kvp.Length == 2)
                {
                    string key = kvp[0];
                    string value = kvp[1];

                    switch (key)
                    {
                        case "id":
                            request.requestId = value;
                            break;
                        case "type":
                            if (Enum.TryParse<NetworkRequestType>(value, out NetworkRequestType requestType))
                            {
                                request.requestType = requestType;
                            }
                            else
                            {
                                Debug.LogWarning($"[NetworkDataParser] 无法解析请求类型: {value}, 使用默认值");
                                request.requestType = NetworkRequestType.Unknown;
                            }
                            break;
                        case "sender":
                            request.senderId = value;
                            break;
                        case "time":
                            if (float.TryParse(value, out float timestamp))
                            {
                                request.timestamp = timestamp;
                            }
                            else
                            {
                                Debug.LogWarning($"[NetworkDataParser] 无法解析时间戳: {value}, 使用默认值");
                                request.timestamp = Time.time;
                            }
                            break;
                        case "data":
                            request.jsonData = value;
                            break;
                    }
                }
            }

            return request;
        }

        public string SerializeResponse(NetworkResponse response)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"id:{response.requestId};");
            sb.Append($"code:{response.responseCode};");
            sb.Append($"msg:{EscapeString(response.message)};");
            sb.Append($"time:{response.timestamp};");
            sb.Append($"data:{response.jsonData};");
            return sb.ToString();
        }

        public NetworkResponse DeserializeResponse(string data)
        {
            NetworkResponse response = new NetworkResponse();

            if (string.IsNullOrEmpty(data))
            {
                return response;
            }

            string[] parts = data.Split(';');
            foreach (string part in parts)
            {
                string[] kvp = part.Split(':');
                if (kvp.Length == 2)
                {
                    string key = kvp[0];
                    string value = kvp[1];

                    switch (key)
                    {
                        case "id":
                            response.requestId = value;
                            break;
                        case "code":
                            if (Enum.TryParse<NetworkResponseCode>(value, out NetworkResponseCode responseCode))
                            {
                                response.responseCode = responseCode;
                            }
                            else
                            {
                                Debug.LogWarning($"[NetworkDataParser] 无法解析响应代码: {value}, 使用默认值");
                                response.responseCode = NetworkResponseCode.Unknown;
                            }
                            break;
                        case "msg":
                            response.message = UnescapeString(value);
                            break;
                        case "time":
                            if (float.TryParse(value, out float timestamp))
                            {
                                response.timestamp = timestamp;
                            }
                            else
                            {
                                Debug.LogWarning($"[NetworkDataParser] 无法解析时间戳: {value}, 使用默认值");
                                response.timestamp = Time.time;
                            }
                            break;
                        case "data":
                            response.jsonData = value;
                            break;
                    }
                }
            }

            return response;
        }

        public bool ValidateData(string data, Type expectedType)
        {
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            try
            {
                object result = Deserialize(data, expectedType);
                return result != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDataParser] 数据验证失败: {e.Message}");
                return false;
            }
        }

        void OnDestroy()
        {
            serializers.Clear();
            deserializers.Clear();
        }
    }
}
