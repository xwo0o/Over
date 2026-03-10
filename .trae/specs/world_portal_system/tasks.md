# 跨世界连接系统 - 实施计划（分解和优先级任务列表）

## [ ] 任务 1: 创建 WorldPortalData 数据类
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 创建数据类，用于存储世界连接信息（IP、端口、连接历史等）
  - 实现基本的地址验证逻辑
- **Acceptance Criteria Addressed**: [AC-2]
- **Test Requirements**:
  - `programmatic` TR-1.1: 验证IPv4地址格式验证功能正常
  - `programmatic` TR-1.2: 验证端口号范围验证（1-65535）功能正常
  - `human-judgement` TR-1.3: 代码结构清晰，包含中文注释
- **Notes**: 该类应独立于Unity场景，可复用

## [ ] 任务 2: 创建 WorldPortalManager 管理器类
- **Priority**: P0
- **Depends On**: Task 1
- **Description**: 
  - 创建单例管理器，处理世界切换的核心逻辑
  - 实现断开当前连接的功能
  - 实现作为客户端连接新地址的功能
  - 与 NetworkConnectionManager 集成
- **Acceptance Criteria Addressed**: [AC-3, AC-4]
- **Test Requirements**:
  - `programmatic` TR-2.1: 验证能正确调用 NetworkConnectionManager.Disconnect()
  - `programmatic` TR-2.2: 验证能正确设置新地址并调用 StartClient()
  - `programmatic` TR-2.3: 验证网络状态正确转换
  - `human-judgement` TR-2.4: 日志记录完整，便于调试
- **Notes**: 需要考虑协程处理异步断开和连接

## [ ] 任务 3: 创建 WorldPortalUI UI控制器
- **Priority**: P0
- **Depends On**: Task 2
- **Description**: 
  - 创建UI控制器脚本，处理用户输入和交互
  - 实现IP地址和端口输入框
  - 实现连接、取消按钮事件
  - 实现连接状态显示
  - 与现有UI风格保持一致
- **Acceptance Criteria Addressed**: [AC-1, AC-2, AC-5]
- **Test Requirements**:
  - `human-judgement` TR-3.1: UI布局合理，与现有UI风格一致
  - `programmatic` TR-3.2: 输入验证在输入时或点击连接时触发
  - `programmatic` TR-3.3: 点击连接按钮正确调用 WorldPortalManager
  - `programmatic` TR-3.4: 点击取消按钮正确关闭面板
- **Notes**: 参考现有UI脚本的实现方式（如 NetworkConnectionUI）

## [ ] 任务 4: 扩展 NetworkConnectionManager
- **Priority**: P1
- **Depends On**: None
- **Description**: 
  - 添加世界切换相关的事件回调
  - 确保 Disconnect() 能正确清理所有状态
  - 添加连接进度事件
- **Acceptance Criteria Addressed**: [AC-3, AC-4]
- **Test Requirements**:
  - `programmatic` TR-4.1: Disconnect() 后网络状态正确重置
  - `programmatic` TR-4.2: 事件回调正常触发
  - `human-judgement` TR-4.3: 不破坏现有功能

## [ ] 任务 5: 集成到 GameScene
- **Priority**: P1
- **Depends On**: Task 3
- **Description**: 
  - 创建跨世界连接UI预制体
  - 添加打开跨世界连接面板的触发按钮
  - 确保在游戏中能正常打开/关闭面板
- **Acceptance Criteria Addressed**: [AC-1]
- **Test Requirements**:
  - `human-judgement` TR-5.1: UI预制体可正常实例化
  - `human-judgement` TR-5.2: 面板打开/关闭动画流畅
  - `programmatic` TR-5.3: 不影响游戏其他功能

## [ ] 任务 6: 错误处理和用户反馈
- **Priority**: P1
- **Depends On**: Task 2, Task 3
- **Description**: 
  - 实现各种错误场景的处理
  - 添加连接进度指示器
  - 添加友好的错误提示信息
- **Acceptance Criteria Addressed**: [AC-2, AC-5]
- **Test Requirements**:
  - `human-judgement` TR-6.1: 错误提示信息清晰易懂
  - `programmatic` TR-6.2: 连接超时处理正常
  - `human-judgement` TR-6.3: 加载状态指示明显

## [ ] 任务 7: 更新项目文档
- **Priority**: P2
- **Depends On**: All Tasks
- **Description**: 
  - 更新项目文件梳理.md，添加跨世界系统说明
  - 添加使用说明
- **Acceptance Criteria Addressed**: []
- **Test Requirements**:
  - `human-judgement` TR-7.1: 文档更新完整
  - `human-judgement` TR-7.2: 使用说明清晰
