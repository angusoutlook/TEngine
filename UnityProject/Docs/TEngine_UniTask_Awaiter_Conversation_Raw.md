## TEngine UniTask Awaiter & C# Async Conversation Raw Log

> 说明：本文件尽可能按时间顺序、逐轮记录“用户 ↔ 助手”的原始对话内容（问与答），仅做极少量排版（如空行、Markdown 标题），不对措辞和技术细节做二次改写。  
> 会话时间：2025-12-12

---

### Round 1

**User：**

我们继续了unitask的设计意图和架构
unitask是如何实现架构的？

**Assistant：**

我是使用 GPT-5.1 模型的助手。  

### 1. UniTask 的设计目标（回顾一下“意图”）

- **零 GC / 高性能**：避免 `Task`/`async void` 在 Unity 中频繁分配和装箱导致的 GC。
- **完全兼容 C# async/await 语法**：写法尽量接近原生 `Task`，但运行时负担更小。
- **针对 Unity 场景优化**：支持主线程（PlayerLoop）调度、帧同步、`AsyncOperation`、`Coroutine` 等。
- **可组合的异步原语**：像 LINQ 一样组合各种等待、超时、取消、并行、序列化执行。

---

### 2. 核心抽象：UniTask / IUniTaskSource / Awaiter 三层

从底向上看，UniTask 的架构大致分三层：

1. **底层：`IUniTaskSource` + 各种 `*Source` 实现**
   - **接口 `IUniTaskSource`**：统一抽象“一个可被 await 的异步操作”。
   - 具体实现如 `UniTaskCompletionSource`, `DelayPromise`, `TimeoutPromise`, 各种 `AsyncOperation` 源等。
   - 这些类内部负责：
     - 记录状态（未完成 / 已完成 / 失败 / 取消）
     - 存放回调（awaiter 的 continuation）
     - 在操作完成时调用 continuation，并把结果传递出去。
   - **通常配合对象池使用**，完成后归还，减少 GC。

2. **中层：`UniTask` / `UniTask<T>` 结构体**
   - `UniTask` 本身是一个 **struct**，几乎只保存：
     - 一个 `IUniTaskSource` 引用（或轻量状态）
     - 一个短整型 Token（区分池中复用的 source 实例）
   - 它 **不直接保存回调链**，而是把等待和完成逻辑委托给 `IUniTaskSource`。
   - 关键点：`UniTask` 是值类型 + 内部引用/状态，非常轻量，避免了 `Task` 那种堆对象分配。

3. **上层：Awaiter（`GetAwaiter()`）**
   - `UniTask.GetAwaiter()` 返回一个 **轻量 Awaiter 结构体**，实现 `INotifyCompletion` / `ICriticalNotifyCompletion`。
   - `await` 关键字只和 Awaiter 协作：
     - `IsCompleted`：是否已完成
     - `OnCompleted(Action)`：注册 continuation
     - `GetResult()`：获取结果 / 抛异常
   - Awaiter 内部再回到 `IUniTaskSource` 去登记或获取结果。
   - 这一层把 C# 编译器生成的状态机，优雅地接入到 UniTask 体系。

---

### 3. 调度与执行：PlayerLoop / Scheduler / SynchronizationContext

为了适配 Unity，UniTask 在“调度模型”上做了专门设计：

- **PlayerLoop 集成**
  - UniTask 在启动时会把自己的 Runner 挂到 Unity 各种 `PlayerLoopSystem` 节点里，如 `Update`, `LateUpdate`, `FixedUpdate` 等。
  - 任何“需要在主线程 / 某个阶段回调”的 continuation，都会被放入对应 Runner 的队列，然后在该帧该阶段执行。
  - 这就是为什么有 `UniTask.Yield(PlayerLoopTiming.Update)`、`UniTask.NextFrame()` 等 API。

- **自定义 Scheduler / TaskPool**
  - 为避免频繁创建 `Task`，UniTask 自己维护一套：
    - **工作队列**：记录待执行 continuation
    - **对象池**：复用各种 `*Source`/`Promise` 实例
  - 某些操作（如 `Delay`）会挂到定时/计数结构上，每帧检查是否到期，到期后把 continuation 压入执行队列。

