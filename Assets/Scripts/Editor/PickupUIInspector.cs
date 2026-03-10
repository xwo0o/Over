using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class PickupUIInspector : EditorWindow
{
    [MenuItem("Tools/UI检查/检查PickupUI预制体")]
    public static void ShowWindow()
    {
        GetWindow<PickupUIInspector>("PickupUI检查器");
    }

    void OnGUI()
    {
        GUILayout.Label("PickupUI预制体字段检查", EditorStyles.boldLabel);
        GUILayout.Space(10);

        string prefabPath = "Assets/Prefabs/UI/pickupUI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            EditorGUILayout.HelpBox($"未找到预制体: {prefabPath}", MessageType.Error);
            return;
        }

        ResourcePickupUIController controller = prefab.GetComponent<ResourcePickupUIController>();
        if (controller == null)
        {
            EditorGUILayout.HelpBox("预制体上没有ResourcePickupUIController组件", MessageType.Error);
            return;
        }

        SerializedObject serializedObject = new SerializedObject(controller);

        GUILayout.Label("字段引用状态:", EditorStyles.boldLabel);
        GUILayout.Space(5);

        SerializedProperty pickupUIPanelProp = serializedObject.FindProperty("pickupUIPanel");
        SerializedProperty amountTextProp = serializedObject.FindProperty("amountText");

        DrawFieldStatus("pickupUIPanel", pickupUIPanelProp);
        DrawFieldStatus("amountText", amountTextProp);

        GUILayout.Space(20);

        if (GUILayout.Button("自动修复字段引用", GUILayout.Height(30)))
        {
            AutoFixReferences(prefab, controller);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("显示预制体层级结构", GUILayout.Height(30)))
        {
            ShowPrefabHierarchy(prefab);
        }
    }

    void DrawFieldStatus(string fieldName, SerializedProperty property)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(fieldName, GUILayout.Width(150));

        if (property == null)
        {
            EditorGUILayout.HelpBox("字段未找到", MessageType.Error);
        }
        else if (property.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("未引用任何对象", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"已引用: {property.objectReferenceValue.name}", MessageType.Info);
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    void AutoFixReferences(GameObject prefab, ResourcePickupUIController controller)
    {
        if (controller.pickupUIPanel == null)
        {
            Transform panelTransform = prefab.transform.Find("PickupPanel");
            if (panelTransform != null)
            {
                controller.pickupUIPanel = panelTransform.gameObject;
                Debug.Log("[PickupUI检查器] 已自动设置pickupUIPanel");
            }
            else
            {
                Debug.LogWarning("[PickupUI检查器] 未找到PickupPanel子对象");
            }
        }

        if (controller.amountText == null)
        {
            Transform amountTextTransform = prefab.transform.Find("PickupPanel/AmountText");
            if (amountTextTransform != null)
            {
                controller.amountText = amountTextTransform.GetComponent<TextMeshProUGUI>();
                if (controller.amountText != null)
                {
                    Debug.Log("[PickupUI检查器] 已自动设置amountText");
                }
                else
                {
                    Debug.LogWarning("[PickupUI检查器] AmountText对象上没有TextMeshProUGUI组件");
                }
            }
            else
            {
                Debug.LogWarning("[PickupUI检查器] 未找到AmountText子对象");
            }
        }

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PickupUI检查器] 预制体已保存");
    }

    void ShowPrefabHierarchy(GameObject prefab)
    {
        Debug.Log("[PickupUI检查器] 预制体层级结构:");
        LogHierarchy(prefab.transform, 0);
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
