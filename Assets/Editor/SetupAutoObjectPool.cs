using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 自动化对象池设置编辑器脚本
/// 用于在GameScene场景中创建对象并挂载脚本
/// </summary>
public class SetupAutoObjectPool : EditorWindow
{
    [MenuItem("Tools/Setup Auto Object Pool")]
    public static void ShowWindow()
    {
        GetWindow<SetupAutoObjectPool>(true, "Setup Auto Object Pool");
    }

    private void OnGUI()
    {
        GUILayout.Label("自动化对象池设置", EditorStyles.boldLabel);
        
        if (GUILayout.Button("在GameScene中创建对象池管理器"))
        {
            SetupObjectPoolManager();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("操作说明:");
        GUILayout.Label("1. 确保GameScene场景已打开");
        GUILayout.Label("2. 点击按钮创建对象池管理器");
        GUILayout.Label("3. 系统会自动创建所需的组件");
    }

    /// <summary>
    /// 设置对象池管理器
    /// </summary>
    private static void SetupObjectPoolManager()
    {
        // 检查GameScene是否已打开
        if (EditorSceneManager.GetActiveScene().name != "GameScene")
        {
            // 尝试加载GameScene
            SceneAsset gameScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/GameScene.unity");
            if (gameScene != null)
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(gameScene));
                Debug.Log("已打开GameScene场景");
            }
            else
            {
                Debug.LogError("未找到GameScene场景");
                return;
            }
        }

        // 检查是否已存在AutoObjectPoolManager
        GameObject existingManager = GameObject.Find("AutoObjectPoolManager");
        if (existingManager != null)
        {
            Debug.LogWarning("AutoObjectPoolManager已存在，跳过创建");
            return;
        }

        // 创建AutoObjectPoolManager对象
        GameObject poolManagerObj = new GameObject("AutoObjectPoolManager");
        
        // 挂载AutoObjectPoolManager脚本
        AutoObjectPoolManager poolManager = poolManagerObj.AddComponent<AutoObjectPoolManager>();
        
        // 挂载AutoPoolConfigProvider脚本
        poolManagerObj.AddComponent<AutoPoolConfigProvider>();
        
        Debug.Log("已在GameScene中创建AutoObjectPoolManager并挂载所需脚本");
        
        // 检查是否已存在ObjectPoolManager适配器
        GameObject existingAdapter = GameObject.Find("ObjectPoolManager");
        if (existingAdapter != null)
        {
            Debug.LogWarning("ObjectPoolManager适配器已存在，跳过创建");
            return;
        }
        
        // 创建ObjectPoolManager适配器（用于向后兼容）
        GameObject adapterObj = new GameObject("ObjectPoolManager");
        adapterObj.AddComponent<ObjectPoolManager>();
        
        Debug.Log("已在GameScene中创建ObjectPoolManager适配器");
    }
}
