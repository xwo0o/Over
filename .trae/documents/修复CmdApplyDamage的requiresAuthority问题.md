## 问题根源

`WeaponDamageSystem.CmdApplyDamage` 使用了默认的 `[Command]`（requiresAuthority = true），连接玩家对这个 NetworkBehaviour 没有 authority，所以 Command 无法发送到服务器。

## 修复方案

将 `WeaponDamageSystem.CmdApplyDamage` 改为 `[Command(requiresAuthority = false)]`，允许任何客户端发送伤害应用命令。

## 具体修改

**文件**: WeaponDamageSystem.cs
**修改**: 将 `[Command]` 改为 `[Command(requiresAuthority = false)]`

这样修改后：
1. 连接玩家攻击 → 动画事件触发
2. 发送 CmdPerformDamageCheck 到服务器
3. 服务器执行伤害检测 → 检测到敌人
4. 调用 CmdApplyDamage → 现在可以发送到服务器
5. 服务器应用伤害 → 同步血量