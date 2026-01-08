using UnityEngine;
using Mirror;

/// <summary>
/// 动画事件转发器
/// 用于将角色模型上的动画事件转发到PlayerInputHandler组件
/// 攻击周期由PlayerInputHandler统一管理，确保每次攻击只触发一次伤害
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    private PlayerInputHandler playerInputHandler;

    private void Start()
    {
        // 延迟查找PlayerInputHandler组件，确保对象完全初始化
        StartCoroutine(DelayedFindPlayerInputHandler());
    }

    /// <summary>
    /// 延迟查找PlayerInputHandler组件
    /// </summary>
    private System.Collections.IEnumerator DelayedFindPlayerInputHandler()
    {
        // 等待几帧确保对象完全初始化
        yield return null;
        yield return null;
        yield return null;
        
        // 尝试多种方式查找PlayerInputHandler组件
        FindPlayerInputHandler();
        
        // 如果第一次查找失败，再尝试几次
        int attempts = 0;
        int maxAttempts = 5;
        
        while (playerInputHandler == null && attempts < maxAttempts)
        {
            Debug.LogWarning($"[AnimationEventForwarder] 第 {attempts+1}/{maxAttempts} 次尝试查找PlayerInputHandler组件");
            yield return new WaitForSeconds(0.1f);
            FindPlayerInputHandler();
            attempts++;
        }
        
        Debug.Log("[AnimationEventForwarder] 初始化完成，PlayerInputHandler: " + (playerInputHandler != null ? "找到" : "未找到"));
    }

    /// <summary>
    /// 多种方式查找PlayerInputHandler组件
    /// </summary>
    private void FindPlayerInputHandler()
    {
        // 方式1：递归查找父对象
        playerInputHandler = FindParentComponent<PlayerInputHandler>(transform);
        
        // 方式2：如果递归查找失败，尝试查找场景中的所有PlayerInputHandler组件
        if (playerInputHandler == null)
        {
            Debug.Log("[AnimationEventForwarder] 递归查找失败，尝试查找场景中的所有PlayerInputHandler组件");
            PlayerInputHandler[] handlers = FindObjectsOfType<PlayerInputHandler>();
            if (handlers.Length > 0)
            {
                // 选择第一个PlayerInputHandler组件
                playerInputHandler = handlers[0];
                Debug.Log("[AnimationEventForwarder] 通过FindObjectsOfType找到PlayerInputHandler组件");
            }
        }
        
        // 方式3：如果前两种方式都失败，尝试通过NetworkPlayer查找
        if (playerInputHandler == null)
        {
            Debug.Log("[AnimationEventForwarder] 前两种方式失败，尝试通过NetworkPlayer查找");
            NetworkPlayer[] networkPlayers = FindObjectsOfType<NetworkPlayer>();
            foreach (NetworkPlayer player in networkPlayers)
            {
                PlayerInputHandler handler = player.GetComponent<PlayerInputHandler>();
                if (handler != null)
                {
                    playerInputHandler = handler;
                    Debug.Log("[AnimationEventForwarder] 通过NetworkPlayer找到PlayerInputHandler组件");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 递归查找父对象上的指定组件
    /// </summary>
    private T FindParentComponent<T>(Transform current) where T : Component
    {
        T component = current.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        if (current.parent != null)
        {
            return FindParentComponent<T>(current.parent);
        }

        return null;
    }

    /// <summary>
    /// 攻击命中事件转发
    /// 在攻击动画的命中帧调用，直接转发给PlayerInputHandler
    /// 攻击周期由PlayerInputHandler统一管理
    /// </summary>
    public void OnAttackHit()
    {
        Debug.Log("[AnimationEventForwarder] OnAttackHit被调用，转发给PlayerInputHandler");
        
        // 如果PlayerInputHandler为空，尝试重新查找
        if (playerInputHandler == null)
        {
            Debug.LogWarning("[AnimationEventForwarder] PlayerInputHandler为空，尝试重新查找");
            FindPlayerInputHandler();
        }
        
        if (playerInputHandler != null)
        {
            playerInputHandler.OnAttackHit();
        }
        else
        {
            Debug.LogWarning("[AnimationEventForwarder] 没有找到PlayerInputHandler组件来接收OnAttackHit事件");
        }
    }
}