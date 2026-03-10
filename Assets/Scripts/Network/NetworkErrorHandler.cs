using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace NetworkCore
{
    public enum NetworkErrorType
    {
        ConnectionError,
        TimeoutError,
        SerializationError,
        DeserializationError,
        ServerError,
        ClientError,
        UnknownError
    }

    public class NetworkErrorHandler : MonoBehaviour
    {
        private static NetworkErrorHandler instance;
        public static NetworkErrorHandler Instance => instance;

        [Header("错误处理设置")]
        public bool enableErrorLogging = true;
        public bool showInGameErrorUI = false;
        public int maxErrorHistorySize = 100;

        [Header("自动恢复设置")]
        public bool enableAutoReconnect = true;
        public int maxAutoReconnectAttempts = 3;
        public float autoReconnectDelay = 3f;

        private List<NetworkError> errorHistory = new List<NetworkError>();
        private int reconnectAttempts = 0;

        public event Action<NetworkError> OnNetworkError;
        public event Action<NetworkError> OnErrorResolved;
        public event Action<List<NetworkError>> OnErrorHistoryUpdated;

        public IReadOnlyList<NetworkError> ErrorHistory => errorHistory.AsReadOnly();
        public int ErrorCount => errorHistory.Count;

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
            RegisterErrorListeners();
            LogDebug("[NetworkErrorHandler] 网络错误处理器已启动");
        }

        void RegisterErrorListeners()
        {
            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.OnConnectionError += HandleConnectionError;
            }

            if (NetworkMessageHandler.Instance != null)
            {
                NetworkMessageHandler.Instance.RegisterResponseHandler(NetworkRequestType.Custom, HandleErrorResponse);
            }
        }

        void UnregisterErrorListeners()
        {
            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.OnConnectionError -= HandleConnectionError;
            }
        }

        public void ReportError(NetworkError error)
        {
            LogError($"[NetworkErrorHandler] 报告错误: {error}");

            errorHistory.Add(error);
            TrimErrorHistory();

            OnNetworkError?.Invoke(error);
            OnErrorHistoryUpdated?.Invoke(new List<NetworkError>(errorHistory));

            HandleError(error);
        }

        public void ReportError(NetworkErrorType errorType, string errorMessage, string stackTrace = null, string requestId = null)
        {
            NetworkError error = new NetworkError(errorType, errorMessage);
            error.stackTrace = stackTrace;
            error.requestId = requestId;

            ReportError(error);
        }

        void HandleConnectionError(string errorMessage)
        {
            NetworkError error = new NetworkError(NetworkErrorType.ConnectionError, errorMessage);
            ReportError(error);
        }

        void HandleErrorResponse(NetworkResponse response)
        {
            if (!response.IsSuccess())
            {
                NetworkError error = new NetworkError(NetworkErrorType.ServerError, response.message);
                error.requestId = response.requestId;
                ReportError(error);
            }
        }

        void HandleError(NetworkError error)
        {
            switch (error.errorType)
            {
                case NetworkErrorType.ConnectionError:
                    HandleConnectionErrorInternal(error);
                    break;
                case NetworkErrorType.TimeoutError:
                    HandleTimeoutError(error);
                    break;
                case NetworkErrorType.SerializationError:
                case NetworkErrorType.DeserializationError:
                    HandleSerializationError(error);
                    break;
                case NetworkErrorType.ServerError:
                    HandleServerError(error);
                    break;
                default:
                    HandleUnknownError(error);
                    break;
            }
        }

        void HandleConnectionErrorInternal(NetworkError error)
        {
            LogError($"[NetworkErrorHandler] 处理连接错误: {error.errorMessage}");

            if (enableAutoReconnect && reconnectAttempts < maxAutoReconnectAttempts)
            {
                LogDebug($"[NetworkErrorHandler] 尝试自动重连 ({reconnectAttempts + 1}/{maxAutoReconnectAttempts})");
                reconnectAttempts++;
                Invoke(nameof(AttemptAutoReconnect), autoReconnectDelay);
            }
            else
            {
                LogError("[NetworkErrorHandler] 自动重连失败，已达到最大尝试次数");
            }
        }

        void HandleTimeoutError(NetworkError error)
        {
            LogError($"[NetworkErrorHandler] 处理超时错误: {error.errorMessage}");
        }

        void HandleSerializationError(NetworkError error)
        {
            LogError($"[NetworkErrorHandler] 处理序列化错误: {error.errorMessage}");
        }

        void HandleServerError(NetworkError error)
        {
            LogError($"[NetworkErrorHandler] 处理服务器错误: {error.errorMessage}");
        }

        void HandleUnknownError(NetworkError error)
        {
            LogError($"[NetworkErrorHandler] 处理未知错误: {error.errorMessage}");
        }

        void AttemptAutoReconnect()
        {
            LogDebug("[NetworkErrorHandler] 执行自动重连");

            if (NetworkConnectionManager.Instance != null)
            {
                NetworkConnectionManager.Instance.Reconnect();
            }
        }

        public void ResolveError(NetworkError error)
        {
            LogDebug($"[NetworkErrorHandler] 错误已解决: {error}");
            OnErrorResolved?.Invoke(error);
            reconnectAttempts = 0;
        }

        public void ClearErrorHistory()
        {
            errorHistory.Clear();
            OnErrorHistoryUpdated?.Invoke(new List<NetworkError>());
            LogDebug("[NetworkErrorHandler] 错误历史已清空");
        }

        public List<NetworkError> GetErrorsByType(NetworkErrorType errorType)
        {
            return errorHistory.FindAll(e => e.errorType == errorType);
        }

        public List<NetworkError> GetRecentErrors(int count)
        {
            int startIndex = Mathf.Max(0, errorHistory.Count - count);
            return errorHistory.GetRange(startIndex, errorHistory.Count - startIndex);
        }

        public NetworkError GetErrorByRequestId(string requestId)
        {
            return errorHistory.Find(e => e.requestId == requestId);
        }

        void TrimErrorHistory()
        {
            while (errorHistory.Count > maxErrorHistorySize)
            {
                errorHistory.RemoveAt(0);
            }
        }

        public void EnableAutoReconnect(bool enable)
        {
            enableAutoReconnect = enable;
            LogDebug($"[NetworkErrorHandler] 自动重连已{(enable ? "启用" : "禁用")}");
        }

        public void SetAutoReconnectSettings(int maxAttempts, float delay)
        {
            maxAutoReconnectAttempts = maxAttempts;
            autoReconnectDelay = delay;
            LogDebug($"[NetworkErrorHandler] 自动重连设置已更新 - 最大尝试次数: {maxAttempts}, 延迟: {delay}秒");
        }

        void LogDebug(string message)
        {
            if (enableErrorLogging)
            {
                Debug.Log(message);
            }
        }

        void LogError(string message)
        {
            if (enableErrorLogging)
            {
                Debug.LogError(message);
            }
        }

        void OnDestroy()
        {
            UnregisterErrorListeners();
            CancelInvoke(nameof(AttemptAutoReconnect));
        }
    }

    public static class NetworkErrorExtensions
    {
        public static bool IsRecoverable(this NetworkError error)
        {
            switch (error.errorType)
            {
                case NetworkErrorType.ConnectionError:
                case NetworkErrorType.TimeoutError:
                    return true;
                case NetworkErrorType.SerializationError:
                case NetworkErrorType.DeserializationError:
                case NetworkErrorType.ServerError:
                case NetworkErrorType.ClientError:
                case NetworkErrorType.UnknownError:
                    return false;
                default:
                    return false;
            }
        }

        public static string GetErrorSeverity(this NetworkError error)
        {
            switch (error.errorType)
            {
                case NetworkErrorType.ConnectionError:
                case NetworkErrorType.TimeoutError:
                    return "警告";
                case NetworkErrorType.SerializationError:
                case NetworkErrorType.DeserializationError:
                    return "错误";
                case NetworkErrorType.ServerError:
                case NetworkErrorType.ClientError:
                    return "严重";
                case NetworkErrorType.UnknownError:
                    return "未知";
                default:
                    return "未知";
            }
        }
    }
}