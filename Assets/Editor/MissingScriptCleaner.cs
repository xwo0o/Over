using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public class MissingScriptCleaner : EditorWindow
{
    private List<GameObject> objectsWithMissingScripts = new List<GameObject>();
    private Dictionary<GameObject, int> missingScriptCounts = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, string> prefabPaths = new Dictionary<GameObject, string>();
    private int totalMissingScripts = 0;
    private Vector2 scrollPosition;
    private bool showDetails = false;

    [MenuItem("工具/Missing Script 清理工具")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptCleaner>("Missing Script 清理工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("Missing Script 清理工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("扫描场景", GUILayout.Height(30)))
        {
            ScanScene();
        }

        if (GUILayout.Button("清理预制体中的 Missing Scripts", GUILayout.Height(30)))
        {
            CleanPrefabMissingScripts();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("此工具会修改预制体本身，所有实例会自动更新。", MessageType.Info);

        GUILayout.Space(10);

        if (objectsWithMissingScripts.Count > 0)
        {
            EditorGUILayout.HelpBox($"发现 {objectsWithMissingScripts.Count} 个对象包含 Missing Scripts，共 {totalMissingScripts} 个", MessageType.Warning);

            showDetails = EditorGUILayout.Foldout(showDetails, "显示详细信息");

            if (showDetails)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                for (int i = objectsWithMissingScripts.Count - 1; i >= 0; i--)
                {
                    var obj = objectsWithMissingScripts[i];
                    
                    if (obj == null)
                    {
                        objectsWithMissingScripts.RemoveAt(i);
                        continue;
                    }
                    
                    int missingCount = missingScriptCounts.ContainsKey(obj) ? missingScriptCounts[obj] : 0;
                    string prefabPath = prefabPaths.ContainsKey(obj) ? prefabPaths[obj] : "场景对象";
                    
                    EditorGUILayout.ObjectField(obj.name, obj, typeof(GameObject), true);
                    EditorGUILayout.LabelField($"  Missing Scripts: {missingCount}", EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(prefabPath) && prefabPath != "场景对象")
                    {
                        EditorGUILayout.LabelField($"  预制体路径: {prefabPath}", EditorStyles.miniLabel);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
        else if (totalMissingScripts == 0 && objectsWithMissingScripts.Count == 0)
        {
            EditorGUILayout.HelpBox("场景中未发现 Missing Scripts", MessageType.Info);
        }
    }

    private void ScanScene()
    {
        objectsWithMissingScripts.Clear();
        missingScriptCounts.Clear();
        prefabPaths.Clear();
        totalMissingScripts = 0;

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            int missingCount = CountMissingScripts(obj);

            if (missingCount > 0)
            {
                objectsWithMissingScripts.Add(obj);
                missingScriptCounts[obj] = missingCount;
                totalMissingScripts += missingCount;

                if (PrefabUtility.IsPartOfAnyPrefab(obj))
                {
                    string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj));
                    prefabPaths[obj] = prefabPath;
                }
                else
                {
                    prefabPaths[obj] = "场景对象";
                }
            }
        }

        int prefabCount = prefabPaths.Values.Count(path => path != "场景对象");
        EditorUtility.DisplayDialog("扫描完成", $"扫描完成！\n发现 {objectsWithMissingScripts.Count} 个对象包含 Missing Scripts\n共 {totalMissingScripts} 个\n其中 {prefabCount} 个来自预制体", "确定");
    }

    private int CountMissingScripts(GameObject obj)
    {
        if (obj == null) return 0;

        SerializedObject serializedObject = new SerializedObject(obj);
        SerializedProperty componentProperty = serializedObject.FindProperty("m_Component");

        if (componentProperty == null || !componentProperty.isArray) return 0;

        int missingCount = 0;

        for (int i = 0; i < componentProperty.arraySize; i++)
        {
            SerializedProperty componentRef = componentProperty.GetArrayElementAtIndex(i);
            SerializedProperty componentPPtr = componentRef.FindPropertyRelative("component");

            if (componentPPtr != null && componentPPtr.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (componentPPtr.objectReferenceValue == null)
                {
                    missingCount++;
                }
            }
        }

        return missingCount;
    }

    private void CleanPrefabMissingScripts()
    {
        string prefabFolderPath = "Assets/SD Unity-Chan Haon Custom/Prefabs";

        if (!Directory.Exists(Path.GetFullPath(prefabFolderPath)))
        {
            EditorUtility.DisplayDialog("错误", $"预设体文件夹不存在: {prefabFolderPath}", "确定");
            return;
        }

        string[] prefabFiles = Directory.GetFiles(Path.GetFullPath(prefabFolderPath), "*.prefab", SearchOption.AllDirectories);

        if (prefabFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", $"在 {prefabFolderPath} 中没有找到预设体文件", "确定");
            return;
        }

        string prefabList = string.Join("\n", prefabFiles.Take(10).Select(Path.GetFileName));
        if (prefabFiles.Length > 10)
        {
            prefabList += $"\n... 还有 {prefabFiles.Length - 10} 个";
        }

        if (!EditorUtility.DisplayDialog("确认清理", $"将修改以下 {prefabFiles.Length} 个预设体：\n\n{prefabList}\n\n此操作将修改预设体本身，所有实例会自动更新。\n确定要继续吗？", "确定", "取消"))
        {
            return;
        }

        int cleanedCount = 0;
        int failedCount = 0;
        int processedPrefabs = 0;
        float totalPrefabs = prefabFiles.Length;
        List<string> failedPrefabs = new List<string>();

        foreach (string prefabPath in prefabFiles)
        {
            string relativePath = prefabPath.Replace(Path.GetFullPath("Assets"), "Assets").Replace("\\", "/");

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);

            if (prefabAsset != null)
            {
                int removedCount = RemoveMissingScriptsFromPrefab(prefabAsset);

                if (removedCount > 0)
                {
                    cleanedCount += removedCount;
                    EditorUtility.SetDirty(prefabAsset);
                    AssetDatabase.SaveAssetIfDirty(prefabAsset);
                }
                else
                {
                    failedCount++;
                    failedPrefabs.Add(relativePath);
                }
            }
            else
            {
                failedCount++;
                failedPrefabs.Add(relativePath);
            }

            processedPrefabs++;
            EditorUtility.DisplayProgressBar("清理预设体中", $"正在清理: {Path.GetFileName(prefabPath)} ({processedPrefabs}/{totalPrefabs})", processedPrefabs / totalPrefabs);
        }

        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"预制体清理完成！\n\n成功删除: {cleanedCount} 个 Missing Script\n失败: {failedCount} 个预制体";

        if (failedCount > 0 && failedPrefabs.Count > 0)
        {
            message += $"\n\n失败的预制体:\n{string.Join("\n", failedPrefabs.Take(10))}";
            if (failedPrefabs.Count > 10)
            {
                message += $"\n... 还有 {failedPrefabs.Count - 10} 个";
            }
        }

        message += "\n\n所有实例已自动更新。";

        EditorUtility.DisplayDialog("清理完成", message, "确定");

        ScanScene();
    }

    private int RemoveMissingScriptsFromPrefab(GameObject prefab)
    {
        if (prefab == null) return 0;

        int removedCount = 0;
        string prefabPath = AssetDatabase.GetAssetPath(prefab);

        try
        {
            if (!File.Exists(prefabPath))
            {
                Debug.LogError($"预制体文件不存在: {prefabPath}");
                return 0;
            }

            string prefabContent = File.ReadAllText(prefabPath);

            if (string.IsNullOrEmpty(prefabContent))
            {
                Debug.LogError($"预制体文件为空: {prefabPath}");
                return 0;
            }

            removedCount = RemoveMissingScriptsFromYAML(ref prefabContent);

            if (removedCount > 0)
            {
                File.WriteAllText(prefabPath, prefabContent);
                AssetDatabase.ImportAsset(prefabPath);
                Debug.Log($"已从预制体 {prefab.name} 删除 {removedCount} 个 Missing Script");
            }
            else
            {
                Debug.Log($"预制体 {prefab.name} 没有发现 Missing Script");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理预制体 {prefab.name} 时出错: {e.Message}\n{e.StackTrace}");
        }

        return removedCount;
    }

    private int RemoveMissingScriptsFromYAML(ref string yamlContent)
    {
        if (string.IsNullOrEmpty(yamlContent)) return 0;

        string[] lines = yamlContent.Split('\n');
        List<string> modifiedLines = new List<string>();
        int removedCount = 0;
        bool inComponentSection = false;
        bool skipCurrentComponent = false;
        int currentComponentIndent = 0;
        string lastComponentLine = "";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int indentLevel = GetIndentLevel(line);
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("m_Component:"))
            {
                inComponentSection = true;
                skipCurrentComponent = false;
                modifiedLines.Add(line);
                continue;
            }

            if (inComponentSection)
            {
                if (trimmedLine.StartsWith("- component:"))
                {
                    skipCurrentComponent = false;
                    currentComponentIndent = indentLevel;
                    lastComponentLine = line;
                    continue;
                }

                if (!skipCurrentComponent && trimmedLine.StartsWith("component:"))
                {
                    Match match = Regex.Match(trimmedLine, @"component:\s*\{fileID:\s*(\d+)\}");

                    if (match.Success)
                    {
                        int fileID = int.Parse(match.Groups[1].Value);

                        if (fileID == 0)
                        {
                            Debug.Log($"检测到 Missing Script (fileID: 0) 在行 {i + 1}: {trimmedLine}");
                            skipCurrentComponent = true;
                            removedCount++;
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(lastComponentLine))
                    {
                        modifiedLines.Add(lastComponentLine);
                        lastComponentLine = "";
                    }
                    modifiedLines.Add(line);
                    continue;
                }

                if (skipCurrentComponent)
                {
                    if (indentLevel <= currentComponentIndent && !trimmedLine.StartsWith("- component:") && !trimmedLine.StartsWith("component:"))
                    {
                        skipCurrentComponent = false;
                        lastComponentLine = "";
                    }
                    else
                    {
                        continue;
                    }
                }

                if (indentLevel <= GetIndentLevel("  m_Component:") && !trimmedLine.StartsWith("- component:") && !trimmedLine.StartsWith("component:"))
                {
                    inComponentSection = false;
                    skipCurrentComponent = false;
                    lastComponentLine = "";
                }
            }

            modifiedLines.Add(line);
        }

        yamlContent = string.Join("\n", modifiedLines);
        Debug.Log($"YAML删除完成: 实际删除 {removedCount} 个 Missing Script");
        return removedCount;
    }

    private int GetIndentLevel(string line)
    {
        int count = 0;
        foreach (char c in line)
        {
            if (c == ' ' || c == '\t')
            {
                count++;
            }
            else
            {
                break;
            }
        }
        return count;
    }

    private int RemoveMissingScriptsRecursive(GameObject obj)
    {
        if (obj == null) return 0;

        int removedCount = 0;

        removedCount += RemoveMissingScriptsFromObject(obj);

        foreach (Transform child in obj.transform)
        {
            removedCount += RemoveMissingScriptsRecursive(child.gameObject);
        }

        return removedCount;
    }

    private int RemoveMissingScriptsFromObject(GameObject obj)
    {
        if (obj == null) return 0;

        int removedCount = 0;

        try
        {
            int missingCount = CountMissingScripts(obj);

            if (missingCount > 0)
            {
                SerializedObject serializedObject = new SerializedObject(obj);
                SerializedProperty componentProperty = serializedObject.FindProperty("m_Component");

                if (componentProperty != null && componentProperty.isArray)
                {
                    List<int> indicesToRemove = new List<int>();

                    for (int i = 0; i < componentProperty.arraySize; i++)
                    {
                        SerializedProperty componentRef = componentProperty.GetArrayElementAtIndex(i);
                        SerializedProperty componentPPtr = componentRef.FindPropertyRelative("component");

                        if (componentPPtr != null && componentPPtr.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (componentPPtr.objectReferenceValue == null)
                            {
                                indicesToRemove.Add(i);
                            }
                        }
                    }

                    for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            int indexToRemove = indicesToRemove[i];
                            componentProperty.DeleteArrayElementAtIndex(indexToRemove);
                            removedCount++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"无法删除 {obj.name} 的 Missing Script: {e.Message}");
                        }
                    }

                    try
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"应用 {obj.name} 的更改时出错: {e.Message}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"处理对象 {obj.name} 时出错: {e.Message}");
        }

        return removedCount;
    }
}
