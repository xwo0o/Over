## 需要添加的设计内容

### 1. 武器数据JSON配置

**文件**: `Assets/StreamingAssets/WeaponData.json`

```json
{
  "Weapons": [
    {
      "weaponId": 1,
      "weaponName": "阔刀",
      "weaponType": "单手武器",
      "modelAddressableKey": "WGS",
      "spriteAddressableKey": "WGS.png",
      "boneNames": ["Prop_R"],
      "damage": 80,
      "comboStages": 3,
      "comboWindowStart": 0.15,
      "comboWindowEnd": 0.6,
      "attackAnimationNames": ["WGS_A1", "WGS_A2", "WGS_A3"]
    },
    {
      "weaponId": 2,
      "weaponName": "双刀",
      "weaponType": "双手武器",
      "modelAddressableKey": "WTD",
      "spriteAddressableKey": "WTD.png",
      "boneNames": ["Prop_R", "Prop_L"],
      "damage": 50,
      "comboStages": 3,
      "comboWindowStart": 0.12,
      "comboWindowEnd": 0.5,
      "attackAnimationNames": ["WTD_A1", "WTD_A2", "WTD_A3"]
    },
    {
      "weaponId": 3,
      "weaponName": "斧头",
      "weaponType": "单手武器",
      "modelAddressableKey": "Axe",
      "spriteAddressableKey": "Axe.png",
      "boneNames": ["Prop_R"],
      "damage": 80,
      "comboStages": 1,
      "comboWindowStart": 0.0,
      "comboWindowEnd": 0.0,
      "attackAnimationNames": ["Axe_Spin"]
    }
  ]
}
```

### 2. 武器数据库 (WeaponDatabase.cs)

**路径**: `Assets/Scripts/Data/WeaponDatabase.cs`

- 从JSON加载武器配置
- 提供按ID查询武器数据
- 单例模式管理

### 3. 伤害计算系统

**路径**: `Assets/Scripts/Weapon/WeaponDamageSystem.cs`

- 基础伤害 = 武器伤害值
- 连击伤害递增（每段+20%）
- 暴击判定
- 伤害类型（物理/元素）

### 4. 武器切换与装备系统

**修改**: `WeaponAttachmentSystem.cs`
- 支持双手武器（双刀同时挂载到左右手）
- 武器切换时更新Animator参数
- 同步武器状态到网络

### 5. 武器状态管理

**新增**: `WeaponStateManager.cs`
- 当前装备武器ID
- 当前连击段数
- 连击窗口状态
- 攻击冷却管理

## 实现步骤

1. 创建WeaponData.json配置文件
2. 创建WeaponDatabase.cs数据管理器
3. 创建WeaponDamageSystem.cs伤害计算
4. 修改WeaponAttachmentSystem支持双手武器
5. 创建WeaponStateManager.cs状态管理
6. 集成到现有攻击系统中