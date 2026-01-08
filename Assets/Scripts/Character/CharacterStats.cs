using UnityEngine;
using Mirror;

public class CharacterStats : NetworkBehaviour
{
    [SyncVar]
    public string characterId;
    
    [SyncVar]
    public int maxHealth;
    
    [SyncVar]
    public int attack;
    
    [SyncVar]
    public float moveSpeed;
    
    [SyncVar]
    public string specialAbility;
    
    [SyncVar]
    public float specialValue;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;

    public GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private bool healthBarCreated = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // 服务器启动时初始化血条创建状态
        healthBarCreated = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // 在所有客户端（包括主机）上创建血条UI
        if (isClient)
        {
            CreateHealthBarOnClient();
        }
    }

    void Start()
    {
        try
        {
            // 增强null检查，确保gameObject存在
            if (gameObject == null)
            {
                Debug.LogError("[CharacterStats] Start方法异常: gameObject为null");
                return;
            }
            
            string objectName = gameObject != null ? gameObject.name : "未知对象";
            string objectTag = gameObject != null && gameObject.tag != null ? gameObject.tag : "无标签";
            string layerName = gameObject != null ? LayerMask.LayerToName(gameObject.layer) : "未知层级";
            string characterIdStr = string.IsNullOrEmpty(characterId) ? "空" : characterId;
            string healthBarPrefabName = healthBarPrefab != null ? healthBarPrefab.name : "空";
            
            Debug.Log("[CharacterStats] Start方法调用 - gameObject: " + objectName + ", Tag: " + objectTag + ", Layer: " + layerName);
            
            // 安全检查：避免直接访问isServer和isClient，因为在对象池激活时可能尚未初始化
            bool isServerValue = false;
            bool isClientValue = false;
            try
            {
                isServerValue = isServer;
                isClientValue = isClient;
                Debug.Log("[CharacterStats] Start - isServer: " + isServerValue + ", isClient: " + isClientValue + ", characterId: " + characterIdStr);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CharacterStats] 无法访问网络属性，可能是对象池激活时的时序问题: " + ex.Message);
            }
            
            Debug.Log("[CharacterStats] Start - healthBarPrefab: " + healthBarPrefabName + ", healthBarCreated: " + healthBarCreated);
            
            // 创建血条（仅用于玩家角色和其他需要血条的实体）
            if (!healthBarCreated)
            {
                try
                {
                    if (isServer)
                    {
                        Debug.Log("[CharacterStats] Start - 调用CreateHealthBar创建血条");
                        CreateHealthBar();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[CharacterStats] 无法在Start中访问isServer，将在后续时机创建血条: " + ex.Message);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CharacterStats] Start方法异常: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    public void InitializeCharacterData(CharacterData data)
    {
        try
        {
            if (data == null)
            {
                Debug.LogWarning("[CharacterStats] InitializeCharacterData - data为null");
                return;
            }

            characterId = data.id;
            maxHealth = data.health;
            attack = data.attack;
            moveSpeed = data.speed;
            specialAbility = data.specialAbility;
            specialValue = data.specialValue;
            currentHealth = data.health;

            Debug.Log("[CharacterStats] 角色数据已初始化 - ID: " + characterId + ", 血量: " + currentHealth + "/" + maxHealth);

            if (!healthBarCreated && isServer)
            {
                CreateHealthBar();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CharacterStats] InitializeCharacterData方法异常: " + ex.Message);
        }
    }

    void CreateHealthBar()
    {
        // 服务器端不再创建血条，改为在客户端上创建
        healthBarCreated = true;
    }

    /// <summary>
    /// 在客户端上创建血条UI
    /// </summary>
    private void CreateHealthBarOnClient()
    {
        try
        {
            string healthBarPrefabName = healthBarPrefab != null ? healthBarPrefab.name : "空";
            Debug.Log("[CharacterStats] CreateHealthBarOnClient调用 - healthBarPrefab: " + healthBarPrefabName + ", healthBarCreated: " + healthBarCreated);
            
            if (healthBarPrefab != null && healthBarInstance == null)
            {
                Debug.Log("[CharacterStats] CreateHealthBarOnClient - 实例化血条预设体");
                healthBarInstance = Instantiate(healthBarPrefab, transform);
                healthBarInstance.transform.localPosition = Vector3.up * 2f;

                HealthBarUI healthBarUI = healthBarInstance.GetComponent<HealthBarUI>();
                if (healthBarUI != null)
                {
                    Debug.Log("[CharacterStats] CreateHealthBarOnClient - 找到HealthBarUI组件，调用SetTarget");
                    healthBarUI.SetTarget(this);
                }
                
                healthBarCreated = true;
                Debug.Log("[CharacterStats] 客户端血条已创建 - 当前血量: " + currentHealth + "/" + maxHealth);
            }
            else
            {
                if (healthBarPrefab == null)
                {
                    Debug.LogError("[CharacterStats] CreateHealthBarOnClient - healthBarPrefab为空，无法创建血条");
                }
                if (healthBarInstance != null)
                {
                    Debug.LogWarning("[CharacterStats] CreateHealthBarOnClient - 血条已经创建过了");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CharacterStats] CreateHealthBarOnClient方法异常: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    void OnHealthChanged(int oldValue, int newValue)
    {
        try
        {
            currentHealth = newValue;
            Debug.Log("[CharacterStats] 血量变化 - 旧值: " + oldValue + ", 新值: " + newValue);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CharacterStats] OnHealthChanged方法异常: " + ex.Message);
        }
    }

    /// <summary>
    /// 应用伤害 - 服务器权威，只在服务器上执行
    /// </summary>
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
            
            Debug.Log("[CharacterStats] 应用伤害 - 当前血量: " + currentHealth + ", 伤害: " + amount + ", 新血量: " + newHealth);
            
            // 更新当前血量（SyncVar会自动同步到所有客户端）
            currentHealth = newHealth;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CharacterStats] ApplyDamage方法异常: " + ex.Message);
        }
    }

    /// <summary>
    /// 客户端请求应用伤害的命令
    /// </summary>
    [Command]
    public void CmdApplyDamage(int amount)
    {
        ApplyDamage(amount);
    }

    /// <summary>
    /// 恢复血量 - 服务器权威，只在服务器上执行
    /// </summary>
    [Server]
    public void Heal(int amount)
    {
        try
        {
            // 确保治疗值为正数
            amount = Mathf.Abs(amount);
            
            // 计算新的血量
            int newHealth = currentHealth + amount;
            
            // 确保血量不会超过最大值
            newHealth = Mathf.Min(newHealth, maxHealth);
            
            // 只有当血量有变化时才更新
            if (newHealth != currentHealth)
            {
                Debug.Log("[CharacterStats] 恢复血量 - 当前血量: " + currentHealth + ", 治疗: " + amount + ", 新血量: " + newHealth);
                
                // 更新当前血量（SyncVar会自动同步到所有客户端）
                currentHealth = newHealth;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[CharacterStats] Heal方法异常: " + ex.Message);
        }
    }
}