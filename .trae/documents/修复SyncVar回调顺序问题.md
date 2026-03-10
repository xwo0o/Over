## 问题根源

`OnWeaponIdChanged` 和 `OnSlotIndexChanged` 是两个独立的 SyncVar 回调，执行顺序不确定。

当选择非武器资源时：
1. `OnWeaponIdChanged` 先执行，调用 `EquipResourceVisual()`
2. 但此时 `currentSlotIndex` 还是旧值
3. `OnSlotIndexChanged` 后执行，更新 `currentSlotIndex`
4. 导致 `EquipResourceVisual` 使用错误的槽位索引

## 修复方案

### 方案：在 OnSlotIndexChanged 中调用 EquipResourceVisual

当槽位索引变化时，如果当前是资源（weaponId <= 0），重新调用 `EquipResourceVisual`。

## 修改文件

- [PlayerWeaponController.cs](file:///f:/团结引擎/项目存放/Over/Assets/Scripts/Weapon/PlayerWeaponController.cs)
  - 修改 `OnSlotIndexChanged`，在槽位变化时重新挂载资源