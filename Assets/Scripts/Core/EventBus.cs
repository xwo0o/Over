using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件总线系统，用于组件间解耦通信
/// 注意：使用DontDestroyOnLoad，确保场景切换时保持实例
/// </summary>
public class EventBus : MonoBehaviour
{
    private static EventBus _instance;
    public static EventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                // 尝试查找场景中已存在的实例
                _instance = FindObjectOfType<EventBus>();
                
                // 如果没有找到，则创建一个新的
                if (_instance == null)
                {
                    GameObject eventBusObject = new GameObject("EventBus");
                    _instance = eventBusObject.AddComponent<EventBus>();
                }
            }
            return _instance;
        }
    }

    // 事件字典，存储事件类型到回调列表的映射
    private Dictionary<string, List<Action<object>>> eventDictionary;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        eventDictionary = new Dictionary<string, List<Action<object>>>();
    }

    void OnDestroy()
    {
        // 清理所有事件订阅
        if (eventDictionary != null)
        {
            eventDictionary.Clear();
        }
        
        // 如果这是主实例，重置静态引用
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">回调函数</param>
    public void Subscribe(string eventName, Action<object> callback)
    {
        if (eventDictionary.TryGetValue(eventName, out List<Action<object>> subscribers))
        {
            if (!subscribers.Contains(callback))
            {
                subscribers.Add(callback);
            }
        }
        else
        {
            List<Action<object>> newSubscribers = new List<Action<object>>();
            newSubscribers.Add(callback);
            eventDictionary.Add(eventName, newSubscribers);
        }
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="callback">回调函数</param>
    public void Unsubscribe(string eventName, Action<object> callback)
    {
        if (eventDictionary.TryGetValue(eventName, out List<Action<object>> subscribers))
        {
            subscribers.Remove(callback);
            
            if (subscribers.Count == 0)
            {
                eventDictionary.Remove(eventName);
            }
        }
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="data">事件数据</param>
    public void Publish(string eventName, object data = null)
    {
        if (eventDictionary.TryGetValue(eventName, out List<Action<object>> subscribers))
        {
            // 创建副本以防止在回调中修改列表
            List<Action<object>> subscribersCopy = new List<Action<object>>(subscribers);
            
            foreach (var callback in subscribersCopy)
            {
                try
                {
                    callback?.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"事件 '{eventName}' 的回调函数执行出错: {e.Message}");
                }
            }
        }
    }
}

/// <summary>
/// 预定义的事件类型
/// </summary>
public static class GameEvents
{
    // 角色选择相关事件
    public const string CHARACTER_SELECTED = "CharacterSelected";
    public const string CHARACTER_SELECTION_CONFIRMED = "CharacterSelectionConfirmed";
    
    // 网络模式选择相关事件
    public const string NETWORK_MODE_SELECTED = "NetworkModeSelected";
    public const string NETWORK_CONNECTED = "NetworkConnected";
    public const string NETWORK_DISCONNECTED = "NetworkDisconnected";
    
    // 游戏状态相关事件
    public const string GAME_STARTED = "GameStarted";
    public const string GAME_ENDED = "GameEnded";
    public const string PLAYER_JOINED = "PlayerJoined";
    public const string PLAYER_LEFT = "PlayerLeft";
    
    // UI相关事件
    public const string SCENE_LOADED = "SceneLoaded";
    public const string UI_NAVIGATION_CHANGED = "UINavigationChanged";
}