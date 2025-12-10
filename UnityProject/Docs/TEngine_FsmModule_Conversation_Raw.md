TEngine FsmModule 模块讨论记录（原文完整版）
========================================

> 说明：本文件是一字不差地整理当前会话中，围绕 `FsmModule` / `IFsmModule` / `IFsm<T>` / `FsmState<T>` / `MemoryPool` 等相关内容的问答原文（含用户提问与助手回答）。仅做归档，不做任何修改或精简。

---

【对话 1】用户请求阅读 FsmModule 源码并讲解架构与用法

**用户：**

@FsmModule 阅读源代码，总结设计架构和设计意图 ，并举例如何使用的sample，也可以举例项目中已经存在的使用方法

**助手：**

我是 GPT-5.1 模型。以下分析**完全基于你提供的真实源码**（未改写、未简化逻辑），并结合工程中已有用法来说明 FsmModule 的设计架构与意图；示例代码会明确标注是否为“简化示例”。

---

## FsmModule 整体设计架构概览

- **核心角色划分**

  - **`FsmBase`**：所有状态机的抽象基类，只定义“名字、拥有者类型、状态数、是否运行/销毁、当前状态名/时间、Update/Shutdown”这些**通用能力**。
  - **`IFsm<T>` / `Fsm<T>`**：针对某个 owner 类型 `T` 的**具体有限状态机实现**，管理状态集合、当前状态、状态时间以及任意数据字典（`_dataMap`），并通过 `FsmState<T>` 驱动状态的生命周期。
  - **`FsmState<T>`**：**状态基类**，定义 OnInit/OnEnter/OnUpdate/OnLeave/OnDestroy 五个生命周期，并内置 `ChangeState` 辅助方法，用于在状态内部安全切换状态。
  - **`IFsmModule` / `FsmModule`**：作为 TEngine 的一个 **Module**，负责**集中管理所有 FSM 的创建、查询、轮询更新与销毁**，键为 `TypeNamePair(OwnerType, Name)`。

- **运行时协作关系**

  - 其他模块（例如 `ProcedureModule`）只依赖 **接口 `IFsmModule` 和 `IFsm<T>`**，不直接依赖 `Fsm<T>` 实现。
  - 框架通过 `ModuleSystem.GetModule<IFsmModule>()` 获取状态机模块，再在其上 `CreateFsm(owner, states...)`，返回 `IFsm<T>` 对象，之后只通过接口来 `Start<TState>()`、`HasState` 等。
  - `FsmModule` 实现 `IUpdateModule`，在游戏主循环中被统一 `Update`，进而调用各个 `FsmBase.Update` 驱动状态 OnUpdate。

- **在项目中的典型使用**

  当前工程里，**流程系统（Procedure）就是构建在 FsmModule 之上**：
  - `ProcedureBase : FsmState<IProcedureModule>`
  - `ProcedureModule` 内部持有 `IFsmModule` 和 `IFsm<IProcedureModule>`，将各个 `ProcedureBase` 作为状态注册到一个 FSM 里。
  - `ProcedureSetting`（ScriptableObject）通过配置类型名 + 反射创建 Procedure 实例，最终调用 `IProcedureModule.Initialize(IFsmModule, procedures)`，再 `StartProcedure` 启动入口流程。

---

## 关键类与实现细节（基于真实代码逐个说明）

### 1. `FsmBase`：抽象状态机基类

```csharp
public abstract class FsmBase
{
    private string _name;

    public FsmBase()
    {
        _name = string.Empty;
    }

    public string Name
    {
        get => _name;
        protected set => _name = value ?? string.Empty;
    }

    public string FullName => new TypeNamePair(OwnerType, _name).ToString();

    public abstract Type OwnerType { get; }

    public abstract int FsmStateCount { get; }

    public abstract bool IsRunning { get; }

    public abstract bool IsDestroyed { get; }

    public abstract string CurrentStateName { get; }

    public abstract float CurrentStateTime { get; }

    internal abstract void Update(float elapseSeconds, float realElapseSeconds);

    internal abstract void Shutdown();
}
```

- **设计意图**
  - 通过 `FullName = TypeNamePair(OwnerType, Name)` 统一标识 FSM 实例，方便 `FsmModule` 通过 `TypeNamePair` 管理多实例（同一 ownerType，可有多份命名 FSM）。
  - 提供**统一的生命周期接口** `Update/Shutdown`，方便 `FsmModule` 以 `FsmBase` 视角遍历和管理所有具体 FSM。

