using Mirror;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NetworkTransformReliable))]
public class EnemyAIController : NetworkBehaviour
{
    public string enemyType;
    public Transform target;
    public float detectionRadius = 10f;
    public float attackDistance = 3f;
    public float attackCooldown = 3f;
    public float patrolWaitTime = 2f;

    EnemyData data;
    UnityEngine.AI.NavMeshAgent agent;
    Animator animator;
    EnemyHealthManager healthManager;
    SphereCollider detectionCollider;
    NetworkTransformReliable networkTransform;

    float lastAttackTime;
    bool isDead = false;
    bool isAttacking = false;
    float postAttackWaitTimer = 0f;

    // 同步变量 - 用于在客户端上同步敌人的状态
    [SyncVar(hook = nameof(OnStateChanged))]
    private State syncState = State.Patrol;

#pragma warning disable CS0414
    [SyncVar(hook = nameof(OnAnimationStateChanged))]
    private AnimationState syncAnimationState = AnimationState.Idle;
#pragma warning restore CS0414

    enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    // 动画状态枚举 - 用于同步动画
    enum AnimationState
    {
        Idle,
        Patrol,
        Chase,
        Attack
    }

    State state;
    Vector3[] patrolPoints;
    int currentPatrolIndex;
    float patrolWaitTimer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // 验证并修复配置
        ValidateAndFixConfiguration();
        
        // 初始化NetworkTransform组件
        InitializeNetworkTransform();
        
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        healthManager = GetComponent<EnemyHealthManager>();

        SetupDetectionCollider();

        // 如果enemyType为空，尝试从游戏对象名称中获取
        if (string.IsNullOrEmpty(enemyType))
        {
            string objName = gameObject.name;
            // 处理克隆对象名称，如 "BigEnemy(Clone)" -> "BigEnemy"
            if (objName.Contains("(Clone)"))
            {
                enemyType = objName.Split('(')[0].Trim();
                Debug.Log($"[EnemyAIController] 从游戏对象名称获取敌人类型: {enemyType}");
            }
            else
            {
                enemyType = objName;
                Debug.Log($"[EnemyAIController] 从游戏对象名称获取敌人类型: {enemyType}");
            }
        }

        if (!string.IsNullOrEmpty(enemyType) && EnemyDatabase.GetInstance() != null)
        {
            data = EnemyDatabase.GetInstance().GetEnemy(enemyType);
        }
        SetupFromData();
        GeneratePatrolPoints();
        state = State.Patrol;
        syncState = State.Patrol;
        
        // 将敌人数据传递给血量管理器
        if (healthManager != null && data != null)
        {
            healthManager.InitializeFromEnemyData(data);
            Debug.Log($"[EnemyAIController] 已将敌人数据传递给血量管理器 - 类型: {enemyType}, 血量: {data.health}");
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
        
        // 重新获取组件引用（关键修复：对象池重新激活时组件引用可能失效）
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        healthManager = GetComponent<EnemyHealthManager>();
        
        // 如果已经死亡，说明这是从对象池重新激活的对象
        if (isDead)
        {
            Debug.Log($"[EnemyAIController] 对象从对象池重新激活 - 类型: {enemyType}");
            
            // 重新初始化敌人数据
            if (!string.IsNullOrEmpty(enemyType) && EnemyDatabase.GetInstance() != null)
            {
                data = EnemyDatabase.GetInstance().GetEnemy(enemyType);
            }
            SetupFromData();
            GeneratePatrolPoints();
            state = State.Patrol;
            
            // 将敌人数据传递给血量管理器
            if (healthManager != null && data != null)
            {
                healthManager.InitializeFromEnemyData(data);
                Debug.Log($"[EnemyAIController] 已将敌人数据传递给血量管理器 - 类型: {enemyType}, 血量: {data.health}");
            }
        }
    }

