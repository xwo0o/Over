## 问题分析

在 `PerformDamageCheck` 中，使用 `Physics.OverlapSphere` 检测碰撞体，然后对每个碰撞体调用 `CmdApplyDamage`。

但一个敌人可能有**多个碰撞体**（例如：身体Collider、头部Collider等），导致：
- 一次 `AE_Hit` 检测到敌人的多个碰撞体
- 对每个碰撞体都发送伤害命令
- 同一敌人受到多次伤害

## 解决方案

修改 `PerformDamageCheck`，使用 `HashSet<uint>` 记录已经发送过伤害命令的敌人 NetId，确保每个敌人只受到一次伤害：

1. 在遍历碰撞体时，获取目标的 NetworkIdentity
2. 检查该 NetId 是否已经处理过
3. 如果已处理，跳过；否则发送伤害命令并记录

## 具体修改

### WeaponDamageSystem.cs

修改 `PerformDamageCheck` 方法：
- 添加 `HashSet<uint> processedTargets` 记录已处理的敌人
- 遍历碰撞体时，先检查 NetId 是否已处理
- 确保每个敌人只受到一次伤害

这样修改后：
- 斧头的两次 `AE_Hit` 各造成 80 伤害，总共 160
- 每个 `AE_Hit` 内，同一敌人只受到一次伤害