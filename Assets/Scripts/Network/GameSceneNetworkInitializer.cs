using UnityEngine;
using Mirror;
using System.Collections;
using NetworkCore;

namespace NetworkCore
{
    /// <summary>
    /// GameScene网络初始化管理器
    /// 负责在GameScene加载后读取保存的角色选择和网络模式数据
    /// 并启动对应的网络连接和角色实例化
    /// </summary>
    public class GameSceneNetworkInitializer : MonoBehaviour
    {
        [Header("初始化设置")]
        public float networkStartupDelay = 3f;
        public float maxNetworkWaitTime = 180f;
        public float maxPlayerWaitTime = 180f;

        [Header("调试")]
        public bool enableDebugLogs = true;
        public bool enableNetworkDiagnostics = true;

        private bool isInitialized = false;
        private NetworkPlayer localNetworkPlayer;
        private bool isNetworkStarted = false;
        private NetworkDiagnostics networkDiagnostics;

        public bool IsInitialized => isInitialized;
        public bool IsNetworkStarted => isNetworkStarted;

        void Start()
        {
            LogDebug("[GameSceneNetworkInitializer] GameScene已加载，开始初始化网络");
            
            // 初始化网络诊断工具
            if (enableNetworkDiagnostics)
            {
                networkDiagnostics = gameObject.AddComponent<NetworkDiagnostics>();
                LogDebug("[GameSceneNetworkInitializer] 网络诊断工具已启用");
            }
            
            // 检查是否有保存的数据
            if (!PlayerSelectionData.IsDataSaved)
            {
                LogError("[GameSceneNetworkInitializer] 未找到保存的玩家选择数据");
                LogError("[GameSceneNetworkInitializer] 请先在Character场景完成角色和网络模式选择");
                return;
            }

            LogDebug($"[GameSceneNetworkInitializer] 找到保存的数据:");
            LogDebug($"  角色ID: {PlayerSelectionData.SelectedCharacterId}");
            LogDebug($"  网络模式: {PlayerSelectionData.GetNetworkModeDescription(PlayerSelectionData.SelectedNetworkMode)}");
            LogDebug($"  服务器地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");

            // 延迟启动网络，确保场景完全加载
            StartCoroutine(InitializeNetworkWithDelay());
        }

        IEnumerator InitializeNetworkWithDelay()
        {
            LogDebug($"[GameSceneNetworkInitializer] 等待 {networkStartupDelay} 秒后启动网络");
            yield return new WaitForSeconds(networkStartupDelay);

            // 检查NetworkManager是否存在
            if (NetworkManager.singleton == null)
            {
                LogError("[GameSceneNetworkInitializer] NetworkManager未找到，无法启动网络");
                yield break;
            }

            LogDebug("[GameSceneNetworkInitializer] NetworkManager已找到，开始启动网络");

            // 关键修复：强制重新初始化Transport.active，确保在ParrelSync环境中正确赋值
            Transport transportComponent = NetworkManager.singleton.GetComponent<Transport>();
            if (transportComponent != null)
            {
                Transport.active = transportComponent;
                LogDebug($"[GameSceneNetworkInitializer] 强制设置Transport.active: {Transport.active.GetType().Name}");
            }
            else
            {
                LogError("[GameSceneNetworkInitializer] NetworkManager上未找到Transport组件");
                yield break;
            }

            // 再次验证Transport.active
            LogDebug($"[GameSceneNetworkInitializer] Transport.active: {(Transport.active != null ? Transport.active.GetType().Name : "null")}");
            if (Transport.active == null)
            {
                LogError("[GameSceneNetworkInitializer] Transport.active仍然为null，无法启动网络");
                yield break;
            }

            // 运行网络诊断
            if (enableNetworkDiagnostics && networkDiagnostics != null)
            {
                LogDebug("[GameSceneNetworkInitializer] 运行网络诊断");
                networkDiagnostics.RunFullDiagnostics();
            }

            // 根据保存的网络模式启动对应的网络连接
            switch (PlayerSelectionData.SelectedNetworkMode)
            {
                case NetworkMode.Host:
                    StartCoroutine(StartHostMode());
                    break;
                case NetworkMode.Client:
                    StartCoroutine(StartClientMode());
                    break;
                case NetworkMode.None:
                    LogError("[GameSceneNetworkInitializer] 网络模式为None，无法启动网络");
                    break;
                default:
                    LogError($"[GameSceneNetworkInitializer] 未知的网络模式: {PlayerSelectionData.SelectedNetworkMode}");
                    break;
            }
        }

