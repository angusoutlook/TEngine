## TEngine UniTask Awaiter & C# Async 规范（摘要版）

> 说明：本文件为本次会话的整理版记录，按主题结构化排版，便于后续查阅。  
> 会话时间：2025-12-12  
> 主题：UniTask 的架构设计、C# 自定义 awaiter / task-like 规范、`IsCompleted` + `OnCompleted` 的编译器语义，以及基于真实源码的 `UniTask.Awaiter` 实现解析。

---

### 1. UniTask 的整体架构设计

**用户关注点：**

> 我们继续了 UniTask 的设计意图和架构，UniTask 是如何实现架构的？

**要点整理：**

- **设计目标**：
  - 在 Unity 环境下实现 **高性能、低 GC** 的异步方案，替代 `Task` 带来的频繁堆分配与装箱。  
  - 完全兼容 C# `async/await` 语法，同时对 Unity 主线程（PlayerLoop）、帧同步、`AsyncOperation`/`Coroutine` 等做专门优化。  
  - 提供一套可组合的异步“运算符”：延时、超时、取消、`WhenAll/WhenAny` 等。

- **三层核心抽象：`UniTask` / `IUniTaskSource` / Awaiter**：
  - **底层 `IUniTaskSource`**：统一抽象“一个可被 await 的异步操作”，负责：
    - 状态记录（未完成/成功/失败/取消）。
    - 存放 continuation（awaiter 注册的回调）。
    - 在完成时调用 continuation + 返回结果或抛异常。
    - 通常结合对象池复用实例（例如 Delay、Timeout、AsyncOperation 适配等）。
  - **中层 `UniTask`/`UniTask<T>` struct**：
    - 自身是轻量值类型，只持有：`IUniTaskSource` 引用 + `short token`。  
    - 所有真实状态都在 `IUniTaskSource`；`UniTask` 只作为“任务句柄”。
  - **上层 Awaiter**：
    - `UniTask.GetAwaiter()` 返回一个 `Awaiter`（struct），实现 `ICriticalNotifyCompletion`。  
    - 对编译器暴露 `IsCompleted` / `OnCompleted` / `GetResult`，内部再回调 `IUniTaskSource`。

- **调度与执行：PlayerLoop + 自定义 Scheduler**：
  - UniTask 将自己的 Runner 注册到 Unity 的 `PlayerLoopSystem` 不同阶段（`Update`、`LateUpdate` 等）。
  - 所有需要在主线程/特定帧回调的 continuation 会被放入对应 Runner 队列，在那一帧执行（`UniTask.Yield(PlayerLoopTiming.Update)`、`NextFrame` 等即基于此）。
  - 内部维护工作队列 + 对象池，避免频繁创建 `Task` 对象，延时和计时操作通过每帧检查到期来推动。

- **Unity 适配 & 组合 API**：
  - 通过扩展方法适配 `AsyncOperation` / `ResourceRequest` / `AssetBundleRequest` 等为 `UniTask`。  
  - 封装 Unity 事件 / 协程 等为可 await 的任务。  
  - 高层提供 `Delay`、`DelayFrame`、`Timeout`、`WhenAll`、`WhenAny`、`AttachExternalCancellation` 等组合 API，都只是基于 `IUniTaskSource` 的状态机组合。

---

### 2. C# 默认 Task/Awaiter 与自定义实现需要遵守的规范

**用户问题：**

> C# 本来有自己的一套默认的 awaiter 和 Task 吗？
> 如果自己要开发一套符合 C# 标准，需要遵循什么规范、注意哪些坑？

**关键结论：**

- **是的，C# 自带默认实现**：
  - 任务类型：`Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`。
  - awaiter 类型：`TaskAwaiter` / `TaskAwaiter<T>` / `ValueTaskAwaiter` 等。
- **但编译器真正依赖的是“模式（pattern）”，不是特定类型**：
  - **awaiter 模式**：让任意类型 `T` 能被 `await`。  
  - **task-like 类型规范 + AsyncMethodBuilder**：让自定义类型可以作为 `async` 方法返回值。

