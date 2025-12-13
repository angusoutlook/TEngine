# TEngine UniTask 设计原理对话（Raw）

> 日期：2025-01-XX  
> 说明：完整记录本次会话中 **用户与助手** 的可见内容（不包含系统与工具调用元数据）。  
> 主题：UniTask 的 awaiter 接口、Task 桥接、自定义 task-like 实现、IUniTaskSource 与 token 的作用、Source 维持机制、UniTask 与 Task 转换

---

## 1. 为什么要桥接 Task 类？

**User：**  
unitask的awaiter 的接口已经了解，那么为社么要桥接task 类？

**Assistant：**  
基于真实代码，解释 UniTask 桥接 Task 的原因：

### 1. 与 .NET 生态互操作

.NET 标准库和第三方库大量使用 `Task`。如果 UniTask 不能桥接 Task，就无法在 UniTask 代码中使用这些 API。

**真实场景示例：**
```csharp
// .NET 标准库返回 Task
public async UniTask<string> DownloadFileAsync(string url)
{
    using var client = new HttpClient();
    // 必须桥接：HttpClient.GetStringAsync 返回 Task<string>
    var content = await client.GetStringAsync(url).AsUniTask();
    return content;
}
```

**桥接实现（真实代码）：**
```17:42:UnityProject/Packages/UniTask/Runtime/UniTaskExtensions.cs
public static UniTask<T> AsUniTask<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
{
    var promise = new UniTaskCompletionSource<T>();

    task.ContinueWith((x, state) =>
    {
        var p = (UniTaskCompletionSource<T>)state;

        switch (x.Status)
        {
            case TaskStatus.Canceled:
                p.TrySetCanceled();
                break;
            case TaskStatus.Faulted:
                p.TrySetException(x.Exception.InnerException ?? x.Exception);
                break;
            case TaskStatus.RanToCompletion:
                p.TrySetResult(x.Result);
                break;
            default:
                throw new NotSupportedException();
        }
    }, promise, useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);

    return promise.Task;
}
```

**设计意图：**
- 通过 `Task.ContinueWith` 订阅 Task 完成
- 用 `UniTaskCompletionSource<T>` 包装结果，转为 UniTask
- `useCurrentSynchronizationContext` 控制是否切回 Unity 主线程

### 2. 渐进式迁移

项目可能已有大量基于 `Task` 的代码，需要逐步迁移到 UniTask。桥接让两者可以共存。

### 3. 与外部系统集成

需要调用返回 `Task` 的外部服务或库时，桥接是必需的。

### 4. 线程切换与调度控制

`AsUniTask` 的 `useCurrentSynchronizationContext` 参数用于控制回调是否切回 Unity 主线程。

### 5. 反向桥接：UniTask → Task

有时需要将 UniTask 转为 Task，以兼容只接受 Task 的 API。

**总结：**
- UniTask 的 awaiter 接口用于内部 await 机制
- Task 桥接用于与外部 Task 生态互操作
- 两者解决不同问题：awaiter 是内部实现，Task 桥接是外部兼容

---

## 2. 如何实现自己的 Task-like 类型？

**User：**  
刚刚讨论了如何实现自己的awaiter 。awaiter里面有自己实现的Unitask，为什么要这么设计，如何实现自己的task？

**Assistant：**  
基于真实代码，解释 UniTask 的设计原因和实现方式：

### 一、为什么要自己实现 UniTask（而不是直接用 Task）？

#### 1. 性能：值类型 + 零 GC

**真实代码对比：**

```32:45:UnityProject/Packages/UniTask/Runtime/UniTask.cs
[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct UniTask
{
    readonly IUniTaskSource source;
    readonly short token;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTask(IUniTaskSource source, short token)
    {
        this.source = source;
        this.token = token;
    }
```

**设计意图：**
- `UniTask` 是 `readonly struct`（值类型），只包含两个字段：`source` 引用 + `short token`
- 标准 `Task` 是引用类型，每次创建都会堆分配
- 在 Unity 高帧率场景下，`Task` 会带来大量 GC 压力

#### 2. Unity 集成：PlayerLoop 调度

UniTask 可以直接集成到 Unity 的 PlayerLoop，而 `Task` 依赖 `SynchronizationContext`，在 Unity 中不够自然。

#### 3. 完全控制：调度和状态管理

