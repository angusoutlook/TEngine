## TEngine 更新系统 & UniTask 深度讨论（摘要版）

> 说明：本文件为本次会话的整理版记录，按主题结构化排版，便于后续查阅。  
> 会话时间：2025-12-06  
> 主题：Unity `SleepTimeout`、`Application.lowMemory`、TEngine 的 `ModuleSystem` / `IUpdateDriver` / `UpdateDriver`、以及 UniTask 的 PlayerLoop 集成与线程切换。

---

### 对话整理（按主题）

> 下文内容基于原始问答完整还原技术细节，但在段落顺序和标题上略作整理，以提高可读性。  
> 如需逐轮原文，请参考同目录的 `TEngine_UniTask_ModuleSystem_Conversation_Raw.md`。

---

#### 1. Unity `SleepTimeout` 的含义与用法

**用户提问：**

> UnityEngine sleeptimeout 是什么设定，有什么作用？

**要点回答：**

- `UnityEngine.SleepTimeout` 是一个 **控制设备是否允许自动休眠/熄屏** 的设置，通常通过 `Screen.sleepTimeout` 使用：

```csharp
Screen.sleepTimeout = SleepTimeout.NeverSleep;
```

- 主要取值：
  - `SleepTimeout.NeverSleep`：禁止自动熄屏/锁屏，适合长时间展示内容。  
  - `SleepTimeout.SystemSetting`：使用系统默认休眠策略。
- 主要对移动平台（Android/iOS）有效，PC/主机通常由系统策略决定。
- 使用时注意耗电和恢复系统默认配置，可放在 `RootModule` / `GameEntry` / 关键 UI 界面的生命周期中统一管理。

---

#### 2. `Application.lowMemory` 事件与 `OnLowMemory` 回调

**用户提问：**

> `Application.lowMemory += OnLowMemory;` 有什么作用，什么时候会触发？

**要点回答：**

- 作用：将 `OnLowMemory` 方法订阅到 Unity 的 `Application.lowMemory` 事件，当系统报告内存紧张时，Unity 调用该回调：

```csharp
Application.lowMemory += OnLowMemory;

void OnLowMemory()
{
    // 释放缓存、卸载资源等
}
```

- 触发场景（主要在移动端）：
  - 可用内存过低；  
  - 场景资源过大；  
  - 后台应用过多；  
  - 某些机型切换前后台时。
- 在回调中一般做：
  - 释放缓存（对象池、贴图、音频等）；  
  - 卸载不再使用的资源（如 `Resources.UnloadUnusedAssets()`）；  
  - 必要时触发 GC（慎用）。
- 注意在不再需要时取消订阅：`Application.lowMemory -= OnLowMemory;`，避免静态事件长期持有引用。

在当前 TEngine 中，`RootModule.OnLowMemory` 内部会调用：

- `IObjectPoolModule.ReleaseAllUnused()`  
- `IResourceModule.OnLowMemory()`（若实现）

用以紧急释放对象池和资源。

---

#### 3. `ModuleSystem`、`IUpdateModule` 与 `IUpdateDriver`/`UpdateDriver` 的区别

**用户问题抽象：**

- modulesystem 和某个 “single system”（实际是 `IUpdateDriver` / `UpdateDriver`）都有“更新接口”，有什么不同，是否冗余？

##### 3.1 模块系统：`ModuleSystem + Module + IUpdateModule`

关键代码：

```csharp
// Module.cs
public interface IUpdateModule
{
    void Update(float elapseSeconds, float realElapseSeconds);
}

public abstract class Module
{
    public virtual int Priority => 0;
    public abstract void OnInit();
    public abstract void Shutdown();
}
```

```csharp
// ModuleSystem.cs
public static void Update(float elapseSeconds, float realElapseSeconds)
{
    if (_isExecuteListDirty)
    {
        _isExecuteListDirty = false;
        BuildExecuteList();
    }

    int executeCount = _updateExecuteList.Count;
    for (int i = 0; i < executeCount; i++)
    {
        _updateExecuteList[i].Update(elapseSeconds, realElapseSeconds);
    }
}
```

```csharp
// RootModule.cs
private void Update()
{
    GameTime.StartFrame();
    ModuleSystem.Update(GameTime.deltaTime, GameTime.unscaledDeltaTime);
}
```

**特点：**

