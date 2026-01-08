using UnityEngine;
using UnityEditor;
using Mirror;
using System.Collections.Generic;
using System.Linq;

namespace NetworkCore.Editor
{
    /// <summary>
    /// 网络同步验证工具
    /// 用于检查Mirror网络配置和同步逻辑的正确性
    /// </summary>
    public class NetworkSyncValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        [MenuItem("Tools/Network/网络同步验证工具")]
        public static void ShowWindow()
        {
            GetWindow<NetworkSyncValidator>("网络同步验证工具");
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mirror网络同步验证工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("此工具用于验证Mirror网络配置和同步逻辑的正确性", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("执行完整验证", GUILayout.Height(30)))
            {
                RunFullValidation();
            }

            EditorGUILayout.Space();

            if (validationResults.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (var result in validationResults)
                {
                    MessageType messageType = MessageType.None;
                    switch (result.severity)
                    {
                        case ValidationSeverity.Error:
                            messageType = MessageType.Error;
                            break;
                        case ValidationSeverity.Warning:
                            messageType = MessageType.Warning;
                            break;
                        case ValidationSeverity.Info:
                            messageType = MessageType.Info;
                            break;
                    }

                    EditorGUILayout.HelpBox(result.message, messageType);

                    if (!string.IsNullOrEmpty(result.filePath))
                    {
                        if (GUILayout.Button($"查看: {result.fileName}", EditorStyles.miniButton))
                        {
                            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(result.filePath));
                        }
                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void RunFullValidation()
        {
            validationResults.Clear();

            ValidateNetworkManager();
            ValidateNetworkPlayer();
            ValidateCharacterModelManager();
            ValidateGameSceneNetworkInitializer();
            ValidateInventorySync();
            ValidateTransportSettings();

            EditorUtility.DisplayDialog("验证完成", $"验证完成！发现 {validationResults.Count(r => r.severity == ValidationSeverity.Error)} 个错误，{validationResults.Count(r => r.severity == ValidationSeverity.Warning)} 个警告。", "确定");
        }

        private void ValidateNetworkManager()
        {
            NetworkManager networkManager = NetworkManager.singleton;
            if (networkManager == null)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "未找到NetworkManager单例！请确保场景中有NetworkManager组件。",
                    fileName = "NetworkManager",
                    filePath = ""
                });
                return;
            }

            if (networkManager.playerPrefab == null)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "NetworkManager的Player Prefab未设置！请设置玩家预制体。",
                    fileName = "NetworkManager",
                    filePath = ""
                });
            }

            Transport transport = Transport.active;
            if (transport == null)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "未找到Transport组件！请添加KCPTransport或TelepathyTransport。",
                    fileName = "Transport",
                    filePath = ""
                });
            }

            validationResults.Add(new ValidationResult
            {
                severity = ValidationSeverity.Info,
                message = "NetworkManager配置检查完成。",
                fileName = "NetworkManager",
                filePath = ""
            });
        }

        private void ValidateNetworkPlayer()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script NetworkPlayer");
            if (guids.Length == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "未找到NetworkPlayer脚本！",
                    fileName = "NetworkPlayer.cs",
                    filePath = ""
                });
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptContent = System.IO.File.ReadAllText(path);

            if (!scriptContent.Contains("public string selectedCharacterId"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "NetworkPlayer缺少selectedCharacterId SyncVar！",
                    fileName = "NetworkPlayer.cs",
                    filePath = path
                });
            }

            if (!scriptContent.Contains("OnCharacterModelLoaded"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Warning,
                    message = "NetworkPlayer缺少OnCharacterModelLoaded回调，可能导致初始化状态检查失败。",
                    fileName = "NetworkPlayer.cs",
                    filePath = path
                });
            }

            validationResults.Add(new ValidationResult
            {
                severity = ValidationSeverity.Info,
                message = "NetworkPlayer脚本检查完成。",
                fileName = "NetworkPlayer.cs",
                filePath = path
            });
        }

        private void ValidateCharacterModelManager()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script CharacterModelManager");
            if (guids.Length == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "未找到CharacterModelManager脚本！",
                    fileName = "CharacterModelManager.cs",
                    filePath = ""
                });
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptContent = System.IO.File.ReadAllText(path);

            if (!scriptContent.Contains("OnModelLoaded"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "CharacterModelManager缺少OnModelLoaded事件！",
                    fileName = "CharacterModelManager.cs",
                    filePath = path
                });
            }

            validationResults.Add(new ValidationResult
            {
                severity = ValidationSeverity.Info,
                message = "CharacterModelManager脚本检查完成。",
                fileName = "CharacterModelManager.cs",
                filePath = path
            });
        }

        private void ValidateGameSceneNetworkInitializer()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script GameSceneNetworkInitializer");
            if (guids.Length == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "未找到GameSceneNetworkInitializer脚本！",
                    fileName = "GameSceneNetworkInitializer.cs",
                    filePath = ""
                });
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptContent = System.IO.File.ReadAllText(path);

            if (!scriptContent.Contains("maxRetries = 3"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Warning,
                    message = "GameSceneNetworkInitializer的角色数据上传验证可能缺少重试机制。",
                    fileName = "GameSceneNetworkInitializer.cs",
                    filePath = path
                });
            }

            if (scriptContent.Contains("maxWaitTime = 5"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Warning,
                    message = "GameSceneNetworkInitializer的角色数据上传验证超时时间可能过短（5秒），建议增加到15秒。",
                    fileName = "GameSceneNetworkInitializer.cs",
                    filePath = path
                });
            }

            validationResults.Add(new ValidationResult
            {
                severity = ValidationSeverity.Info,
                message = "GameSceneNetworkInitializer脚本检查完成。",
                fileName = "GameSceneNetworkInitializer.cs",
                filePath = path
            });
        }

        private void ValidateInventorySync()
        {
            string[] guids = AssetDatabase.FindAssets("t:Script Inventory");
            if (guids.Length == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "未找到Inventory脚本！",
                    fileName = "Inventory.cs",
                    filePath = ""
                });
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            string scriptContent = System.IO.File.ReadAllText(path);

            if (!scriptContent.Contains("SyncVar"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Error,
                    message = "Inventory缺少SyncVar属性！背包数据无法同步到客户端。",
                    fileName = "Inventory.cs",
                    filePath = path
                });
            }

            if (!scriptContent.Contains("CmdSwapSlots"))
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Warning,
                    message = "Inventory可能缺少CmdSwapSlots命令，物品交换功能可能无法正常工作。",
                    fileName = "Inventory.cs",
                    filePath = path
                });
            }

            validationResults.Add(new ValidationResult
            {
                severity = ValidationSeverity.Info,
                message = "Inventory脚本检查完成。",
                fileName = "Inventory.cs",
                filePath = path
            });
        }

        private void ValidateTransportSettings()
        {
            Transport transport = Transport.active;
            if (transport == null)
            {
                return;
            }

            ushort port = 7777;
            try
            {
                var portProperty = transport.GetType().GetProperty("Port");
                if (portProperty != null)
                {
                    port = (ushort)portProperty.GetValue(transport);
                }
            }
            catch
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Warning,
                    message = "无法读取Transport端口设置。",
                    fileName = "Transport",
                    filePath = ""
                });
                return;
            }

            if (port < 1024 || port > 65535)
            {
                validationResults.Add(new ValidationResult
                {
                    severity = ValidationSeverity.Warning,
                    message = $"Transport端口 {port} 超出推荐范围（1024-65535）。",
                    fileName = "Transport",
                    filePath = ""
                });
            }

            validationResults.Add(new ValidationResult
            {
                severity = ValidationSeverity.Info,
                message = $"Transport端口设置正确: {port}",
                fileName = "Transport",
                filePath = ""
            });
        }

        private enum ValidationSeverity
        {
            Info,
            Warning,
            Error
        }

        private class ValidationResult
        {
            public ValidationSeverity severity;
            public string message;
            public string fileName;
            public string filePath;
        }
    }
}
