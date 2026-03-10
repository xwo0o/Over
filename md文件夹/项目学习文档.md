# Over 项目学习文档

## 目录
- [对象池系统详解](#对象池系统详解)
- [背包系统详解](#背包系统详解)
  - [一、实现思路](#一实现思路-1)
  - [二、核心数据结构](#二核心数据结构-1)
  - [三、核心算法](#三核心算法-1)
  - [四、与其他模块的联系](#四与其他模块的联系-1)
  - [五、UI交互流程](#五ui交互流程)
  - [六、关键设计点](#六关键设计点-1)
  - [七、总结](#七总结-1)
- [敌人系统详解](#敌人系统详解)
  - [一、实现思路](#一实现思路-2)
  - [二、核心数据结构](#二核心数据结构-2)
  - [三、核心算法](#三核心算法-2)
  - [四、与其他模块的联系](#四与其他模块的联系-2)
  - [五、关键设计点](#五关键设计点-2)
  - [六、总结](#六总结-2)
- [武器系统详解](#武器系统详解)
  - [一、实现思路](#一实现思路-3)
  - [二、核心数据结构](#二核心数据结构-3)
  - [三、核心算法](#三核心算法-3)
  - [四、与其他模块的联系](#四与其他模块的联系-3)
  - [五、关键设计点](#五关键设计点-3)
  - [六、总结](#六总结-3)
- [角色系统详解](#角色系统详解)
  - [一、实现思路](#一实现思路-4)
  - [二、核心数据结构](#二核心数据结构-4)
  - [三、核心算法](#三核心算法-4)
  - [四、与其他模块的联系](#四与其他模块的联系-4)
  - [五、关键设计点](#五关键设计点-4)
  - [六、总结](#六总结-4)
- [数据库系统详解](#数据库系统详解)
  - [一、实现思路](#一实现思路-5)
  - [二、核心数据结构](#二核心数据结构-5)
  - [三、核心算法](#三核心算法-5)
  - [四、与其他模块的联系](#四与其他模块的联系-5)
  - [五、总结](#五总结-5)
- [资源系统详解](#资源系统详解)
  - [一、实现思路](#一实现思路-6)
  - [二、核心数据结构](#二核心数据结构-6)
  - [三、核心算法](#三核心算法-6)
  - [四、与其他模块的联系](#四与其他模块的联系-6)
  - [五、总结](#五总结-6)

---

## 对象池系统详解

### 一、实现思路

#### 1.1 什么是对象池？
对象池是一种设计模式，核心思想是：
- **预先创建**一定数量的游戏对象
- **重复使用**这些对象，而不是频繁地创建和销毁
- 减少**GC（垃圾回收）压力**，提升性能

#### 1.2 为什么要用对象池？
在游戏中，像**敌人**、**资源**这类对象会频繁出现和消失：
- 如果每次都用 `Instantiate()` 和 `Destroy()`，会造成：
  - 频繁的GC，导致卡顿
  - 内存碎片
  - 性能下降

---

### 二、核心数据结构

#### 2.1 配置数据结构

```csharp
// AutoObjectPoolConfigItem - 单个对象池的配置
public class AutoObjectPoolConfigItem
{
    public string poolId;           // 对象池ID（如"SmallEnemy"）
    public string objectType;       // 对象类型（Enemy/Resource/UI）
    public string addressablePath;  // Addressable资源路径
    public int initialCapacity;    // 初始容量
    public int updateThreshold;    // 更新阈值
    public int maxCapacity;        // 最大容量
}
```

#### 2.2 对象池内部数据结构

```csharp
// AutoObjectPool - 单个对象池
public class AutoObjectPool
{
    // 可用对象队列 - 存放待使用的对象
    private Queue<GameObject> availableObjects = new Queue<GameObject>();
    
    // 活跃对象列表 - 存放正在使用的对象
    private List<GameObject> activeObjects = new List<GameObject>();
    
    // 预制体引用
    private GameObject prefab;
}
```

**为什么用 Queue（队列）存可用对象？**
- 队列是**先进先出（FIFO）**的数据结构
- 保证对象的公平使用，避免某些对象被频繁使用

#### 2.3 管理器的数据结构

```csharp
// AutoObjectPoolManager - 总管理器
public class AutoObjectPoolManager : NetworkBehaviour
{
    // 所有对象池的字典 - key是poolId，value是对象池
    private Dictionary<string, AutoObjectPool> pools = new Dictionary<string, AutoObjectPool>();
    
    // 资源加载句柄字典
    private Dictionary<string, AsyncOperationHandle<GameObject>> assetHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
    
    // 活跃和非活跃对象的根节点
    public GameObject ActiveRoot { get; private set; }
    public GameObject InactiveRoot { get; private set; }
}
```

---

### 三、核心算法

#### 3.1 对象获取算法（GetObject）

```
获取对象流程：
1. 检查是否有可用对象？
   ├─ 是 → 从队列取出，激活，加入活跃列表
   └─ 否 → 检查是否达到最大容量？
             ├─ 是 → 返回null（或动态扩容）
             └─ 否 → Instantiate新对象，加入活跃列表
2. 设置位置和旋转
3. 网络同步（如果有NetworkIdentity）
4. 返回对象
```

**代码位置：** `AutoObjectPoolManager.cs:510-619`

#### 3.2 对象归还算法（ReturnObject）

```
归还对象流程：
1. 通知所有客户端隐藏对象（ClientRpc）
2. 隐藏对象（SetActive(false)）
3. 从活跃列表移除
4. 加入可用队列
5. 移动到InactiveRoot下
```

**代码位置：** `AutoObjectPoolManager.cs:762-862`

#### 3.3 容量检查算法（CheckCapacity）

```
定时检查（每30秒）：
1. 可用对象 < 更新阈值？
   └─ 是 → 生成新对象补充
2. 可用对象 > 最大容量？
   └─ 是 → 销毁多余对象
```

---

### 四、与其他模块的联系

#### 4.1 与敌人系统的联系

看 `EnemySpawner.cs:38`：
```csharp
GameObject enemy = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);
```

**联系点：**
1. **EnemySpawner** 调用对象池获取敌人
2. 对象池负责：
   - 从队列取敌人预制体
   - 设置位置
   - 网络同步
3. 敌人死亡时，调用 `ReturnObject` 归还到对象池

#### 4.2 与资源系统的联系

看 `ResourceSpawner.cs:217`：
```csharp
GameObject resource = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);
```

**联系点：**
1. **ResourceSpawner** 从对象池获取资源
2. 对象池会**自动生成随机位置**（`GenerateRandomResourcePosition`）
3. 玩家拾取后，归还到对象池

#### 4.3 与ResourceDatabase的联系

看 `AutoObjectPoolManager.cs:375-471`：
```csharp
// 自动从ResourceDatabase同步资源类型
private void SyncResourceTypesFromDatabase()
```

**联系点：**
- 对象池管理器会**自动扫描ResourceDatabase中的资源类型**
- 自动为新资源类型创建对象池配置
- 无需手动配置JSON

#### 4.4 与网络系统的联系

看 `AutoObjectPoolManager.cs:551-607`：
```csharp
// 网络同步关键代码
NetworkIdentity netIdentity = obj.GetComponent<NetworkIdentity>();
if (netIdentity != null)
{
    NetworkServer.Spawn(obj);
    RpcSetObjectParent(netIdentity.netId);
}
```

**联系点：**
1. 对象池管理器继承自 `NetworkBehaviour`
2. 只在**服务器端**运行（`[Server]`属性）
3. 使用 `ClientRpc` 通知所有客户端同步对象状态

---

### 五、配置文件示例

看 `AutoObjectPoolConfig.json` 配置：
```json
{
  "poolId": "SmallEnemy",
  "objectType": "Enemy",
  "addressablePath": "SmallEnemy",
  "initialCapacity": 30,    // 预生成30个
  "updateThreshold": 10,      // 少于10个时补充
  "maxCapacity": 40          // 最多40个
}
```

---

### 六、关键设计点

#### 6.1 单例模式
- `AutoObjectPoolManager.Instance` 全局唯一实例

#### 6.2 根节点管理
- `ActiveRoot` - 所有活跃对象挂在这
- `InactiveRoot` - 所有非活跃对象挂在这
- 便于管理和查找

#### 6.3 服务器权威
- 所有对象池操作只在服务器执行
- 客户端只负责显示

---

### 七、总结

对象池系统是整个项目的**基础核心系统**，它：
1. 为敌人系统和资源系统提供了高效的对象管理
2. 与网络系统紧密配合实现多人同步
3. 从ResourceDatabase自动同步配置
4. 大幅减少GC，提升游戏性能

---

## 背包系统详解

---

### 一、实现思路

#### 1.1 背包系统的核心功能
背包系统是玩家管理物品的核心模块，主要功能包括：
- **物品存储**：存放资源、武器、消耗品等
- **物品堆叠**：相同物品可以堆叠到一起
- **物品交换**：拖拽交换物品位置
- **物品消耗**：右键使用食物恢复血量
- **网络同步**：多人游戏中背包数据实时同步

#### 1.2 服务器权威设计
背包数据非常重要，所以采用**服务器权威模式**：
- 所有背包修改都在**服务器**上执行
- 客户端只负责显示和发送请求
- 使用 `SyncList` 自动同步数据到所有客户端

---

### 二、核心数据结构

#### 2.1 物品数据结构

```csharp
// InventoryItem - 单个格子的物品数据
// ⚠️ 重要：必须是 struct（结构体）而不是 class（类）
// Mirror SyncList 只能正确同步值类型（struct）
// 如果用 class，修改元素字段时 SyncList 无法检测到变化
[System.Serializable]
public struct InventoryItem
{
    public string resourceId;  // 物品ID（如"WGS"表示阔刀）
    public int amount;         // 物品数量
    
    // 提供一个静态的空实例，用于初始化空槽位
    public static InventoryItem Empty => new InventoryItem { resourceId = null, amount = 0 };
    
    // 检查槽位是否为空
    public bool IsEmpty => string.IsNullOrEmpty(resourceId) || amount <= 0;
}
```

**为什么 InventoryItem 必须是 struct？**
- struct 是值类型，修改 `slots[i] = slot` 会替换整个元素
- SyncList 能检测到元素替换，触发增量同步
- 如果用 class（引用类型），修改字段不会改变引用，SyncList 无法检测变化
- 这是 Mirror SyncList 的核心限制

#### 2.2 背包主数据结构

看 `Inventory.cs:19-30`：
```csharp
public class Inventory : NetworkBehaviour
{
    public int capacity = 20;                    // 背包容量（20个格子）
    public int maxStackPerSlot = 40;            // 每个格子最多堆叠40个

    // 使用 SyncList 实现增量同步（优化方案）
    // ⚠️ 重要：InventoryItem 必须是 struct 才能被正确同步
    public readonly SyncList<InventoryItem> slots = new SyncList<InventoryItem>();
    
    // 本地数据变化事件（用于主机模式下的立即刷新）
    public event System.Action OnDataChanged;
    
    // 标记是否已初始化
    private bool isInitialized = false;
}
```

**关键设计点：SyncList + struct + Callback + 本地事件**
- `slots` 使用 `SyncList<InventoryItem>` 而非 `SyncVar` 数组
- **InventoryItem 必须是 struct**：Mirror SyncList 只能正确同步值类型
- **增量同步**：SyncList会自动检测元素变化，只同步变化的部分
- **零GC压力**：无需克隆数组，减少内存分配
- **本地数据变化事件**：解决主机模式下 SyncList.Callback 不触发的问题

**为什么不用SyncVar数组？**
- SyncVar检测的是引用变化，不检测数组内容变化
- 如果用数组，每次修改都要 `slots = slots.Clone()` 才能触发同步
- 这会造成大量GC压力，性能较差

#### 2.3 UI控制器数据结构

看 `InventoryUIController.cs:11-43`：
```csharp
public class InventoryUIController : MonoBehaviour
{
    public Inventory targetInventory;           // 指向玩家的背包
    public GameObject inventoryPanel;            // 背包主面板
    public GameObject previewPanel;              // 预览面板（显示前5个格子）
    public KeyCode toggleKey = KeyCode.Tab;      // 切换背包的按键

    private GameObject[] slotPanels;             // 所有格子UI
    private GameObject[] previewSlotPanels;      // 预览格子UI
    bool isVisible;
    private bool hasSubscribedToPlayerEvent = false;
    private bool isToggleBlocked = false;        // 是否阻止切换背包
    
    // 资源Sprite缓存（避免重复加载）
    private Dictionary<string, AsyncOperationHandle<Sprite>> spriteHandles = new Dictionary<string, AsyncOperationHandle<Sprite>>();
    
    // 滚轮选择相关
    public int currentSelectedIndex = 0;         // 当前选中的预览槽索引 (0-4)
    public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);  // 选中状态的颜色
    public Color normalColor = Color.white;      // 正常状态的颜色
    public float selectedScale = 1.15f;          // 选中状态的缩放
    public float normalScale = 1f;               // 正常状态的缩放
    public float transitionDuration = 0.1f;      // 选择切换动画持续时间
    
    // 选择变更事件
    public System.Action<int> OnPreviewSelectionChanged;
}
```

---

### 三、核心算法

#### 3.1 背包初始化算法

看 `Inventory.cs:32-79`：
```
初始化流程：
1. Awake：
   - 不初始化slots，因为此时网络状态可能还未初始化
2. OnStartServer（服务器端）：
   - 调用 InitializeSlots() 初始化slots
   - 创建 capacity 个空槽位
3. OnStartClient（客户端）：
   - 不需要初始化，等待服务器同步
```

**关键代码：**
```csharp
void Awake()
{
    // 注意：不要在 Awake 中初始化 slots！
    // 因为此时 NetworkBehaviour 的网络状态（isServer/isClient）可能还未初始化
    Debug.Log($"[Inventory] Awake被调用 - slots.Count={(slots != null ? slots.Count : 0)}, capacity={capacity}");
}

public override void OnStartServer()
{
    base.OnStartServer();
    // 服务器端初始化 slots（这是正确的时机，此时网络状态已初始化）
    InitializeSlots();
}

[Server]
void InitializeSlots()
{
    if (isInitialized) return;
    
    slots.Clear();
    for (int i = 0; i < capacity; i++)
    {
        slots.Add(InventoryItem.Empty);  // 使用 Empty 静态属性创建空槽位
    }
    isInitialized = true;
}
```

#### 3.2 添加物品算法（Add）

看 `Inventory.cs:89-145`：
```
添加物品流程：
1. 先调用 CanAdd 检查是否能放下
2. 遍历所有格子：
   ├─ 找到同类物品且没满的格子 → 先填满
   └─ 找到空格子 → 放新物品
3. 直接修改slots元素（SyncList自动检测变化）
4. 通知本地数据变化事件
5. 返回是否成功
```

**关键代码：**
```csharp
[Server]  // 只在服务器执行
public bool Add(string resourceId, int amount)
{
    if (!CanAdd(resourceId, amount))
    {
        Debug.LogWarning($"[Inventory] Add失败：无法添加资源 - 资源ID: {resourceId}, 数量: {amount}");
        return false;
    }

    int remaining = amount;
    for (int i = 0; i < slots.Count; i++)
    {
        var slot = slots[i];
        if (string.IsNullOrEmpty(slot.resourceId) || slot.resourceId == resourceId)
        {
            int current = slot.amount;
            int space = maxStackPerSlot - current;
            int toAdd = Mathf.Min(space, remaining);
            slot.resourceId = resourceId;
            slot.amount = current + toAdd;
            slots[i] = slot;  // SyncList自动检测变化
            remaining -= toAdd;
            if (remaining <= 0) break;
        }
    }
    
    // 通知本地玩家UI刷新（解决主机模式下SyncList.Callback不触发的问题）
    NotifyDataChanged();
    return true;
}
```

**优化亮点：**
- SyncList会自动检测 `slots[i] = slot` 的变化
- 直接修改元素即可触发增量同步
- 不需要 `slots = slots.Clone()` 这种昂贵的操作
- 主机模式下通过 `OnDataChanged` 事件确保UI刷新

#### 3.3 消耗物品算法（Consume）

看 `Inventory.cs:147-191`：
```
消耗物品流程：
1. 调用 HasEnough 检查是否足够
2. 遍历格子，找到对应物品
3. 从后往前扣除数量
4. 如果数量减到0，清空 resourceId
5. SyncList自动检测变化并同步
6. 通知本地数据变化事件
```

**关键代码：**
```csharp
[Server]
public bool Consume(string resourceId, int amount)
{
    if (!HasEnough(resourceId, amount))
        return false;

    int remaining = amount;
    for (int i = 0; i < slots.Count; i++)
    {
        var slot = slots[i];
        if (slot.resourceId == resourceId && slot.amount > 0)
        {
            int toRemove = Mathf.Min(slot.amount, remaining);
            slot.amount -= toRemove;
            if (slot.amount <= 0)
            {
                slot.resourceId = null;
                slot.amount = 0;
            }
            slots[i] = slot;
            remaining -= toRemove;
            if (remaining <= 0) break;
        }
    }
    
    // 通知本地玩家UI刷新
    NotifyDataChanged();
    return true;
}
```

#### 3.4 拖拽交换算法（CmdSwapSlots）

看 `Inventory.cs:193-239`：
```
交换格子流程（客户端→服务器）：
1. 客户端发送 CmdSwapSlots 命令到服务器
2. 服务器验证索引有效
3. 检查源格子是否有资源
4. 检查目标格子：
   ├─ 目标为空 → 直接移动资源
   └─ 目标有物品 → 交换两个格子
5. 直接修改slots元素（SyncList自动增量同步）
6. 通知本地数据变化事件
```

**关键代码：**
```csharp
[Command(requiresAuthority = false)]
public void CmdSwapSlots(int slotIndex1, int slotIndex2)
{
    // 验证索引
    if (slotIndex1 < 0 || slotIndex1 >= slots.Count || 
        slotIndex2 < 0 || slotIndex2 >= slots.Count)
        return;

    if (slotIndex1 == slotIndex2) return;

    InventoryItem sourceSlot = slots[slotIndex1];
    InventoryItem targetSlot = slots[slotIndex2];

    // 检查源格子是否有资源
    bool sourceHasItem = !string.IsNullOrEmpty(sourceSlot.resourceId) && sourceSlot.amount > 0;
    // 检查目标格子是否为空
    bool targetIsEmpty = string.IsNullOrEmpty(targetSlot.resourceId) || targetSlot.amount <= 0;

    if (!sourceHasItem) return;

    if (targetIsEmpty)
    {
        // 目标格子为空，直接移动资源
        slots[slotIndex2] = sourceSlot;
        slots[slotIndex1] = InventoryItem.Empty;
    }
    else
    {
        // 目标格子有资源，交换两个格子
        InventoryItem temp = slots[slotIndex1];
        slots[slotIndex1] = slots[slotIndex2];
        slots[slotIndex2] = temp;
    }
    
    // 通知本地玩家UI刷新
    NotifyDataChanged();
}
```

#### 3.5 消耗食物恢复血量算法（CmdConsumeFood）

看 `Inventory.cs:241-317`：
```
右键吃食物流程：
1. 客户端发送 CmdConsumeFood 命令
2. 服务器验证：
   ├─ 格子索引有效吗？
   ├─ 格子有物品吗？
   ├─ 是食物资源吗？（检查 ResourceDatabase）
   ├─ 血量没满吗？
3. 扣除1个食物
4. 调用 CharacterStats.Heal() 恢复血量
5. 通知本地数据变化事件
```

**关键代码：**
```csharp
[Command(requiresAuthority = true)]
public void CmdConsumeFood(int slotIndex)
{
    // 验证格子索引
    if (slotIndex < 0 || slotIndex >= slots.Count) return;

    var slot = slots[slotIndex];
    
    // 检查格子是否有物品
    if (string.IsNullOrEmpty(slot.resourceId) || slot.amount <= 0) return;

    // 从资源数据库获取资源数据
    ResourceData resourceData = ResourceDatabase.Instance.GetResource(slot.resourceId);
    if (resourceData == null) return;

    // 验证是否为食物资源
    if (resourceData.type != "食物资源") return;

    // 验证是否有血量可以恢复
    CharacterStats stats = GetComponentInChildren<CharacterStats>();
    if (stats == null) return;

    // 如果血量已满，不消耗食物
    if (stats.currentHealth >= stats.maxHealth) return;

    // 消耗1个食物
    slot.amount -= 1;
    if (slot.amount <= 0)
    {
        slot.resourceId = null;
        slot.amount = 0;
    }
    slots[slotIndex] = slot;

    // 恢复血量
    int healAmount = resourceData.healthRestore;
    stats.Heal(healAmount);
    
    // 通知本地玩家UI刷新
    NotifyDataChanged();
}
```

---

### 四、与其他模块的联系

#### 4.1 与 NetworkPlayer 的联系

看 `NetworkPlayer.cs` 中的相关代码：
```csharp
// OnStartServer 中
inventory = GetComponent<Inventory>();
if (inventory == null)
{
    inventory = gameObject.AddComponent<Inventory>();
}

// 初始化背包武器
void InitializeInventoryWeapons()
{
    // 注意：现在使用 SyncList 直接修改，无需克隆数组
    inventory.slots[1] = new InventoryItem { resourceId = "WGS", amount = 1 };  // 阔刀
    inventory.slots[2] = new InventoryItem { resourceId = "WTD", amount = 1 };  // 双刀
    inventory.slots[3] = new InventoryItem { resourceId = "Axe", amount = 1 };  // 斧头
}
```

**联系点：**
1. **NetworkPlayer** 拥有并管理 **Inventory** 组件
2. 玩家进入游戏时，服务器自动初始化背包（放3把武器）
3. 通过 `GetInventory()` 方法获取背包引用
4. 使用 SyncList 直接修改元素，无需数组克隆

#### 4.2 与 ResourceDatabase 的联系

看 `Inventory.cs:265-276` 和 `InventoryUIController.cs:660-713`：
```csharp
// Inventory 中验证食物
ResourceData resourceData = ResourceDatabase.Instance.GetResource(slot.resourceId);
if (resourceData == null) return;
if (resourceData.type != "食物资源") return;

// InventoryUIController 中获取 Sprite
string GetSpriteAddressableKey(string resourceId)
{
    // 先查 WeaponDatabase
    if (WeaponDatabase.Instance != null)
    {
        WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(resourceId);
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.spriteAddressableKey))
            return weaponData.spriteAddressableKey;
    }
    
    // 再查 ResourceDatabase
    if (ResourceDatabase.Instance != null)
    {
        ResourceData resourceData = ResourceDatabase.Instance.GetResource(resourceId);
        if (resourceData != null && !string.IsNullOrEmpty(resourceData.spriteAddressableKey))
            return resourceData.spriteAddressableKey;
    }
    
    return null;
}
```

**联系点：**
1. **Inventory** 从 **ResourceDatabase** 读取：
   - 物品类型（判断是不是食物）
   - 恢复血量值
2. **InventoryUIController** 从数据库读取 Sprite 地址显示图标
3. 优先从 WeaponDatabase 查找武器数据

#### 4.3 与 CharacterStats 的联系

看 `Inventory.cs:286-311`：
```csharp
// 获取 CharacterStats 组件
CharacterStats stats = GetComponentInChildren<CharacterStats>();
if (stats == null) return;

// 如果血量已满，不消耗食物
if (stats.currentHealth >= stats.maxHealth) return;

// 恢复血量
int healAmount = resourceData.healthRestore;
stats.Heal(healAmount);
```

**联系点：**
- 吃食物时，调用 **CharacterStats.Heal()** 恢复血量
- 血量满时不消耗食物，避免浪费

#### 4.4 与武器系统的联系

看 `InventoryUIController.cs:669-688` 和 `PlayerWeaponController`：
```csharp
// 优先从 WeaponDatabase 查找武器
if (WeaponDatabase.Instance != null)
{
    WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(resourceId);
    if (weaponData != null)
    {
        Debug.Log($"[InventoryUIController] 从WeaponDatabase找到武器: {resourceId} -> {weaponData.weaponName}");
        return weaponData.spriteAddressableKey;
    }
}
```

**联系点：**
- 背包UI会优先显示武器图标（从WeaponDatabase）
- 预览面板的前5个格子用于武器快速切换
- PlayerWeaponController 监听 OnPreviewSelectionChanged 事件

#### 4.5 与网络系统的联系

看 `Inventory.cs:24, 50-57` 和 `InventoryUIController.cs:259-312`：
```csharp
// SyncList 增量同步
public readonly SyncList<InventoryItem> slots = new SyncList<InventoryItem>();

// 本地数据变化事件（解决主机模式下SyncList.Callback不触发的问题）
public event System.Action OnDataChanged;

// UI控制器中订阅事件
void OnPlayerInitialized(NetworkPlayer player)
{
    targetInventory = player.GetInventory();
    if (targetInventory != null)
    {
        // 订阅SyncList网络同步回调
        targetInventory.slots.Callback += OnInventorySlotsChanged;
        
        // 订阅本地数据变化事件（解决主机模式问题）
        targetInventory.OnDataChanged += OnLocalDataChanged;
    }
}

// SyncList变化回调
void OnInventorySlotsChanged(SyncList<InventoryItem>.Operation op, int index, 
    InventoryItem oldItem, InventoryItem newItem)
{
    Refresh();
    RefreshPreview();
}

// 本地数据变化回调（主机模式下使用）
void OnLocalDataChanged()
{
    Refresh();
    RefreshPreview();
}
```

**联系点：**
1. **Inventory** 继承自 `NetworkBehaviour`
2. 使用 `SyncList<InventoryItem>` 实现增量同步
3. 使用 `[Command]` 让客户端发送请求到服务器
4. `Callback` 机制在本地玩家上刷新UI
5. **主机模式修复**：通过 `OnDataChanged` 事件解决 SyncList.Callback 不触发的问题

**SyncList vs SyncVar数组对比：**
| 特性 | SyncVar数组 | SyncList |
|------|-------------|----------|
| 同步方式 | 全量同步 | 增量同步 |
| GC压力 | 高（每次克隆数组） | 低（无克隆） |
| 网络带宽 | 大（整个数组） | 小（只同步变化） |
| 代码复杂度 | 高（需手动触发） | 低（自动检测） |
| 主机模式支持 | 正常 | 需要额外事件处理 |

---

### 五、UI交互流程

#### 5.1 背包显示/隐藏流程

看 `InventoryUIController.cs:144-153, 314-327`：
```
显示/隐藏流程：
1. 检测 Tab 键按下（可配置 toggleKey）
2. 调用 ToggleInventory() 切换显示状态
3. 显示时调用 Refresh() 刷新UI
4. 可通过 SetToggleBlocked() 阻止切换（如对话框打开时）
```

**关键代码：**
```csharp
void Update()
{
    if (Input.GetKeyDown(toggleKey) && !isToggleBlocked)
    {
        ToggleInventory();
    }
}

public void ToggleInventory()
{
    isVisible = !isVisible;
    if (inventoryPanel != null)
        inventoryPanel.SetActive(isVisible);
    if (isVisible)
        Refresh();
}

// 设置是否阻止背包切换
public void SetToggleBlocked(bool blocked)
{
    isToggleBlocked = blocked;
}
```

#### 5.2 滚轮选择流程

看 `InventoryUIController.cs:27-43, 186-201, 206-257`：
```
滚轮选择流程：
1. PlayerWeaponController 检测滚轮输入
2. 计算新的选中索引（循环 0-4）
3. 调用 SetSelectedSlot(index) 更新选中状态
4. 更新UI高亮显示（颜色+缩放动画）
5. 触发 OnPreviewSelectionChanged 事件
6. PlayerWeaponController 监听事件并切换武器
```

**关键代码：**
```csharp
public void SetSelectedSlot(int index)
{
    if (index < 0 || index >= 5) return;
    
    currentSelectedIndex = index;
    UpdateSlotHighlight();
    OnPreviewSelectionChanged?.Invoke(index);
}

void UpdateSlotHighlight()
{
    for (int i = 0; i < previewSlotPanels.Length && i < 5; i++)
    {
        Image bgImage = previewSlotPanels[i].transform.GetComponent<Image>();
        bool isSelected = (i == currentSelectedIndex);
        
        // 设置颜色
        if (bgImage != null)
            bgImage.color = isSelected ? selectedColor : normalColor;
        
        // 设置缩放动画
        StartCoroutine(AnimateSlotScale(panelTransform, 
            isSelected ? selectedScale : normalScale));
    }
}
```

#### 5.3 拖拽交换流程

看 `InventorySlotUI.cs:33-121`：
```
玩家拖拽流程：
1. OnBeginDrag：
   ├─ 检查格子有物品吗？
   ├─ 检查 inventoryUIController 和 targetInventory 是否有效
   ├─ 创建拖拽图标（DragIcon）并设置透明度
   └─ 跟随鼠标移动
2. OnDrag：更新拖拽图标位置
3. OnEndDrag：销毁拖拽图标
4. OnDrop（目标格子）：
   ├─ 获取源格子
   ├─ 验证索引有效性
   ├─ 调用 CmdSwapSlots
   └─ 服务器处理交换
```

**关键代码：**
```csharp
public void OnBeginDrag(PointerEventData eventData)
{
    // 验证
    if (inventoryUIController == null || inventoryUIController.targetInventory == null)
        return;
    
    var slotData = inventoryUIController.targetInventory.slots[slotIndex];
    if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
        return;
    
    // 创建拖拽图标
    dragIcon = new GameObject("DragIcon");
    dragIcon.transform.SetParent(canvas.transform, false);
    dragIcon.transform.SetAsLastSibling();
    
    dragIconImage = dragIcon.AddComponent<Image>();
    dragIconImage.sprite = iconImage.sprite;
    dragIconImage.raycastTarget = false;  // 不阻挡射线检测
    
    // 设置半透明
    Color iconColor = dragIconImage.color;
    iconColor.a = 0.7f;
    dragIconImage.color = iconColor;
}

public void OnDrop(PointerEventData eventData)
{
    InventorySlotUI sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
    if (sourceSlot == null || sourceSlot == this) return;
    
    // 调用服务器命令
    inventoryUIController.targetInventory.CmdSwapSlots(sourceSlot.slotIndex, slotIndex);
}
```

#### 5.4 右键吃食物流程

看 `InventorySlotUI` 和 `Inventory.CmdConsumeFood`：
```
1. 玩家右键点击食物格子
2. InventorySlotUI 检测右键点击
3. 调用 inventory.CmdConsumeFood(slotIndex)
4. 服务器验证并处理：
   ├─ 格子有物品吗？
   ├─ 是食物资源吗？
   ├─ 血量没满吗？
   ├─ 扣除1个食物
   └─ 恢复血量
5. SyncList 自动同步到所有客户端
6. UI 通过 Callback 刷新显示
```

---

### 六、关键设计点

#### 6.1 SyncList + Callback + 本地事件 模式
- 服务器修改数据 → SyncList增量同步 → Callback刷新UI
- **主机模式修复**：通过 `OnDataChanged` 事件解决主机模式下 SyncList.Callback 不触发的问题
- 完美的MVC分离：数据（Inventory）、视图（UI）分离
- **增量同步**：只同步变化的元素，减少网络带宽
- **零GC压力**：无需数组克隆操作

**为什么需要本地数据变化事件？**
- 在主机模式（Host Mode）下，服务器和客户端在同一进程中运行
- SyncList.Callback 在主机模式下有时不会触发（Mirror的已知问题）
- 通过 `OnDataChanged` 事件，服务器修改数据后立即通知本地UI刷新
- 确保主机模式下UI能够及时更新

#### 6.2 服务器权威
- 所有修改方法都加 `[Server]` 或 `[Command]`
- 客户端只负责显示和发送请求，不能直接改数据
- 保证数据一致性和安全性

#### 6.3 资源缓存与内存管理
- `InventoryUIController` 用 `Dictionary` 缓存Sprite句柄
- 避免重复从Addressables加载，提升性能
- `OnDestroy` 时释放所有缓存的资源句柄，防止内存泄漏
- `CleanupUnusedSpriteHandles` 清理不再使用的资源

#### 6.4 SyncList自动检测变化
- 直接修改 `slots[i] = slot` 即可触发同步
- 不需要 `slots = slots.Clone()` 这种昂贵的操作
- Mirror自动检测元素变化并增量同步
- **注意**：必须使用 struct 才能实现自动检测

#### 6.5 背包初始化时机
- **不要在 Awake 中初始化 slots**！
- 因为此时 NetworkBehaviour 的网络状态（isServer/isClient）可能还未初始化
- 正确的时机：
  - 服务器端：`OnStartServer()` 中调用 `InitializeSlots()`
  - 客户端：等待服务器同步，不需要主动初始化

#### 6.6 UI事件订阅管理
- 使用 `hasSubscribedToPlayerEvent` 标记避免重复订阅
- `OnDestroy` 中取消订阅所有事件，防止内存泄漏
- 订阅的事件包括：
  - `NetworkPlayer.OnPlayerInitialized`
  - `targetInventory.slots.Callback`
  - `targetInventory.OnDataChanged`

#### 6.7 滚轮选择与武器切换解耦
- InventoryUIController 只负责UI显示和选中状态
- 滚轮输入检测由 PlayerWeaponController 处理
- 通过 `OnPreviewSelectionChanged` 事件通知武器切换
- 避免两个系统之间的直接依赖

---

### 七、常见问题与解决方案

#### 7.1 主机模式下UI不刷新
**问题**：在主机模式下，修改背包数据后UI没有立即更新。

**原因**：Mirror的SyncList在主机模式下有时不会触发Callback。

**解决方案**：
```csharp
// 添加本地数据变化事件
public event System.Action OnDataChanged;

// 服务器修改数据后，通知本地UI
void NotifyDataChanged()
{
    OnDataChanged?.Invoke();
}

// UI控制器订阅本地事件
void OnPlayerInitialized(NetworkPlayer player)
{
    targetInventory.OnDataChanged += OnLocalDataChanged;
}
```

#### 7.2 背包初始化时机错误
**问题**：背包slots为空或网络同步异常。

**原因**：在 Awake 中初始化slots，此时网络状态还未准备好。

**解决方案**：
```csharp
// 错误的：在 Awake 中初始化
void Awake()
{
    // 不要在这里初始化！
}

// 正确的：在 OnStartServer 中初始化
public override void OnStartServer()
{
    base.OnStartServer();
    InitializeSlots();  // 此时网络状态已初始化
}
```

#### 7.3 资源图片加载失败
**问题**：背包格子不显示物品图标。

**原因**：
1. WeaponDatabase 或 ResourceDatabase 未初始化
2. Sprite 的 AddressableKey 配置错误
3. 资源未被打包到Addressables

**解决方案**：
```csharp
// 添加空值检查
if (WeaponDatabase.Instance == null)
{
    Debug.LogWarning("[InventoryUIController] WeaponDatabase.Instance为null");
    return null;
}

// 优先从WeaponDatabase查找，再从ResourceDatabase查找
string GetSpriteAddressableKey(string resourceId)
{
    if (WeaponDatabase.Instance != null)
    {
        WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(resourceId);
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.spriteAddressableKey))
            return weaponData.spriteAddressableKey;
    }
    
    if (ResourceDatabase.Instance != null)
    {
        ResourceData resourceData = ResourceDatabase.Instance.GetResource(resourceId);
        if (resourceData != null && !string.IsNullOrEmpty(resourceData.spriteAddressableKey))
            return resourceData.spriteAddressableKey;
    }
    
    return null;
}
```

---

### 八、总结

背包系统是玩家交互的核心模块，它：
1. **与NetworkPlayer紧密集成**，每个玩家有自己的背包
2. **与ResourceDatabase和WeaponDatabase协作**，读取物品数据
3. **与CharacterStats协作**，实现吃食物回血
4. **使用SyncList+Callback+本地事件实现增量同步和UI刷新**
5. **采用服务器权威模式**，保证数据安全
6. **性能优化**：
   - 增量同步减少网络带宽
   - 零GC压力（无需数组克隆）
   - Sprite缓存避免重复加载
7. **主机模式支持**：通过本地事件解决SyncList.Callback不触发的问题
8. **完善的内存管理**：事件取消订阅、资源句柄释放

---

## 敌人系统详解

---

### 一、实现思路

#### 1.1 敌人系统的核心功能
敌人系统是游戏中的主要敌对单位，主要功能包括：
- **敌人生成**：从对象池获取敌人，随机位置生成
- **AI状态机**：巡逻→追击→攻击→死亡四种状态
- **NavMesh寻路**：使用Unity的NavMesh实现自动寻路
- **血量管理**：独立的血量系统，支持血条显示
- **对象池回收**：敌人死亡后归还到对象池

#### 1.2 服务器权威设计
敌人AI和血量管理都在**服务器端**执行：
- 只有服务器能修改敌人状态和血量
- 客户端只负责显示动画和接收状态同步
- 使用 `SyncVar` 同步敌人状态和血量

---

### 二、核心数据结构

#### 2.1 敌人AI状态枚举

看 `EnemyAIController.cs:36-43`：
```csharp
enum State
{
    Idle,      // 待机
    Patrol,    // 巡逻
    Chase,     // 追击
    Attack,    // 攻击
    Dead       // 死亡
}

enum AnimationState
{
    Idle,
    Patrol,
    Chase,
    Attack
}
```

#### 2.2 敌人AI控制器数据结构

看 `EnemyAIController.cs:8-58`：
```csharp
public class EnemyAIController : NetworkBehaviour
{
    public string enemyType;              // 敌人类型
    public Transform target;               // 追击目标
    public float detectionRadius = 10f;   // 检测半径
    public float attackDistance = 3f;     // 攻击距离
    public float attackCooldown = 3f;     // 攻击冷却
    
    private EnemyData data;                // 敌人数据（从数据库读取）
    private NavMeshAgent agent;            // 寻路代理
    private Animator animator;              // 动画器
    private EnemyHealthManager healthManager;  // 血量管理器
    
    // 同步变量
    [SyncVar(hook = nameof(OnStateChanged))]
    private State syncState = State.Patrol;
    
    [SyncVar(hook = nameof(OnAnimationStateChanged))]
    private AnimationState syncAnimationState;
    
    private Vector3[] patrolPoints;       // 巡逻点数组
    private int currentPatrolIndex;       // 当前巡逻点索引
}
```

#### 2.3 敌人血量管理器数据结构

看 `EnemyHealthManager.cs:8-25`：
```csharp
public class EnemyHealthManager : NetworkBehaviour, IDamageable
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;
    
    [SyncVar]
    public int maxHealth;
    
    [SyncVar]
    public string enemyType;
    
    // 血条UI
    public GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private bool healthBarCreated = false;
    
    private EnemyAIController enemyController;
    private EnemyData enemyData;
}
```

---

### 三、核心算法

#### 3.1 敌人巡逻算法

看 `EnemyAIController.cs:357-389`：
```
巡逻流程：
1. 检查是否在等待？
   └─ 是 → 减少等待时间，继续等待
2. 设置目标巡逻点
3. 启动NavMesh寻路
4. 检查是否到达？
   ├─ 是 → 随机选择下一个巡逻点（不重复）
   └─ 否 → 继续移动
```

**关键代码：**
```csharp
void UpdatePatrol()
{
    if (patrolWaitTimer > 0f)
    {
        patrolWaitTimer -= Time.deltaTime;
        return;
    }

    agent.SetDestination(patrolPoints[currentPatrolIndex]);
    
    if (!agent.pathPending && agent.remainingDistance <= 0.5f)
    {
        // 随机选择下一个巡逻点，避免重复
        int nextIndex;
        do
        {
            nextIndex = Random.Range(0, patrolPoints.Length);
        } while (nextIndex == currentPatrolIndex && patrolPoints.Length > 1);
        
        currentPatrolIndex = nextIndex;
        patrolWaitTimer = patrolWaitTime;
    }
}
```

#### 3.2 巡逻点生成算法

看 `EnemyAIController.cs:278-322`：
```
巡逻点生成流程：
1. 将360度均匀分成N个扇区
2. 在每个扇区内随机生成点
3. 检查点是否在营地区域内？
   ├─ 是 → 重新生成
   └─ 否 → 保留该点
4. 生成完成
```

**关键设计点：**
- 扇区分区保证分布均匀
- 避免在营地区域内生成敌人

#### 3.3 敌人追击算法

看 `EnemyAIController.cs:391-425`：
```
追击流程：
1. 检查目标是否存在？
   └─ 否 → 转回巡逻状态
2. 面向目标
3. 计算距离
4. 距离 > 攻击距离？
   ├─ 是 → 继续追击
   └─ 否 → 停止，检查冷却时间
5. 冷却时间到？
   ├─ 是 → 发起攻击
   └─ 否 → 等待
```

#### 3.4 敌人攻击伤害判定算法

看 `EnemyAIController.cs:510-541`：
```
攻击判定流程：
1. 面向目标
2. OverlapSphere检测范围内的碰撞体
3. 遍历检测到的碰撞体：
   ├─ 检查标签是否是Player？
   ├─ 检查是否在攻击角度内？
   └─ 获取CharacterStats并应用伤害
```

**关键代码：**
```csharp
[Server]
public void OnAttackHit()
{
    Vector3 origin = transform.position;
    Vector3 forward = transform.forward;
    
    // 检测范围内的碰撞体
    Collider[] hitsBuffer = new Collider[10];
    int hitCount = Physics.OverlapSphereNonAlloc(
        origin, 3f, hitsBuffer, LayerMask.GetMask("Player"));
    
    for (int i = 0; i < hitCount; i++)
    {
        Collider hit = hitsBuffer[i];
        if (!hit.CompareTag("Player_new")) continue;
        
        // 检查角度
        Vector3 dirToTarget = (hit.transform.position - origin).normalized;
        float angle = Vector3.Angle(forward, dirToTarget);
        if (angle > 180f) continue;
        
        // 应用伤害
        CharacterStats targetStats = hit.GetComponentInChildren<CharacterStats>();
        if (targetStats != null && data != null)
        {
            targetStats.ApplyDamage(data.attackDamage);
        }
    }
}
```

#### 3.5 敌人死亡和对象池回收算法

看 `EnemyAIController.cs:568-714`：
```
死亡流程：
1. 标记死亡状态
2. 停止NavMesh寻路
3. 触发死亡动画
4. 等待1.5秒
5. 重置对象状态
6. 归还到对象池
```

**关键代码：**
```csharp
public void Die()
{
    isDead = true;
    state = State.Dead;
    
    // 停止寻路
    agent.isStopped = true;
    agent.ResetPath();
    
    // 触发死亡动画
    animator.SetTrigger("IsDie");
    RpcPlayDeathAnimation();
    
    StartCoroutine(ReturnToPoolAfterDelay());
}

IEnumerator ReturnToPoolAfterDelay()
{
    yield return new WaitForSeconds(1.5f);
    
    // 重置状态
    ResetPoolState();
    
    // 归还到对象池
    string poolId = GetPoolIdForEnemy(enemyType);
    AutoObjectPoolManager.Instance.ReturnObject(poolId, gameObject);
}
```

---

### 四、与其他模块的联系

#### 4.1 与对象池系统的联系

看 `EnemySpawner.cs:38` 和 `EnemyAIController.cs:666`：
```csharp
// 生成敌人时从对象池获取
GameObject enemy = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);

// 死亡时归还到对象池
AutoObjectPoolManager.Instance.ReturnObject(poolId, gameObject);
```

**联系点：**
1. **EnemySpawner** 从对象池获取敌人
2. **EnemyAIController** 死亡时归还敌人
3. 对象池负责重置敌人状态

#### 4.2 与敌人数据库的联系

看 `EnemyAIController.cs:92-96` 和 `EnemyHealthManager.cs:184`：
```csharp
// 从数据库读取敌人数据
data = EnemyDatabase.GetInstance().GetEnemy(enemyType);
agent.speed = data.patrolSpeed;

// 血量管理器从数据库读取血量
enemyData = EnemyDatabase.GetInstance().GetEnemy(enemyType);
maxHealth = enemyData.health;
currentHealth = enemyData.health;
```

**联系点：**
- 敌人类型、血量、速度、攻击力等都从EnemyDatabase读取
- 支持多种敌人类型配置

#### 4.3 与角色系统的联系

看 `EnemyAIController.cs:535`：
```csharp
// 敌人攻击玩家
CharacterStats targetStats = hit.GetComponentInChildren<CharacterStats>();
targetStats.ApplyDamage(data.attackDamage);
```

**联系点：**
- 敌人通过 `CharacterStats.ApplyDamage()` 对玩家造成伤害
- 玩家通过 `WeaponDamageSystem` 对敌人造成伤害

#### 4.4 与网络系统的联系

看 `EnemyAIController.cs:28-34`：
```csharp
// SyncVar同步状态
[SyncVar(hook = nameof(OnStateChanged))]
private State syncState = State.Patrol;

// ClientRpc播放动画
[ClientRpc]
void RpcPlayAttackAnimation()
{
    animator.SetTrigger("IsAtk");
}
```

**联系点：**
- 使用 `SyncVar` 同步敌人状态
- 使用 `ClientRpc` 同步动画播放
- 所有AI逻辑只在服务器执行

---

### 五、关键设计点

#### 5.1 状态机模式
- 巡逻、追击、攻击、死亡四种状态
- 每种状态独立处理逻辑

#### 5.2 对象池重置机制
- `ResetPoolState()` 方法重置所有状态
- 死亡后1.5秒归还到对象池
- 支持对象池复用

#### 5.3 扇区式巡逻点生成
- 360度均匀分成多个扇区
- 每个扇区内随机生成点
- 保证巡逻点分布均匀

#### 5.4 服务器权威
- 所有AI决策在服务器执行
- 客户端只显示和接收同步

---

### 六、总结

敌人系统是游戏战斗的核心模块，它：
1. **与对象池系统紧密集成**，高效管理敌人生成和回收
2. **使用状态机**实现巡逻→追击→攻击→死亡的完整流程
3. **从EnemyDatabase读取配置**，支持多种敌人类型
4. **通过NavMesh实现自动寻路**
5. **采用服务器权威模式**，保证AI决策一致

---

## 武器系统详解

---

### 一、实现思路

#### 1.1 武器系统的核心功能
武器系统是玩家战斗的核心模块，主要功能包括：
- **武器切换**：滚轮快速切换背包中的武器
- **连击系统**：多段攻击，每段伤害倍率递增
- **伤害判定**：扇形/圆形范围检测，角度限制
- **武器配置**：JSON配置文件，支持多种武器
- **网络同步**：服务器权威，同步武器状态和攻击

#### 1.2 连击系统设计
连击系统的核心思想：
- 每段攻击后有一个短暂的"连击窗口"
- 在窗口期内继续攻击，进入下一段连击
- 每段连击有不同的伤害倍率
- 超过窗口期未攻击，连击重置

---

### 二、核心数据结构

#### 2.1 武器数据结构

看 `WeaponDatabase.cs:9-81`：
```csharp
public class WeaponData
{
    public int weaponId;                    // 武器ID
    public string resourceId;              // 背包资源ID
    public string weaponName;               // 武器名称
    public string weaponType;               // 武器类型
    public string[] modelAddressableKeys;  // 模型Addressable路径（支持多模型）
    public string spriteAddressableKey;     // 图标Addressable路径
    public int damage;                      // 基础伤害
    public int comboStages;                 // 连击段数
    public float attackRange;               // 攻击范围（半径）
    public float attackAngle;               // 攻击角度（扇形）
    public float[] comboDamageMultipliers;  // 每段连击的伤害倍率
    public string description;               // 描述
    
    // 获取指定连击段的伤害倍率
    public float GetComboDamageMultiplier(int comboStage)
    {
        int index = Mathf.Clamp(comboStage - 1, 0, comboDamageMultipliers.Length - 1);
        return comboDamageMultipliers[index];
    }
}
```

#### 2.2 武器伤害系统数据结构

看 `WeaponDamageSystem.cs:8-53`：
```csharp
public class WeaponDamageSystem : NetworkBehaviour
{
    // 同步变量
    [SyncVar(hook = nameof(OnWeaponIdChanged))]
    private int currentWeaponId = -1;
    
    [SyncVar]
    private int currentComboStage = 0;
    
    [SyncVar]
    private bool isInComboWindow = false;
    
    // 连击设置
    public float comboResetTime = 1.5f;        // 连击重置时间
    public float comboWindowDuration = 0.3f;   // 连击窗口持续时间
    
    // 内部数据
    private WeaponData currentWeaponData;       // 当前武器数据
    private HashSet<GameObject> hitTargets;     // 已命中目标（避免重复伤害）
    private float comboResetTimer;              // 连击重置计时器
    private float comboWindowTimer;             // 连击窗口计时器
    private bool isAttacking;                   // 是否正在攻击
    private bool hasDealtDamage;                // 是否已造成伤害
}
```

#### 2.3 玩家武器控制器数据结构

看 `PlayerWeaponController.cs:14-33`：
```csharp
public class PlayerWeaponController : NetworkBehaviour
{
    public WeaponAttachmentSystem attachmentSystem;  // 武器挂载系统
    public SceneAwareAnimatorManager animatorManager;
    public PreviewWeaponController previewWeaponController;
    
    // 同步变量
    [SyncVar(hook = nameof(OnWeaponIdChanged))]
    private int currentWeaponId = -1;
    
    [SyncVar(hook = nameof(OnSlotIndexChanged))]
    private int currentSlotIndex = 0;
    
    private int localSlotIndex = 0;           // 本地槽位索引（客户端预测）
    private const int MAX_SLOTS = 5;          // 最大槽位数
}
```

---

### 三、核心算法

#### 3.1 连击系统算法

看 `WeaponDamageSystem.cs:199-252`：
```
连击流程：
1. 开始攻击（StartAttack）：
   ├─ 连击阶段+1
   ├─ 超过最大段数则重置为1
   ├─ 重置连击重置计时器
   └─ 开启连击窗口
2. 结束攻击（EndAttack）：
   ├─ 如果不是最后一段，延迟关闭连击窗口
   └─ 如果是最后一段，立即关闭并重置连击
3. 连击窗口计时：
   ├─ 窗口内可以继续攻击
   └─ 窗口关闭后无法连击
4. 连击重置计时：
   ├─ 超过重置时间未攻击
   └─ 连击阶段重置为0
```

**关键代码：**
```csharp
public void StartAttack()
{
    isAttacking = true;
    hasDealtDamage = false;
    hitTargets.Clear();
    
    // 增加连击阶段
    currentComboStage++;
    if (currentComboStage > currentWeaponData.comboStages)
    {
        currentComboStage = 1;
    }
    
    // 重置连击重置计时器
    comboResetTimer = comboResetTime;
    
    // 开启连击窗口
    OpenComboWindow();
}

public void EndAttack()
{
    isAttacking = false;
    
    // 如果不是最后一段，延迟关闭连击窗口
    if (currentComboStage < currentWeaponData.comboStages)
    {
        Invoke(nameof(CloseComboWindow), comboWindowDuration);
    }
    else
    {
        // 最后一段，立即关闭并重置
        CloseComboWindow();
        ResetCombo();
    }
}
```

#### 3.2 伤害判定算法

看 `WeaponDamageSystem.cs:345-484`：
```
伤害判定流程：
1. 获取武器配置（范围、角度）
2. OverlapSphere检测范围内的碰撞体
3. 遍历检测到的碰撞体：
   ├─ 检查是否在范围内？
   ├─ 检查是否在攻击角度内？（360度攻击跳过此步）
   ├─ 检查是否已命中过？
   └─ 计算伤害并应用
4. 标记已造成伤害
```

**关键代码：**
```csharp
public void PerformDamageCheck(Vector3 attackCenter, float attackRadius)
{
    // 使用武器配置中的范围和角度
    float range = currentWeaponData.attackRange;
    float angle = currentWeaponData.attackAngle;
    
    Vector3 origin = transform.position + Vector3.up * 1.0f;
    Vector3 forward = transform.forward;
    
    // 检测范围内的碰撞体（只检测Enemy层）
    Collider[] hitColliders = Physics.OverlapSphere(
        origin, range, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Ignore);
    
    foreach (Collider hitCollider in hitColliders)
    {
        // 跳过自己
        if (hitCollider.gameObject == gameObject) continue;
        
        // 检查标签
        if (!hitCollider.CompareTag("Enemy")) continue;
        
        // 检查距离
        Vector3 targetCenter = hitCollider.bounds.center;
        float distanceToTarget = Vector3.Distance(origin, targetCenter);
        if (distanceToTarget > range) continue;
        
        // 检查角度（360度攻击不需要）
        if (angle < 360f)
        {
            Vector3 dirToTarget = (targetCenter - origin).normalized;
            float targetAngle = Vector3.Angle(forward, dirToTarget);
            if (targetAngle > angle * 0.5f) continue;
        }
        
        // 检查是否已命中过
        if (hitTargets.Contains(hitCollider.gameObject)) continue;
        
        // 计算伤害
        float damage = CalculateDamage();
        
        // 应用伤害
        CmdApplyDamage(targetNetId, damage);
        
        // 记录已命中
        hitTargets.Add(hitCollider.gameObject);
    }
    
    hasDealtDamage = true;
}
```

#### 3.3 伤害计算算法

看 `WeaponDamageSystem.cs:577-589`：
```
伤害计算流程：
1. 获取基础伤害
2. 获取连击倍率
3. 最终伤害 = 基础伤害 × 连击倍率
```

**关键代码：**
```csharp
float CalculateDamage()
{
    float baseDamage = currentWeaponData.damage;
    float comboMultiplier = GetComboMultiplier();
    float finalDamage = baseDamage * comboMultiplier;
    return finalDamage;
}

float GetComboMultiplier()
{
    return currentWeaponData.GetComboDamageMultiplier(currentComboStage);
}
```

#### 3.4 武器切换算法

看 `PlayerWeaponController.cs:66-126`：
```
武器切换流程（客户端→服务器）：
1. 客户端检测滚轮输入
2. 立即更新本地UI选中状态（即时反馈）
3. 发送CmdRequestWeaponSwitch到服务器
4. 服务器验证：
   ├─ 槽位合法？
   ├─ 玩家有该武器？
5. 服务器设置SyncVar
6. SyncVar自动同步到所有客户端
7. 所有客户端更新武器显示
```

**关键代码：**
```csharp
void HandleLocalInput()
{
    float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
    if (Mathf.Abs(scrollDelta) < scrollSensitivity) return;
    
    // 计算新索引
    int newIndex = localSlotIndex;
    if (scrollDelta > 0)
        newIndex = (localSlotIndex - 1 + MAX_SLOTS) % MAX_SLOTS;
    else
        newIndex = (localSlotIndex + 1) % MAX_SLOTS;
    
    if (newIndex != localSlotIndex)
    {
        // 立即更新本地UI
        localSlotIndex = newIndex;
        previewWeaponController.inventoryUI.SetSelectedSlot(newIndex);
        
        // 发送请求到服务器
        CmdRequestWeaponSwitch(newIndex);
    }
}

[Command(requiresAuthority = true)]
void CmdRequestWeaponSwitch(int slotIndex)
{
    // 服务器验证
    if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;
    
    int weaponId = GetWeaponIdFromSlot(slotIndex);
    if (weaponId > 0 && !IsWeaponValid(weaponId)) return;
    
    // 设置SyncVar，自动同步
    currentWeaponId = weaponId;
    currentSlotIndex = slotIndex;
}
```

---

### 四、与其他模块的联系

#### 4.1 与背包系统的联系

看 `PlayerWeaponController.cs:132-168`：
```csharp
// 从背包获取武器ID
int GetWeaponIdFromSlot(int slotIndex)
{
    Inventory inventory = GetComponent<Inventory>();
    var slotData = inventory.slots[slotIndex];
    
    WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(slotData.resourceId);
    return weaponData?.weaponId ?? -1;
}

// 验证玩家是否有该武器
bool IsWeaponValid(int weaponId)
{
    Inventory inventory = GetComponent<Inventory>();
    for (int i = 0; i < inventory.slots.Length; i++)
    {
        var slotData = inventory.slots[i];
        WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(slotData.resourceId);
        if (weaponData != null && weaponData.weaponId == weaponId)
            return true;
    }
    return false;
}
```

**联系点：**
- 武器存放在背包中
- 武器切换从背包读取武器ID
- 验证玩家是否真正拥有该武器

#### 4.2 与武器数据库的联系

看 `WeaponDatabase.cs:154-162` 和 `WeaponDamageSystem.cs:99-111`：
```csharp
// 从数据库获取武器数据
public WeaponData GetWeapon(int weaponId)
{
    if (weaponDict.TryGetValue(weaponId, out WeaponData weapon))
        return weapon;
    return null;
}

// 武器伤害系统从数据库加载
if (WeaponDatabase.Instance != null && WeaponDatabase.Instance.IsInitialized)
{
    currentWeaponData = WeaponDatabase.Instance.GetWeapon(newId);
}
else
{
    // 如果数据库不可用，使用默认数据
    currentWeaponData = CreateDefaultWeaponData(newId);
}
```

**联系点：**
- 武器配置从JSON文件加载到WeaponDatabase
- 伤害、范围、角度、连击倍率都从数据库读取
- 支持动态添加新武器

#### 4.3 与敌人血量系统的联系

看 `WeaponDamageSystem.cs:516-572`：
```csharp
void ApplyDamageToTarget(GameObject target, float damage)
{
    // 优先尝试EnemyHealthManager
    EnemyHealthManager enemyHealth = target.GetComponent<EnemyHealthManager>();
    if (enemyHealth != null)
    {
        enemyHealth.ApplyDamage((int)damage);
        return;
    }
    
    // 备用：IDamageable接口
    IDamageable damageable = target.GetComponent<IDamageable>();
    if (damageable != null)
    {
        damageable.TakeDamage(damage, netId);
    }
}
```

**联系点：**
- 通过 `EnemyHealthManager.ApplyDamage()` 对敌人造成伤害
- 备用方案：`IDamageable` 接口

#### 4.4 与动画系统的联系

看 `WeaponDamageSystem.cs:217-218` 和 `PlayerWeaponController.cs:415-428`：
```csharp
// 连击窗口由动画事件触发
public void OpenComboWindow() { ... }
public void CloseComboWindow() { ... }

// 更新动画参数
void UpdateAnimatorWeaponType(int weaponId)
{
    foreach (var animator in animators)
    {
        animator.SetFloat("WeaponType", weaponId);
        animator.SetInteger("WeaponType_int", weaponId);
    }
}
```

**联系点：**
- 动画事件（AE_OpenWindow、AE_CloseWindow）控制连击窗口
- 武器类型通过Animator参数控制动画播放

---

### 五、关键设计点

#### 5.1 连击窗口机制
- 动画事件触发连击窗口开启/关闭
- 窗口内可以继续连击
- 超过窗口连击重置

#### 5.2 客户端预测 + 服务器验证
- 滚轮切换时立即更新本地UI（即时反馈）
- 服务器验证后再真正切换武器
- 兼顾响应性和权威性

#### 5.3 扇形/圆形伤害判定
- `attackRange` 控制半径
- `attackAngle` 控制扇形角度
- 360度表示圆形攻击（如斧头）

#### 5.4 HashSet防重复伤害
- 单次攻击中，同一目标只受一次伤害
- 多段攻击中，每段可以命中相同目标

#### 5.5 武器数据库降级方案
- 如果WeaponDatabase不可用，使用默认武器数据
- 保证游戏仍能正常运行

---

### 六、总结

武器系统是玩家战斗的核心模块，它：
1. **与背包系统集成**，武器存放在背包中
2. **从WeaponDatabase读取配置**，支持多种武器
3. **连击系统**提供多段攻击和伤害递增
4. **扇形/圆形伤害判定**，支持不同攻击方式
5. **客户端预测+服务器验证**，兼顾响应性和权威性

---

## 角色系统详解

---

### 一、实现思路

#### 1.1 角色系统的核心功能
角色系统是玩家的核心控制模块，主要功能包括：
- **角色初始化**：生成角色模型、初始化组件
- **血量管理**：血量显示、受伤、治疗
- **移动控制**：输入处理、速度同步
- **动画同步**：跑步、攻击等动画同步
- **网络同步**：服务器权威，同步位置、速度、状态

#### 1.2 网络玩家架构
NetworkPlayer是整个玩家的核心容器：
- 持有CharacterStats、Inventory、WeaponController等组件
- 协调各个子系统的工作
- 处理网络同步和初始化

---

### 二、核心数据结构

#### 2.1 角色状态数据结构

看 `CharacterStats.cs:4-19`：
```csharp
public class CharacterStats : NetworkBehaviour
{
    [SyncVar]
    public string characterId;
    
    [SyncVar]
    public int maxHealth;
    
    [SyncVar]
    public float moveSpeed;
    
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth;
    
    public GameObject healthBarPrefab;
    private GameObject healthBarInstance;
}
```

#### 2.2 网络玩家数据结构

看 `NetworkPlayer.cs:6-42`：
```csharp
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar]
    public string selectedCharacterId;
    
    // 新方案：同步speed值（0-1），而不是布尔值
    [SyncVar(hook = nameof(OnSpeedChanged))]
    private float syncedSpeed = 0f;
    
    private CharacterStats characterStats;
    private CharacterMovementController movementController;
    private Inventory inventory;
    private List<Animator> animators;
    private SceneAwareAnimatorManager sceneAnimatorManager;
    private CharacterModelManager characterModelManager;
    
    [SyncVar(hook = nameof(OnModelLoadedChanged))]
    private bool modelLoaded = false;
}
```

---

### 三、核心算法

#### 3.1 玩家初始化算法

看 `NetworkPlayer.cs:211-271`：
```
初始化流程：
1. OnStartServer：
   ├─ 初始化NetworkTransform
   ├─ 获取CharacterStats
   ├─ 创建/获取Inventory
   └─ 初始化背包武器
2. OnStartClient：
   ├─ 初始化NetworkTransform
   ├─ 获取组件引用
   └─ 远程玩家延迟初始化动画器
3. OnStartLocalPlayer：
   ├─ 并行初始化非关键组件
   ├─ SceneAwareAnimatorManager
   ├─ PlayerInputHandler
   └─ CharacterModelManager
```

**关键代码：**
```csharp
public override void OnStartServer()
{
    InitializeNetworkTransform();
    characterStats = GetComponentInChildren<CharacterStats>();
    movementController = GetComponentInChildren<CharacterMovementController>();
    
    inventory = GetComponent<Inventory>();
    if (inventory == null)
        inventory = gameObject.AddComponent<Inventory>();
    
    InitializeInventoryWeapons();
}

public override void OnStartLocalPlayer()
{
    StartCoroutine(ParallelInitializeComponents());
}

private IEnumerator ParallelInitializeComponents()
{
    StartCoroutine(InitializeSceneAnimatorManagerAsync(...));
    StartCoroutine(InitializePlayerInputHandlerAsync(...));
    StartCoroutine(InitializeCharacterModelManagerAsync(...));
    
    // 等待关键组件初始化完成
    while ((!playerInputInitialized || !characterModelInitialized) && elapsedTime < maxWaitTime)
    {
        yield return new WaitForSeconds(checkInterval);
    }
    
    MarkAsInitialized();
}
```

#### 3.2 背包武器初始化算法

看 `NetworkPlayer.cs:111-151`：
```
背包初始化流程：
1. 检查背包容量
2. 格子2（索引1）：放阔刀（WGS）
3. 格子3（索引2）：放双刀（WTD）
4. 格子4（索引3）：放斧头（Axe）
5. 克隆数组触发SyncVar同步
```

**关键代码：**
```csharp
[Server]
void InitializeInventoryWeapons()
{
    inventory.slots[1].resourceId = "WGS";
    inventory.slots[1].amount = 1;
    
    inventory.slots[2].resourceId = "WTD";
    inventory.slots[2].amount = 1;
    
    inventory.slots[3].resourceId = "Axe";
    inventory.slots[3].amount = 1;
    
    // 克隆数组触发SyncVar
    InventoryItem[] newSlots = (InventoryItem[])inventory.slots.Clone();
    inventory.slots = newSlots;
}
```

#### 3.3 速度同步算法

看 `NetworkPlayer.cs:657-706`：
```
速度同步流程（新方案）：
1. 客户端检测输入，计算speed值（0-1）
2. 发送CmdSetSpeed到服务器
3. 服务器设置syncedSpeed（SyncVar）
4. SyncVar自动同步到所有客户端
5. 所有客户端通过Hook更新Animator的speed参数
```

**关键代码：**
```csharp
[Command]
public void CmdSetSpeed(float speed)
{
    speed = Mathf.Clamp01(speed);
    syncedSpeed = speed;
    
    if (movementController != null)
        movementController.SetSpeedMultiplier(speed);
}

private void OnSpeedChanged(float oldValue, float newValue)
{
    if (animatorsReady && animators.Count > 0)
    {
        foreach (Animator anim in animators)
        {
            anim.SetFloat("speed", newValue);
        }
    }
}
```

**为什么用连续值而不是布尔值？**
- 连续值可以实现更平滑的动画过渡
- 支持不同的移动速度（走路/跑步）
- 更灵活的动画控制

#### 3.4 角色模型加载和动画器初始化算法

看 `NetworkPlayer.cs:377-481`：
```
模型加载流程：
1. CharacterModelManager加载模型
2. 本地玩家调用CmdNotifyModelLoaded
3. 服务器设置modelLoaded = true（SyncVar）
4. 服务器通过RpcNotifyModelLoaded通知所有客户端
5. 所有客户端初始化动画器
6. 同步当前speed状态到动画器
```

**关键代码：**
```csharp
private void OnCharacterModelLoaded(GameObject model)
{
    StartCoroutine(InitializeAnimatorsAfterModelLoaded());
    CmdNotifyModelLoaded();
}

[Command]
private void CmdNotifyModelLoaded()
{
    modelLoaded = true;
    RpcNotifyModelLoaded();
}

[ClientRpc]
private void RpcNotifyModelLoaded()
{
    StartCoroutine(InitializeAnimatorsAfterModelLoaded());
}

private IEnumerator InitializeAnimatorsAfterModelLoaded()
{
    yield return null;
    
    Animator[] foundAnimators = GetComponentsInChildren<Animator>(true);
    if (foundAnimators.Length > 0)
    {
        this.animators = new List<Animator>(foundAnimators);
        animatorsReady = true;
        
        // 同步当前speed
        foreach (Animator anim in this.animators)
            anim.SetFloat("speed", syncedSpeed);
    }
}
```

---

### 四、与其他模块的联系

#### 4.1 与背包系统的联系

看 `NetworkPlayer.cs:89-93` 和 `896-899`：
```csharp
// OnStartServer中创建/获取Inventory
inventory = GetComponent<Inventory>();
if (inventory == null)
    inventory = gameObject.AddComponent<Inventory>();

// 提供访问方法
public Inventory GetInventory()
{
    return inventory;
}
```

**联系点：**
- NetworkPlayer拥有并管理Inventory组件
- 初始化时自动填充背包武器
- 其他系统通过GetInventory()获取背包

#### 4.2 与角色状态系统的联系

看 `CharacterStats.cs:194-216`：
```csharp
[Server]
public void ApplyDamage(int amount)
{
    amount = Mathf.Abs(amount);
    int newHealth = currentHealth - amount;
    newHealth = Mathf.Max(0, newHealth);
    currentHealth = newHealth;
}

[Server]
public void Heal(int amount)
{
    amount = Mathf.Abs(amount);
    int newHealth = currentHealth + amount;
    newHealth = Mathf.Min(newHealth, maxHealth);
    if (newHealth != currentHealth)
        currentHealth = newHealth;
}
```

**联系点：**
- 敌人通过CharacterStats.ApplyDamage()对玩家造成伤害
- 背包系统通过CharacterStats.Heal()恢复血量

#### 4.3 与武器系统的联系

看 `NetworkPlayer.cs:874-894`：
```csharp
[Command]
public void CmdOnAttackHit()
{
    if (!isServer) return;
    
    WeaponDamageSystem damageSystem = GetComponent<WeaponDamageSystem>();
    if (damageSystem != null)
    {
        damageSystem.PerformDamageCheck(Vector3.zero, 0f);
    }
}
```

**联系点：**
- NetworkPlayer持有WeaponDamageSystem组件
- 攻击命中事件转发给WeaponDamageSystem处理

#### 4.4 与角色数据库的联系

看 `CharacterStats.cs:99-125`：
```csharp
public void InitializeCharacterData(CharacterData data)
{
    characterId = data.id;
    maxHealth = data.health;
    moveSpeed = data.speed;
    currentHealth = data.health;
}
```

**联系点：**
- 角色属性从CharacterDatabase读取
- 支持多种角色配置

---

### 五、关键设计点

#### 5.1 并行初始化
- SceneAwareAnimatorManager、PlayerInputHandler、CharacterModelManager并行初始化
- 提高初始化效率，减少等待时间

#### 5.2 Speed连续值同步
- 替代原来的isRunning布尔值
- 更平滑的动画过渡
- 支持不同移动速度

#### 5.3 模型加载通知机制
- 本地玩家加载完成 → 通知服务器
- 服务器 → 通过ClientRpc通知所有客户端
- 所有客户端初始化动画器
- 解决后加入客户端的动画器初始化问题

#### 5.4 延迟初始化远程玩家动画器
- 远程玩家加入时，等待模型加载完成
- 最多等待10秒，超时放弃
- 解决时序问题

---

### 六、总结

角色系统是整个游戏的核心模块，它：
1. **协调各个子系统**：背包、武器、血量、移动
2. **并行初始化**提高效率
3. **Speed连续值同步**实现平滑动画
4. **模型加载通知机制**解决后加入客户端问题
5. **采用服务器权威**保证同步一致性

---

## 数据库系统详解

---

### 一、实现思路

#### 1.1 数据库系统的核心功能
数据库系统是游戏的配置中心，主要功能包括：
- **JSON配置加载**：从StreamingAssets加载JSON文件
- **数据缓存**：Dictionary快速查询
- **单例模式**：全局唯一访问
- **多种数据库**：WeaponDatabase、EnemyDatabase、ResourceDatabase、CharacterDatabase

#### 1.2 JSON配置设计
所有游戏数据都通过JSON配置：
- 武器数据：WeaponData.json
- 敌人数据：EnemyData.json
- 资源数据：ResourceData.json
- 角色数据：CharacterData.json

---

### 二、核心数据结构

#### 2.1 武器数据库结构

看 `WeaponDatabase.cs:92-231`：
```csharp
public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }
    
    private Dictionary<int, WeaponData> weaponDict = new Dictionary<int, WeaponData>();
    private bool isInitialized = false;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadWeaponData();
    }
    
    void LoadWeaponData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "WeaponData.json");
        string json = File.ReadAllText(path);
        
        WeaponDataCollection collection = JsonUtility.FromJson<WeaponDataCollection>(json);
        
        foreach (var weapon in collection.Weapons)
        {
            weaponDict[weapon.weaponId] = weapon;
        }
        
        isInitialized = true;
    }
    
    public WeaponData GetWeapon(int weaponId)
    {
        if (weaponDict.TryGetValue(weaponId, out WeaponData weapon))
            return weapon;
        return null;
    }
    
    public WeaponData GetWeaponByResourceId(string resourceId)
    {
        foreach (var weapon in weaponDict.Values)
        {
            if (weapon.resourceId == resourceId)
                return weapon;
        }
        return null;
    }
}
```

#### 2.2 武器数据集合结构

看 `WeaponDatabase.cs:86-90`：
```csharp
[System.Serializable]
class WeaponDataCollection
{
    public List<WeaponData> Weapons;
}
```

---

### 三、核心算法

#### 3.1 JSON数据加载算法

所有数据库的加载流程都类似：
```
加载流程：
1. 构建JSON文件路径（Application.streamingAssetsPath）
2. 读取文件内容
3. JsonUtility.FromJson解析
4. 遍历数据，存入Dictionary
5. 标记初始化完成
```

**关键代码：**
```csharp
void LoadWeaponData()
{
    string path = Path.Combine(Application.streamingAssetsPath, "WeaponData.json");
    
    if (!File.Exists(path))
    {
        Debug.LogError($"武器数据文件不存在: {path}");
        return;
    }
    
    string json = File.ReadAllText(path);
    WeaponDataCollection collection = JsonUtility.FromJson<WeaponDataCollection>(json);
    
    if (collection == null || collection.Weapons == null)
    {
        Debug.LogError("武器数据解析失败");
        return;
    }
    
    foreach (var weapon in collection.Weapons)
    {
        weaponDict[weapon.weaponId] = weapon;
    }
    
    isInitialized = true;
}
```

#### 3.2 武器数据查询算法

看 `WeaponDatabase.cs:154-225`：
```
查询流程（通过武器ID）：
1. Dictionary.TryGetValue查找
2. 找到返回，没找到返回null

查询流程（通过资源ID）：
1. 遍历Dictionary.Values
2. 匹配resourceId字段
3. 找到返回，没找到返回null
```

**关键代码：**
```csharp
public WeaponData GetWeapon(int weaponId)
{
    if (weaponDict.TryGetValue(weaponId, out WeaponData weapon))
        return weapon;
    Debug.LogWarning($"未找到武器ID: {weaponId}");
    return null;
}

public WeaponData GetWeaponByResourceId(string resourceId)
{
    foreach (var weapon in weaponDict.Values)
    {
        if (!string.IsNullOrEmpty(weapon.resourceId) && weapon.resourceId == resourceId)
            return weapon;
    }
    return null;
}
```

---

### 四、与其他模块的联系

#### 4.1 与武器系统的联系

看 `WeaponDamageSystem.cs:99-111` 和 `PlayerWeaponController.cs:469-471`：
```csharp
// WeaponDamageSystem从数据库加载武器数据
if (WeaponDatabase.Instance != null && WeaponDatabase.Instance.IsInitialized)
{
    currentWeaponData = WeaponDatabase.Instance.GetWeapon(newId);
}

// PlayerWeaponController从数据库获取武器ID
WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(slotData.resourceId);
return weaponData?.weaponId ?? -1;
```

**联系点：**
- 武器系统从WeaponDatabase读取武器配置
- 伤害、范围、角度、连击倍率都来自数据库

#### 4.2 与敌人系统的联系

看 `EnemyAIController.cs:92-96` 和 `EnemyHealthManager.cs:184`：
```csharp
// EnemyAIController从数据库读取敌人数据
data = EnemyDatabase.GetInstance().GetEnemy(enemyType);
agent.speed = data.patrolSpeed;

// EnemyHealthManager从数据库读取血量
enemyData = EnemyDatabase.GetInstance().GetEnemy(enemyType);
maxHealth = enemyData.health;
currentHealth = enemyData.health;
```

**联系点：**
- 敌人系统从EnemyDatabase读取敌人配置
- 血量、速度、攻击力来自数据库

#### 4.3 与背包系统的联系

看 `Inventory.cs:230-241` 和 `InventoryUIController.cs:625-678`：
```csharp
// Inventory从ResourceDatabase验证食物类型
ResourceData resourceData = ResourceDatabase.Instance.GetResource(slot.resourceId);
if (resourceData.type != "食物资源") return;

// InventoryUIController从数据库获取Sprite
string GetSpriteAddressableKey(string resourceId)
{
    WeaponData weaponData = WeaponDatabase.Instance.GetWeaponByResourceId(resourceId);
    if (weaponData != null)
        return weaponData.spriteAddressableKey;
    
    ResourceData resourceData = ResourceDatabase.Instance.GetResource(resourceId);
    if (resourceData != null)
        return resourceData.spriteAddressableKey;
}
```

**联系点：**
- 背包系统从ResourceDatabase读取资源类型
- 背包UI从数据库读取Sprite地址

---

### 五、总结

数据库系统是整个游戏的配置中心，它：
1. **从JSON文件加载配置**，易于修改和扩展
2. **使用Dictionary缓存**，查询速度O(1)
3. **单例模式**，全局唯一访问
4. **与所有游戏系统协作**，提供配置数据
5. **DontDestroyOnLoad**，跨场景保持

---

## 资源系统详解

---

### 一、实现思路

#### 1.1 资源系统的核心功能
资源系统是玩家收集物品的核心模块，主要功能包括：
- **资源生成**：从对象池获取资源，随机位置生成
- **资源拾取**：玩家靠近自动拾取，加入背包
- **资源类型**：木材、石材、苹果、梨等多种类型
- **动态加载**：从ResourceDatabase动态加载资源类型

#### 1.2 服务器权威设计
资源生成和拾取都在服务器端执行：
- 只有服务器能生成资源
- 只有服务器能处理拾取
- 客户端只负责显示和发送请求

---

### 二、核心数据结构

#### 2.1 资源生成器数据结构

看 `ResourceSpawner.cs:8-16`：
```csharp
public class ResourceSpawner : NetworkBehaviour
{
    public string woodKey = "Wood";
    public string stoneKey = "Stone";
    public string appleKey = "Apple";
    public string pearKey = "Pear";
    
    // 动态资源类型列表
    private List<string> dynamicResourceTypes = new List<string>();
}
```

---

### 三、核心算法

#### 3.1 动态资源类型加载算法

看 `ResourceSpawner.cs:71-131`：
```
动态加载流程：
1. 等待ResourceDatabase初始化
2. 通过反射获取ResourceDatabase的私有字段resources
3. 遍历resources字典，添加所有资源类型
4. 如果反射失败，使用硬编码的资源类型
```

**关键代码：**
```csharp
private void LoadDynamicResourceTypes()
{
    dynamicResourceTypes.Clear();
    
    // 等待ResourceDatabase初始化
    int maxAttempts = 10;
    int attempts = 0;
    while (attempts < maxAttempts)
    {
        if (ResourceDatabase.Instance != null)
            break;
        attempts++;
        new WaitForSeconds(0.5f);
    }
    
    if (ResourceDatabase.Instance == null)
    {
        // 回退到硬编码
        dynamicResourceTypes.Add(woodKey);
        dynamicResourceTypes.Add(stoneKey);
        return;
    }
    
    // 通过反射获取resources字段
    var resourcesField = typeof(ResourceDatabase).GetField(
        "resources", BindingFlags.NonPublic | BindingFlags.Instance);
    
    if (resourcesField != null)
    {
        var resources = resourcesField.GetValue(ResourceDatabase.Instance) 
            as Dictionary<string, ResourceData>;
        
        if (resources != null)
        {
            foreach (var kvp in resources)
            {
                dynamicResourceTypes.Add(kvp.Key);
            }
        }
    }
}
```

**为什么用反射？**
- ResourceDatabase的resources字段是private
- 不修改ResourceDatabase代码的情况下获取数据
- 保持模块解耦

#### 3.2 初始资源生成算法

看 `ResourceSpawner.cs:134-183`：
```
初始生成流程：
1. 等待AutoObjectPoolManager初始化
2. 等待所有资源加载完成（最多30秒）
3. 生成config.initialResourcePoolSize个资源
4. 每个资源随机类型和位置
```

**关键代码：**
```csharp
IEnumerator DelayedInitialSpawn()
{
    // 等待对象池管理器初始化
    while (AutoObjectPoolManager.Instance == null)
    {
        yield return new WaitForSeconds(0.5f);
    }
    
    // 等待资源加载完成
    float maxWaitTime = 30f;
    float elapsedTime = 0f;
    while (!AutoObjectPoolManager.Instance.AllResourcesLoaded && elapsedTime < maxWaitTime)
    {
        yield return new WaitForSeconds(0.5f);
        elapsedTime += 0.5f;
    }
    
    InitialSpawn();
}

[Server]
void InitialSpawn()
{
    PoolConfig config = PoolConfigProvider.Instance.Config;
    
    for (int i = 0; i < config.initialResourcePoolSize; i++)
    {
        SpawnRandomResource();
    }
}
```

#### 3.3 随机资源生成算法

看 `ResourceSpawner.cs:186-234`：
```
随机生成流程：
1. 生成随机位置（避开营地）
2. 从动态资源类型列表随机选择类型
3. 从对象池获取资源对象
4. 设置resourceId
```

**关键代码：**
```csharp
[Server]
void SpawnRandomResource()
{
    Vector3 pos = SpawnPositionHelper.GetRandomPositionOnSceneTerrain(
        config.campAvoidanceDistance, 20);
    
    if (pos == Vector3.zero)
        return;
    
    // 随机选择资源类型
    int randomIndex = Random.Range(0, dynamicResourceTypes.Count);
    string key = dynamicResourceTypes[randomIndex];
    
    // 从对象池获取
    GameObject resource = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);
    if (resource != null)
    {
        ResourceNode resourceNode = resource.GetComponent<ResourceNode>();
        if (resourceNode != null)
            resourceNode.resourceId = key;
    }
}
```

---

### 四、与其他模块的联系

#### 4.1 与对象池系统的联系

看 `ResourceSpawner.cs:217`：
```csharp
GameObject resource = AutoObjectPoolManager.Instance.GetObject(key, pos, Quaternion.identity);
```

**联系点：**
- ResourceSpawner从对象池获取资源
- 玩家拾取后，资源归还到对象池

#### 4.2 与资源数据库的联系

看 `ResourceSpawner.cs:100-110`：
```csharp
var resources = resourcesField.GetValue(ResourceDatabase.Instance) as Dictionary<string, ResourceData>;
if (resources != null)
{
    foreach (var kvp in resources)
    {
        dynamicResourceTypes.Add(kvp.Key);
    }
}
```

**联系点：**
- 从ResourceDatabase动态加载资源类型
- 支持添加新资源类型无需修改代码

#### 4.3 与背包系统的联系

（ResourceNode拾取逻辑）
```csharp
// 玩家拾取资源后，加入背包
inventory.Add(resourceId, 1);
// 归还到对象池
AutoObjectPoolManager.Instance.ReturnObject(resourceId, gameObject);
```

**联系点：**
- 资源拾取后加入玩家背包
- 背包系统验证是否能放下

---

### 五、总结

资源系统是玩家收集的核心模块，它：
1. **从对象池获取资源**，高效管理
2. **动态加载资源类型**，易于扩展
3. **随机位置生成**，避开营地
4. **与背包系统集成**，自动拾取
5. **采用服务器权威**，保证一致性

