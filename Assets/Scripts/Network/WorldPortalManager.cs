using Mirror;
using UnityEngine;
using System;
using System.Collections;

namespace NetworkCore
{
    public class WorldPortalManager : MonoBehaviour
    {
        #region 单例模式

        private static WorldPortalManager instance;
        public static WorldPortalManager Instance => instance;

        #endregion

        #region 设置与调试

        [Header("设置")]
        public float switchWorldTimeout = 30f;
        public float disconnectWaitTime = 1f;

        [Header("调试")]
        public bool enableDebugLogs = true;

        #endregion

        #region 状态变量

        private bool isSwitchingWorld = false;
        private Coroutine switchWorldCoroutine;
        private string pendingWorldAddress;
        private int pendingWorldPort;

        #endregion

        #region 事件回调

        public event Action OnWorldSwitchStarted;
        public event Action OnWorldSwitchCompleted;
        public event Action<string> OnWorldSwitchFailed;

        #endregion

        #region Unity 生命周期

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
            LogDebug("[WorldPortalManager] 世界传送管理器已初始化");
        }

        void OnDestroy()
        {
            if (switchWorldCoroutine != null)
            {
                StopCoroutine(switchWorldCoroutine);
                switchWorldCoroutine = null;
            }
        }

        #endregion

        #region 公共方法

        public void SwitchWorld(string serverAddress, int port = 7777)
        {
            if (isSwitchingWorld)
            {
                LogWarning("[WorldPortalManager] 正在进行世界切换，无法再次切换");
                return;
            }

            if (string.IsNullOrEmpty(serverAddress))
            {
                string error = "服务器地址为空";
                LogError($"[WorldPortalManager] {error}");
                OnWorldSwitchFailed?.Invoke(error);
                return;
            }

            LogDebug($"[WorldPortalManager] 开始切换世界到 {serverAddress}:{port}");
            pendingWorldAddress = serverAddress;
            pendingWorldPort = port;

            switchWorldCoroutine = StartCoroutine(SwitchWorldCoroutine());
        }

        public void CancelSwitchWorld()
        {
            if (!isSwitchingWorld)
            {
                return;
            }

            LogWarning("[WorldPortalManager] 取消世界切换");
            isSwitchingWorld = false;

            if (switchWorldCoroutine != null)
            {
                StopCoroutine(switchWorldCoroutine);
                switchWorldCoroutine = null;
            }

            OnWorldSwitchFailed?.Invoke("切换被取消");
        }

        #endregion

        #region 私有方法

        private IEnumerator SwitchWorldCoroutine()
        {
            isSwitchingWorld = true;
            OnWorldSwitchStarted?.Invoke();

            LogDebug("[WorldPortalManager] 步骤1: 断开当前世界连接");
            yield return DisconnectCurrentWorld();

            if (!isSwitchingWorld)
            {
                yield break;
            }

            LogDebug("[WorldPortalManager] 步骤2: 等待断开完成");
            yield return new WaitForSeconds(disconnectWaitTime);

            if (!isSwitchingWorld)
            {
                yield break;
            }

            LogDebug("[WorldPortalManager] 步骤3: 连接到新世界");
            yield return ConnectToNewWorld();

            if (!isSwitchingWorld)
            {
                yield break;
            }

            LogDebug("[WorldPortalManager] 世界切换完成");
            isSwitchingWorld = false;
            OnWorldSwitchCompleted?.Invoke();
        }

        private IEnumerator DisconnectCurrentWorld()
        {
            LogDebug("[WorldPortalManager] 开始断开当前世界");

            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.Disconnect();
            }
            else
            {
                LogWarning("[WorldPortalManager] NetworkConnectionManager 不存在，直接使用 Mirror API 断开");

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
            }

            float startTime = Time.time;
            while (NetworkServer.active || NetworkClient.isConnected)
            {
                if (Time.time - startTime > switchWorldTimeout)
                {
                    string error = "断开连接超时";
                    LogError($"[WorldPortalManager] {error}");
                    OnWorldSwitchFailed?.Invoke(error);
                    isSwitchingWorld = false;
                    yield break;
                }
                yield return null;
            }

            LogDebug("[WorldPortalManager] 当前世界已断开");
        }

        private IEnumerator ConnectToNewWorld()
        {
            LogDebug($"[WorldPortalManager] 开始连接到新世界 {pendingWorldAddress}:{pendingWorldPort}");

            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.StartClient(pendingWorldAddress, pendingWorldPort);
            }
            else
            {
                LogWarning("[WorldPortalManager] NetworkConnectionManager 不存在，直接使用 Mirror API 连接");

                if (NetworkManager.singleton == null)
                {
                    string error = "NetworkManager 未找到";
                    LogError($"[WorldPortalManager] {error}");
                    OnWorldSwitchFailed?.Invoke(error);
                    isSwitchingWorld = false;
                    yield break;
                }

                NetworkManager.singleton.networkAddress = pendingWorldAddress;
                SetTransportPort(pendingWorldPort);
                NetworkManager.singleton.StartClient();
            }

            float startTime = Time.time;
            while (!NetworkClient.isConnected)
            {
                if (Time.time - startTime > switchWorldTimeout)
                {
                    string error = "连接新世界超时";
                    LogError($"[WorldPortalManager] {error}");
                    OnWorldSwitchFailed?.Invoke(error);
                    isSwitchingWorld = false;
                    yield break;
                }
                yield return null;
            }

            LogDebug("[WorldPortalManager] 已连接到新世界");
        }

        private void SetTransportPort(int port)
        {
            Transport transport = Transport.active;
            try
            {
                var portProperty = transport.GetType().GetProperty("Port");
                if (portProperty != null && portProperty.CanWrite)
                {
                    portProperty.SetValue(transport, (ushort)port);
                    LogDebug($"[WorldPortalManager] 已设置传输层端口: {port}");
                }
            }
            catch (Exception ex)
            {
                LogWarning($"[WorldPortalManager] 设置端口失败: {ex.Message}");
            }
        }

        #endregion

        #region 日志方法

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        private void LogError(string message)
        {
            Debug.LogError(message);
        }

        #endregion
    }
}