通过自定义 `IUniTaskSource` 和 `AsyncMethodBuilder`，可以完全控制异步的执行方式。

### 二、Awaiter 在 UniTask 设计中的作用

**真实代码：**

```290:345:UnityProject/Packages/UniTask/Runtime/UniTask.cs
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
```

**设计意图：**
- `GetAwaiter()` 返回 Awaiter，让编译器识别为可 await 类型
- `IsCompleted` 决定是否同步执行或挂起
- `OnCompleted` 注册 continuation，由 `IUniTaskSource` 在完成时调用
- `GetResult()` 获取结果或抛出异常

### 三、如何实现自己的 Task-like 类型

基于 UniTask 的实现，实现自定义 Task-like 需要以下部分：

#### 步骤 1：定义 Task-like 结构体

```csharp
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
public readonly struct MyTask
{
    private readonly IMyTaskSource source;
    private readonly short token;

    public MyTask(IMyTaskSource source, short token)
    {
        this.source = source;
        this.token = token;
    }

    public Awaiter GetAwaiter()
    {
        return new Awaiter(this);
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly MyTask task;

        public Awaiter(in MyTask task)
        {
            this.task = task;
        }

        public bool IsCompleted
        {
            get => task.source?.GetStatus(task.token).IsCompleted() ?? true;
        }

        public void GetResult()
        {
            task.source?.GetResult(task.token);
        }

        public void OnCompleted(Action continuation)
        {
            if (task.source == null)
            {
                continuation();
            }
            else
            {
                task.source.OnCompleted(continuation, task.token);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }
}
```

#### 步骤 2：定义 Source 接口

```csharp
public interface IMyTaskSource
{
    MyTaskStatus GetStatus(short token);
    void GetResult(short token);
    void OnCompleted(Action<object> continuation, object state, short token);
}
```

#### 步骤 3：实现 AsyncMethodBuilder

```csharp
public struct MyTaskMethodBuilder
{
    private IMyTaskSource source;
    private Exception exception;

    public static MyTaskMethodBuilder Create()
    {
        return default;
    }

    public MyTask Task
    {
        get
        {
            if (source != null)
                return new MyTask(source, 0);
            if (exception != null)
                return MyTask.FromException(exception);
            return MyTask.CompletedTask;
        }
    }

    public void SetResult()
    {
        if (source != null)
            source.SetResult();
    }

    public void SetException(Exception ex)
    {
        if (source != null)
            source.SetException(ex);
        else
            exception = ex;
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, 
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (source == null)
        {
            source = new MyTaskSource<TStateMachine>();
        }
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, 
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        AwaitOnCompleted(ref awaiter, ref stateMachine);
    }
}
```

---

## 3. IUniTaskSource 和 token 的作用

**User：**  
task like的 为什么要用 ITaskSource和 token 两个字段，有社么作用？

**Assistant：**  
基于真实代码，解释这两个字段的作用：

### 一、IUniTaskSource 的作用：异步操作的"源"

#### 1. 职责：管理异步状态和回调

**真实代码：**

```26:33:UnityProject/Packages/UniTask/Runtime/IUniTaskSource.cs
public interface IUniTaskSource
{
    UniTaskStatus GetStatus(short token);
    void OnCompleted(Action<object> continuation, object state, short token);
    void GetResult(short token);
```

**设计意图：**
- `IUniTaskSource` 是异步操作的"源"，负责：
  1. 状态管理：Pending/Succeeded/Faulted/Canceled
  2. 回调存储：保存 `continuation` 和 `state`
  3. 结果提供：通过 `GetResult` 返回结果或抛异常

#### 2. 为什么需要 Source？

**分离关注点：**
- `UniTask` 是值类型句柄，只保存 `source` 引用和 `token`
- `IUniTaskSource` 是引用类型，保存实际状态和回调
- 这样 `UniTask` 可以零 GC，而状态管理在 Source 中

### 二、Token 的作用：版本号，防止对象池复用误用

#### 1. Token 的本质：Version（版本号）

**真实代码：**

```78:99:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
public struct UniTaskCompletionSourceCore<TResult>
{
    TResult result;
    object error;
    short version;  // 这就是 token 的来源
    bool hasUnhandledError;
    int completedCount;
    Action<object> continuation;
    object continuationState;

    [DebuggerHidden]
    public void Reset()
    {
        ReportUnhandledError();

        unchecked
        {
            version += 1; // 每次 Reset 时递增版本号
        }
        completedCount = 0;
        result = default;
        error = null;
        hasUnhandledError = false;
        continuation = null;
        continuationState = null;
    }
```

