TEngine DebuggerModule 模块讨论总结
===============================

> 说明：本文件是对 `TEngine_DebuggerModule_Conversation_Raw.md` 的精简总结，保留关键信息与设计意图，便于快速阅读与回顾。详细原文请查看同目录下的 Raw 文件。

---

### 1. 模块整体结构与职责分层

- **接口层：`IDebuggerModule` / `IDebuggerWindow` / `IDebuggerWindowGroup`**
  - `IDebuggerModule`：调试模块总入口，提供：
    - `ActiveWindow`：是否激活调试器窗口；
    - `DebuggerWindowRoot`：根窗口组；
    - 注册 / 反注册 / 获取 / 选中调试窗口的接口（基于字符串路径）。
  - `IDebuggerWindow`：单个调试窗口的生命周期与行为接口：
    - `Initialize` / `Shutdown` / `OnEnter` / `OnLeave` / `OnUpdate` / `OnDraw`。
  - `IDebuggerWindowGroup : IDebuggerWindow`：窗口组，额外提供：
    - `DebuggerWindowCount`、`SelectedIndex`、`SelectedWindow`；
    - `GetDebuggerWindowNames()`、`GetDebuggerWindow(path)`、`RegisterDebuggerWindow(path, window)`。

- **实现层（模块与窗口树）：`DebuggerModule` + 内部类 `DebuggerWindowGroup`**
  - `DebuggerModule : Module, IDebuggerModule, IUpdateModule`：
    - 在 `OnInit` 中创建 `_debuggerWindowRoot = new DebuggerWindowGroup()`，默认 `ActiveWindow = false`；
    - 在 `Update` 中，如果 `_activeWindow` 为 `true`，就把更新转发给根窗口组的 `OnUpdate`；
    - `RegisterDebuggerWindow(path, window, args)`：
      1. 将窗口按路径注册到 `_debuggerWindowRoot`；
      2. 调用 `window.Initialize(args)` 初始化窗口。
  - 内部 `DebuggerWindowGroup` 通过：
    - `List<KeyValuePair<string, IDebuggerWindow>> _debuggerWindows` 维护当前层的所有窗口或子组；
    - `SelectedIndex` / `SelectedWindow` 管理当前选中窗口；
    - 按 `'/'` 分割路径，实现递归注册 / 查找 / 选中窗口的树形结构：
      - 无 `/`：在当前组直接查找 / 注册 / 选择；
      - 有 `/`：前半段视为子组名，递归到对应子组继续处理；
    - `OnUpdate`：只对 `SelectedWindow` 调用 `OnUpdate`，避免所有窗口都更新。

- **展示层（Unity OnGUI）：`Debugger` MonoBehaviour**
  - 单例：`Debugger.Instance`。
  - 通过 `ModuleSystem.GetModule<IDebuggerModule>()` 获取 `_debuggerModule`，并根据 `DebuggerActiveWindowType`（AlwaysOpen / OnlyOpenWhenDevelopment / OnlyOpenInEditor）在 `Start()` 决定是否激活。
  - 管理：
    - 漂浮图标窗口矩形 `_iconRect`、主窗口矩形 `_windowRect`、缩放 `_windowScale`；
    - `GUISkin skin` 与 `GUI.matrix` 缩放；
    - 通过 `PlayerPrefs` 读写上次的窗口布局与缩放；
    - `ShowFullWindow` 属性打开完整调试界面时，自动禁用 `"UIRoot/EventSystem"`，防止与游戏 UI 抢输入。
  - 持有多种具体调试窗口实例（`ConsoleWindow`、多种 `InformationWindow`、`Profiler`、对象池信息、设置等），在 `Start()` 中通过路径统一注册：
    - 例如：`"Console"`、`"Information/System"`、`"Profiler/Memory/Summary"` 等。

---

### 2. 关键 GUI 流程与窗口树绘制

