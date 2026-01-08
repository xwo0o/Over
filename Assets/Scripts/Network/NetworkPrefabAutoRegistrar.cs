using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 网络预制体自动注册器
/// 在游戏启动时自动将所有带有NetworkIdentity组件的预制体注册到NetworkManager
/// </summary>
public class NetworkPrefabAutoRegistrar : MonoBehaviour
{
    [Header("自动注册设置")]
    public bool autoRegisterOnStart = true;
    public bool useAssetDatabase = true;
    public bool registerFromResources = true;
    public string resourcesPath = "Prefabs";
    public string[] searchPaths = new string[] { "Assets/Prefabs" };
    
    [Header("调试")]
    public bool enableDebugLogs = true;

    private void Awake()
    {
        // 在Awake中注册，确保在网络初始化之前完成
        if (autoRegisterOnStart)
        {
            RegisterNetworkPrefabs();
        }
    }

    /// <summary>
    /// 自动注册所有网络预制体到NetworkManager
    /// </summary>
    [ContextMenu("注册所有网络预制体")]
    public void RegisterNetworkPrefabs()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[NetworkPrefabAutoRegistrar] NetworkManager未找到，无法注册预制体");
            return;
        }

        List<GameObject> networkPrefabs = new List<GameObject>();

        // 使用AssetDatabase查找预制体（编辑器模式，包括ParrelSync客户端编辑器）
        if (useAssetDatabase)
        {
            foreach (var searchPath in searchPaths)
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { searchPath });
                foreach (string guid in prefabGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    
                    if (prefab != null && prefab.GetComponent<NetworkIdentity>() != null)
                    {
                        networkPrefabs.Add(prefab);
                        LogDebug($"[NetworkPrefabAutoRegistrar] 找到网络预制体: {prefab.name} (路径: {assetPath})");
                    }
                }
            }
        }

        // 从Resources文件夹加载预制体（运行时支持）
        if (registerFromResources)
        {
            GameObject[] resourcePrefabs = Resources.LoadAll<GameObject>(resourcesPath);
            foreach (var prefab in resourcePrefabs)
            {
                if (prefab != null && prefab.GetComponent<NetworkIdentity>() != null)
                {
                    networkPrefabs.Add(prefab);
                    LogDebug($"[NetworkPrefabAutoRegistrar] 找到网络预制体(Resources): {prefab.name}");
                }
            }
        }

        if (networkPrefabs.Count == 0)
        {
            Debug.LogWarning($"[NetworkPrefabAutoRegistrar] 未找到任何网络预制体。请确保预制体在 '{resourcesPath}' 文件夹中，或者在编辑器模式下使用AssetDatabase。");
            return;
        }

        // 检查是否已经注册
        List<GameObject> newPrefabs = new List<GameObject>();
        foreach (var prefab in networkPrefabs)
        {
            bool isRegistered = false;
            foreach (var registeredPrefab in NetworkManager.singleton.spawnPrefabs)
            {
                if (registeredPrefab != null && registeredPrefab.name == prefab.name)
                {
                    isRegistered = true;
                    break;
                }
            }

            if (!isRegistered)
            {
                newPrefabs.Add(prefab);
            }
        }

        // 添加新预制体到NetworkManager
        if (newPrefabs.Count > 0)
        {
            List<GameObject> currentPrefabs = new List<GameObject>(NetworkManager.singleton.spawnPrefabs);
            currentPrefabs.AddRange(newPrefabs);
            NetworkManager.singleton.spawnPrefabs = currentPrefabs;

            LogDebug($"[NetworkPrefabAutoRegistrar] 成功注册 {newPrefabs.Count} 个网络预制体到NetworkManager");
            foreach (var prefab in newPrefabs)
            {
                LogDebug($"  - {prefab.name} (assetId: {prefab.GetComponent<NetworkIdentity>().assetId})");
            }
        }
        else
        {
            LogDebug("[NetworkPrefabAutoRegistrar] 所有网络预制体已注册，无需重复注册");
        }

        LogDebug($"[NetworkPrefabAutoRegistrar] NetworkManager当前注册的预制体总数: {NetworkManager.singleton.spawnPrefabs.Count}");
    }

    /// <summary>
    /// 清除所有已注册的预制体（谨慎使用）
    /// </summary>
    [ContextMenu("清除所有注册的预制体")]
    public void ClearRegisteredPrefabs()
    {
        if (NetworkManager.singleton == null)
        {
            Debug.LogError("[NetworkPrefabAutoRegistrar] NetworkManager未找到");
            return;
        }

        NetworkManager.singleton.spawnPrefabs = new List<GameObject>();
        LogDebug("[NetworkPrefabAutoRegistrar] 已清除所有注册的预制体");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
}
