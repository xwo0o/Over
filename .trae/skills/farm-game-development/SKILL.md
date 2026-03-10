---
name: "farm-game-development"
description: "农场游戏开发指南。当用户需要开发农场游戏、实现农作物系统、动物系统、社交偷菜功能时调用。"
---

# 农场游戏开发指南

此skill专门用于将现有的多人生存游戏转型为社交农场游戏，基于Unity + Mirror网络架构。

## 适用场景

**必须调用此skill的情况：**
- 用户需要开发农场游戏或农场模拟系统
- 实现农作物种植、生长、收获系统
- 改造动物系统（从战斗AI到农场动物）
- 实现社交功能（好友、农场访问、偷菜）
- 设计农场建筑系统（鸡舍、谷仓、水井等）
- 需要农场游戏的整体架构设计
- 实现农场数据管理和配置系统

**技术栈：**
- Unity 2022.3.15f1
- Mirror网络插件（服务器权威模式）
- 现有项目组件复用（网络管理器、对象池、建筑系统等）

## 项目结构分析

### 可复用组件（无需修改）
- **GameNetworkManager.cs**：网络管理核心
- **ObjectPoolManager.cs**：对象池系统
- **Inventory.cs**：物品栏系统框架
- **BuildingSystem.cs**：建筑系统架构
- **JSON数据配置系统**：数据驱动架构

### 需要改造的组件
- **EnemySpawner.cs** → **AnimalSpawner.cs**（动物生成）
- **GameScene.unity** → **FarmScene.unity**（农场场景）
- **NetworkPlayer.cs** → 扩展农场功能
- **UI系统** → 农场主题界面

### 需要新建的系统
- 农作物系统（CropSystem）
- 农场数据管理（FarmDataManager）
- 社交系统（SocialSystem）
- 偷菜机制（StealingSystem）

## 开发路线图（8周计划）

### 第一阶段：基础架构改造（Week 1-2）

**Week 1: 场景和数据准备**
- Day 1-2: 创建农场基础场景
  - 复制GameScene.unity → FarmScene.unity
  - 删除战斗相关物体
  - 添加农场地形（草地、土地块）
  - 设置摄像机视角（俯视角度）

- Day 3-4: 数据配置文件创建
  - 创建CropData.json（农作物配置）
  - 创建AnimalData.json（动物配置）
  - 创建FarmBuildingData.json（农场建筑配置）
  - 扩展CharacterData.json（添加农场相关属性）

- Day 5-7: 基础数据类实现
  - 创建CropData.cs（农作物数据结构）
  - 创建AnimalData.cs（动物数据结构）
  - 创建FarmBuildingData.cs（农场建筑数据结构）
  - 创建FarmDatabase.cs（农场数据库管理器）

**Week 2: 核心系统实现**
- Day 8-10: 农作物系统核心
  - 实现CropSystem.cs（种植、生长、收获）
  - 实现FarmLand.cs（可种植土地）
  - 创建种植工具（PlantingTool.cs）
  - 添加基础农作物预制体

- Day 11-14: 网络同步集成
  - 集成Mirror网络到农作物系统
  - 实现种植收获的Command/Rpc
  - 测试多人种植功能
  - 优化网络同步性能

### 第二阶段：动物系统改造（Week 3）

**Week 3: 动物系统开发**
- Day 15-17: 改造Enemy系统
  - 复制EnemySpawner.cs → AnimalSpawner.cs
  - 修改AI行为（从攻击→觅食）
  - 实现动物喂养机制
  - 创建动物产品收获功能

- Day 18-21: 动物行为实现
  - 实现AnimalBehavior.cs（动物AI）
  - 添加动物幸福度系统
  - 实现动物生产效率计算
  - 创建动物预制体和动画

### 第三阶段：建筑系统适配（Week 4）

**Week 4: 农场建筑系统**
- Day 22-24: 建筑系统改造
  - 修改BuildingSystem.cs支持农场建筑
  - 实现农场建筑类型（鸡舍、谷仓、水井等）
  - 添加建筑功能效果（水井自动浇水）
  - 创建农场建筑预制体