- **与 SynchronizationContext / ThreadPool 的桥接**
  - 对需要在后台线程做 I/O 或 CPU 工作的情况，UniTask 通过：
    - 调用标准 `Task`/`ThreadPool` 承载真实工作
    - 然后再把完成回调切回 PlayerLoop（主线程）  
  - 提供 `SwitchToMainThread()`、`SwitchToThreadPool()` 这类 API，让你显式控制线程切换。

---

### 4. Unity 适配层：从 Unity 对象到 UniTask

在架构上，还有一个非常关键的“适配层”，负责把 Unity 世界的各种异步对象转成 `UniTask`：

- **`AsyncOperation` / `ResourceRequest` / `AssetBundleRequest` 等适配**
  - 通过扩展方法如 `AsyncOperation.ToUniTask()`，内部使用一个具体的 `IUniTaskSource` 实现：
    - 订阅 `completed` 事件或在 PlayerLoop 中轮询进度
    - 完成时通知 Awaiter。
- **与 Coroutine 的互操作**
  - `UniTask` 可以把协程封装成 `UniTask`，或把 `UniTask` 封装成协程。
- **各种辅助 Awaiter**
  - 如等待 GameObject 激活/销毁、等待 UI 事件、等待按键等，都是在这个适配层中实现：
    - 用一个 `IUniTaskSource` 监听事件
    - 事件触发时完成 `UniTask`。

---

### 5. 组合与扩展：UniTask 的“运算符式” API

在最外层是你经常用到的 API，它们本质上都是基于前面那套抽象组合出来的：

- **时间相关**：`Delay` / `DelayFrame` / `Timeout`
- **控制流**：`WhenAll` / `WhenAny` / `Race` / `AttachExternalCancellation`
- **LINQ 式操作**：部分扩展包中提供针对异步流/通道的操作。
- 这些函数内部要么：
  - 创建一个新的 `*Source`（如 TimeoutPromise）
  - 要么组合多个 `IUniTaskSource`，注册多个回调来实现并行/竞争等逻辑。

---

### 6. 总结：UniTask 的“架构图”用文字概括

可以把 UniTask 的整体架构理解为：

- **最底层**：`IUniTaskSource` + 各种 Promise/Source 实现 + 对象池（负责状态&回调&资源复用）
- **中间层**：`UniTask`/`UniTask<T>` struct + Awaiter（把底层抽象包装成 `await` 友好的类型）
- **调度层**：PlayerLoop Runner + 自定义 Scheduler（决定在哪一帧/哪个线程执行 continuation）
- **Unity 适配层**：各种 Unity 异步对象 → `UniTask` 的桥接
- **API 组合层**：`Delay/Timeout/WhenAll/WhenAny/…` 等高层工具方法

如果你愿意，我们下一步可以结合你项目里 `SceneModule` / `YooAsset` 的实际代码，分别指出：  
- 具体哪里在返回 `UniTask`  
- 等待的到底是哪种 `IUniTaskSource`  
- 以及在 PlayerLoop 里是怎么被调度回主线程的。

---

### Round 2

**User：**

c#  本来有自己的一套 默认的awaiter 和 task 是不是？
如果自己要开发一套符合c#的标准需要遵循什么规范和注意什么样子的坑

**Assistant：**

我是使用 GPT-5.1 模型的助手。  

### 1. 先回答你的直觉问题

- **是的**：C# 自带了一套“默认”的异步实现：
  - **任务类型**：`Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`
  - **awaiter 类型**：`TaskAwaiter` / `TaskAwaiter<T>` / `ValueTaskAwaiter` 等
- 但 **编译器并不是只认 `Task` 这一家**，而是认一个叫“**awaiter / task-like 模式**”的规范。  
  只要你写的类型满足这个模式，`await` 和 `async` 就能认你。

---

### 2. 两个层级：只想被 `await` VS 想当“新 Task”

**要搞清楚先问自己：**

1. **只想让某个类型能被 `await`**  
   比如：`await myCustomDelay;`  
   👉 只需要遵守 **“awaiter 模式 (awaiter pattern)”**。
2. **想让自己的类型可以作为 `async` 方法的返回类型**  
   比如：`async UniTask Foo()` 这种  
   👉 需要支持 **“task-like 类型规范 + AsyncMethodBuilder”**。  
   UniTask 就是走的这一套。

---

### 3. awaiter 模式：让“某个对象可以被 await”

任何类型 `T`，如果你希望写 `await someT`，需要满足 **编译器约定的 awaiter 模式**：

