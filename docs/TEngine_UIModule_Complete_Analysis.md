# TEngine UIModule 完整架构分析与使用指南

> 本文档详细分析了 TEngine 框架中 UIModule 模块的设计架构、实现原理和使用方法
> 
> 生成时间：2024年
> 基于 Unity 2022.3.61f1c1

---

## 目录

1. [Module 模块设计架构分析](#一-module-模块设计架构分析)
2. [OnSetWindowVisible 详细解析](#二-onsetwindowvisible-详细解析)
3. [UIModule InternalUpdate 完整流程解析](#三-uimodule-internalupdate-完整流程解析)
4. [UIWindow 和 UIWidget 子对象关系](#四-uiwindow-和-uiwidget-子对象关系)
5. [项目中 UIWindow 和 UIWidget 使用示例](#五-项目中-uiwindow-和-uiwidget-使用示例)
6. [资源路径创建 Widget 和 UIBattleWindow 创建机制](#六-资源路径创建-widget-和-uibattlewindow-创建机制)

---

## 一、Module 模块设计架构分析

### 1.1 整体架构设计

这是一个基于单例模式的 UI 管理模块，采用分层架构和堆栈管理机制。

#### 核心组件层次结构

```
UIModule (单例管理器)
    ├── UIBase (抽象基类)
    │   ├── UIWindow (窗口类)
    │   └── UIWidget (组件类)
    ├── WindowAttribute (特性标记)
    ├── IUIResourceLoader (资源加载接口)
    └── ErrorLogger (错误日志系统)
```

#### 设计模式

- **单例模式**：`UIModule` 继承 `Singleton<UIModule>`，全局唯一
- **模板方法模式**：`UIBase` 定义生命周期钩子（`OnCreate`、`OnRefresh`、`OnUpdate`、`OnDestroy`）
- **策略模式**：`IUIResourceLoader` 抽象资源加载，可替换实现
- **观察者模式**：通过 `GameEventMgr` 处理 UI 事件

### 1.2 核心代码实现

#### UIModule 核心字段

```csharp
public sealed partial class UIModule : Singleton<UIModule>, IUpdate
{
    // 核心字段
    private static Transform _instanceRoot = null;          // UI根节点变换组件
    private bool _enableErrorLog = true;                    // 是否启用错误日志
    private Camera _uiCamera = null;                        // UI专用摄像机
    private readonly List<UIWindow> _uiStack = new List<UIWindow>(128); // 窗口堆栈
    private ErrorLogger _errorLogger;                       // 错误日志记录器

    // 常量定义
    public const int LAYER_DEEP = 2000; 
    public const int WINDOW_DEEP = 100;
    public const int WINDOW_HIDE_LAYER = 2; // Ignore Raycast
    public const int WINDOW_SHOW_LAYER = 5; // UI

    // 资源加载接口
    public static IUIResourceLoader Resource;
}
```

#### 窗口堆栈管理

使用 `List<UIWindow>` 维护窗口堆栈，支持：
- 层级排序与深度计算
- 全屏窗口自动隐藏下层窗口
- 窗口复用机制

### 1.3 层级系统

```csharp
public enum UILayer : int
{
    Bottom = 0,
    UI = 1,
    Top = 2,
    Tips = 3,
    System = 4,
}
```

- **5个预定义层级**：Bottom、UI、Top、Tips、System
- **深度计算公式**：`depth = layer * LAYER_DEEP + windowIndex * WINDOW_DEEP`
- **同层窗口**：按打开顺序递增深度

### 1.4 生命周期管理

窗口生命周期流程：

```
ShowUI → InternalLoad → Handle_Completed → OnWindowPrepare 
    → InternalCreate → InternalRefresh → OnSetWindowVisible
```

#### 关键代码

```csharp
internal async UniTaskVoid InternalLoad(string location, Action<UIWindow> prepareCallback, bool isAsync, System.Object[] userDatas)
{
    _prepareCallback = prepareCallback;
    this._userDatas = userDatas;
    if (!FromResources)
    {
        if (isAsync)
        {
            var uiInstance = await UIModule.Resource.LoadGameObjectAsync(location, parent: UIModule.UIRoot);
            Handle_Completed(uiInstance);
        }
        else
        {
            var uiInstance = UIModule.Resource.LoadGameObject(location, parent: UIModule.UIRoot);
            Handle_Completed(uiInstance);
        }
    }
    else
    {
        GameObject panel = Object.Instantiate(Resources.Load<GameObject>(location), UIModule.UIRoot);
        Handle_Completed(panel);
    }
}
```

### 1.5 资源加载抽象

```csharp
public interface IUIResourceLoader
{
    /// <summary>
    /// 同步加载游戏物体并实例化。
    /// </summary>
    public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "");

    /// <summary>
    /// 异步加载游戏物体并实例化。
    /// </summary>
    public UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, CancellationToken cancellationToken = default, string packageName = "");
}
```

- **接口抽象**：支持同步/异步加载
- **默认实现**：`UIResourceLoader` 使用 `IResourceModule`
- **资源来源**：支持 Resources 和 AssetBundle 两种方式

### 1.6 Update 系统集成

```csharp
public void OnUpdate()
{
    if (_uiStack == null)
    {
        return;
    }

    int count = _uiStack.Count;
    for (int i = 0; i < _uiStack.Count; i++)
    {
        if (_uiStack.Count != count)
        {
            break;
        }

        var window = _uiStack[i];
        window.InternalUpdate();
    }
}
```

- `UIModule` 实现 `IUpdate` 接口，由 `ModuleSystem` 统一轮询
- 遍历窗口堆栈调用 `InternalUpdate`
- 支持 Widget 的嵌套 Update

### 1.7 设计亮点

#### 1. 窗口复用机制

```csharp
private bool TryGetWindow(string windowName,out UIWindow window, params System.Object[] userDatas)
{
    window = null;
    if (IsContains(windowName))
    {
        window = GetWindow(windowName);
        Pop(window); //弹出窗口
        Push(window); //重新压入
        window.TryInvoke(OnWindowPrepare, userDatas);
        
        return true;
    }
    return false;
}
```

- 已存在的窗口直接复用，避免重复创建
- 弹出后重新压入栈顶，刷新数据

#### 2. 智能深度排序

```csharp
private void OnSortWindowDepth(int layer)
{
    int depth = layer * LAYER_DEEP;
    for (int i = 0; i < _uiStack.Count; i++)
    {
        if (_uiStack[i].WindowLayer == layer)
        {
            _uiStack[i].Depth = depth;
            depth += WINDOW_DEEP;
        }
    }
}
```

- 同层窗口按堆栈顺序自动计算深度
- 避免深度冲突

#### 3. 全屏窗口自动隐藏

```csharp
private void OnSetWindowVisible()
{
    bool isHideNext = false;
    for (int i = _uiStack.Count - 1; i >= 0; i--)
    {
        UIWindow window = _uiStack[i];
        if (isHideNext == false)
        {
            if (window.IsHide)
            {
                continue;
            }
            window.Visible = true;
            if (window.IsPrepare && window.FullScreen)
            {
                isHideNext = true;
            }
        }
        else
        {
            window.Visible = false;
        }
    }
}
```

- 全屏窗口打开时，自动隐藏下层窗口
- 关闭后恢复显示

#### 4. Widget 嵌套系统

```csharp
public T CreateWidget<T>(string goPath, bool visible = true) where T : UIWidget, new()
{
    var goRootTrans = FindChild(goPath);

    if (goRootTrans != null)
    {
        return CreateWidget<T>(goRootTrans.gameObject, visible);
    }

    return null;
}
```

- Window 可以创建多个 Widget
- Widget 可以嵌套 Widget
- 统一生命周期管理

---

## 二、OnSetWindowVisible 详细解析

### 2.1 OnSetWindowVisible 方法流程

```csharp
private void OnSetWindowVisible()
{
    bool isHideNext = false;
    for (int i = _uiStack.Count - 1; i >= 0; i--)
    {
        UIWindow window = _uiStack[i];
        if (isHideNext == false)
        {
            if (window.IsHide)
            {
                continue;
            }
            window.Visible = true;
            if (window.IsPrepare && window.FullScreen)
            {
                isHideNext = true;
            }
        }
        else
        {
            window.Visible = false;
        }
    }
}
```

#### 执行逻辑详解

1. **从栈顶向栈底遍历**（`i = _uiStack.Count - 1` 到 `0`）
2. **遇到第一个已准备且全屏的窗口**时，设置 `isHideNext = true`
3. **之后所有窗口**设置为隐藏

#### 示例场景

假设窗口堆栈（从底到顶）：
```
[WindowA(非全屏)] → [WindowB(全屏)] → [WindowC(非全屏)]
```

执行流程：
```
遍历顺序（从顶到底）：
1. WindowC (i=2)
   - isHideNext = false
   - Visible = true
   - FullScreen = false，不设置 isHideNext

2. WindowB (i=1)  
   - isHideNext = false
   - Visible = true
   - FullScreen = true 且 IsPrepare = true
   - 设置 isHideNext = true ✅

3. WindowA (i=0)
   - isHideNext = true
   - Visible = false ❌ (被隐藏)
```

**结果**：WindowB 显示，WindowA 隐藏，WindowC 显示（在 WindowB 之上）

### 2.2 Visible 属性的实现机制

```csharp
/// <summary>
/// 窗口可见性。
/// </summary>
public bool Visible
{
    get
    {
        if (_canvas != null)
        {
            return _canvas.gameObject.layer == UIModule.WINDOW_SHOW_LAYER;
        }
        else
        {
            return false;
        }
    }

    set
    {
        if (_canvas != null)
        {
            int setLayer = value ? UIModule.WINDOW_SHOW_LAYER : UIModule.WINDOW_HIDE_LAYER;
            if (_canvas.gameObject.layer == setLayer)
                return;

            // 显示设置
            _canvas.gameObject.layer = setLayer;
            for (int i = 0; i < _childCanvas.Length; i++)
            {
                _childCanvas[i].gameObject.layer = setLayer;
            }

            // 交互设置
            Interactable = value;

            // 虚函数
            if (_isCreate)
            {
                OnSetVisible(value);
            }
        }
    }
}
```

#### 关键点

1. **使用 Layer 控制显示/隐藏**
   - `WINDOW_SHOW_LAYER = 5`（UI 层）
   - `WINDOW_HIDE_LAYER = 2`（Ignore Raycast 层）

2. **同步子 Canvas 的 Layer**
   ```csharp
   for (int i = 0; i < _childCanvas.Length; i++)
   {
       _childCanvas[i].gameObject.layer = setLayer;
   }
   ```

3. **控制交互性**
   ```csharp
   private bool Interactable
   {
       set
       {
           if (_raycaster != null)
           {
               _raycaster.enabled = value;
               for (int i = 0; i < _childRaycaster.Length; i++)
               {
                   _childRaycaster[i].enabled = value;
               }
           }
       }
   }
   ```

### 2.3 Unity Canvas 显示/隐藏原理

#### Layer 与 Camera Culling Mask 的关系

Unity 的显示机制：

```
Camera.CullingMask (位掩码)
    ↓
决定哪些 Layer 会被渲染
    ↓
GameObject.layer 必须匹配 CullingMask 才能被看到
```

UICamera 配置（典型）：
- **Culling Mask**：只勾选 UI 层（Layer 5）
- **其他层**（如 Layer 2）不在 Culling Mask 中，不会被渲染

#### 显示/隐藏的完整流程

```
设置 window.Visible = true
    ↓
_canvas.gameObject.layer = 5 (UI层)
    ↓
UICamera.CullingMask 包含 Layer 5
    ↓
Canvas 被 UICamera 渲染 ✅ 显示

设置 window.Visible = false
    ↓
_canvas.gameObject.layer = 2 (Ignore Raycast层)
    ↓
UICamera.Culling Mask 不包含 Layer 2
    ↓
Canvas 不被 UICamera 渲染 ❌ 隐藏
```

#### 为什么使用 Layer 而不是 SetActive？

**优势**：
- **性能**：不触发 GameObject 的激活/禁用，减少开销
- **状态保留**：组件状态、动画等保持
- **深度控制**：通过 Layer 配合 Culling Mask 精确控制

**对比**：
```csharp
// 方式1：使用 SetActive（不推荐）
gameObject.SetActive(false);  // 会触发 OnDisable，状态丢失

// 方式2：使用 Layer（推荐，项目采用）
gameObject.layer = WINDOW_HIDE_LAYER;  // 只改变渲染，状态保留
```

### 2.4 GraphicRaycaster 的交互控制

```csharp
/// <summary>
/// 窗口交互性。
/// </summary>
private bool Interactable
{
    get
    {
        if (_raycaster != null)
        {
            return _raycaster.enabled;
        }
        else
        {
            return false;
        }
    }

    set
    {
        if (_raycaster != null)
        {
            _raycaster.enabled = value;
            for (int i = 0; i < _childRaycaster.Length; i++)
            {
                _childRaycaster[i].enabled = value;
            }
        }
    }
}
```

#### GraphicRaycaster 的作用

- 负责 UI 事件检测（点击、悬停等）
- 当 `enabled = false` 时，该 Canvas 下的 UI 不响应事件

#### 显示/隐藏时的交互控制

```csharp
// Visible = true 时
Interactable = true
    ↓
_raycaster.enabled = true
    ↓
UI 可以响应点击事件 ✅

// Visible = false 时
Interactable = false
    ↓
_raycaster.enabled = false
    ↓
UI 不响应点击事件 ❌
```

### 2.5 完整示例流程

假设打开一个全屏窗口：

```csharp
// 1. 打开全屏窗口
UIModule.Instance.ShowUIAsync<FullScreenWindow>();

// 2. 窗口加载完成后，触发 OnWindowPrepare
OnWindowPrepare(window)
    ↓
OnSortWindowDepth(window.WindowLayer)  // 排序深度
    ↓
window.InternalCreate()  // 创建窗口
    ↓
window.InternalRefresh()  // 刷新数据
    ↓
OnSetWindowVisible()  // 设置可见性 ⭐

// 3. OnSetWindowVisible 执行
for (i = _uiStack.Count - 1; i >= 0; i--)
{
    if (window.FullScreen && window.IsPrepare)
    {
        isHideNext = true;  // 标记后续窗口需要隐藏
    }
    
    if (isHideNext)
    {
        window.Visible = false;  // 隐藏下层窗口
    }
    else
    {
        window.Visible = true;   // 显示当前窗口
    }
}

// 4. Visible 属性设置
window.Visible = false
    ↓
_canvas.gameObject.layer = 2  // Ignore Raycast
    ↓
_childCanvas[].gameObject.layer = 2  // 子Canvas也设置
    ↓
_raycaster.enabled = false  // 禁用交互
    ↓
OnSetVisible(false)  // 调用虚函数（子类可重写）
```

---

## 三、UIModule InternalUpdate 完整流程解析

### 3.1 更新系统调用链

```
Unity Update()
    ↓
UpdateDriver.Update()
    ↓
SingletonSystem.OnUpdate()
    ↓
UIModule.OnUpdate()  ⭐ 入口
    ↓
UIWindow.InternalUpdate()
    ↓
UIWidget.InternalUpdate()
```

### 3.2 系统注册流程

```csharp
private static void BuildLifeCycle(object singleton)
{
    Type iUpdate = typeof(IUpdate);
    bool needUpdate = iUpdate.IsInstanceOfType(singleton);
    if (needUpdate && singleton is IUpdate update)
    {
        _updates.Add(update);
    }
}
```

当 `UIModule.Instance` 首次访问时：
- `Singleton<T>` 创建实例并调用 `SingletonSystem.Retain()`
- `BuildLifeCycle()` 检测到实现了 `IUpdate`，加入 `_updates` 列表

```csharp
private static void OnUpdate()
{
    foreach (var update in _updates)
    {
        update.OnUpdate();
    }
}
```

每帧 Unity Update 时，`SingletonSystem.OnUpdate()` 遍历所有 `IUpdate` 对象并调用 `OnUpdate()`。

### 3.3 UIModule.OnUpdate() 实现

```csharp
public void OnUpdate()
{
    if (_uiStack == null)
    {
        return;
    }

    int count = _uiStack.Count;
    for (int i = 0; i < _uiStack.Count; i++)
    {
        if (_uiStack.Count != count)
        {
            break;
        }

        var window = _uiStack[i];
        window.InternalUpdate();
    }
}
```

**关键点**：
1. **遍历窗口堆栈**：从底到顶（`i = 0` 到 `_uiStack.Count - 1`）
2. **堆栈变化检测**：如果在更新过程中堆栈数量变化，立即中断，避免迭代器失效
3. **调用每个窗口的 `InternalUpdate()`**

### 3.4 UIWindow.InternalUpdate() 详细流程

```csharp
internal bool InternalUpdate()
{
    if (!IsPrepare || !Visible)
    {
        return false;
    }

    List<UIWidget> listNextUpdateChild = null;
    if (ListChild != null && ListChild.Count > 0)
    {
        listNextUpdateChild = _listUpdateChild;
        var updateListValid = _updateListValid;
        List<UIWidget> listChild = null;
        if (!updateListValid)
        {
            if (listNextUpdateChild == null)
            {
                listNextUpdateChild = new List<UIWidget>();
                _listUpdateChild = listNextUpdateChild;
            }
            else
            {
                listNextUpdateChild.Clear();
            }

            listChild = ListChild;
        }
        else
        {
            listChild = listNextUpdateChild;
        }

        for (int i = 0; i < listChild.Count; i++)
        {
            var uiWidget = listChild[i];

            if (uiWidget == null)
            {
                continue;
            }

            var needValid = uiWidget.InternalUpdate();

            if (!updateListValid && needValid)
            {
                listNextUpdateChild.Add(uiWidget);
            }
        }

        if (!updateListValid)
        {
            _updateListValid = true;
        }
    }

    bool needUpdate = false;
    if (listNextUpdateChild == null || listNextUpdateChild.Count <= 0)
    {
        _hasOverrideUpdate = true;
        OnUpdate();
        needUpdate = _hasOverrideUpdate;
    }
    else
    {
        OnUpdate();
        needUpdate = true;
    }

    return needUpdate;
}
```

#### 步骤分解

**步骤 1：前置检查**
```csharp
if (!IsPrepare || !Visible)
{
    return false;  // 未准备或不可见，不更新
}
```

**步骤 2：Update 列表缓存机制**
```csharp
var updateListValid = _updateListValid;
List<UIWidget> listChild = null;

if (!updateListValid)  // 列表无效，需要重建
{
    // 清空或创建缓存列表
    listNextUpdateChild.Clear();
    listChild = ListChild;  // 使用完整子列表
}
else  // 列表有效，使用缓存
{
    listChild = listNextUpdateChild;  // 只更新需要更新的Widget
}
```

**设计意图**：
- `_updateListValid = false`：需要重建列表（Widget 增删或调用 `SetUpdateDirty()`）
- `_updateListValid = true`：使用缓存列表，只更新需要更新的 Widget

**步骤 3：遍历子 Widget 并更新**
```csharp
for (int i = 0; i < listChild.Count; i++)
{
    var uiWidget = listChild[i];
    if (uiWidget == null) continue;
    
    var needValid = uiWidget.InternalUpdate();  // 递归更新
    
    if (!updateListValid && needValid)
    {
        listNextUpdateChild.Add(uiWidget);  // 记录需要更新的Widget
    }
}
```

**步骤 4：调用窗口自身的 OnUpdate()**
```csharp
bool needUpdate = false;
if (listNextUpdateChild == null || listNextUpdateChild.Count <= 0)
{
    _hasOverrideUpdate = true;
    OnUpdate();  // 调用虚函数
    needUpdate = _hasOverrideUpdate;  // 检查是否重写了OnUpdate
}
else
{
    OnUpdate();
    needUpdate = true;  // 有子Widget需要更新，窗口也需要更新
}
```

#### `_hasOverrideUpdate` 机制

```csharp
/// <summary>
/// 是否需要Update。
/// </summary>
protected bool _hasOverrideUpdate = true;

/// <summary>
/// 窗口更新。
/// </summary>
protected virtual void OnUpdate()
{
    _hasOverrideUpdate = false;  // 默认实现会设置为false
}
```

- 默认 `OnUpdate()` 会将 `_hasOverrideUpdate` 设置为 `false`
- 子类重写后不设置，保持为 `true`，表示需要持续更新

### 3.5 UIWidget.InternalUpdate() 流程

```csharp
internal bool InternalUpdate()
{
    if (!IsPrepare)
    {
        return false;
    }

    List<UIWidget> listNextUpdateChild = null;
    if (ListChild != null && ListChild.Count > 0)
    {
        listNextUpdateChild = _listUpdateChild;
        var updateListValid = _updateListValid;
        List<UIWidget> listChild = null;
        if (!updateListValid)
        {
            if (listNextUpdateChild == null)
            {
                listNextUpdateChild = new List<UIWidget>();
                _listUpdateChild = listNextUpdateChild;
            }
            else
            {
                listNextUpdateChild.Clear();
            }

            listChild = ListChild;
        }
        else
        {
            listChild = listNextUpdateChild;
        }

        for (int i = 0; i < listChild.Count; i++)
        {
            var uiWidget = listChild[i];
            
            if (uiWidget == null)
            {
                continue;
            }

            var needValid = uiWidget.InternalUpdate();

            if (!updateListValid && needValid)
            {
                listNextUpdateChild.Add(uiWidget);
            }
        }

        if (!updateListValid)
        {
            _updateListValid = true;
        }
    }

    bool needUpdate = false;
    if (listNextUpdateChild is not { Count: > 0 })
    {
        _hasOverrideUpdate = true;
        OnUpdate();
        needUpdate = _hasOverrideUpdate;
    }
    else
    {
        OnUpdate();
        needUpdate = true;
    }

    return needUpdate;
}
```

与 `UIWindow.InternalUpdate()` 逻辑一致，支持 Widget 嵌套 Widget。

### 3.6 SetUpdateDirty() 机制

```csharp
internal void SetUpdateDirty()
{
    _updateListValid = false;
    if (Parent != null)
    {
        Parent.SetUpdateDirty();
    }
}
```

**触发时机**：
- Widget 创建/销毁时
- 动态添加/移除子 Widget 时

**作用**：
- 标记当前 UI 的 Update 列表无效
- 向上传播到父节点，确保父节点也重建列表

### 3.7 完整更新流程图

```
┌─────────────────────────────────────────────────────────┐
│ Unity Update()                                           │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│ UpdateDriver.Update()                                   │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│ SingletonSystem.OnUpdate()                              │
│ foreach (var update in _updates)                        │
│     update.OnUpdate();                                   │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────┐
│ UIModule.OnUpdate()                                     │
│ for (i = 0; i < _uiStack.Count; i++)                    │
│     window.InternalUpdate()                              │
└──────────────────┬──────────────────────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
        ▼                     ▼
┌──────────────────┐  ┌──────────────────┐
│ WindowA          │  │ WindowB          │
│ InternalUpdate() │  │ InternalUpdate() │
└────────┬─────────┘  └────────┬─────────┘
         │                     │
         ▼                     ▼
┌─────────────────────────────────────────┐
│ 1. 检查 IsPrepare && Visible            │
│ 2. 处理 Update 列表缓存                 │
│    - updateListValid ? 用缓存 : 重建    │
└────────┬────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│ 3. 遍历子 Widget                        │
│    for (widget in listChild)            │
│        widget.InternalUpdate() ⬅ 递归   │
└────────┬────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│ 4. 调用 OnUpdate() 虚函数               │
│    - 子类可重写实现自定义更新逻辑        │
└─────────────────────────────────────────┘
```

### 3.8 性能优化设计

#### 1. Update 列表缓存
- 首次遍历所有子 Widget，记录需要更新的
- 后续帧只更新缓存列表中的 Widget
- Widget 增删时通过 `SetUpdateDirty()` 重建列表

#### 2. 条件更新
- 未准备或不可见的窗口/Widget 不更新
- 通过 `_hasOverrideUpdate` 判断是否需要持续更新

#### 3. 堆栈变化保护
```csharp
int count = _uiStack.Count;
for (int i = 0; i < _uiStack.Count; i++)
{
    if (_uiStack.Count != count)  // 检测堆栈变化
    {
        break;  // 立即中断，避免异常
    }
    // ...
}
```

---

## 四、UIWindow 和 UIWidget 子对象关系

### 4.1 代码证据

#### UIBase 中的子对象列表定义

```csharp
/// <summary>
/// UI子组件列表。
/// </summary>
internal readonly List<UIWidget> ListChild = new List<UIWidget>();
```

- `ListChild` 的类型是 `List<UIWidget>`
- **只能存储 `UIWidget` 类型的子对象**

#### CreateWidget 方法的泛型约束

```csharp
public T CreateWidget<T>(string goPath, bool visible = true) where T : UIWidget, new()
{
    var goRootTrans = FindChild(goPath);

    if (goRootTrans != null)
    {
        return CreateWidget<T>(goRootTrans.gameObject, visible);
    }

    return null;
}
```

所有 `CreateWidget` 方法都要求 `where T : UIWidget`，因此**只能创建 `UIWidget` 类型的子对象**。

#### 继承关系

```
UIBase (基类)
    ├── UIWindow (窗口)
    └── UIWidget (组件)
```

- `UIWindow` 继承自 `UIBase`
- `UIWidget` 继承自 `UIBase`
- 两者都使用 `UIBase` 的 `ListChild` 字段

### 4.2 父子关系总结

```
UIWindow (窗口)
    └── 子对象：只能是 UIWidget
        └── 子对象：只能是 UIWidget
            └── 子对象：只能是 UIWidget
                ... (可以无限嵌套)
```

### 4.3 设计意图

- **统一管理**：所有子对象都是 `UIWidget`，统一生命周期和更新
- **支持嵌套**：`UIWidget` 可以嵌套 `UIWidget`，形成树形结构
- **类型安全**：通过泛型约束确保类型正确

---

## 五、项目中 UIWindow 和 UIWidget 使用示例

### 5.1 UIWindow 使用示例

#### 示例 1：基础窗口 - LoginUI

```csharp
using UnityEngine.UI;
using TEngine;
using Log = TEngine.Log;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class LoginUI : UIWindow
    {
        
    }
}
```

**要点**：
- 使用 `[Window(UILayer.UI)]` 标记窗口层级
- 继承 `UIWindow`
- 最简单的窗口实现

#### 示例 2：战斗窗口 - UIBattleWindow（完整功能）

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class UIBattleWindow : UIWindow
    {
        #region 脚本工具生成的代码

        private Text m_textScore;
        private GameObject m_goOverView;
        private Button m_btnRestart;
        private Button m_btnHome;

        protected override void ScriptGenerator()
        {
            m_textScore = FindChildComponent<Text>("ScoreView/m_textScore");
            m_goOverView = FindChild("m_goOverView").gameObject;
            m_btnRestart = FindChildComponent<Button>("m_goOverView/m_btnRestart");
            m_btnHome = FindChildComponent<Button>("m_goOverView/m_btnHome");
            m_btnRestart.onClick.AddListener(UniTask.UnityAction(OnClickRestartBtn));
            m_btnHome.onClick.AddListener(UniTask.UnityAction(OnClickHomeBtn));
        }

        #endregion

        protected override void RegisterEvent()
        {
            AddUIEvent<int>(ActorEventDefine.ScoreChange, OnScoreChange);
            AddUIEvent(ActorEventDefine.GameOver, OnGameOver);
        }

        protected override void OnRefresh()
        {
            m_textScore.text = "Score : 0";
            m_goOverView.SetActive(false);
        }

        #region 事件

        private async UniTaskVoid OnClickRestartBtn()
        {
            await UniTask.Yield();
            await GameModule.Scene.LoadSceneAsync("scene_battle");
    
            BattleSystem.Instance.DestroyRoom();
            BattleSystem.Instance.LoadRoom().Forget();
        }

        private async UniTaskVoid OnClickHomeBtn()
        {
            await UniTask.Yield();
        }

        #endregion

        private void OnScoreChange(int currentScores)
        {
            m_textScore.text = $"Score : {currentScores}";
        }

        private void OnGameOver()
        {
            m_goOverView.SetActive(true);
        }
    }
}
```

**要点**：
- `ScriptGenerator()`：绑定 UI 组件
- `RegisterEvent()`：注册事件监听
- `OnRefresh()`：刷新显示数据
- 按钮点击处理
- 事件响应处理

#### 示例 3：打开窗口

```csharp
// 异步打开
GameModule.UI.ShowUIAsync<UIBattleWindow>();

// 同步打开
GameModule.UI.ShowUI<UIBattleWindow>();

// 等待打开完成
var window = await GameModule.UI.ShowUIAsyncAwait<UIBattleWindow>();

// 关闭窗口
GameModule.UI.CloseUI<UIBattleWindow>();
```

### 5.2 UIWidget 使用示例

#### 示例 1：在 Window 中创建 Widget（通过路径查找）

```csharp
public class ItemListWindow : UIWindow
{
    private List<ItemWidget> _itemWidgets = new List<ItemWidget>();
    
    protected override void ScriptGenerator()
    {
        // 绑定容器节点
        var container = FindChild("ItemContainer");
    }
    
    protected override void OnCreate()
    {
        // 方式1：通过路径查找已存在的 GameObject，创建 Widget
        var itemWidget = CreateWidget<ItemWidget>("ItemContainer/ItemTemplate");
        
        // 方式2：通过 Transform 和路径
        var container = FindChild("ItemContainer");
        var itemWidget2 = CreateWidget<ItemWidget>(container, "ItemTemplate");
        
        // 方式3：直接通过 GameObject
        var itemGo = FindChild("ItemContainer/ItemTemplate").gameObject;
        var itemWidget3 = CreateWidget<ItemWidget>(itemGo);
    }
}
```

#### 示例 2：通过资源路径创建 Widget

```csharp
public class ShopWindow : UIWindow
{
    private List<ShopItemWidget> _shopItems = new List<ShopItemWidget>();
    
    protected override void OnCreate()
    {
        var container = FindChild("ShopItemContainer");
        
        // 通过资源路径创建 Widget（从 AssetBundle 加载）
        for (int i = 0; i < 10; i++)
        {
            var itemWidget = CreateWidgetByPath<ShopItemWidget>(
                container, 
                "UI/ShopItemWidget"  // 资源路径
            );
            
            if (itemWidget != null)
            {
                itemWidget.SetData(i);
                _shopItems.Add(itemWidget);
            }
        }
    }
}
```

#### 示例 3：通过 Prefab 创建 Widget

```csharp
public class InventoryWindow : UIWindow
{
    private GameObject _itemPrefab;
    private List<InventoryItemWidget> _items = new List<InventoryItemWidget>();
    
    protected override void ScriptGenerator()
    {
        // 假设预制体已经在场景中
        _itemPrefab = FindChild("ItemPrefab").gameObject;
    }
    
    protected override void OnCreate()
    {
        var container = FindChild("ItemContainer");
        
        // 通过 Prefab 创建 Widget
        for (int i = 0; i < 20; i++)
        {
            var itemWidget = CreateWidgetByPrefab<InventoryItemWidget>(
                _itemPrefab, 
                container
            );
            
            if (itemWidget != null)
            {
                itemWidget.Init(i);
                _items.Add(itemWidget);
            }
        }
    }
}
```

#### 示例 4：动态调整 Widget 数量（常用模式）

```csharp
public class PlayerListWindow : UIWindow
{
    private List<PlayerInfoWidget> _playerWidgets = new List<PlayerInfoWidget>();
    private Transform _container;
    
    protected override void ScriptGenerator()
    {
        _container = FindChild("PlayerContainer");
    }
    
    protected override void OnCreate()
    {
        RefreshPlayerList(new List<PlayerData>());
    }
    
    // 根据数据动态调整 Widget 数量
    public void RefreshPlayerList(List<PlayerData> players)
    {
        // 自动调整数量：如果不够就创建，多了就销毁
        AdjustIconNum<PlayerInfoWidget>(
            _playerWidgets,           // Widget 列表
            players.Count,             // 目标数量
            _container,                // 父节点
            null,                      // Prefab（null 表示通过类型名加载）
            "UI/PlayerInfoWidget"      // 资源路径
        );
        
        // 更新每个 Widget 的数据
        for (int i = 0; i < players.Count; i++)
        {
            _playerWidgets[i].SetPlayerData(players[i]);
        }
    }
}
```

#### 示例 5：异步创建 Widget

```csharp
public class AsyncLoadWindow : UIWindow
{
    protected override async void OnCreate()
    {
        var container = FindChild("Container");
        
        // 异步加载 Widget
        var widget = await CreateWidgetByPathAsync<HeavyWidget>(
            container,
            "UI/HeavyWidget"
        );
        
        if (widget != null)
        {
            widget.Initialize();
        }
    }
}
```

#### 示例 6：Widget 嵌套 Widget（多层嵌套）

```csharp
// 外层 Widget
public class CategoryWidget : UIWidget
{
    private List<ItemWidget> _items = new List<ItemWidget>();
    
    protected override void OnCreate()
    {
        var itemContainer = FindChild("ItemContainer");
        
        // Widget 创建子 Widget
        for (int i = 0; i < 5; i++)
        {
            var item = CreateWidget<ItemWidget>(itemContainer, $"Item_{i}");
            _items.Add(item);
        }
    }
}

// 在 Window 中使用
public class ShopWindow : UIWindow
{
    protected override void OnCreate()
    {
        // Window 创建 Widget
        var category = CreateWidget<CategoryWidget>("CategoryContainer/Category_1");
        
        // CategoryWidget 内部会创建 ItemWidget
        // 形成：Window -> CategoryWidget -> ItemWidget 的嵌套结构
    }
}
```

### 5.3 完整使用场景示例

#### 场景 1：背包窗口（Window + 多个 Widget）

```csharp
[Window(UILayer.UI, location: "UI/InventoryWindow")]
public class InventoryWindow : UIWindow
{
    private List<ItemSlotWidget> _slots = new List<ItemSlotWidget>();
    private Button m_btnClose;
    private Text m_textGold;
    
    protected override void ScriptGenerator()
    {
        m_btnClose = FindChildComponent<Button>("m_btnClose");
        m_textGold = FindChildComponent<Text>("TopBar/m_textGold");
        m_btnClose.onClick.AddListener(() => Close());
    }
    
    protected override void OnCreate()
    {
        var container = FindChild("SlotContainer");
        
        // 创建 30 个物品槽位 Widget
        for (int i = 0; i < 30; i++)
        {
            var slot = CreateWidgetByType<ItemSlotWidget>(container);
            slot.SetSlotIndex(i);
            _slots.Add(slot);
        }
    }
    
    protected override void OnRefresh()
    {
        // 刷新金币显示
        m_textGold.text = $"Gold: {PlayerData.Instance.Gold}";
        
        // 刷新所有槽位
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].RefreshItem(InventoryManager.GetItem(i));
        }
    }
}

// 物品槽位 Widget
public class ItemSlotWidget : UIWidget
{
    private Image m_imgIcon;
    private Text m_textCount;
    private int _slotIndex;
    
    protected override void ScriptGenerator()
    {
        m_imgIcon = FindChildComponent<Image>("m_imgIcon");
        m_textCount = FindChildComponent<Text>("m_textCount");
    }
    
    public void SetSlotIndex(int index)
    {
        _slotIndex = index;
    }
    
    public void RefreshItem(ItemData item)
    {
        if (item != null)
        {
            m_imgIcon.sprite = item.Icon;
            m_textCount.text = item.Count > 1 ? item.Count.ToString() : "";
            Visible = true;
        }
        else
        {
            Visible = false;
        }
    }
}
```

#### 场景 2：聊天窗口（Window + 动态 Widget 列表）

```csharp
[Window(UILayer.UI)]
public class ChatWindow : UIWindow
{
    private List<ChatMessageWidget> _messages = new List<ChatMessageWidget>();
    private Transform _messageContainer;
    private InputField m_inputField;
    private Button m_btnSend;
    
    protected override void ScriptGenerator()
    {
        _messageContainer = FindChild("MessageContainer");
        m_inputField = FindChildComponent<InputField>("InputBar/m_inputField");
        m_btnSend = FindChildComponent<Button>("InputBar/m_btnSend");
        m_btnSend.onClick.AddListener(OnSendMessage);
    }
    
    protected override void RegisterEvent()
    {
        AddUIEvent<string>(ChatEventDefine.NewMessage, OnNewMessage);
    }
    
    private void OnSendMessage()
    {
        string text = m_inputField.text;
        if (!string.IsNullOrEmpty(text))
        {
            ChatManager.SendMessage(text);
            m_inputField.text = "";
        }
    }
    
    private void OnNewMessage(string message)
    {
        // 动态创建消息 Widget
        var messageWidget = CreateWidgetByPath<ChatMessageWidget>(
            _messageContainer,
            "UI/ChatMessageWidget"
        );
        
        if (messageWidget != null)
        {
            messageWidget.SetMessage(message);
            _messages.Add(messageWidget);
            
            // 限制消息数量
            if (_messages.Count > 50)
            {
                var oldMessage = _messages[0];
                _messages.RemoveAt(0);
                oldMessage.Destroy();
            }
        }
    }
}
```

### 5.4 常用模式总结

| 使用场景 | 推荐方法 | 示例 |
|---------|---------|------|
| 窗口已存在 GameObject | `CreateWidget<T>(string path)` | 模板节点创建 Widget |
| 从资源加载 Widget | `CreateWidgetByPath<T>()` | 动态加载列表项 |
| 使用 Prefab | `CreateWidgetByPrefab<T>()` | 复用预制体 |
| 动态数量列表 | `AdjustIconNum<T>()` | 背包、商店列表 |
| 异步加载 | `CreateWidgetByPathAsync<T>()` | 大型 Widget |
| Widget 嵌套 | Widget 内调用 `CreateWidget` | 复杂 UI 结构 |

### 5.5 最佳实践

1. **窗口职责**：管理整体布局和逻辑
2. **Widget 职责**：可复用的 UI 组件
3. **生命周期**：在 `OnCreate()` 中创建 Widget
4. **数据刷新**：在 `OnRefresh()` 中更新显示
5. **事件管理**：在 `RegisterEvent()` 中注册，在 `OnDestroy()` 中清理
6. **资源管理**：使用 `AdjustIconNum` 管理动态列表，避免频繁创建销毁

---

## 六、资源路径创建 Widget 和 UIBattleWindow 创建机制

### 6.1 通过资源路径创建 Widget 的资源类型

#### 资源是 Prefab（GameObject 预制体），不是 Script

```csharp
public T CreateWidgetByPath<T>(Transform parentTrans, string assetLocation, bool visible = true) where T : UIWidget, new()
{
    GameObject goInst = UIModule.Resource.LoadGameObject(assetLocation, parent: parentTrans);
    return CreateWidget<T>(goInst, visible);
}
```

**要点**：
1. `LoadGameObject()` 返回 `GameObject`，说明加载的是 Prefab
2. 资源路径指向 Unity Prefab 资源文件（如 `"UI/ShopItemWidget"`）
3. Script（C# 类）是逻辑代码，Prefab 是 UI 资源

#### 资源加载流程

```csharp
public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "")
{
    return _resourceLoaderImp.LoadGameObject(location, parent, packageName);
}
```

最终调用 `ResourceModule.LoadGameObject()`，从 AssetBundle 或 Resources 加载 Prefab 并实例化。

### 6.2 UIBattleWindow 的创建流程

#### 1. 窗口类定义

```csharp
[Window(UILayer.UI)]
class UIBattleWindow : UIWindow
{
    // ...
}
```

**要点**：
- 使用 `[Window(UILayer.UI)]` 特性
- 没有指定 `Location`，默认使用类名作为资源路径

#### 2. 资源路径确定

```csharp
if (attribute != null)
{
    string assetName = string.IsNullOrEmpty(attribute.Location) ? type.Name : attribute.Location;
    window.Init(type.FullName, attribute.WindowLayer, attribute.FullScreen, assetName, attribute.FromResources, attribute.HideTimeToClose);
}
```

- `Location` 为空时，使用 `type.Name`（即 `"UIBattleWindow"`）
- 资源路径为 `"UIBattleWindow"`

#### 3. 创建流程

```csharp
private void ShowUIImp<T>(bool isAsync, params System.Object[] userDatas) where T : UIWindow , new()
{
    Type type = typeof(T);
    string windowName = type.FullName;

    if (!TryGetWindow(windowName, out UIWindow window, userDatas))
    {
        window = CreateInstance<T>();
        Push(window); //首次压入
        window.InternalLoad(window.AssetName, OnWindowPrepare, isAsync, userDatas).Forget();
    }
}
```

**步骤**：
1. `CreateInstance<T>()` 创建 `UIBattleWindow` 实例
2. `window.AssetName = "UIBattleWindow"`
3. `InternalLoad("UIBattleWindow", ...)` 加载资源

#### 4. 资源加载

```csharp
internal async UniTaskVoid InternalLoad(string location, Action<UIWindow> prepareCallback, bool isAsync, System.Object[] userDatas)
{
    _prepareCallback = prepareCallback;
    this._userDatas = userDatas;
    if (!FromResources)
    {
        if (isAsync)
        {
            var uiInstance = await UIModule.Resource.LoadGameObjectAsync(location, parent: UIModule.UIRoot);
            Handle_Completed(uiInstance);
        }
        else
        {
            var uiInstance = UIModule.Resource.LoadGameObject(location, parent: UIModule.UIRoot);
            Handle_Completed(uiInstance);
        }
    }
    else
    {
        GameObject panel = Object.Instantiate(Resources.Load<GameObject>(location), UIModule.UIRoot);
        Handle_Completed(panel);
    }
}
```

- `FromResources = false`，使用 AssetBundle 加载
- 调用 `LoadGameObjectAsync("UIBattleWindow", parent: UIRoot)`
- 加载并实例化 `UIBattleWindow.prefab` 到 `UIRoot` 下

#### 5. 资源文件位置

根据搜索结果，Prefab 文件位于：
```
UnityProject/Assets/AssetRaw/UI/UIBattleWindow.prefab
```

### 6.3 完整创建流程图

```
调用：GameModule.UI.ShowUIAsync<UIBattleWindow>()
    ↓
ShowUIImp<UIBattleWindow>()
    ↓
CreateInstance<UIBattleWindow>()
    ├─ 读取 [Window(UILayer.UI)] 特性
    ├─ Location 为空，使用 type.Name = "UIBattleWindow"
    └─ window.Init(..., assetName: "UIBattleWindow", ...)
    ↓
Push(window)  // 压入窗口堆栈
    ↓
window.InternalLoad("UIBattleWindow", ...)
    ↓
UIModule.Resource.LoadGameObjectAsync("UIBattleWindow", UIRoot)
    ↓
ResourceModule 从 AssetBundle 加载 Prefab
    ↓
实例化 GameObject 到 UIRoot 下
    ↓
Handle_Completed(uiInstance)
    ├─ _panel = uiInstance  // 保存引用
    ├─ 获取 Canvas 组件
    ├─ IsPrepare = true
    └─ 调用 OnWindowPrepare 回调
    ↓
OnWindowPrepare(window)
    ├─ OnSortWindowDepth()  // 排序深度
    ├─ window.InternalCreate()  // 创建窗口
    │   ├─ ScriptGenerator()  // 绑定 UI 组件
    │   ├─ BindMemberProperty()
    │   ├─ RegisterEvent()  // 注册事件
    │   └─ OnCreate()
    ├─ window.InternalRefresh()  // 刷新数据
    │   └─ OnRefresh()
    └─ OnSetWindowVisible()  // 设置可见性
```

### 6.4 资源路径的两种方式

#### 方式 1：使用类名（默认）

```csharp
[Window(UILayer.UI)]  // Location 为空
class UIBattleWindow : UIWindow
{
    // 资源路径 = "UIBattleWindow"
}
```

#### 方式 2：指定资源路径

```csharp
[Window(UILayer.UI, location: "UI/Battle/BattleWindow")]  // 指定路径
class UIBattleWindow : UIWindow
{
    // 资源路径 = "UI/Battle/BattleWindow"
}
```

### 6.5 Widget 和 Window 的资源对比

| 类型 | 资源类型 | 资源路径示例 | 说明 |
|------|---------|------------|------|
| **UIWindow** | Prefab | `"UIBattleWindow"` 或 `"UI/BattleWindow"` | 窗口的 Prefab 资源 |
| **UIWidget** | Prefab | `"UI/ShopItemWidget"` | Widget 的 Prefab 资源 |
| **Script** | C# 类 | `UIBattleWindow.cs` | 逻辑代码，不是资源 |

### 6.6 总结

1. **通过资源路径创建 Widget 时，资源是 Prefab（GameObject 预制体），不是 Script**
2. **UIBattleWindow 的创建**：
   - 类定义：`[Window(UILayer.UI)] class UIBattleWindow : UIWindow`
   - 资源路径：默认使用类名 `"UIBattleWindow"`
   - 资源文件：`UIBattleWindow.prefab`（位于 `AssetRaw/UI/`）
   - 加载方式：通过 `ResourceModule` 从 AssetBundle 异步加载
   - 实例化：加载后实例化到 `UIRoot` 下

**设计要点**：
- **逻辑与视图分离**：Script 是逻辑，Prefab 是视图
- **约定优于配置**：默认使用类名作为资源路径
- **支持自定义路径**：通过 `WindowAttribute.Location` 指定

这种设计便于管理 UI 资源，并保持代码与资源的对应关系。

---

## 附录：关键代码文件位置

- `UIModule.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`
- `UIWindow.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs`
- `UIWidget.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWidget.cs`
- `UIBase.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs`
- `WindowAttribute.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs`
- `IUIResourceLoader.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/IUIResourceLoader.cs`
- `UIBattleWindow.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Demo/UI/UIBattleWindow.cs`
- `SingletonSystem.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/SingletonSystem/SingletonSystem.cs`

---

## 文档信息

- **生成时间**：2024年
- **Unity 版本**：2022.3.61f1c1
- **框架版本**：TEngine
- **文档类型**：架构分析与使用指南

---

*本文档基于实际代码分析生成，包含完整的架构设计、实现原理和使用示例。*