**2.1 awaiter 模式（只想被 `await` 时要满足的条件）**

要让某个类型 `T` 能写成 `await someT`，需要：

- 在 `T` 上提供：
  - `public Awaiter GetAwaiter();` 或类似签名，返回 awaiter 类型（可以是 struct）。
- awaiter 类型本身要满足：
  - `bool IsCompleted { get; }`：
    - `true` → 操作已完成，可同步 `GetResult`。  
    - `false` → 操作未完成，编译器会挂起状态机并调用 `OnCompleted`。
  - `void OnCompleted(Action continuation)`（以及可选的 `UnsafeOnCompleted`）：
    - 编译器将“后续要执行的逻辑”封装为 `Action continuation` 传入。  
    - 底层异步完成时必须调用这个 `continuation` 恢复状态机。
  - `GetResult()`：
    - 无返回值：`void GetResult()`；有返回值：`TResult GetResult()`。  
    - 异常和取消应在这里抛出，保证 `async` 调用方看到一致行为。

> 这些是 **约定式模式**，不强制要求实现某个接口，但生产环境中通常会实现 `INotifyCompletion` / `ICriticalNotifyCompletion` 以配合编译器优化和安全性。

**2.2 task-like 类型（想写 `async MyTask Foo()`）**

要让自定义类型作为 `async` 方法返回值，例如：

```csharp
public async UniTask<int> MyMethod() { ... }
```

需要额外满足：

- **在类型上标注 `AsyncMethodBuilder` 特性**：

```csharp
[AsyncMethodBuilder(typeof(UniTaskAsyncMethodBuilder<>))]
public struct UniTask<T> { ... }
```

- 提供对应的 builder 类型（如 `UniTaskAsyncMethodBuilder` / `UniTaskAsyncMethodBuilder<T>`），实现：
  - `static TBuilder Create()`
  - `void Start(ref TStateMachine stateMachine)`
  - `void SetResult(TResult result)`
  - `void SetException(Exception exception)`
  - `void AwaitOnCompleted<TAwaiter, TStateMachine>(...)`
  - `TTask Task { get; }`（返回最终任务对象，如 `UniTask<T>`）。

编译器在看到 `async UniTask<T>` 时，会使用这些 builder API 来构建和驱动状态机，从而达到“看起来像 Task，但底层全是你自己的实现”的效果。

**2.3 设计 Task/Awaiter 时的关键规范与注意点**

- **语义要与原生 Task 一致或清晰可预期**：
  - 成功 / 失败 / 取消的表达：
    - 通常成功为正常返回，失败在 `GetResult` 抛出异常。  
    - 取消一般使用 `OperationCanceledException`，或至少在库内保持统一。
  - 是否允许 **对同一个任务多次 await** 必须明确：
    - `Task` 默认支持多 await 共用结果。  
    - 高性能 struct 型任务（如 UniTask 的部分用法）常常只支持单次 await，需要在 Debug / 文档中提示。

- **线程与上下文语义**：
  - `OnCompleted` 触发 continuation 时，应明确：
    - 在主线程？线程池？原始 SynchronizationContext？  
  - 建议提供类似 `.ConfigureAwait(false)` 或分离 API（例如 UniTask 的 `SwitchToMainThread`、`SwitchToThreadPool`），让调用方能控制线程/上下文切换。

- **异常传播与观测策略**：
  - 所有异步过程中的异常统一通过 `GetResult` 抛出。  
  - 对“未观察到的异常”要有清晰策略（日志、调试断言、全局事件等）。

- **取消与超时模型**：
  - 通常与 `CancellationToken` 结合，或提供超时包装。  
  - 需要定义清楚：取消时是否调用 continuation、释放哪些资源、是否抛例外等。

