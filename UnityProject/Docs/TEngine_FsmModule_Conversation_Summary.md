TEngine FsmModule 模块讨论总结
=============================

> 说明：本文件是对 `TEngine_FsmModule_Conversation_Raw.md` 的精简总结，保留关键信息、设计意图和关键代码示例，便于快速阅读与回顾。详细原文请查看同目录下的 Raw 文件。

---

### 1. 模块整体结构与职责分层

- **基础抽象：`FsmBase`**
  - 抽象出所有状态机的公共信息：`Name`、`FullName`（基于 `TypeNamePair(OwnerType, Name)`）、`OwnerType`、`FsmStateCount`、`IsRunning`、`IsDestroyed`、`CurrentStateName`、`CurrentStateTime`。
  - 提供统一的生命周期接口：`internal abstract void Update(...)` 与 `internal abstract void Shutdown()`，供管理模块统一调度。

- **具体状态机：`IFsm<T>` 与 `Fsm<T>`**
  - `IFsm<T>` 暴露强类型 API：启动状态（`Start<TState>()`）、查询/获取状态（`HasState` / `GetState` / `GetAllStates`）、以及一个通用的数据字典（`HasData` / `GetData` / `SetData` / `RemoveData`）。
  - `Fsm<T>` 继承 `FsmBase` 且实现 `IMemory`，内部使用：

    ```csharp
    private readonly Dictionary<Type, FsmState<T>> _states;
    private Dictionary<string, object> _dataMap;
    private FsmState<T> _currentState;
    private float _currentStateTime;
    private bool _isDestroyed;
    ```

  - 通过 `Fsm<T>.Create(name, owner, states...)` 使用 `MemoryPool.Acquire<Fsm<T>>()` 创建实例，注册所有 `FsmState<T>` 状态并调用它们的 `OnInit`。

- **状态基类：`FsmState<T>`**
  - 定义状态生命周期钩子：`OnInit`、`OnEnter`、`OnUpdate`、`OnLeave`、`OnDestroy`（均为 `protected internal virtual`，由状态子类按需重写）。
  - 提供内部切换封装：

    ```csharp
    protected void ChangeState<TState>(IFsm<T> fsm) where TState : FsmState<T>
    {
        Fsm<T> fsmImplement = (Fsm<T>)fsm;
        if (fsmImplement == null)
        {
            throw new GameFrameworkException("FSM is invalid.");
        }
        fsmImplement.ChangeState<TState>();
    }
    ```

- **管理模块：`IFsmModule` 与 `FsmModule`**
  - 持有字典 `Dictionary<TypeNamePair, FsmBase> _fsmMap`，Key 为 `(OwnerType, Name)` 组合值 `TypeNamePair`，支持同一 owner 类型多份命名 FSM。
  - 对外接口支持：
    - 检查是否存在 FSM：`HasFsm<T>() / HasFsm<T>(string name) / HasFsm(Type ownerType, string name)`；
    - 获取 FSM：`GetFsm<T>() / GetFsm<T>(string name) / GetFsm(Type, string)`；
    - 创建 FSM：多种重载，既支持 `params FsmState<T>[]` 又支持 `List<FsmState<T>>`；
    - 销毁 FSM：按类型、类型+名称、或直接传 `FsmBase` / `IFsm<T>`。
  - 实现 `IUpdateModule`，在 `Update` 中使用 `_tempFsmList` 快照遍历，避免遍历字典时修改它：

    ```csharp
    public void Update(float elapseSeconds, float realElapseSeconds)
    {
        _tempFsmList.Clear();
        if (_fsmMap.Count <= 0) return;

        foreach (var kv in _fsmMap)
        {
            _tempFsmList.Add(kv.Value);
        }

        foreach (FsmBase fsm in _tempFsmList)
        {
            if (fsm.IsDestroyed) continue;
            fsm.Update(elapseSeconds, realElapseSeconds);
        }
    }
    ```

---

### 2. Fsm 与 MemoryPool 的关系与生命周期

- **`Fsm<T>` 实现 `IMemory`，其 `Shutdown` 与 `Clear` 的真实调用链为：**

  ```csharp
  // FsmModule 内部销毁
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

  // Fsm<T>
  internal override void Shutdown()
  {
      MemoryPool.Release(this);
  }

  // MemoryPool
  public static void Release(IMemory memory)
  {
      if (memory == null) throw new Exception("Memory is invalid.");
      Type memoryType = memory.GetType();
      InternalCheckMemoryType(memoryType);
      GetMemoryCollection(memoryType).Release(memory);
  }

  // MemoryCollection
  public void Release(IMemory memory)
  {
      memory.Clear();      // 调用 Fsm<T>.Clear()
      lock (_memories)
      {
          if (_enableStrictCheck && _memories.Contains(memory))
              throw new Exception("The memory has been released.");
          _memories.Enqueue(memory);
      }
      _releaseMemoryCount++;
      _usingMemoryCount--;
  }
  ```

- **`Fsm<T>.Clear` 负责真正“逻辑上的销毁”：**

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

- **设计意图与安全性说明：**
  - MemoryPool **不会主动调用 `Shutdown`**，只是被 `FsmModule` 通过 `DestroyFsm` 间接触发。
  - `Shutdown()` 只是把 FSM 交给内存池，实际清理由 `Clear()` 完成：状态离开、状态销毁、解除对 owner 与数据的引用、标记 `IsDestroyed=true`。
  - 只要你不在状态内部的 `OnUpdate` 等回调里主动 `DestroyFsm` 自己，FSM 不会“执行到一半被回收”；下一帧 `FsmModule.Update` 会根据 `IsDestroyed` 跳过已销毁的 FSM。
  - FSM 不负责销毁 owner 或其内部资源，只是**解除引用**并通过状态的 `OnDestroy` 留给你做清理，真正的内存回收由 GC 或业务侧负责。

