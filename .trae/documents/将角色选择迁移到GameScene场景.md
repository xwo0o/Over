## 需求理解
1. Character场景：保留网络模式选择（主机/客户端）
2. GameScene场景：
   - 默认实例化01角色
   - 按Q键展开角色选择UI面板
   - 面板显示4个角色选择和确认更换按钮
   - 点击确认后更换角色模型（替换Model容器下的模型）

## 实现方案

### 1. 修改 NetworkPlayer.cs
- 添加 CmdSwitchCharacter 命令用于切换角色
- 添加角色切换冷却时间

### 2. 修改 CharacterModelManager.cs  
- 添加 SwitchCharacter 方法支持切换角色模型
- 添加角色切换事件通知

### 3. 创建 InGameCharacterSelectionUI.cs
- 游戏内角色选择UI控制器
- 显示4个角色按钮
- 确认更换按钮
- 按Q键显示/隐藏

### 4. 修改 PlayerInputHandler.cs
- 添加Q键检测打开角色选择面板

### 5. 修改 GameNetworkManager.cs
- 玩家进入GameScene时默认使用01角色

## 文件修改清单
1. NetworkPlayer.cs - 添加角色切换命令
2. CharacterModelManager.cs - 添加切换模型方法
3. 新增 InGameCharacterSelectionUI.cs - 游戏内角色选择UI
4. PlayerInputHandler.cs - 添加Q键打开面板
5. GameNetworkManager.cs - 默认01角色

## 关键逻辑
```csharp
// 切换角色流程
1. 玩家按Q键 → 打开角色选择面板
2. 选择新角色 → 记录选择
3. 点击确认 → 调用 CmdSwitchCharacter
4. 服务器更新 selectedCharacterId
5. CharacterModelManager 重新加载模型
6. 旧模型销毁，新模型实例化
```