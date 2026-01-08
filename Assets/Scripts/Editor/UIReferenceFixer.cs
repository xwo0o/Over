using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class UIReferenceFixer : EditorWindow
{
    [MenuItem("Tools/UI检查/修复所有UI引用")]
    public static void ShowWindow()
    {
        GetWindow<UIReferenceFixer>("UI引用修复器");
    }

    void OnGUI()
    {
        GUILayout.Label("UI引用修复器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("修复步骤:", EditorStyles.boldLabel);
        GUILayout.Label("1. 检查pickupUI预制体字段引用");
        GUILayout.Label("2. 检查GameSceneInitializer预制体引用");
        GUILayout.Label("3. 自动修复缺失的引用");
        GUILayout.Space(10);

        if (GUILayout.Button("检查并修复所有引用", GUILayout.Height(40)))
        {
            FixAllReferences();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("仅检查引用状态", GUILayout.Height(30)))
        {
            CheckAllReferences();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("显示pickupUI预制体结构", GUILayout.Height(30)))
        {
            ShowPickupUIStructure();
        }
    }

    void CheckAllReferences()
    {
        Debug.Log("[UI引用修复器] 开始检查所有UI引用...");

        CheckPickupUIPrefab();
        CheckGameSceneInitializer();

        Debug.Log("[UI引用修复器] 检查完成");
    }

    void FixAllReferences()
    {
        Debug.Log("[UI引用修复器] 开始修复所有UI引用...");

        FixPickupUIPrefab();
        FixGameSceneInitializer();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[UI引用修复器] 修复完成");
    }

    void CheckPickupUIPrefab()
    {
        string prefabPath = "Assets/Prefabs/UI/pickupUI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[UI引用修复器] 未找到pickupUI预制体: {prefabPath}");
            return;
        }

        ResourcePickupUIController controller = prefab.GetComponent<ResourcePickupUIController>();
        if (controller == null)
        {
            Debug.LogError("[UI引用修复器] pickupUI预制体上没有ResourcePickupUIController组件");
            return;
        }

        Debug.Log("[UI引用修复器] pickupUI预制体字段状态:");
        LogFieldStatus("pickupUIPanel", controller.pickupUIPanel != null);
        LogFieldStatus("amountText", controller.amountText != null);
    }

    void FixPickupUIPrefab()
    {
        string prefabPath = "Assets/Prefabs/UI/pickupUI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[UI引用修复器] 未找到pickupUI预制体: {prefabPath}");
            return;
        }

        ResourcePickupUIController controller = prefab.GetComponent<ResourcePickupUIController>();
        if (controller == null)
        {
            Debug.LogError("[UI引用修复器] pickupUI预制体上没有ResourcePickupUIController组件");
            return;
        }

        bool hasChanges = false;

        if (controller.pickupUIPanel == null)
        {
            GameObject panel = FindChildByName(prefab.transform, "PickupPanel") ??
                              FindChildByName(prefab.transform, "Panel") ??
                              FindChildByName(prefab.transform, "UIPanel");

            if (panel != null)
            {
                controller.pickupUIPanel = panel;
                hasChanges = true;
                Debug.Log($"[UI引用修复器] 已设置pickupUIPanel: {panel.name}");
            }
            else
            {
                Debug.LogWarning("[UI引用修复器] 未找到pickupUIPanel子对象");
            }
        }

        if (controller.amountText == null)
        {
            GameObject amountTextObj = FindChildByName(prefab.transform, "AmountText") ??
                                     FindChildByName(prefab.transform, "CountText") ??
                                     FindChildByName(prefab.transform, "Amount");

            if (amountTextObj != null)
            {
                controller.amountText = amountTextObj.GetComponent<TextMeshProUGUI>();
                if (controller.amountText != null)
                {
                    hasChanges = true;
                    Debug.Log($"[UI引用修复器] 已设置amountText: {amountTextObj.name}");
                }
                else
                {
                    Debug.LogWarning($"[UI引用修复器] {amountTextObj.name}上没有TextMeshProUGUI组件");
                }
            }
            else
            {
                Debug.LogWarning("[UI引用修复器] 未找到amountText子对象");
            }
        }

        if (hasChanges)
        {
            EditorUtility.SetDirty(prefab);
            Debug.Log("[UI引用修复器] pickupUI预制体已更新");
        }
    }

    void CheckGameSceneInitializer()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameSceneInitializer");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[UI引用修复器] 未找到GameSceneInitializer组件");
            return;
        }

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (obj != null)
            {
                GameSceneInitializer initializer = obj.GetComponent<GameSceneInitializer>();
                if (initializer != null)
                {
                    Debug.Log($"[UI引用修复器] GameSceneInitializer ({assetPath}):");
                    
                    SerializedObject serializedObject = new SerializedObject(initializer);
                    SerializedProperty pickupUIPrefabProp = serializedObject.FindProperty("pickupUIPrefab");
                    
                    if (pickupUIPrefabProp != null)
                    {
                        LogFieldStatus("pickupUIPrefab", pickupUIPrefabProp.objectReferenceValue != null);
                    }
                    else
                    {
                        Debug.LogError("[UI引用修复器] 无法找到pickupUIPrefab字段");
                    }
                }
            }
        }
    }

    void FixGameSceneInitializer()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameSceneInitializer");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[UI引用修复器] 未找到GameSceneInitializer组件");
            return;
        }

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (obj != null)
            {
                GameSceneInitializer initializer = obj.GetComponent<GameSceneInitializer>();
                if (initializer != null)
                {
                    SerializedObject serializedObject = new SerializedObject(initializer);
                    SerializedProperty pickupUIPrefabProp = serializedObject.FindProperty("pickupUIPrefab");
                    
                    if (pickupUIPrefabProp != null && pickupUIPrefabProp.objectReferenceValue == null)
                    {
                        GameObject pickupUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/pickupUI.prefab");
                        if (pickupUIPrefab != null)
                        {
                            pickupUIPrefabProp.objectReferenceValue = pickupUIPrefab;
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(obj);
                            Debug.Log($"[UI引用修复器] 已设置GameSceneInitializer的pickupUIPrefab引用: {assetPath}");
                        }
                        else
                        {
                            Debug.LogError("[UI引用修复器] 无法加载pickupUI预制体");
                        }
                    }
                }
            }
        }
    }

    void ShowPickupUIStructure()
    {
        string prefabPath = "Assets/Prefabs/UI/pickupUI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[UI引用修复器] 未找到pickupUI预制体: {prefabPath}");
            return;
        }

        Debug.Log("[UI引用修复器] pickupUI预制体层级结构:");
        LogHierarchy(prefab.transform, 0);
    }

    GameObject FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }

            GameObject found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    void LogFieldStatus(string fieldName, bool isSet)
    {
        string status = isSet ? "✓ 已设置" : "✗ 未设置";
        Debug.Log($"  {fieldName}: {status}");
    }

    void LogHierarchy(Transform transform, int indent)
    {
        string indentStr = new string(' ', indent * 2);
        Debug.Log($"{indentStr}{transform.name}");

        foreach (Transform child in transform)
        {
            LogHierarchy(child, indent + 1);
        }
    }
}