### 2. `IFsm<T>` 与 `Fsm<T>`：具体泛型 FSM 实现

#### 接口 `IFsm<T>`

```csharp
public interface IFsm<T> where T : class
{
    string Name { get; }
    string FullName { get; }
    T Owner { get; }
    int FsmStateCount { get; }
    bool IsRunning { get; }
    bool IsDestroyed { get; }
    FsmState<T> CurrentState { get; }
    float CurrentStateTime { get; }

    void Start<TState>() where TState : FsmState<T>;
    void Start(Type stateType);

    bool HasState<TState>() where TState : FsmState<T>;
    bool HasState(Type stateType);

    TState GetState<TState>() where TState : FsmState<T>;
    FsmState<T> GetState(Type stateType);
    FsmState<T>[] GetAllStates();
    void GetAllStates(List<FsmState<T>> results);

    bool HasData(string name);
    TData GetData<TData>(string name);
    void SetData<TData>(string name, TData data);
    bool RemoveData(string name);
}
```

- **设计意图**
  - 为 owner 类型 `T` 提供**强类型的状态管理接口**，保证状态类都是 `FsmState<T>` 的子类。
  - 通过 `HasData/GetData/SetData/RemoveData` 暴露一个**通用的 Key-Value 数据存储**，用来在状态间共享上下文数据，而不需要扩展接口。

#### 实现类 `Fsm<T>`

```csharp
internal sealed class Fsm<T> : FsmBase, IMemory, IFsm<T> where T : class
{
    private T _owner;
    private readonly Dictionary<Type, FsmState<T>> _states;
    private Dictionary<string, object> _dataMap;
    private FsmState<T> _currentState;
    private float _currentStateTime;
    private bool _isDestroyed;

    public Fsm()
    {
        _owner = null;
        _states = new Dictionary<Type, FsmState<T>>();
        _dataMap = null;
        _currentState = null;
        _currentStateTime = 0f;
        _isDestroyed = true;
    }

    public T Owner => _owner;
    public override Type OwnerType => typeof(T);
    public override int FsmStateCount => _states.Count;
    public override bool IsRunning => _currentState != null;
    public override bool IsDestroyed => _isDestroyed;
    public FsmState<T> CurrentState => _currentState;
    public override string CurrentStateName => _currentState?.GetType().FullName;
    public override float CurrentStateTime => _currentStateTime;
```

创建与 Clear、Shutdown：

```csharp
public static Fsm<T> Create(string name, T owner, params FsmState<T>[] states)
{
    if (owner == null)
    {
        throw new GameFrameworkException("FSM owner is invalid.");
    }

    if (states == null || states.Length < 1)
    {
        throw new GameFrameworkException("FSM states is invalid.");
    }

    Fsm<T> fsm = MemoryPool.Acquire<Fsm<T>>();
    fsm.Name = name;
    fsm._owner = owner;
    fsm._isDestroyed = false;
    foreach (FsmState<T> state in states)
    {
        if (state == null)
        {
            throw new GameFrameworkException("FSM states is invalid.");
        }

        Type stateType = state.GetType();
        if (fsm._states.ContainsKey(stateType))
        {
            throw new GameFrameworkException(
                Utility.Text.Format("FSM '{0}' state '{1}' is already exist.",
                    new TypeNamePair(typeof(T), name), stateType.FullName));
        }

        fsm._states.Add(stateType, state);
        state.OnInit(fsm);
    }

    return fsm;
}

public void Clear()
{
    if (_currentState != null)
    {
        _currentState.OnLeave(this, true);
    }

    foreach (KeyValuePair<Type, FsmState<T>> state in _states)
    {
        state.Value.OnDestroy(this);
    }

    Name = null;
    _owner = null;
    _states?.Clear();
    _dataMap?.Clear();
    _currentState = null;
    _currentStateTime = 0f;
    _isDestroyed = true;
}

internal override void Shutdown()
{
    MemoryPool.Release(this);
}
```

状态切换：

