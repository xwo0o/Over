using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Mirror;
using TMPro;
using System.Text;
using System.Threading.Tasks;

public class BuildingUIController : MonoBehaviour
{
    [Header("面板引用")]
    
    // 静态列表用于跟踪所有consumptionText，以便在ResourceDatabase加载完成后更新资源消耗文本
    private static List<TMPro.TextMeshProUGUI> allConsumptionTexts = new List<TMPro.TextMeshProUGUI>();
    public GameObject buildingPanel;        // 建筑选择面板
    public GameObject backgroundPanel;       // 背景面板
    public GameObject gridPlane;             // 网格平面
    
    [Header("InventoryUI引用")]
    public GameObject inventoryBackgroundPanel;  // InventoryUI下的BackgroundPanel_2
    public GameObject inventoryPreviewPanel;     // InventoryUI下的PreviewPanel
    
    [Header("InventoryUI控制器引用")]
    public InventoryUIController inventoryUIController; // InventoryUI控制器引用
    
    private bool isInitialized = false;
    private bool isBuildingMode = false;     // 是否处于建筑模式
    private bool wasInventoryToggleEnabled = true; // 记录进入建筑模式前的背包切换状态
    private bool isInventoryToggleBlocked = false; // 记录是否阻止了背包切换
    private Coroutine initializationCoroutine;

    void Awake()
    {
        
        // 确保BuildingUI根对象激活
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        
        // 初始化时只隐藏建筑相关元素，InventoryUI相关元素保持原状态
        HideAllBuildingElements();
    }

    void Start()
    {
        
        // 确保BuildingUI根对象激活
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        
        // 初始化时只隐藏建筑相关元素，InventoryUI相关元素保持原状态
        HideAllBuildingElements();
        
        // 开始初始化
        StartCoroutine(DelayedInitialize());
    }

    void OnEnable()
    {
        
        // 确保BuildingUI根对象激活
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        
        // 检查并确保Grid_Plane状态正确
        EnsureGridPlaneState();
    }

    // 延迟初始化协程
    System.Collections.IEnumerator DelayedInitialize()
    {
        // 防止重复初始化
        if (isInitialized)
        {
            yield break;
        }
        
        // 确保BuildingUI根对象始终激活
        gameObject.SetActive(true);
        
        // 等待一帧，确保所有组件都已初始化
        yield return null;
        
        // 确保面板激活
        EnsureAllObjectsActive();
        
        // 初始化建筑格子
        InitializeBuildingSlots();
        
        // 等待建筑图片加载完成
        yield return StartCoroutine(WaitForBuildingImagesLoaded());
        
        // 初始化完成后，隐藏所有建筑相关元素（BuildingUI根对象保持激活，但不显示任何内容）
        HideAllBuildingElements();
        
        // 标记为已初始化
        isInitialized = true;
    }
    
    // 等待建筑图片加载完成（静态UI实现不需要等待，因为图片是异步加载的）
    System.Collections.IEnumerator WaitForBuildingImagesLoaded()
    {
        // 静态UI实现中，图片是异步加载的，不阻塞初始化过程
        yield return null; // 等待一帧后继续
    }

    /// <summary>
    /// 确保所有相关子对象在初始化时激活（BuildingUI根对象始终保持激活）
    /// </summary>
    void EnsureAllObjectsActive()
    {
        // BuildingUI根对象应该始终保持激活，不需要在这里设置
        // 确保BuildingPanel子对象激活
        if (buildingPanel != null)
        {
            buildingPanel.SetActive(true);
        }
        
        // 确保背景面板激活（如果需要初始化）
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }
        
