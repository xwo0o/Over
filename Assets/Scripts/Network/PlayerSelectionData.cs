using UnityEngine;

namespace NetworkCore
{
    /// <summary>
    /// 网络模式枚举
    /// </summary>
    public enum NetworkMode
    {
        None,           // 无网络模式（单机模式）
        Host,           // 主机模式
        Client          // 客户端模式
    }

    /// <summary>
    /// 玩家选择数据管理类
    /// 用于在Character场景保存角色选择和网络模式，并在GameScene中读取应用
    /// </summary>
    public static class PlayerSelectionData
    {
        private static string selectedCharacterId = string.Empty;
        private static NetworkMode selectedNetworkMode = NetworkMode.None;
        private static string serverAddress = "127.0.0.1";
        private static int serverPort = 7777;
        private static bool isDataSaved = false;

        /// <summary>
        /// 保存角色ID
        /// </summary>
        public static string SelectedCharacterId
        {
            get => selectedCharacterId;
            set => selectedCharacterId = value;
        }

        /// <summary>
        /// 保存网络模式
        /// </summary>
        public static NetworkMode SelectedNetworkMode
        {
            get => selectedNetworkMode;
            set => selectedNetworkMode = value;
        }

        /// <summary>
        /// 服务器地址（客户端模式使用）
        /// </summary>
        public static string ServerAddress
        {
            get => serverAddress;
            set => serverAddress = value;
        }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public static int ServerPort
        {
            get => serverPort;
            set => serverPort = value;
        }

        /// <summary>
        /// 数据是否已保存
        /// </summary>
        public static bool IsDataSaved => isDataSaved;

        /// <summary>
        /// 保存玩家选择数据
        /// </summary>
        /// <param name="characterId">角色ID</param>
        /// <param name="networkMode">网络模式</param>
        /// <param name="address">服务器地址（可选）</param>
        /// <param name="port">服务器端口（可选）</param>
        public static void SavePlayerSelection(string characterId, NetworkMode networkMode, string address = "127.0.0.1", int port = 7777)
        {
            selectedCharacterId = characterId;
            selectedNetworkMode = networkMode;
            serverAddress = address;
            serverPort = port;
            isDataSaved = true;

            Debug.Log($"[PlayerSelectionData] 保存玩家选择数据:");
            Debug.Log($"  角色ID: {selectedCharacterId}");
            Debug.Log($"  网络模式: {selectedNetworkMode}");
            Debug.Log($"  服务器地址: {serverAddress}:{serverPort}");
        }

        /// <summary>
        /// 清除保存的数据
        /// </summary>
        public static void ClearData()
        {
            selectedCharacterId = string.Empty;
            selectedNetworkMode = NetworkMode.None;
            serverAddress = "127.0.0.1";
            serverPort = 7777;
            isDataSaved = false;

            Debug.Log("[PlayerSelectionData] 已清除保存的数据");
        }

        /// <summary>
        /// 检查数据是否有效
        /// </summary>
        public static bool IsValidData()
        {
            return isDataSaved && !string.IsNullOrEmpty(selectedCharacterId) && selectedNetworkMode != NetworkMode.None;
        }

        /// <summary>
        /// 获取网络模式描述
        /// </summary>
        public static string GetNetworkModeDescription(NetworkMode mode)
        {
            switch (mode)
            {
                case NetworkMode.Host:
                    return "主机模式";
                case NetworkMode.Client:
                    return "客户端模式";
                case NetworkMode.None:
                    return "单机模式";
                default:
                    return "未知模式";
            }
        }
    }
}
