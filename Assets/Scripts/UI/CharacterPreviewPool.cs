using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

/// <summary>
/// 角色预览对象池 - 预加载所有角色模型到对象池，支持快速切换显示
/// </summary>
public class CharacterPreviewPool : MonoBehaviour
{
    public static CharacterPreviewPool Instance { get; private set; }
    
    [Header("预览设置")]
    public Transform previewSpot;           // 角色预览位置
    public string[] characterIds = { "01", "02", "03", "04" };
    public float modelRotationSpeed = 30f;  // 模型旋转速度
    
    [Header("相机设置")]
    public Camera previewCamera;            // 预览相机（引用场景中的相机）
    
    private Dictionary<string, GameObject> previewModels = new Dictionary<string, GameObject>();
    private string currentDisplayedId = "";
    private bool isPreloading = false;
    private int loadedCount = 0;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // 预加载所有角色模型
        PreloadAllCharacters();
    }
    
    // 取消自动旋转功能
    // void Update() { }
    
    /// <summary>
    /// 预加载所有角色模型到对象池
    /// </summary>
    void PreloadAllCharacters()
    {
        if (isPreloading) return;
        isPreloading = true;
        loadedCount = 0;
        
        Debug.Log("[CharacterPreviewPool] 开始预加载角色预览模型...");
        
        foreach (var id in characterIds)
        {
            LoadCharacterModel(id);
        }
    }
    
    /// <summary>
    /// 加载单个角色模型
    /// </summary>
    void LoadCharacterModel(string characterId)
    {
        // 从CharacterDatabase获取角色数据
        if (CharacterDatabase.Instance == null)
        {
            Debug.LogError($"[CharacterPreviewPool] CharacterDatabase未初始化，无法加载角色 {characterId}");
            return;
        }
        
        CharacterData data = CharacterDatabase.Instance.GetCharacter(characterId);
        if (data == null)
        {
            Debug.LogError($"[CharacterPreviewPool] 未找到角色数据: {characterId}");
            return;
        }
        
        // 使用Addressables加载模型
        Addressables.LoadAssetAsync<GameObject>(data.addressableKey).Completed += handle =>
        {
            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                // 实例化模型到预览位置
                GameObject model = Instantiate(handle.Result, previewSpot);
                model.name = $"Preview_{characterId}";
                
                // 设置层级为CharacterPreview层，避免被主相机渲染
                SetLayerRecursively(model, LayerMask.NameToLayer("CharacterPreview"));
                
                // 初始隐藏
                model.SetActive(false);
                
                // 存储到字典
                previewModels[characterId] = model;
                
                loadedCount++;
                Debug.Log($"[CharacterPreviewPool] 角色 {characterId} 预览模型加载完成 ({loadedCount}/{characterIds.Length})");
                
                // 如果全部加载完成，默认显示第一个
                if (loadedCount >= characterIds.Length)
                {
                    Debug.Log("[CharacterPreviewPool] 所有角色预览模型加载完成");
                    if (string.IsNullOrEmpty(currentDisplayedId) && characterIds.Length > 0)
                    {
                        ShowCharacter(characterIds[0]);
                    }
                }
            }
            else
            {
                Debug.LogError($"[CharacterPreviewPool] 加载角色模型失败: {characterId}");
            }
        };
    }
    
    /// <summary>
    /// 递归设置层级
    /// </summary>
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    /// <summary>
    /// 显示指定角色预览
    /// </summary>
    public void ShowCharacter(string characterId)
    {
        if (!previewModels.ContainsKey(characterId))
        {
            Debug.LogWarning($"[CharacterPreviewPool] 角色 {characterId} 预览模型未加载");
            return;
        }
        
        // 隐藏当前显示的模型
        if (!string.IsNullOrEmpty(currentDisplayedId) && 
            previewModels.ContainsKey(currentDisplayedId) &&
            previewModels[currentDisplayedId] != null)
        {
            previewModels[currentDisplayedId].SetActive(false);
            Debug.Log($"[CharacterPreviewPool] 隐藏角色: {currentDisplayedId}");
        }
        
        // 显示新模型
        GameObject newModel = previewModels[characterId];
        if (newModel != null)
        {
            newModel.SetActive(true);
            
            // 重置旋转
            newModel.transform.rotation = Quaternion.identity;
            
            currentDisplayedId = characterId;
            Debug.Log($"[CharacterPreviewPool] 显示角色: {characterId}");
        }
    }
    
    /// <summary>
    /// 获取当前显示的角色ID
    /// </summary>
    public string GetCurrentDisplayedId()
    {
        return currentDisplayedId;
    }
    
    /// <summary>
    /// 检查是否已加载完成
    /// </summary>
    public bool IsLoaded()
    {
        return loadedCount >= characterIds.Length;
    }
    
    /// <summary>
    /// 获取角色ID数组
    /// </summary>
    public string[] GetCharacterIds()
    {
        return characterIds;
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        
        // 清理对象池
        foreach (var model in previewModels.Values)
        {
            if (model != null)
            {
                Destroy(model);
            }
        }
        previewModels.Clear();
    }
}