- 面向 **框架级模块**（例如 `ObjectPoolModule`、`TimerModule`、`AudioModule`、`FsmModule` 等）。  
- 有模块优先级（`Module.Priority`），控制：
  - 更新顺序；  
  - Shutdown 顺序。  
- 接受逻辑时间和真实时间两个参数，受 `GameSpeed`、暂停等影响。
- 通过 `RootModule.Update()` 驱动，是“**模块级主循环**”。

##### 3.2 `IUpdateDriver` / `UpdateDriver`：MonoBehaviour 能力中心

接口：

```csharp
public interface IUpdateDriver
{
    // 协程控制
    Coroutine StartCoroutine(string methodName);
    Coroutine StartCoroutine(IEnumerator routine);
    void StopCoroutine(string methodName);
    void StopCoroutine(IEnumerator routine);
    void StopCoroutine(Coroutine routine);
    void StopAllCoroutines();

    // Update/FixedUpdate/LateUpdate 事件注入
    void AddUpdateListener(Action action);
    void AddFixedUpdateListener(Action action);
    void AddLateUpdateListener(Action action);
    void RemoveUpdateListener(Action action);
    void RemoveFixedUpdateListener(Action action);
    void RemoveLateUpdateListener(Action action);

    // Unity 生命周期事件注入
    void AddDestroyListener(Action action);
    void RemoveDestroyListener(Action action);
    void AddOnDrawGizmosListener(Action action);
    void RemoveOnDrawGizmosListener(Action action);
    void AddOnDrawGizmosSelectedListener(Action action);
    void RemoveOnDrawGizmosSelectedListener(Action action);
    void AddOnApplicationPauseListener(Action<bool> action);
    void RemoveOnApplicationPauseListener(Action<bool> action);
}
```

实现（节选）：

```csharp
internal class UpdateDriver : Module, IUpdateDriver
{
    private GameObject _entity;
    private MainBehaviour _behaviour;

    public override void OnInit()
    {
        _MakeEntity();
    }

    private void _MakeEntity()
    {
        if (_entity != null) return;

        _entity = new GameObject("[UpdateDriver]");
        _entity.SetActive(true);
        Object.DontDestroyOnLoad(_entity);
        _behaviour = _entity.AddComponent<MainBehaviour>();
    }

    private class MainBehaviour : MonoBehaviour
    {
        private event Action UpdateEvent;
        private event Action FixedUpdateEvent;
        private event Action LateUpdateEvent;
        // ...

        void Update()
        {
            UpdateEvent?.Invoke();
        }

        void FixedUpdate()
        {
            FixedUpdateEvent?.Invoke();
        }

        void LateUpdate()
        {
            LateUpdateEvent?.Invoke();
        }

        // OnDestroy / OnDrawGizmos / OnApplicationPause 等同理
    }
}
```

**特点：**

- `UpdateDriver` 本身是 `Module`，但**没有实现 `IUpdateModule`**，因此**不会**被 `ModuleSystem.Update` 每帧轮询。  
- 通过内部 `MainBehaviour : MonoBehaviour` 直接接收 Unity 的生命周期事件，转换为 C# 事件，并通过 `IUpdateDriver` 暴露。  
- 外部通常通过 `Utility.Unity` 静态方法访问：

```csharp
_updateDriver = ModuleSystem.GetModule<IUpdateDriver>();
```

提供：

- 全局协程启动/停止；  
- 全局 Update/FixedUpdate/LateUpdate 事件订阅；  
- Destroy / OnApplicationPause / OnDrawGizmos 等生命周期事件订阅。

##### 3.3 两者差异总结

- **`IUpdateModule`（ModuleSystem 的更新接口）**：
  - 面向框架模块；  
  - 带 `elapseSeconds` / `realElapseSeconds`，与 GameTime/暂停挂钩；  
  - 有优先级与生命周期管理。

- **`IUpdateDriver`（MonoBehaviour 能力中枢）**：
  - 面向普通逻辑/工具类/静态代码；  
  - 通过事件提供 Unity 的 Update/FixedUpdate/LateUpdate + 协程能力；  
  - 不参与 `ModuleSystem.Update` 的模块执行队列。

二者职责不同，并非简单冗余。

---

#### 4. UpdateDriver 中为何使用 UniTask：`await UniTask.Yield().Forget()`

关键实现：

