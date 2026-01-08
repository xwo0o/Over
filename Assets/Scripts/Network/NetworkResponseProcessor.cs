using Mirror;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace NetworkCore
{
    public class NetworkResponseProcessor : NetworkBehaviour
    {
        private static NetworkResponseProcessor instance;
        public static NetworkResponseProcessor Instance => instance;

        private Dictionary<NetworkRequestType, Func<NetworkRequest, NetworkResponse>> requestProcessors = new Dictionary<NetworkRequestType, Func<NetworkRequest, NetworkResponse>>();

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        void Start()
        {
            RegisterDefaultProcessors();
        }

        void RegisterDefaultProcessors()
        {
            RegisterProcessor(NetworkRequestType.CharacterSelection, ProcessCharacterSelection);
            RegisterProcessor(NetworkRequestType.PlayerMovement, ProcessPlayerMovement);
            RegisterProcessor(NetworkRequestType.PlayerAttack, ProcessPlayerAttack);
            RegisterProcessor(NetworkRequestType.ResourceCollection, ProcessResourceCollection);
            RegisterProcessor(NetworkRequestType.BuildingPlacement, ProcessBuildingPlacement);
            RegisterProcessor(NetworkRequestType.GameStateSync, ProcessGameStateSync);
            RegisterProcessor(NetworkRequestType.PlayerStateSync, ProcessPlayerStateSync);
            RegisterProcessor(NetworkRequestType.ChatMessage, ProcessChatMessage);

            Debug.Log("[NetworkResponseProcessor] 默认请求处理器已注册");
        }

        public void RegisterProcessor(NetworkRequestType type, Func<NetworkRequest, NetworkResponse> processor)
        {
            if (requestProcessors.ContainsKey(type))
            {
                requestProcessors[type] = processor;
            }
            else
            {
                requestProcessors.Add(type, processor);
            }
            Debug.Log($"[NetworkResponseProcessor] 注册处理器: {type}");
        }

        public NetworkResponse ProcessRequest(NetworkRequest request)
        {
            Debug.Log($"[NetworkResponseProcessor] 处理请求: {request.requestType}, ID: {request.requestId}");

            if (requestProcessors.ContainsKey(request.requestType))
            {
                try
                {
                    NetworkResponse response = requestProcessors[request.requestType]?.Invoke(request);
                    if (response == null)
                    {
                        response = new NetworkResponse(request.requestId, NetworkResponseCode.ServerError, "处理器返回空响应");
                    }
                    return response;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkResponseProcessor] 处理请求时发生异常: {ex.Message}");
                    return new NetworkResponse(request.requestId, NetworkResponseCode.ServerError, $"服务器错误: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkResponseProcessor] 未找到处理器: {request.requestType}");
                return new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到请求处理器");
            }
        }

        private NetworkResponse ProcessCharacterSelection(NetworkRequest request)
        {
            string characterId = request.GetData<string>("characterId", "");
            string playerId = request.senderId;

            Debug.Log($"[NetworkResponseProcessor] 处理角色选择: 玩家={playerId}, 角色={characterId}");

            if (string.IsNullOrEmpty(characterId))
            {
                return new NetworkResponse(request.requestId, NetworkResponseCode.InvalidData, "角色ID为空");
            }

            NetworkPlayer player = FindPlayerById(playerId);
            if (player == null)
            {
                return new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到玩家对象");
            }

            player.selectedCharacterId = characterId;

            NetworkResponse response = new NetworkResponse(request.requestId, NetworkResponseCode.Success, "角色选择成功");
            response.SetData("characterId", characterId);
            response.SetData("playerId", playerId);

            return response;
        }

        private NetworkResponse ProcessPlayerMovement(NetworkRequest request)
        {
            Vector2 input = request.GetData<Vector2>("input", Vector2.zero);
            string playerId = request.senderId;

            Debug.Log($"[NetworkResponseProcessor] 处理玩家移动: 玩家={playerId}, 输入={input}");

            NetworkPlayer player = FindPlayerById(playerId);
            if (player == null)
            {
                return new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到玩家对象");
            }

            player.CmdSetMovementInput(input);

            return new NetworkResponse(request.requestId, NetworkResponseCode.Success, "移动指令已处理");
        }

        private NetworkResponse ProcessPlayerAttack(NetworkRequest request)
        {
            string playerId = request.senderId;

            Debug.Log($"[NetworkResponseProcessor] 处理玩家攻击: 玩家={playerId}");

            NetworkPlayer player = FindPlayerById(playerId);
            if (player == null)
            {
                return new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到玩家对象");
            }

            player.CmdAttack();

            return new NetworkResponse(request.requestId, NetworkResponseCode.Success, "攻击指令已处理");
        }

        private NetworkResponse ProcessResourceCollection(NetworkRequest request)
        {
            uint resourceNetId = request.GetData<uint>("resourceNetId", 0);
            string playerId = request.senderId;

            Debug.Log($"[NetworkResponseProcessor] 处理资源收集: 玩家={playerId}, 资源={resourceNetId}");

            NetworkPlayer player = FindPlayerById(playerId);
            if (player == null)
            {
                return new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到玩家对象");
            }

            player.CmdTryCollectResource(resourceNetId);

            return new NetworkResponse(request.requestId, NetworkResponseCode.Success, "收集指令已处理");
        }

        private NetworkResponse ProcessBuildingPlacement(NetworkRequest request)
        {
            string buildingId = request.GetData<string>("buildingId", "");
            Vector3 position = request.GetData<Vector3>("position", Vector3.zero);
            Quaternion rotation = request.GetData<Quaternion>("rotation", Quaternion.identity);
            string playerId = request.senderId;

            Debug.Log($"[NetworkResponseProcessor] 处理建筑放置: 玩家={playerId}, 建筑={buildingId}, 位置={position}");

            NetworkBuilding building = FindObjectOfType<NetworkBuilding>();
            if (building == null)
            {
                return new NetworkResponse(request.requestId, NetworkResponseCode.NotFound, "未找到建筑系统");
            }

            NetworkResponse response = new NetworkResponse(request.requestId, NetworkResponseCode.Success, "建筑放置成功");
            response.SetData("buildingId", buildingId);
            response.SetData("position", position);

            return response;
        }

        private NetworkResponse ProcessGameStateSync(NetworkRequest request)
        {
            GamePhase phase = request.GetData<GamePhase>("phase", GamePhase.Lobby);

            Debug.Log($"[NetworkResponseProcessor] 处理游戏状态同步: 阶段={phase}");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.SetPhase(phase);
            }

            NetworkResponse response = new NetworkResponse(request.requestId, NetworkResponseCode.Success, "游戏状态已同步");
            response.SetData("phase", phase);

            return response;
        }

        private NetworkResponse ProcessPlayerStateSync(NetworkRequest request)
        {
            string playerId = request.senderId;
            Dictionary<string, object> stateData = request.GetData<Dictionary<string, object>>("stateData", new Dictionary<string, object>());

            Debug.Log($"[NetworkResponseProcessor] 处理玩家状态同步: 玩家={playerId}");

            NetworkResponse response = new NetworkResponse(request.requestId, NetworkResponseCode.Success, "玩家状态已同步");
            response.SetData("playerId", playerId);

            return response;
        }

        private NetworkResponse ProcessChatMessage(NetworkRequest request)
        {
            string message = request.GetData<string>("message", "");
            string playerId = request.senderId;

            Debug.Log($"[NetworkResponseProcessor] 处理聊天消息: 玩家={playerId}, 消息={message}");

            NetworkResponse response = new NetworkResponse(request.requestId, NetworkResponseCode.Success, "消息已广播");
            response.SetData("playerId", playerId);
            response.SetData("message", message);

            BroadcastResponse(response);

            return null;
        }

        private NetworkPlayer FindPlayerById(string playerId)
        {
            if (uint.TryParse(playerId, out uint netId))
            {
                try
                {
                    foreach (var conn in NetworkServer.connections.Values)
                    {
                        if (conn != null && conn.identity != null && conn.identity.netId == netId)
                        {
                            NetworkPlayer player = conn.identity.GetComponent<NetworkPlayer>();
                            if (player != null)
                            {
                                return player;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NetworkResponseProcessor] 查找玩家时发生异常: {ex.Message}");
                }
            }
            return null;
        }

        private void BroadcastResponse(NetworkResponse response)
        {
            if (NetworkMessageHandler.Instance != null)
            {
                NetworkMessageHandler.Instance.BroadcastResponse(response);
            }
        }

        void OnDestroy()
        {
            requestProcessors.Clear();
        }
    }
}