using UnityEngine;

/// <summary>
/// 动画事件转发器 - 挂在模型上，将动画事件转发给父物体的 WeaponAnimEvents
/// </summary>
public class AnimEventForwarder : MonoBehaviour
{
    private WeaponAnimEvents targetEvents;

    void Awake()
    {
        // 查找父物体上的 WeaponAnimEvents
        targetEvents = GetComponentInParent<WeaponAnimEvents>();
        
        if (targetEvents == null)
        {
            Debug.LogError("[AnimEventForwarder] 未找到父物体上的 WeaponAnimEvents 组件");
        }
    }

    /// <summary>
    /// 转发 AE_Hit 事件
    /// </summary>
    public void AE_Hit(int hitIndex)
    {
        if (targetEvents != null)
        {
            targetEvents.AE_Hit(hitIndex);
        }
    }

    /// <summary>
    /// 转发 EndAttack 事件
    /// </summary>
    public void EndAttack()
    {
        if (targetEvents != null)
        {
            targetEvents.EndAttack();
        }
    }
}