```csharp
public void AddUpdateListener(Action action)
{
    _MakeEntity();
    AddUpdateListenerImp(action).Forget();
}

private async UniTaskVoid AddUpdateListenerImp(Action action)
{
    await UniTask.Yield();
    _behaviour.AddUpdateListener(action);
}
```

```csharp
public void AddFixedUpdateListener(Action action)
{
    _MakeEntity();
    AddFixedUpdateListenerImp(action).Forget();
}

private async UniTaskVoid AddFixedUpdateListenerImp(Action action)
{
    await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
    _behaviour.AddFixedUpdateListener(action);
}
```

**原因：**

- `await UniTask.Yield()`：
  - 把 `_behaviour.AddUpdateListener(action)` 延迟到下一次 PlayerLoop 指定阶段执行；
  - 避免在当前帧 `Update()` 正在执行时修改事件委托链；
  - 确保 `_MakeEntity()` 完成，`MainBehaviour` 已经存在并稳定。

- `.Forget()`：
  - 不需要调用方 `await` 注册过程，只要保证它在合适时机执行即可；
  - 避免“未观察任务”的警告/潜在问题。

---

#### 5. `Yield` / `YieldAwaitable` / `PlayerLoopTiming` 的含义与执行路径

**`UniTask.Yield()` 源码（节选）：**

```csharp
public static YieldAwaitable Yield()
{
    // optimized for single continuation
    return new YieldAwaitable(PlayerLoopTiming.Update);
}
```

- 返回一个 `YieldAwaitable`，内部记录了目标 `PlayerLoopTiming`（如 `Update`）。  
- 在 `await UniTask.Yield()` 时：
  - 编译器生成：

    ```csharp
    var awaitable = UniTask.Yield();
    var awaiter = awaitable.GetAwaiter();
    if (!awaiter.IsCompleted)
        awaiter.OnCompleted(continuation);
    else
        continuation();
    ```

  - `YieldAwaiter.OnCompleted` 会调用：

    ```csharp
    PlayerLoopHelper.AddContinuation(timing, continuation);
    ```

**`PlayerLoopHelper.AddContinuation`：**

```csharp
public static void AddContinuation(PlayerLoopTiming timing, Action continuation)
{
    var q = yielders[(int)timing];
    if (q == null) ThrowInvalidLoopTiming(timing);
    q.Enqueue(continuation);
}
```

这里的 `yielders` 是 `ContinuationQueue[]`：

```csharp
static ContinuationQueue[] yielders;
```

**`ContinuationQueue.Run()` 在 PlayerLoop 中执行：**

```csharp
public void Run()
{
    // DEBUG 模式下根据 timing 包一层，最终调用 RunCore
    RunCore();
}

void RunCore()
{
    // 设置 dequing 标志，防止并发入队问题
    // 依次执行 actionList 里的所有 Action（即 continuation）
    for (int i = 0; i < actionListCount; i++)
    {
        var action = actionList[i];
        actionList[i] = null;
        try
        {
            action();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
        }
    }

    // 交换 waitingList 和 actionList，准备下一轮
}
```

`PlayerLoopHelper.Initialize` 会把每个 `ContinuationQueue.Run` 注入到对应的 Unity PlayerLoop 阶段（如 `Update`）：

```csharp
var yieldLoop = new PlayerLoopSystem
{
    type = loopRunnerYieldType,
    updateDelegate = cq.Run
};

// copyList[i].subSystemList = InsertRunner(..., yieldLoop, runnerLoop);
PlayerLoop.SetPlayerLoop(playerLoop);
```

**总结执行路径：**

1. `await UniTask.Yield(PlayerLoopTiming.Update)`；  
2. Awaiter 调用 `PlayerLoopHelper.AddContinuation(Update, continuation)`；  
3. `ContinuationQueue<Update>.Enqueue(continuation)`；  
4. 下一次 Unity PlayerLoop.Update 执行 `ContinuationQueue<Update>.Run()`；  
5. 依次执行其中的所有 continuation，恢复 async 方法的“后半截代码”。

---

#### 6. `yielders` 与 `runners` 的职责与调用示例

##### 6.1 `yielders`（ContinuationQueue[]）

用途：**一次性 continuation**，典型场景：

- `UniTask.Yield/Delay/NextFrame`；  
- `UniTask.SwitchToMainThread` 的 continuation；  
- `UniTask.Post`。