    /// <summary>
    /// 验证并修复敌人AI配置
    /// 确保攻击冷却时间和攻击距离符合预期值
    /// </summary>
    void ValidateAndFixConfiguration()
    {
        bool needsFix = false;
        string fixInfo = "";

        // 验证并修复攻击冷却时间
        if (attackCooldown != 3f)
        {
            float oldValue = attackCooldown;
            attackCooldown = 3f;
            needsFix = true;
            fixInfo += $"攻击冷却时间: {oldValue}秒 -> 3秒\n";
        }

        // 验证并修复攻击距离
        if (attackDistance != 3f)
        {
            float oldValue = attackDistance;
            attackDistance = 3f;
            needsFix = true;
            fixInfo += $"攻击距离: {oldValue}米 -> 3米\n";
        }

        if (needsFix)
        {
            Debug.LogWarning($"[EnemyAIController] 已自动修复配置 - {gameObject.name}:\n{fixInfo}");
        }
    }

    /// <summary>
    /// 初始化NetworkTransform组件，确保位置同步正常工作
    /// </summary>
    void InitializeNetworkTransform()
    {
        networkTransform = GetComponent<NetworkTransformReliable>();
        if (networkTransform == null)
        {
            networkTransform = gameObject.AddComponent<NetworkTransformReliable>();
            Debug.Log($"[EnemyAIController] 已添加NetworkTransformReliable组件");
        }
        
        networkTransform.syncDirection = Mirror.SyncDirection.ServerToClient;
        networkTransform.positionPrecision = 0.01f;
        networkTransform.rotationSensitivity = 0.01f;
        networkTransform.interpolatePosition = true;
        networkTransform.interpolateRotation = true;
        
        Debug.Log($"[EnemyAIController] NetworkTransform初始化完成 - 类型: {enemyType}");
    }

    /// <summary>
    /// 当状态在服务器上改变时，在所有客户端上调用此方法
    /// </summary>
    void OnStateChanged(State oldState, State newState)
    {
        // 只在客户端上执行
        if (isServer)
            return;
        
        Debug.Log($"[EnemyAIController] 状态已同步: {oldState} -> {newState}");
    }

    /// <summary>
    /// 当动画状态在服务器上改变时，在所有客户端上调用此方法
    /// </summary>
    void OnAnimationStateChanged(AnimationState oldState, AnimationState newState)
    {
        // 只在客户端上执行
        if (isServer)
            return;
        
        UpdateClientAnimation(newState);
    }

