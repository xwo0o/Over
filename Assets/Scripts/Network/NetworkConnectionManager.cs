using Mirror;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NetworkCore
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }

    public class NetworkConnectionManager : MonoBehaviour
    {
        private static NetworkConnectionManager instance;
        public static NetworkConnectionManager Instance => instance;

        [Header("连接设置")]
        public float connectionTimeout = 180f;
        public int maxReconnectAttempts = 3;
        public float reconnectDelay = 3f;

        [Header("调试")]
        public bool enableDebugLogs = true;

        private ConnectionStatus currentStatus = ConnectionStatus.Disconnected;
        private NetworkMode currentNetworkMode = NetworkMode.None;
        private int reconnectAttempts = 0;
        private Coroutine timeoutCheckCoroutine;
        private Coroutine reconnectCoroutine;

        private enum NetworkMode
        {
            None,
            Host,
            Client
        }

        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnConnectionError;

        public ConnectionStatus CurrentStatus => currentStatus;
        public bool IsConnected => currentStatus == ConnectionStatus.Connected;
        public bool IsConnecting => currentStatus == ConnectionStatus.Connecting;
        public bool IsHost => currentNetworkMode == NetworkMode.Host;
        public bool IsClient => currentNetworkMode == NetworkMode.Client;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            RegisterNetworkCallbacks();
            LogDebug("[NetworkConnectionManager] 连接管理器已初始化");
        }

        void RegisterNetworkCallbacks()
        {
            NetworkServer.OnConnectedEvent += OnServerConnected;
            NetworkServer.OnDisconnectedEvent += OnServerDisconnected;
            NetworkClient.OnConnectedEvent += OnClientConnected;
            NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
        }

        void UnregisterNetworkCallbacks()
        {
            NetworkServer.OnConnectedEvent -= OnServerConnected;
            NetworkServer.OnDisconnectedEvent -= OnServerDisconnected;
            NetworkClient.OnConnectedEvent -= OnClientConnected;
            NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
        }

        public void StartHost(string serverAddress = "127.0.0.1", int port = 7777)
        {
            if (currentStatus == ConnectionStatus.Connected || currentStatus == ConnectionStatus.Connecting)
            {
                LogWarning("[NetworkConnectionManager] 已经处于连接状态，无法启动主机");
                return;
            }

            LogDebug($"[NetworkConnectionManager] 启动主机模式: {serverAddress}:{port}");
            currentNetworkMode = NetworkMode.Host;
            UpdateConnectionStatus(ConnectionStatus.Connecting);

            if (NetworkManager.singleton == null)
            {
                HandleConnectionError("NetworkManager未找到");
                return;
            }

            if (Transport.active == null)
            {
                LogError("[NetworkConnectionManager] Transport.active为null，无法启动主机");
                LogError("[NetworkConnectionManager] 请检查NetworkManager是否正确配置了Transport组件");
                HandleConnectionError("Transport未初始化");
                return;
            }

            NetworkManager.singleton.networkAddress = serverAddress;
            SetTransportPort(port);

            NetworkManager.singleton.StartHost();
            StartTimeoutCheck();
        }

        public void StartClient(string serverAddress = "127.0.0.1", int port = 7777)
        {
            if (currentStatus == ConnectionStatus.Connected || currentStatus == ConnectionStatus.Connecting)
            {
                LogWarning("[NetworkConnectionManager] 已经处于连接状态，无法启动客户端");
                return;
            }

            LogDebug($"[NetworkConnectionManager] 启动客户端模式: {serverAddress}:{port}");
            currentNetworkMode = NetworkMode.Client;
            UpdateConnectionStatus(ConnectionStatus.Connecting);

            if (NetworkManager.singleton == null)
            {
                HandleConnectionError("NetworkManager未找到");
                return;
            }

            if (Transport.active == null)
            {
                LogError("[NetworkConnectionManager] Transport.active为null，无法启动客户端");
                LogError("[NetworkConnectionManager] 请检查NetworkManager是否正确配置了Transport组件");
                HandleConnectionError("Transport未初始化");
                return;
            }

            NetworkManager.singleton.networkAddress = serverAddress;
            SetTransportPort(port);

            NetworkManager.singleton.StartClient();
            StartTimeoutCheck();
        }

        void SetTransportPort(int port)
        {
            Transport transport = Transport.active;
            try
            {
                var portProperty = transport.GetType().GetProperty("Port");
                if (portProperty != null && portProperty.CanWrite)
                {
                    portProperty.SetValue(transport, (ushort)port);
                    LogDebug($"[NetworkConnectionManager] 已设置传输层端口: {port}");
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[NetworkConnectionManager] 设置端口失败: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            LogDebug("[NetworkConnectionManager] 断开连接");

            if (timeoutCheckCoroutine != null)
            {
                StopCoroutine(timeoutCheckCoroutine);
                timeoutCheckCoroutine = null;
            }

            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
                reconnectCoroutine = null;
            }

            reconnectAttempts = 0;
            currentNetworkMode = NetworkMode.None;

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton?.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton?.StopClient();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton?.StopServer();
            }

            UpdateConnectionStatus(ConnectionStatus.Disconnected);
        }

        public void Reconnect()
        {
            if (reconnectAttempts >= maxReconnectAttempts)
            {
                LogError("[NetworkConnectionManager] 已达到最大重连次数");
                UpdateConnectionStatus(ConnectionStatus.Failed);
                return;
            }

            LogDebug($"[NetworkConnectionManager] 尝试重连 ({reconnectAttempts + 1}/{maxReconnectAttempts})");
            UpdateConnectionStatus(ConnectionStatus.Reconnecting);

            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
            }

            reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
        }

        IEnumerator ReconnectCoroutine()
        {
            yield return new WaitForSeconds(reconnectDelay);

            reconnectAttempts++;

            if (currentNetworkMode == NetworkMode.Host)
            {
                StartHost();
            }
            else if (currentNetworkMode == NetworkMode.Client)
            {
                StartClient();
            }
        }

        void StartTimeoutCheck()
        {
            if (timeoutCheckCoroutine != null)
            {
                StopCoroutine(timeoutCheckCoroutine);
            }
            timeoutCheckCoroutine = StartCoroutine(TimeoutCheckCoroutine());
        }

        IEnumerator TimeoutCheckCoroutine()
        {
            float elapsedTime = 0f;
            float checkInterval = 5f;

            LogDebug($"[NetworkConnectionManager] 开始连接超时检查，超时时间: {connectionTimeout}秒");

            while (elapsedTime < connectionTimeout)
            {
                yield return new WaitForSeconds(checkInterval);
                elapsedTime += checkInterval;

                if (CheckConnectionEstablished())
                {
                    LogDebug($"[NetworkConnectionManager] 连接建立成功 (耗时: {elapsedTime:F1}秒)");
                    UpdateConnectionStatus(ConnectionStatus.Connected);
                    reconnectAttempts = 0;
                    OnConnected?.Invoke();
                    yield break;
                }

                LogDebug($"[NetworkConnectionManager] 连接状态检查 (已等待: {elapsedTime:F1}秒 / {connectionTimeout}秒)");
                LogDebug($"  - 当前状态: {currentStatus}");
                LogDebug($"  - 网络模式: {currentNetworkMode}");
                LogDebug($"  - NetworkServer.active: {NetworkServer.active}");
                LogDebug($"  - NetworkClient.isConnected: {NetworkClient.isConnected}");
                LogDebug($"  - NetworkClient.localPlayer: {(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.name : "null")}");
                LogDebug($"  - NetworkManager.singleton.networkAddress: {NetworkManager.singleton?.networkAddress}");
                LogDebug($"  - Transport.active: {(Transport.active != null ? Transport.active.GetType().Name : "null")}");
            }

            if (currentStatus == ConnectionStatus.Connecting)
            {
                LogError("[NetworkConnectionManager] 连接超时！");
                LogError("[NetworkConnectionManager] 最终状态:");
                LogError($"  - 当前状态: {currentStatus}");
                LogError($"  - 网络模式: {currentNetworkMode}");
                LogError($"  - NetworkServer.active: {NetworkServer.active}");
                LogError($"  - NetworkClient.isConnected: {NetworkClient.isConnected}");
                LogError($"  - NetworkClient.localPlayer: {(NetworkClient.localPlayer != null ? NetworkClient.localPlayer.name : "null")}");
                LogError($"  - NetworkManager.singleton.networkAddress: {NetworkManager.singleton?.networkAddress}");
                LogError($"  - Transport.active: {(Transport.active != null ? Transport.active.GetType().Name : "null")}");

                HandleConnectionError("连接超时");
            }
        }

        bool CheckConnectionEstablished()
        {
            if (currentNetworkMode == NetworkMode.Host)
            {
                return NetworkServer.active && NetworkClient.isConnected;
            }
            else if (currentNetworkMode == NetworkMode.Client)
            {
                return NetworkClient.isConnected && NetworkClient.localPlayer != null;
            }
            return false;
        }

        void OnServerConnected(NetworkConnectionToClient conn)
        {
            LogDebug($"[NetworkConnectionManager] 服务器: 客户端已连接 {conn.connectionId}");
            
            if (currentNetworkMode == NetworkMode.Host && NetworkClient.isConnected)
            {
                UpdateConnectionStatus(ConnectionStatus.Connected);
                reconnectAttempts = 0;
                OnConnected?.Invoke();
            }
        }

        void OnServerDisconnected(NetworkConnectionToClient conn)
        {
            LogDebug($"[NetworkConnectionManager] 服务器: 客户端已断开 {conn.connectionId}");
            OnDisconnected?.Invoke();
        }

        void OnClientConnected()
        {
            LogDebug("[NetworkConnectionManager] 客户端: 已连接到服务器");
            
            if (currentNetworkMode == NetworkMode.Client && NetworkClient.localPlayer != null)
            {
                UpdateConnectionStatus(ConnectionStatus.Connected);
                reconnectAttempts = 0;
                OnConnected?.Invoke();
            }
        }

        void OnClientDisconnected()
        {
            LogDebug("[NetworkConnectionManager] 客户端: 已从服务器断开");
            UpdateConnectionStatus(ConnectionStatus.Disconnected);
            OnDisconnected?.Invoke();

            if (reconnectAttempts < maxReconnectAttempts)
            {
                LogDebug("[NetworkConnectionManager] 自动尝试重连...");
                Reconnect();
            }
            else
            {
                LogError("[NetworkConnectionManager] 重连失败");
                UpdateConnectionStatus(ConnectionStatus.Failed);
            }
        }

        void HandleConnectionError(string errorMessage)
        {
            LogError($"[NetworkConnectionManager] 连接错误: {errorMessage}");
            UpdateConnectionStatus(ConnectionStatus.Failed);
            OnConnectionError?.Invoke(errorMessage);
        }

        void UpdateConnectionStatus(ConnectionStatus newStatus)
        {
            if (currentStatus != newStatus)
            {
                LogDebug($"[NetworkConnectionManager] 连接状态变更: {currentStatus} -> {newStatus}");
                currentStatus = newStatus;
                OnConnectionStatusChanged?.Invoke(newStatus);
            }
        }

        public NetworkConnectionToClient GetLocalConnection()
        {
            if (NetworkClient.isConnected && NetworkClient.localPlayer != null)
            {
                return NetworkClient.localPlayer.connectionToServer as NetworkConnectionToClient;
            }
            return null;
        }

        public NetworkConnectionToClient GetConnectionById(int connectionId)
        {
            try
            {
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn != null && conn.connectionId == connectionId)
                    {
                        return conn as NetworkConnectionToClient;
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogError($"[NetworkConnectionManager] 获取连接时发生异常: {ex.Message}");
            }
            return null;
        }

        public int GetConnectedPlayerCount()
        {
            return NetworkServer.connections.Count;
        }

        public List<NetworkConnectionToClient> GetAllConnections()
        {
            List<NetworkConnectionToClient> connections = new List<NetworkConnectionToClient>();
            try
            {
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn != null)
                    {
                        connections.Add(conn as NetworkConnectionToClient);
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogError($"[NetworkConnectionManager] 获取所有连接时发生异常: {ex.Message}");
            }
            return connections;
        }

        void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        void LogError(string message)
        {
            Debug.LogError(message);
        }

        void OnDestroy()
        {
            UnregisterNetworkCallbacks();

            if (timeoutCheckCoroutine != null)
            {
                StopCoroutine(timeoutCheckCoroutine);
            }

            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
            }
        }
    }
}