- Day 25-28: 建筑交互优化
  - 实现建筑升级系统
  - 添加建筑维护机制
  - 优化建筑放置体验
  - 集成建筑系统到农场场景

### 第四阶段：社交系统开发（Week 5-6）

**Week 5: 好友系统实现**
- Day 29-31: 好友系统核心
  - 实现SocialSystem.cs（好友管理）
  - 创建好友列表UI
  - 实现好友请求/接受机制
  - 添加好友亲密度系统

- Day 32-35: 农场访问功能
  - 实现农场访问权限控制
  - 创建访客模式UI
  - 实现跨服农场数据加载
  - 优化访问体验

**Week 6: 偷菜机制实现**
- Day 36-38: 偷菜系统核心
  - 实现StealingSystem.cs（偷菜逻辑）
  - 添加偷取验证机制
  - 实现被偷记录功能
  - 创建偷菜动画效果

- Day 39-42: 保护机制
  - 实现FarmProtectionSystem.cs（农场保护）
  - 添加保护道具系统
  - 实现保护时间计算
  - 创建保护状态UI

### 第五阶段：UI系统重构（Week 7）

**Week 7: 农场UI开发**
- Day 43-45: 主界面重构
  - 设计农场主界面布局
  - 实现快捷工具栏
  - 创建农作物状态显示
  - 添加资源信息显示

- Day 46-49: 功能UI实现
  - 创建背包界面（适配农场物品）
  - 实现商店界面（购买种子/动物）
  - 添加好友列表UI
  - 创建被偷记录界面

### 第六阶段：优化和测试（Week 8）

**Week 8: 性能优化和测试**
- Day 50-52: 性能优化
  - 优化对象池使用
  - 减少网络同步频率
  - 实现LOD系统
  - 优化移动端性能

- Day 53-56: 系统测试
  - 进行功能完整性测试
  - 测试多人并发场景
  - 修复发现的bug
  - 优化用户体验

## 核心系统实现

### 1. 农场地形系统

```csharp
// 文件路径: Assets/Scripts/Farm/FarmTerrain.cs
using UnityEngine;
using Mirror;

public class FarmTerrain : NetworkBehaviour
{
    [Header("地形设置")]
    public int farmSize = 20; // 农场大小
    public GameObject grassTile; // 草地瓦片
    public GameObject landTile; // 可种植土地
    
    [Server]
    public override void OnStartServer()
    {
        GenerateFarmTerrain();
    }
    
    /// <summary>
    /// 生成农场基础地形
    /// </summary>
    void GenerateFarmTerrain()
    {
        // 生成草地背景
        for (int x = -farmSize; x <= farmSize; x++)
        {
            for (int z = -farmSize; z <= farmSize; z++)
            {
                Vector3 position = new Vector3(x, 0, z);
                GameObject tile = Instantiate(grassTile, position, Quaternion.identity);
                NetworkServer.Spawn(tile);
            }
        }
        
        // 生成可种植土地（4x4网格）
        GenerateFarmableLands();
    }
    
    /// <summary>
    /// 生成可种植土地
    /// </summary>
    void GenerateFarmableLands()
    {
        int landSize = 4;
        float spacing = 2f;
        
        for (int x = 0; x < landSize; x++)
        {
            for (int z = 0; z < landSize; z++)
            {
                Vector3 position = new Vector3(
                    (x - landSize/2) * spacing, 
                    0.1f, 
                    (z - landSize/2) * spacing
                );
                
                GameObject land = Instantiate(landTile, position, Quaternion.identity);
                land.GetComponent<FarmLand>().landId = $"land_{x}_{z}";
                NetworkServer.Spawn(land);
            }
        }
    }
}
```

### 2. 农作物系统

