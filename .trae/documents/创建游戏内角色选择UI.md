## 方案：对象池预加载角色预览模型

### 核心思路
1. **游戏启动时** - 预加载4个角色模型到对象池
2. **切换预览时** - 从对象池获取/回收，不创建不销毁
3. **只显示当前选中的角色**，其他隐藏

### 对象池设计

```
CharacterPreviewPool (单例)
├── 预加载4个角色模型
│   ├── Role01_Preview (inactive)
│   ├── Role02_Preview (inactive)
│   ├── Role03_Preview (inactive)
│   └── Role04_Preview (inactive)
├── 当前显示的模型引用
└── 预览位置 (Transform)
```

### 需要创建/修改的脚本

1. **CharacterPreviewPool.cs** (新建)
   - 预加载4个角色模型
   - 提供 GetPreviewModel(string characterId) 方法
   - 提供 ReturnPreviewModel(GameObject model) 方法
   - 只激活当前显示的模型

2. **修改 InGameCharacterSelectionUI.cs**
   - 使用 CharacterPreviewPool 获取模型
   - 切换时隐藏旧模型，显示新模型

### 代码结构

```csharp
// CharacterPreviewPool.cs
public class CharacterPreviewPool : MonoBehaviour
{
    public static CharacterPreviewPool Instance;
    
    [Header("预览设置")]
    public Transform previewSpot;      // 预览位置
    public string[] characterIds = { "01", "02", "03", "04" };
    
    private Dictionary<string, GameObject> previewModels = new Dictionary<string, GameObject>();
    private string currentDisplayedId = "";
    
    void Start()
    {
        // 预加载所有角色模型
        PreloadAllCharacters();
    }
    
    void PreloadAllCharacters()
    {
        foreach (var id in characterIds)
        {
            Addressables.LoadAssetAsync<GameObject>(id).Completed += handle =>
            {
                GameObject model = Instantiate(handle.Result, previewSpot);
                model.name = $"Preview_{id}";
                model.SetActive(false); // 初始隐藏
                previewModels[id] = model;
            };
        }
    }
    
    // 显示指定角色
    public void ShowCharacter(string characterId)
    {
        // 隐藏当前
        if (!string.IsNullOrEmpty(currentDisplayedId) && previewModels.ContainsKey(currentDisplayedId))
        {
            previewModels[currentDisplayedId].SetActive(false);
        }
        
        // 显示新的
        if (previewModels.ContainsKey(characterId))
        {
            previewModels[characterId].SetActive(true);
            currentDisplayedId = characterId;
        }
    }
}
```

### UI结构

```
CharacterSelectionCanvas
└── CharacterSelectionPanel
    ├── Background
    ├── TitleText
    ├── CharacterDisplayArea
    │   ├── LeftButton ("<")
    │   ├── CharacterRawImage (Raw Image - 显示Render Texture)
    │   └── RightButton (">")
    ├── SelectedInfoText
    └── ButtonContainer
```

### 场景设置

```
GameScene:
├── [原有内容]
├── CharacterPreviewArea
│   ├── PreviewCamera (Camera -> Render Texture)
│   └── PreviewSpot (空物体 - 模型放置位置)
└── CharacterPreviewPool (挂载脚本，引用PreviewSpot)
```

### 切换流程

```
点击左/右按钮
    ↓
计算新角色ID (循环: 01→02→03→04→01)
    ↓
CharacterPreviewPool.ShowCharacter(newId)
    ↓
隐藏旧模型 (SetActive(false))
显示新模型 (SetActive(true))
    ↓
更新UI显示
```

### 优势

- ✅ 无创建销毁开销
- ✅ 切换瞬间完成
- ✅ 内存占用固定（4个模型）
- ✅ 支持模型旋转动画

### 文件修改清单

1. 新建 CharacterPreviewPool.cs - 对象池管理
2. 修改 InGameCharacterSelectionUI.cs - 使用对象池切换
3. 可选：创建 Render Texture 和 Preview Camera