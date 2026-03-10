using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class RunSceneFix
{
    [MenuItem("Tools/运行场景修复")]
    public static void RunFix()
    {
        Debug.Log("开始运行场景修复...");
        
        // 先修复 Character 场景，添加 ResourceDatabase
        string characterScenePath = "Assets/Scenes/Character.unity";
        var characterScene = EditorSceneManager.OpenScene(characterScenePath);

        GameObject resourceDatabaseObj = GameObject.Find("ResourceDatabase");
        if (resourceDatabaseObj == null)
        {
            resourceDatabaseObj = new GameObject("ResourceDatabase");
            resourceDatabaseObj.AddComponent<ResourceDatabase>();
            EditorUtility.SetDirty(resourceDatabaseObj);
            Debug.Log("[场景修复] 已添加ResourceDatabase到Character场景");
        }
        else
        {
            Debug.Log("[场景修复] Character场景中ResourceDatabase已存在，跳过");
        }

        EditorSceneManager.SaveScene(characterScene);
        Debug.Log("[场景修复] Character场景修复完成并已保存");

        // 然后修复 GameScene
        string gameScenePath = "Assets/Scenes/GameScene.unity";
        var gameScene = EditorSceneManager.OpenScene(gameScenePath);

        GameObject gameSceneInitializerObj = GameObject.Find("GameSceneInitializer");
        if (gameSceneInitializerObj == null)
        {
            gameSceneInitializerObj = new GameObject("GameSceneInitializer");
            gameSceneInitializerObj.AddComponent<GameSceneInitializer>();
            EditorUtility.SetDirty(gameSceneInitializerObj);
            Debug.Log("[场景修复] 已添加GameSceneInitializer到GameScene");
        }
        else
        {
            Debug.Log("[场景修复] GameScene中GameSceneInitializer已存在，跳过");
        }

        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[场景修复] GameScene修复完成并已保存");
        
        Debug.Log("场景修复完成！现在 ResourceDatabase 将在 Character 场景中初始化，并通过 DontDestroyOnLoad 传递到 GameScene。");
    }
}
