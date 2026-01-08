using UnityEngine;
using UnityEditor;

public class UIClickTestWindow : EditorWindow
{
    [MenuItem("Tools/UI点击测试工具")]
    public static void ShowWindow()
    {
        GetWindow<UIClickTestWindow>("UI点击测试工具");
    }

    void OnGUI()
    {
        GUILayout.Label("UI点击检测测试工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("使用说明：", EditorStyles.boldLabel);
        GUILayout.Label("1. 在游戏中打开背包UI（按Tab键）");
        GUILayout.Label("2. 点击背包UI中的格子");
        GUILayout.Label("3. 检查控制台是否输出：");
        GUILayout.Label("   '[PlayerInputHandler] 鼠标点击在UI上，忽略攻击输入'");
        GUILayout.Space(10);

        GUILayout.Label("预期结果：", EditorStyles.boldLabel);
        GUILayout.Label("✓ 点击背包UI时不触发攻击");
        GUILayout.Label("✓ 点击游戏场景时正常触发攻击");
        GUILayout.Space(10);

        GUILayout.Label("代码检查项：", EditorStyles.boldLabel);
        GUILayout.Label("✓ PlayerInputHandler.cs 已添加 EventSystem.current.IsPointerOverGameObject() 检测");
        GUILayout.Label("✓ 背包UI的Canvas需要GraphicRaycaster组件");
        GUILayout.Label("✓ 背包UI的格子需要有Graphic组件（如Image）");
        GUILayout.Space(10);

        if (GUILayout.Button("打开PlayerInputHandler.cs"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Character/PlayerInputHandler.cs");
            if (script != null)
            {
                AssetDatabase.OpenAsset(script);
            }
        }

        if (GUILayout.Button("打开InventoryUIController.cs"))
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Inventory/InventoryUIController.cs");
            if (script != null)
            {
                AssetDatabase.OpenAsset(script);
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("如果点击UI时仍然触发攻击，请检查：\n1. 背包UI的Canvas是否有GraphicRaycaster组件\n2. 背包UI的格子是否有Graphic组件\n3. 背包UI是否正确显示在屏幕上", MessageType.Info);
    }
}
