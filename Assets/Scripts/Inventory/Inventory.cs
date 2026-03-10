using Mirror;
using UnityEngine;

// 必须是结构体才能正确被 Mirror SyncList 序列化和同步
// 结构体是值类型，修改时会触发 SyncList 的变化检测
[System.Serializable]
public struct InventoryItem
{
    public string resourceId;
    public int amount;
    
    // 提供一个静态的空实例，用于初始化空槽位
    public static InventoryItem Empty => new InventoryItem { resourceId = null, amount = 0 };
    
    // 检查槽位是否为空
    public bool IsEmpty => string.IsNullOrEmpty(resourceId) || amount <= 0;
}

public class Inventory : NetworkBehaviour
{
    public int capacity = 20;
    public int maxStackPerSlot = 40;

    public readonly SyncList<InventoryItem> slots = new SyncList<InventoryItem>();
    
    // 本地数据变化事件（用于主机模式下的立即刷新）
    public event System.Action OnDataChanged;
    
    // 标记是否已初始化
    private bool isInitialized = false;

    void Awake()
    {
        // 注意：不要在 Awake 中初始化 slots！
        // 因为此时 NetworkBehaviour 的网络状态（isServer/isClient）可能还未初始化
        // 服务器端初始化在 OnStartServer() 中进行
        // 客户端通过 SyncList 自动同步服务器数据
        Debug.Log($"[Inventory] Awake被调用 - slots.Count={(slots != null ? slots.Count : 0)}, capacity={capacity}");
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"[Inventory] OnStartServer被调用 - slots.Count={slots.Count}");
        
