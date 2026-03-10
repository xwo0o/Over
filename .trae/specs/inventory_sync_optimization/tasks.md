# 背包同步优化方案 - The Implementation Plan (Decomposed and Prioritized Task List)

## [x] Task 1: 创建自定义 SyncList 类型
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 直接使用 SyncList&lt;InventoryItem&gt;，无需额外自定义类（InventoryItem 已有 Serializable 特性）
- **Acceptance Criteria Addressed**: [AC-1]
- **Test Requirements**:
  - `programmatic` TR-1.1: 代码可以正确编译 ✓
  - `programmatic` TR-1.2: Mirror 可以正确序列化和反序列化 InventoryItem ✓
- **Notes**: 参考 Mirror 的 SyncList 文档和示例

## [x] Task 2: 修改 Inventory.cs 使用新的 SyncList
- **Priority**: P0
- **Depends On**: [Task 1]
- **Description**: 
  - 将 Inventory.cs 中的 `[SyncVar(hook = nameof(OnSlotsChanged))] public InventoryItem[] slots;` 改为 `public readonly SyncList&lt;InventoryItem&gt; slots = new SyncList&lt;InventoryItem&gt;();` ✓
  - 修改 Awake 方法，初始化 slots 为 SyncList 而不是数组 ✓
  - 修改 OnSlotsChanged hook 以适配 SyncList 的 Callback 机制 ✓
- **Acceptance Criteria Addressed**: [AC-2, AC-6]
- **Test Requirements**:
  - `programmatic` TR-2.1: 代码编译无错误 ✓
  - `programmatic` TR-2.2: Awake 方法正确初始化所有格子 ✓
  - `human-judgement` TR-2.3: UI 刷新逻辑正常工作
- **Notes**: SyncList 使用 Callback 机制替代 SyncVar 的 hook

## [x] Task 3: 更新 Add 方法
- **Priority**: P0
- **Depends On**: [Task 2]
- **Description**: 
  - 移除 Add 方法中的 `InventoryItem[] newSlots = (InventoryItem[])slots.Clone(); slots = newSlots;` 代码 ✓
  - 修改对 slots 的访问，确保与 SyncList 的索引器兼容（slots.Length → slots.Count）✓
  - 当修改某个格子时，使用 `slots[i] = slot;` 来触发 SyncList 的变化检测 ✓
- **Acceptance Criteria Addressed**: [AC-3, AC-4]
- **Test Requirements**:
  - `programmatic` TR-3.1: 所有 slots.Clone() 调用已移除 ✓
  - `human-judgement` TR-3.2: Add 功能正常工作
- **Notes**: SyncList 在修改元素时会自动同步变化

## [x] Task 4: 更新 Consume 方法
- **Priority**: P0
- **Depends On**: [Task 3]
- **Description**: 
  - 移除 Consume 方法中的数组克隆代码 ✓
  - 确保对 slots 的修改正确触发 SyncList 同步 ✓
- **Acceptance Criteria Addressed**: [AC-3, AC-4]
- **Test Requirements**:
  - `programmatic` TR-4.1: 所有 slots.Clone() 调用已移除 ✓
  - `human-judgement` TR-4.2: Consume 功能正常工作

## [x] Task 5: 更新 CmdSwapSlots 方法
- **Priority**: P0
- **Depends On**: [Task 4]
- **Description**: 
  - 移除 CmdSwapSlots 方法中的数组克隆代码 ✓
  - 确保格子交换操作正确触发 SyncList 同步 ✓
- **Acceptance Criteria Addressed**: [AC-3, AC-4]
- **Test Requirements**:
  - `programmatic` TR-5.1: 所有 slots.Clone() 调用已移除 ✓
  - `human-judgement` TR-5.2: 交换格子功能正常工作

## [x] Task 6: 更新 CmdConsumeFood 方法
- **Priority**: P0
- **Depends On**: [Task 5]
- **Description**: 
  - 移除 CmdConsumeFood 方法中的数组克隆代码 ✓
  - 确保食物消耗功能正常触发 SyncList 同步 ✓
- **Acceptance Criteria Addressed**: [AC-3, AC-4]
- **Test Requirements**:
  - `programmatic` TR-6.1: 所有 slots.Clone() 调用已移除 ✓
  - `human-judgement` TR-6.2: 消耗食物功能正常工作

## [ ] Task 7: 完整功能测试
- **Priority**: P1
- **Depends On**: [Task 6]
- **Description**: 
  - 测试所有背包功能（添加、消耗、交换、食物回血）
  - 验证多人联机同步
  - 检查 UI 刷新
- **Acceptance Criteria Addressed**: [AC-4, AC-5, AC-6]
- **Test Requirements**:
  - `human-judgement` TR-7.1: 所有功能与修改前行为一致
  - `human-judgement` TR-7.2: 多人联机同步正常
  - `human-judgement` TR-7.3: UI 正确刷新
