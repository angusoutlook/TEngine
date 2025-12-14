# TEngine UI Bind Component 脚本生成器使用摘要

> 日期：2025-01-27  
> 说明：提取本次会话中的关键信息、代码示例和最佳实践。

---

## 📋 核心要点总结

### 1. 索引绑定机制

**工作原理：**
- 代码生成器通过深度优先遍历（DFS）扫描 UI 层级结构
- 按 Hierarchy 面板中的子节点顺序绑定组件
- 使用索引（0, 1, 2...）访问 `UIBindComponent.m_components` 列表

**关键限制：**
- ⚠️ **对 UI 结构顺序敏感**：修改节点顺序会导致索引错位
- ⚠️ **修改 UI 后必须重新绑定**：点击"重新绑定组件"按钮

**解决方案：**
```csharp
// 重新绑定组件
private void RebindComponents()
{
    if (m_uiBindComponent == null) return;
    m_uiBindComponent.Clear();
    ScriptGenerator.GenerateUIComponentScript();
    ScriptGenerator.GenerateCSharpScript(false);
}
```

---

### 2. 命名规则（区分大小写）

**重要规则：**
- 节点名称必须完全匹配命名规则前缀（区分大小写）
- `m_BtnOK` ❌ 不匹配 `m_btn` 规则
- `m_btnOK` ✅ 匹配 `m_btn` 规则

**常见命名规则：**
- `m_go` / `m_item` → GameObject
- `m_rect` → RectTransform
- `m_text` → Text
- `m_btn` → Button
- `m_img` → Image
- `m_slider` → Slider

**代码实现：**
```csharp
var rule = ScriptGeneratorSetting.GetScriptGenerateRule()
    .Find(r => varName.StartsWith(r.uiElementRegex));  // 区分大小写
```

---

### 3. 四个生成按钮的区别

| 按钮 | includeListener | isUniTask | isAutoGenerate | 输出方式 | 使用场景 |
|------|----------------|-----------|----------------|---------|---------|
| **生成脚本窗口** | ✅ | ❌ | ✅ | 自动保存文件 | 标准 UI 窗口 |
| **生成UniTask脚本本窗口** | ✅ | ✅ | ✅ | 自动保存文件 | 需要异步操作的 UI |
| **生成标准版绑定代码** | ❌ | ❌ | ❌ | 剪贴板 | 只需要组件引用 |
| **生成UniTask代码** | ❌ | ✅ | ❌ | 剪贴板 | UniTask + 手动管理事件 |

**代码差异示例：**

标准版：
```csharp
m_btnOK.onClick.AddListener(OnClickOKBtn);
private partial void OnClickOKBtn();
```

UniTask 版：
```csharp
m_btnOK.onClick.AddListener(UniTask.UnityAction(OnClickOKBtn));
private partial UniTaskVoid OnClickOKBtn();
```

---

### 4. UIWindow vs UIWidget

| 特性 | UIWindow | UIWidget |
|------|----------|----------|
| **用途** | 独立窗口/面板 | 可复用组件 |
| **管理方式** | UIModule 统一管理 | 依附于父 UI |
| **打开方式** | `UIModule.OpenWindow<T>()` | `CreateWidget<T>()` |
| **资源加载** | 独立加载资源 | 资源已存在或动态加载 |
| **代码生成** | 生成 `[Window]` 特性 | 不生成特性 |
| **使用场景** | 主界面、设置面板 | 列表项、页签 |

**代码生成差异：**

UIWindow：
```csharp
[Window(UILayer.UI, location : "TestUI")]
public partial class TestUI : UIWindow
```

UIWidget：
```csharp
public partial class ItemWidget : UIWidget
```

---

### 5. 资源加载和管理机制

**资源加载流程：**
```
UIModule.OpenWindow<T>()
    ↓
UIWindow.InternalLoad()
    ↓
UIModule.Resource.LoadGameObjectAsync()  (IUIResourceLoader)
    ↓
UIResourceLoader.LoadGameObjectAsync()
    ↓
ResourceModule.LoadGameObjectAsync()  (IResourceModule)
    ↓
自动添加 AssetsReference 组件
    ↓
GameObject 实例化到场景
```

**资源管理：**
- 由 `AssetsReference` 组件自动管理
- 在 GameObject 销毁时自动释放资源
- 无需手动调用 `UnloadAsset`

**关键代码：**
```csharp
// UIWindow 资源加载
internal async UniTaskVoid InternalLoad(string location, ...)
{
    var uiInstance = await UIModule.Resource.LoadGameObjectAsync(location, parent: UIModule.UIRoot);
    Handle_Completed(uiInstance);
}

// CreateWidget 资源加载
public T CreateWidgetByPath<T>(Transform parentTrans, string assetLocation, ...)
{
    GameObject goInst = UIModule.Resource.LoadGameObject(assetLocation, parent: parentTrans);
    return CreateWidget<T>(goInst, visible);
}
```

**资源释放流程：**
```csharp
// UIWindow 销毁
internal void InternalDestroy(bool isShutDown = false)
{
    // ... 清理逻辑 ...
    Object.Destroy(_panel);  // 触发 AssetsReference.OnDestroy()
}

// AssetsReference 自动释放
private void OnDestroy()
{
    if (sourceGameObject != null)
    {
        _resourceModule.UnloadAsset(sourceGameObject);
    }
}
```

---

### 6. 组件绑定问题修复

**问题：** 绑定逻辑缺少空值检查，导致 null 引用被添加到列表

