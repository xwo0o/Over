using Mirror;
using UnityEngine;
using System.Collections;

public class NetworkBuilding : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnBuildingIdChanged))]
    public string buildingId;
    
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;
    
    [SyncVar(hook = nameof(OnIsBuiltChanged))]
    public bool isBuilt = false;
    
    public int maxHealth = 100;
    public float buildTime = 3f; // 建造时间
    
    private BuildingData buildingData;
    private Renderer buildingRenderer;
    private Collider buildingCollider;
    
    void Start()
    {
        buildingRenderer = GetComponentInChildren<Renderer>();
        buildingCollider = GetComponent<Collider>();
        
        // 初始化时禁用碰撞体，建造完成后再启用
        if (buildingCollider != null)
        {
            buildingCollider.enabled = false;
            
            // 设置碰撞体参数以减少对CharacterController的影响
            // 如果是BoxCollider，调整其中心和大小
            if (buildingCollider is BoxCollider boxCollider)
            {
                // 确保盒子碰撞体不会影响角色的Y轴移动
                // 例如，如果这是一个椅子，我们可能只想要水平方向的阻挡
                if (boxCollider.size.y < 1.0f) // 对于低矮物体
                {
                    // 调整中心点，使其更贴近地面，同时确保角色可以正确地在上面或旁边移动
                    boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.size.y / 2f, boxCollider.center.z);
                }
            }
            // 如果是其他类型的碰撞体（如MeshCollider），也可以进行类似调整
            else if (buildingCollider is CapsuleCollider capsuleCollider)
            {
                if (capsuleCollider.height < 1.0f) // 对于低矮物体
                {
                    // 调整中心点，使其更贴近地面
                    capsuleCollider.center = new Vector3(capsuleCollider.center.x, capsuleCollider.height / 2f, capsuleCollider.center.z);
                }
            }
        }
        
        // 如果是服务器，初始化建筑
        if (isServer && !string.IsNullOrEmpty(buildingId))
        {
            InitializeBuilding();
        }
    }
    
    // 初始化建筑
    void InitializeBuilding()
    {
        buildingData = BuildingDataManager.Instance.GetBuildingData(buildingId);
        
        if (buildingData != null)
        {
            // 设置建筑名称
            gameObject.name = $"Building_{buildingData.name}";
            
            // 开始建造过程
            StartCoroutine(BuildingProcess());
        }
    }
    
    // 建筑过程协程
    IEnumerator BuildingProcess()
    {
        float elapsedTime = 0f;
        
        // 建造动画
        while (elapsedTime < buildTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / buildTime;
            
            // 更新建筑透明度
            UpdateBuildingAppearance(progress);
            
            yield return null;
        }
        
        // 建造完成
        isBuilt = true;
        currentHealth = maxHealth;
        
        // 启用碰撞体
        if (buildingCollider != null)
        {
            buildingCollider.enabled = true;
            
            // 确保碰撞体参数正确设置
            if (buildingCollider is BoxCollider boxCollider)
            {
                if (boxCollider.size.y < 1.0f) // 对于低矮物体
                {
                    // 调整中心点，使其更贴近地面
                    boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.size.y / 2f, boxCollider.center.z);
                }
            }
        }
        
        // 设置最终外观
        UpdateBuildingAppearance(1f);
        

    }
    
    // 更新建筑外观
    void UpdateBuildingAppearance(float progress)
    {
        if (buildingRenderer != null)
        {
            Color color = buildingRenderer.material.color;
            color.a = progress;
            buildingRenderer.material.color = color;
        }
    }
    
    // 应用伤害
    [Server]
    public void ApplyDamage(int damage)
    {
        if (!isBuilt) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            DestroyBuilding();
        }
    }
    
    // 销毁建筑
    [Server]
    void DestroyBuilding()
    {
        // 释放网格区域
        if (BuildingGrid.Instance != null && buildingData != null)
        {
            BuildingGrid.Instance.FreeArea(transform.position, buildingData.width, buildingData.height);
        }
        
        // 网络同步销毁
        NetworkServer.Destroy(gameObject);
    }
    
    // 建筑ID变化回调
    void OnBuildingIdChanged(string oldId, string newId)
    {
        buildingId = newId;
        
        if (!isServer && !string.IsNullOrEmpty(newId))
        {
            buildingData = BuildingDataManager.Instance.GetBuildingData(newId);
            
            if (buildingData != null)
            {
                gameObject.name = $"Building_{buildingData.name}";
            }
        }
    }
    
    // 生命值变化回调
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        currentHealth = newHealth;
        
        // 可以在这里添加生命值变化的视觉效果
        if (isBuilt && buildingRenderer != null)
        {
            float healthRatio = (float)newHealth / maxHealth;
            
            // 根据生命值比例改变颜色
            Color color = Color.Lerp(Color.red, Color.white, healthRatio);
            buildingRenderer.material.color = color;
        }
    }
    
    // 建造状态变化回调
    void OnIsBuiltChanged(bool oldValue, bool newValue)
    {
        isBuilt = newValue;
        
        if (newValue && buildingCollider != null)
        {
            buildingCollider.enabled = true;
            
            // 确保碰撞体参数正确设置
            if (buildingCollider is BoxCollider boxCollider)
            {
                if (boxCollider.size.y < 1.0f) // 对于低矮物体
                {
                    // 调整中心点，使其更贴近地面
                    boxCollider.center = new Vector3(boxCollider.center.x, boxCollider.size.y / 2f, boxCollider.center.z);
                }
            }
        }
    }
    
    // 获取建筑数据
    public BuildingData GetBuildingData()
    {
        return buildingData;
    }
    
    // 检查是否建造完成
    public bool IsBuilt()
    {
        return isBuilt;
    }
    
    // 获取建筑尺寸
    public Vector2Int GetBuildingSize()
    {
        if (buildingData != null)
        {
            return new Vector2Int(buildingData.width, buildingData.height);
        }
        return Vector2Int.one;
    }
}