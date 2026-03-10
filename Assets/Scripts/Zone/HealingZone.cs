using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// 治疗区域 - 服务器权威设计
/// 当角色在区域内且血量不满时，每秒恢复5点生命值
/// 同时作为建筑系统的营地区域触发器
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class HealingZone : NetworkBehaviour
{
    [Header("治疗设置")]
    [Tooltip("每秒恢复的生命值")]
    [SerializeField]
    private int healAmountPerSecond = 5;

    [Tooltip("治疗间隔时间（秒）")]
    [SerializeField]
    private float healInterval = 1f;

    [Header("调试信息")]
    [SerializeField]
    private int currentPlayersInZone = 0;

    [Header("建筑系统集成")]
    [SerializeField]
    private bool enableBuildingMode = true;

    // 存储在区域内的玩家
    private HashSet<NetworkPlayer> playersInZone = new HashSet<NetworkPlayer>();

    // 治疗计时器
    private float healTimer;

    // 建筑系统事件
    public System.Action<NetworkPlayer> OnPlayerEnterCampArea;
    public System.Action<NetworkPlayer> OnPlayerExitCampArea;

    void Awake()
    {
        // 确保BoxCollider是触发器
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
        else
        {
        }
    }

    void Start()
    {
        healTimer = 0f;
    }

    void Update()
    {
        // 只在服务器上执行治疗逻辑
        if (!isServer)
            return;

        // 更新治疗计时器
        healTimer += Time.deltaTime;

        // 检查是否到达治疗间隔
        if (healTimer >= healInterval)
        {
            HealPlayersInZone();
            healTimer = 0f;
        }
    }

    /// <summary>
    /// 治疗区域内的所有玩家 - 只在服务器上执行
    /// </summary>
    [Server]
    private void HealPlayersInZone()
    {
        if (playersInZone.Count == 0)
            return;

        // 遍历区域内的所有玩家
        foreach (NetworkPlayer player in playersInZone)
        {
            if (player == null)
                continue;

            // 获取玩家的CharacterStats组件
            CharacterStats stats = player.GetComponentInChildren<CharacterStats>();
            if (stats == null)
            {
                continue;
            }

            // 只有当玩家血量不满时才治疗
            if (stats.currentHealth < stats.maxHealth)
            {
                stats.Heal(healAmountPerSecond);
            }
        }
    }

    /// <summary>
    /// 当玩家进入触发器时调用
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // 检查是否是玩家
        NetworkPlayer player = other.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            // 尝试从父对象获取
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player != null)
        {
            // 服务器端处理：添加到治疗集合
            if (isServer)
            {
                if (playersInZone.Add(player))
                {
                    currentPlayersInZone = playersInZone.Count;
                }
            }
            
            // 客户端处理：触发建筑系统事件
            if (player.isLocalPlayer && enableBuildingMode)
            {
                OnPlayerEnterCampArea?.Invoke(player);
            }
        }
    }

    /// <summary>
    /// 当玩家离开触发器时调用
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // 检查是否是玩家
        NetworkPlayer player = other.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            // 尝试从父对象获取
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        if (player != null)
        {
            // 服务器端处理：从治疗集合移除
            if (isServer)
            {
                if (playersInZone.Remove(player))
                {
                    currentPlayersInZone = playersInZone.Count;
                }
            }
            
            // 客户端处理：触发建筑系统事件
            if (player.isLocalPlayer && enableBuildingMode)
            {
                OnPlayerExitCampArea?.Invoke(player);
            }
        }
    }

    /// <summary>
    /// 当玩家被销毁时清理引用
    /// </summary>
    void OnDestroy()
    {
        // 清空玩家集合
        playersInZone.Clear();
    }

    /// <summary>
    /// 在Scene视图中绘制治疗区域的可视化边界
    /// </summary>
    void OnDrawGizmos()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }


/// <summary>
/// 检查指定玩家是否在当前治疗区域内
/// </summary>
public bool IsPlayerInZone(NetworkPlayer player)
{
    if (player == null)
        return false;
    
    // 服务器端：检查玩家是否在集合中
    if (isServer)
    {
        return playersInZone.Contains(player);
    }
    
    // 客户端：使用距离检测
    float distance = Vector3.Distance(transform.position, player.transform.position);
    BoxCollider boxCollider = GetComponent<BoxCollider>();
    if (boxCollider != null)
    {
        // 计算触发器的大致半径
        float triggerRadius = Mathf.Max(boxCollider.size.x, boxCollider.size.z) * 0.5f;
        return distance <= triggerRadius + 2f; // 增加一些容错距离
    }
    
    return false;
}
}
