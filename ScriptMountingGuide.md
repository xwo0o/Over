# 游戏脚本挂载说明文档

本文档详细说明游戏中各个脚本的挂载位置、依赖项和使用方法，帮助开发者正确配置游戏场景。

## 核心系统脚本
 
### 1. NetworkManager 相关脚本

#### CustomNetworkManager
- **挂载位置**：场景中的 `NetworkManager` GameObject
- **依赖项**：
  - 需要添加 `NetworkManager` 组件
  - 需要添加 `NetworkManagerHUD` 组件（可选，用于调试）
- **主要功能**：
  - 处理角色选择和玩家生成
  - 管理角色预设体的Addressable加载
  - 处理网络连接和玩家同步
- **使用方法**：
  - 在Inspector面板中，为每个角色设置对应的 `AssetReference`
  - 设置 `Default Player Prefab Reference` 作为备选
  - 确保 `gameSceneName` 字段设置为正确的游戏场景名称

#### NetworkManagerSetup
- **挂载位置**：场景中的 `NetworkManager` GameObject
- **依赖项**：
  - 需要 `CustomNetworkManager` 组件
- **主要功能**：
  - 配置 `NetworkManager` 的基本参数
  - 提供启动服务器、客户端和主机的方法
- **使用方法**：
  - 自动配置 `NetworkManager`，无需手动设置

### 2. 对象池系统

#### ObjectPoolManager
- **挂载位置**：场景中的 `ObjectPoolManager` GameObject
- **依赖项**：
  - 需要添加 `NetworkBehaviour` 组件
- **主要功能**：
  - 管理敌人和资源的对象池
  - 异步加载敌人和资源预设体
  - 提供获取和回收对象的方法
- **使用方法**：
  - 在Inspector面板中，为每个敌人和资源预设体设置对应的 `AssetReference`
  - 可以调整初始对象池大小
  - 服务器启动时会自动异步加载所有预设体

#### ObjectPool
- **挂载位置**：由 `ObjectPoolManager` 动态创建
- **主要功能**：
  - 管理单个类型的对象池
  - 提供获取和回收对象的方法
- **使用方法**：
  - 无需手动挂载，由 `ObjectPoolManager` 自动创建

### 3. 事件系统

#### EventBus
- **挂载位置**：场景中的 `EventBus` GameObject
- **主要功能**：
  - 处理游戏事件的订阅和触发
  - 提供全局事件管理机制
- **使用方法**：
  - 在脚本中使用 `EventBus.Instance.Subscribe()` 订阅事件
  - 使用 `EventBus.Instance.Trigger()` 触发事件
  - 确保场景中只有一个 `EventBus` 实例

### 4. 数据管理

#### DataManager
- **挂载位置**：场景中的 `DataManager` GameObject
- **主要功能**：
  - 加载和管理所有游戏数据
  - 提供获取角色、敌人、资源和建筑数据的方法
  - 从JSON配置文件加载数据
- **使用方法**：
  - 确保 `StreamingAssets` 目录下存在正确的JSON配置文件
  - 无需手动调用方法，自动在启动时加载数据

## 游戏玩法脚本

### 1. 角色系统

#### PlayerCharacter
- **挂载位置**：玩家角色预设体
- **依赖项**：
  - 需要添加 `NetworkBehaviour` 组件
  - 需要添加 `Animator` 组件
  - 需要添加 `Rigidbody` 组件
- **主要功能**：
  - 处理角色的属性和状态
  - 处理角色的移动和攻击
  - 处理角色的网络同步
  - 处理角色的回血逻辑
- **使用方法**：
  - 将脚本挂载到玩家角色预设体上
  - 在Inspector面板中，设置动画组件和刚体组件的引用
  - 确保角色数据中包含正确的属性值

#### EnemyAI
- **挂载位置**：敌人角色预设体
- **依赖项**：
  - 需要添加 `NetworkBehaviour` 组件
  - 需要添加 `NavMeshAgent` 组件
  - 需要添加 `Animator` 组件
- **主要功能**：
  - 处理敌人的行为和状态
  - 处理敌人的视野检测和追击
  - 处理敌人的攻击和死亡
- **使用方法**：
  - 将脚本挂载到敌人角色预设体上
  - 在Inspector面板中，设置导航代理、动画组件等引用
  - 确保敌人数据中包含正确的属性值

#### EnemyAIStateMachine
- **挂载位置**：敌人角色预设体
- **依赖项**：
  - 需要 `EnemyAI` 组件
- **主要功能**：
  - 管理敌人的状态机（巡逻、追击、攻击、死亡）
  - 处理状态切换和行为逻辑
- **使用方法**：
  - 将脚本挂载到敌人角色预设体上
  - 无需手动配置，自动使用 `EnemyAI` 组件

### 2. 资源系统

#### NetworkResource
- **挂载位置**：资源预设体
- **依赖项**：
  - 需要添加 `NetworkBehaviour` 组件
  - 需要添加 `Collider` 组件
  - 需要添加 `MeshRenderer` 组件
