# Inventory 初始化时机修复 - 任务列表

## [x] Task 1: 修复 Inventory 初始化时机
- **Priority**: P0
- **Description**: 
  - 移除 Awake() 中的 isServer 检查
  - 使用 OnStartServer() 作为服务器端初始化的唯一入口
  - 确保客户端不初始化 slots，只等待同步
- **Acceptance Criteria**: 
  - 服务器端 slots 正确初始化
  - 客户端 slots 从服务器同步

## [x] Task 2: 修复 NetworkPlayer 初始化顺序
- **Priority**: P0
- **Depends On**: [Task 1]
- **Description**: 
  - 确保 InitializeInventoryWeapons() 在 Inventory.OnStartServer() 之后调用
  - 添加初始化状态检查
- **Acceptance Criteria**: 
  - 武器正确放置在背包中
  - 初始化顺序正确

## [x] Task 3: 验证修复效果
- **Priority**: P1
- **Depends On**: [Task 2]
- **Description**: 
  - 测试主机玩家背包初始化
  - 测试客户端玩家背包同步
  - 测试客户端玩家收集资源
- **Acceptance Criteria**: 
  - 所有测试通过

# Task Dependencies
- [Task 2] depends on [Task 1]
- [Task 3] depends on [Task 2]
