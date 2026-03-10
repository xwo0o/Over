## 问题根源

`AnimationEventForwarder.AE_Hit()` 中限制了只在服务器执行伤害判定：
```csharp
if (networkBehaviour != null && networkBehaviour.isServer)
{
    weaponStateManager.PerformDamageCheck();
}
```

这导致连接玩家的攻击动画事件无法触发伤害检测。

## 修复方案

修改 `AnimationEventForwarder.AE_Hit()`，移除 `isServer` 检查，让动画事件在所有客户端都调用 `PerformDamageCheck()`。

`WeaponStateManager.PerformDamageCheck()` 已经设计为：
- 在客户端调用时发送 Command 到服务器
- 使用 `[Command(requiresAuthority = false)]` 允许任何客户端发送

所以只需要修改 AnimationEventForwarder.cs 即可。

## 具体修改

**文件**: AnimationEventForwarder.cs
**修改**: 将 AE_Hit 方法中的 `if (networkBehaviour != null && networkBehaviour.isServer)` 改为 `if (networkBehaviour != null)`

这样修改后：
- 连接客户端触发 AE_Hit → 调用 PerformDamageCheck → 发送 CmdPerformDamageCheck 到服务器
- 服务器执行伤害检测 → 应用伤害 → 同步血量