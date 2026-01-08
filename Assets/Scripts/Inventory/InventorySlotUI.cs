using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    public InventoryUIController inventoryUIController;
    
    private Image iconImage;
    private Canvas canvas;
    private GameObject dragIcon;
    private Image dragIconImage;
    private RectTransform dragIconRect;
    private InventorySlotUI originalSlot;
    private bool isDragging = false;

    void Awake()
    {
        iconImage = transform.Find("Image")?.GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        
        // 确保Panel本身有一个Graphic组件来接收拖放事件
        Image panelImage = GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = gameObject.AddComponent<Image>();
            panelImage.color = new Color(1, 1, 1, 0);  // 完全透明
        }
        panelImage.raycastTarget = true;  // 确保可以接收射线检测
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[InventorySlotUI] OnBeginDrag触发 - 格子索引: {slotIndex}");
        
        if (inventoryUIController == null || inventoryUIController.targetInventory == null)
        {
            Debug.LogWarning($"[InventorySlotUI] OnBeginDrag被忽略：inventoryUIController或targetInventory为空");
            return;
        }

        var slotData = inventoryUIController.targetInventory.slots[slotIndex];
        if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
        {
            Debug.LogWarning($"[InventorySlotUI] OnBeginDrag被忽略：格子为空 - 资源ID: {slotData.resourceId}, 数量: {slotData.amount}");
            return;
        }

        isDragging = true;
        originalSlot = this;

        if (iconImage != null && iconImage.sprite != null)
        {
            dragIcon = new GameObject("DragIcon");
            dragIcon.transform.SetParent(canvas.transform, false);
            dragIcon.transform.SetAsLastSibling();
            
            dragIconImage = dragIcon.AddComponent<Image>();
            dragIconImage.sprite = iconImage.sprite;
            dragIconImage.raycastTarget = false;
            
            dragIconRect = dragIcon.GetComponent<RectTransform>();
            dragIconRect.sizeDelta = new Vector2(80, 80);
            
            Color iconColor = dragIconImage.color;
            iconColor.a = 0.7f;
            dragIconImage.color = iconColor;
            
            SetDraggedPosition(eventData);
            
            Debug.Log($"[InventorySlotUI] 开始拖拽 - 格子索引: {slotIndex}, 资源ID: {slotData.resourceId}");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && dragIcon != null)
        {
            SetDraggedPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[InventorySlotUI] OnDrop触发 - 目标格子索引: {slotIndex}");
        
        InventorySlotUI sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
        if (sourceSlot == null || sourceSlot == this)
        {
            Debug.LogWarning($"[InventorySlotUI] OnDrop被忽略：源格子为null或拖拽到自己");
            return;
        }

        if (inventoryUIController == null || inventoryUIController.targetInventory == null)
        {
            Debug.LogWarning($"[InventorySlotUI] OnDrop被忽略：inventoryUIController或targetInventory为空");
            return;
        }

        if (sourceSlot.slotIndex < 0 || sourceSlot.slotIndex >= inventoryUIController.targetInventory.slots.Length ||
            slotIndex < 0 || slotIndex >= inventoryUIController.targetInventory.slots.Length)
        {
            Debug.LogWarning($"[InventorySlotUI] OnDrop被忽略：无效的格子索引 - 源: {sourceSlot.slotIndex}, 目标: {slotIndex}");
            return;
        }

        Debug.Log($"[InventorySlotUI] 调用CmdSwapSlots - 源格子: {sourceSlot.slotIndex}, 目标格子: {slotIndex}");
        inventoryUIController.targetInventory.CmdSwapSlots(sourceSlot.slotIndex, slotIndex);
    }

    private void SetDraggedPosition(PointerEventData eventData)
    {
        if (dragIconRect != null && canvas != null)
        {
            Vector3 globalMousePos;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out globalMousePos))
            {
                dragIconRect.position = globalMousePos;
            }
        }
    }
}