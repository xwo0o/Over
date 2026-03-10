# Inventory 初始化时机修复 - 验证清单

- [x] Inventory.OnStartServer() 正确初始化 slots
- [x] Inventory.Awake() 不干扰网络同步
- [x] 客户端 Inventory 从服务器正确同步数据
- [x] NetworkPlayer.InitializeInventoryWeapons() 在正确时机调用
- [x] 主机玩家背包初始化包含武器
- [x] 客户端玩家背包同步包含武器
- [x] 客户端玩家收集资源后背包数据正确增加