- **主要功能**：
  - 处理资源的收集和同步
  - 处理资源的高亮和交互
  - 处理资源的收集特效
- **使用方法**：
  - 将脚本挂载到资源预设体上
  - 在Inspector面板中，设置碰撞体、网格渲染器等引用
  - 确保资源数据中包含正确的属性值

### 3. 建造系统

#### BuildingManager
- **挂载位置**：场景中的 `BuildingManager` GameObject
- **依赖项**：
  - 需要 `GridSystem` 组件
  - 需要 `PlayerInventory` 组件
- **主要功能**：
  - 处理建筑的放置和预览
  - 处理资源验证和扣除
  - 处理建筑的Addressable加载
- **使用方法**：
  - 在Inspector面板中，设置网格系统、预览预设体等
  - 建筑数据中的 `addressableKey` 字段用于加载建筑预设体
  - 建造时会自动使用Addressable异步加载和实例化建筑

#### GridSystem
- **挂载位置**：场景中的 `GridSystem` GameObject
- **主要功能**：
  - 处理建筑的网格对齐
  - 提供网格坐标和世界坐标的转换
  - 检查位置是否可以建造
- **使用方法**：
  - 在Inspector面板中，设置 `gridSize` 字段（建议设置为2.0f）
  - 确保网格覆盖整个可建造区域

### 4. 背包系统

#### PlayerInventory
- **挂载位置**：玩家角色预设体
- **依赖项**：
  - 需要 `NetworkBehaviour` 组件
- **主要功能**：
  - 管理玩家的资源背包
  - 处理资源的添加、移除和堆叠
  - 处理资源的网络同步
- **使用方法**：
  - 将脚本挂载到玩家角色预设体上
  - 在Inspector面板中，设置 `maxSlots` 字段（建议设置为20）
  - 资源堆叠数量由资源数据中的 `maxStack` 字段决定

#### InventoryUI
- **挂载位置**：玩家UI画布
- **依赖项**：
  - 需要 `PlayerInventory` 组件
  - 需要UI组件（如 `Canvas`、`Image`、`Text` 等）
- **主要功能**：
  - 显示玩家背包的内容
  - 处理背包的交互和操作
  - 更新背包的显示
- **使用方法**：
  - 将脚本挂载到UI画布上
  - 在Inspector面板中，设置背包UI元素的引用
  - 确保UI元素的层级和布局正确

## 场景配置建议

### 角色选择场景
1. 创建 `NetworkManager` GameObject，添加 `CustomNetworkManager` 和 `NetworkManagerSetup` 组件
2. 创建 `EventBus` GameObject，添加 `EventBus` 组件
3. 创建 `DataManager` GameObject，添加 `DataManager` 组件
4. 创建角色选择UI，添加 `CharacterSelectUI` 组件
5. 确保场景中包含 `CharacterSelectScene` 场景名称

### 游戏场景
1. 创建 `NetworkManager` GameObject，添加 `CustomNetworkManager` 和 `NetworkManagerSetup` 组件
2. 创建 `EventBus` GameObject，添加 `EventBus` 组件
3. 创建 `DataManager` GameObject，添加 `DataManager` 组件
4. 创建 `GridSystem` GameObject，添加 `GridSystem` 组件
5. 创建 `ObjectPoolManager` GameObject，添加 `ObjectPoolManager` 组件
6. 创建 `BuildingManager` GameObject，添加 `BuildingManager` 组件
7. 确保场景中包含 `GameScene` 场景名称
8. 添加地形、光源和其他环境元素

## 注意事项

1. **Addressable配置**：
   - 确保所有预设体都已添加到Addressable Group中
   - 为每个预设体分配唯一的AssetReference
   - 在Addressables Groups窗口中管理预设体资源

2. **网络同步**：
   - 确保所有需要网络同步的属性都使用 `[SyncVar]` 标记
   - 确保 `Command` 和 `ClientRpc` 方法的使用符合Mirror规范
   - 避免在客户端直接修改需要同步的属性

3. **对象池设置**：
   - 根据游戏需求调整初始对象池大小
   - 监控对象池的使用情况，及时调整大小
   - 确保对象池管理器在服务器端初始化

4. **性能优化**：
   - 避免在Update方法中执行复杂计算
   - 使用对象池减少内存分配
   - 优化Addressable资源的加载和卸载

5. **调试建议**：
   - 使用 `NetworkManagerHUD` 组件进行网络调试
   - 查看控制台日志，了解脚本的运行情况
   - 使用Unity Profiler监控游戏性能

## 版本信息

- 文档版本：v1.0
- 游戏引擎：Unity 2022.3.15f1
- 网络框架：Mirror
- 资源管理：Addressables

## 更新日志

- 2025-12-15：初始版本，包含核心系统和游戏玩法脚本的挂载说明

---

本文档将随着游戏开发的进展不断更新，确保提供最新的脚本挂载说明。