示例（真实代码）：

```csharp
// UniTask.Threading.cs
public static void Post(Action action, PlayerLoopTiming timing = PlayerLoopTiming.Update)
{
    PlayerLoopHelper.AddContinuation(timing, action);
}
```

业务用法示例：

```csharp
UniTask.Post(() =>
{
    // 下一次指定 PlayerLoopTiming 执行
}, PlayerLoopTiming.Update);
```

```csharp
// SwitchToMainThreadAwaitable.Awaiter
public void OnCompleted(Action continuation)
{
    PlayerLoopHelper.AddContinuation(playerLoopTiming, continuation);
}
```

##### 6.2 `runners`（PlayerLoopRunner[]）

用途：**每帧执行 `MoveNext()` 直到完成**，典型场景：

- `UniTask.WaitUntil` / `WaitWhile` / `WaitUntilCanceled`；  
- `WaitUntilValueChanged`。

示例（真实代码）：

```csharp
// WaitUntilPromise.Create
PlayerLoopHelper.AddAction(timing, result); // result : IPlayerLoopItem
```

`WaitUntilPromise` 的 `MoveNext()`：

```csharp
public bool MoveNext()
{
    if (cancellationToken.IsCancellationRequested)
    {
        core.TrySetCanceled(cancellationToken);
        return false;
    }

    try
    {
        if (!predicate())
        {
            return true; // 继续下一帧检查
        }
    }
    catch (Exception ex)
    {
        core.TrySetException(ex);
        return false;
    }

    core.TrySetResult(null);
    return false; // 条件满足，结束
}
```

业务调用：

```csharp
await UniTask.WaitUntil(() => isReady, PlayerLoopTiming.Update);
```

由 `runners[(int)Update]` 中的 `PlayerLoopRunner` 每帧调用 `MoveNext` 推进。

---

#### 7. `SwitchToMainThread` 与 `ReturnToMainThread`

##### 7.1 `SwitchToMainThreadAwaitable`

工厂方法：

```csharp
public static SwitchToMainThreadAwaitable SwitchToMainThread(CancellationToken cancellationToken = default)
{
    return new SwitchToMainThreadAwaitable(PlayerLoopTiming.Update, cancellationToken);
}
```

Awaiter 核心逻辑：

```csharp
public bool IsCompleted
{
    get
    {
        var currentThreadId = Thread.CurrentThread.ManagedThreadId;
        return PlayerLoopHelper.MainThreadId == currentThreadId;
    }
}

public void OnCompleted(Action continuation)
{
    PlayerLoopHelper.AddContinuation(playerLoopTiming, continuation);
}
```

**语义：**

- 如果当前就是主线程，`IsCompleted == true`，`await` 立即继续；  
- 否则，将 continuation 丢到主线程对应的 PlayerLoopTiming 上执行。

##### 7.2 `ReturnToMainThread`

工厂方法：

```csharp
public static ReturnToMainThread ReturnToMainThread(PlayerLoopTiming timing, CancellationToken cancellationToken = default)
{
    return new ReturnToMainThread(timing, cancellationToken);
}
```

用法（`await using`）：

```csharp
await using (UniTask.ReturnToMainThread())
{
    // 作用域中可以随意切线程
}
// 离开作用域时必定回主线程
```

`DisposeAsync` 返回 Awaiter，Awaiter 的 `OnCompleted` 一样调用 `PlayerLoopHelper.AddContinuation`，保证在主线程继续执行。

**区别：**

- `SwitchToMainThread`：在调用行**立即切回主线程**。  
- `ReturnToMainThread`：在 using 作用域**结束时**切回主线程，适合作用域锁定。

---

#### 8. `SwitchToThreadPoolAwaitable` 与 `SwitchToTaskPoolAwaitable`

##### 8.1 `SwitchToThreadPoolAwaitable`

实现（节选）：

```csharp
public struct SwitchToThreadPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public struct Awaiter : ICriticalNotifyCompletion
    {
        static readonly WaitCallback switchToCallback = Callback;

        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            ThreadPool.QueueUserWorkItem(switchToCallback, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            ThreadPool.UnsafeQueueUserWorkItem(switchToCallback, continuation);
        }

        static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}
```

用法：

```csharp
await UniTask.SwitchToThreadPool(); // 把 await 之后代码放到 .NET 线程池线程执行
```