```csharp
// 文件路径: Assets/Scripts/Farm/CropSystem.cs
using UnityEngine;
using Mirror;
using System.Collections;

public class CropSystem : NetworkBehaviour
{
    [Header("作物信息")]
    [SyncVar(hook = nameof(OnCropIdChanged))]
    public string cropId = "";
    
    [SyncVar(hook = nameof(OnGrowthStageChanged))]
    public int growthStage = 0;
    
    [SyncVar]
    public bool isReady = false;
    
    [SyncVar]
    public bool isStolen = false;
    
    private CropData cropData;
    private float growthStartTime;
    
    /// <summary>
    /// 种植作物
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdPlantCrop(string newCropId, NetworkConnectionToClient sender = null)
    {
        // 验证是否可以种植
        if (!string.IsNullOrEmpty(cropId)) return;
        
        // 检查玩家是否有种子
        NetworkPlayer player = sender?.identity?.GetComponent<NetworkPlayer>();
        if (player == null || !player.HasItem(newCropId, 1)) return;
        
        // 扣除种子
        player.ConsumeItem(newCropId, 1);
        
        // 设置作物数据
        cropId = newCropId;
        growthStage = 0;
        isReady = false;
        isStolen = false;
        growthStartTime = Time.time;
        
        cropData = FarmDatabase.GetCropData(cropId);
        
        // 开始生长
        StartCoroutine(GrowthCoroutine());
        
        // 广播种植事件
        RpcOnCropPlanted(newCropId);
    }
    
    /// <summary>
    /// 收获作物
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdHarvestCrop(NetworkConnectionToClient sender = null)
    {
        if (!isReady || isStolen) return;
        
        NetworkPlayer player = sender?.identity?.GetComponent<NetworkPlayer>();
        if (player == null) return;
        
        // 计算收获数量
        int quantity = cropData.baseHarvestQuantity;
        
        // 添加到玩家背包
        player.AddItem(cropId, quantity);
        
        // 给予经验
        player.AddExperience(cropData.experience);
        
        // 重置作物
        ResetCrop();
        
        // 广播收获事件
        RpcOnCropHarvested(quantity);
    }
    
    /// <summary>
    /// 尝试偷取作物
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdStealCrop(NetworkConnectionToClient sender = null)
    {
        if (!isReady || isStolen) return;
        
        NetworkPlayer thief = sender?.identity?.GetComponent<NetworkPlayer>();
        if (thief == null) return;
        
        // 计算偷取数量（最多30%）
        int stealQuantity = Mathf.CeilToInt(cropData.baseHarvestQuantity * 0.3f);
        
        // 给偷取者
        thief.AddItem(cropId, stealQuantity);
        
        // 标记为被偷
        isStolen = true;
        
        // 广播偷取事件
        RpcOnCropStolen(thief.playerName, stealQuantity);
    }
    
    /// <summary>
    /// 生长协程
    /// </summary>
    IEnumerator GrowthCoroutine()
    {
        while (growthStage < cropData.growthStages - 1)
        {
            yield return new WaitForSeconds(cropData.growthTimePerStage);
            
            growthStage++;
            
            if (growthStage >= cropData.growthStages - 1)
            {
                isReady = true;
                RpcOnCropReady();
            }
        }
    }
    
    /// <summary>
    /// 重置作物
    /// </summary>
    void ResetCrop()
    {
        cropId = "";
        growthStage = 0;
        isReady = false;
        isStolen = false;
    }
    
    /// <summary>
    /// 作物ID变化回调
    /// </summary>
    void OnCropIdChanged(string oldId, string newId)
    {
        // 更新视觉表现
        UpdateVisual();
    }
    
    /// <summary>
    /// 生长阶段变化回调
    /// </summary>
    void OnGrowthStageChanged(int oldStage, int newStage)
    {
        // 更新视觉表现
        UpdateVisual();
    }
    
    /// <summary>
    /// 更新视觉表现
    /// </summary>
    void UpdateVisual()
    {
        if (string.IsNullOrEmpty(cropId))
        {
            // 隐藏作物模型
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            // 显示对应生长阶段的模型
            if (cropData != null && growthStage < cropData.stageModels.Length)
            {
                // 隐藏所有子物体
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
                
                // 显示当前阶段模型
                if (growthStage < transform.childCount)
                {
                    transform.GetChild(growthStage).gameObject.SetActive(true);
                }
            }
        }
    }
    
    // RPC函数
    [ObserversRpc]
    void RpcOnCropPlanted(string newCropId)
    {
        // 客户端播放种植特效
        Debug.Log($"作物 {newCropId} 已种植");
    }
    
    [ObserversRpc]
    void RpcOnCropHarvested(int quantity)
    {
        // 客户端播放收获特效
        Debug.Log($"收获了 {quantity} 个作物");
    }
    
    [ObserversRpc]
    void RpcOnCropStolen(string thiefName, int quantity)
    {
        // 客户端播放偷取特效
        Debug.Log($"{thiefName} 偷走了 {quantity} 个作物");
    }
    
    [ObserversRpc]
    void RpcOnCropReady()
    {
        // 客户端播放成熟特效
        Debug.Log("作物已成熟，可以收获了！");
    }
}
```

