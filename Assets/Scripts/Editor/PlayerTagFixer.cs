using UnityEngine;
using UnityEditor;

public class PlayerTagFixer : EditorWindow
{
    [MenuItem("Tools/玩家修复/修复Player标签")]
    public static void FixPlayerTag()
    {
        string prefabPath = "Assets/Prefabs/Characters/PlayerContainer.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[玩家标签修复] 未找到PlayerContainer预制体: {prefabPath}");
            return;
        }

        SerializedObject serializedObject = new SerializedObject(prefab);
        SerializedProperty tagProperty = serializedObject.FindProperty("m_Tag");

        if (tagProperty != null)
        {
            tagProperty.stringValue = "Player_new";
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"[玩家标签修复] 已将PlayerContainer预制体的标签设置为: Player_new");
        }
        else
        {
            Debug.LogError($"[玩家标签修复] 无法找到标签属性");
        }
    }

    [MenuItem("Tools/玩家修复/验证Player标签")]
    public static void VerifyPlayerTag()
    {
        string prefabPath = "Assets/Prefabs/Characters/PlayerContainer.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[玩家标签验证] 未找到PlayerContainer预制体: {prefabPath}");
            return;
        }

        string currentTag = prefab.tag;
        Debug.Log($"[玩家标签验证] PlayerContainer预制体的当前标签: {currentTag}");

        if (currentTag == "Player_new")
        {
            Debug.Log($"[玩家标签验证] ✓ 标签正确！资源收集系统将正常工作");
        }
        else
        {
            Debug.LogWarning($"[玩家标签验证] ✗ 标签不正确！当前标签: {currentTag}, 期望标签: Player_new");
            Debug.LogWarning($"[玩家标签验证] 请使用 'Tools/玩家修复/修复Player标签' 菜单进行修复");
        }
    }
}