        IEnumerator StartHostMode()
        {
            LogDebug("[GameSceneNetworkInitializer] 启动主机模式");

            // 先设置网络地址和端口，再启动主机
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.networkAddress = PlayerSelectionData.ServerAddress;
                SetTransportPort(PlayerSelectionData.ServerPort);
                LogDebug($"[GameSceneNetworkInitializer] 已设置网络地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");
            }

            // 使用NetworkConnectionManager启动主机
            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.StartHost(
                    PlayerSelectionData.ServerAddress,
                    PlayerSelectionData.ServerPort
                );
            }
            else
            {
                // 如果NetworkConnectionManager不存在，直接使用NetworkManager
                NetworkManager.singleton.StartHost();
            }

            isNetworkStarted = true;

            // 等待网络服务器完全启动（关键修复：确保NetworkServer.active为true）
            yield return WaitForNetworkServerReady(maxNetworkWaitTime);

            // 等待网络连接建立
            yield return WaitForNetworkConnection(maxNetworkWaitTime);

            // 等待NetworkPlayer创建并初始化
            yield return WaitForNetworkPlayerReady(maxPlayerWaitTime);

            // 发送角色选择数据到服务器
            yield return SendCharacterSelectionToServer();

            // 实例化角色
            yield return InstantiateSelectedCharacter();

