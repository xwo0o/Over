using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace NetworkCore
{
    public class NetworkStateMonitor : MonoBehaviour
    {
        private static NetworkStateMonitor instance;
        public static NetworkStateMonitor Instance => instance;

        [Header("监控设置")]
        public float pingUpdateInterval = 1f;
        public float statsUpdateInterval = 5f;
        public bool enableDetailedLogging = false;

        [Header("网络统计")]
        [SerializeField] private float currentPing = 0f;
        [SerializeField] private int totalBytesSent = 0;
        [SerializeField] private int totalBytesReceived = 0;
        [SerializeField] private float packetsPerSecond = 0f;
        [SerializeField] private int connectionCount = 0;

        private float lastPingUpdateTime = 0f;
        private float lastStatsUpdateTime = 0f;
        private int packetsSentLastInterval = 0;
        private int packetsReceivedLastInterval = 0;

        public event Action<float> OnPingUpdated;
        public event Action<NetworkStats> OnStatsUpdated;
        public event Action<NetworkState> OnNetworkStateChanged;

        public float CurrentPing => currentPing;
        public NetworkStats CurrentStats => new NetworkStats
        {
            ping = currentPing,
            totalBytesSent = totalBytesSent,
            totalBytesReceived = totalBytesReceived,
            packetsPerSecond = packetsPerSecond,
            connectionCount = connectionCount
        };

        public NetworkState GetCurrentNetworkState()
        {
            if (NetworkClient.isConnected)
            {
                return NetworkState.Connected;
            }
            else if (NetworkServer.active)
            {
                return NetworkState.Connected;
            }
            else
            {
                return NetworkState.Disconnected;
            }
        }

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
            RegisterEventListeners();
            LogDebug("[NetworkStateMonitor] 网络状态监听器已启动");
        }

        void Update()
        {
            UpdatePing();
            UpdateStats();
        }

        void RegisterEventListeners()
        {
            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.OnConnectionStatusChanged += OnConnectionStatusChangedHandler;
                NetworkConnectionManager.Instance.OnConnected += OnConnectedHandler;
                NetworkConnectionManager.Instance.OnDisconnected += OnDisconnectedHandler;
            }
        }

        void UnregisterEventListeners()
        {
            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.OnConnectionStatusChanged -= OnConnectionStatusChangedHandler;
                NetworkConnectionManager.Instance.OnConnected -= OnConnectedHandler;
                NetworkConnectionManager.Instance.OnDisconnected -= OnDisconnectedHandler;
            }
        }

        void UpdatePing()
        {
            if (Time.time - lastPingUpdateTime >= pingUpdateInterval)
            {
                if (NetworkClient.isConnected)
                {
                    try
                    {
                        currentPing = (float)(NetworkTime.rtt * 1000.0);
                        OnPingUpdated?.Invoke(currentPing);

                        if (enableDetailedLogging)
                        {
                            LogDebug($"[NetworkStateMonitor] Ping更新: {currentPing:F2}ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NetworkStateMonitor] 获取Ping值失败: {ex.Message}");
                        currentPing = 0f;
                    }
                }
                else
                {
                    currentPing = 0f;
                }

                lastPingUpdateTime = Time.time;
            }
        }

        void UpdateStats()
        {
            if (Time.time - lastStatsUpdateTime >= statsUpdateInterval)
            {
                UpdateNetworkStatistics();
                lastStatsUpdateTime = Time.time;
            }
        }

        void UpdateNetworkStatistics()
        {
            Transport transport = Transport.active;
            if (transport != null)
            {
                totalBytesSent = 0;
                totalBytesReceived = 0;
            }

            float timeElapsed = statsUpdateInterval;
            packetsPerSecond = (packetsSentLastInterval + packetsReceivedLastInterval) / timeElapsed;
            connectionCount = NetworkServer.connections.Count;

            NetworkStats stats = new NetworkStats
            {
                ping = currentPing,
                totalBytesSent = totalBytesSent,
                totalBytesReceived = totalBytesReceived,
                packetsPerSecond = packetsPerSecond,
                connectionCount = connectionCount
            };

            OnStatsUpdated?.Invoke(stats);

            if (enableDetailedLogging)
            {
                LogDebug($"[NetworkStateMonitor] 统计更新 - Ping: {currentPing:F2}ms, 发送: {totalBytesSent}字节, 接收: {totalBytesReceived}字节, PPS: {packetsPerSecond:F1}, 连接数: {connectionCount}");
            }

            packetsSentLastInterval = 0;
            packetsReceivedLastInterval = 0;
        }

        void OnConnectionStatusChangedHandler(ConnectionStatus status)
        {
            NetworkState state = ConvertToNetworkState(status);
            OnNetworkStateChanged?.Invoke(state);
            LogDebug($"[NetworkStateMonitor] 连接状态变更: {status}");
        }

        void OnConnectedHandler()
        {
            LogDebug("[NetworkStateMonitor] 已连接");
            OnNetworkStateChanged?.Invoke(NetworkState.Connected);
        }

        void OnDisconnectedHandler()
        {
            LogDebug("[NetworkStateMonitor] 已断开");
            OnNetworkStateChanged?.Invoke(NetworkState.Disconnected);
        }

        NetworkState ConvertToNetworkState(ConnectionStatus status)
        {
            switch (status)
            {
                case ConnectionStatus.Connected:
                    return NetworkState.Connected;
                case ConnectionStatus.Connecting:
                    return NetworkState.Connecting;
                case ConnectionStatus.Reconnecting:
                    return NetworkState.Reconnecting;
                case ConnectionStatus.Disconnected:
                    return NetworkState.Disconnected;
                case ConnectionStatus.Failed:
                    return NetworkState.Failed;
                default:
                    return NetworkState.Disconnected;
            }
        }

        public List<NetworkPlayerInfo> GetAllPlayerInfo()
        {
            List<NetworkPlayerInfo> playerInfos = new List<NetworkPlayerInfo>();

            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn != null && conn.identity != null)
                {
                    NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
                    if (player != null)
                    {
                        playerInfos.Add(new NetworkPlayerInfo
                        {
                            playerId = player.netId.ToString(),
                            characterId = player.selectedCharacterId,
                            isLocalPlayer = player.isLocalPlayer,
                            position = player.transform.position,
                            ping = currentPing
                        });
                    }
                }
            }

            return playerInfos;
        }

        public NetworkPlayerInfo GetLocalPlayerInfo()
        {
            if (NetworkClient.localPlayer != null)
            {
                NetworkPlayer player = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();
                if (player != null)
                {
                    return new NetworkPlayerInfo
                    {
                        playerId = player.netId.ToString(),
                        characterId = player.selectedCharacterId,
                        isLocalPlayer = player.isLocalPlayer,
                        position = player.transform.position,
                        ping = currentPing
                    };
                }
            }

            return new NetworkPlayerInfo();
        }

        void LogDebug(string message)
        {
            if (enableDetailedLogging)
            {
                Debug.Log(message);
            }
        }

        void OnDestroy()
        {
            UnregisterEventListeners();
        }
    }
}