**关键点：**
- `version` 在 `Reset()` 时递增
- `token` 就是创建 `UniTask` 时的 `core.Version`

#### 2. Token 的验证：防止误用

**真实代码：**

```298:306:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void ValidateToken(short token)
{
    if (token != version)
    {
        throw new InvalidOperationException("Token version is not matched, can not await twice or get Status after await.");
    }
}
```

**所有操作都会验证 token：**

```202:209:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public UniTaskStatus GetStatus(short token)
{
    ValidateToken(token);  // 验证 token
    return (continuation == null || (completedCount == 0)) ? UniTaskStatus.Pending
         : (error == null) ? UniTaskStatus.Succeeded
         : (error is OperationCanceledException) ? UniTaskStatus.Canceled
         : UniTaskStatus.Faulted;
}
```

### 三、为什么需要两个字段？对象池复用的安全机制

#### 1. 对象池复用场景

**真实代码：**

```338:347:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
public static AutoResetUniTaskCompletionSource Create()
{
    if (!pool.TryPop(out var result))  // 从对象池获取
    {
        result = new AutoResetUniTaskCompletionSource();
    }
    result.version = result.core.Version;  // 保存当前版本号
    TaskTracker.TrackActiveTask(result, 2);
    return result;
}
```

#### 2. 问题场景：如果没有 token 会怎样？

**假设场景：**

```csharp
// 1. 创建第一个 UniTask
var source1 = AutoResetUniTaskCompletionSource.Create();  // version = 1
var task1 = source1.Task;  // token = 1

// 2. task1 完成，source1 被回收到对象池
await task1;  // 完成
// source1.Reset() 被调用，version 变成 2

// 3. 创建第二个 UniTask，复用了同一个 source1
var source2 = AutoResetUniTaskCompletionSource.Create();  // 从池中取出 source1，version = 2
var task2 = source2.Task;  // token = 2

// 4. 如果没有 token 验证：
// 如果 task1 的引用还在，再次 await task1 会错误地使用 source1 的新状态（属于 task2）
await task1;  // ❌ 错误！应该失败，但如果没有 token 验证，可能会成功
```

#### 3. 有 token 的保护机制

**真实场景：**

```csharp
// 1. 创建第一个 UniTask
var source1 = AutoResetUniTaskCompletionSource.Create();  // version = 1
var task1 = source1.Task;  // token = 1

// 2. task1 完成，source1 被回收到对象池
await task1;  // 完成，source1.Reset()，version 变成 2

// 3. 创建第二个 UniTask，复用了同一个 source1
var source2 = AutoResetUniTaskCompletionSource.Create();  // 从池中取出 source1，version = 2
var task2 = source2.Task;  // token = 2

// 4. 有 token 验证：
try
{
    await task1;  // ❌ 抛出异常！
    // ValidateToken(1) 检查：token=1 != version=2
    // 抛出："Token version is not matched, can not await twice or get Status after await."
}
catch (InvalidOperationException)
{
    // 正确捕获到错误
}
```

### 四、总结：两个字段的协同作用

#### 1. IUniTaskSource（引用类型）
- 作用：管理异步状态、存储回调、提供结果
- 特点：可被对象池复用，减少 GC
- 位置：堆上（引用类型）

#### 2. Token（short 值类型）
- 作用：版本号，标识 Source 的当前"生命周期"
- 特点：每次 `Reset()` 递增，用于验证操作合法性
- 位置：栈上（值类型，作为 `UniTask` 的字段）

#### 3. 协同工作

```
UniTask (struct, 栈上)
├── source: IUniTaskSource (引用, 堆上)
│   ├── version: short (当前版本号)
│   ├── result: TResult (结果)
│   ├── continuation: Action<object> (回调)
│   └── ... (其他状态)
└── token: short (创建时的版本号快照)
```

**工作流程：**
1. 创建 `UniTask` 时，保存 `source` 和创建时的 `core.Version` 作为 `token`
2. 每次调用 `GetStatus/GetResult/OnCompleted` 时，验证 `token == source.Version`
3. 如果 `token` 不匹配，说明 Source 已被复用，抛出异常
4. 这样确保每个 `UniTask` 只能使用对应生命周期的 Source 状态