- **OnGUI 主流程**
  - 若 `_debuggerModule == null` 或未激活，则直接返回；
  - 备份当前 `GUI.skin` 与 `GUI.matrix`，使用调试器皮肤与缩放；
  - 根据 `_showFullWindow`：
    - `true`：`GUILayout.Window(0, _windowRect, DrawWindow, "<b>DEBUGGER</b>")`，绘制完整调试窗口；
    - `false`：`GUILayout.Window(0, _iconRect, DrawDebuggerWindowIcon, "<b>DEBUGGER</b>")`，绘制角落悬浮按钮；
  - 绘制完后恢复 `GUI.matrix` 与 `GUI.skin`。

- **`DrawWindow`：完整窗口内容入口**
  - `GUI.DragWindow(_dragRect)`：允许在顶部 25 像素区域拖动窗口；
  - 调用 `DrawDebuggerWindowGroup(_debuggerModule.DebuggerWindowRoot)` 绘制整个窗口树。

- **`DrawDebuggerWindowGroup`：窗口树导航 + 内容区域**
  - 从 `debuggerWindowGroup.GetDebuggerWindowNames()` 取出当前组下的所有窗口名，包装为 `<b>name</b>` 放进 `names` 列表；
  - 如果是根组，额外添加 `<b>Close</b>` 用于关闭调试窗口；
  - `GUILayout.Toolbar` 绘制标签栏，返回当前选中索引：
    - 如果索引超出 `DebuggerWindowCount`，说明点的是 `Close`，则 `ShowFullWindow = false` 并返回；
  - 若 `SelectedWindow` 变化：
    - 对旧窗口调用 `OnLeave()`；
    - 更新 `SelectedIndex`；
    - 对新窗口调用 `OnEnter()`；
  - 若当前选中窗口实现了 `IDebuggerWindowGroup`，递归调用 `DrawDebuggerWindowGroup`，实现多级菜单；
  - 最后调用 `SelectedWindow.OnDraw()` 绘制具体窗口内容。

- **`DrawDebuggerWindowIcon`：角落悬浮按钮**
  - 通过 `GUI.DragWindow` 让小窗口可拖动，`GUILayout.Space(5)` 留出顶部空白；
  - 调用 `_consoleWindow.RefreshCount()` 后，根据 `Fatal → Error → Warning → Log` 的优先级选择按钮文字颜色；
  - 使用富文本 `<color=#rrggbbaa><b>FPS: xx.xx</b></color>` 显示当前 FPS；
  - `GUILayout.Button` 绘制 100×40 按钮，点击后将 `ShowFullWindow = true`，切换到完整窗口模式；
  - 由于 `GUILayout.Window` 回调默认处于垂直布局组中，`GUILayout.Space(5)` 表现为纵向空白。

---

### 3. `ConsoleWindow.OnDraw` 行为总结

- **结构概览**
  - `ConsoleWindow` 是 `Debugger` 的内部 `[Serializable]` 类，实现 `IDebuggerWindow`，负责记录并展示 Unity 的日志输出；
  - 通过订阅 `Application.logMessageReceived` 收集 `LogNode` 队列 `_logNodes`，最大行数 `maxLine` 超出时释放旧日志。

- **顶部工具栏**
  - `RefreshCount()` 统计 `_infoCount / _warningCount / _errorCount / _fatalCount`；
  - 一行横向布局，包含：
    - `"Clear All"` 按钮：清空日志队列；
    - `Lock Scroll` Toggle：勾选时自动滚动到最新日志；
    - 四个过滤 Toggle：`Info (x) / Warning (y) / Error (z) / Fatal (w)`，控制是否显示对应类型日志。

