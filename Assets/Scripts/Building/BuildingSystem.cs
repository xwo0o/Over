using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class BuildingSystem : NetworkBehaviour
{
    [Header("建筑预览")]
    [SerializeField] private Material previewMaterial; // 预览材质
    
    private BuildingData currentBuildingData;
    private GameObject previewInstance;
    private Renderer[] previewRenderers;
    private bool isInCampArea = false;
    private BuildingUIController uiController;
    private NetworkIdentity networkIdentity;
    
    // Addressables加载句柄
    private Dictionary<string, AsyncOperationHandle<GameObject>> prefabHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
    
    void Start()
    {
        // 在主机模式下，本地玩家也需要建筑功能
        // 检查是否拥有控制权（包括主机的本地客户端）
        if (!isOwned)
        {
            enabled = false;
            return;
        }
        
        // 获取网络身份
        networkIdentity = GetComponent<NetworkIdentity>();
        if (networkIdentity == null)
            {
                enabled = false;
                return;
            }
        
        // 查找UI控制器
        uiController = FindObjectOfType<BuildingUIController>();
        
        PreloadBuildingPrefabs();
        

    }
    
    public override void OnStartLocalPlayer()
    {
        // 确保本地玩家的建筑系统启用
        if (isOwned)
        {
            enabled = true;
        }
    }
    
    public override void OnStopLocalPlayer()
    {
        // 确保在不再拥有控制权时禁用建筑系统
        if (isOwned)
        {
            enabled = false;
        }
    }
    
    void Update()
    {
        // 在主机模式下，本地玩家也需要建筑功能
        // 检查是否拥有控制权（包括主机的本地客户端）
        if (!isOwned)
        {
            return;
        }
        
        // 确保组件已启用
        if (!enabled)
        {
            return;
        }
        
        // 检查是否在营地范围内
        CheckCampArea();
        
        // 更新建筑预览
        UpdateBuildingPreview();
        
        // 处理建造输入
        HandleBuildingInput();
    }
    
    // 检查是否在营地范围内
    void CheckCampArea()
    {
        // 复用现有的HealingZone组件进行营地区域检测
        if (uiController != null)
        {
            // 如果UI控制器显示建筑模式，说明在营地范围内
            isInCampArea = uiController.IsInBuildingMode();
        }
        else
        {
            // 备用方法：通过标签检测是否在yingdi平面
            if (Camera.main == null) 
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 200f))
            {
                bool wasInCampArea = isInCampArea;
                isInCampArea = hit.collider.CompareTag("yingdi");

                if (wasInCampArea != isInCampArea)
                {
                }

                // 更新预览显示
                if (previewInstance != null)
                {
                    bool shouldShowPreview = isInCampArea && currentBuildingData != null;
                    previewInstance.SetActive(shouldShowPreview);
                }
            }
            else
            {
                // 当射线未击中任何对象时，不在营地区域内
                if (isInCampArea)
                {
                    isInCampArea = false;

                    if (previewInstance != null)
                    {
                        previewInstance.SetActive(false);
                    }
                }
            }
        }
        
        // 额外的安全检查：确保本地玩家的建筑状态是正确的
        if (isOwned && uiController == null)
        {
            // 如果UI控制器为空，尝试重新查找
            uiController = FindObjectOfType<BuildingUIController>();
        }
    }
    
    // 预热建筑预制体
    async void PreloadBuildingPrefabs()
    {
        List<BuildingData> allBuildings = BuildingDataManager.Instance.GetAllBuildingData();
        
        foreach (BuildingData building in allBuildings)
        {
            if (!string.IsNullOrEmpty(building.addressableKey))
            {
                try
                {
                    var handle = Addressables.LoadAssetAsync<GameObject>(building.addressableKey);
                    await handle.Task;
                    
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        prefabHandles[building.buildingId] = handle;
                    }
                }
                catch (System.Exception e)
                {
                }
            }
        }
    }
    
    // 更新建筑预览 - 射线检测逻辑
        void UpdateBuildingPreview()
        {
            if (currentBuildingData == null)
            {
                if (previewInstance != null)
                {
                    previewInstance.SetActive(false);
                }
                return;
            }
            
            if (!isInCampArea)
            {
                if (previewInstance != null)
                    previewInstance.SetActive(false);
                return;
            }
            
            Camera cam = Camera.main;
            if (cam == null)
                return;
            
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // 检测yingdi标签的地面
            if (Physics.Raycast(ray, out hit, 200f, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.CompareTag("yingdi"))
                {
                    // 获取网格对齐的位置，强制Y轴为0
                    Vector3 hitPoint = hit.point;
                    Vector3 gridAlignedPos = new Vector3(hitPoint.x, 0f, hitPoint.z);
                    if (BuildingGrid.Instance != null)
                    {
                        // 对X和Z坐标进行网格对齐，Y轴强制为0
                        Vector2Int gridCell = BuildingGrid.Instance.WorldToCell(new Vector3(hitPoint.x, 0, hitPoint.z));
                        Vector3 gridWorldPos = BuildingGrid.Instance.CellToWorld(gridCell);
                        gridAlignedPos = new Vector3(gridWorldPos.x, 0f, gridWorldPos.z);
                    }
                    
                    // 创建或更新预览实例
                    if (previewInstance == null)
                    {
                        CreatePreviewInstance();
                    }
                    
                    if (previewInstance != null)
                    {
                        previewInstance.SetActive(true);
                        previewInstance.transform.position = gridAlignedPos;
                    }
                    
                    bool canBuild = CanBuildHere(gridAlignedPos);
                    UpdatePreviewColor(canBuild);
                }
                else
                {
                    // 如果击中了非地面对象，隐藏预览
                    if (previewInstance != null)
                        previewInstance.SetActive(false);
                }
            }
            else
            {
                // 如果没有击中任何对象，隐藏预览
                if (previewInstance != null)
                    previewInstance.SetActive(false);
            }
        }
    
    // 创建预览实例
    async void CreatePreviewInstance()
    {
        if (currentBuildingData == null || string.IsNullOrEmpty(currentBuildingData.addressableKey))
            return;
        
        try
        {
            // 异步加载预览预制体
            var handle = Addressables.LoadAssetAsync<GameObject>(currentBuildingData.addressableKey);
            GameObject prefab = await handle.Task;
            
            if (prefab != null)
            {
                previewInstance = Instantiate(prefab);
                previewRenderers = previewInstance.GetComponentsInChildren<Renderer>();
                
                // 设置预览材质
                foreach (var renderer in previewRenderers)
                {
                    if (previewMaterial != null)
                    {
                        renderer.material = previewMaterial;
                    }
                }
                
                // 禁用碰撞体
                var colliders = previewInstance.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                {
                    collider.enabled = false;
                }
            }
        }
        catch (System.Exception e)
        {
        }
    }
    
    // 检查是否可以在此处建造
    bool CanBuildHere(Vector3 position)
    {
        
        if (BuildingGrid.Instance == null || currentBuildingData == null)
        {
            return false;
        }
        
        if (!BuildingGrid.Instance.IsOnCamp(position))
        {
            return false;
        }
        
        if (!BuildingGrid.Instance.IsAreaFree(position, currentBuildingData.width, currentBuildingData.height))
        {
            return false;
        }
        
        // 检查资源是否足够
        NetworkPlayer player = GetComponent<NetworkPlayer>();
        
        if (player == null)
        {
            return false;
        }
        
        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return false;
        }
        
        bool hasResources = HasEnoughResources(inventory, currentBuildingData);
        
        return hasResources;
    }
    
    // 检查资源是否足够
    bool HasEnoughResources(Inventory inventory, BuildingData data)
    {
        if (data.resourceCostList == null)
            return true;
        
        foreach (var cost in data.resourceCostList)
        {
            if (!inventory.HasEnough(cost.resourceId, cost.amount))
                return false;
        }
        
        return true;
    }
    
    // 更新预览颜色
    void UpdatePreviewColor(bool canBuild)
    {
        if (previewRenderers == null)
            return;
        
        Color color = canBuild ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        foreach (var r in previewRenderers)
        {
            if (r.material != null)
            {
                r.material.color = color;
            }
        }
    }
    
    // 处理建造输入
        void HandleBuildingInput()
        {
            // 检查是否有选择的建筑和是否在营地区域
            if (currentBuildingData == null || !isInCampArea)
            {
                return;
            }
            
            // 检测鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击在UI上，如果是则忽略建造输入
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                
                Camera cam = Camera.main;
                if (cam == null) return;

                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                // 检测yingdi标签的地面
                if (Physics.Raycast(ray, out hit, 200f, ~0, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider.CompareTag("yingdi"))
                    {
                        // 尝试在指定位置建造 - 使用网格对齐的位置，Y轴强制为0
                        Vector3 hitPoint = hit.point;
                        Vector3 gridAlignedPos = new Vector3(hitPoint.x, 0f, hitPoint.z);
                        if (BuildingGrid.Instance != null)
                        {
                            // 对X和Z坐标进行网格对齐，Y轴强制为0
                            Vector2Int gridCell = BuildingGrid.Instance.WorldToCell(new Vector3(hitPoint.x, 0, hitPoint.z));
                            Vector3 gridWorldPos = BuildingGrid.Instance.CellToWorld(gridCell);
                            gridAlignedPos = new Vector3(gridWorldPos.x, 0f, gridWorldPos.z);
                        }
                        
                        AttemptBuildAtPosition(gridAlignedPos);
                    }
                }
            }
        }
    
    // 在指定位置尝试建造
    public void AttemptBuildAtPosition(Vector3 position)
    {
        
        if (currentBuildingData == null)
        {
            return;
        }

        // 位置已经经过网格对齐处理（X和Z坐标对齐，Y坐标保持原始高度）
        // 直接使用传入的位置，不需要再次对齐
        Vector3 gridPosition = new Vector3(position.x, 0f, position.z);
            

        if (CanBuildHere(gridPosition))
        {
            CmdTryBuild(currentBuildingData.buildingId, gridPosition);
        }
        else
        {
        }
    }
    
    // 选择建筑
    public void SelectBuilding(string buildingId)
    {
        BuildingData data = BuildingDataManager.Instance.GetBuildingData(buildingId);
        if (data != null)
        {
            currentBuildingData = data;
            
            // 销毁旧预览
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
                previewRenderers = null;
            }
            

        }
    }
    
    // 选择建筑用于放置（供BuildingUIController调用）
    public void SelectBuildingForPlacement(string buildingId)
    {
        BuildingData data = BuildingDataManager.Instance.GetBuildingData(buildingId);
        if (data != null)
        {
            currentBuildingData = data;
            
            // 销毁旧预览
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
                previewRenderers = null;
            }
            

        }
        else
        {
        }
    }

    // 取消建筑选择
    public void CancelBuildingSelection()
    {
        currentBuildingData = null;
        
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
            previewRenderers = null;
        }
        

    }
    
    // 获取是否在建筑模式
    public bool IsInBuildingMode()
    {
        return currentBuildingData != null && isInCampArea;
    }
    
    // 网络命令：尝试建造
    [Command]
    void CmdTryBuild(string buildingId, Vector3 position)
    {
        
        if (BuildingGrid.Instance == null)
        {
            return;
        }
        
        BuildingData data = BuildingDataManager.Instance.GetBuildingData(buildingId);
        if (data == null)
        {
            return;
        }
        
        if (!BuildingGrid.Instance.IsOnCamp(position))
        {
            return;
        }
        
        if (!BuildingGrid.Instance.IsAreaFree(position, data.width, data.height))
        {
            return;
        }
        
        NetworkPlayer player = GetComponent<NetworkPlayer>();
        if (player == null)
        {
            return;
        }
        
        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return;
        }
        
        if (!HasEnoughResources(inventory, data))
        {
            return;
        }
        
        // 消耗资源
        ConsumeResources(inventory, data);
        
        // 建造建筑
        BuildStructure(buildingId, position);
    }
    
    // 消耗资源
    [Server]
    void ConsumeResources(Inventory inventory, BuildingData data)
    {
        if (data.resourceCostList == null)
            return;
        
        // 记录消耗的资源信息用于调试和UI反馈
        string resourceLog = $"建造 {data.name} 消耗资源：";
        bool hasConsumed = false;
        
        foreach (var cost in data.resourceCostList)
        {
            if (inventory.Consume(cost.resourceId, cost.amount))
            {
                resourceLog += $" {cost.resourceId}:{cost.amount}";
                hasConsumed = true;
                
                // 同步资源消耗到客户端（通过SyncVar自动处理）
            }
            else
            {
            }
        }
        
        if (hasConsumed)
        {
            
            // 触发客户端资源更新事件
            RpcOnResourcesConsumed(data.buildingId, data.resourceCostList);
        }
    }
    
    // 客户端资源消耗回调
    [ClientRpc]
    void RpcOnResourcesConsumed(string buildingId, List<BuildingResourceCost> consumedResources)
    {
        if (!isOwned) return;
        
        // 更新本地UI显示
        BuildingData data = BuildingDataManager.Instance.GetBuildingData(buildingId);
        if (data != null)
        {
            string message = $"建造 {data.name} 消耗：";
            foreach (var cost in consumedResources)
            {
                message += $" {cost.resourceId}:{cost.amount}";
            }
            
            
            // 可以在这里添加UI提示或特效
            ShowResourceConsumptionEffect(consumedResources);
        }
    }
    
    // 显示资源消耗效果
    void ShowResourceConsumptionEffect(List<BuildingResourceCost> consumedResources)
    {
        // 可以在这里实现资源消耗的视觉反馈
        // 比如播放特效、显示提示文本等
        
        // 示例：查找背包UI控制器并触发刷新
        InventoryUIController uiController = FindObjectOfType<InventoryUIController>();
        if (uiController != null)
        {
            uiController.Refresh();
        }
    }
    
    // 建造建筑
    [Server]
    void BuildStructure(string buildingId, Vector3 position)
    {
        BuildingData data = BuildingDataManager.Instance.GetBuildingData(buildingId);
        if (data == null)
        {
            return;
        }
        


        // 异步加载并实例化建筑
        StartCoroutine(BuildStructureCoroutine(buildingId, position, data));
    }
    
    // 建造协程
    System.Collections.IEnumerator BuildStructureCoroutine(string buildingId, Vector3 position, BuildingData data)
    {
        
        // 异步加载预制体
        var loadOperation = Addressables.LoadAssetAsync<GameObject>(data.addressableKey);
        yield return loadOperation;
        
        if (loadOperation.Status == AsyncOperationStatus.Succeeded)
        {
            
            GameObject prefab = loadOperation.Result;
            GameObject buildingObj = Instantiate(prefab, position, Quaternion.identity);
            
            // 设置建筑数据
            NetworkBuilding networkBuilding = buildingObj.GetComponent<NetworkBuilding>();
            if (networkBuilding != null)
            {
                networkBuilding.buildingId = buildingId;
            }
            
            // 网络同步
            NetworkServer.Spawn(buildingObj);
            
            // 占用网格区域
            BuildingGrid.Instance.OccupyArea(position, data.width, data.height);
            
        }
        else
        {
        }
    }
    
    void OnDestroy()
    {
        // 释放Addressables资源
        foreach (var handle in prefabHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        prefabHandles.Clear();
    }
}