- **性能与 GC 细节**：
  - 倾向使用 struct + 对象池，避免频繁分配。  
  - 避免闭包捕获（lambda 引用外部变量）带来的隐形堆分配。  
  - 避免装箱（例如将值类型装进 `object` 或 `Action` 时的隐性分配）。

---

### 3. 为什么 `IsCompleted == false` 时必须挂起状态机并调用 `OnCompleted`

**用户问题：**

> 为什么 `IsCompleted` 为 false 没有完成，要挂起状态机，调用 `OnCompleted`？

**编译器视角下的执行流程：**

```csharp
await someAwaitable;

// 编译器展开后近似为：
var awaiter = someAwaitable.GetAwaiter();
if (awaiter.IsCompleted)
{
    awaiter.GetResult();
    goto 下一段状态机代码;
}
else
{
    // 需要挂起：保存状态机，注册回调
    awaiter.OnCompleted(Continuation);
}
```

- 当 `IsCompleted == true`：
  - 表示异步操作已完成，编译器可以 **同步调用 `GetResult()`**，直接执行后续代码，不需要真正“异步”。
- 当 `IsCompleted == false`：
  - 如果在此处同步等待（例如 `while(!IsCompleted){}`）会**卡死线程**（在 Unity 中就是主线程卡死、画面不再刷新）。
  - 正确做法是：
    - 将当前 `async` 方法转换为 **状态机对象**，保存所有局部变量和当前执行位置。  
    - 构造一个 continuation（本质是 `stateMachine.MoveNext` 的封装）。  
    - 调用 `awaiter.OnCompleted(continuation)` 把“后续要做的事情”注册给 awaiter。

**为什么不能“直接等”而要靠 `OnCompleted`？**

- `await` 的设计目标是 **“不阻塞线程地等待结果”**：
  - 主线程可以继续跑其他逻辑（渲染、输入、其他异步操作）。
  - 异步操作完成后，由 awaiter 再调用 continuation 恢复状态机。
- `OnCompleted` 是编译器与 awaiter 之间的 **语言级协议**：
  - `IsCompleted` 决定走同步路径还是异步路径。  
  - 异步路径就必须走 `OnCompleted(continuation)`，否则状态机永远不会恢复，`await` 后的代码就永远不执行。

**形象比喻：**

- `IsCompleted == true`：外卖已经到了门口，你直接开门拿（同步继续执行）。
- `IsCompleted == false` + `OnCompleted(continuation)`：外卖还在路上，你先去干别的，在外卖上写“到了给我打电话”（注册 continuation，完成时再回来执行后半段）。

---

### 4. 基于真实源码的 `UniTask.Awaiter` 规范实现（无返回值版）

**用户要求：**

> 使用 UniTask 最简洁的一个真实 awaiter 实现，讲解整个规范的实现。

以下内容 **完全基于你工程中 `UnityProject/Packages/UniTask/Runtime/UniTask.cs` 的真实代码**。

#### 4.1 `GetAwaiter()`：让 `UniTask` 可以被 `await`

```csharp
// 文件：UnityProject/Packages/UniTask/Runtime/UniTask.cs
[DebuggerHidden]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public Awaiter GetAwaiter()
{
    return new Awaiter(this);
}
```

- 满足 C# awaiter 模式的入口要求：提供 `public Awaiter GetAwaiter()`。  
- 编译器遇到 `await someUniTask` 时，会先生成 `var awaiter = someUniTask.GetAwaiter();`。

#### 4.2 `UniTask.Awaiter` 的完整实现

```csharp
public readonly struct Awaiter : ICriticalNotifyCompletion
{
    readonly UniTask task;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Awaiter(in UniTask task)
    {
        this.task = task;
    }

    public bool IsCompleted
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return task.Status.IsCompleted();
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult()
    {
        if (task.source == null) return;
        task.source.GetResult(task.token);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation)
    {
        if (task.source == null)
        {
            continuation();
        }
        else
        {
            task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation)
    {
        if (task.source == null)
        {
            continuation();
        }
        else
        {
            task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
        }
    }

    /// <summary>
    /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
    /// </summary>
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SourceOnCompleted(Action<object> continuation, object state)
    {
        if (task.source == null)
        {
            continuation(state);
        }
        else
        {
            task.source.OnCompleted(continuation, state, task.token);
        }
    }
}
```

