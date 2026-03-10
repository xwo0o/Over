using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// 武器伤害系统 - 处理武器伤害计算、连击伤害倍率、攻击判定等
/// </summary>
public class WeaponDamageSystem : NetworkBehaviour
{
    [Header("基础设置")]
    [Tooltip("当前装备的武器ID")]
    // 关键修复：改为private，防止客户端直接修改
    [SyncVar(hook = nameof(OnWeaponIdChanged))]
    private int currentWeaponId = -1;

    [Header("攻击判定")]
    [Tooltip("攻击检测的层级")]
    public LayerMask attackLayerMask;
    [Tooltip("攻击检测的原点偏移（相对于武器）")]
    public Vector3 attackOriginOffset = Vector3.zero;

    [Header("连击系统")]
    [Tooltip("当前连击阶段")]
    // 关键修复：改为private，防止客户端直接修改
    [SyncVar]
    private int currentComboStage = 0;
    [Tooltip("是否在连击窗口期内")]
    // 关键修复：改为private，防止客户端直接修改
    [SyncVar]
    private bool isInComboWindow = false;
    [Tooltip("连击重置时间（秒）")]
    public float comboResetTime = 1.5f;
    [Tooltip("连击窗口持续时间（动画结束后）")]
    public float comboWindowDuration = 0.3f;

    // 当前武器数据
    private WeaponData currentWeaponData;
    // 已命中目标列表（避免单次攻击多段伤害）
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    // 连击重置计时器
    private float comboResetTimer = 0f;
    // 连击窗口计时器
    private float comboWindowTimer = 0f;
    // 是否正在攻击
    private bool isAttacking = false;
    // 攻击是否已造成伤害（防止重复伤害）
    private bool hasDealtDamage = false;

    // 事件
    public System.Action<int, float> OnDamageDealt; // 参数：目标ID，伤害值
    public System.Action<int> OnComboStageChanged;  // 参数：当前连击阶段
    public System.Action OnComboWindowOpen;         // 连击窗口开启
    public System.Action OnComboWindowClose;        // 连击窗口关闭
    public System.Action OnComboReset;              // 连击重置

    void Update()
    {
        if (!isLocalPlayer) return;

        // 更新连击计时器
        UpdateComboTimers();
    }

    /// <summary>
    /// 更新连击相关计时器
    /// </summary>
    void UpdateComboTimers()
    {
        // 连击窗口计时
        if (isInComboWindow)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
            {
                CloseComboWindow();
            }
        }

