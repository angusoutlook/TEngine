# ProcedureModule 与 FSM 连接机制分析
_Exported on 2025-01-27 from Cursor_

---

## 问题一：ProcedureModule 靠什么推进？update？还是fsm？还是iupdatemodule？

**User**

proceduremodule 靠什么推进？update？还是fsm？还是iupdatemodule？

---

**Assistant**

我是 **Auto**，一个由 Cursor 设计的智能体路由器。正在查找 ProcedureModule 的实现，分析其推进机制。

**答案：ProcedureModule 靠 FSM（通过 FsmModule 的 IUpdateModule）推进**

### 关键代码分析

**1. ProcedureModule 本身没有 Update 方法**

```8:26:UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureModule.cs
internal sealed class ProcedureModule : Module, IProcedureModule
{
    private IFsmModule _fsmModule;
    private IFsm<IProcedureModule> _procedureFsm;
    // ... 没有实现 IUpdateModule，没有 Update 方法
    public override int Priority => -2;
}
```

**2. FsmModule 实现了 IUpdateModule，负责推进所有 FSM**

```9:61:UnityProject/Assets/TEngine/Runtime/Module/FsmModule/FsmModule.cs
internal sealed class FsmModule : Module, IFsmModule, IUpdateModule
{
    // ...
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

            fsm.Update(elapseSeconds, realElapseSeconds);  // 调用每个 FSM 的 Update
        }
    }
}
```

**3. Fsm<T>.Update 调用当前状态的 OnUpdate**

```454:463:UnityProject/Assets/TEngine/Runtime/Module/FsmModule/Fsm.cs
internal override void Update(float elapseSeconds, float realElapseSeconds)
{
    if (_currentState == null)
    {
        return;
    }

    _currentStateTime += elapseSeconds;
    _currentState.OnUpdate(this, elapseSeconds, realElapseSeconds);  // 调用当前状态的 OnUpdate
}
```

**4. ProcedureBase.OnUpdate 被调用**

```34:37:UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureBase.cs
protected internal override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
{
    base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
}
```

### 推进流程链

```
ModuleSystem.Update()
    ↓
FsmModule.Update()  (IUpdateModule)
    ↓
Fsm<IProcedureModule>.Update()
    ↓
ProcedureBase.OnUpdate()  (当前流程的 OnUpdate)
```

### 设计意图

- **ProcedureModule 不直接实现 IUpdateModule**：避免重复更新，因为流程本身是 FSM 的状态
- **由 FsmModule 统一管理**：所有 FSM 的更新都由 FsmModule 统一管理，包括 ProcedureModule 内部的 `_procedureFsm`
- **流程作为 FSM 状态**：流程本质上是 FSM 的状态，通过 FSM 的更新机制来推进

**结论：ProcedureModule 通过 FSM（由 FsmModule 的 IUpdateModule 驱动）推进，而不是 ProcedureModule 自己的 Update 或 IUpdateModule。**

---

## 问题二：ProcedureModule 如何与 FsmModule 建立连接？

**User**

procudure是如何于fsmmodule建立连接的？

---

**Assistant**

查找 ProcedureModule 如何与 FsmModule 建立连接：

**答案：ProcedureModule 与 FsmModule 的连接过程**

### 1. 连接入口：ProcedureSetting.StartProcedure()

```55:97:UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureSetting.cs
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
```

**关键步骤：**
- 第 59 行：通过 `ModuleSystem.GetModule<IProcedureModule>()` 获取 ProcedureModule
- 第 97 行：通过 `ModuleSystem.GetModule<IFsmModule>()` 获取 FsmModule，并调用 `Initialize`

### 2. 建立连接：ProcedureModule.Initialize()

```86:95:UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureModule.cs
public void Initialize(IFsmModule fsmModule, params ProcedureBase[] procedures)
{
    if (fsmModule == null)
    {
        throw new GameFrameworkException("FSM manager is invalid.");
    }

    _fsmModule = fsmModule;
    _procedureFsm = _fsmModule.CreateFsm(this, procedures);
}
```

**关键操作：**
- 第 93 行：保存 `_fsmModule` 引用
- 第 94 行：调用 `_fsmModule.CreateFsm(this, procedures)` 创建 FSM
  - `this` 作为 FSM 的 owner（类型为 `IProcedureModule`）
  - `procedures` 作为 FSM 的状态集合

### 3. FsmModule 注册 FSM

```226:250:UnityProject/Assets/TEngine/Runtime/Module/FsmModule/FsmModule.cs
public IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states) where T : class
{
    return CreateFsm(string.Empty, owner, states);
}

/// <summary>
/// 创建有限状态机。
/// </summary>
/// <typeparam name="T">有限状态机持有者类型。</typeparam>
/// <param name="name">有限状态机名称。</param>
/// <param name="owner">有限状态机持有者。</param>
/// <param name="states">有限状态机状态集合。</param>
/// <returns>要创建的有限状态机。</returns>
public IFsm<T> CreateFsm<T>(string name, T owner, params FsmState<T>[] states) where T : class
{
    TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
    if (HasFsm<T>(name))
    {
        throw new GameFrameworkException(Utility.Text.Format("Already exist FSM '{0}'.", typeNamePair));
    }

    Fsm<T> fsm = Fsm<T>.Create(name, owner, states);
    _fsmMap.Add(typeNamePair, fsm);
    return fsm;
}
```

**关键操作：**
- 第 247 行：创建 `Fsm<T>` 实例
- 第 248 行：将 FSM 添加到 `_fsmMap` 字典中，由 FsmModule 统一管理

### 连接流程总结

```
ProcedureSetting.StartProcedure()
    ↓
1. ModuleSystem.GetModule<IProcedureModule>()  // 获取 ProcedureModule
    ↓
2. ModuleSystem.GetModule<IFsmModule>()        // 获取 FsmModule
    ↓
3. _procedureModule.Initialize(fsmModule, procedures)
    ↓
4. _fsmModule.CreateFsm(this, procedures)      // 在 FsmModule 中创建 FSM
    ↓
5. FsmModule._fsmMap.Add(typeNamePair, fsm)     // 注册到 FsmModule 的字典
    ↓
6. 返回 IFsm<IProcedureModule> 给 ProcedureModule._procedureFsm
```

### 设计要点

1. **依赖注入**：`ProcedureModule` 通过 `Initialize` 方法接收 `IFsmModule`，而不是直接创建
2. **服务定位器模式**：通过 `ModuleSystem.GetModule<T>()` 获取模块实例
3. **统一管理**：FSM 由 `FsmModule` 统一管理，便于统一更新和销毁
4. **接口解耦**：使用 `IFsmModule` 和 `IFsm<T>` 接口，降低耦合度

**结论：连接通过 `ProcedureSetting.StartProcedure()` → `ProcedureModule.Initialize()` → `FsmModule.CreateFsm()` 建立，`FsmModule` 将创建的 FSM 注册到内部字典，完成连接。**

---

## 问题三：保存会话内容

**User**

把这次会话的内容，一字不漏的保存下来，取个合适的名字，放到合适的目录

---

**Assistant**

已将本次会话内容保存到文档。
