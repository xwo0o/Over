using UnityEngine;
using UnityEditor;
using Mirror;

namespace NetworkTools
{
    /// <summary>
    /// NetworkPlayer验证工具 - 确保NetworkPlayer预制体正确配置了NetworkTransform组件
    /// </summary>
    public class NetworkPlayerValidator : EditorWindow
    {
        [MenuItem("Tools/Network/验证NetworkPlayer配置")]
        public static void ShowWindow()
        {
            GetWindow<NetworkPlayerValidator>("NetworkPlayer验证器");
        }

        private void OnGUI()
        {
            GUILayout.Label("NetworkPlayer配置验证工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("验证当前选中的NetworkPlayer"))
            {
                ValidateSelectedNetworkPlayer();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("查找并验证所有NetworkPlayer预制体"))
            {
                ValidateAllNetworkPlayerPrefabs();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("自动修复NetworkPlayer配置"))
            {
                AutoFixNetworkPlayerConfiguration();
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox("此工具用于验证NetworkPlayer是否正确配置了NetworkTransform组件，以确保位置同步正常工作。", MessageType.Info);
        }

        private void ValidateSelectedNetworkPlayer()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个NetworkPlayer对象或预制体", "确定");
                return;
            }

            NetworkPlayer networkPlayer = selected.GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                EditorUtility.DisplayDialog("错误", "选中的对象没有NetworkPlayer组件", "确定");
                return;
            }

            ValidateNetworkPlayer(selected, networkPlayer);
        }

        private void ValidateAllNetworkPlayerPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int foundCount = 0;
            int validCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null && prefab.GetComponent<NetworkPlayer>() != null)
                {
                    foundCount++;
                    NetworkPlayer networkPlayer = prefab.GetComponent<NetworkPlayer>();
                    if (ValidateNetworkPlayer(prefab, networkPlayer, false))
                    {
                        validCount++;
                    }
                }
            }

            EditorUtility.DisplayDialog("验证结果", $"找到 {foundCount} 个NetworkPlayer预制体\n其中 {validCount} 个配置正确", "确定");
        }

        private bool ValidateNetworkPlayer(GameObject obj, NetworkPlayer networkPlayer, bool showDialog = true)
        {
            bool isValid = true;
            string message = $"NetworkPlayer验证结果: {obj.name}\n\n";

            // 检查NetworkTransformReliable组件
            NetworkTransformReliable networkTransform = obj.GetComponent<NetworkTransformReliable>();
            if (networkTransform == null)
            {
                message += "❌ 缺少NetworkTransformReliable组件\n";
                isValid = false;
            }
            else
            {
                message += "✓ NetworkTransformReliable组件已添加\n";

                // 验证NetworkTransformReliable配置
                if (networkTransform.syncDirection != Mirror.SyncDirection.ServerToClient)
                {
                    message += "⚠ NetworkTransformReliable.syncDirection应设置为ServerToClient（服务器权威）\n";
                    isValid = false;
                }
                else
                {
                    message += "✓ NetworkTransformReliable.syncDirection配置正确（服务器权威）\n";
                }

                if (networkTransform.positionPrecision > 0.01f)
                {
                    message += "⚠ NetworkTransformReliable.positionPrecision建议设置为0.01f或更小\n";
                }
                else
                {
                    message += "✓ NetworkTransformReliable.positionPrecision配置合理\n";
                }

                if (networkTransform.rotationSensitivity > 0.01f)
                {
                    message += "⚠ NetworkTransformReliable.rotationSensitivity建议设置为0.01f或更小\n";
                }
                else
                {
                    message += "✓ NetworkTransformReliable.rotationSensitivity配置合理\n";
                }

                if (!networkTransform.interpolatePosition)
                {
                    message += "⚠ NetworkTransformReliable.interpolatePosition建议设置为true\n";
                }
                else
                {
                    message += "✓ NetworkTransformReliable.interpolatePosition配置合理\n";
                }

                if (!networkTransform.interpolateRotation)
                {
                    message += "⚠ NetworkTransformReliable.interpolateRotation建议设置为true\n";
                }
                else
                {
                    message += "✓ NetworkTransformReliable.interpolateRotation配置合理\n";
                }
            }

            // 检查NetworkIdentity组件
            NetworkIdentity networkIdentity = obj.GetComponent<NetworkIdentity>();
            if (networkIdentity == null)
            {
                message += "❌ 缺少NetworkIdentity组件\n";
                isValid = false;
            }
            else
            {
                message += "✓ NetworkIdentity组件已添加\n";
            }

            message += "\n" + (isValid ? "✓ 配置验证通过" : "❌ 配置存在问题");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(isValid ? "验证通过" : "验证失败", message, "确定");
            }

            return isValid;
        }

        private void AutoFixNetworkPlayerConfiguration()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个NetworkPlayer对象或预制体", "确定");
                return;
            }

            NetworkPlayer networkPlayer = selected.GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                EditorUtility.DisplayDialog("错误", "选中的对象没有NetworkPlayer组件", "确定");
                return;
            }

            // 添加NetworkTransformReliable组件（如果不存在）
            NetworkTransformReliable networkTransform = selected.GetComponent<NetworkTransformReliable>();
            if (networkTransform == null)
            {
                networkTransform = selected.AddComponent<NetworkTransformReliable>();
                Debug.Log($"[NetworkPlayerValidator] 已为 {selected.name} 添加NetworkTransformReliable组件");
            }

            // 配置NetworkTransformReliable
            networkTransform.syncDirection = Mirror.SyncDirection.ServerToClient;
            networkTransform.positionPrecision = 0.01f;
            networkTransform.rotationSensitivity = 0.01f;
            networkTransform.interpolatePosition = true;
            networkTransform.interpolateRotation = true;

            // 标记为脏，保存更改
            EditorUtility.SetDirty(selected);

            if (PrefabUtility.IsPartOfAnyPrefab(selected))
            {
                PrefabUtility.SavePrefabAsset(PrefabUtility.GetOutermostPrefabInstanceRoot(selected));
                Debug.Log($"[NetworkPlayerValidator] 已保存预制体更改: {selected.name}");
            }

            EditorUtility.DisplayDialog("修复完成", $"已自动修复 {selected.name} 的NetworkTransformReliable配置\n\n配置详情:\n- syncDirection: ServerToClient（服务器权威）\n- positionPrecision: 0.01f\n- rotationSensitivity: 0.01f\n- interpolatePosition: true\n- interpolateRotation: true", "确定");
        }
    }
}