**设计优势：**
- 性能：对象池复用 Source，减少 GC
- 安全：Token 防止误用已复用的 Source
- 轻量：`UniTask` 本身是值类型，零 GC
- 清晰：分离状态管理（Source）和句柄（UniTask）

---

## 4. Source 通过什么维持状态？

**User：**  
awaiter 内部通过unitask维持， unitask通过source，token ，source通过什么来维持？

**Assistant：**  
基于真实代码，解释 Source 的维持机制：

### 一、完整的维持链条

#### 1. 四层结构

```
Awaiter (struct)
  └─> UniTask (struct)
        ├─> IUniTaskSource source (引用类型，堆上)
        └─> short token (值类型，栈上)
              │
              └─> Source 内部持有：
                    └─> UniTaskCompletionSourceCore<T> core (struct，嵌入在 Source 中)
```

#### 2. Source 通过 `UniTaskCompletionSourceCore` 维持状态

**真实代码：**

```319:331:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
public class AutoResetUniTaskCompletionSource : IUniTaskSource, ITaskPoolNode<AutoResetUniTaskCompletionSource>, IPromise
{
    static TaskPool<AutoResetUniTaskCompletionSource> pool;
    AutoResetUniTaskCompletionSource nextNode;
    public ref AutoResetUniTaskCompletionSource NextNode => ref nextNode;

    static AutoResetUniTaskCompletionSource()
    {
        TaskPool.RegisterSizeGetter(typeof(AutoResetUniTaskCompletionSource), () => pool.Size);
    }

    UniTaskCompletionSourceCore<AsyncUnit> core;  // ← 这就是 Source 维持状态的核心
    short version;
```

**`UniTaskCompletionSourceCore` 包含的状态：**

```71:82:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[StructLayout(LayoutKind.Auto)]
public struct UniTaskCompletionSourceCore<TResult>
{
    // Struct Size: TResult + (8 + 2 + 1 + 1 + 8 + 8)

    TResult result;                    // 结果值
    object error;                      // 异常或取消异常
    short version;                     // 版本号（用于 token 验证）
    bool hasUnhandledError;           // 是否有未处理的错误
    int completedCount;                // 完成计数（0 表示未完成）
    Action<object> continuation;       // 回调委托
    object continuationState;          // 回调状态
```

### 二、Source 的生命周期管理

#### 1. 创建：从对象池获取或新建

**真实代码：**

```337:347:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
public static AutoResetUniTaskCompletionSource Create()
{
    if (!pool.TryPop(out var result))  // 尝试从对象池获取
    {
        result = new AutoResetUniTaskCompletionSource();  // 池中没有，新建
    }
    result.version = result.core.Version;  // 保存当前 core 的版本号
    TaskTracker.TrackActiveTask(result, 2);
    return result;
}
```

#### 2. 使用：所有操作都委托给 core

**真实代码：**

```403:432:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
public void GetResult(short token)
{
    try
    {
        core.GetResult(token);  // 委托给 core
    }
    finally
    {
        TryReturn();  // 完成后尝试回收到对象池
    }
}

[DebuggerHidden]
public UniTaskStatus GetStatus(short token)
{
    return core.GetStatus(token);  // 委托给 core
}

[DebuggerHidden]
public void OnCompleted(Action<object> continuation, object state, short token)
{
    core.OnCompleted(continuation, state, token);  // 委托给 core
}
```

#### 3. 回收：Reset core 并放回对象池

**真实代码：**

```434:440:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
bool TryReturn()
{
    TaskTracker.RemoveTracking(this);
    core.Reset();  // 重置 core 的所有状态，version 递增
    return pool.TryPush(this);  // 放回对象池
}
```

**`core.Reset()` 的实现：**

```84:99:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
public void Reset()
{
    ReportUnhandledError();

    unchecked
    {
        version += 1; // 递增版本号
    }
    completedCount = 0;
    result = default;
    error = null;
    hasUnhandledError = false;
    continuation = null;
    continuationState = null;
}
```

### 三、为什么这样设计？

#### 1. 分离关注点

- `IUniTaskSource`（类）：对象池管理、生命周期追踪
- `UniTaskCompletionSourceCore`（结构体）：实际状态存储、线程安全操作

#### 2. 性能优化

