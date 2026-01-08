using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

[DefaultExecutionOrder(100)]
public class CharacterPreviewManager : MonoBehaviour
{
    [Header("模型设置")]
    [SerializeField]
    private float modelScale = 1f;

    [SerializeField]
    private float rotationSpeed = 30f;

    [SerializeField]
    private bool autoRotate = false;

    [Header("实例化位置")]
    [SerializeField]
    private Transform spawnParent;

    [SerializeField]
    private Vector3 localSpawnPosition = Vector3.zero;

    [SerializeField]
    private Quaternion localSpawnRotation = Quaternion.identity;

    private GameObject currentModel;
    private CharacterData currentCharacterData;
    private Dictionary<string, AsyncOperationHandle<GameObject>> loadedHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    private void Awake()
    {
        if (CharacterDatabase.Instance == null)
        {
            Debug.LogError($"[CharacterPreviewManager] CharacterDatabase未初始化，请确保场景中有CharacterDatabase组件");
        }

        if (spawnParent == null)
        {
            spawnParent = GameObject.Find("CharacterPreviewSpawn")?.transform;
            if (spawnParent != null)
            {
                Debug.Log($"[CharacterPreviewManager] 自动找到CharacterPreviewSpawn对象");
            }
            else
            {
                Debug.LogWarning($"[CharacterPreviewManager] 未找到CharacterPreviewSpawn对象，将使用默认位置");
            }
        }
    }

    private void Update()
    {
        if (autoRotate && currentModel != null)
        {
            currentModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    public void LoadCharacterModel(string characterId)
    {
        if (CharacterDatabase.Instance == null)
        {
            Debug.LogError($"[CharacterPreviewManager] CharacterDatabase未初始化，请确保场景中有CharacterDatabase组件");
            return;
        }

        currentCharacterData = CharacterDatabase.Instance.GetCharacter(characterId);
        if (currentCharacterData == null)
        {
            Debug.LogError($"[CharacterPreviewManager] 未找到角色ID: {characterId}");
            return;
        }

        StartCoroutine(LoadModelAsync(currentCharacterData));
    }

    private System.Collections.IEnumerator LoadModelAsync(CharacterData characterData)
    {
        string addressableKey = characterData.addressableKey;
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError($"[CharacterPreviewManager] 角色{characterData.id}的addressableKey为空");
            yield break;
        }

        Debug.Log($"[CharacterPreviewManager] 开始加载角色模型: {addressableKey}");

        AsyncOperationHandle<GameObject> handle;

        if (loadedHandles.ContainsKey(addressableKey))
        {
            handle = loadedHandles[addressableKey];
        }
        else
        {
            handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            loadedHandles[addressableKey] = handle;
        }

        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            InstantiateModel(handle.Result);
        }
        else
        {
            Debug.LogError($"[CharacterPreviewManager] 加载角色模型失败: {addressableKey}, 错误: {handle.OperationException}");
        }
    }

    private void InstantiateModel(GameObject modelPrefab)
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        Vector3 worldPosition = spawnParent != null ? spawnParent.TransformPoint(localSpawnPosition) : localSpawnPosition;
        Quaternion worldRotation = spawnParent != null ? spawnParent.rotation * localSpawnRotation : localSpawnRotation;

        currentModel = Instantiate(modelPrefab, worldPosition, worldRotation);

        if (spawnParent != null)
        {
            currentModel.transform.SetParent(spawnParent, false);
            currentModel.transform.localPosition = localSpawnPosition;
            currentModel.transform.localRotation = localSpawnRotation;
            Debug.Log($"[CharacterPreviewManager] 模型已作为{spawnParent.name}的子对象实例化");
        }

        currentModel.transform.localScale = Vector3.one * modelScale;

        Debug.Log($"[CharacterPreviewManager] 模型实例化后世界位置: {currentModel.transform.position}");
        Debug.Log($"[CharacterPreviewManager] 模型实例化后本地位置: {currentModel.transform.localPosition}");
        Debug.Log($"[CharacterPreviewManager] 模型实例化后缩放: {currentModel.transform.localScale}");
        Debug.Log($"[CharacterPreviewManager] 模型实例化后旋转: {currentModel.transform.rotation.eulerAngles}");

        Animator animator = currentModel.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Idle");
            Debug.Log($"[CharacterPreviewManager] 找到Animator组件，播放Idle动画");
        }
        else
        {
            Debug.LogWarning($"[CharacterPreviewManager] 未找到Animator组件");
        }

        Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();
        Debug.Log($"[CharacterPreviewManager] 找到 {renderers.Length} 个Renderer组件");
        foreach (var renderer in renderers)
        {
            Debug.Log($"[CharacterPreviewManager] Renderer: {renderer.name}, Enabled: {renderer.enabled}, Layer: {LayerMask.LayerToName(renderer.gameObject.layer)}");
            if (renderer is SkinnedMeshRenderer)
            {
                ((SkinnedMeshRenderer)renderer).updateWhenOffscreen = true;
            }
        }

        if (renderers.Length == 0)
        {
            Debug.LogError($"[CharacterPreviewManager] 模型没有Renderer组件，无法显示！");
        }

        Debug.Log($"[CharacterPreviewManager] 角色模型实例化成功: {currentCharacterData?.name}");
    }

    public void SetSpawnPosition(Vector3 position)
    {
        localSpawnPosition = position;
        Debug.Log($"[CharacterPreviewManager] 更新本地实例化位置: {position}");
    }

    public void SetSpawnParent(Transform parent)
    {
        spawnParent = parent;
        Debug.Log($"[CharacterPreviewManager] 更新父对象: {parent?.name}");
    }

    public void SetSpawnRotation(Quaternion rotation)
    {
        localSpawnRotation = rotation;
        Debug.Log($"[CharacterPreviewManager] 更新本地实例化旋转: {rotation.eulerAngles}");
    }

    public void SetAutoRotate(bool enable)
    {
        autoRotate = enable;
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    public GameObject GetCurrentModel()
    {
        return currentModel;
    }

    public CharacterData GetCurrentCharacterData()
    {
        return currentCharacterData;
    }

    public Vector3 GetSpawnPosition()
    {
        return localSpawnPosition;
    }

    public Transform GetSpawnParent()
    {
        return spawnParent;
    }

    private void OnDestroy()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        foreach (var handle in loadedHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        loadedHandles.Clear();
    }
}
