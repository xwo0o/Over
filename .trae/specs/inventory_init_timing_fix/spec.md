# Inventory 初始化时机修复 Spec

## Why
当前 Inventory 的初始化逻辑存在严重问题：
1. `Inventory.Awake()` 中使用 `isServer` 判断，但在 Awake 时 NetworkBehaviour 的网络状态可能还未初始化，导致 `isServer` 为 false
2. 这导致服务器端的 slots 也没有正确初始化
3. 客户端无法同步到正确的数据，导致背包为空且无法收集资源

## What Changes
- 修改 Inventory 初始化逻辑，使用 `OnStartServer()` 而不是 `Awake()` 来初始化 slots
- 确保 NetworkPlayer.InitializeInventoryWeapons() 在 Inventory 初始化之后调用
- 添加初始化顺序保证

## Impact
- Affected specs: client_inventory_sync_fix, inventory_sync_optimization
- Affected code: 
  - Inventory.cs
  - NetworkPlayer.cs

## Root Cause Analysis

### 问题根因：Awake 时机问题
Mirror 的 NetworkBehaviour 生命周期：
1. `Awake()` - 组件创建时调用，此时 `isServer`、`isClient` 都可能为 false
2. `OnStartServer()` - 服务器端启动时调用，此时网络状态已初始化
3. `OnStartClient()` - 客户端启动时调用

当前代码在 `Awake()` 中检查 `isServer`，但此时网络状态可能还未初始化，导致服务器端也不执行 `InitializeSlots()`。

### 问题根因：初始化顺序问题
`NetworkPlayer.OnStartServer()` 中：
1. 先获取/创建 Inventory 组件
2. 然后调用 `InitializeInventoryWeapons()`

但 Inventory 的 slots 可能还没有初始化，导致武器无法正确放置。

## ADDED Requirements

### Requirement: Inventory 正确初始化
系统应当在正确的时机初始化 Inventory 的 slots，确保服务器端和客户端都能正确工作。

#### Scenario: 服务器端 Inventory 正确初始化
- **WHEN** 服务器启动并创建玩家对象
- **THEN** Inventory 的 slots 被正确初始化为 capacity 个空槽位

#### Scenario: 客户端 Inventory 正确同步
- **WHEN** 客户端连接服务器
- **THEN** Inventory 的 slots 从服务器同步，包含正确的数据

## MODIFIED Requirements

### Requirement: 初始化顺序保证
系统应当确保 Inventory 初始化完成后再执行武器放置等后续操作。