- **在 `T` 上：必须有 `GetAwaiter()` 方法**
  - 形式大致为：
    - `public Awaiter GetAwaiter();`
    - 或 `public SomeStructAwaiter GetAwaiter();`
- **`GetAwaiter()` 返回的 awaiter 类型，必须满足：**
  1. **属性 `bool IsCompleted { get; }`**
     - `true` 表示已完成，可以同步执行 `GetResult`
     - `false` 表示还没完成，编译器会挂起状态机并调用 `OnCompleted`
  2. **方法 `void OnCompleted(Action continuation)`**  
     - 编译器会把“后续要跑的逻辑”丢进这个 `continuation`
     - 你需要在“异步操作完成时”调用这个 `continuation`
     - 为了更底层，还可以实现 `ICriticalNotifyCompletion` 的 `UnsafeOnCompleted`
  3. **`GetResult()`**
     - 无返回值任务：`void GetResult()`
     - 有返回值任务：`TResult GetResult()`
     - 如果异步过程中发生异常，这里要抛出，编译器会帮你包成异常任务 / 让 `async` 方法抛出

**注意：**  
这一整套是“约定优于配置”的 **pattern**，不要求显式实现某个接口，但真实项目里通常会实现 `INotifyCompletion` / `ICriticalNotifyCompletion`。

---

### 4. task-like 类型：想让 `async MyTask Foo()` 生效需要什么

如果你不只想被 `await`，而是想写：

```csharp
public async UniTask<int> MyMethod() { ... }
```

让编译器把 `async` 状态机构建在你自己的类型上，就要满足 **task-like 类型规范**：

- **① 类型本身（比如 `UniTask<T>`）需要：**
  - 有 `GetAwaiter()`（同上，供外部 `await` 使用）
  - 通常是 `struct`，减 GC，但这不是强制要求
- **② 指定 AsyncMethodBuilder（关键）**
  - 通过特性：
    ```csharp
    [AsyncMethodBuilder(typeof(UniTaskAsyncMethodBuilder<>))]
    public struct UniTask<T> { ... }
    ```
  - 编译器在看到 `async UniTask<T>` 时，会去找这个 **builder 类型**。

- **③ builder 类型必须实现一整套固定 API（简化版）：**
  - `static TBuilder Create()`
  - `void Start(ref TStateMachine stateMachine)`
  - `void SetResult(TResult result)`
  - `void SetException(Exception exception)`
  - `void AwaitOnCompleted<TAwaiter, TStateMachine>(...)`
  - `TTask Task { get; }`（返回最终的 `UniTask<T>` 等）

如果这些都实现正确，编译器就会把状态机和 awaiter 调度逻辑，搭在你的 **builder + 自定义 Task 类型** 上。  
这就是 UniTask、ValueTask 这类“task-like 类型”的基础。

---

### 5. 规范上关键要点（写自己的一套要遵守什么）

- **语义要和 C# 原生 Task 一致**：
  - 成功 / 失败 / 取消 的行为要清晰：
    - 失败 → `GetResult` 抛异常
    - 取消 → 通常抛 `OperationCanceledException`，或者自定义，但一定要统一
  - 同一个实例 **是否允许多次 await** 要事先设计好：
    - `Task` 是可以多 await、结果共享
    - 很多高性能 struct 型 task（含 UniTask 某些用法）**禁止多 await**，要在文档和 Debug 检查中体现

- **线程/上下文语义要清楚：**
  - `OnCompleted` 调用 continuation 时：
    - 是在主线程？线程池？原始 SynchronizationContext？  
    - 建议和 `Task` 一样，可提供：
      - 默认捕获上下文
      - 配合 `.ConfigureAwait(false)` 或类似 API 禁止捕获
  - 尤其在 Unity，要明确是否回到 Main Thread（PlayerLoop）。

- **异常传播与观测：**
  - 异步过程中出的异常要统一通过 `GetResult` 抛出
  - 对类似“未观察到的异常”（像 Task 的 `UnobservedTaskException`）要有策略：
    - 日志输出
    - Debug 构建下抛错
    - 或者软处理

- **取消模型（通常用 `CancellationToken`）：**
  - 是否支持 `CancellationToken` / 超时？
  - 取消时回调顺序、是否一定触发 continuation、是否释放资源，都要明确。

---

### 6. 常见“坑点”与踩坑建议

