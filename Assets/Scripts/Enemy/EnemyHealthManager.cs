using UnityEngine;
using Mirror;

/// <summary>
/// 敌人血条管理系统
/// 负责敌人血条的创建和管理，保持与角色血条系统的一致性
/// </summary>
public class EnemyHealthManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;
    
    [SyncVar]
    public int maxHealth;
    
    [SyncVar]
    public string enemyType;
    
    // 血条UI相关
    public GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private bool healthBarCreated = false;
    
    private EnemyAIController enemyController;
    private EnemyData enemyData;
    
    /// <summary>
    /// 初始化敌人血条系统
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // 获取敌人控制器
        enemyController = GetComponent<EnemyAIController>();
        if (enemyController != null)
        {
            enemyType = enemyController.enemyType;
        }
        
        // 检查游戏对象名称，尝试从名称中获取敌人类型
        if (string.IsNullOrEmpty(enemyType))
        {
            string objName = gameObject.name;
            // 处理克隆对象名称，如 "BigEnemy(Clone)" -> "BigEnemy"
            if (objName.Contains("(Clone)"))
            {
                enemyType = objName.Split('(')[0].Trim();
                Debug.Log("[EnemyHealthManager] 从游戏对象名称获取敌人类型: " + enemyType);
            }
            else
            {
                // 如果不包含 "(Clone)"，直接使用游戏对象名称作为敌人类型
                enemyType = objName;
                Debug.Log("[EnemyHealthManager] 从游戏对象名称获取敌人类型: " + enemyType);
            }
        }
        
        // 初始化敌人数据
        InitializeEnemyData();
    }
    
    /// <summary>
    /// 客户端启动时创建血条UI
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // 在所有客户端（包括主机）上创建血条UI
        if (isClient)
        {
            CreateHealthBarOnClient();
        }
    }
    
    /// <summary>
    /// 当对象从对象池重新激活时调用
    /// </summary>
    void OnEnable()
    {
        // 只在服务器端处理
        if (!NetworkServer.active)
            return;
        
        // 如果血条已经创建过，说明这是从对象池重新激活的对象
        // 需要重新初始化血量数据和血条
        if (healthBarCreated || currentHealth == 0)
        {
            Debug.Log($"[EnemyHealthManager] 对象从对象池重新激活 - 类型: {enemyType}");
            
            // 重新初始化敌人数据
            InitializeEnemyData();
            
            // 通知所有客户端重新创建血条UI
            RpcRecreateHealthBar();
        }
    }
    
    /// <summary>
    /// 客户端RPC：重新创建血条UI
    /// </summary>
    [ClientRpc]
    private void RpcRecreateHealthBar()
    {
        try
        {
            // 销毁旧的血条UI实例
            if (healthBarInstance != null)
            {
                Destroy(healthBarInstance);
                healthBarInstance = null;
            }
            
            // 重置血条创建标志
            healthBarCreated = false;
            
            // 重新创建血条UI
            CreateHealthBarOnClient();
            
            Debug.Log($"[EnemyHealthManager] 客户端重新创建血条 - 类型: {enemyType}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] RpcRecreateHealthBar方法异常: " + ex.Message);
        }
    }
    
    /// <summary>
    /// 从EnemyAIController接收敌人数据并初始化
    /// </summary>
    /// <param name="data">敌人数据</param>
    public void InitializeFromEnemyData(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[EnemyHealthManager] InitializeFromEnemyData: 敌人数据为空");
            return;
        }
        
        enemyData = data;
        enemyType = data.enemyType;
        maxHealth = data.health;
        currentHealth = data.health;
        
        Debug.Log($"[EnemyHealthManager] 从EnemyAIController接收敌人数据 - 类型: {enemyType}, 血量: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 初始化敌人数据
    /// </summary>
    private void InitializeEnemyData()
    {
        try
        {
            // 如果已经通过InitializeFromEnemyData初始化过了，则跳过
            if (enemyData != null && maxHealth > 0)
            {
                Debug.Log("[EnemyHealthManager] 敌人数据已经初始化，跳过重复初始化");
                CreateHealthBar();
                return;
            }
            
            // 确保enemyType有默认值
            if (string.IsNullOrEmpty(enemyType))
            {
                enemyType = "DefaultEnemy";
                Debug.LogWarning("[EnemyHealthManager] 敌人类型为空，设置默认类型: " + enemyType);
            }
            
            if (EnemyDatabase.GetInstance() == null)
            {
                Debug.LogWarning("[EnemyHealthManager] EnemyDatabase未初始化，使用默认值");
                // 使用默认值
                maxHealth = 100;
                currentHealth = 100;
                CreateHealthBar();
                return;
            }

            Debug.Log("[EnemyHealthManager] 尝试从数据库获取敌人数据 - 类型: " + enemyType);

            // 从数据库获取敌人数据
            enemyData = EnemyDatabase.GetInstance().GetEnemy(enemyType);
            if (enemyData != null)
            {
                maxHealth = enemyData.health;
                currentHealth = enemyData.health;
            }
            else
            {
                Debug.LogWarning("[EnemyHealthManager] 未找到敌人数据: " + enemyType + ", 尝试使用对象名称作为备选");
                
                // 尝试使用对象名称作为备选
                string objName = gameObject.name;
                if (objName.Contains("(Clone)"))
                {
                    string altType = objName.Split('(')[0].Trim();
                    enemyData = EnemyDatabase.GetInstance().GetEnemy(altType);
                    if (enemyData != null)
                    {
                        maxHealth = enemyData.health;
                        currentHealth = enemyData.health;
                        Debug.Log("[EnemyHealthManager] 从备选类型获取敌人数据 - 类型: " + altType + ", 血量: " + currentHealth + "/" + maxHealth);
                        CreateHealthBar();
                        return;
                    }
                }
                
                // 最后使用默认值
                Debug.LogWarning("[EnemyHealthManager] 使用默认值初始化敌人数据");
                maxHealth = 100;
                currentHealth = 100;
            }
            
            CreateHealthBar();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] InitializeEnemyData方法异常: " + ex.Message);
            // 确保敌人初始血量不为0
            maxHealth = 100;
            currentHealth = 100;
            CreateHealthBar();
        }
    }
    
    /// <summary>
    /// 创建敌人血条
    /// </summary>
    [Server]
    private void CreateHealthBar()
    {
        try
        {
            // 服务器端不再创建血条UI，血条UI只在客户端上创建
            // 标记血条已创建，避免重复调用
            healthBarCreated = true;
            Debug.Log($"[EnemyHealthManager] 服务器端血条创建标记 - 类型: {enemyType}, 血量: {currentHealth}/{maxHealth}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] CreateHealthBar方法异常: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
    
    /// <summary>
    /// 在客户端上创建血条UI
    /// </summary>
    private void CreateHealthBarOnClient()
    {
        try
        {
            if (healthBarPrefab == null)
            {
                Debug.LogError("[EnemyHealthManager] 血条预制体未设置，无法创建血条");
                return;
            }
            
            if (healthBarInstance != null)
            {
                Debug.LogWarning("[EnemyHealthManager] 血条已经创建过了");
                return;
            }
            
            // 实例化血条
            healthBarInstance = Instantiate(healthBarPrefab, transform);
            healthBarInstance.transform.localPosition = Vector3.up * 2f;
            
            // 设置血条目标为自身
            HealthBarUI healthBarUI = healthBarInstance.GetComponent<HealthBarUI>();
            if (healthBarUI != null)
            {
                healthBarUI.SetTarget(this);
            }
            
            healthBarCreated = true;
            Debug.Log($"[EnemyHealthManager] 客户端血条已创建 - 类型: {enemyType}, 血量: {currentHealth}/{maxHealth}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] CreateHealthBarOnClient方法异常: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
    
    /// <summary>
    /// 应用伤害
    /// </summary>
    /// <param name="amount">伤害值</param>
    [Server]
    public void ApplyDamage(int amount)
    {
        try
        {
            // 确保伤害值为正数
            amount = Mathf.Abs(amount);
            
            // 计算新的血量
            int newHealth = currentHealth - amount;
            
            // 确保血量不会小于0
            newHealth = Mathf.Max(0, newHealth);
            
            Debug.Log("[EnemyHealthManager] 敌人受伤 - 类型: " + enemyType + ", 当前血量: " + currentHealth + ", 伤害: " + amount + ", 新血量: " + newHealth);
            
            // 更新当前血量
            currentHealth = newHealth;
            
            // 检查是否死亡
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] ApplyDamage方法异常: " + ex.Message);
        }
    }
    
    /// <summary>
    /// 处理敌人死亡
    /// </summary>
    [Server]
    private void Die()
    {
        try
        {
            Debug.Log("[EnemyHealthManager] 敌人死亡 - 类型: " + enemyType);
            
            // 调用EnemyAIController的死亡处理，该方法会启动延迟回收协程
            if (enemyController != null)
            {
                enemyController.Die();
            }
            else
            {
                // 如果没有AI控制器，直接回收对象
                string poolId = GetPoolIdForEnemy(enemyType);
                if (AutoObjectPoolManager.Instance != null)
                {
                    ResetPoolState();
                    AutoObjectPoolManager.Instance.ReturnObject(poolId, gameObject);
                }
                else
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] Die方法异常: " + ex.Message);
            // 确保敌人被销毁（如果对象池不可用）
            if (AutoObjectPoolManager.Instance == null)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
    
    /// <summary>
    /// 血量变化回调
    /// </summary>
    /// <param name="oldValue">旧血量</param>
    /// <param name="newValue">新血量</param>
    private void OnHealthChanged(int oldValue, int newValue)
    {
        try
        {
            currentHealth = newValue;
            Debug.Log("[EnemyHealthManager] 血量同步 - 类型: " + enemyType + ", 旧值: " + oldValue + ", 新值: " + newValue);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] OnHealthChanged方法异常: " + ex.Message);
        }
    }
    
    /// <summary>
    /// 获取当前血量百分比
    /// </summary>
    /// <returns>血量百分比 (0-1)</returns>
    public float GetHealthPercentage()
    {
        try
        {
            if (maxHealth <= 0)
                return 0f;
            
            return (float)currentHealth / maxHealth;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] GetHealthPercentage方法异常: " + ex.Message);
            return 0f;
        }
    }
    
    /// <summary>
    /// 重置对象池状态（当对象从对象池重新获取时调用）
    /// </summary>
    public void ResetPoolState()
    {
        try
        {
            // 重置血条创建标志
            healthBarCreated = false;
            
            // 重置血条实例
            if (healthBarInstance != null)
            {
                NetworkServer.Destroy(healthBarInstance);
                healthBarInstance = null;
            }
            
            // 重置敌人数据引用
            enemyData = null;
            
            // 重置血量数据（将在OnStartServer中重新初始化）
            currentHealth = 0;
            maxHealth = 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[EnemyHealthManager] ResetPoolState方法异常: " + ex.Message);
        }
    }
    
    /// <summary>
    /// 根据敌人类型获取对应的对象池ID
    /// </summary>
    /// <param name="enemyType">敌人类型</param>
    /// <returns>对象池ID</returns>
    private string GetPoolIdForEnemy(string enemyType)
    {
        // 根据敌人类型确定对象池ID
        if (enemyType.Contains("SmallEnemy") || enemyType.Contains("Small"))
        {
            return "SmallEnemy";
        }
        else if (enemyType.Contains("BigEnemy") || enemyType.Contains("Big"))
        {
            return "BigEnemy";
        }
        else if (enemyType.Contains("FastEnemy") || enemyType.Contains("Fast"))
        {
            return "FastEnemy";
        }
        else
        {
            // 默认返回通用敌人池ID
            return "SmallEnemy"; // 默认使用小敌人池
        }
    }
}