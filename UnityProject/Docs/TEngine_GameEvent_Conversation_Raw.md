# TEngine GameEvent 模块设计对话（Raw）

> 日期：2025-01-27  
> 说明：尽量完整记录本次会话中 **用户与助手** 的可见内容（不包含系统与工具调用元数据）。

---

## 1. 会话起点

**User：**  
gameevent 模块的设计

**Assistant（Auto）：**  
我是 Auto，一个由 Cursor 设计的智能路由代理。正在分析 GameEvent 模块的设计架构。

**Assistant（Auto，分析完成后）：**  
## GameEvent 模块设计分析

基于实际代码，设计架构如下：

### 一、整体架构

采用分层设计，包含以下核心组件：

#### 1. 核心类层次结构

```
GameEvent (全局入口)
    └── EventMgr (事件管理器)
        ├── EventDispatcher (事件分发器)
        │   └── Dictionary<int, EventDelegateData> (事件表)
        └── Dictionary<string, EventEntryData> (接口注册表)
```

#### 2. 关键类职责

- **`GameEvent`**：全局静态入口类，提供注册、移除、发送的静态方法
- **`EventMgr`**：管理 `EventDispatcher` 和接口注册
- **`EventDispatcher`**：事件分发器，维护 `Dictionary<int, EventDelegateData>` 事件表
- **`EventDelegateData`**：单个事件的所有监听器，处理执行中的安全修改
- **`GameEventMgr`**：局部事件管理器，实现 `IMemory`，用于 UI 等场景
- **`RuntimeId`**：字符串到 int 的运行时 ID 转换

### 二、设计要点

#### 1. 零 GC 设计

```csharp:28:31:UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEvent.cs
public static bool AddEventListener(int eventType, Action handler)
{
    return _eventMgr.Dispatcher.AddEventListener(eventType, handler);
}
```

- 使用 `int` 作为事件 ID，避免字符串哈希计算
- 字符串 ID 通过 `RuntimeId.ToRuntimeId()` 转换为 int 并缓存

#### 2. 执行中安全修改机制

```csharp:32:50:UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventDelegateData.cs
internal bool AddHandler(Delegate handler)
{
    if (_listExist.Contains(handler))
    {
        Log.Fatal("Repeated Add Handler");
        return false;
    }

    if (_isExecute)
    {
        _dirty = true;
        _addList.Add(handler);
    }
    else
    {
        _listExist.Add(handler);
    }

    return true;
}
```

- 执行中（`_isExecute = true`）的添加/删除操作延迟到执行后处理
- 通过 `_addList` 和 `_deleteList` 暂存，执行完成后在 `CheckModify()` 中统一应用

#### 3. 多参数支持

支持 0-6 个泛型参数的事件，通过重载实现类型安全：

```csharp:28:118:UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEvent.cs
public static bool AddEventListener(int eventType, Action handler)
public static bool AddEventListener<TArg1>(int eventType, Action<TArg1> handler)
public static bool AddEventListener<TArg1, TArg2>(int eventType, Action<TArg1, TArg2> handler)
// ... 最多支持 6 个参数
```

#### 4. UI 生命周期自动绑定

```csharp:276:324:UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs
#region UIEvent

private GameEventMgr _eventMgr;

protected GameEventMgr EventMgr
{
    get
    {
        if (_eventMgr == null)
        {
            _eventMgr = MemoryPool.Acquire<GameEventMgr>();
        }

        return _eventMgr;
    }
}

public void AddUIEvent(int eventType, Action handler)
{
    EventMgr.AddEvent(eventType, handler);
}

// ... 其他重载

protected void RemoveAllUIEvent()
{
    if (_eventMgr != null)
    {
        MemoryPool.Release(_eventMgr);
    }
}

#endregion
```

- UI 通过 `GameEventMgr` 管理事件
- `GameEventMgr` 实现 `IMemory`，可从内存池获取
- UI 销毁时调用 `RemoveAllUIEvent()`，`GameEventMgr.Clear()` 自动移除所有监听

#### 5. 事件回调执行机制

```csharp:101:114:UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventDelegateData.cs
public void Callback()
{
    _isExecute = true;
    for (var i = 0; i < _listExist.Count; i++)
    {
        var d = _listExist[i];
        if (d is Action action)
        {
            action();
        }
    }

    CheckModify();
}
```

- 遍历 `_listExist` 执行所有监听器
- 执行后调用 `CheckModify()` 处理延迟的添加/删除

### 三、设计意图