    /// <summary>
    /// 在客户端上更新动画状态
    /// </summary>
    void UpdateClientAnimation(AnimationState animState)
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                return;
        }

        switch (animState)
        {
            case AnimationState.Idle:
                animator.SetBool("IsWork", false);
                animator.SetBool("IsRun", false);
                break;
            case AnimationState.Patrol:
                animator.SetBool("IsWork", true);
                animator.SetBool("IsRun", false);
                break;
            case AnimationState.Chase:
                animator.SetBool("IsWork", false);
                animator.SetBool("IsRun", true);
                break;
            case AnimationState.Attack:
                animator.SetBool("IsWork", false);
                animator.SetBool("IsRun", false);
                animator.ResetTrigger("IsAtk");
                animator.SetTrigger("IsAtk");
                break;
        }
    }

    void SetupDetectionCollider()
    {
        detectionCollider = gameObject.AddComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;
    }

    void SetupFromData()
    {
        if (data == null)
        {
            Debug.LogWarning($"[EnemyAIController] SetupFromData: 敌人数据为空 - enemyType: {enemyType}");
            return;
        }
        
        agent.speed = data.patrolSpeed;
        Debug.Log($"[EnemyAIController] SetupFromData完成 - 类型: {enemyType}, 血量: {data.health}, 巡逻速度: {data.patrolSpeed}, 攻击冷却时间: {attackCooldown}");
    }

    void GeneratePatrolPoints()
    {
        patrolPoints = new Vector3[5];
        Vector3 origin = transform.position;
        
        // 将360度均匀分成5个扇区，每个扇区72度
        float sectorAngle = (Mathf.PI * 2f) / patrolPoints.Length;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 candidatePoint = origin;
            int attempts = 0;
            const int maxAttempts = 20;
            bool validPointFound = false;
            
            while (!validPointFound && attempts < maxAttempts)
            {
                // 在每个扇区内随机选择角度，确保点分布均匀
                float baseAngle = sectorAngle * i;
                float randomAngleOffset = Random.Range(-sectorAngle * 0.4f, sectorAngle * 0.4f);
                float angle = baseAngle + randomAngleOffset;
                
                // 距离在5-10单位之间随机
                float distance = Random.Range(5f, 10f);
                
                Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                candidatePoint = origin + offset;
                
                attempts++;
                
                // 检查是否在yingdi区域内
                if (!Core.SpawnPositionHelper.IsInYingdiArea(candidatePoint, 30f))
                {
                    validPointFound = true;
                }
            }
            
            // 如果多次尝试后仍然在yingdi区域内，使用最后一次尝试的位置
            patrolPoints[i] = candidatePoint;
        }
        
        currentPatrolIndex = 0;
        
        Debug.Log($"[EnemyAIController] 生成{patrolPoints.Length}个巡逻点，分布在{origin}周围");
    }

    [ServerCallback]
    void Update()
    {
        // 死亡状态下不执行任何逻辑
        if (isDead)
            return;

        // 检查血量，如果血量小于等于0，立即处理死亡
        if (healthManager != null && healthManager.currentHealth <= 0)
        {
            Die();
            return;
        }

        // 如果正在攻击后等待，减少等待计时器
        if (postAttackWaitTimer > 0f)
        {
            postAttackWaitTimer -= Time.deltaTime;
            return;
        }

        // 根据当前状态执行相应逻辑
        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
        }
    }

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (patrolWaitTimer > 0f)
        {
            patrolWaitTimer -= Time.deltaTime;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            SetAnimationState(false, false, false);
            return;
        }

        agent.speed = data != null ? data.patrolSpeed : agent.speed;
        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[currentPatrolIndex]);
        SetAnimationState(true, false, false);

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            // 随机选择下一个巡逻点，避免重复访问同一个点
            int nextIndex;
            do
            {
                nextIndex = Random.Range(0, patrolPoints.Length);
            } while (nextIndex == currentPatrolIndex && patrolPoints.Length > 1);
            
            currentPatrolIndex = nextIndex;
            patrolWaitTimer = patrolWaitTime;
        }
    }

    void UpdateChase()
    {
        if (target == null)
        {
            state = State.Patrol;
            SetAnimationState(false, false, false);
            return;
        }

        LookAtTarget();
        
        float distance = Vector3.Distance(transform.position, target.position);
        
        agent.speed = data != null ? data.chaseSpeed : agent.speed;
        agent.stoppingDistance = 2.5f;
        agent.SetDestination(target.position);
        
        if (distance <= 2.7f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            SetAnimationState(false, false, false);
            
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= attackCooldown)
            {
                TryAttackTarget();
            }
        }
        else
        {
            agent.isStopped = false;
            SetAnimationState(false, true, false);
        }
    }


    void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active || isDead)
            return;

        if (other.CompareTag("Player_new") && target == null)
        {
            target = other.transform;
            if (state == State.Patrol || state == State.Idle)
            {
                state = State.Chase;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!NetworkServer.active || isDead)
            return;

        if (other.CompareTag("Player_new") && other.transform == target)
        {
            target = null;
            state = State.Patrol;
        }
    }

    void TryAttackTarget()
    {
        if (data == null || target == null)
        {
            return;
        }

        float timeSinceLastAttack = Time.time - lastAttackTime;
        if (timeSinceLastAttack < attackCooldown)
        {
            return;
        }

        if (isAttacking)
        {
            return;
        }

        lastAttackTime = Time.time;
        isAttacking = true;

        syncAnimationState = AnimationState.Attack;
        StartCoroutine(MonitorAttackAnimation());
        RpcPlayAttackAnimation();
    }

    [ClientRpc]
    void RpcPlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.ResetTrigger("IsAtk");
            animator.SetTrigger("IsAtk");
        }
    }

    System.Collections.IEnumerator MonitorAttackAnimation()
    {
        float attackAnimationDuration = 1.0f;
        float elapsedTime = 0f;

        while (elapsedTime < attackAnimationDuration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        isAttacking = false;
        
        syncAnimationState = AnimationState.Idle;
        
        postAttackWaitTimer = 0.5f;
    }

    [Server]
    public void OnAttackHit()
    {
        if (target == null)
            return;

        LookAtTarget();

        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        Collider[] hitsBuffer = new Collider[10];
        int hitCount = Physics.OverlapSphereNonAlloc(origin, 3f, hitsBuffer, LayerMask.GetMask("Player"));
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitsBuffer[i];
            
            if (!hit.CompareTag("Player_new"))
                continue;

            Vector3 dirToTarget = (hit.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);
            if (angle > 180f)
                continue;

            CharacterStats targetStats = hit.GetComponentInChildren<CharacterStats>();
            if (targetStats != null && data != null)
            {
                targetStats.ApplyDamage(data.attackDamage);
            }
        }
    }

    void LookAtTarget()
    {
        if (target == null)
            return;

        Vector3 direction = (target.position - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void SetAnimationState(bool isWork, bool isRun, bool isAtk)
    {
        if (animator == null)
            return;

        animator.SetBool("IsWork", isWork);
        animator.SetBool("IsRun", isRun);
    }

    /// <summary>
    /// 处理敌人死亡
    /// </summary>
    public void Die()
    {
        isDead = true;
        state = State.Dead;
        
        // 先停止寻路功能
        agent.isStopped = true;
        agent.ResetPath();
        
        // 清除所有动画状态
        if (animator != null)
        {
            animator.SetBool("IsWork", false);
            animator.SetBool("IsRun", false);
            animator.ResetTrigger("IsAtk");
        }
        
        // 然后触发死亡动画
        if (animator != null)
        {
            animator.SetTrigger("IsDie");
        }
        RpcPlayDeathAnimation();

        StartCoroutine(ReturnToPoolAfterDelay());
    }

    [ClientRpc]
    void RpcPlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsDie");
        }
    }

    IEnumerator ReturnToPoolAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);

        try
        {
            // 检查对象是否仍然存在
            if (this == null || gameObject == null)
            {
                Debug.LogWarning("[EnemyAIController] 对象已被销毁，无需返回对象池");
                yield break;
            }

            // 检查网络服务器是否仍然活跃
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[EnemyAIController] 网络服务器已停止，无法返回对象池");
                yield break;
            }

            // 检查对象池管理器是否存在
            if (AutoObjectPoolManager.Instance == null)
            {
                Debug.LogWarning("[EnemyAIController] 无法返回对象池，AutoObjectPoolManager.Instance为null");
                NetworkServer.Destroy(gameObject);
                yield break;
            }

            // 检查敌人类型是否有效
            if (string.IsNullOrEmpty(enemyType))
            {
                Debug.LogWarning("[EnemyAIController] 无法返回对象池，enemyType为空");
                NetworkServer.Destroy(gameObject);
                yield break;
            }

            // 检查网络对象是否仍然有效
            NetworkIdentity netIdentity = GetComponent<NetworkIdentity>();
            if (netIdentity == null)
            {
                Debug.LogWarning("[EnemyAIController] 网络对象不存在，无法返回对象池");
                Destroy(gameObject);
                yield break;
            }

            // 检查网络对象是否仍然被spawned（netId为0表示未被spawned）
            if (netIdentity.netId == 0)
            {
                Debug.LogWarning("[EnemyAIController] 网络对象已被unspawned，无需返回对象池");
                Destroy(gameObject);
                yield break;
            }

            // 重置对象池状态
            if (healthManager != null)
            {
                healthManager.ResetPoolState();
            }
            
            // 将对象返回到正确的对象池
            string poolId = GetPoolIdForEnemy(enemyType);
            Debug.Log($"[EnemyAIController] 尝试返回对象池 - 类型: {enemyType}, 池ID: {poolId}");
            AutoObjectPoolManager.Instance.ReturnObject(poolId, gameObject);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnemyAIController] 返回对象池时发生异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
            
            // 如果发生异常，尝试销毁对象
            if (gameObject != null && NetworkServer.active)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
    
    /// <summary>
    /// 重置对象池状态（当对象从对象池重新获取时调用）
    /// </summary>
    public void ResetPoolState()
    {
        try
        {
            isDead = false;
            state = State.Patrol;
            target = null;
            lastAttackTime = 0f;
            isAttacking = false;
            postAttackWaitTimer = 0f;
            
            if (agent != null)
            {
                agent.isStopped = false;
                agent.ResetPath();
            }
            
            if (animator != null)
            {
                animator.SetBool("IsWork", false);
                animator.SetBool("IsRun", false);
                animator.ResetTrigger("IsAtk");
                animator.ResetTrigger("IsDie");
            }
            
            GeneratePatrolPoints();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnemyAIController] ResetPoolState方法异常: {ex.Message}");
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