        // 确保网格平面隐藏（初始化时不应显示）
        if (gridPlane != null)
        {
            gridPlane.SetActive(false);
        }
        else
        {
            // 如果引用为空，尝试查找
            GameObject gridObj = TryFindGridPlane();
            if (gridObj != null)
            {
                gridObj.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 初始化建筑格子
    /// </summary>
    void InitializeBuildingSlots()
    {
        // 检查建筑面板引用是否为空
        if (buildingPanel == null)
        {
            return;
        }

        // 将GameObject转换为Transform以访问子对象
        Transform buildingPanelTransform = buildingPanel.transform;

        // 获取所有建筑数据
        List<BuildingData> buildingDatas = BuildingDataManager.Instance.GetAllBuildingData();
        
        // 遍历建筑面板下的子对象（假设为Panel (1), Panel (2), Panel (3), Panel (4) 或类似的命名）
        int maxSlots = Mathf.Min(buildingPanelTransform.childCount, buildingDatas.Count);
        
        for (int i = 0; i < maxSlots; i++)
        {
            Transform slotPanel = buildingPanelTransform.GetChild(i);
            if (slotPanel == null)
            {
                continue;
            }
            
            // 尝试获取或添加BuildingSlotUI组件
            BuildingSlotUI buildingSlotUI = slotPanel.GetComponent<BuildingSlotUI>();
            if (buildingSlotUI == null)
            {
                buildingSlotUI = slotPanel.gameObject.AddComponent<BuildingSlotUI>();
            }
            
            // 查找Image组件 - 首先尝试直接在Panel下查找，然后查找子对象
            Image slotImage = null;
            Transform imageObj = slotPanel.Find("Image") ?? slotPanel.Find("ItemImage") ?? slotPanel.Find("BuildingImage");
            if (imageObj != null)
            {
                slotImage = imageObj.GetComponent<Image>();
            }
            // 如果没有找到指定名称的Image对象，尝试查找第一个Image组件
            if (slotImage == null)
            {
                slotImage = slotPanel.GetComponent<Image>(); // 检查Panel自身是否有Image组件
            }
            if (slotImage == null)
            {
                slotImage = slotPanel.GetComponentInChildren<Image>(true); // 查找子对象中的Image组件
            }
            
            // 查找名称文本组件
            TMPro.TextMeshProUGUI nameText = null;
            Transform nameTextObj = slotPanel.Find("NameText") ?? slotPanel.Find("Name") ?? slotPanel.Find("Text") ?? slotPanel.Find("TMP_Name");
            if (nameTextObj != null)
            {
                nameText = nameTextObj.GetComponent<TMPro.TextMeshProUGUI>();
            }
            // 如果没有找到指定名称的对象，尝试查找第一个TMP文本组件
            if (nameText == null)
            {
                nameText = slotPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            }
            
            // 查找消耗文本组件
            TMPro.TextMeshProUGUI consumptionText = null;
            Transform consumptionTextObj = slotPanel.Find("ConsumptionText") ?? slotPanel.Find("Consumption") ?? slotPanel.Find("CostText");
            if (consumptionTextObj != null)
            {
                consumptionText = consumptionTextObj.GetComponent<TMPro.TextMeshProUGUI>();
            }
            // 如果没有找到指定名称的对象，尝试查找第二个TMP文本组件（假设第一个是nameText，第二个是consumptionText）
            if (consumptionText == null)
            {
                var allTextComponents = slotPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (allTextComponents.Length >= 2)
                {
                    // 假设第二个文本组件是消耗文本（第一个是nameText）
                    consumptionText = allTextComponents[1];
                }
                else if (allTextComponents.Length == 1 && allTextComponents[0] != nameText)
                {
                    // 如果只有一个文本组件且不是nameText，则可能是consumptionText
                    consumptionText = allTextComponents[0];
                }
            }
            
            // 设置BuildingSlotUI组件的引用
            buildingSlotUI.SetUIReferences(slotImage, nameText, consumptionText);
            
            // 设置建筑数据
            buildingSlotUI.SetBuildingData(buildingDatas[i]);
            
            // 添加点击事件
            if (slotImage != null)
            {
                Button button = slotImage.GetComponent<Button>();
                if (button == null)
                {
                    button = slotImage.gameObject.AddComponent<Button>();
                }
                
                int index = i; // 保存循环变量的副本
                button.onClick.AddListener(() => OnSlotClicked(buildingDatas[index].buildingId, buildingSlotUI));
            }
            

        }
        

    }

    // 槽位点击事件处理
    private void OnSlotClicked(string buildingId, BuildingSlotUI slotUI)
    {
        SelectBuilding(buildingId);
    }

    // 进入建筑模式
    public void EnterBuildingMode()
    {

        
        isBuildingMode = true;
        
        // 禁用Tab键打开背包功能
        if (inventoryUIController != null)
        {
            // 检查背包当前是否是可见的，如果是，则记录状态并在退出建筑模式时恢复
            wasInventoryToggleEnabled = inventoryUIController.IsVisible();
            
            // 通过修改toggleKey为一个不太可能被按到的键来阻止背包打开
            // 或者我们可以通过其他方式来实现
            inventoryUIController.SetToggleBlocked(true);
            isInventoryToggleBlocked = true;
        }
        
        // 激活背景面板
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(true);
        }
        
        // 激活建筑面板
        if (buildingPanel != null)
        {
            buildingPanel.SetActive(true);
        }
        
        // 显示网格平面
        if (gridPlane != null)
        {
            gridPlane.SetActive(true);
        }
        
        // 隐藏InventoryUI相关元素
        HideInventoryUIElements();
        
        // 激活BuildingUI根对象
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    // 退出建筑模式
    public void ExitBuildingMode()
    {

        
        isBuildingMode = false;
        
        // 隐藏网格平面
        if (gridPlane != null)
        {
            gridPlane.SetActive(false);
        }
        
        // 隐藏建筑面板
        if (buildingPanel != null)
        {
            buildingPanel.SetActive(false);
        }
        
        // 隐藏背景面板
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }
        
        // 恢复Tab键打开背包功能
        if (inventoryUIController != null && isInventoryToggleBlocked)
        {
            inventoryUIController.SetToggleBlocked(false);
            isInventoryToggleBlocked = false;
            
            // 如果进入建筑模式前背包是可见的，则恢复其可见状态
            if (wasInventoryToggleEnabled)
            {
                inventoryUIController.ShowInventory();
            }
        }
        
        // 显示InventoryUI相关元素（如果它们存在）
        ShowInventoryUIElements();
    }

    // 检查玩家是否在营地区域内
    bool IsPlayerInCampArea()
    {
        // 查找本地玩家
        NetworkPlayer localPlayer = FindLocalPlayer();
        if (localPlayer == null)
            return false;
        
        // 使用更可靠的触发器检测方式
        // 查找场景中的HealingZone（营地区域触发器）
        HealingZone[] healingZones = FindObjectsOfType<HealingZone>();
        foreach (HealingZone zone in healingZones)
        {
            if (zone != null)
            {
                // 检查玩家是否在区域的触发器范围内
                BoxCollider zoneCollider = zone.GetComponent<BoxCollider>();
                if (zoneCollider != null && localPlayer != null)
                {
                    // 检查玩家位置是否在区域范围内
                    Vector3 playerPos = localPlayer.transform.position;
                    Vector3 zonePos = zone.transform.position;
                    Vector3 zoneSize = zoneCollider.size;
                    
                    // 考虑对象的本地坐标系
                    Vector3 localPlayerPos = zone.transform.InverseTransformPoint(playerPos);
                    Vector3 localZoneCenter = zoneCollider.center;
                    
                    // 计算区域的边界（考虑中心偏移）
                    float minX = localZoneCenter.x - (zoneSize.x / 2f);
                    float maxX = localZoneCenter.x + (zoneSize.x / 2f);
                    float minZ = localZoneCenter.z - (zoneSize.z / 2f);
                    float maxZ = localZoneCenter.z + (zoneSize.z / 2f);
                    
                    // 检查玩家是否在X-Z平面上的区域内（忽略Y轴）
                    if (localPlayerPos.x >= minX && localPlayerPos.x <= maxX &&
                        localPlayerPos.z >= minZ && localPlayerPos.z <= maxZ)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查是否处于建筑模式（供其他脚本调用）
    /// </summary>
    public bool IsInBuildingMode()
    {
        return isBuildingMode;
    }

    /// <summary>
    /// 选择建筑（供BuildingSlotUI调用）
    /// </summary>
    public void SelectBuilding(string buildingId)
    {

        
        // 通知当前玩家拥有的BuildingSystem选择的建筑
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
            buildingSystem.SelectBuildingForPlacement(buildingId);
        }
        else
        {
        }
    }

    // 查找本地玩家
    NetworkPlayer FindLocalPlayer()
    {
        foreach (NetworkPlayer player in FindObjectsOfType<NetworkPlayer>())
        {
            if (player.isLocalPlayer)
                return player;
        }
        return null;
    }

    void Update()
    {
        // 检查T键切换建筑模式
        if (Input.GetKeyDown(KeyCode.T))
        {
            bool inCampArea = IsPlayerInCampArea();
            if (inCampArea)
            {
                if (isBuildingMode)
                {
                    ExitBuildingMode();
                }
                else
                {
                    EnterBuildingMode();
                }
            }
            else
            {
            }
        }
        
        // 检查玩家是否在营地区域内
        bool currentPlayerInCampArea = IsPlayerInCampArea();
        
        // 如果玩家不在营地区域，强制退出建筑模式
        if (currentPlayerInCampArea != isBuildingMode)
        {
            if (!currentPlayerInCampArea && isBuildingMode)
            {
                ExitBuildingMode();
            }
            // 如果在营地区域但不在建筑模式，可以考虑自动进入建筑模式
            // 但通常由UI按钮控制
        }
        
        // 持续检查并确保Grid_Plane状态正确
        EnsureGridPlaneState();
    }

    /// <summary>
    /// 隐藏所有建筑相关元素
    /// </summary>
    void HideAllBuildingElements()
    {
        // 隐藏建筑面板（BuildingPanel子对象）
        if (buildingPanel != null)
        {
            buildingPanel.SetActive(false);
        }
        
        // 隐藏背景面板（BackgroundPanel子对象）
        if (backgroundPanel != null)
        {
            backgroundPanel.SetActive(false);
        }
        
        // 隐藏网格平面
        if (gridPlane != null)
        {
            gridPlane.SetActive(false);
        }
    }
    
    /// <summary>
    /// 隐藏InventoryUI相关元素
    /// </summary>
    void HideInventoryUIElements()
    {
        // 隐藏InventoryUI下的BackgroundPanel_2
        if (inventoryBackgroundPanel != null)
        {
            inventoryBackgroundPanel.SetActive(false);
        }
        
        // 隐藏InventoryUI下的PreviewPanel
        if (inventoryPreviewPanel != null)
        {
            inventoryPreviewPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 显示InventoryUI相关元素
    /// </summary>
    void ShowInventoryUIElements()
    {
        // 显示InventoryUI下的BackgroundPanel_2
        if (inventoryBackgroundPanel != null)
        {
            inventoryBackgroundPanel.SetActive(true);
        }
        
        // 显示InventoryUI下的PreviewPanel
        if (inventoryPreviewPanel != null)
        {
            inventoryPreviewPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 尝试查找Grid_Plane对象（如果引用为空）
    /// </summary>
    private GameObject TryFindGridPlane()
    {
        if (gridPlane == null)
        {
            // 尝试在子对象中查找名为"Grid_Plane"的对象
            Transform gridTransform = transform.Find("Grid_Plane");
            if (gridTransform != null)
            {
                gridPlane = gridTransform.gameObject;
            }
            else
            {
                // 如果在直接子对象中没找到，搜索所有子对象
                gridPlane = gameObject.transform.Find("Grid_Plane")?.gameObject;
                if (gridPlane == null)
                {
                    // 最后尝试通过标签或组件查找
                    Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.name == "Grid_Plane" || renderer.name.Contains("Grid"))
                        {
                            gridPlane = renderer.gameObject;
                        break;
                        }
                    }
                }
            }
        }
        return gridPlane;
    }
    
    /// <summary>
    /// 检查并确保Grid_Plane处于正确状态
    /// </summary>
    private void EnsureGridPlaneState()
    {
        // 尝试获取Grid_Plane引用
        GameObject gridObj = TryFindGridPlane();
        
        if (gridObj != null)
        {
            // 如果不在建筑模式下，确保Grid_Plane被隐藏
            if (!isBuildingMode && gridObj.activeSelf)
            {
                gridObj.SetActive(false);
            }
            // 如果在建筑模式下，确保Grid_Plane被显示
            else if (isBuildingMode && !gridObj.activeSelf)
            {
                gridObj.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// 将建筑数据设置到指定槽位
    /// </summary>
    /// <param name="slotPanel">槽位面板</param>
    /// <param name="slotImage">槽位图片组件</param>
    /// <param name="nameText">名称文本组件</param>
    /// <param name="consumptionText">消耗文本组件</param>
    /// <param name="buildingData">建筑数据</param>
    private void SetBuildingDataToSlot(Transform slotPanel, Image slotImage, TMPro.TextMeshProUGUI nameText, TMPro.TextMeshProUGUI consumptionText, BuildingData buildingData)
    {
        // 设置建筑名称
        if (nameText != null)
        {
            nameText.text = buildingData.name;
        }
        
        // 设置消耗资源文本
        if (consumptionText != null)
        {
            consumptionText.text = GetResourceCostString(buildingData);
            
            // 添加到静态列表以便后续更新
            if (!allConsumptionTexts.Contains(consumptionText))
            {
                allConsumptionTexts.Add(consumptionText);
            }
        }
        
        // 异步加载并设置建筑图片
        if (slotImage != null && !string.IsNullOrEmpty(buildingData.imageAddressableKey))
        {
            LoadBuildingImage(slotImage, buildingData.imageAddressableKey);
        }
        
        // 添加点击事件（如果槽位面板有按钮组件）
        var button = slotPanel.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
        {
            button = slotPanel.gameObject.AddComponent<UnityEngine.UI.Button>();
        }
        
        // 确保按钮事件只添加一次
        button.onClick.RemoveAllListeners();
        string buildingIdCopy = buildingData.buildingId; // 避免闭包问题
        button.onClick.AddListener(() => SelectBuilding(buildingIdCopy));
    }
    
    /// <summary>
    /// 异步加载建筑图片
    /// </summary>
    /// <param name="image">图片组件</param>
    /// <param name="addressableKey">Addressable资源键</param>
    private async void LoadBuildingImage(Image image, string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey) || image == null)
            return;
        
        try
        {
            // 使用Addressables异步加载图片
            var loadOperation = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<UnityEngine.Sprite>(addressableKey);
            UnityEngine.Sprite sprite = await loadOperation.Task;
            
            if (sprite != null && image != null)
            {
                image.sprite = sprite;
            }
        }
        catch (System.Exception e)
        {
        }
        }
    /// <summary>
    /// 获取资源消耗字符串
    /// </summary>
    /// <param name="buildingData">建筑数据</param>
    /// <returns>资源消耗描述字符串</returns>
    private string GetResourceCostString(BuildingData buildingData)
    {
        if (buildingData.resourceCostList == null || buildingData.resourceCostList.Count == 0)
            return "无消耗";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < buildingData.resourceCostList.Count; i++)
        {
            var cost = buildingData.resourceCostList[i];
            
            // 检查ResourceDatabase是否已初始化
            if (ResourceDatabase.Instance == null)
            {
                // 如果ResourceDatabase未初始化，返回资源ID作为临时解决方案
                sb.Append($"{cost.resourceId}: {cost.amount}");
            }
            else
            {
                ResourceData resourceData = ResourceDatabase.Instance.GetResource(cost.resourceId);
                string resourceName = resourceData != null ? resourceData.name : cost.resourceId; // 如果资源不存在，使用ID作为名称
                sb.Append($"{resourceName}: {cost.amount}");
            }
            
            if (i < buildingData.resourceCostList.Count - 1)
                sb.Append("\n");
        }
        
        return sb.ToString();
    }

// 静态方法，供ResourceDatabase调用以更新所有consumptionText
public static void UpdateAllResourceCostTexts()
{
    foreach (var text in allConsumptionTexts)
    {
        if (text != null) // 检查UI元素是否已被销毁
        {
            // 由于我们没有保存consumptionText与BuildingData的映射关系，
            // 我们暂时不实现此功能，因为无法知道每个consumptionText对应的BuildingData
            // 更好的方法是使用BuildingSlotUI组件而不是直接操作TextMeshProUGUI
        }
    }
}
}
    