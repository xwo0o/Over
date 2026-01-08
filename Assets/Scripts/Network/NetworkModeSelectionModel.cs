using UnityEngine;
using NetworkCore;

public class NetworkModeSelectionModel : MVCModel<NetworkModeSelectionModel>
{
    public NetworkModeSelectionModel()
    {
        InitializeSingleton();
    }
    
    private NetworkMode selectedMode = NetworkMode.None;
    private bool isCharacterScene = false;
    private bool autoLoadGameScene = true;
    private float sceneSwitchDelay = 0.5f;

    public NetworkMode SelectedMode 
    { 
        get { return selectedMode; } 
        set { selectedMode = value; }
    }

    public bool IsCharacterScene
    {
        get { return isCharacterScene; }
        set { isCharacterScene = value; }
    }

    public bool AutoLoadGameScene
    {
        get { return autoLoadGameScene; }
        set { autoLoadGameScene = value; }
    }

    public float SceneSwitchDelay
    {
        get { return sceneSwitchDelay; }
        set { sceneSwitchDelay = value; }
    }

    /// <summary>
    /// 选择主机模式
    /// </summary>
    public void SelectHostMode()
    {
        selectedMode = NetworkMode.Host;
        Debug.Log("[NetworkModeSelectionModel] 用户选择主机模式");

        // 保存角色选择和网络模式数据
        PlayerSelectionData.SavePlayerSelection(
            PlayerSelectionData.SelectedCharacterId,
            NetworkCore.NetworkMode.Host,
            "127.0.0.1",
            7777
        );
        Debug.Log($"[NetworkModeSelectionModel] 已保存角色选择: {PlayerSelectionData.SelectedCharacterId}");
    }

    /// <summary>
    /// 选择客户端模式
    /// </summary>
    public void SelectClientMode()
    {
        selectedMode = NetworkMode.Client;
        Debug.Log("[NetworkModeSelectionModel] 用户选择客户端模式");

        // 保存角色选择和网络模式数据
        PlayerSelectionData.SavePlayerSelection(
            PlayerSelectionData.SelectedCharacterId,
            NetworkCore.NetworkMode.Client,
            "localhost",
            7777
        );
        Debug.Log($"[NetworkModeSelectionModel] 已保存角色选择: {PlayerSelectionData.SelectedCharacterId}");
    }

    /// <summary>
    /// 重置选择
    /// </summary>
    public void ResetSelection()
    {
        selectedMode = NetworkMode.None;
    }

    /// <summary>
    /// 获取当前选择的网络模式
    /// </summary>
    /// <returns>当前选择的网络模式</returns>
    public NetworkMode GetCurrentSelection()
    {
        return selectedMode;
    }
}