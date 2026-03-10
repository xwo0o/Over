---
name: "server-authoritative-network"
description: "设计服务器权威网络架构。当用户需要设计Mirror网络系统、实现服务器权威逻辑、处理网络同步问题时调用。"
---

# 服务器权威网络架构设计

此skill专门用于设计和实现基于Unity + Mirror插件的服务器权威网络架构。

## 适用场景

**必须调用此skill的情况：**
- 用户需要设计新的网络同步系统
- 实现服务器权威的游戏逻辑（移动、战斗、状态等）
- 处理客户端与服务器之间的数据同步
- 解决网络延迟、预测、插值等问题
- 设计对象池、敌人生成等网络相关系统
- 处理玩家重连、状态恢复等网络异常情况
- 实现网络事件总线、RPC调用等通信机制

**技术栈：**
- Unity 2022.3.15f1
- Mirror网络插件
- 服务器权威的主机-客户端模式
- MVC架构模式
- 事件总线系统

## 核心设计原则

### 1. 服务器权威（Server Authority）
- **所有游戏逻辑必须在服务器端执行**
- 客户端只负责发送输入请求和显示预测结果
- 服务器验证所有客户端请求，拒绝非法操作
- 关键数据（血量、位置、资源等）由服务器维护和广播

### 2. 网络同步策略
- **NetworkBehaviour组件**：用于需要同步的对象
- **SyncVar**：自动同步服务器端变量到客户端
- **ServerRpc**：客户端调用服务器方法
- **ClientRpc**：服务器调用客户端方法
- **NetworkTransform**：处理位置、旋转、缩放同步

### 3. MVC分层架构
- **Model**：数据层，存储游戏状态（血量、位置、资源等）
- **View**：表现层，负责UI显示、动画播放、特效渲染
- **Controller**：逻辑层，处理输入、业务逻辑、网络通信

### 4. 事件总线系统
- 解耦网络事件和业务逻辑
- 支持事件的订阅和发布
- 处理跨模块通信

## 实现步骤

### 第一步：网络管理器设置
```csharp
// NetworkManager配置
- 网络传输层（Transport）
- 玩家预制体（Player Prefab）
- 注册的消息处理器
- 场景管理
```

### 第二步：玩家控制器设计
```csharp
// 服务器权威的玩家移动
[Server]
public void ServerMove(Vector3 direction)
{
    // 服务器端验证移动请求
    if (IsValidMove(direction))
    {
        // 更新服务器端位置
        transform.position += direction;
        // 自动同步到所有客户端
    }
}

// 客户端输入处理
void Update()
{
    if (isLocalPlayer)
    {
        Vector3 input = GetInput();
        ClientMove(input); // 发送RPC到服务器
    }
}
```

### 第三步：状态同步设计
```csharp
// 使用SyncVar同步关键数据
[SyncVar(hook = nameof(OnHealthChanged))]
public int health = 100;

// 当服务器端health变化时，自动调用客户端hook
void OnHealthChanged(int oldHealth, int newHealth)
{
    // 更新UI显示
    UpdateHealthUI(newHealth);
}
```

### 第四步：对象池网络管理
```csharp
// 服务器端管理对象池
[Server]
public void SpawnEnemy(Vector3 position)
{
    GameObject enemy = enemyPool.GetFromPool(position);
    NetworkServer.Spawn(enemy); // 在所有客户端生成
}

// 服务器端回收对象
[Server]
public void DespawnEnemy(GameObject enemy)
{
    NetworkServer.Unspawn(enemy); // 从所有客户端移除
    enemyPool.ReturnToPool(enemy); // 返回对象池
}
```

### 第五步：事件总线集成
```csharp
// 网络事件定义
public class NetworkEvents
{
    public static event Action<PlayerData> OnPlayerConnected;
    public static event Action<PlayerData> OnPlayerDisconnected;
    public static event Action<int> OnHealthChanged;
}

// 触发事件
[Server]
public void OnServerPlayerConnected(NetworkConnection conn)
{
    NetworkEvents.OnPlayerConnected?.Invoke(playerData);
}
```

## 常见问题解决方案

### 1. 客户端预测
- 客户端立即执行输入并显示结果
- 服务器验证后发送确认或修正
- 使用插值平滑显示服务器位置

### 2. 网络延迟补偿
- 服务器记录玩家输入时间戳
- 使用时间回溯验证移动合法性
- 客户端使用插值显示其他玩家位置

### 3. 状态同步优化
- 只同步变化的数据
- 使用压缩算法减少带宽
- 设置同步频率限制

### 4. 玩家重连处理
- 服务器保存玩家状态
- 重连时恢复之前的状态
- 重新同步所有必要数据

## 项目流程集成

根据项目基本流程：

1. **Character场景**：选择角色和网络模式
   - 使用NetworkManager.StartHost()启动主机
   - 使用NetworkManager.StartClient()连接服务器

2. **GameScene场景**：初始化游戏
   - 实例化玩家预制体
   - 服务器初始化敌人、资源对象池
   - 建立网络事件订阅

3. **游戏运行**：持续同步
   - 客户端发送输入请求
   - 服务器处理并广播结果
   - 客户端接收并更新显示

## 代码规范

- 所有网络方法使用[Server]或[Client]属性标记
- ServerRpc方法名以"Server"开头
- ClientRpc方法名以"Client"开头
- SyncVar变量使用hook方法处理客户端更新
- 所有中文注释说明网络逻辑

## 安全注意事项

- 永远不要信任客户端数据
- 服务器端验证所有请求
- 使用防作弊机制（速度检测、位置验证等）
- 敏感操作（购买、升级）必须在服务器端执行