### 3. 农场建筑系统

```csharp
// 文件路径: Assets/Scripts/Building/FarmBuildingSystem.cs
using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class FarmBuildingSystem : NetworkBehaviour
{
    [Header("农场建筑")]
    public GameObject[] farmBuildingPrefabs;
    public Transform buildingParent;
    
    private Dictionary<string, GameObject> buildingPrefabsDict;
    private List<FarmBuilding> activeBuildings;
    
    public override void OnStartServer()
    {
        InitializeBuildingDictionary();
        activeBuildings = new List<FarmBuilding>();
    }
    
    /// <summary>
    /// 初始化建筑字典
    /// </summary>
    void InitializeBuildingDictionary()
    {
        buildingPrefabsDict = new Dictionary<string, GameObject>();
        foreach (GameObject prefab in farmBuildingPrefabs)
        {
            FarmBuilding building = prefab.GetComponent<FarmBuilding>();
            if (building != null)
            {
                buildingPrefabsDict[building.buildingId] = prefab;
            }
        }
    }
    
    /// <summary>
    /// 建造农场建筑
    /// </summary>
    [Command]
    public void CmdBuildFarmBuilding(string buildingId, Vector3 position, NetworkConnectionToClient sender = null)
    {
        // 验证建筑ID
        if (!buildingPrefabsDict.ContainsKey(buildingId))
        {
            Debug.LogError($"未知的建筑ID: {buildingId}");
            return;
        }
        
        // 获取建筑数据
        FarmBuildingData buildingData = FarmDatabase.GetBuildingData(buildingId);
        if (buildingData == null) return;
        
        // 检查玩家资源
        NetworkPlayer player = sender?.identity?.GetComponent<NetworkPlayer>();
        if (player == null) return;
        
        if (!player.HasResources(buildingData.buildCost))
        {
            TargetShowMessage(sender, "资源不足！");
            return;
        }
        
        // 扣除资源
        player.ConsumeResources(buildingData.buildCost);
        
        // 实例化建筑
        GameObject buildingObj = Instantiate(buildingPrefabsDict[buildingId], position, Quaternion.identity);
        FarmBuilding building = buildingObj.GetComponent<FarmBuilding>();
        building.ownerId = player.playerId;
        building.buildingId = buildingId;
        
        // 网络生成
        NetworkServer.Spawn(buildingObj, sender);
        
        // 添加到活动建筑列表
        activeBuildings.Add(building);
        
        // 广播建造事件
        RpcOnBuildingBuilt(buildingId, position);
    }
    
    /// <summary>
    /// 升级建筑
    /// </summary>
    [Command]
    public void CmdUpgradeBuilding(NetworkIdentity buildingNetId, NetworkConnectionToClient sender = null)
    {
        if (buildingNetId == null) return;
        
        FarmBuilding building = buildingNetId.GetComponent<FarmBuilding>();
        if (building == null) return;
        
        // 检查所有权
        NetworkPlayer player = sender?.identity?.GetComponent<NetworkPlayer>();
        if (player == null || building.ownerId != player.playerId) return;
        
        // 检查是否可以升级
        if (!building.CanUpgrade()) return;
        
        // 获取升级数据
        FarmBuildingUpgradeData upgradeData = building.GetNextUpgradeData();
        if (upgradeData == null) return;
        
        // 检查资源
        if (!player.HasResources(upgradeData.cost))
        {
            TargetShowMessage(sender, "升级资源不足！");
            return;
        }
        
        // 扣除资源并升级
        player.ConsumeResources(upgradeData.cost);
        building.ApplyUpgrade(upgradeData);
        
        RpcOnBuildingUpgraded(buildingNetId);
    }
    
    [TargetRpc]
    void TargetShowMessage(NetworkConnection target, string message)
    {
        // 显示消息给特定玩家
        Debug.Log(message);
    }
    
    [ObserversRpc]
    void RpcOnBuildingBuilt(string buildingId, Vector3 position)
    {
        // 播放建造特效
        Debug.Log($"建筑 {buildingId} 建造完成");
    }
    
    [ObserversRpc]
    void RpcOnBuildingUpgraded(NetworkIdentity buildingNetId)
    {
        // 播放升级特效
        Debug.Log($"建筑 {buildingNetId} 升级完成");
    }
}
```

