using UnityEngine;
using Mirror;

namespace NetworkCore
{
    /// <summary>
    /// 运行时网络配置诊断工具
    /// 用于在运行时验证NetworkManager和Transport的配置状态
    /// </summary>
    public class NetworkDiagnostics : MonoBehaviour
    {
        [Header("诊断设置")]
        public bool enableAutoDiagnostics = true;
        public float diagnosticInterval = 5f;

        private float lastDiagnosticTime = 0f;

        void Start()
        {
            if (enableAutoDiagnostics)
            {
                RunFullDiagnostics();
            }
        }

        void Update()
        {
            if (enableAutoDiagnostics && Time.time - lastDiagnosticTime >= diagnosticInterval)
            {
                RunBasicDiagnostics();
                lastDiagnosticTime = Time.time;
            }
        }

        /// <summary>
        /// 运行完整的网络诊断
        /// </summary>
        public void RunFullDiagnostics()
        {
            Debug.Log("=== 开始完整网络诊断 ===");
            
            DiagnoseNetworkManager();
            DiagnoseTransport();
            DiagnoseNetworkState();
            DiagnoseParrelSyncEnvironment();
            
            Debug.Log("=== 完整网络诊断完成 ===\n");
        }

        /// <summary>
        /// 运行基础网络诊断
        /// </summary>
        public void RunBasicDiagnostics()
        {
            DiagnoseNetworkState();
        }

        /// <summary>
        /// 诊断NetworkManager配置
        /// </summary>
        void DiagnoseNetworkManager()
        {
            Debug.Log("[NetworkDiagnostics] 诊断NetworkManager配置:");
            
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("[NetworkDiagnostics] ✗ NetworkManager.singleton为null");
                return;
            }
            
            Debug.Log($"[NetworkDiagnostics] ✓ NetworkManager.singleton: {NetworkManager.singleton.name}");
            Debug.Log($"[NetworkDiagnostics]   - 网络地址: {NetworkManager.singleton.networkAddress}");
            Debug.Log($"[NetworkDiagnostics]   - 最大连接数: {NetworkManager.singleton.maxConnections}");
            Debug.Log($"[NetworkDiagnostics]   - 发送频率: {NetworkManager.singleton.sendRate}");
            Debug.Log($"[NetworkDiagnostics]   - PlayerPrefab: {(NetworkManager.singleton.playerPrefab != null ? NetworkManager.singleton.playerPrefab.name : "null")}");
        }

