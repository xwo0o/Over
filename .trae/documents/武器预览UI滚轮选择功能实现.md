## 方案概述

基于现有系统架构，实现预览UI的滚轮选择功能：
- 预览UI关联背包前5个格子
- 鼠标滚轮切换选中物品（类似MC）
- 在角色手臂Prop_R骨骼上实例化当前选中物品的3D模型
- 复用现有的 `addressableKey` 字段加载模型

## 需要修改/创建的文件

### 1. 修改现有文件

**Assets/Scripts/Inventory/InventoryUIController.cs**
- 添加滚轮输入检测 (Update方法)
- 添加 `currentSelectedIndex` 字段 (0-4)
- 添加选中高亮显示逻辑 (边框/缩放效果)
- 添加 `OnPreviewSelectionChanged` 事件

### 2. 创建新文件

**Assets/Scripts/Inventory/PreviewWeaponController.cs**
- 订阅InventoryUIController的选择变更事件
- 获取当前选中格子的ResourceData
- 调用WeaponAttachmentSystem切换模型

**Assets/Scripts/Weapon/WeaponAttachmentSystem.cs**
- 查找角色Prop_R骨骼 (HumanBodyBones.RightHand 或 transform.Find)
- 使用Addressables加载3D模型 (复用addressableKey)
- 实例化到骨骼下并设置偏移
- 提供位置/旋转偏移配置

## 详细实现

### InventoryUIController.cs 修改内容

```csharp
// 新增字段
[Header("滚轮选择")]
public int currentSelectedIndex = 0;  // 当前选中索引 0-4
public Color selectedColor = Color.yellow;  // 选中高亮色
public Color normalColor = Color.white;     // 正常颜色
public float selectedScale = 1.2f;          // 选中缩放
public float normalScale = 1f;              // 正常缩放

// 事件
public System.Action<int> OnPreviewSelectionChanged;

// Update中添加
void Update()
{
    // 原有代码...
    
    // 滚轮选择
    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (scroll != 0)
    {
        HandleScrollInput(scroll);
    }
}

// 滚轮处理
void HandleScrollInput(float scrollDelta)
{
    int newIndex = currentSelectedIndex;
    if (scrollDelta > 0) 
        newIndex = (newIndex - 1 + 5) % 5;  // 向上滚动
    else if (scrollDelta < 0) 
        newIndex = (newIndex + 1) % 5;      // 向下滚动
    
    if (newIndex != currentSelectedIndex)
    {
        SetSelectedSlot(newIndex);
    }
}

// 设置选中
void SetSelectedSlot(int index)
{
    currentSelectedIndex = index;
    UpdateSlotHighlight();
    OnPreviewSelectionChanged?.Invoke(index);
}

// 更新高亮显示
void UpdateSlotHighlight()
{
    for (int i = 0; i < previewSlotPanels.Length; i++)
    {
        Transform panel = previewSlotPanels[i].transform;
        Image bgImage = panel.GetComponent<Image>();
        
        if (i == currentSelectedIndex)
        {
            // 选中状态
            panel.localScale = Vector3.one * selectedScale;
            if (bgImage != null) bgImage.color = selectedColor;
        }
        else
        {
            // 正常状态
            panel.localScale = Vector3.one * normalScale;
            if (bgImage != null) bgImage.color = normalColor;
        }
    }
}
```

### PreviewWeaponController.cs 新文件

