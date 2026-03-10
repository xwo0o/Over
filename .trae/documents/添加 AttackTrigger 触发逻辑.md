## 问题分析

Animator 状态机转换条件需要同时满足：
1. `WeaponType_int == 1` ✓
2. `AttackStage > 0` ✓
3. `AttackTrigger` (Trigger) ✗ - 缺失

之前移除了 AttackTrigger 的触发，导致无法进入攻击状态。

## 修复方案

1. 重新添加 AttackTrigger 参数
2. 在开始攻击时触发 AttackTrigger
3. 在连击时也需要触发 AttackTrigger
4. 重置时清除 AttackTrigger

## 代码修改

### 1. 添加 AttackTrigger 参数
```csharp
private static readonly int AttackTrigger = Animator.StringToHash("AttackTrigger");
```

### 2. 在 UpdateAnimatorAttackStage 中触发 Trigger
```csharp
// 使用Trigger触发动画状态转换（从Idle进入攻击状态或连击）
if (stage > 0)
{
    animator.SetTrigger(AttackTrigger);
}
```

### 3. 在 ResetCombo 中重置 Trigger
```csharp
animator.ResetTrigger(AttackTrigger);
```

这样设计符合原本意图：
- AttackStage 用于标识当前攻击阶段
- AttackTrigger 用于触发状态转换（开始攻击或连击）