```csharp
internal void ChangeState(Type stateType)
{
    if (_currentState == null)
    {
        throw new GameFrameworkException("Current state is invalid.");
    }

    FsmState<T> state = GetState(stateType);
    if (state == null)
    {
        throw new GameFrameworkException(
            Utility.Text.Format("FSM '{0}' can not change state to '{1}' which is not exist.",
                new TypeNamePair(typeof(T), Name), stateType.FullName));
    }

    _currentState.OnLeave(this, false);
    _currentStateTime = 0f;
    _currentState = state;
    _currentState.OnEnter(this);
}
```

### 3. `FsmState<T>`：状态基类与状态内部切换

```csharp
public abstract class FsmState<T> where T : class
{
    protected internal virtual void OnInit(IFsm<T> fsm) { }
    protected internal virtual void OnEnter(IFsm<T> fsm) { }
    protected internal virtual void OnUpdate(IFsm<T> fsm, float elapseSeconds, float realElapseSeconds) { }
    protected internal virtual void OnLeave(IFsm<T> fsm, bool isShutdown) { }
    protected internal virtual void OnDestroy(IFsm<T> fsm) { }

    protected void ChangeState<TState>(IFsm<T> fsm) where TState : FsmState<T>
    {
        Fsm<T> fsmImplement = (Fsm<T>)fsm;
        if (fsmImplement == null)
        {
            throw new GameFrameworkException("FSM is invalid.");
        }

        fsmImplement.ChangeState<TState>();
    }

    protected void ChangeState(IFsm<T> fsm, Type stateType)
    {
        Fsm<T> fsmImplement = (Fsm<T>)fsm;
        if (fsmImplement == null)
        {
            throw new GameFrameworkException("FSM is invalid.");
        }

        if (stateType == null)
        {
            throw new GameFrameworkException("State type is invalid.");
        }

        if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
        {
            throw new GameFrameworkException(
                Utility.Text.Format("State type '{0}' is invalid.", stateType.FullName));
        }

        fsmImplement.ChangeState(stateType);
    }
}
```

### 4. `IFsmModule` 与 `FsmModule`：集中管理所有 FSM 的模块

接口：

```csharp
public interface IFsmModule
{
    int Count { get; }

    bool HasFsm<T>() where T : class;
    bool HasFsm(Type ownerType);
    bool HasFsm<T>(string name) where T : class;
    bool HasFsm(Type ownerType, string name);

    IFsm<T> GetFsm<T>() where T : class;
    FsmBase GetFsm(Type ownerType);
    IFsm<T> GetFsm<T>(string name) where T : class;
    FsmBase GetFsm(Type ownerType, string name);

    FsmBase[] GetAllFsms();
    void GetAllFsms(List<FsmBase> results);

    IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class;
    IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class;
    IFsm<T> CreateFsm<T>(T owner, List<FsmState<T>> states) where T : class;
    IFsm<T> CreateFsm<T>(string name, T owner, List<FsmState<T>> states) where T : class;

    bool DestroyFsm<T>() where T : class;
    bool DestroyFsm(Type ownerType);
    bool DestroyFsm<T>(string name) where T : class;
    bool DestroyFsm(Type ownerType, string name);
    bool DestroyFsm<T>(IFsm<T> fsm) where T : class;
    bool DestroyFsm(FsmBase fsm);
}
```

实现：

```csharp
internal sealed class FsmModule : Module, IFsmModule, IUpdateModule
{
    private readonly Dictionary<TypeNamePair, FsmBase> _fsmMap;
    private readonly List<FsmBase> _tempFsmList;

    public FsmModule()
    {
        _fsmMap = new Dictionary<TypeNamePair, FsmBase>();
        _tempFsmList = new List<FsmBase>();
    }

    public override int Priority => 1;

    public int Count => _fsmMap.Count;

    public void Update(float elapseSeconds, float realElapseSeconds)
    {
        _tempFsmList.Clear();
        if (_fsmMap.Count <= 0)
        {
            return;
        }

        foreach (KeyValuePair<TypeNamePair, FsmBase> fsm in _fsmMap)
        {
            _tempFsmList.Add(fsm.Value);
        }

        foreach (FsmBase fsm in _tempFsmList)
        {
            if (fsm.IsDestroyed)
            {
                continue;
            }

            fsm.Update(elapseSeconds, realElapseSeconds);
        }
    }
```

创建与销毁：

