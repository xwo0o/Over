# 背包同步优化方案 - Product Requirement Document

## Overview
- **Summary**: 优化背包系统的网络同步机制，使用 SyncList 替代 SyncVar 数组，减少不必要的数组克隆操作，降低内存分配和网络开销。
- **Purpose**: 解决当前背包系统使用 SyncVar 标记数组时，每次修改都需要克隆整个数组才能触发同步的性能问题。
- **Target Users**: 游戏开发者、服务器端和客户端玩家

## Goals
- 移除背包数据同步时的数组克隆操作
- 减少不必要的内存分配和GC压力
- 使用 SyncList 实现增量同步，只同步变化的元素
- 保持现有功能完全不变，保证向后兼容

## Non-Goals (Out of Scope)
- 不改变背包的容量、堆叠限制等业务逻辑
- 不修改 UI 展示逻辑
- 不添加新的背包功能

## Background & Context
当前背包系统（Inventory.cs）使用 `[SyncVar(hook = nameof(OnSlotsChanged))]` 标记 `InventoryItem[] slots` 数组。每次修改背包物品（Add、Consume、SwapSlots 等方法）时，都需要通过 `slots.Clone()` 克隆整个数组并重新赋值，才能触发 SyncVar 的 hook 回调和网络同步。这种方式会造成：
- 每次修改都分配新的数组内存
- 网络传输整个数组，即使只有一个格子变化
- 增加 GC 压力，可能导致帧率下降

## Functional Requirements
- **FR-1**: 使用自定义 SyncList 类型替代 SyncVar 数组
- **FR-2**: 保持所有现有背包功能（Add、Consume、SwapSlots、CmdConsumeFood 等）正常工作
- **FR-3**: 保持 OnSlotsChanged hook 的行为不变，UI 刷新逻辑不受影响
- **FR-4**: 网络同步只传输变化的元素，而非整个背包数据

## Non-Functional Requirements
- **NFR-1**: 背包修改操作的内存分配减少 80% 以上
- **NFR-2**: 网络传输数据量减少（根据修改范围而定）
- **NFR-3**: 功能修改后无编译错误
- **NFR-4**: 多人联机测试同步正常

## Constraints
- **Technical**: 必须使用 Mirror 框架提供的 SyncList 机制
- **Business**: 必须在不破坏现有功能的前提下进行优化
- **Dependencies**: Mirror Networking 框架

## Assumptions
- Mirror 的 SyncList 机制在项目中正常工作
- InventoryItem 类可以正确序列化
- 现有测试覆盖主要背包功能

## Acceptance Criteria

### AC-1: 自定义 SyncList 类型创建成功
- **Given**: 项目环境正常
- **When**: 创建继承自 SyncList 的 InventoryItemList 类
- **Then**: 代码可以正确编译，Mirror 可以正确序列化该类型
- **Verification**: `programmatic`
- **Notes**: 确保 InventoryItem 可以正确读写

### AC-2: Inventory.cs 使用 SyncList 替代 SyncVar
- **Given**: InventoryItemList 已创建
- **When**: 修改 Inventory.cs，将 slots 变量类型改为 InventoryItemList
- **Then**: 代码编译通过，所有引用 slots 的地方正常工作
- **Verification**: `programmatic`

### AC-3: 移除所有数组克隆操作
- **Given**: Inventory.cs 已使用 SyncList
- **When**: 检查 Add、Consume、SwapSlots、CmdConsumeFood 等方法
- **Then**: 所有 `slots.Clone()` 操作已被移除
- **Verification**: `programmatic`

### AC-4: 背包功能正常工作
- **Given**: 代码修改完成
- **When**: 测试添加物品、消耗物品、交换格子、消耗食物等功能
- **Then**: 所有功能与修改前行为一致
- **Verification**: `human-judgment`
- **Notes**: 需要手动测试或运行现有测试

### AC-5: 网络同步正常
- **Given**: 代码修改完成
- **When**: 在多人联机环境中测试背包修改
- **Then**: 所有客户端背包数据同步一致
- **Verification**: `human-judgment`

### AC-6: UI 刷新正常
- **Given**: 背包数据修改
- **When**: 触发 OnSlotsChanged hook
- **Then**: UI 正确刷新显示
- **Verification**: `human-judgment`

## Open Questions
- [ ] 是否需要保留对旧版本数据的兼容性？（建议不需要，因为是同版本内优化）
