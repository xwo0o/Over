using Mirror;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string resourceId;
    public int amount;
}

public class Inventory : NetworkBehaviour
{
    public int capacity = 20;
    public int maxStackPerSlot = 40;

    [SyncVar(hook = nameof(OnSlotsChanged))]
    public InventoryItem[] slots;

    void Awake()
    {
        if (slots == null || slots.Length != capacity)
        {
            slots = new InventoryItem[capacity];
            for (int i = 0; i < capacity; i++)
            {
                slots[i] = new InventoryItem();
            }
        }
    }

    void OnSlotsChanged(InventoryItem[] oldSlots, InventoryItem[] newSlots)
    {
        NetworkPlayer player = GetComponent<NetworkPlayer>();
        if (player != null && player.isLocalPlayer)
        {
            InventoryUIController uiController = FindObjectOfType<InventoryUIController>();
            if (uiController != null)
            {
                Debug.Log($"[Inventory] OnSlotsChanged hook触发，刷新UI - 玩家: {player.netId}");
                uiController.Refresh();
                uiController.RefreshPreview();
            }
        }
    }

    public bool CanAdd(string resourceId, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < slots.Length; i++)
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
        for (int i = 0; i < slots.Length; i++)
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
        
        // 重新赋值整个数组以触发SyncVar hook
        InventoryItem[] newSlots = (InventoryItem[])slots.Clone();
        slots = newSlots;
        
        return true;
    }

    public bool HasEnough(string resourceId, int amount)
    {
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
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
        for (int i = 0; i < slots.Length; i++)
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

        InventoryItem[] newSlots = (InventoryItem[])slots.Clone();
        slots = newSlots;
        
        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdSwapSlots(int slotIndex1, int slotIndex2)
    {
        if (slotIndex1 < 0 || slotIndex1 >= slots.Length || slotIndex2 < 0 || slotIndex2 >= slots.Length)
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
            slots[slotIndex1] = new InventoryItem();
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

        InventoryItem[] newSlots = (InventoryItem[])slots.Clone();
        slots = newSlots;
    }
}