- `core` 是值类型，嵌入在 Source 类中，减少间接访问
- Source 可被对象池复用，减少 GC
- `core` 的字段直接存储，避免额外分配

#### 3. 线程安全

`core` 内部使用 `Interlocked` 操作保证线程安全：

```130:145:UnityProject/Packages/UniTask/Runtime/UniTaskCompletionSource.cs
[DebuggerHidden]
public bool TrySetResult(TResult result)
{
    if (Interlocked.Increment(ref completedCount) == 1)  // 原子操作
    {
        // setup result
        this.result = result;

        if (continuation != null || Interlocked.CompareExchange(ref this.continuation, UniTaskCompletionSourceCoreShared.s_sentinel, null) != null)
        {
            continuation(continuationState);
        }
        return true;
    }

    return false;
}
```

### 四、完整的数据流

**示例：创建一个 UniTask 并 await**

```csharp
// 1. 创建 Source
var source = AutoResetUniTaskCompletionSource.Create();
//    - 从对象池获取或新建
//    - source.core 包含所有状态字段（初始值）
//    - source.version = source.core.Version (例如 version = 1)

// 2. 创建 UniTask
var task = source.Task;
//    - new UniTask(source, core.Version)
//    - task.source = source (引用)
//    - task.token = 1 (版本号快照)

// 3. 创建 Awaiter
var awaiter = task.GetAwaiter();
//    - new Awaiter(task)
//    - awaiter.task = task

// 4. 设置结果
source.TrySetResult();
//    - source.core.TrySetResult()
//    - core.result = AsyncUnit.Default
//    - core.continuation?.Invoke(core.continuationState)

// 5. await 完成，调用 GetResult
awaiter.GetResult();
//    - task.source.GetResult(task.token)
//    - source.GetResult(1)
//    - source.core.GetResult(1) 验证 token == version
//    - 返回 core.result
//    - finally: source.TryReturn() → core.Reset() → 放回对象池
```

### 五、总结

Source 通过内部的 `UniTaskCompletionSourceCore`（值类型结构体）维持状态：

1. **`UniTaskCompletionSourceCore` 存储：**
   - `result`：结果值
   - `error`：异常信息
   - `version`：版本号（用于 token 验证）
   - `continuation`：回调委托
   - `continuationState`：回调状态
   - 其他状态字段

2. **Source（类）负责：**
   - 对象池管理（创建/回收）
   - 生命周期追踪（TaskTracker）
   - 将操作委托给 `core`

3. **设计优势：**
   - 性能：`core` 是值类型，嵌入在 Source 中
   - 复用：Source 可被对象池复用
   - 安全：token 验证防止误用已复用的 Source
   - 清晰：状态管理集中在 `core` 中

因此，Source 通过内部的 `UniTaskCompletionSourceCore` 结构体来维持所有异步状态。

---

## 5. 简化理解

**User：**  
感觉有点难啊！

**Assistant：**  
用更简单的方式重新梳理 UniTask 的核心设计：

### 用生活化的类比理解 UniTask

#### 简化版理解：UniTask 就像一张"取餐号"

想象你去餐厅点餐：

```
UniTask = 取餐号（小票）
├─ source = 厨房（真正做菜的地方）
└─ token = 号码（比如 123 号）
```

**工作流程：**
1. 你拿到取餐号（UniTask）
2. 厨房（source）在做菜
3. 菜做好了，叫你的号码（token）
4. 你凭号码取餐

**为什么需要 token？**
- 防止拿错：如果 123 号已经取过，厨房又做了新菜，新号码可能是 124
- 如果还用旧的 123 号去取，会被拒绝（token 不匹配）

### 三层结构（简化版）

#### 第一层：Awaiter（服务员）
- 作用：帮你检查菜是否做好
- 简单理解：`await` 时，编译器会问服务员"菜好了吗？"

#### 第二层：UniTask（取餐号）
- 作用：一个轻量的小票
- 包含：
  - `source`：指向厨房（引用）
  - `token`：你的号码（值）

#### 第三层：Source（厨房）
- 作用：真正做菜的地方
- 包含：
  - 菜的状态（做好了没）
  - 你的号码（version）
  - 做好的回调（通知你）

### 为什么要这样设计？