        // 服务器端初始化 slots（这是正确的时机，此时网络状态已初始化）
        InitializeSlots();
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"[Inventory] OnStartClient被调用 - isLocalPlayer: {isLocalPlayer}, slots.Count={slots.Count}");
        
        // 客户端不需要初始化 slots，等待服务器同步
        // SyncList 会自动从服务器同步数据
    }
    
    /// <summary>
    /// 初始化背包槽位（仅在服务器端调用）
    /// </summary>
    [Server]
    void InitializeSlots()
    {
        if (isInitialized)
        {
            Debug.Log($"[Inventory] 已初始化，跳过");
            return;
        }
        
        Debug.Log($"[Inventory] 服务器端初始化slots列表 - 容量: {capacity}");
        slots.Clear();
        for (int i = 0; i < capacity; i++)
        {
            // 使用 Empty 静态属性创建空槽位
            slots.Add(InventoryItem.Empty);
        }
        isInitialized = true;
    }
    
    /// <summary>
    /// 通知本地玩家UI刷新
    /// </summary>
    void NotifyDataChanged()
    {
        OnDataChanged?.Invoke();
    }

    public bool CanAdd(string resourceId, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (string.IsNullOrEmpty(slot.resourceId) || slot.resourceId == resourceId)
            {
                int current = slot.amount;
                int space = maxStackPerSlot - current;
                if (space > 0)
                {
                    remaining -= space;
                    if (remaining <= 0)
                        return true;
                }
            }
        }
        return remaining <= 0;
    }

    [Server]
    public bool Add(string resourceId, int amount)
    {
        if (!CanAdd(resourceId, amount))
        {
            Debug.LogWarning($"[Inventory] Add失败：无法添加资源 - 资源ID: {resourceId}, 数量: {amount}");
            return false;
        }

        Debug.Log($"[Inventory] 开始添加资源 - 资源ID: {resourceId}, 数量: {amount}");
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
                slots[i] = slot;
                remaining -= toAdd;
                Debug.Log($"[Inventory] 格子[{i}] - 资源ID: {resourceId}, 原数量: {current}, 添加: {toAdd}, 新数量: {slot.amount}");
                if (remaining <= 0)
                    break;
            }
        }

        Debug.Log($"[Inventory] 添加资源完成 - 资源ID: {resourceId}, 总添加数量: {amount}");
        
        // 通知本地玩家UI刷新（解决主机模式下SyncList.Callback不触发的问题）
        NotifyDataChanged();
        
        return true;
    }

    public bool HasEnough(string resourceId, int amount)
    {
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.resourceId == resourceId)
            {
                total += slot.amount;
            }
        }
        return total >= amount;
    }

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
                if (remaining <= 0)
                    break;
            }
        }
        
        // 通知本地玩家UI刷新
        NotifyDataChanged();
        
        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdSwapSlots(int slotIndex1, int slotIndex2)
    {
        if (slotIndex1 < 0 || slotIndex1 >= slots.Count || slotIndex2 < 0 || slotIndex2 >= slots.Count)
        {
            Debug.LogWarning($"[Inventory] CmdSwapSlots失败：无效的格子索引 - slot1: {slotIndex1}, slot2: {slotIndex2}");
            return;
        }

        if (slotIndex1 == slotIndex2)
        {
            return;
        }

        InventoryItem sourceSlot = slots[slotIndex1];
        InventoryItem targetSlot = slots[slotIndex2];

        // 检查源格子是否有资源
        bool sourceHasItem = !string.IsNullOrEmpty(sourceSlot.resourceId) && sourceSlot.amount > 0;
        // 检查目标格子是否为空
        bool targetIsEmpty = string.IsNullOrEmpty(targetSlot.resourceId) || targetSlot.amount <= 0;

        if (!sourceHasItem)
        {
            Debug.LogWarning($"[Inventory] CmdSwapSlots失败：源格子[{slotIndex1}]为空，无法移动");
            return;
        }

        if (targetIsEmpty)
        {
            // 目标格子为空，直接移动资源
            slots[slotIndex2] = sourceSlot;
            slots[slotIndex1] = InventoryItem.Empty;
            Debug.Log($"[Inventory] 资源移动成功 - 格子[{slotIndex1}] -> 格子[{slotIndex2}], 资源ID: {sourceSlot.resourceId}, 数量: {sourceSlot.amount}");
        }
        else
        {
            // 目标格子有资源，交换两个格子
            InventoryItem temp = slots[slotIndex1];
            slots[slotIndex1] = slots[slotIndex2];
            slots[slotIndex2] = temp;
            Debug.Log($"[Inventory] 格子交换成功 - 格子[{slotIndex1}] <-> 格子[{slotIndex2}]");
        }
        
        // 通知本地玩家UI刷新
        NotifyDataChanged();
    }

    /// <summary>
    /// 消耗食物恢复血量 - 服务器权威命令
    /// </summary>
    /// <param name="slotIndex">食物所在的格子索引</param>
    [Command(requiresAuthority = true)]
    public void CmdConsumeFood(int slotIndex)
    {
        // 验证格子索引
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            Debug.LogWarning($"[Inventory] CmdConsumeFood失败：无效的格子索引 {slotIndex}");
            return;
        }

        var slot = slots[slotIndex];
        
        // 检查格子是否有物品
        if (string.IsNullOrEmpty(slot.resourceId) || slot.amount <= 0)
        {
            Debug.LogWarning($"[Inventory] CmdConsumeFood失败：格子[{slotIndex}]为空");
            return;
        }

        // 从资源数据库获取资源数据
        if (ResourceDatabase.Instance == null)
        {
            Debug.LogWarning("[Inventory] CmdConsumeFood失败：ResourceDatabase.Instance为null");
            return;
        }

        ResourceData resourceData = ResourceDatabase.Instance.GetResource(slot.resourceId);
        if (resourceData == null)
        {
            Debug.LogWarning($"[Inventory] CmdConsumeFood失败：未找到资源数据 {slot.resourceId}");
            return;
        }

        // 验证是否为食物资源
        if (resourceData.type != "食物资源")
        {
            Debug.Log($"[Inventory] CmdConsumeFood：格子[{slotIndex}]的物品 {slot.resourceId} 不是食物资源，类型为 {resourceData.type}");
            return;
        }

        // 验证是否有血量可以恢复
        CharacterStats stats = GetComponentInChildren<CharacterStats>();
        if (stats == null)
        {
            Debug.LogWarning("[Inventory] CmdConsumeFood失败：未找到CharacterStats组件");
            return;
        }

        // 如果血量已满，不消耗食物
        if (stats.currentHealth >= stats.maxHealth)
        {
            Debug.Log("[Inventory] CmdConsumeFood：血量已满，无需恢复");
            return;
        }

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

        Debug.Log($"[Inventory] 消耗食物成功 - 格子[{slotIndex}], 资源: {resourceData.name}, 恢复血量: {healAmount}");
        
        // 通知本地玩家UI刷新
        NotifyDataChanged();
    }
}