- **中间日志列表**
  - 外层 `BeginVertical("box")` 包一块带边框的区域；
  - 内部 `BeginScrollView(_logScrollPosition)` 实现可滚动列表：
    - 遍历 `_logNodes`，根据当前过滤条件跳过不需要的日志；
    - 对每条日志使用 `GUILayout.Toggle(_selectedNode == logNode, GetLogString(logNode))` 作为“可选行”：
      - 当前选中日志高亮显示；
      - 选中行变化时更新 `_selectedNode` 并重置堆栈滚动 `_stackScrollPosition`；
    - 若本帧没有任何行被选中，则 `_selectedNode = null`。

- **底部堆栈详情**
  - 第二块 `BeginVertical("box")`，内部 `BeginScrollView(_stackScrollPosition, GUILayout.Height(100f))` 绘制选中日志的详情；
  - 若有 `_selectedNode`：
    - 使用富文本 `<color=#rrggbbaa><b>LogMessage</b></color>\n\nStackTrack` 显示消息和堆栈；
    - 通过 `GUILayout.Button(..., "label")` 让整段文本看起来像 Label，但仍可点击；
    - 点击时调用 `CopyToClipboard`，将“消息 + 空行 + 堆栈”复制到系统剪贴板。

---

### 4. 设计意图小结

- **分层解耦**
  - 模块层（`DebuggerModule` + `DebuggerWindowGroup`）只负责窗口树与逻辑更新；
  - 展示层（`Debugger` MonoBehaviour）只负责 OnGUI 绘制与用户交互；
  - 窗口实现层（各个 `IDebuggerWindow` 实现）各自封装具体调试功能。

- **树形路径注册**
  - 统一使用字符串路径（如 `"Information/System"`、`"Profiler/Memory/Summary"`）定义菜单结构；
  - 通过 `'/'` 分割在 `DebuggerWindowGroup` 中递归创建 / 查找子组，实现菜单树；
  - 便于扩展新调试窗口，只需实现 `IDebuggerWindow` 并按路径注册。

- **生命周期与状态管理**
  - `Initialize` / `Shutdown` 管理资源与事件订阅；
  - `OnEnter` / `OnLeave` 管理窗口切换时的状态准备与清理；
  - `OnUpdate` 只调用当前选中窗口，降低性能开销；
  - `OnDraw` 统一由 `DrawDebuggerWindowGroup` 调度，形成一致的 UI 渲染入口。

- **易用与安全**
  - 浮动 Icon + 完整窗口的模式，既不干扰正常游戏体验，又可随时唤出完整调试界面；
  - 打开完整界面时自动禁用 UI `EventSystem`，避免操作冲突；
  - `DebuggerActiveWindowType` 控制在 Editor / Development / Release 等场景下的默认启用策略。

---

### 5. 使用方式要点（基于真实代码）

- **接入步骤**
  1. 在常驻场景中新建空 GameObject，挂载 `Debugger` 组件；
  2. 将 `skin` 指定为项目中自带的 `DebuggerSkin.guiskin`；
  3. 确保 `ModuleSystem` 初始化时创建并注册了 `DebuggerModule`，使 `ModuleSystem.GetModule<IDebuggerModule>()` 有效；
  4. 运行时可通过 `Debugger.Instance.ActiveWindow` / `ShowFullWindow` 控制调试界面开关。

- **运行时控制示例（伪代码，非项目源码）**

```csharp
var debugger = Debugger.Instance;
if (debugger != null)
{
    debugger.ActiveWindow = true;       // 打开调试器
    debugger.ShowFullWindow = true;     // 展示完整窗口
    debugger.SelectDebuggerWindow("Console"); // 切到控制台窗口
}
```

- **扩展自定义窗口**
  - 实现 `IDebuggerWindow` 的新类（实现 `Initialize / Shutdown / OnEnter / OnLeave / OnUpdate / OnDraw`）；
  - 在初始化阶段调用：

```csharp
Debugger.Instance.RegisterDebuggerWindow("Custom/MyWindow", new MyCustomDebuggerWindow());
```

  - 即可在 Debugger 界面中通过菜单 `Custom → MyWindow` 访问该窗口。


