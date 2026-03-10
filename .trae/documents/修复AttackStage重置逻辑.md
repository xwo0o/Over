## 实现目标

添加一个输入缓存系统，限制最大连击次数为3次：

* Idle状态连续点击5次 → 只记录3次，播放完3段后停止

* 第2段动画中多次点击 → 只记录到第3段

* 第3段动画中点击 → 不采纳

* 退出子状态机后 → 重置缓存

## 实现步骤

### 1. 添加字段

在 WeaponStateManager 中添加：

* `private int pendingAttackCount = 0` - 待处理的攻击次数

* `private const int MAX_COMBO = 3` - 最大连击数

### 2. 修改 TryAttack() 方法

* 增加缓存逻辑：如果 `pendingAttackCount < MAX_COMBO`，则 `pendingAttackCount++`

* 如果当前可以攻击（Idle/ComboWindow），立即执行攻击

### 3. 修改 EndAttack() 方法

* 动画结束时，如果 `pendingAttackCount > 1`（还有缓存的点击），自动触发下一段攻击

* `pendingAttackCount--`

### 4. 添加 ResetPendingAttacks() 方法

* 在退出子状态机时调用，重置 `pendingAttackCount = 0`

### 5. 修改 OnComboReset() 方法

* 调用 `ResetPendingAttacks()`，确保退出时重置缓存

## 代码变更文件

* WeaponStateManager.cs

