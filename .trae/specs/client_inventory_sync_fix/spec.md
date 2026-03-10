# 客户端背包数据同步修复 Spec

## Why
当使用 ParrySync 复制编辑器作为客户端连接主机时，客户端玩家收集资源时背包数据不会增加。这是一个严重的网络同步问题，影响多人游戏的核心玩法。

## What Changes
- 分析 Inventory 组件在客户端和服务器端的初始化时机
- 检查 SyncList 在客户端是否正确接收服务器同步的数据
- 修复 Inventory.Awake() 中可能覆盖服务器同步数据的问题
- 确保 SyncList 正确初始化和同步

## Impact
- Affected specs: inventory_sync_optimization
- Affected code: 
  - Inventory.cs
  - NetworkPlayer.cs
  - InventoryUIController.cs

## Root Cause Analysis

### 可能原因1：Inventory.Awake() 覆盖同步数据
在 `NetworkPlayer.OnStartClient()` 中，客户端创建了一个新的 `Inventory` 组件。这个新组件的 `Awake()` 方法会清空并重新初始化 `slots`，可能覆盖了服务器同步过来的数据。

### 可能原因2：SyncList 初始化时机问题
`SyncList` 需要在 `NetworkBehaviour` 完全初始化后才能正确同步。如果在 `Awake()` 中修改 `SyncList`，可能会干扰网络同步。

### 可能原因3：Inventory 组件重复创建
服务器端和客户端可能各自创建了独立的 `Inventory` 组件实例，导致数据不同步。

## ADDED Requirements

### Requirement: 客户端背包数据正确同步
系统应当确保客户端玩家的背包数据能够正确接收服务器同步的数据。

#### Scenario: 客户端玩家收集资源后背包数据增加
- **WHEN** 客户端玩家收集资源
- **THEN** 服务器处理收集请求后，客户端背包数据正确增加

#### Scenario: 客户端背包初始化不覆盖服务器数据
- **WHEN** 客户端连接服务器并初始化背包
- **THEN** 背包数据从服务器同步，不被本地初始化覆盖

## MODIFIED Requirements

### Requirement: Inventory 初始化逻辑优化
系统应当区分服务器端初始化和客户端同步初始化，避免客户端覆盖服务器数据。