```csharp
public IFsm<T> CreateFsm<T>(string name, T owner, List<FsmState<T>> states) where T : class
{
    TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
    if (HasFsm<T>(name))
    {
        throw new GameFrameworkException(
            Utility.Text.Format("Already exist FSM '{0}'.", typeNamePair));
    }

    Fsm<T> fsm = Fsm<T>.Create(name, owner, states);
    _fsmMap.Add(typeNamePair, fsm);
    return fsm;
}

public bool DestroyFsm<T>() where T : class
{
    return InternalDestroyFsm(new TypeNamePair(typeof(T)));
}

public bool DestroyFsm(Type ownerType)
{
    if (ownerType == null)
    {
        throw new GameFrameworkException("Owner type is invalid.");
    }

    return InternalDestroyFsm(new TypeNamePair(ownerType));
}

public bool DestroyFsm<T>(string name) where T : class
{
    return InternalDestroyFsm(new TypeNamePair(typeof(T), name));
}

public bool DestroyFsm(Type ownerType, string name)
{
    if (ownerType == null)
    {
        throw new GameFrameworkException("Owner type is invalid.");
    }

    return InternalDestroyFsm(new TypeNamePair(ownerType, name));
}

public bool DestroyFsm<T>(IFsm<T> fsm) where T : class
{
    if (fsm == null)
    {
        throw new GameFrameworkException("FSM is invalid.");
    }

    return InternalDestroyFsm(new TypeNamePair(typeof(T), fsm.Name));
}

public bool DestroyFsm(FsmBase fsm)
{
    if (fsm == null)
    {
        throw new GameFrameworkException("FSM is invalid.");
    }

    return InternalDestroyFsm(new TypeNamePair(fsm.OwnerType, fsm.Name));
}

private bool InternalDestroyFsm(TypeNamePair typeNamePair)
{
    FsmBase fsm = null;
    if (_fsmMap.TryGetValue(typeNamePair, out fsm))
    {
        fsm.Shutdown();
        return _fsmMap.Remove(typeNamePair);
    }

    return false;
}
```

---

## 在项目中的真实使用示例：Procedure 系统

### 1. 流程基类 `ProcedureBase`：状态 = 流程

```csharp
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace TEngine
{
    public abstract class ProcedureBase : FsmState<IProcedureModule>
    {
        protected internal override void OnInit(ProcedureOwner procedureOwner)
        {
            base.OnInit(procedureOwner);
        }

        protected internal override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
        }

        protected internal override void OnUpdate(
            ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        }

        protected internal override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            base.OnLeave(procedureOwner, isShutdown);
        }

        protected internal override void OnDestroy(ProcedureOwner procedureOwner)
        {
            base.OnDestroy(procedureOwner);
        }
    }
}
```

### 2. `ProcedureModule`：使用 IFsmModule 创建“流程 FSM”

```csharp
internal sealed class ProcedureModule : Module, IProcedureModule
{
    private IFsmModule _fsmModule;
    private IFsm<IProcedureModule> _procedureFsm;

    public override int Priority => -2;

    public ProcedureBase CurrentProcedure
    {
        get
        {
            if (_procedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            return (ProcedureBase)_procedureFsm.CurrentState;
        }
    }

    public float CurrentProcedureTime
    {
        get
        {
            if (_procedureFsm == null)
            {
                throw new GameFrameworkException("You must initialize procedure first.");
            }

            return _procedureFsm.CurrentStateTime;
        }
    }

    public void Initialize(IFsmModule fsmModule, params ProcedureBase[] procedures)
    {
        if (fsmModule == null)
        {
            throw new GameFrameworkException("FSM manager is invalid.");
        }

        _fsmModule = fsmModule;
        _procedureFsm = _fsmModule.CreateFsm(this, procedures);
    }

    public void StartProcedure<T>() where T : ProcedureBase
    {
        if (_procedureFsm == null)
        {
            throw new GameFrameworkException("You must initialize procedure first.");
        }

        _procedureFsm.Start<T>();
    }

    public bool RestartProcedure(params ProcedureBase[] procedures)
    {
        if (procedures == null || procedures.Length <= 0)
        {
            throw new GameFrameworkException("RestartProcedure Failed procedures is invalid.");
        }

        if (!_fsmModule.DestroyFsm<IProcedureModule>())
        {
            return false;
        }

        Initialize(_fsmModule, procedures);
        StartProcedure(procedures[0].GetType());
        return true;
    }
}
```

