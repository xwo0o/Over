## 步骤

### 1. 修改 ConfigureNetworkAnimator 方法
删除预制体上的 NetworkAnimator，改为代码动态添加：

```csharp
private void ConfigureNetworkAnimator(Animator animator)
{
    if (animator == null || networkPlayer == null) return;

    // 查找现有的 NetworkAnimator
    NetworkAnimator netAnim = networkPlayer.GetComponent<NetworkAnimator>();
    
    // 如果存在且 animator 不匹配，删除旧的
    if (netAnim != null && netAnim.animator != animator)
    {
        Destroy(netAnim);
        netAnim = null;
    }
    
    // 如果不存在，动态添加
    if (netAnim == null)
    {
        netAnim = networkPlayer.gameObject.AddComponent<NetworkAnimator>();
        Debug.Log("[SceneAwareAnimatorManager] 动态添加 NetworkAnimator 组件");
    }
    
    // 配置 NetworkAnimator
    netAnim.animator = animator;
    netAnim.clientAuthority = false;  // 服务器权威
    netAnim.syncDirection = NetworkAnimator.SyncDirection.ServerToClient;
    
    Debug.Log($"[SceneAwareAnimatorManager] NetworkAnimator 已配置 - Animator={animator.name}");
}
```

### 2. 从 PlayerContainer 预制体删除 NetworkAnimator
- 打开 PlayerContainer 预制体
- 删除 NetworkAnimator 组件
- 保存预制体

### 3. 测试
- 运行游戏
- NetworkAnimator 会在模型加载后自动添加
- 动画应该能正常同步