- **① awaiter 复用 & 多次 await**
  - 如果你的 awaiter/Source 是有状态的（大部分都是），要么：
    - 明确只允许 await 一次（多次就抛异常/Debug 警告）
    - 要么非常小心设计，支持多 await，保证线程安全且不重复调 continuation
- **② struct awaiter / struct task 的“值拷贝”问题**
  - `awaiter` 是 struct 时，C# 状态机会 **复制** 它：
    - 如果你在 struct 内部保存状态（回调、进度），  
      一旦因为复制而操作的是副本，就可能导致 continuation 丢失、状态错乱
  - 典型做法是：struct awaiter 只是轻量包装，真实可变状态放在堆对象（source/promise）里。

- **③ continuation 被丢失 / 没有（或多次）调用**
  - `OnCompleted` 里要能保证：  
    - 完成时 **一定** 调用一次 continuation
    - 不多次调用
  - 如果操作可能“立即完成”（同步完成），要和 `IsCompleted` 联动：
    - `IsCompleted == true` 时，编译器不会调用 `OnCompleted`，而是直接 `GetResult`
    - 你的实现必须在这个语义下仍然正确。

- **④ 死锁与上下文捕获**
  - 如果你像 `Task` 一样默认捕获 `SynchronizationContext`（比如 Unity 主线程），  
    又有人在主线程上 `.Result` / `.Wait()` 阻塞等待，就会死锁。
  - 解决方案：
    - 建议文档里强烈不推荐同步阻塞
    - 提供类似 `.ConfigureAwait(false)` 或 `.Forget()` 的用法
    - Unity 场景中，尽量走 pure async + PlayerLoop，不要做同步阻塞。

- **⑤ GC / 性能细节**
  - 你如果号称“高性能、零 GC”，要特别注意：
    - awaiter 和 source 尽量用 struct + 对象池
    - 避免闭包捕获（比如 lambda 内引用外部局部变量）
    - 减少装箱（比如 `Action`/`object` 里塞值类型时）

---

### 7. 如果你想“做一个自己的 UniTask”大致 checklist

- **类型层面**
  - 定义一个 `MyTask` / `MyTask<T>`（通常 struct）
  - 提供 `GetAwaiter()`，返回 struct awaiter
  - 为 `MyTask` 标上 `[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]`

- **awaiter 层面**
  - awaiter 有：`IsCompleted` / `OnCompleted` / `GetResult`（最好实现 `ICriticalNotifyCompletion`）
  - awaiter 内部引用一个真实的 `Source` 对象，所有可变状态放在 `Source` 里

- **builder 层面**
  - 实现 `MyTaskMethodBuilder` / `MyTaskMethodBuilder<T>`  
    完整实现 `Create/Start/AwaitOnCompleted/SetResult/SetException/Task` 等规范方法

- **调度层面**
  - 决定 continuation 运行在哪：
    - 线程池？主线程？自定义 Scheduler？
    - Unity 中通常接入 PlayerLoop

- **语义与文档**
  - 明确：
    - 是否多 await
    - 是否默认回到主线程
    - 取消 / 超时如何表达
    - 异常如何传播与日志

---

如果你愿意，下一步我可以结合 UniTask 的实际接口，**一步步对照 C# 的规范**，指出：  
- 它的 `GetAwaiter` / awaiter / builder 各自在哪里  
- 它是在哪些地方刻意规避了上面提到的“坑”（特别是 struct 值拷贝、多 await、GC）。

---

### Round 3

**User：**

为什么iscompleted 为 false 没有完成，要挂起状态机，调用oncompleted？

**Assistant：**

我是使用 GPT-5.1 模型的助手。  

### 一句话直白版

**因为：`IsCompleted == false` 表示“结果现在还拿不到”，  
这时编译器必须先把当前 `async` 方法的“执行现场”保存成一个状态机，然后把“后面要继续执行的那段逻辑”交给 awaiter 的 `OnCompleted` 管理，等真正完成时再回调回来**。  
否则要么就会死等/卡死线程，要么后半段代码根本不会被执行。

---

### 按编译器实际流程拆开讲

当你写：

```csharp
await someAwaitable;
```

编译器真正生成的逻辑可以抽象成三步：