        /// <summary>
        /// 诊断Transport配置
        /// </summary>
        void DiagnoseTransport()
        {
            Debug.Log("[NetworkDiagnostics] 诊断Transport配置:");
            
            Transport transport = NetworkManager.singleton?.GetComponent<Transport>();
            
            if (transport == null)
            {
                Debug.LogError("[NetworkDiagnostics] ✗ NetworkManager上未找到Transport组件");
                return;
            }
            
            Debug.Log($"[NetworkDiagnostics] ✓ Transport类型: {transport.GetType().Name}");
            
            // 获取Transport端口
            try
            {
                var portProperty = transport.GetType().GetProperty("Port");
                if (portProperty != null)
                {
                    int port = (int)portProperty.GetValue(transport);
                    Debug.Log($"[NetworkDiagnostics]   - 端口: {port}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NetworkDiagnostics]   - 无法读取端口: {ex.Message}");
            }
            
            // 检查Transport.active
            if (Transport.active == null)
            {
                Debug.LogError("[NetworkDiagnostics] ✗ Transport.active为null，网络连接将失败");
                Debug.LogError("[NetworkDiagnostics]   建议: 在GameSceneNetworkInitializer中强制设置Transport.active");
            }
            else if (Transport.active == transport)
            {
                Debug.Log($"[NetworkDiagnostics] ✓ Transport.active正确配置: {Transport.active.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"[NetworkDiagnostics] ⚠ Transport.active与NetworkManager的Transport不匹配");
                Debug.LogWarning($"[NetworkDiagnostics]   - Transport.active: {Transport.active.GetType().Name}");
                Debug.LogWarning($"[NetworkDiagnostics]   - NetworkManager.Transport: {transport.GetType().Name}");
            }
        }

        /// <summary>
        /// 诊断网络状态
        /// </summary>
        void DiagnoseNetworkState()
        {
            Debug.Log("[NetworkDiagnostics] 诊断网络状态:");
            
            bool serverActive = NetworkServer.active;
            bool clientConnected = NetworkClient.isConnected;
            bool clientReady = NetworkClient.ready;
            
            Debug.Log($"[NetworkDiagnostics]   - NetworkServer.active: {serverActive}");
            Debug.Log($"[NetworkDiagnostics]   - NetworkClient.isConnected: {clientConnected}");
            Debug.Log($"[NetworkDiagnostics]   - NetworkClient.ready: {clientReady}");
            
            if (serverActive && clientConnected)
            {
                Debug.Log("[NetworkDiagnostics] ✓ 主机模式运行正常");
            }
            else if (!serverActive && clientConnected)
            {
                Debug.Log("[NetworkDiagnostics] ✓ 客户端模式运行正常");
            }
            else if (serverActive && !clientConnected)
            {
                Debug.LogWarning("[NetworkDiagnostics] ⚠ 服务器已启动但客户端未连接");
            }
            else if (!serverActive && !clientConnected)
            {
                Debug.LogWarning("[NetworkDiagnostics] ⚠ 网络未启动");
            }
            
            // 检查本地玩家
            if (NetworkClient.localPlayer != null)
            {
                Debug.Log($"[NetworkDiagnostics] ✓ 本地玩家: {NetworkClient.localPlayer.name} (netId: {NetworkClient.localPlayer.netId})");
            }
            else
            {
                Debug.LogWarning("[NetworkDiagnostics] ⚠ 本地玩家未创建");
            }
        }

        /// <summary>
        /// 诊断ParrelSync环境
        /// </summary>
        void DiagnoseParrelSyncEnvironment()
        {
            Debug.Log("[NetworkDiagnostics] 诊断ParrelSync环境:");
            
            bool isParrelSync = IsParrelSyncEnvironment();
            Debug.Log($"[NetworkDiagnostics]   - 是否在ParrelSync环境中: {isParrelSync}");
            
            if (isParrelSync)
            {
                Debug.LogWarning("[NetworkDiagnostics] ⚠ 检测到ParrelSync环境");
                Debug.LogWarning("[NetworkDiagnostics]   - 确保每个编辑器实例使用不同的端口");
                Debug.LogWarning("[NetworkDiagnostics]   - 主机使用端口7777，客户端使用端口7778");
                Debug.LogWarning("[NetworkDiagnostics]   - 检查Clones Manager中的端口偏移设置");
            }
        }

        /// <summary>
        /// 检查是否在ParrelSync环境中
        /// </summary>
        bool IsParrelSyncEnvironment()
        {
            string projectPath = Application.dataPath;
            
            if (System.IO.Directory.Exists(System.IO.Path.Combine(Application.dataPath, "../ParrelSync")))
            {
                return true;
            }

            if (projectPath.Contains("_Clone"))
            {
                return true;
            }

            string parrelSyncConfig = System.IO.Path.Combine(Application.dataPath, "../ProjectSettings/ParrelSyncSettings.asset");
            if (System.IO.File.Exists(parrelSyncConfig))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 强制重新初始化Transport.active
        /// </summary>
        [ContextMenu("强制初始化Transport.active")]
        public void ForceInitializeTransportActive()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("[NetworkDiagnostics] NetworkManager.singleton为null，无法初始化Transport.active");
                return;
            }

            Transport transport = NetworkManager.singleton.GetComponent<Transport>();
            if (transport == null)
            {
                Debug.LogError("[NetworkDiagnostics] NetworkManager上未找到Transport组件");
                return;
            }

            Transport.active = transport;
            Debug.Log($"[NetworkDiagnostics] 已强制设置Transport.active: {Transport.active.GetType().Name}");
        }
    }
}