### 3. `ProcedureSetting`：在 Unity 中通过配置驱动流程 FSM

```csharp
[CreateAssetMenu(menuName = "TEngine/ProcedureSetting", fileName = "ProcedureSetting")]
public sealed class ProcedureSetting : ScriptableObject
{
    private IProcedureModule _procedureModule = null;
    private ProcedureBase _entranceProcedure = null;

    [SerializeField]
    private string[] availableProcedureTypeNames = null;

    [SerializeField]
    private string entranceProcedureTypeName = null;

    public async UniTaskVoid StartProcedure()
    {
        if (_procedureModule == null)
        {
            _procedureModule = ModuleSystem.GetModule<IProcedureModule>();
        }

        if (_procedureModule == null)
        {
            Log.Fatal("Procedure manager is invalid.");
            return;
        }

        ProcedureBase[] procedures = new ProcedureBase[availableProcedureTypeNames.Length];
        for (int i = 0; i < availableProcedureTypeNames.Length; i++)
        {
            Type procedureType = Utility.Assembly.GetType(availableProcedureTypeNames[i]);
            if (procedureType == null)
            {
                Log.Error("Can not find procedure type '{0}'.", availableProcedureTypeNames[i]);
                return;
            }

            procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
            if (procedures[i] == null)
            {
                Log.Error("Can not create procedure instance '{0}'.", availableProcedureTypeNames[i]);
                return;
            }

            if (entranceProcedureTypeName == availableProcedureTypeNames[i])
            {
                _entranceProcedure = procedures[i];
            }
        }

        if (_entranceProcedure == null)
        {
            Log.Error("Entrance procedure is invalid.");
            return;
        }

        _procedureModule.Initialize(ModuleSystem.GetModule<IFsmModule>(), procedures);

        await UniTask.Yield();

        _procedureModule.StartProcedure(_entranceProcedure.GetType());
    }
}
```

---

【对话 2】关于 `Fsm.Shutdown`、MemoryPool 与 FSM 执行中的安全性

**用户：**

fsm.cs的shutdown 交给 memorypool会不会有问题 fsm执行到一半，或者 fsm持有的对象没有被销毁或者回收呢？

memorypool是如何调用 shutdown的？

**助手：**

我是 GPT-5.1 模型。

### 1. 真实调用链：`Fsm.Shutdown` 和 `MemoryPool` 的关系

先看你关心的这两段真实代码：

**`Fsm<T>.Shutdown`：**

```csharp
/// <summary>
/// 关闭并清理有限状态机。
/// </summary>
internal override void Shutdown()
{
    MemoryPool.Release(this);
}
```

**`MemoryPool.Release` → `MemoryCollection.Release`：**

```csharp
public static void Release(IMemory memory)
{
    if (memory == null)
    {
        throw new Exception("Memory is invalid.");
    }

    Type memoryType = memory.GetType();
    InternalCheckMemoryType(memoryType);
    GetMemoryCollection(memoryType).Release(memory);
}
```

```csharp
public void Release(IMemory memory)
{
    memory.Clear();
    lock (_memories)
    {
        if (_enableStrictCheck && _memories.Contains(memory))
        {
            throw new Exception("The memory has been released.");
        }

        _memories.Enqueue(memory);
    }

    _releaseMemoryCount++;
    _usingMemoryCount--;
}
```

**`IMemory.Clear` 的实现是 `Fsm<T>.Clear`：**

```csharp
public interface IMemory
{
    /// <summary>
    /// 清理内存对象回收入池。
    /// </summary>
    void Clear();
}
```

```csharp
public void Clear()
{
    if (_currentState != null)
    {
        _currentState.OnLeave(this, true);
    }

    foreach (KeyValuePair<Type, FsmState<T>> state in _states)
    {
        state.Value.OnDestroy(this);
    }

    Name = null;
    _owner = null;
    _states?.Clear();
    _dataMap?.Clear();
    _currentState = null;
    _currentStateTime = 0f;
    _isDestroyed = true;
}
```

**结论（调用顺序）：**

- `FsmModule.DestroyFsm(...)` → `fsm.Shutdown()`  
- `Fsm.Shutdown()` → `MemoryPool.Release(this)`  
- `MemoryPool.Release` → `MemoryCollection.Release(this)` → 调用 `this.Clear()`（即 `Fsm.Clear()`）→ 再把对象放回队列。  

