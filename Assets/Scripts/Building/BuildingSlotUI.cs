using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class BuildingSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI组件")]
    [SerializeField] private Image buildingImage; // 建筑图片
    [SerializeField] private TextMeshProUGUI nameText; // 建筑名称
    [SerializeField] private TextMeshProUGUI consumptionText; // 消耗资源文本
    [SerializeField] private Image selectedHighlight; // 选中高亮
    
    private BuildingData buildingData;
    private BuildingUIController uiController;
    private bool isSelected = false;
    private bool isImageLoaded = false;
    
    // 静态列表用于跟踪所有实例，以便在ResourceDatabase加载完成后更新资源消耗文本
    public static List<BuildingSlotUI> allInstances = new List<BuildingSlotUI>();
    
    private void Awake()
    {
        // 添加到静态实例列表
        allInstances.Add(this);
    }
    
    private void OnDestroy()
    {
        // 从静态实例列表中移除
        allInstances.Remove(this);
    }
    
    public string BuildingId => buildingData?.buildingId ?? string.Empty;
    
    private void OnEnable()
    {
        allInstances.Add(this);
    }
    
    private void OnDisable()
    {
        allInstances.Remove(this);
    }
    
    // 初始化建筑格子
    public void Initialize(BuildingData data, BuildingUIController controller)
    {
        buildingData = data;
        uiController = controller;
        
        // 设置建筑名称
        if (nameText != null)
            nameText.text = data.name;
        
        // 设置消耗资源文本
        if (consumptionText != null)
            consumptionText.text = GetResourceCostString(data);
        
        // 加载建筑图片（异步）
        LoadBuildingImage(data.imageAddressableKey);
        
        // 默认隐藏选中高亮
        if (selectedHighlight != null)
            selectedHighlight.gameObject.SetActive(false);
        
        // 开始检查资源状态
        StartCoroutine(CheckResourceStatusCoroutine());
    }

/// <summary>
/// 设置UI引用
/// </summary>
public void SetUIReferences(Image image, TMPro.TextMeshProUGUI nameTxt, TMPro.TextMeshProUGUI consumptionTxt)
{
    buildingImage = image;
    nameText = nameTxt;
    consumptionText = consumptionTxt;
}