```csharp
using UnityEngine;

public class PreviewWeaponController : MonoBehaviour
{
    [Header("组件引用")]
    public InventoryUIController inventoryUI;
    public WeaponAttachmentSystem weaponAttachment;
    
    void Start()
    {
        if (inventoryUI != null)
        {
            inventoryUI.OnPreviewSelectionChanged += OnSelectionChanged;
        }
    }
    
    void OnSelectionChanged(int slotIndex)
    {
        // 获取背包数据
        if (inventoryUI.targetInventory == null) return;
        
        var slotData = inventoryUI.targetInventory.slots[slotIndex];
        
        // 空格子，卸下武器
        if (string.IsNullOrEmpty(slotData.resourceId) || slotData.amount <= 0)
        {
            weaponAttachment?.DetachWeapon();
            return;
        }
        
        // 获取资源数据
        ResourceData resourceData = ResourceDatabase.Instance?.GetResource(slotData.resourceId);
        if (resourceData == null) return;
        
        // 切换武器
        weaponAttachment?.AttachWeapon(resourceData.addressableKey);
    }
    
    void OnDestroy()
    {
        if (inventoryUI != null)
        {
            inventoryUI.OnPreviewSelectionChanged -= OnSelectionChanged;
        }
    }
}
```

### WeaponAttachmentSystem.cs 新文件

```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class WeaponAttachmentSystem : MonoBehaviour
{
    [Header("骨骼设置")]
    public Animator characterAnimator;  // 角色Animator
    public string boneName = "Prop_R";  // 骨骼名称
    
    [Header("偏移设置")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    
    private GameObject currentWeaponInstance;
    private AsyncOperationHandle<GameObject> currentHandle;
    
    /// <summary>
    /// 挂载武器
    /// </summary>
    public void AttachWeapon(string addressableKey)
    {
        if (string.IsNullOrEmpty(addressableKey)) return;
        
        // 先卸下当前武器
        DetachWeapon();
        
        // 加载新武器
        currentHandle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        currentHandle.Completed += OnWeaponLoaded;
    }
    
    void OnWeaponLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded) return;
        
        // 查找骨骼
        Transform boneTransform = FindBoneTransform();
        if (boneTransform == null)
        {
            Debug.LogError($"[WeaponAttachmentSystem] 未找到骨骼: {boneName}");
            return;
        }
        
        // 实例化武器
        currentWeaponInstance = Instantiate(handle.Result, boneTransform);
        currentWeaponInstance.transform.localPosition = positionOffset;
        currentWeaponInstance.transform.localRotation = Quaternion.Euler(rotationOffset);
        currentWeaponInstance.transform.localScale = Vector3.one;
        
        Debug.Log($"[WeaponAttachmentSystem] 武器挂载成功: {handle.Result.name}");
    }
    
    /// <summary>
    /// 卸下武器
    /// </summary>
    public void DetachWeapon()
    {
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }
        
        if (currentHandle.IsValid())
        {
            Addressables.Release(currentHandle);
        }
    }
    
    /// <summary>
    /// 查找骨骼变换
    /// </summary>
    Transform FindBoneTransform()
    {
        // 方法1: 通过Animator获取
        if (characterAnimator != null)
        {
            Transform bone = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            if (bone != null) return bone;
        }
        
        // 方法2: 通过名称查找
        return transform.Find(boneName);
    }
    
    void OnDestroy()
    {
        DetachWeapon();
    }
}
```

## 场景配置步骤

1. **InventoryUIController配置**
   - 在Inspector中设置选中颜色/缩放
   - 确保previewPanel有5个Panel子物体

2. **创建PreviewWeaponController**
   - 挂载到Player或UI根物体
   - 引用InventoryUIController
   - 引用WeaponAttachmentSystem

3. **WeaponAttachmentSystem配置**
   - 挂载到Player物体
   - 配置characterAnimator引用
   - 调整positionOffset/rotationOffset

4. **武器模型配置**
   - 确保ResourceData.json中的addressableKey对应3D模型
   - 模型预制体需要有正确的缩放和朝向

## 测试验证项

- [ ] 滚轮滚动正确循环选择0-4格子
- [ ] UI高亮反馈正常显示 (颜色/缩放)
- [ ] 武器模型正确挂载到Prop_R骨骼
- [ ] 切换武器时旧模型正确销毁
- [ ] 空格子时武器正确卸下
- [ ] Addressable资源正确加载/释放