        // 连击重置计时
        if (currentComboStage > 0 && !isAttacking)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f)
            {
                ResetCombo();
            }
        }
    }

    /// <summary>
    /// 武器ID变化回调 - 关键修复：处理客户端WeaponDatabase可能未加载的情况
    /// </summary>
    void OnWeaponIdChanged(int oldId, int newId)
    {
        Debug.Log($"[WeaponDamageSystem] OnWeaponIdChanged: {oldId} -> {newId}, WeaponDatabase.Instance={(WeaponDatabase.Instance != null ? "有" : "null")}");
        
        if (newId > 0)
        {
            // 优先从WeaponDatabase获取（如果可用）
            if (WeaponDatabase.Instance != null && WeaponDatabase.Instance.IsInitialized)
            {
                currentWeaponData = WeaponDatabase.Instance.GetWeapon(newId);
                Debug.Log($"[WeaponDamageSystem] 从WeaponDatabase装备武器: {currentWeaponData?.weaponName ?? "未知"} (ID: {newId})");
            }
            else
            {
                // 关键修复：如果WeaponDatabase不可用，创建一个临时的武器数据
                // 这样客户端即使没有数据库也能攻击
                currentWeaponData = CreateDefaultWeaponData(newId);
                Debug.Log($"[WeaponDamageSystem] WeaponDatabase不可用，使用默认武器数据 (ID: {newId})");
            }
        }
        else
        {
            currentWeaponData = null;
            Debug.Log($"[WeaponDamageSystem] 卸下武器 (ID: {newId})");
        }

        // 重置连击
        ResetCombo();
    }

    /// <summary>
    /// 设置武器数据 - 由 WeaponStateManager 调用
    /// 关键修复：允许外部系统设置武器数据
    /// </summary>
    public void SetWeaponData(WeaponData weaponData)
    {
        currentWeaponData = weaponData;
        if (weaponData != null)
        {
            currentWeaponId = weaponData.weaponId;
            Debug.Log($"[WeaponDamageSystem] 设置武器数据: {weaponData.weaponName} (ID: {weaponData.weaponId})");
        }
        else
        {
            currentWeaponId = -1;
            Debug.Log("[WeaponDamageSystem] 清除武器数据");
        }
    }

    /// <summary>
    /// 创建默认武器数据（当WeaponDatabase不可用时使用）
    /// </summary>
    WeaponData CreateDefaultWeaponData(int weaponId)
    {
        // 根据武器ID创建基本的武器数据
        WeaponData defaultData = new WeaponData();
        defaultData.weaponId = weaponId;
        defaultData.damage = 50; // 默认伤害
        defaultData.comboStages = 3; // 默认3段连击
        defaultData.attackRange = 2.0f; // 默认攻击范围
        defaultData.attackAngle = 90f; // 默认攻击角度
        defaultData.comboDamageMultipliers = new float[] { 1.0f, 1.0f, 1.5f }; // 默认伤害倍率
        
        // 根据ID设置武器名称和特性
        switch (weaponId)
        {
            case 1: // 阔刀
                defaultData.weaponName = "阔刀";
                defaultData.damage = 80;
                defaultData.attackRange = 2.7f;
                defaultData.attackAngle = 120f;
                break;
            case 2: // 双刀
                defaultData.weaponName = "双刀";
                defaultData.damage = 50;
                defaultData.attackRange = 2.0f;
                defaultData.attackAngle = 90f;
                break;
            case 3: // 斧头
                defaultData.weaponName = "斧头";
                defaultData.damage = 130;
                defaultData.comboStages = 1;
                defaultData.attackRange = 2.5f;
                defaultData.attackAngle = 360f;
                break;
            default:
                defaultData.weaponName = $"武器{weaponId}";
                break;
        }
        
        return defaultData;
    }

    /// <summary>
    /// 装备武器
    /// </summary>
    [Command]
    public void CmdEquipWeapon(int weaponId)
    {
        currentWeaponId = weaponId;
        Debug.Log($"[WeaponDamageSystem] 服务器: 玩家装备武器 ID={weaponId}");
    }

    /// <summary>
    /// 开始攻击（每一段攻击动画开始时调用）
    /// </summary>
    public void StartAttack()
    {
        if (currentWeaponData == null)
        {
            Debug.LogWarning("[WeaponDamageSystem] 没有装备武器，无法攻击");
            return;
        }

        isAttacking = true;
        // 每一段攻击都需要独立的伤害判定，所以重置hasDealtDamage
        hasDealtDamage = false;
        // 清空已命中列表，允许每一段攻击都能命中相同目标
        hitTargets.Clear();

        // 增加连击阶段
        currentComboStage++;
        if (currentComboStage > currentWeaponData.comboStages)
        {
            currentComboStage = 1; // 重置为第一阶段
        }

        OnComboStageChanged?.Invoke(currentComboStage);
        Debug.Log($"[WeaponDamageSystem] 开始攻击 - 连击阶段: {currentComboStage}/{currentWeaponData.comboStages}");

        // 重置连击重置计时器
        comboResetTimer = comboResetTime;
        
        // 开启连击窗口（动画播放期间就可以连击）
        OpenComboWindow();
    }

    /// <summary>
    /// 结束攻击（每一段攻击动画结束时调用）
    /// </summary>
    public void EndAttack()
    {
        isAttacking = false;
        hasDealtDamage = false;

        // 如果不是最后一段攻击，延迟关闭连击窗口（动画结束后0.3秒）
        if (currentWeaponData != null && currentComboStage < currentWeaponData.comboStages)
        {
            // 延迟关闭连击窗口
            Invoke(nameof(CloseComboWindow), comboWindowDuration);
            Debug.Log($"[WeaponDamageSystem] 动画结束，连击窗口将在{comboWindowDuration}秒后关闭");
        }
        else
        {
            // 最后一段攻击结束，立即关闭连击窗口并重置
            CloseComboWindow();
            ResetCombo();
            Debug.Log("[WeaponDamageSystem] 最后一段攻击结束，连击完成并重置");
        }
    }

    /// <summary>
    /// 开启连击窗口（由动画事件AE_OpenWindow调用）
    /// </summary>
    public void OpenComboWindow()
    {
        Debug.Log($"[WeaponDamageSystem] OpenComboWindow被调用 - currentWeaponData={(currentWeaponData != null)}, currentComboStage={currentComboStage}");
        
        if (currentWeaponData == null) 
        {
            Debug.Log("[WeaponDamageSystem] OpenComboWindow返回：currentWeaponData为null");
            return;
        }

        // 如果已经达到最大连击阶段，不开启窗口
        if (currentComboStage >= currentWeaponData.comboStages) 
        {
            Debug.Log($"[WeaponDamageSystem] OpenComboWindow返回：已达到最大连击阶段 {currentComboStage}/{currentWeaponData.comboStages}");
            return;
        }

        isInComboWindow = true;

        OnComboWindowOpen?.Invoke();
        Debug.Log($"[WeaponDamageSystem] 连击窗口开启 - 当前阶段: {currentComboStage}/{currentWeaponData.comboStages}");
    }

    /// <summary>
    /// 关闭连击窗口（由动画事件AE_CloseWindow调用）
    /// </summary>
    public void CloseComboWindow()
    {
        isInComboWindow = false;
        OnComboWindowClose?.Invoke();
        Debug.Log("[WeaponDamageSystem] 连击窗口关闭");
    }

    /// <summary>
    /// 结束攻击时尝试开启连击窗口（备用机制，如果动画事件未触发）
    /// </summary>
    void TryOpenComboWindow()
    {
        if (currentWeaponData != null && currentComboStage < currentWeaponData.comboStages)
        {
            // 使用JSON配置的窗口时间作为备用
            float windowDuration = currentWeaponData.comboWindowEnd - currentWeaponData.comboWindowStart;
            comboWindowTimer = windowDuration;

            // 延迟开启窗口（等待动画事件，如果动画事件未触发则使用代码计时）
            Invoke(nameof(OpenComboWindow), currentWeaponData.comboWindowStart);
            Invoke(nameof(CloseComboWindow), currentWeaponData.comboWindowEnd);
        }
    }

    /// <summary>
    /// 重置连击
    /// </summary>
    public void ResetCombo()
    {
        currentComboStage = 0;
        isInComboWindow = false;
        comboResetTimer = 0f;
        comboWindowTimer = 0f;
        hitTargets.Clear();
        isAttacking = false;
        hasDealtDamage = false;

        OnComboReset?.Invoke();
        Debug.Log("[WeaponDamageSystem] 连击已重置");
    }

    /// <summary>
    /// 同步连击阶段 - 由 WeaponStateManager 调用
    /// </summary>
    public void SyncComboStage(int stage)
    {
        currentComboStage = stage;
        isAttacking = true;
        hasDealtDamage = false;
        hitTargets.Clear();

        OnComboStageChanged?.Invoke(currentComboStage);
        Debug.Log($"[WeaponDamageSystem] 同步连击阶段: {currentComboStage}");
    }

    /// <summary>
    /// 执行伤害判定（在动画事件或攻击触发时调用）
    /// 使用武器配置中的攻击范围和角度进行检测
    /// 在本地客户端执行检测，然后通知服务器应用伤害
    /// </summary>
    /// <param name="attackCenter">攻击判定中心点</param>
    /// <param name="attackRadius">攻击判定半径（备用）</param>
    public void PerformDamageCheck(Vector3 attackCenter, float attackRadius)
    {
        Debug.Log($"[WeaponDamageSystem] PerformDamageCheck被调用 - isServer={isServer}, isLocalPlayer={isLocalPlayer}, hasDealtDamage={hasDealtDamage}, currentWeaponData={(currentWeaponData != null ? "有" : "null")}");

        // 关键修复：改为在服务器执行检测，而不是只在本地玩家执行
        // 这样连接玩家的攻击请求发送到服务器后，服务器可以正确执行伤害检测
        if (!isServer)
        {
            Debug.LogWarning("[WeaponDamageSystem] 无法执行伤害检测：不是服务器");
            return;
        }
        if (hasDealtDamage) 
        {
            Debug.LogWarning("[WeaponDamageSystem] 无法执行伤害检测：已经造成过伤害");
            return;
        }
        if (currentWeaponData == null) 
        {
            Debug.LogError("[WeaponDamageSystem] 无法执行伤害检测：currentWeaponData为null");
            return;
        }

        // 使用武器配置中的攻击范围和角度
        float range = currentWeaponData.attackRange;
        float angle = currentWeaponData.attackAngle;
        
        // 攻击判定中心（角色位置 + 偏移）
        Vector3 origin = transform.position + Vector3.up * 1.0f; // 从角色中心偏上1米
        Vector3 forward = transform.forward;

        Debug.Log($"[WeaponDamageSystem] 攻击检测 - 范围={range}, 角度={angle}, 原点={origin}, 玩家位置={transform.position}");

        // 检测范围内的所有碰撞体（使用Enemy层）
        int enemyLayer = LayerMask.GetMask("Enemy");
        Debug.Log($"[WeaponDamageSystem] Enemy层掩码: {enemyLayer}");
        
        // 先检测所有层，看看有什么
        Collider[] allColliders = Physics.OverlapSphere(origin, range);
        Debug.Log($"[WeaponDamageSystem] 范围内所有碰撞体: {allColliders.Length} 个");
        foreach (var col in allColliders)
        {
            Debug.Log($"[WeaponDamageSystem] - 碰撞体: {col.name}, Layer={LayerMask.LayerToName(col.gameObject.layer)}({col.gameObject.layer}), Tag={col.tag}");
        }
        
        // 只检测Enemy层，使用QueryTriggerInteraction.Ignore忽略触发器
        Collider[] hitColliders = Physics.OverlapSphere(origin, range, enemyLayer, QueryTriggerInteraction.Ignore);
        
        Debug.Log($"[WeaponDamageSystem] Enemy层检测到 {hitColliders.Length} 个碰撞体（已忽略触发器）");

        foreach (Collider hitCollider in hitColliders)
        {
            // 跳过自己
            if (hitCollider.gameObject == gameObject) continue;
            
            // 跳过触发器
            if (hitCollider.isTrigger)
            {
                Debug.Log($"[WeaponDamageSystem] 跳过: {hitCollider.name} 是触发器");
                continue;
            }

            Debug.Log($"[WeaponDamageSystem] 检查碰撞体: {hitCollider.name}, Tag={hitCollider.tag}, isTrigger={hitCollider.isTrigger}");

            // 检查标签是否为Enemy
            if (!hitCollider.CompareTag("Enemy"))
            {
                Debug.Log($"[WeaponDamageSystem] 跳过: {hitCollider.name} 标签不是Enemy");
                continue;
            }

            // 获取目标中心点（使用碰撞体中心或transform位置）
            Vector3 targetCenter = hitCollider.bounds.center;
            
            // 计算到目标的实际距离
            float distanceToTarget = Vector3.Distance(origin, targetCenter);
            Debug.Log($"[WeaponDamageSystem] {hitCollider.name} 距离: {distanceToTarget}, 攻击范围: {range}");
            
            // 检查是否在攻击范围内
            if (distanceToTarget > range)
            {
                Debug.Log($"[WeaponDamageSystem] 跳过: {hitCollider.name} 超出攻击范围");
                continue;
            }

            // 检查是否在攻击角度范围内（360度攻击不需要角度检查）
            if (angle < 360f)
            {
                Vector3 dirToTarget = (targetCenter - origin).normalized;
                float targetAngle = Vector3.Angle(forward, dirToTarget);
                
                Debug.Log($"[WeaponDamageSystem] {hitCollider.name} 角度: {targetAngle}, 最大角度: {angle * 0.5f}");
                
                if (targetAngle > angle * 0.5f)
                {
                    // 目标在攻击角度外
                    Debug.Log($"[WeaponDamageSystem] 跳过: {hitCollider.name} 在攻击角度外");
                    continue;
                }
            }

            // 检查是否已经命中过该目标
            if (hitTargets.Contains(hitCollider.gameObject)) 
            {
                Debug.Log($"[WeaponDamageSystem] 跳过: {hitCollider.name} 已经命中过");
                continue;
            }

            // 计算伤害
            float damage = CalculateDamage();
            
            Debug.Log($"[WeaponDamageSystem] 命中目标: {hitCollider.name}, 伤害: {damage}");

            // 获取目标的NetworkIdentity
            NetworkIdentity targetNetId = hitCollider.GetComponent<NetworkIdentity>();
            if (targetNetId == null)
            {
                targetNetId = hitCollider.GetComponentInParent<NetworkIdentity>();
            }
            if (targetNetId == null)
            {
                targetNetId = hitCollider.GetComponentInChildren<NetworkIdentity>();
            }
            
            if (targetNetId != null)
            {
                // 通知服务器应用伤害
                CmdApplyDamage(targetNetId.netId, damage);
                Debug.Log($"[WeaponDamageSystem] 发送伤害命令到服务器 - 目标NetId: {targetNetId.netId}, 伤害: {damage}");
            }
            else
            {
                Debug.LogWarning($"[WeaponDamageSystem] 目标 {hitCollider.name} 没有NetworkIdentity");
            }

            // 记录已命中目标
            hitTargets.Add(hitCollider.gameObject);
        }

        hasDealtDamage = true;
    }

    /// <summary>
    /// 服务器命令：应用伤害到目标
    /// 关键修复：添加requiresAuthority=false，允许任何客户端发送伤害命令
    /// </summary>
    [Command(requiresAuthority = false)]
    void CmdApplyDamage(uint targetNetId, float damage)
    {
        Debug.Log($"[WeaponDamageSystem] CmdApplyDamage被调用 - 目标NetId: {targetNetId}, 伤害: {damage}");
        
        if (!isServer)
        {
            Debug.LogWarning("[WeaponDamageSystem] CmdApplyDamage: 不是服务器");
            return;
        }
        
        // 在服务器上查找目标
        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
        {
            GameObject target = identity.gameObject;
            ApplyDamageToTarget(target, damage);
        }
        else
        {
            Debug.LogWarning($"[WeaponDamageSystem] 未找到目标NetId: {targetNetId}");
        }
    }

    /// <summary>
    /// 应用伤害到目标（服务器端）
    /// </summary>
    void ApplyDamageToTarget(GameObject target, float damage)
    {
        bool damageApplied = false;
        
        // 1. 优先尝试EnemyHealthManager（敌人血量管理）
        EnemyHealthManager enemyHealth = target.GetComponent<EnemyHealthManager>();
        if (enemyHealth == null)
        {
            enemyHealth = target.GetComponentInParent<EnemyHealthManager>();
        }
        if (enemyHealth == null)
        {
            enemyHealth = target.GetComponentInChildren<EnemyHealthManager>();
        }
        
        if (enemyHealth != null)
        {
            enemyHealth.ApplyDamage((int)damage);
            damageApplied = true;
            Debug.Log($"[WeaponDamageSystem] 通过EnemyHealthManager对 {target.name} 造成 {damage:F1} 点伤害");
        }
        
        // 2. 如果没有EnemyHealthManager，尝试IDamageable接口
        if (!damageApplied)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = target.GetComponentInParent<IDamageable>();
            }
            if (damageable == null)
            {
                damageable = target.GetComponentInChildren<IDamageable>();
            }
            
            if (damageable != null)
            {
                damageable.TakeDamage(damage, netId);
                damageApplied = true;
                Debug.Log($"[WeaponDamageSystem] 通过IDamageable对 {target.name} 造成 {damage:F1} 点伤害");
            }
        }
        
        // 3. 尝试获取NetworkIdentity用于事件回调
        if (damageApplied)
        {
            NetworkIdentity targetNetIdComp = target.GetComponent<NetworkIdentity>();
            if (targetNetIdComp != null)
            {
                OnDamageDealt?.Invoke((int)targetNetIdComp.netId, damage);
            }
        }
        else
        {
            Debug.LogWarning($"[WeaponDamageSystem] 目标 {target.name} 没有血量管理组件");
        }
    }

    /// <summary>
    /// 计算当前攻击的伤害值
    /// </summary>
    float CalculateDamage()
    {
        if (currentWeaponData == null) return 0f;

        float baseDamage = currentWeaponData.damage;
        float comboMultiplier = GetComboMultiplier();

        float finalDamage = baseDamage * comboMultiplier;

        Debug.Log($"[WeaponDamageSystem] 伤害计算: 基础伤害={baseDamage}, 连击倍率={comboMultiplier:F2}, 最终伤害={finalDamage:F1}");

        return finalDamage;
    }

    /// <summary>
    /// 获取连击伤害倍率（从配置文件读取）
    /// </summary>
    float GetComboMultiplier()
    {
        // 使用武器数据中的配置
        if (currentWeaponData != null)
        {
            return currentWeaponData.GetComboDamageMultiplier(currentComboStage);
        }
        return 1.0f; // 默认倍率
    }

    /// <summary>
    /// 获取当前武器数据
    /// </summary>
    public WeaponData GetCurrentWeaponData()
    {
        return currentWeaponData;
    }

    /// <summary>
    /// 获取当前连击阶段
    /// </summary>
    public int GetCurrentComboStage()
    {
        return currentComboStage;
    }

    /// <summary>
    /// 检查是否在连击窗口期内
    /// </summary>
    public bool IsInComboWindow()
    {
        return isInComboWindow;
    }

    /// <summary>
    /// 检查是否正在攻击
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// 可视化攻击范围（仅在编辑器中）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 绘制攻击范围（示例）
        Gizmos.color = Color.red;
        Vector3 attackCenter = transform.position + transform.TransformDirection(attackOriginOffset);
        Gizmos.DrawWireSphere(attackCenter, 1.5f);
    }
}