**对应 C# await 规范的关键点：**

- `IsCompleted`：
  - 直接返回 `task.Status.IsCompleted()`，而 `Status` 又来自底层 `IUniTaskSource.GetStatus(token)`。  
  - 当 `source == null` 时，`Status` 被视为 `Succeeded`，代表“已完成、立即可用”。
- `GetResult()`：
  - `source == null` → 无需任何操作，直接返回（对应已完成的默认任务）。  
  - `source != null` → 调用 `source.GetResult(token)`：
    - 成功时正常返回。  
    - 失败 / 取消时抛出异常，包括 `OperationCanceledException` 等，由编译器向上传播。
- `OnCompleted` / `UnsafeOnCompleted`：
  - `source == null` → 直接同步 `continuation()`，不真正挂起。  
  - `source != null` → 调用 `source.OnCompleted(...)` 注册 continuation，由底层在完成时回调。
- `ICriticalNotifyCompletion`：
  - 实现 `OnCompleted` + `UnsafeOnCompleted`，与编译器的高级 awaiter 协作接口兼容。

#### 4.3 `AwaiterActions`：统一的 continuation 包装，避免 GC

```csharp
internal static class AwaiterActions
{
    internal static readonly Action<object> InvokeContinuationDelegate = Continuation;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void Continuation(object state)
    {
        ((Action)state).Invoke();
    }
}
```

- `IUniTaskSource.OnCompleted` 的签名是：

```csharp
void OnCompleted(Action<object> continuation, object state, short token);
```

- 而编译器生成的 continuation 类型是 `Action`，为此 UniTask：
  - 使用一个 **静态共享的 `Action<object>`**：`AwaiterActions.InvokeContinuationDelegate`。  
  - 将真实 continuation 作为 `state` 传入，在回调时 `((Action)state).Invoke()`。
- 这样设计的好处：
  - 所有 continuation 共用同一个委托实例，避免为每次注册都 new 一个 `Action<object>`，减少 GC 压力。  
  - 简化 `IUniTaskSource` 的实现，统一回调路径。

#### 4.4 `UniTask` 作为 task-like 返回类型：`AsyncMethodBuilder` 特性

```csharp
[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct UniTask
```

- 通过 `AsyncMethodBuilder` 特性，告诉编译器：
  - 当方法签名是 `async UniTask` 时，使用 `AsyncUniTaskMethodBuilder` 来驱动状态机。  
- `AsyncUniTaskMethodBuilder` 在其他文件中实现了标准 task-like builder 接口：
  - `Create` / `Start` / `SetResult` / `SetException` / `AwaitOnCompleted` / `Task` 等。  
- 结果是：
  - `UniTask` 既可以被 `await`，又可以作为 `async` 方法的返回类型参与状态机构建，**行为上与 Task 类似，而实现完全可控且高性能**。

---

### 5. 实战应用建议

- 在设计你自己的 UniTask 类库或其他高性能异步库时：
  - 严格对照 UniTask 的 `GetAwaiter` + `Awaiter` + `AsyncMethodBuilder` 三层结构来实现。  
  - 在 `awaiter` 中避免持有可变状态，将状态下沉到可复用的 `Source`（类似 `IUniTaskSource`）。  
  - 明确线程语义（是否回主线程、是否捕获 SynchronizationContext），并在文档中标明。  
  - 对多次 await、取消行为、异常传播策略给出清晰定义和约束（必要时在 Debug 下进行检测）。

如需下一步，我们可以进一步根据你项目中的具体场景（例如 `SceneModule` 加载流程），画出“YooAsset ➜ IUniTaskSource 实现 ➜ UniTask.Awaiter ➜ async 状态机”的完整调用时序图。