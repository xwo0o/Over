using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mirror;

public class SceneSetupFixer : EditorWindow
{
    [MenuItem("Tools/场景修复/修复Character场景")]
    public static void FixCharacterScene()
    {
        string scenePath = "Assets/Scenes/Character.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        GameObject characterDatabaseObj = GameObject.Find("CharacterDatabase");
        if (characterDatabaseObj == null)
        {
            characterDatabaseObj = new GameObject("CharacterDatabase");
            characterDatabaseObj.AddComponent<CharacterDatabase>();
            EditorUtility.SetDirty(characterDatabaseObj);
        }
        else
        {
            CharacterDatabase db = characterDatabaseObj.GetComponent<CharacterDatabase>();
            if (db == null)
            {
                characterDatabaseObj.AddComponent<CharacterDatabase>();
                EditorUtility.SetDirty(characterDatabaseObj);
            }
            else
            {
            }
        }

        GameObject sceneInitializerObj = GameObject.Find("CharacterSceneInitializer");
        if (sceneInitializerObj == null)
        {
            sceneInitializerObj = new GameObject("CharacterSceneInitializer");
            sceneInitializerObj.AddComponent<CharacterSceneInitializer>();
            EditorUtility.SetDirty(sceneInitializerObj);
            Debug.Log("[场景修复] 已添加CharacterSceneInitializer到Character场景");
        }
        else
        {
            CharacterSceneInitializer initializer = sceneInitializerObj.GetComponent<CharacterSceneInitializer>();
            if (initializer == null)
            {
                sceneInitializerObj.AddComponent<CharacterSceneInitializer>();
                EditorUtility.SetDirty(sceneInitializerObj);
                Debug.Log("[场景修复] 已为CharacterSceneInitializer对象添加CharacterSceneInitializer组件");
            }
            else
            {
                Debug.Log("[场景修复] CharacterSceneInitializer已存在，跳过");
            }
        }

        RemoveNetworkComponentsFromCharacterScene();

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[场景修复] Character场景修复完成并已保存");
    }