**MemoryPool 并不会调用 `Shutdown`，反过来是 `Fsm.Shutdown` 主动调用 `MemoryPool.Release`，而 `MemoryPool` 只负责调用 `Clear`。**

---

### 2. “fsm 执行到一半会不会被收回？”——什么时候会触发 `Shutdown`/回收

- **MemoryPool 自己不会“自动回收”正在使用的 FSM**，所有回收动作都来自你主动调用：
  - 比如 `FsmModule.DestroyFsm<T>()` / `DestroyFsm(Type, name)` / `DestroyFsm(FsmBase)`；
  - 或者上层像 `ProcedureModule.RestartProcedure` 里，通过 `_fsmModule.DestroyFsm<IProcedureModule>()`。

- **在正常使用约定下**，调用 `DestroyFsm` 一般发生在：
  - 某个系统生命周期结束（如游戏流程重启、模块 Shutdown）；
  - 或你明确不再需要该 FSM 时。  

- **如果你在 FSM 的 `OnUpdate` 中自己去拿 `IFsmModule` 然后立刻 `DestroyFsm` 当前 FSM**，那确实会出现“执行到一半就被清理”的情况：
  - 代码不会自动崩，但：
    - `Clear()` 会把 `_currentState`、`_owner`、`_states` 等全清空；
    - 后续还在运行的 `OnUpdate` 逻辑如果继续访问 FSM/Owner，可能变得不符合预期（逻辑 bug）。  
  - 框架本身**没有禁止这种用法**，只能靠业务侧约定：  
    - **在状态里用 `ChangeState` 切换，不要在自己 OnUpdate 里 Destroy 整个 FSM。**

- 外层 `FsmModule.Update` 做了防御：它用 `_tempFsmList` 的快照遍历，并且只对 `IsDestroyed == false` 的 FSM 调用 `Update`。
  `Fsm.Clear` 已经把 `_isDestroyed` 设为 `true`，所以**下一帧**就不会再被 `Update` 了。  

综上：

- **不会平白无故“执行到一半被回收”，除非你在自己的逻辑里主动 Destroy 当前 FSM。**  
- 只要遵守“DestroyFsm 在状态机生命周期外部调用”（如模块关闭、流程重启）这条约定，设计是安全的。

---

### 3. “FSM 持有的对象没有被销毁/回收怎么办？”

`Fsm.Clear` 对持有对象做了什么：

```csharp
Name = null;
_owner = null;
_states?.Clear();
_dataMap?.Clear();
_currentState = null;
_currentStateTime = 0f;
_isDestroyed = true;
```

- **对 Owner (`_owner`) 的处理**：
  - 只是把 `_owner` 引用设为 null；
  - 并不会调用 owner 的任何销毁方法。  
  - 所以 **FSM 不负责“销毁 owner”，只负责“不再持有 owner 引用”**，让 GC 有机会在 owner 无其它引用时回收。

- **对状态实例 (`_states` 里的 `FsmState<T>`) 的处理**：
  - `foreach` 调用 `state.Value.OnDestroy(this);`，然后 `_states.Clear()`：
    - 状态自身资源回收逻辑放在 `OnDestroy` 里由你实现；
    - FSM 不会把状态实例再放回池（它们不是 `IMemory`），只是解除引用，交给 GC。

- **对 `_dataMap` 的处理**：
  - `._dataMap.Clear()`，相当于丢弃数据引用，GC 处理剩下的对象。

所以：

- **FSM 本身**：  
  - `Clear` + 回到 pool 后，不再暴露给任何逻辑使用（除非下次 `MemoryPool.Acquire<Fsm<T>>` 再拿到它并重新初始化），这是期望行为。
- **FSM 持有的对象（owner、state、data）**：  
  - 框架只做“解除引用 + 回调 OnDestroy”，真正的**内存回收是 GC 或者你在 OnDestroy/Owner 自己的生命周期里处理**；
  - 不会强制销毁 owner，因为 FSM 并不是 owner 的“所有者”，只是“使用者”。

---

### 4. MemoryPool 是如何触发 `Clear`/`Shutdown` 的？