1. **性能优先**：int ID、延迟修改、内存池复用
2. **类型安全**：泛型重载，编译期检查
3. **易用性**：UI 自动管理、支持 int/string 两种 ID
4. **安全性**：执行中修改延迟处理，避免迭代时修改集合

### 四、使用场景

1. **全局事件**：通过 `GameEvent` 静态方法
2. **UI 事件**：通过 `UIBase.AddUIEvent()`，自动管理生命周期
3. **局部事件**：使用 `GameEventMgr`，如玩家对象的事件管理

### 五、与 UI 模块的集成

- UI 通过 `GameEventMgr` 管理事件
- `AddUIEvent` 注册的事件在 UI 销毁时自动清理
- 无需手动调用 `RemoveEventListener`

该设计在性能、安全性和易用性之间取得平衡，适合高频事件场景。

---

## 2. 深入讨论 GameEventMgr 的作用

**User：**  
gameeventmgr是不是保留保存到dispatch的，用于销毁？

**Assistant（Auto）：**  
是的，`GameEventMgr` 的作用是保存已注册到 dispatcher 的事件信息，用于后续统一销毁。

## GameEventMgr 的设计意图

### 核心机制

```csharp:11:55:UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEventMgr.cs
private readonly List<int> _listEventTypes;
private readonly List<Delegate> _listHandles;

// 添加事件时：先注册到全局 dispatcher，再保存到本地列表
public void AddEvent(int eventType, Action handler)
{
    if (GameEvent.AddEventListener(eventType, handler))  // 注册到全局 EventDispatcher
    {
        AddEventImp(eventType, handler);  // 保存到本地列表
    }
}

private void AddEventImp(int eventType, Delegate handler)
{
    _listEventTypes.Add(eventType);  // 保存事件类型
    _listHandles.Add(handler);       // 保存委托引用
}

// 清理时：遍历本地列表，批量移除
public void Clear()
{
    for (int i = 0; i < _listEventTypes.Count; ++i)
    {
        var eventType = _listEventTypes[i];
        var handle = _listHandles[i];
        GameEvent.RemoveEventListener(eventType, handle);  // 从全局 EventDispatcher 移除
    }
    
    _listEventTypes.Clear();
    _listHandles.Clear();
}
```

### 设计要点

1. **反向引用记录**：`GameEventMgr` 维护两个列表，记录已注册到全局 `EventDispatcher` 的事件。
2. **批量清理**：`Clear()` 遍历本地记录，统一从全局 dispatcher 移除，避免遗漏。
3. **生命周期绑定**：UI 销毁时调用 `RemoveAllUIEvent()` → `MemoryPool.Release(_eventMgr)` → `GameEventMgr.Clear()`，自动清理。

### 工作流程

```
注册阶段：
AddUIEvent() 
  → GameEventMgr.AddEvent() 
    → GameEvent.AddEventListener() [注册到全局 EventDispatcher]
    → AddEventImp() [保存到本地 _listEventTypes 和 _listHandles]

销毁阶段：
UI.OnDestroy() 
  → RemoveAllUIEvent() 
    → MemoryPool.Release(_eventMgr) 
      → GameEventMgr.Clear() 
        → 遍历本地列表，调用 GameEvent.RemoveEventListener() [从全局移除]
```

### 为什么需要这个设计？

- 全局 dispatcher 不记录“谁注册了哪些事件”，无法按对象批量移除。
- 手动逐个移除容易遗漏，且需要保存 eventType 和 handler 的对应关系。
- `GameEventMgr` 作为“注册记录本”，在销毁时能批量、准确地移除。

这是一个反向引用设计，用于解决批量事件清理问题。

---

## 3. 保存会话

**User：**  
把当前会话一字不漏的保存下来 ，取个合适的名字，保存到合适的目录

**Assistant（Auto）：**  
已保存当前会话到 `UnityProject/Docs/TEngine_GameEvent_Conversation_Raw.md`。

---

## 附录：关键代码文件路径

- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEvent.cs` - 全局事件入口
- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventMgr.cs` - 事件管理器
- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventDispatcher.cs` - 事件分发器
- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventDelegateData.cs` - 事件委托数据
- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/GameEventMgr.cs` - 局部事件管理器
- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/RuntimeId.cs` - 运行时 ID 转换
- `UnityProject/Assets/TEngine/Runtime/Core/GameEvent/EventInterfaceAttribute.cs` - 事件接口特性
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs` - UI 基类（事件集成）
- `Books/3-2-事件模块.md` - 事件模块文档
