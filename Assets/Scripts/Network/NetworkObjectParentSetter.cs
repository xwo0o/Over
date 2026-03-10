using UnityEngine;
using Mirror;

/// <summary>
/// 网络对象父对象设置器
/// 在客户端上自动将网络对象设置到AutoObjectPoolManager的activeRoot下
/// </summary>
public class NetworkObjectParentSetter : NetworkBehaviour
{
    /// <summary>
    /// 客户端启动时自动设置父对象关系
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // 在所有客户端上执行（包括主机）
        // 检查是否是客户端（主机也是客户端）
        if (NetworkClient.spawned.ContainsKey(netId))
        {
            SetObjectParent();
        }
    }
    
    /// <summary>
    /// 设置对象的父对象为AutoObjectPoolManager的activeRoot
    /// </summary>
    private void SetObjectParent()
    {
        try
        {
            // 查找AutoObjectPoolManager实例
            AutoObjectPoolManager poolManager = AutoObjectPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogWarning($"[NetworkObjectParentSetter] AutoObjectPoolManager实例未找到 - 对象: {gameObject.name}, netId: {netId}");
                return;
            }
            
            // 确保ActiveRoot存在
            if (poolManager.ActiveRoot == null)
            {
                Debug.LogWarning($"[NetworkObjectParentSetter] AutoObjectPoolManager的ActiveRoot未找到 - 对象: {gameObject.name}, netId: {netId}");
                return;
            }
            
            // 检查当前父对象是否已经是ActiveRoot，避免重复设置
            if (transform.parent == poolManager.ActiveRoot.transform)
            {
                return;
            }
            
            // 设置父对象关系，保持世界坐标不变
            transform.SetParent(poolManager.ActiveRoot.transform, true);
            
            Debug.Log($"[NetworkObjectParentSetter] 已设置父对象 - 对象: {gameObject.name}, netId: {netId}, 父对象: {poolManager.ActiveRoot.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkObjectParentSetter] 设置父对象时发生异常 - 对象: {gameObject.name}, netId: {netId}, 异常: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
        }
    }
}