### 4. 社交系统

```csharp
// 文件路径: Assets/Scripts/Social/FarmSocialSystem.cs
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class FarmSocialSystem : NetworkBehaviour
{
    [Header("社交设置")]
    [SyncVar(hook = nameof(OnFriendsListUpdated))]
    private SyncList<FarmFriendData> friendsList = new SyncList<FarmFriendData>();
    
    [SyncVar(hook = nameof(OnStealRecordsUpdated))]
    private SyncList<StealRecord> stealRecords = new SyncList<StealRecord>();
    
    /// <summary>
    /// 添加好友
    /// </summary>
    [Command]
    public void CmdAddFriend(string targetPlayerId, NetworkConnectionToClient sender = null)
    {
        if (string.IsNullOrEmpty(targetPlayerId)) return;
        
        // 检查是否已经是好友
        if (IsFriend(targetPlayerId)) return;
        
        // 获取目标玩家信息
        NetworkPlayer targetPlayer = GetPlayerById(targetPlayerId);
        if (targetPlayer == null) return;
        
        // 获取当前玩家信息
        NetworkPlayer currentPlayer = sender?.identity?.GetComponent<NetworkPlayer>();
        if (currentPlayer == null) return;
        
        // 发送好友请求
        targetPlayer.GetComponent<FarmSocialSystem>().TargetReceiveFriendRequest(
            currentPlayer.playerId,
            currentPlayer.playerName
        );
    }
    
    /// <summary>
    /// 访问好友农场
    /// </summary>
    [Command]
    public void CmdVisitFriendFarm(string friendId, NetworkConnectionToClient sender = null)
    {
        if (!IsFriend(friendId))
        {
            TargetShowMessage(sender, "对方不是您的好友！");
            return;
        }
        
        NetworkPlayer friendPlayer = GetPlayerById(friendId);
        if (friendPlayer == null)
        {
            TargetShowMessage(sender, "好友不在线！");
            return;
        }
        
        // 获取好友农场数据
        FarmData friendFarm = FarmDataManager.Instance.GetPlayerFarm(friendId);
        if (friendFarm == null)
        {
            TargetShowMessage(sender, "好友农场数据不存在！");
            return;
        }
        
        // 发送农场数据给访客
        TargetSendFarmData(sender, friendFarm);
        
        // 增加亲密度
        UpdateIntimacy(friendId, 2);
        
        // 记录访问日志
        LogFarmVisit(sender.identity.GetComponent<NetworkPlayer>().playerId, friendId);
    }
    
    /// <summary>
    /// 添加偷取记录
    /// </summary>
    [Server]
    public void AddStealRecord(string victimId, string cropId, int quantity)
    {
        NetworkPlayer thief = NetworkServer.localConnection.identity.GetComponent<NetworkPlayer>();
        
        StealRecord record = new StealRecord
        {
            thiefId = thief.playerId,
            thiefName = thief.playerName,
            victimId = victimId,
            cropId = cropId,
            quantity = quantity,
            stealTime = Time.time
        };
        
        stealRecords.Add(record);
        
        // 限制记录数量（最多保留50条）
        if (stealRecords.Count > 50)
        {
            stealRecords.RemoveAt(0);
        }
    }
    
    #region 辅助方法
    
    /// <summary>
    /// 检查是否是好友
    /// </summary>
    bool IsFriend(string playerId)
    {
        return friendsList.Any(friend => friend.playerId == playerId);
    }
    
    /// <summary>
    /// 获取玩家（辅助方法）
    /// </summary>
    NetworkPlayer GetPlayerById(string playerId)
    {
        return NetworkServer.connections.Values
            .Select(conn => conn?.identity?.GetComponent<NetworkPlayer>())
            .FirstOrDefault(player => player != null && player.playerId == playerId);
    }
    
    /// <summary>
    /// 记录农场访问
    /// </summary>
    void LogFarmVisit(string visitorId, string farmOwnerId)
    {
        Debug.Log($"玩家 {visitorId} 访问了 {farmOwnerId} 的农场");
    }
    
    #endregion
    
    #region RPC函数
    
    [TargetRpc]
    void TargetReceiveFriendRequest(NetworkConnection target, string requesterId, string requesterName)
    {
        // 显示好友请求UI
        Debug.Log($"收到来自 {requesterName} 的好友请求");
    }
    
    [TargetRpc]
    void TargetSendFarmData(NetworkConnection target, FarmData farmData)
    {
        // 发送农场数据给访客
        Debug.Log($"接收到农场数据: {farmData.farmName}");
    }
    
    [TargetRpc]
    void TargetShowMessage(NetworkConnection target, string message)
    {
        // 显示消息
        Debug.Log(message);
    }
    
    [ObserversRpc]
    void OnFriendsListUpdated(SyncList<FarmFriendData>.Operation op, int index, FarmFriendData oldItem, FarmFriendData newItem)
    {
        // 好友列表更新回调
        Debug.Log("好友列表已更新");
    }
    
    [ObserversRpc]
    void OnStealRecordsUpdated(SyncList<StealRecord>.Operation op, int index, StealRecord oldItem, StealRecord newItem)
    {
        // 被偷记录更新回调
        Debug.Log("被偷记录已更新");
    }
    
    #endregion
}

/// <summary>
/// 好友数据结构
/// </summary>
[System.Serializable]
public struct FarmFriendData
{
    public string playerId;
    public string playerName;
    public int farmLevel;
    public bool isOnline;
    public int intimacyLevel;
    public float lastInteractionTime;
}

/// <summary>
/// 偷取记录数据结构
/// </summary>
[System.Serializable]
public struct StealRecord
{
    public string thiefId;
    public string thiefName;
    public string victimId;
    public string cropId;
    public int quantity;
    public float stealTime;
}
```

## 开发顺序建议

1. **先实现核心玩法**：种植→生长→收获循环
2. **再添加网络同步**：确保多人游戏正常
3. **然后改造现有系统**：建筑、库存适配农场
4. **最后添加社交功能**：偷菜、好友系统

## 测试要点

### 基础功能测试
- 农作物种植正常
- 生长时间准确
- 收获功能正常
- 网络同步无延迟

### 社交功能测试
- 好友添加正常
- 农场访问正常
- 偷菜机制平衡
- 保护系统有效

### 性能测试
- 对象池复用正常
- 内存使用合理
- 帧率稳定60FPS
- 网络延迟<100ms

## 代码规范

- 所有网络方法使用[Server]或[Client]属性标记
- ServerRpc方法名以"Cmd"开头
- ClientRpc方法名以"Rpc"开头
- SyncVar变量使用hook方法处理客户端更新
- 所有中文注释说明农场逻辑
- 遵循MVC架构模式
- 使用事件总线解耦系统