    private static void RemoveNetworkComponentsFromCharacterScene()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int removedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            NetworkIdentity networkIdentity = obj.GetComponent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                DestroyImmediate(networkIdentity);
                EditorUtility.SetDirty(obj);
                removedCount++;
                Debug.Log($"[场景修复] 已从 {obj.name} 移除NetworkIdentity组件");
            }

            NetworkBehaviour[] networkBehaviours = obj.GetComponents<NetworkBehaviour>();
            foreach (var behaviour in networkBehaviours)
            {
                if (behaviour is CharacterSelectionManager)
                {
                    DestroyImmediate(behaviour);
                    EditorUtility.SetDirty(obj);
                    removedCount++;
                    Debug.Log($"[场景修复] 已从 {obj.name} 移除CharacterSelectionManager组件");
                }
            }
        }

        if (removedCount > 0)
        {
            Debug.Log($"[场景修复] 共移除了 {removedCount} 个网络相关组件");
        }
    }

    [MenuItem("Tools/场景修复/修复GameScene标签")]
    public static void FixGameSceneTags()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        bool found = false;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("yingdi") || obj.name.Contains("Yingdi"))
            {
                obj.tag = "yingdi";
                EditorUtility.SetDirty(obj);
                Debug.Log($"[场景修复] 已将 {obj.name} 的标签设置为 yingdi");
                found = true;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[场景修复] 未找到包含'yingdi'的对象，请手动设置标签");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[场景修复] GameScene标签修复完成并已保存");
    }

    [MenuItem("Tools/场景修复/修复GameScene数据库")]
    public static void FixGameSceneDatabases()
    {
        string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        GameObject enemyDatabaseObj = GameObject.Find("EnemyDatabase");
        if (enemyDatabaseObj == null)
        {
            enemyDatabaseObj = new GameObject("EnemyDatabase");
            enemyDatabaseObj.AddComponent<EnemyDatabase>();
            EditorUtility.SetDirty(enemyDatabaseObj);
            Debug.Log("[场景修复] 已添加EnemyDatabase到GameScene");
        }
        else
        {
            EnemyDatabase db = enemyDatabaseObj.GetComponent<EnemyDatabase>();
            if (db == null)
            {
                enemyDatabaseObj.AddComponent<EnemyDatabase>();
                EditorUtility.SetDirty(enemyDatabaseObj);
                Debug.Log("[场景修复] 已为EnemyDatabase对象添加EnemyDatabase组件");
            }
            else
            {
                Debug.Log("[场景修复] EnemyDatabase已存在，跳过");
            }
        }

        GameObject resourceDatabaseObj = GameObject.Find("ResourceDatabase");
        if (resourceDatabaseObj == null)
        {
            resourceDatabaseObj = new GameObject("ResourceDatabase");
            resourceDatabaseObj.AddComponent<ResourceDatabase>();
            EditorUtility.SetDirty(resourceDatabaseObj);
            Debug.Log("[场景修复] 已添加ResourceDatabase到GameScene");
        }
        else
        {
            ResourceDatabase db = resourceDatabaseObj.GetComponent<ResourceDatabase>();
            if (db == null)
            {
                resourceDatabaseObj.AddComponent<ResourceDatabase>();
                EditorUtility.SetDirty(resourceDatabaseObj);
                Debug.Log("[场景修复] 已为ResourceDatabase对象添加ResourceDatabase组件");
            }
            else
            {
                Debug.Log("[场景修复] ResourceDatabase已存在，跳过");
            }
        }

        GameObject buildingDataManagerObj = GameObject.Find("BuildingDataManager");
        if (buildingDataManagerObj == null)
        {
            buildingDataManagerObj = new GameObject("BuildingDataManager");
            buildingDataManagerObj.AddComponent<BuildingDataManager>();
            EditorUtility.SetDirty(buildingDataManagerObj);
            Debug.Log("[场景修复] 已添加BuildingDataManager到GameScene");
        }
        else
        {
            BuildingDataManager db = buildingDataManagerObj.GetComponent<BuildingDataManager>();
            if (db == null)
            {
                buildingDataManagerObj.AddComponent<BuildingDataManager>();
                EditorUtility.SetDirty(buildingDataManagerObj);
                Debug.Log("[场景修复] 已为BuildingDataManager对象添加BuildingDataManager组件");
            }
            else
            {
                Debug.Log("[场景修复] BuildingDataManager已存在，跳过");
            }
        }

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
            GameSceneInitializer initializer = gameSceneInitializerObj.GetComponent<GameSceneInitializer>();
            if (initializer == null)
            {
                gameSceneInitializerObj.AddComponent<GameSceneInitializer>();
                EditorUtility.SetDirty(gameSceneInitializerObj);
                Debug.Log("[场景修复] 已为GameSceneInitializer对象添加GameSceneInitializer组件");
            }
            else
            {
                Debug.Log("[场景修复] GameSceneInitializer已存在，跳过");
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[场景修复] GameScene数据库修复完成并已保存");
    }

    [MenuItem("Tools/场景修复/修复PlayerContainer预制体")]
    public static void FixPlayerContainerPrefab()
    {
        string prefabPath = "Assets/Prefabs/Characters/PlayerContainer.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[场景修复] 未找到PlayerContainer预制体: {prefabPath}");
            return;
        }

        CharacterModelManager modelManager = prefab.GetComponent<CharacterModelManager>();
        if (modelManager == null)
        {
            modelManager = prefab.AddComponent<CharacterModelManager>();
            EditorUtility.SetDirty(prefab);
            Debug.Log("[场景修复] 已为PlayerContainer添加CharacterModelManager组件");
        }
        else
        {
            Debug.Log("[场景修复] PlayerContainer已包含CharacterModelManager组件，跳过");
        }

        PlayerContainerInitializer initializer = prefab.GetComponent<PlayerContainerInitializer>();
        if (initializer == null)
        {
            initializer = prefab.AddComponent<PlayerContainerInitializer>();
            EditorUtility.SetDirty(prefab);
            Debug.Log("[场景修复] 已为PlayerContainer添加PlayerContainerInitializer组件");
        }
        else
        {
            Debug.Log("[场景修复] PlayerContainer已包含PlayerContainerInitializer组件，跳过");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[场景修复] PlayerContainer预制体修复完成并已保存");
    }

    [MenuItem("Tools/场景修复/一键修复所有场景")]
    public static void FixAllScenes()
    {
        FixCharacterScene();
        FixGameSceneTags();
        FixGameSceneDatabases();
        FixPlayerContainerPrefab();
        Debug.Log("[场景修复] 所有场景修复完成！");
    }
}
