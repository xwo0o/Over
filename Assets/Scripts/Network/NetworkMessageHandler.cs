using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace NetworkCore
{
    public class NetworkMessageHandler : NetworkBehaviour
    {
        private static NetworkMessageHandler instance;
        public static NetworkMessageHandler Instance => instance;

        private Dictionary<string, Action<NetworkResponse>> pendingRequests = new Dictionary<string, Action<NetworkResponse>>();
        private Dictionary<NetworkRequestType, Action<NetworkRequest>> requestHandlers = new Dictionary<NetworkRequestType, Action<NetworkRequest>>();
        private Dictionary<NetworkRequestType, Action<NetworkResponse>> responseHandlers = new Dictionary<NetworkRequestType, Action<NetworkResponse>>();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public void RegisterRequestHandler(NetworkRequestType type, Action<NetworkRequest> handler)
        {
            if (requestHandlers.ContainsKey(type))
            {
                requestHandlers[type] = handler;
            }
            else
            {
                requestHandlers.Add(type, handler);
            }
            Debug.Log($"[NetworkMessageHandler] 注册请求处理器: {type}");
        }

        public void RegisterResponseHandler(NetworkRequestType type, Action<NetworkResponse> handler)
        {
            if (responseHandlers.ContainsKey(type))
            {
                responseHandlers[type] = handler;
            }
            else
            {
                responseHandlers.Add(type, handler);
            }
            Debug.Log($"[NetworkMessageHandler] 注册响应处理器: {type}");
        }

        public void SendRequest(NetworkRequest request, Action<NetworkResponse> callback = null, float timeout = 180f)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.LogError($"[NetworkMessageHandler] 网络未连接，无法发送请求: {request.requestType}");
                try
                {
                    callback?.Invoke(new NetworkResponse(request.requestId, NetworkResponseCode.Failed, "网络未连接"));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkMessageHandler] 执行失败响应回调时发生异常: {ex.Message}");
                }
                return;
            }

            request.senderId = NetworkClient.localPlayer?.netId.ToString() ?? "unknown";
            request.timestamp = Time.time;

            if (callback != null)
            {
                pendingRequests[request.requestId] = callback;
                StartCoroutine(RequestTimeoutCoroutine(request.requestId, timeout));
            }

            CmdSendRequest(request);
            Debug.Log($"[NetworkMessageHandler] 发送网络请求: {request.requestType}, ID: {request.requestId}");
        }

        System.Collections.IEnumerator RequestTimeoutCoroutine(string requestId, float timeout)
        {
            yield return new WaitForSeconds(timeout);

            if (pendingRequests.ContainsKey(requestId))
            {
                var callback = pendingRequests[requestId];
                pendingRequests.Remove(requestId);
                try
                {
                    callback?.Invoke(new NetworkResponse(requestId, NetworkResponseCode.Timeout, "请求超时"));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkMessageHandler] 执行超时响应回调时发生异常: {ex.Message}");
                }
                Debug.LogWarning($"[NetworkMessageHandler] 请求超时: {requestId}");
            }
        }

        [Command]
        void CmdSendRequest(NetworkRequest request)
        {
            Debug.Log($"[NetworkMessageHandler] 服务器收到请求: {request.requestType}, ID: {request.requestId}");

            if (requestHandlers.ContainsKey(request.requestType))
            {
                requestHandlers[request.requestType]?.Invoke(request);
            }
            else
            {
                Debug.LogWarning($"[NetworkMessageHandler] 未找到请求处理器: {request.requestType}");
                SendResponse(new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到请求处理器"));
            }
        }

        [TargetRpc]
        void SendResponse(NetworkResponse response)
        {
            Debug.Log($"[NetworkMessageHandler] 收到响应: {response.responseCode}, ID: {response.requestId}");

            if (pendingRequests.ContainsKey(response.requestId))
            {
                var callback = pendingRequests[response.requestId];
                pendingRequests.Remove(response.requestId);
                try
                {
                    callback?.Invoke(response);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkMessageHandler] 执行响应回调时发生异常: {ex.Message}");
                }
            }

            if (responseHandlers.ContainsKey(NetworkRequestType.Custom))
            {
                responseHandlers[NetworkRequestType.Custom]?.Invoke(response);
            }
        }

        [ClientRpc]
        void RpcBroadcastResponse(NetworkResponse response)
        {
            Debug.Log($"[NetworkMessageHandler] 广播响应: {response.responseCode}, ID: {response.requestId}");

            if (responseHandlers.ContainsKey(NetworkRequestType.Custom))
            {
                responseHandlers[NetworkRequestType.Custom]?.Invoke(response);
            }
        }

        public void SendResponseToClient(NetworkConnectionToClient conn, NetworkResponse response)
        {
            TargetSendResponse(conn, response);
        }

        [TargetRpc]
        void TargetSendResponse(NetworkConnectionToClient conn, NetworkResponse response)
        {
            Debug.Log($"[NetworkMessageHandler] 向客户端发送响应: {response.responseCode}, ID: {response.requestId}");

            if (pendingRequests.ContainsKey(response.requestId))
            {
                var callback = pendingRequests[response.requestId];
                pendingRequests.Remove(response.requestId);
                try
                {
                    callback?.Invoke(response);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkMessageHandler] 执行响应回调时发生异常: {ex.Message}");
                }
            }
        }

        public void BroadcastResponse(NetworkResponse response)
        {
            RpcBroadcastResponse(response);
        }

        void OnDestroy()
        {
            pendingRequests.Clear();
            requestHandlers.Clear();
            responseHandlers.Clear();
        }
    }
}