**修复代码：**
```csharp
// GameObject 类型绑定
if (rule.componentName == UIComponentName.GameObject)
{
    var c = child.gameObject.GetComponent<RectTransform>();
    if (c == null)
    {
        Debug.LogWarning($"节点 '{child.name}' 上未找到 RectTransform 组件，跳过绑定");
        return;
    }
    uiBindComponent.AddComponent(c);
    return;
}

// 其他组件类型绑定
var com = child.GetComponent(componentType);
if (com == null)
{
    Debug.LogWarning($"节点 '{child.name}' 上未找到组件类型 '{componentType.Name}'，跳过绑定");
    return;
}
uiBindComponent.AddComponent(com);
```

---

### 7. Partial 方法实现问题

**问题：** C# 中带访问修饰符的 partial 方法必须有实现部分

**解决方案：**
```csharp
// 生成的代码（TestUI_Gen.g.cs）
private partial void OnClickOKBtn();

// 实现类（TestUI.cs）
private partial void OnClickOKBtn()
{
    // TODO: 实现按钮点击逻辑
}
```

**注意：** 如果勾选了"生成实现类"，但文件已存在，生成器会跳过。需要删除现有文件后重新生成。

---

### 8. 组件绑定列表的作用

**重要理解：**
- **代码生成阶段**：不依赖红色区域（组件引用列表），通过遍历 UI 层级结构生成代码
- **运行时绑定阶段**：依赖红色区域，使用索引访问组件列表

**两种情况：**
1. **红色区域为空，但 UI 中有符合命名规则的节点**
   - ✅ 代码会生成
   - ❌ 运行时绑定会失败（索引访问报错）

2. **红色区域为空，且 UI 中也没有符合命名规则的节点**
   - ✅ 只生成基本结构
   - ❌ 不会生成组件变量和事件方法

**正确工作流程：**
1. 确保 UI 节点名称符合命名规则
2. 点击"重新绑定组件"
3. 点击"生成脚本窗口"

---

## 🔧 关键代码片段

### 绑定顺序确定逻辑
```csharp
public static void ErgodicUIComponent(Transform root, Transform transform, UIBindComponent uiBindComponent)
{
    for (int i = 0; i < transform.childCount; i++)
    {
        Transform child = transform.GetChild(i);
        WriteScriptUIComponent(root, child, uiBindComponent);

        if (child.name.StartsWith(GetUIWidgetName()))
        {
            continue;
        }

        ErgodicUIComponent(root, child, uiBindComponent);
    }
}
```

### 组件识别逻辑
```csharp
private static void WriteScriptUIComponent(Transform root, Transform child, UIBindComponent uiBindComponent)
{
    string varName = child.name;
    var rule = ScriptGeneratorSetting.GetScriptGenerateRule()
        .Find(r => varName.StartsWith(r.uiElementRegex));  // 区分大小写

    if (rule == null)
    {
        return;
    }
    // ... 绑定逻辑 ...
}
```

### 资源加载器接口
```csharp
public interface IUIResourceLoader
{
    GameObject LoadGameObject(string location, Transform parent = null, string packageName = "");
    UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, CancellationToken cancellationToken = default, string packageName = "");
}

public class UIResourceLoader : IUIResourceLoader
{
    private readonly IResourceModule _resourceLoaderImp = ModuleSystem.GetModule<IResourceModule>();
    
    public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "")
    {
        return _resourceLoaderImp.LoadGameObject(location, parent, packageName);
    }
}
```

---

## ✅ 最佳实践

### 1. UI 节点命名
- ✅ 使用小写前缀：`m_btnOK`, `m_itemTopBar`
- ❌ 避免大写前缀：`m_BtnOK`, `m_ItemTopBar`

### 2. 修改 UI 结构后
1. 立即点击"重新绑定组件"
2. 重新生成脚本代码
3. 检查生成的代码是否正确

### 3. 资源管理
- ✅ 使用 `UIModule.Resource.LoadGameObject`（自动管理）
- ❌ 避免手动管理资源引用

### 4. 代码生成
- ✅ 使用"生成脚本窗口"或"生成UniTask脚本本窗口"（自动保存）
- ✅ 勾选"生成实现类"自动生成实现方法
- ❌ 避免手动修改生成的 `*_Gen.g.cs` 文件

---

## 📁 相关文件

- **UI Bind Component**：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBindComponent/`
- **代码生成器**：`UnityProject/Assets/Editor/UIScriptGenerator/ScriptAutoGenerator.cs`
- **UI 窗口基类**：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs`
- **UI 组件基类**：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWidget.cs`
- **UI 模块**：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`
- **资源加载器**：`UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/IUIResourceLoader.cs`
- **资源引用管理**：`UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Reference/AssetsReference.cs`
- **文档说明**：`Books/3-5-UI模块.md`

---

## 🎯 快速参考

### 常见问题排查

| 问题 | 可能原因 | 解决方案 |
|------|---------|---------|
| 代码生成为空 | 节点名称不符合命名规则 | 检查节点名称大小写 |
| 编译错误 CS8795 | Partial 方法缺少实现 | 删除实现类文件后重新生成 |
| 绑定列表有 null | 节点上缺少对应组件 | 检查节点组件，重新绑定 |
| 运行时绑定失败 | 红色区域为空 | 点击"重新绑定组件" |

### 工作流程检查清单

- [ ] UI 节点名称符合命名规则（小写前缀）
- [ ] 节点上有对应的组件（Button、RectTransform 等）
- [ ] 点击"重新绑定组件"填充组件列表
- [ ] 检查组件绑定列表无 null 引用
- [ ] 点击"生成脚本窗口"生成代码
- [ ] 如果勾选"生成实现类"，确保实现类文件不存在或已删除
