## 问题根源

在 `SyncLocalSlotIndex` 方法中，当 `weaponId <= 0`（空格子）时，代码保持当前的 `localSlotIndex`，而不是更新为正确的槽位索引。

## 修复方案

### 修改 PlayerWeaponController.cs

在 `SyncLocalSlotIndex` 方法中，当 `weaponId <= 0` 时，仍然需要同步 `localSlotIndex`。

**当前代码（错误）：**
```csharp
if (weaponId <= 0)
{
    // 保持当前的localSlotIndex - 错误！
    previewWeaponController.inventoryUI.SetSelectedSlot(localSlotIndex);
    return;
}
```

**修复方案：**
需要传递槽位索引参数，或者通过其他方式获取当前选择的槽位。

### 更好的方案：同步槽位索引

修改 `CmdRequestWeaponSwitch`，不仅同步武器ID，还同步槽位索引。

1. 添加 `currentSlotIndex` SyncVar
2. 在 `CmdRequestWeaponSwitch` 中设置 `currentSlotIndex`
3. 在 `OnWeaponIdChanged` 中使用 `currentSlotIndex` 更新UI

## 修改文件

- [PlayerWeaponController.cs](file:///f:/团结引擎/项目存放/Over/Assets/Scripts/Weapon/PlayerWeaponController.cs)
  - 添加 `currentSlotIndex` SyncVar
  - 修改 `CmdRequestWeaponSwitch` 同步槽位索引
  - 修改 `OnWeaponIdChanged` 和 `SyncLocalSlotIndex` 使用槽位索引