1. **先问：现在就能拿结果吗？**

   ```csharp
   var awaiter = someAwaitable.GetAwaiter();
   if (awaiter.IsCompleted)
   {
       // 已经完成：同步继续往下跑
       awaiter.GetResult();
       goto 下一段状态机代码;
   }
   else
   {
       // 还没完成：需要“挂起”
       // ↓↓↓
   }
   ```

   - `IsCompleted == true`：  
     说明这个操作其实已经结束了（比如缓存命中、立即可用）。  
     👉 直接 `GetResult()`，**同步继续往下执行**，不需要“异步”。

   - `IsCompleted == false`：  
     说明现在就算调用 `GetResult()` 也得不到结果（要等待 IO、网络、下一帧等等）。  
     👉 编译器不能死等，只能 **先停下来**。

2. **“挂起状态机”具体做了什么？**

   - 编译器把你的 `async` 方法变成一个 **状态机结构体/类**，把：
     - 当前执行的位置（一个 `state` 整数）
     - 所有局部变量
     - 捕获的 `this` 等
     全部保存起来。
   - 然后设置当前 `state` 为“等待中”的某个编号，表示**等异步完成之后要从这里继续**。

3. **为什么要调 `OnCompleted`？**

   - 接下来编译器会构造一个 **continuation（继续执行的方法）**，本质上是：

     ```csharp
     void Continuation()
     {
         // 恢复状态机
         // 调用 awaiter.GetResult()
         // 跳到下一段状态
     }
     ```

   - 然后调用 awaiter 的：

     ```csharp
     awaiter.OnCompleted(Continuation);
     ```

   - 这一步的含义是：
     - **把“异步完成之后要做什么”告诉 awaiter**。
     - awaiter（也就是底层的异步实现）会在真正完成的那一刻，调用这个 `Continuation`：
       - 恢复状态机
       - 调 `GetResult()` 取结果或抛异常
       - 继续执行 `await` 后面的代码

---

### 为什么不能不挂起、直接等？

- **不能 while 等 / 阻塞**：
  - 如果在 `IsCompleted == false` 时你自己“傻等”，就等价于：

    ```csharp
    while (!awaiter.IsCompleted) { } // 卡死线程
    ```

  - 这会：
    - 卡住游戏主线程，画面不再刷新、事件不再处理
    - 无法调度别的异步任务，整体死锁

- **必须交还控制权给“调度者”**：
  - Unity 场景中，调度者是 **主循环（PlayerLoop）** 或某个线程池。
  - 通过 `OnCompleted(continuation)`：
    - awaiter 知道：**异步完成时要回调谁**
    - 它可以选择：
      - 在主线程下一帧调用
      - 在线程池排队调用
      - 在某个自定义 Scheduler 中调用

- **状态机 + OnCompleted 是语言级协议**：
  - C# 编译器与你的 awaiter 之间，没有接口文件，靠的就是这个“约定”：
    - `IsCompleted` 决定“同步还是异步路径”
    - “异步路径”就必须走 `OnCompleted(continuation)`，否则状态机永远不会恢复。

---

### 换个更形象的比喻

- `IsCompleted == true`：  
  “外卖已经在门口了，你直接开门拿就行”（同步继续执行）。
- `IsCompleted == false` + `OnCompleted(continuation)`：  
  “外卖还在路上，那你先去做别的事（主线程继续跑别的逻辑），  
  告诉外卖小哥：**到了门口就给我打电话**（`OnCompleted` 注册 continuation）。”

没有这通电话（`OnCompleted`），你既不能一直站在门口等（阻塞），  
外卖到了你也永远不知道（状态机不会恢复），程序就卡在“等结果”的那一半上。

---

如果你愿意，我可以用 UniTask 里的一个真实 awaiter 实现（比如 `UniTask.Awaiter`）对照这三步，把 `IsCompleted` / `OnCompleted` / `GetResult` 是怎么配合 Unity PlayerLoop 调度的，逐行带你看一遍。

---

### Round 4

**User：**

使用UniTask最简洁的一个真实awaiter实现，讲解一下。整个规范的实现

**Assistant：**

我是使用 GPT-5.1 模型的助手。  

下面我**完全基于你工程里的真实 UniTask 源码**来讲解一个最简洁的 awaiter 实现：`UniTask.Awaiter`，并说明它是如何满足 C# await 规范的。

---

### 1. 入口：`UniTask.GetAwaiter()`（让类型可以被 `await`）

先看 `UniTask` 上暴露给编译器的入口：  

```58:63:UnityProject/Packages/UniTask/Runtime/UniTask.cs
[DebuggerHidden]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public Awaiter GetAwaiter()
{
    return new Awaiter(this);
}
```

