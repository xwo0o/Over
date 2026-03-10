## 新方案核心

直接同步 **speed 浮点值**（0-1），而不是布尔值 isRunning

## 实现步骤

### 1. 添加 speed SyncVar
在 NetworkPlayer 中添加：
```csharp
[SyncVar(hook = nameof(OnSpeedChanged))]
private float syncedSpeed;
```

### 2. 修改输入处理
PlayerInputHandler 中：
- 计算本地 speed 值（0-1 连续值）
- 发送 `CmdSetSpeed(float speed)` 到服务器
- 本地立即更新动画（预测）

### 3. 服务器处理
NetworkPlayer 中：
- 接收 `CmdSetSpeed(float speed)`
- 更新 `syncedSpeed` SyncVar
- 自动同步到所有客户端

### 4. 客户端更新
- SyncVar hook `OnSpeedChanged` 接收新 speed 值
- 直接设置 Animator.speed，无需协程过渡
- 可选：使用短暂插值避免跳变

## 优势

1. **连续值同步**：0-1 之间的所有变化都同步
2. **无延迟感**：客户端看到和其他客户端完全一致的 speed 变化
3. **主机和客户端一致**：都使用相同的同步机制
4. **简单高效**：无需复杂的协程过渡逻辑

## 修改文件

- [NetworkPlayer.cs](file:///f:/团结引擎/项目存放/Over/Assets/Scripts/Core/NetworkPlayer.cs)
  - 添加 `syncedSpeed` SyncVar
  - 添加 `CmdSetSpeed` Command
  - 添加 `OnSpeedChanged` hook
  - 移除 `isRunning` 相关代码
  - 移除 `BlendSpeedCoroutine`

- [PlayerInputHandler.cs](file:///f:/团结引擎/项目存放/Over/Assets/Scripts/Character/PlayerInputHandler.cs)
  - 修改输入处理，计算 speed 值
  - 调用 `CmdSetSpeed` 发送 speed 值
  - 本地立即更新动画