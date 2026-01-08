using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResourceNode : NetworkBehaviour
{
    public string resourceId;
    public int amount = 1;
    public float triggerRadius = 2f;
    public float uiHeightOffset = 1.5f;
    public GameObject pickupUIPrefab;

    private SphereCollider triggerCollider;
    private GameObject pickupUIInstance;
    private ResourcePickupUIController uiController;
    private Camera mainCamera;
    private bool pickupUICreated = false;
    
    // 服务器权威：pickupUI的显示状态
    [SyncVar(hook = nameof(OnIsPickupUIVisibleChanged))]
    private bool isPickupUIVisible = false;

    void Awake()
    {
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;
        
        Debug.Log($"[ResourceNode] Awake初始化 - resourceId: {resourceId}, amount: {amount}");
    }

    public override void OnStartServer()
    {
        // 服务器启动时初始化pickupUI显示状态
        pickupUICreated = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // 在所有客户端（包括主机）上创建pickupUI
        if (isClient)
        {
            CreatePickupUIOnClient();
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (pickupUIInstance != null && pickupUIInstance.activeSelf)
        {
            UpdateUIRotation();
        }
    }

    // 服务器端检测玩家是否在触发区域内
    void FixedUpdate()
    {
        if (!isServer)
            return;

        // 检测是否有玩家在触发区域内
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, triggerRadius);
        bool playerNearby = false;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player_new"))
            {
                playerNearby = true;
                break;
            }
        }

        // 更新pickupUI的显示状态
        if (playerNearby != isPickupUIVisible)
        {
            isPickupUIVisible = playerNearby;
            UpdatePickupUIVisibility();
        }
    }

    // SyncVar回调：当isPickupUIVisible在客户端上变化时调用
    private void OnIsPickupUIVisibleChanged(bool oldValue, bool newValue)
    {
        UpdatePickupUIVisibility();
    }

    /// <summary>
    /// 当玩家进入触发区域时调用
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player_new"))
        {
            NetworkPlayer player = other.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                PlayerInputHandler inputHandler = player.GetComponent<PlayerInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.SetNearbyResource(this);
                    Debug.Log($"[ResourceNode] 玩家进入资源范围 - 资源ID: {resourceId}, 玩家: {player.netId}");
                }
            }
        }
    }

    /// <summary>
    /// 当玩家离开触发区域时调用
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player_new"))
        {
            NetworkPlayer player = other.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                PlayerInputHandler inputHandler = player.GetComponent<PlayerInputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.SetNearbyResource(null);
                    Debug.Log($"[ResourceNode] 玩家离开资源范围 - 资源ID: {resourceId}, 玩家: {player.netId}");
                }
            }
        }
    }

    private void UpdatePickupUIVisibility()
    {
        if (pickupUIInstance != null)
        {
            pickupUIInstance.SetActive(isPickupUIVisible);
        }
    }

    [Server]
    private void CreatePickupUI()
    {
        // 服务器端不再创建pickupUI，改为在客户端上创建
        pickupUICreated = true;
    }

    /// <summary>
    /// 在客户端上创建pickupUI
    /// </summary>
    private void CreatePickupUIOnClient()
    {
        try
        {
            if (pickupUIPrefab == null)
            {
                Debug.LogError("[ResourceNode] pickupUIPrefab未设置，无法创建pickupUI");
                return;
            }
            
            if (pickupUIInstance != null)
            {
                Debug.LogWarning("[ResourceNode] pickupUI已经创建过了");
                return;
            }
            
            // 实例化pickupUI作为ResourceNode的子对象
            pickupUIInstance = Instantiate(pickupUIPrefab, transform);
            pickupUIInstance.transform.localPosition = Vector3.up * uiHeightOffset;
            
            // 设置UI控制器
            uiController = pickupUIInstance.GetComponent<ResourcePickupUIController>();
            if (uiController != null)
            {
                uiController.ShowPickupUI();
                Debug.Log($"[ResourceNode] 客户端pickupUI已创建 - 资源ID: {resourceId}, 数量: {amount}");
            }
            else
            {
                Debug.LogError("[ResourceNode] pickupUI实例上没有ResourcePickupUIController组件");
            }
            
            pickupUICreated = true;
            
            // 根据当前是否有玩家在附近设置pickupUI的初始显示状态
            UpdatePickupUIVisibility();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ResourceNode] CreatePickupUIOnClient方法异常: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private void UpdateUIRotation()
    {
        if (pickupUIInstance == null || mainCamera == null)
            return;

        pickupUIInstance.transform.LookAt(mainCamera.transform.position);
        pickupUIInstance.transform.Rotate(0, 180, 0);
    }

    void OnDestroy()
    {
        if (pickupUIInstance != null)
        {
            if (NetworkServer.active)
            {
                NetworkServer.Destroy(pickupUIInstance);
            }
            else
            {
                Destroy(pickupUIInstance);
            }
            pickupUIInstance = null;
            uiController = null;
        }
    }

    [Server]
    public void ServerCollect(NetworkPlayer player)
    {
        if (player == null)
        {
            Debug.LogError($"[ResourceNode] ServerCollect失败：player为null");
            return;
        }
        
        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            Debug.LogError($"[ResourceNode] ServerCollect失败：无法获取Inventory组件 - 玩家: {player.netId}");
            return;
        }
        
        Debug.Log($"[ResourceNode] ServerCollect - 资源ID: {resourceId}, 数量: {amount}, 玩家: {player.netId}");
        
        if (!inventory.CanAdd(resourceId, amount))
        {
            Debug.LogWarning($"[ResourceNode] ServerCollect失败：背包已满或无法添加 - 资源ID: {resourceId}");
            return;
        }

        bool success = inventory.Add(resourceId, amount);
        if (success)
        {
            Debug.Log($"[ResourceNode] ServerCollect成功：已添加资源 - 资源ID: {resourceId}, 数量: {amount}, 玩家: {player.netId}");
            
            // 从对象池返回资源对象，而不是销毁它
            if (AutoObjectPoolManager.Instance != null)
            {
                string poolId = GetPoolIdForResource(resourceId);
                AutoObjectPoolManager.Instance.ReturnObject(poolId, gameObject);
            }
            else
            {
                // 如果对象池不存在，回退到销毁对象
                NetworkServer.Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogError($"[ResourceNode] ServerCollect失败：添加资源失败 - 资源ID: {resourceId}");
        }
    }

    /// <summary>
    /// 根据资源ID获取对应的对象池ID
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <returns>对象池ID</returns>
    private string GetPoolIdForResource(string resourceId)
    {
        // 根据资源ID确定对象池ID
        // 对象池ID与配置文件中的poolId保持一致
        if (resourceId.StartsWith("Wood"))
        {
            return "Wood";
        }
        else if (resourceId.StartsWith("Stone"))
        {
            return "Stone";
        }
        else if (resourceId.StartsWith("Ore"))
        {
            return "Ore";
        }
        else if (resourceId.StartsWith("Apple"))
        {
            return "Apple";
        }
        else if (resourceId.StartsWith("Pear"))
        {
            return "Pear";
        }
        else
        {
            // 默认返回通用资源池ID
            return resourceId;
        }
    }
}