- **规范要求**：  
  - 任何能被 `await` 的类型，必须有一个 `public X GetAwaiter()` 方法。
- **这里的实现**：  
  - `UniTask` 返回一个内部的 `Awaiter` 结构体实例，把当前 `UniTask` 自己传进去。
  - 编译器在看到 `await someUniTask` 时，实际会生成 `var awaiter = someUniTask.GetAwaiter();`。

---

### 2. 真正的 awaiter：`UniTask.Awaiter` 全代码

这是你工程里 `UniTask` 对应的 awaiter 源码（无返回值版，已经是最简洁的一个）：  

```290:363:UnityProject/Packages/UniTask/Runtime/UniTask.cs
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

下面严格按 **C# awaiter 规范的三个核心点** 来拆解这段实现。

---

### 3. 规范点一：`IsCompleted` —— 告诉编译器“要不要挂起”

```305:308:UnityProject/Packages/UniTask/Runtime/UniTask.cs
get
{
    return task.Status.IsCompleted();
}
```

- **C# 要求**：  
  awaiter 类型必须有 `bool IsCompleted { get; }`。
- **含义**：
  - `true`：异步操作“已经完成”，编译器可以**同步**取结果，不用挂起状态机。
  - `false`：还没完成，编译器需要挂起当前 `async` 方法，并注册回调。
- **这里的设计细节**：
  - `task.Status` 又是基于 `IUniTaskSource.GetStatus(token)` 实现的：
    - `source == null` → 视为 `Succeeded`，说明这是一个“立即完成”的 UniTask（例如 `UniTask.CompletedTask`）。
    - 否则交给真正的 `IUniTaskSource` 去判断当前状态。
  - `IsCompleted()` 是对 `UniTaskStatus` 的扩展方法（在别处定义），本质就是检查是否是完成态（成功/失败/取消）。

**设计意图**：  
`Awaiter` 本身几乎不做逻辑，只是把“是否完成”的判断转发给内部的 `IUniTaskSource`，自己保持极简、零额外状态。

---

### 4. 规范点二：`GetResult()` —— 在恢复时拿结果 / 抛异常

```313:317:UnityProject/Packages/UniTask/Runtime/UniTask.cs
public void GetResult()
{
    if (task.source == null) return;
    task.source.GetResult(task.token);
}
```

- **C# 要求**：
  - 无返回值任务：`void GetResult()`
  - 有返回值任务：`T GetResult()`
- **语义**：
  - 同步路径（`IsCompleted == true`）下，编译器会**立即调用** `GetResult()`，然后继续执行后面的代码。
  - 异步路径下，当 continuation 被调用时，也会先 `GetResult()` 再执行后半段。
- **这里的关键点**：
  - `task.source == null`：
    - 说明 UniTask 是一个“已完成、无实际源”的任务（例如：默认构造或 `CompletedTask`）。
    - 这时 `GetResult` 直接返回，无需任何操作/异常。
  - `task.source != null`：
    - 真实的状态、异常、取消都存放在 `IUniTaskSource` 里。
    - 调用 `source.GetResult(token)`：
      - 如果成功：什么都不抛，正常返回。
      - 如果失败：抛出异常（包括 `OperationCanceledException` 或其他异常），由编译器把异常传播出 `async` 方法。

**设计意图**：  
- `UniTask.Awaiter` 不负责持有结果，不负责记错误，只负责**把这个调用转发**到统一接口 `IUniTaskSource`。  
- 所以无论底层是 `Delay`、`AsyncOperation` 还是 `Scene` 加载，`await` 的语义都是统一的。

---

### 5. 规范点三：`OnCompleted` / `UnsafeOnCompleted` —— 如何挂起与恢复状态机

```321:331:UnityProject/Packages/UniTask/Runtime/UniTask.cs
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

335:344:UnityProject/Packages/UniTask/Runtime/UniTask.cs
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

- **C# 要求**：
  - awaiter 应该实现 `INotifyCompletion` 或 `ICriticalNotifyCompletion`，提供：
    - `void OnCompleted(Action continuation)`
    - `void UnsafeOnCompleted(Action continuation)`