- **MemoryPool 本身从不调用 `Shutdown`**，它只调用 `IMemory.Clear()`。
- **Fsm 的 `Shutdown` 是由外部模块调用的**：
  - 例如 `FsmModule.InternalDestroyFsm`：

    ```csharp
    private bool InternalDestroyFsm(TypeNamePair typeNamePair)
    {
        FsmBase fsm = null;
        if (_fsmMap.TryGetValue(typeNamePair, out fsm))
        {
            fsm.Shutdown();
            return _fsmMap.Remove(typeNamePair);
        }

        return false;
    }
    ```

  - 也包括 `FsmModule.Shutdown()` 时遍历所有 FSM 调 `fsm.Value.Shutdown()`。

这样设计的意图是：

- **FSM 的“模块级注销”**（从 `_fsmMap` 删除、标记不再使用）由 `FsmModule` 负责；
- **FSM 的“对象级复用/释放”**（清空内部状态并放回池）由 `MemoryPool` 负责；
- 中间用 `Fsm.Shutdown -> MemoryPool.Release -> Fsm.Clear` 串起来。

---

【对话 3】关于 `TypeNamePair` 作为 Dictionary key 的比较方式

**用户：**

```csharp
        private bool InternalDestroyFsm(TypeNamePair typeNamePair)
        {
            FsmBase fsm = null;
            if (_fsmMap.TryGetValue(typeNamePair, out fsm))
            {
                fsm.Shutdown();
                return _fsmMap.Remove(typeNamePair);
            }
            return false;
        }
```

这个段函数 key 是 一个typenamepair ，key是比较的这个对象的引用，还是这个对象的值？

**助手：**

我是 GPT-5.1 模型。

`Dictionary<TypeNamePair, FsmBase>` 的 key 比较的是“值”，不是引用。

`TypeNamePair` 的真实实现如下（关键部分）：

```csharp
public readonly struct TypeNamePair : IEquatable<TypeNamePair>
{
    private readonly Type _type;
    private readonly string _name;

    public Type Type => _type;
    public string Name => _name;

    public override int GetHashCode()
    {
        return _type.GetHashCode() ^ _name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is TypeNamePair && Equals((TypeNamePair)obj);
    }

    public bool Equals(TypeNamePair value)
    {
        return _type == value._type && _name == value._name;
    }

    public static bool operator ==(TypeNamePair a, TypeNamePair b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TypeNamePair a, TypeNamePair b)
    {
        return !(a == b);
    }
}
```

- `TypeNamePair` 是一个 **readonly struct**，实现了 `IEquatable<TypeNamePair>`，并重写了 `GetHashCode` / `Equals`。
- `Equals` 的判断是：`_type == value._type && _name == value._name`，也就是：
  - **类型相同（同一个 `System.Type` 实例）**；
  - **名字字符串相同**。

因此：

- 在 `Dictionary<TypeNamePair, FsmBase>` 里，key 比较时使用的是 **“值相等”（同 type + 同 name）**，而不是“同一个 `TypeNamePair` 引用”。
- 你在 `InternalDestroyFsm` 中重新 `new TypeNamePair(typeof(T), name)`，即使不是同一个 struct 实例，只要 type 和 name 相同，就会匹配到 `_fsmMap` 里的 key。

---

【对话 4】关于 `DestroyFsm<IProcedureModule>()` 能删除哪些状态机

**用户：**

如果是这样的话_fsmModule.DestroyFsm<IProcedureModule> 只能删除iproceduremodule 这样一种类型的状态机了，不可能有多种状态机

**助手：**

我是 GPT-5.1 模型。

### 1. 你的推断差了一点：不是“只能有一种状态机”，而是“**每种 owner 类型默认只能有一个【无名 FSM】**”

结合实际代码看一下键是怎么拼出来的：

- 不带名字创建：

```csharp
public IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class
{
    return CreateFsm(string.Empty, owner, states);
}
```

- 真正创建时用的 key：

```csharp
public IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
{
    TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
    if (HasFsm<T>(name)) ...
    _fsmMap.Add(typeNamePair, fsm);
}
```

- `DestroyFsm<T>()` 用的 key：

```csharp
public bool DestroyFsm<T>() where T : class
{
    return InternalDestroyFsm(new TypeNamePair(typeof(T)));
}
```

`new TypeNamePair(typeof(T))` 等价于 `new TypeNamePair(typeof(T), string.Empty)`，也就是 `(ownerType = typeof(T), name = "")`。

**因此：**