---

### 3. `TypeNamePair` 作为 Key 与多实例 FSM 支持

- **`TypeNamePair` 是值类型 struct，采用“类型 + 名称”作为值相等标准：**

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

      public bool Equals(TypeNamePair value)
      {
          return _type == value._type && _name == value._name;
      }
  }
  ```

- **含义与影响：**
  - 在 `Dictionary<TypeNamePair, FsmBase>` 中，Key 比较依赖 `Equals` 与 `GetHashCode`，因此只要 `OwnerType` 与 `Name` 相同，就被视为同一个 FSM 实例。
  - 允许：
    - 不同 owner 类型拥有各自 FSM：`IFsm<IProcedureModule>`、`IFsm<Player>`、`IFsm<Enemy>` 等互不影响；
    - 同一 owner 类型下存在多个命名 FSM，例如：

      ```csharp
      var fsmA = fsmModule.CreateFsm<IProcedureModule>("ProcFSM_A", owner, statesA);
      var fsmB = fsmModule.CreateFsm<IProcedureModule>("ProcFSM_B", owner, statesB);

      fsmModule.DestroyFsm<IProcedureModule>("ProcFSM_A");
      ```

  - 不带 name 的 API，例如：

    ```csharp
    public IFsm<T> CreateFsm<T>(T owner, params FsmState<T>[] states)
    {
        return CreateFsm(string.Empty, owner, states);
    }

    public bool DestroyFsm<T>() where T : class
    {
        return InternalDestroyFsm(new TypeNamePair(typeof(T)));
    }
    ```

    本质上只操作 `Name == ""` 的“默认 FSM”；在当前项目中，`ProcedureModule` 就是只创建了一个无名的 `IFsm<IProcedureModule>`，因此 `DestroyFsm<IProcedureModule>()` 只会删除那一个。

---

### 4. 案例：Procedure 系统如何使用 FsmModule

- **流程基类：`ProcedureBase : FsmState<IProcedureModule>`**

  ```csharp
  using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

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
  ```

- **流程管理模块：`ProcedureModule` 利用 `IFsmModule` 创建“流程 FSM”**

  ```csharp
  internal sealed class ProcedureModule : Module, IProcedureModule
  {
      private IFsmModule _fsmModule;
      private IFsm<IProcedureModule> _procedureFsm;

      public void Initialize(IFsmModule fsmModule, params ProcedureBase[] procedures)
      {
          if (fsmModule == null)
          {
              throw new GameFrameworkException("FSM manager is invalid.");
          }

          _fsmModule = fsmModule;
          _procedureFsm = _fsmModule.CreateFsm(this, procedures); // owner = this, name = ""
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

- **配置入口：`ProcedureSetting` 在 Unity 中通过 ScriptableObject 驱动**

  ```csharp
  [CreateAssetMenu(menuName = "TEngine/ProcedureSetting", fileName = "ProcedureSetting")]
  public sealed class ProcedureSetting : ScriptableObject
  {
      [SerializeField] private string[] availableProcedureTypeNames = null;
      [SerializeField] private string entranceProcedureTypeName = null;

      private IProcedureModule _procedureModule = null;
      private ProcedureBase _entranceProcedure = null;

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

### 5. 简化使用示例：自定义玩家状态机（说明用示例）

> 以下为**简化示例代码**，用于说明如何按现有架构使用 `IFsmModule` 创建普通业务 FSM，并非仓库中的真实代码。

```csharp
// Owner 类型
public class Player
{
    public string Name;
}

// 待机状态
public class PlayerIdleState : FsmState<Player>
{
    protected internal override void OnEnter(IFsm<Player> fsm)
    {
        var owner = fsm.Owner;
        Log.Info($"Player {owner.Name} enter Idle.");
    }

    protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
    {
        // 某些条件下切换到移动
        if (/* 输入按键 */ false)
        {
            ChangeState<PlayerMoveState>(fsm);
        }
    }
}

// 移动状态
public class PlayerMoveState : FsmState<Player>
{
    protected internal override void OnEnter(IFsm<Player> fsm)
    {
        Log.Info($"Player {fsm.Owner.Name} start moving.");
    }

    protected internal override void OnUpdate(IFsm<Player> fsm, float elapseSeconds, float realElapseSeconds)
    {
        // 条件满足回到 Idle
        if (/* 松开按键 */ false)
        {
            ChangeState<PlayerIdleState>(fsm);
        }
    }
}

// 使用 FSM
public class PlayerController
{
    private Player _player;
    private IFsm<Player> _playerFsm;

    public void Init()
    {
        _player = new Player { Name = "Hero" };

        var fsmModule = ModuleSystem.GetModule<IFsmModule>();

        _playerFsm = fsmModule.CreateFsm(_player,
            new PlayerIdleState(),
            new PlayerMoveState()
        );

        _playerFsm.Start<PlayerIdleState>();
    }
}
```

- 该示例展示了：如何定义 `FsmState<T>` 派生状态、通过 `IFsmModule.CreateFsm` 注册状态并创建 FSM、以及在状态中使用 `ChangeState<TState>` 切换逻辑。
- 这与项目中 `ProcedureModule + ProcedureBase` 使用 FSM 的方式是一致的，只是 owner 类型与状态含义不同。



