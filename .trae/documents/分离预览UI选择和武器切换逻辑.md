## 问题分析

当前代码的问题：
1. `HandleLocalInput` 只发送 `CmdRequestWeaponSwitch`，不立即更新UI
2. 当切换到空格子/非武器资源时，`weaponId = -1`
3. `SyncLocalSlotIndex(-1)` 将 `localSlotIndex` 设为 0，不更新UI选中状态

## 修复方案

修改 `HandleLocalInput` 方法：
1. 鼠标滚轮立即更新 `localSlotIndex`
2. 立即调用 `previewWeaponController.inventoryUI.SetSelectedSlot(newIndex)` 更新UI
3. 然后发送 `CmdRequestWeaponSwitch` 请求（用于武器切换）

这样预览UI切换是即时的、无限制的，武器切换是后台的、有条件的。

## 具体修改

**文件**: PlayerWeaponController.cs
**方法**: HandleLocalInput()

修改内容：
- 先更新 localSlotIndex
- 立即更新UI选中状态
- 再发送武器切换请求