#### 1. 性能：UniTask 是"小票"，不是"厨房"
```csharp
// ❌ 如果 UniTask 是引用类型（像 Task）
UniTask task1 = SomeMethod();  // 分配内存
UniTask task2 = SomeMethod();  // 又分配内存
// 频繁创建 → GC 压力大

// ✅ UniTask 是值类型（struct）
UniTask task1 = SomeMethod();  // 栈上，零分配
UniTask task2 = SomeMethod();  // 栈上，零分配
// 频繁创建 → 无 GC 压力
```

#### 2. 复用：厨房可以重复使用
```csharp
// 厨房（Source）用完后可以回收，下次再用
// 但每次用的时候，号码（version）会变
// 这样旧的取餐号就不能再用了（安全）
```

### 实际使用（你只需要知道这些）

#### 作为使用者，你只需要：

```csharp
// 1. 创建异步方法
public async UniTask<string> LoadDataAsync()
{
    await UniTask.Delay(1000);
    return "数据";
}

// 2. 调用
var result = await LoadDataAsync();
```

**内部发生了什么（简化版）：**
1. `LoadDataAsync()` 返回一个 `UniTask<string>`
2. `await` 时，编译器生成代码检查是否完成
3. 如果未完成，挂起并等待
4. 完成后继续执行

**你不需要关心：**
- Awaiter 如何工作
- Source 如何管理状态
- Token 如何验证
- 对象池如何复用

### 核心概念（一句话总结）

1. **UniTask** = 轻量的小票（值类型，零 GC）
2. **Source** = 真正的厨房（管理状态，可复用）
3. **Token** = 号码（防止拿错）
4. **Awaiter** = 服务员（检查是否完成）

---

## 6. UniTask 和 Task 如何转换？

**User：**  
unitask和 Task如何转换的？

**Assistant：**  
基于真实代码，说明 UniTask 与 Task 的转换：

### 一、Task → UniTask（AsUniTask）

#### 1. 转换方式：用 UniTaskCompletionSource 包装

**真实代码：**

```17:42:UnityProject/Packages/UniTask/Runtime/UniTaskExtensions.cs
public static UniTask<T> AsUniTask<T>(this Task<T> task, bool useCurrentSynchronizationContext = true)
{
    var promise = new UniTaskCompletionSource<T>();

    task.ContinueWith((x, state) =>
    {
        var p = (UniTaskCompletionSource<T>)state;

        switch (x.Status)
        {
            case TaskStatus.Canceled:
                p.TrySetCanceled();
                break;
            case TaskStatus.Faulted:
                p.TrySetException(x.Exception.InnerException ?? x.Exception);
                break;
            case TaskStatus.RanToCompletion:
                p.TrySetResult(x.Result);
                break;
            default:
                throw new NotSupportedException();
        }
    }, promise, useCurrentSynchronizationContext ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current);

    return promise.Task;
}
```

#### 2. 工作原理（简化）

```
Task<T> 完成
    ↓
ContinueWith 回调
    ↓
根据 Task 的状态：
  - Canceled → promise.TrySetCanceled()
  - Faulted → promise.TrySetException()
  - RanToCompletion → promise.TrySetResult(Result)
    ↓
返回 promise.Task (UniTask<T>)
```

#### 3. 关键点

- 使用 `Task.ContinueWith` 订阅 Task 完成
- 用 `UniTaskCompletionSource<T>` 作为中间桥梁
- `useCurrentSynchronizationContext` 控制是否切回 Unity 主线程

**使用示例：**
```csharp
// .NET 标准库返回 Task
Task<string> task = HttpClient.GetStringAsync("http://example.com");

// 转换为 UniTask
UniTask<string> uniTask = task.AsUniTask();

// 在 Unity 中使用
string result = await uniTask;
```

### 二、UniTask → Task（AsTask）

#### 1. 转换方式：用 TaskCompletionSource 包装

**真实代码：**

```74:126:UnityProject/Packages/UniTask/Runtime/UniTaskExtensions.cs
public static Task<T> AsTask<T>(this UniTask<T> task)
{
    try
    {
        UniTask<T>.Awaiter awaiter;
        try
        {
            awaiter = task.GetAwaiter();
        }
        catch (Exception ex)
        {
            return Task.FromException<T>(ex);
        }

        if (awaiter.IsCompleted)
        {
            try
            {
                var result = awaiter.GetResult();
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        var tcs = new TaskCompletionSource<T>();

        awaiter.SourceOnCompleted(state =>
        {
            using (var tuple = (StateTuple<TaskCompletionSource<T>, UniTask<T>.Awaiter>)state)
            {
                var (inTcs, inAwaiter) = tuple;
                try
                {
                    var result = inAwaiter.GetResult();
                    inTcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    inTcs.SetException(ex);
                }
            }
        }, StateTuple.Create(tcs, awaiter));

        return tcs.Task;
    }
    catch (Exception ex)
    {
        return Task.FromException<T>(ex);
    }
}
```

