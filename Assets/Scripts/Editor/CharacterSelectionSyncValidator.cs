using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NetworkCore.Editor
{
    public class CharacterSelectionSyncValidator : EditorWindow
    {
        [MenuItem("Tools/Network/角色选择同步验证器")]
        public static void ShowWindow()
        {
            GetWindow<CharacterSelectionSyncValidator>("角色选择同步验证器");
        }

        private Vector2 scrollPosition;
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("角色选择同步验证器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("此工具用于验证角色选择同步逻辑是否符合Mirror服务器权威原则", MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("验证角色选择同步逻辑", GUILayout.Height(30)))
            {
                ValidateCharacterSelectionSync();
            }

            EditorGUILayout.Space();

            if (validationResults.Count > 0)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (var result in validationResults)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField(result.Message, result.IsError ? EditorStyles.boldLabel : EditorStyles.label);
                    EditorGUILayout.LabelField($"文件: {result.FilePath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"行号: {result.LineNumber}", EditorStyles.miniLabel);

                    if (!string.IsNullOrEmpty(result.Suggestion))
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("建议:", EditorStyles.boldLabel);
                        EditorGUILayout.TextArea(result.Suggestion, GUILayout.Height(60));
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ValidateCharacterSelectionSync()
        {
            validationResults.Clear();

            string[] networkPlayerGuids = AssetDatabase.FindAssets("NetworkPlayer t:Script");
            string[] characterModelManagerGuids = AssetDatabase.FindAssets("CharacterModelManager t:Script");
            string[] gameNetworkManagerGuids = AssetDatabase.FindAssets("GameNetworkManager t:Script");

            var allGuids = networkPlayerGuids.Concat(characterModelManagerGuids).Concat(gameNetworkManagerGuids).Distinct().ToArray();

            foreach (string guid in allGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string[] lines = System.IO.File.ReadAllLines(assetPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    if (line.Contains("selectedCharacterId") && line.Contains("=") && !line.Contains("==") && !line.Contains("!="))
                    {
                        if (line.Contains("CmdSelectCharacter"))
                        {
                            continue;
                        }

                        if (line.Contains("selectedCharacterId ="))
                        {
                            bool isValidContext = false;

                            for (int j = i - 5; j < i && j >= 0; j++)
                            {
                                if (lines[j].Contains("CmdSelectCharacter") || lines[j].Contains("[Command]"))
                                {
                                    isValidContext = true;
                                    break;
                                }
                            }

                            if (!isValidContext)
                            {
                                validationResults.Add(new ValidationResult
                                {
                                    IsError = true,
                                    Message = "发现违反服务器权威原则的代码：在非Command方法中直接修改selectedCharacterId",
                                    FilePath = assetPath,
                                    LineNumber = i + 1,
                                    Suggestion = "角色ID必须且只能通过CmdSelectCharacter命令设置。服务器端不应在超时或其他情况下自动设置默认角色。"
                                });
                            }
                        }
                    }

                    if (line.Contains("selectedCharacterId") && line.Contains("Scout") && line.Contains("="))
                    {
                        validationResults.Add(new ValidationResult
                        {
                            IsError = true,
                            Message = "发现硬编码默认角色设置：使用'Scout'作为默认角色",
                            FilePath = assetPath,
                            LineNumber = i + 1,
                            Suggestion = "移除硬编码的默认角色设置。服务器端应等待客户端通过CmdSelectCharacter发送角色ID，而不是自动设置默认值。"
                        });
                    }
                }
            }

            if (validationResults.Count == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    IsError = false,
                    Message = "✓ 所有角色选择同步逻辑符合Mirror服务器权威原则",
                    FilePath = "N/A",
                    LineNumber = 0,
                    Suggestion = ""
                });
            }
        }

        private class ValidationResult
        {
            public bool IsError { get; set; }
            public string Message { get; set; }
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
            public string Suggestion { get; set; }
        }
    }
}
