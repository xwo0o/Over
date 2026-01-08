using UnityEngine;
using Mirror;

/// <summary>
/// 调试工具：用于检查角色攻击力设置
/// </summary>
public class AttackDebugTool : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebug = true;
    public KeyCode debugKey = KeyCode.F9;
    
    void Update()
    {
        if (enableDebug && Input.GetKeyDown(debugKey))
        {
            DebugAttackValues();
        }
    }
    
    /// <summary>
    /// 调试所有玩家的攻击力值
    /// </summary>
    void DebugAttackValues()
    {
        Debug.Log("=== 攻击力调试信息 ===");
        
        // 查找所有NetworkPlayer
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            Debug.Log($"玩家 {player.netId} (本地: {player.isLocalPlayer}):");
            
            // 获取CharacterStats组件
            CharacterStats stats = player.GetComponentInChildren<CharacterStats>();
            if (stats != null)
            {
                Debug.Log($"  角色ID: {stats.characterId}");
                Debug.Log($"  攻击力: {stats.attack}");
                Debug.Log($"  当前血量: {stats.currentHealth}/{stats.maxHealth}");
                Debug.Log($"  移动速度: {stats.moveSpeed}");
            }
            else
            {
                Debug.LogWarning("  未找到CharacterStats组件");
            }
        }
        
        // 查找所有敌人
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"找到 {enemies.Length} 个敌人:");
        
        foreach (GameObject enemy in enemies)
        {
            EnemyHealthManager enemyHealth = enemy.GetComponent<EnemyHealthManager>();
            if (enemyHealth != null)
            {
                Debug.Log($"  敌人 {enemy.name} - 类型: {enemyHealth.enemyType}, 血量: {enemyHealth.currentHealth}/{enemyHealth.maxHealth}");
            }
        }
    }
    
    /// <summary>
    /// 在Scene视图中显示调试信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (!enableDebug) return;
        
        // 查找所有NetworkPlayer
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in players)
        {
            CharacterStats stats = player.GetComponentInChildren<CharacterStats>();
            if (stats != null)
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(player.transform.position + Vector3.up * 2f, 
                    $"攻击力: {stats.attack}\n血量: {stats.currentHealth}/{stats.maxHealth}");
                #endif
            }
        }
    }
}