/// <summary>
/// 设置建筑数据（用于BuildingUIController自动配置）
/// </summary>
public void SetBuildingData(BuildingData data)
{
    buildingData = data;
    
    // 设置建筑名称
    if (nameText != null)
        nameText.text = data.name;
    
    // 设置消耗资源文本
    if (consumptionText != null)
        consumptionText.text = GetResourceCostString(data);
    
    // 加载建筑图片（异步）
    LoadBuildingImage(data.imageAddressableKey);
    
    // 默认隐藏选中高亮
    if (selectedHighlight != null)
        selectedHighlight.gameObject.SetActive(false);
    
    // 开始检查资源状态
        StartCoroutine(CheckResourceStatusCoroutine());
}

    
    // 检查资源状态协程
    System.Collections.IEnumerator CheckResourceStatusCoroutine()
    {
        while (true)
        {
            UpdateResourceStatus();
            yield return new WaitForSeconds(0.5f); // 每0.5秒检查一次
        }
    }
    
    // 更新资源状态显示
    void UpdateResourceStatus()
    {
        if (buildingData == null || consumptionText == null) return;
        
        // 查找本地玩家背包
        NetworkPlayer player = FindLocalPlayer();
        if (player == null) return;
        
        Inventory inventory = player.GetInventory();
        if (inventory == null) return;
        
        // 检查资源是否足够
        bool hasEnoughResources = true;
        string statusText = "";
        
        if (buildingData.resourceCostList != null)
        {
            foreach (var cost in buildingData.resourceCostList)
            {
                bool hasEnough = inventory.HasEnough(cost.resourceId, cost.amount);
                string color = hasEnough ? "green" : "red";
                
                // 获取资源的显示名称
                string displayName = cost.resourceId; // 默认使用ID
                if (ResourceDatabase.Instance != null)
                {
                    ResourceData resourceData = ResourceDatabase.Instance.GetResource(cost.resourceId);
                    if (resourceData != null)
                    {
                        displayName = resourceData.name; // 使用资源名称
                    }
                }
                
                statusText += $"<color={color}>{displayName}:{cost.amount}</color>\n";
                
                if (!hasEnough)
                    hasEnoughResources = false;
            }
        }
        
        // 更新文本显示
        consumptionText.text = statusText.TrimEnd('\n');
        
        // 根据资源状态设置交互性
        SetInteractable(hasEnoughResources);
    }
    
    // 设置格子交互性
    void SetInteractable(bool interactable)
    {
        // 可以在这里添加禁用效果，比如变灰等
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? 1f : 0.5f;
            canvasGroup.blocksRaycasts = interactable;
        }
    }
    
    // 查找本地玩家
    NetworkPlayer FindLocalPlayer()
    {
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            if (player.isLocalPlayer)
                return player;
        }
        return null;
    }
    
    // 获取资源消耗字符串
    string GetResourceCostString(BuildingData data)
    {
        if (data.resourceCostList == null || data.resourceCostList.Count == 0)
            return "无消耗";
        
        // 检查ResourceDatabase是否已初始化
        if (ResourceDatabase.Instance == null)
        {
            // 如果ResourceDatabase未初始化，返回资源ID作为临时解决方案
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < data.resourceCostList.Count; i++)
            {
                var cost = data.resourceCostList[i];
                sb.Append($"{cost.resourceId}: {cost.amount}");
                
                if (i < data.resourceCostList.Count - 1)
                    sb.Append("\n");
            }
            return sb.ToString();
        }
        
        System.Text.StringBuilder sb2 = new System.Text.StringBuilder();
        for (int i = 0; i < data.resourceCostList.Count; i++)
        {
            var cost = data.resourceCostList[i];
            ResourceData resourceData = ResourceDatabase.Instance.GetResource(cost.resourceId);
            if (resourceData != null)
            {
                sb2.Append($"{resourceData.name}: {cost.amount}");
            }
            else
            {
                sb2.Append($"{cost.resourceId}: {cost.amount}");
            }
            
            if (i < data.resourceCostList.Count - 1)
                sb2.Append("\n");
        }
        
        string result = sb2.ToString();
        return result;
    }
    
    // 异步加载建筑图片
    async void LoadBuildingImage(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey) || buildingImage == null)
            return;
        
        try
        {
            // 使用Addressables异步加载图片
            var loadOperation = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(addressableKey);
            Sprite sprite = await loadOperation.Task;
            
            if (sprite != null && buildingImage != null)
            {
                buildingImage.sprite = sprite;
                isImageLoaded = true;
            }
        }
        catch (System.Exception e)
        {
            isImageLoaded = true; // 即使加载失败也标记为已加载，避免无限等待
        }
    }
    
    // 检查图片是否已加载
    public bool IsImageLoaded()
    {
        return isImageLoaded;
    }
    
    // 鼠标点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiController != null && !string.IsNullOrEmpty(BuildingId))
        {
            uiController.SelectBuilding(BuildingId);
        }
    }
    
    // 开始拖拽
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (uiController == null || !uiController.IsInBuildingMode())
            return;
        
        // 检查资源是否足够
        if (!HasEnoughResources())
        {
            eventData.pointerDrag = null; // 取消拖拽
            return;
        }
        
        // 选择建筑
        uiController.SelectBuilding(BuildingId);
        
        // 可以在这里添加拖拽反馈效果
    }
    
    // 检查资源是否足够
    bool HasEnoughResources()
    {
        if (buildingData == null || buildingData.resourceCostList == null)
            return true;
        
        // 查找本地玩家背包
        NetworkPlayer player = FindLocalPlayer();
        if (player == null) return false;
        
        Inventory inventory = player.GetInventory();
        if (inventory == null) return false;
        
        // 检查所有资源是否足够
        foreach (var cost in buildingData.resourceCostList)
        {
            if (!inventory.HasEnough(cost.resourceId, cost.amount))
                return false;
        }
        
        return true;
    }
    
    // 拖拽中
    public void OnDrag(PointerEventData eventData)
    {
        // 拖拽逻辑由BuildingSystem处理，这里只处理UI事件
    }
    
    // 结束拖拽
    public void OnEndDrag(PointerEventData eventData)
    {
        if (uiController == null || !uiController.IsInBuildingMode())
            return;
        
        // 检查是否在网格平面上
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 200f))
        {
            if (hit.collider.CompareTag("yingdi"))
            {
                // 在网格平面上释放，执行建造
                BuildingSystem buildingSystem = null;
                
                // 首先尝试获取当前本地玩家的BuildingSystem
                NetworkPlayer localPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
                if (localPlayer != null && localPlayer.isLocalPlayer)
                {
                    buildingSystem = localPlayer.GetComponent<BuildingSystem>();
                }
                
                // 如果上面没有找到，再尝试查找任何拥有控制权的BuildingSystem
                if (buildingSystem == null)
                {
                    BuildingSystem[] allBuildingSystems = FindObjectsOfType<BuildingSystem>();
                    foreach (BuildingSystem bs in allBuildingSystems)
                    {
                        if (bs.isOwned)
                        {
                            buildingSystem = bs;
                            break;
                        }
                    }
                }
                
                if (buildingSystem != null)
                {
                    buildingSystem.AttemptBuildAtPosition(hit.point);
                }
            }
        }
        
    }
    
    // 设置选中状态
    // 供外部调用以更新资源消耗文本（当ResourceDatabase初始化完成后）
    public void UpdateResourceCostText()
    {
        if (buildingData != null && consumptionText != null)
        {
            consumptionText.text = GetResourceCostString(buildingData);
        }
    }
    
    // 静态方法，供ResourceDatabase调用以更新所有实例的资源消耗文本
    public static void UpdateAllResourceCostTexts()
    {
        foreach (var instance in allInstances.ToList()) // 使用ToList()避免在迭代时修改列表
        {
            if (instance != null) // 检查实例是否已被销毁
            {
                instance.UpdateResourceCostText();
            }
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectedHighlight != null)
            selectedHighlight.gameObject.SetActive(selected);
    }
}