#### 2. 工作原理（简化）

```
检查 UniTask 是否已完成
    ↓
如果已完成：
  - 成功 → Task.FromResult(result)
  - 异常 → Task.FromException(ex)
    ↓
如果未完成：
  - 创建 TaskCompletionSource<T>
  - 订阅 UniTask 的 awaiter.SourceOnCompleted
  - UniTask 完成时：
    - 成功 → tcs.SetResult(result)
    - 异常 → tcs.SetException(ex)
    ↓
返回 tcs.Task
```

#### 3. 关键点

- 已完成：直接返回 `Task.FromResult` 或 `Task.FromException`
- 未完成：用 `TaskCompletionSource<T>` 包装，通过 `awaiter.SourceOnCompleted` 订阅完成

**使用示例：**
```csharp
// UniTask 方法
UniTask<string> uniTask = LoadDataAsync();

// 转换为 Task（给需要 Task 的 API 使用）
Task<string> task = uniTask.AsTask();

// 可以传给需要 Task 的方法
await SomeMethodThatNeedsTask(task);
```

### 三、完整转换示例

#### 场景 1：在 UniTask 代码中使用 .NET Task API

```csharp
public async UniTask<string> DownloadDataAsync()
{
    using var client = new HttpClient();
    
    // .NET API 返回 Task，转换为 UniTask
    var content = await client.GetStringAsync("http://example.com").AsUniTask();
    
    return content;
}
```

#### 场景 2：在 Task 代码中使用 UniTask

```csharp
public async Task ProcessDataAsync()
{
    // UniTask 方法
    UniTask<string> uniTask = LoadDataAsync();
    
    // 转换为 Task
    string data = await uniTask.AsTask();
    
    // 继续使用 Task API
    await ProcessWithTaskAPI(data);
}
```

#### 场景 3：混合使用

```csharp
public async UniTask<string> MixedAsync()
{
    // 1. 使用 .NET Task API
    var task1 = HttpClient.GetStringAsync("url1").AsUniTask();
    
    // 2. 使用 UniTask API
    var task2 = LoadDataAsync();
    
    // 3. 等待所有完成
    var results = await UniTask.WhenAll(task1, task2);
    
    return results[0] + results[1];
}
```

### 四、转换的关键参数

#### `useCurrentSynchronizationContext` 参数

**Task → UniTask 时：**

```csharp
// 默认 true：切回 Unity 主线程
task.AsUniTask();  // 等同于 AsUniTask(true)

// false：在线程池继续执行
task.AsUniTask(false);
```

**作用：**
- `true`：回调在 Unity 主线程执行（适合 Unity API）
- `false`：回调在线程池执行（适合后台计算）

### 五、总结

#### 转换方向

| 方向 | 方法 | 核心机制 |
|------|------|----------|
| **Task → UniTask** | `AsUniTask()` | `Task.ContinueWith` + `UniTaskCompletionSource` |
| **UniTask → Task** | `AsTask()` | `TaskCompletionSource` + `awaiter.SourceOnCompleted` |

#### 使用建议

1. 优先使用 UniTask：在 Unity 项目中优先用 UniTask
2. 需要时再转换：只在需要与 Task API 互操作时转换
3. 注意线程上下文：`AsUniTask(true)` 确保回到主线程

#### 一句话记忆

- Task → UniTask：用 `ContinueWith` 订阅 Task，用 `UniTaskCompletionSource` 包装
- UniTask → Task：用 `TaskCompletionSource` 包装，用 `awaiter.SourceOnCompleted` 订阅

这样就能在 UniTask 和 Task 之间自由转换。

---

## 7. 保存会话

**User：**  
把这个会话的聊天，一字不差的取个合适的名字 保存到合适的目录

**Assistant：**  
已保存本次会话内容到：`UnityProject/Docs/TEngine_UniTask_Design_Conversation_Raw.md`

---