##### 8.2 `SwitchToTaskPoolAwaitable`

实现（节选）：

```csharp
public struct SwitchToTaskPoolAwaitable
{
    public Awaiter GetAwaiter() => new Awaiter();

    public struct Awaiter : ICriticalNotifyCompletion
    {
        static readonly Action<object> switchToCallback = Callback;

        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            Task.Factory.StartNew(
                switchToCallback,
                continuation,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Task.Factory.StartNew(
                switchToCallback,
                continuation,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        static void Callback(object state)
        {
            var continuation = (Action)state;
            continuation();
        }
    }
}
```

**对比：**

- 都会让 continuation 在后台线程执行，通常是同一个 .NET 线程池。  
- 区别在“调度 API”：
  - `SwitchToThreadPool`：直接 `ThreadPool.QueueUserWorkItem`，不生成 Task。  
  - `SwitchToTaskPool`：走 `Task.Factory.StartNew(..., TaskScheduler.Default)`，理论上可作为 Task 使用，但此处结果 Task 未被保存。
- 源码注释推荐使用 `SwitchToThreadPool`。

---

#### 9. 三条“推进链”与 `ObjectPoolModule` 的位置

**三条推进器总览：**

1. **模块系统链路**（框架模块）：

```csharp
Unity PlayerLoop
    → RootModule : MonoBehaviour → RootModule.Update()
        → ModuleSystem.Update(GameTime.deltaTime, GameTime.unscaledDeltaTime)
            → 所有 IUpdateModule.Update(...)
                → 例如 ObjectPoolModule.Update(...)
```

2. **IUpdateDriver / UpdateDriver 链路**（MonoBehaviour 能力中心）：

```csharp
UpdateDriver : Module, IUpdateDriver
    → OnInit() 中创建 [UpdateDriver] GameObject + MainBehaviour : MonoBehaviour
        → MainBehaviour.Update/FixedUpdate/LateUpdate/OnDestroy/OnApplicationPause
            → C# 事件 (UpdateEvent / FixedUpdateEvent / ...)
                → 通过 IUpdateDriver / Utility.Unity.AddXXXListener 注册的回调执行
```

3. **UniTask PlayerLoop 链路**（异步任务）：

```csharp
PlayerLoopHelper.Initialize
    → 往各个 PlayerLoopTiming 注入：
        → ContinuationQueue.Run()（yielders）
        → PlayerLoopRunner.Run()（runners）

await UniTask.Yield / WaitUntil / SwitchToMainThread / Post / ...
    → AddContinuation / AddAction
        → yielders[] / runners[] 中排队
            → PlayerLoop 对应阶段调用 Run() 推进
```

**ObjectPoolModule 在其中的位置：**

```csharp
internal sealed partial class ObjectPoolModule : Module, IObjectPoolModule, IUpdateModule
{
    public override int Priority => 6;

    public void Update(float elapseSeconds, float realElapseSeconds)
    {
        foreach (KeyValuePair<TypeNamePair, ObjectPoolBase> objectPool in _objectPools)
        {
            objectPool.Value.Update(elapseSeconds, realElapseSeconds);
        }
    }
}
```

推进链：

1. Unity PlayerLoop → `RootModule.Update()`；  
2. `RootModule.Update()` → `ModuleSystem.Update(...)`；  
3. `ModuleSystem.Update` → 调用包含 `ObjectPoolModule` 在内的所有 `IUpdateModule.Update`；  
4. `ObjectPoolModule.Update` → 遍历 `_objectPools`，调用每个 `ObjectPoolBase.Update`，进行释放/过期处理。

在这条路径上：

- `ObjectPoolModule` 不依赖 `IUpdateDriver`；  
- 也不直接依赖 UniTask，只需同步 Tick 即可（如需异步预热，可以在内部再用 UniTask）。

---

### 结语

本文件是对本次长对话的按主题整理版（“摘要版”），聚焦于：

- Unity 休眠与内存告警；  
- TEngine 模块系统与 UpdateDriver；  
- UniTask 的 PlayerLoop 集成与线程切换；  
- 三条推进器的关系以及 `ObjectPoolModule` 在其中的角色。

如需查看“逐轮原始对话”（包括每次提问与回答的完整文本），请参见同目录下的：  
`TEngine_UniTask_ModuleSystem_Conversation_Raw.md`。