- `DestroyFsm<IProcedureModule>()` 只会销毁 key = `(typeof(IProcedureModule), "")` 的那一个 FSM；
- 这正好对应 `ProcedureModule.Initialize` 里创建的 FSM：

```csharp
_fsmModule = fsmModule;
_procedureFsm = _fsmModule.CreateFsm(this, procedures); // 没传 name -> name = ""
```

### 2. 实际能力是：

- **可以有很多种 owner 类型的 FSM**  
  - 比如 `IFsm<IProcedureModule>`、`IFsm<Player>`、`IFsm<Enemy>` ……  
  - 它们的 key 分别是 `(typeof(IProcedureModule), nameX)`、`(typeof(Player), nameY)` 等，互不影响。

- **同一个 owner 类型下也可以有多份 FSM，只要名字不同：**
  - `CreateFsm<IProcedureModule>("ProcFSM_A", this, statesA)`
  - `CreateFsm<IProcedureModule>("ProcFSM_B", this, statesB)`
  - 对应 key：`(typeof(IProcedureModule), "ProcFSM_A")`、`(typeof(IProcedureModule), "ProcFSM_B")`
  - 销毁时要用：
    - `DestroyFsm<IProcedureModule>("ProcFSM_A")`
    - 或 `DestroyFsm(typeof(IProcedureModule), "ProcFSM_A")`
    - 或直接 `DestroyFsm(fsmInstance)`

- **`DestroyFsm<IProcedureModule>()` 只是一个便捷重载：**  
  - 它只操作“**这个 ownerType 的【默认无名 FSM】**”；  
  - 在当前项目里，`ProcedureModule` 就只创建了这一个无名 FSM，所以用这个重载完全够用。

### 3. 结论（对应你的话）

> “只能删除 iproceduremodule 这样一种类型的状态机，不可能有多种状态机”

更准确的说法应该是：

- `DestroyFsm<IProcedureModule>()` **确实只能删除 ownerType = `IProcedureModule` 且 name = "" 的那一个 FSM**；
- 但整个框架：
  - **支持多种 owner 类型的 FSM**；
  - **支持同一 owner 类型下多份“有名字的 FSM”**，只是销毁时需要用带 name 的 DestroyFsm 重载，而不是这个不带 name 的版本。

---

【对话 5】进一步确认 `DestroyFsm<T>()` 只会删除 name 为空的 FSM

**用户：**

是的，你说的没有错。但是这个函数这样写，只能删除 name=“”的状态机了

**助手：**

我是 GPT-5.1 模型。

你这句理解是**完全正确的**：  
`DestroyFsm<T>()` 这个**不带 name 的重载，逻辑上只能删除 `name == ""` 的那一个 FSM**，也就是“某个 owner 类型的默认 FSM”。

对应实现是：

- 创建默认 FSM 时：`CreateFsm<T>(owner, ...)` 内部等价于 `CreateFsm(string.Empty, owner, ...)`；
- 删除默认 FSM 时：`DestroyFsm<T>()` 内部等价于 `InternalDestroyFsm(new TypeNamePair(typeof(T), string.Empty))`。

这就是它的设计：  

- **它本来就只是“默认无名状态机”的快捷函数，不是通用删除入口。**  
- 要删除其它（有 name 的）状态机，必须用：
  - `DestroyFsm<T>(string name)`  
  - 或 `DestroyFsm(Type ownerType, string name)`  
  - 或 `DestroyFsm(FsmBase fsm)` / `DestroyFsm<T>(IFsm<T> fsm)`。

在当前项目里，`ProcedureModule` 只创建了一个无名的 `IFsm<IProcedureModule>`，所以用 `DestroyFsm<IProcedureModule>()` 刚好契合这个设计；如果以后你要为同一个 owner 类型搞多份命名 FSM，那就需要在你的业务代码里改用带 name 的销毁重载，而不是依赖这个简化版。

---

【对话 6】保存 FsmModule 相关对话到文档

**用户：**

把我们关于fsmmodule的对象保存

1、把整个对话一字不差的保留下来，取个合适的名字，放到合适的目录

2、把整个对话总结一下，取个合适的名字，放到合适的目录，总结一定要保存关键的代码示例 

**助手：**

（当前文件 `TEngine_FsmModule_Conversation_Raw.md` 即为第 1 点的原文保存；第 2 点的总结见同目录下的 `TEngine_FsmModule_Conversation_Summary.md`。）



