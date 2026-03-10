using UnityEngine;
using Mirror;

[DefaultExecutionOrder(-200)]
public class CharacterSceneInitializer : MonoBehaviour
{
    [Header("角色预览设置")]
    [SerializeField]
    private GameObject previewManagerPrefab;

    [SerializeField]
    private Transform previewSpawnPoint;

    [SerializeField]
    private CharacterPreviewManager previewManager;

    [SerializeField]
    private CharacterSelectionUIController uiController;

    void Awake()
    {
        EnsureNoNetworkActivity();
        InitializeCharacterPreview();
    }

    private void EnsureNoNetworkActivity()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.LogWarning("[CharacterSceneInitializer] 检测到网络活动，正在关闭网络功能");
            
            if (NetworkServer.active)
            {
                NetworkServer.Shutdown();
            }

            if (NetworkClient.active)
            {
                NetworkClient.Shutdown();
            }
        }

        Debug.Log("[CharacterSceneInitializer] 角色选择场景已禁用所有网络功能");
    }

    private void InitializeCharacterPreview()
    {
        if (previewManager == null)
        {
            if (previewManagerPrefab != null)
            {
                GameObject instance = Instantiate(previewManagerPrefab, previewSpawnPoint);
                previewManager = instance.GetComponent<CharacterPreviewManager>();
            }
            else
            {
                GameObject instance = new GameObject("CharacterPreviewManager");
                if (previewSpawnPoint != null)
                {
                    instance.transform.SetParent(previewSpawnPoint);
                    instance.transform.localPosition = Vector3.zero;
                }
                previewManager = instance.AddComponent<CharacterPreviewManager>();
            }
        }

        if (uiController != null && previewManager != null)
        {
            uiController.previewManager = previewManager;
            Debug.Log("[CharacterSceneInitializer] 角色预览管理器已初始化");
        }
    }
}