            isInitialized = true;
            LogDebug("[GameSceneNetworkInitializer] 主机模式初始化完成");
        }

        IEnumerator StartClientMode()
        {
            LogDebug("[GameSceneNetworkInitializer] 启动客户端模式");

            // 验证Transport组件是否正确初始化
            if (Transport.active == null)
            {
                LogError("[GameSceneNetworkInitializer] Transport.active为null，无法启动客户端");
                LogError("[GameSceneNetworkInitializer] 请检查GameScene中的GameNetworkManager是否正确配置了Transport组件");
                yield break;
            }
            LogDebug($"[GameSceneNetworkInitializer] Transport已初始化: {Transport.active.GetType().Name}");

            // 先设置网络地址和端口，再启动客户端
            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.networkAddress = PlayerSelectionData.ServerAddress;
                SetTransportPort(PlayerSelectionData.ServerPort);
                LogDebug($"[GameSceneNetworkInitializer] 已设置网络地址: {PlayerSelectionData.ServerAddress}:{PlayerSelectionData.ServerPort}");
            }

            // 使用NetworkConnectionManager启动客户端
            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.StartClient(
                    PlayerSelectionData.ServerAddress,
                    PlayerSelectionData.ServerPort
                );
            }
            else
            {
                // 如果NetworkConnectionManager不存在，直接使用NetworkManager
                NetworkManager.singleton.StartClient();
            }

            isNetworkStarted = true;

            // 等待网络连接建立
            yield return WaitForNetworkConnection(maxNetworkWaitTime);

            // 等待NetworkPlayer创建并初始化
            yield return WaitForNetworkPlayerReady(maxPlayerWaitTime);

            // 发送角色选择数据到服务器
            yield return SendCharacterSelectionToServer();

            // 实例化角色
            yield return InstantiateSelectedCharacter();

            isInitialized = true;
            LogDebug("[GameSceneNetworkInitializer] 客户端模式初始化完成");
        }

        IEnumerator WaitForNetworkConnection(float maxWaitTime)
        {
            LogDebug($"[GameSceneNetworkInitializer] 等待网络连接建立，最长等待时间: {maxWaitTime}秒");

            float elapsedTime = 0f;
            // 优化：减少检查间隔，从0.5秒减少到0.1秒，提高响应速度
            float checkInterval = 0.1f;

            while (!NetworkClient.isConnected && elapsedTime < maxWaitTime)
            {
                elapsedTime += checkInterval;

                if (Mathf.Approximately(elapsedTime % 1f, 0f))
                {
                    LogDebug($"[GameSceneNetworkInitializer] 等待网络连接... 已等待: {elapsedTime:F1}秒");
                }

                yield return new WaitForSeconds(checkInterval);
            }

            if (NetworkClient.isConnected)
            {
                LogDebug($"[GameSceneNetworkInitializer] 网络连接已建立，等待时间: {elapsedTime:F1}秒");
            }
            else
            {
                LogError($"[GameSceneNetworkInitializer] 网络连接超时，已等待: {elapsedTime:F1}秒");
            }
        }

        /// <summary>
        /// 等待网络服务器完全启动
        /// </summary>
        IEnumerator WaitForNetworkServerReady(float maxWaitTime)
        {
            LogDebug($"[GameSceneNetworkInitializer] 等待网络服务器启动，最长等待时间: {maxWaitTime}秒");

            float elapsedTime = 0f;
            float checkInterval = 0.2f;

            while (!NetworkServer.active && elapsedTime < maxWaitTime)
            {
                elapsedTime += checkInterval;

                if (Mathf.Approximately(elapsedTime % 1f, 0f))
                {
                    LogDebug($"[GameSceneNetworkInitializer] 等待网络服务器启动... 已等待: {elapsedTime:F1}秒, NetworkServer.active: {NetworkServer.active}");
                }

                yield return new WaitForSeconds(checkInterval);
            }

            if (NetworkServer.active)
            {
                LogDebug($"[GameSceneNetworkInitializer] 网络服务器已启动，等待时间: {elapsedTime:F1}秒");
            }
            else
            {
                LogError($"[GameSceneNetworkInitializer] 网络服务器启动超时，已等待: {elapsedTime:F1}秒");
            }
        }

        IEnumerator WaitForNetworkPlayerReady(float maxWaitTime)
        {
            LogDebug($"[GameSceneNetworkInitializer] 等待NetworkPlayer就绪，最长等待时间: {maxWaitTime}秒");

            float elapsedTime = 0f;
            // 优化：减少检查间隔，从0.3秒减少到0.1秒，提高响应速度
            float checkInterval = 0.1f;

            while (!IsNetworkPlayerReady() && elapsedTime < maxWaitTime)
            {
                elapsedTime += checkInterval;

                if (Mathf.Approximately(elapsedTime % 1f, 0f))
                {
                    LogDebug($"[GameSceneNetworkInitializer] 等待NetworkPlayer就绪... 已等待: {elapsedTime:F1}秒");
                }

                // 尝试查找本地NetworkPlayer
                FindLocalNetworkPlayer();

                yield return new WaitForSeconds(checkInterval);
            }

            if (IsNetworkPlayerReady())
            {
                LogDebug($"[GameSceneNetworkInitializer] NetworkPlayer已就绪，等待时间: {elapsedTime:F1}秒");
                LogDebug($"[GameSceneNetworkInitializer] NetworkPlayer ID: {localNetworkPlayer.netId}");
            }
            else
            {
                LogError($"[GameSceneNetworkInitializer] NetworkPlayer就绪超时，已等待: {elapsedTime:F1}秒");
            }
        }

        void FindLocalNetworkPlayer()
        {
            if (localNetworkPlayer != null && localNetworkPlayer.isLocalPlayer)
            {
                return;
            }

            NetworkIdentity[] networkIdentities = FindObjectsOfType<NetworkIdentity>();

            foreach (NetworkIdentity identity in networkIdentities)
            {
                if (identity == null)
                {
                    continue;
                }

                if (identity.isLocalPlayer)
                {
                    localNetworkPlayer = identity.GetComponent<NetworkPlayer>();
                    if (localNetworkPlayer != null)
                    {
                        try
                        {
                            LogDebug($"[GameSceneNetworkInitializer] 找到本地NetworkPlayer: {localNetworkPlayer.netId}");
                        }
                        catch (System.Exception ex)
                        {
                            LogDebug($"[GameSceneNetworkInitializer] 找到本地NetworkPlayer，但无法访问netId: {ex.Message}");
                        }
                        return;
                    }
                }
            }
        }

        bool IsNetworkPlayerReady()
        {
            return localNetworkPlayer != null &&
                   NetworkClient.isConnected &&
                   localNetworkPlayer.isLocalPlayer &&
                   localNetworkPlayer.IsInitialized;
        }

        IEnumerator SendCharacterSelectionToServer()
        {
            LogDebug($"[GameSceneNetworkInitializer] 发送角色选择到服务器: {PlayerSelectionData.SelectedCharacterId}");

            if (localNetworkPlayer == null)
            {
                LogError("[GameSceneNetworkInitializer] 未找到本地NetworkPlayer，无法发送角色选择");
                yield break;
            }

            if (!NetworkClient.isConnected)
            {
                LogError("[GameSceneNetworkInitializer] 网络未连接，无法发送角色选择");
                yield break;
            }

            localNetworkPlayer.CmdSelectCharacter(PlayerSelectionData.SelectedCharacterId);

            // 验证角色数据是否成功上传
            yield return VerifyCharacterDataUpload();
        }

        IEnumerator VerifyCharacterDataUpload()
        {
            LogDebug("[GameSceneNetworkInitializer] 开始验证角色数据上传");

            // 优化：减少最大等待时间，从15秒减少到10秒
            int maxWaitTime = 10;
            float elapsedTime = 0f;
            int retryCount = 0;
            int maxRetries = 3;

            while (elapsedTime < maxWaitTime)
            {
                if (localNetworkPlayer != null && !string.IsNullOrEmpty(localNetworkPlayer.selectedCharacterId))
                {
                    if (localNetworkPlayer.selectedCharacterId == PlayerSelectionData.SelectedCharacterId)
                    {
                        LogDebug($"[GameSceneNetworkInitializer] 角色数据上传验证成功！服务器端角色ID: {localNetworkPlayer.selectedCharacterId}");
                        yield break;
                    }
                }

                // 优化：减少等待间隔，从0.5秒减少到0.1秒，提高响应速度
                elapsedTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            LogError($"[GameSceneNetworkInitializer] 角色数据上传验证失败！等待{maxWaitTime}秒后仍未收到服务器确认");
            
            retryCount++;
            if (retryCount < maxRetries)
            {
                LogWarning($"[GameSceneNetworkInitializer] 尝试重新发送角色选择命令 (重试 {retryCount}/{maxRetries})");
                localNetworkPlayer.CmdSelectCharacter(PlayerSelectionData.SelectedCharacterId);
                yield return VerifyCharacterDataUpload();
            }
            else
            {
                LogError($"[GameSceneNetworkInitializer] 角色数据上传验证失败，已达到最大重试次数 {maxRetries}");
            }
        }

        IEnumerator InstantiateSelectedCharacter()
        {
            LogDebug($"[GameSceneNetworkInitializer] 实例化角色: {PlayerSelectionData.SelectedCharacterId}");

            // 这里可以根据需要实现角色实例化逻辑
            // 角色实例化通常由NetworkPlayer在收到服务器确认后自动完成
            // 如果需要手动实例化，可以在这里添加逻辑

            LogDebug("[GameSceneNetworkInitializer] 角色实例化逻辑已触发（由NetworkPlayer处理）");
            yield return new WaitForSeconds(0.5f);
        }

        void SetTransportPort(int port)
        {
            Transport transport = Transport.active;
            if (transport != null)
            {
                try
                {
                    var portProperty = transport.GetType().GetProperty("Port");
                    if (portProperty != null && portProperty.CanWrite)
                    {
                        portProperty.SetValue(transport, (ushort)port);
                        LogDebug($"[GameSceneNetworkInitializer] 已设置传输层端口: {port}");
                    }
                }
                catch (System.Exception ex)
                {
                    LogWarning($"[GameSceneNetworkInitializer] 设置端口失败: {ex.Message}");
                }
            }
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
            // 清理保存的数据（可选）
            // PlayerSelectionData.ClearData();
            LogDebug("[GameSceneNetworkInitializer] GameSceneNetworkInitializer已销毁");
        }
    }
}