- **编译器怎么用**（简化说明，非源码）：
  - 当 `IsCompleted == false`，编译器会：
    - 保存 async 方法的状态机
    - 构造一个 `Action` 类型的 continuation（其实是 `MoveNext` 的封装）
    - 调用 `awaiter.OnCompleted(continuation)` 或 `UnsafeOnCompleted` 把“后续要做的事”注册进去。
- **这里的分支逻辑**：
  - `task.source == null`：
    - 实际上任务已经是**立即完成态**了（和 `IsCompleted` 的语义一致）。
    - 这时没必要真正挂起，直接 `continuation()` 立刻执行后半段代码。
  - `task.source != null`：
    - 真实的异步逻辑由 `IUniTaskSource` 实现。
    - 调用：
      ```csharp
      task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, task.token);
      ```
      这里传入：
      - 一个统一的 `Action<object>` 委托：`AwaiterActions.InvokeContinuationDelegate`
      - 真正的 continuation（`Action` 对象）当作 `state`
      - `token` 用来保证 Source 的实例复用安全（对象池时区分不同逻辑）。

- **`AwaiterActions` 是什么？**

```17:27:UnityProject/Packages/UniTask/Runtime/UniTask.cs
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

- 设计目的：
  - `IUniTaskSource.OnCompleted` 的签名是 `(Action<object> continuation, object state, short token)`。
  - 但 C# 编译器给我们的 continuation 是 `Action`（不带参数）。
  - 这里用一个**静态共享的** `Action<object>`：
    - 把真实的 `Action` 塞进 `state`
    - 在回调时 `((Action)state).Invoke()`  
  - 这样做的好处：
    - **避免每次注册 continuation 都 new 一个委托**，减少 GC。
    - 统一所有 continuation 的调用方式，简化 `IUniTaskSource` 的实现。

---

### 6. 这个 Awaiter 如何完整满足 C# await 规范

结合上面三点，你可以看到 `UniTask.Awaiter` 完整实现了 C# 的 awaiter 模式：

1. **类型可被 await**  
   - `UniTask` 暴露 `public Awaiter GetAwaiter()`。

2. **awaiter 满足编译器要求的成员**  
   - `bool IsCompleted { get; }`
   - `void GetResult()`（无返回值版本）
   - `void OnCompleted(Action continuation)`
   - `void UnsafeOnCompleted(Action continuation)`  
   - 并显式实现 `ICriticalNotifyCompletion` 接口（对 `UnsafeOnCompleted` 的支持）。

3. **同步/异步路径语义一致**  
   - `IsCompleted == true`：
     - 编译器直接调用 `GetResult()` 然后继续执行后续代码。
   - `IsCompleted == false`：
     - 编译器挂起状态机
     - 把后续逻辑包装成 `Action continuation`
     - 调用 `OnCompleted(continuation)`，交给 `IUniTaskSource` 在完成时回调。

4. **异常与取消语义统一**  
   - 所有异常（包括取消）都统一由 `task.source.GetResult(token)` 抛出，  
     外部 `async UniTask` 方法与 `async Task` 完全一致的感觉。

5. **性能优化点**（不改变语义，只是实现细节）  
   - `Awaiter` 是 `readonly struct`，只是包了一层 `UniTask`，不自己持有复杂状态。
   - 真正的状态存在 `IUniTaskSource` 实现里，可以用对象池复用。
   - `AwaiterActions.InvokeContinuationDelegate` 静态复用，避免频繁创建 `Action<object>`。

---

### 7. 扩展一眼：为什么它还能被 `async` 直接返回？

在文件顶部，你还能看到这一行：

```32:34:UnityProject/Packages/UniTask/Runtime/UniTask.cs
[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct UniTask
```

- 这行特性告诉编译器：  
  **当一个方法写成 `async UniTask` 时，状态机要用 `AsyncUniTaskMethodBuilder` 来构建**。
- `AsyncUniTaskMethodBuilder`（在别的文件里）实现了 Task-like 类型规范：  
  - `Create / Start / SetResult / SetException / AwaitOnCompleted / Task` 等。
- 所以：
  - `UniTask` 既是一个 **可 await 的类型**  
  - 又是一个 **可以作为 async 方法返回值的 task-like 类型**。

---

如果你愿意，下一步我们可以用 `UniTask<T>.Awaiter` 再对照一遍（带返回值的情况），然后结合某个具体的 `IUniTaskSource` 实现（比如 Delay 或 Scene 加载）串起来，看**从底层 Source → Awaiter → 编译器状态机**的完整闭环。
