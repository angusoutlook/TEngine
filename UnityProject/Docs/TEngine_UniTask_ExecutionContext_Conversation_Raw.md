ICriticalNotifyCompletion 接口和 INotifyCompletion 接口，有什么区别？

我是 GPT-5.1 模型。

### 核心区别一句话总结

**`INotifyCompletion`**：标准“可等待”接口，只有安全的 `OnCompleted`，**必须捕获并还原执行上下文**（`ExecutionContext`）。  
**`ICriticalNotifyCompletion`**：扩展接口，增加了 `UnsafeOnCompleted`，允许**跳过执行上下文捕获/还原**，用于高性能/底层场景，但需要调用方自己保证安全。

---

### 1. 两个接口的关系

在 .NET 里，自定义 `awaiter` 通常长这样：

```csharp
public struct MyAwaiter : INotifyCompletion   // 或 ICriticalNotifyCompletion
{
    public bool IsCompleted { get; }
    public void OnCompleted(Action continuation);
    public void GetResult();
}
```

- `INotifyCompletion` 只要求实现：
  - `void OnCompleted(Action continuation);`

- `ICriticalNotifyCompletion` 继承自 `INotifyCompletion`，额外要求：
  - `void UnsafeOnCompleted(Action continuation);`

也就是说：

```csharp
public interface ICriticalNotifyCompletion : INotifyCompletion
{
    void UnsafeOnCompleted(Action continuation);
}
```

---

### 2. `OnCompleted` vs `UnsafeOnCompleted`

#### `OnCompleted`（来自 `INotifyCompletion`）

- 语义：**安全注册延续（continuation）**。
- 要求：
  - 需要 **捕获当前 `ExecutionContext`**（例如：`AsyncLocal`、安全上下文、文化信息等）。
  - 在触发 `continuation` 时，**在相同执行上下文中恢复执行**。
- 优点：
  - 行为安全、语义完整，对上层代码透明。
- 缺点：
  - 捕获/还原上下文有额外开销（分配、拷贝、还原）。

#### `UnsafeOnCompleted`（来自 `ICriticalNotifyCompletion`）

- 语义：**不捕获执行上下文的注册**，是一个“低开销 / 非安全”的版本。
- 特点：
  - **可以跳过 `ExecutionContext.Capture()`**，只把委托存起来，之后原样调用。
  - 性能更好，避免一些分配和上下文切换。
- 风险：
  - 触发 `continuation` 时，可能在**不同的执行上下文**下运行（比如线程池里没有原来的 `AsyncLocal` 值）。
  - 对有安全/上下文依赖的代码，可能产生隐含 bug。

---

### 3. 为什么需要两个接口？

- **设计意图**：
  - 大多数 awaiter 用 `OnCompleted` 就够了，行为简单且安全。
  - 一些底层高性能组件（例如 Task、ValueTask、某些自定义调度器）：
    - 很清楚自己在什么上下文中调度 continuation，
    - 或者明确不需要 `ExecutionContext`，
    - 就可以通过 `UnsafeOnCompleted` 省掉上下文捕获的成本。

因此：

- **框架/编译器可以优先使用 `UnsafeOnCompleted`**（如果存在，且调用点处用的是 `Unsafe` 版本 API），
- 否则退回到 `OnCompleted` 的安全版本。

---

### 4. 在代码中的常见实践

- 自己写 `awaiter` 时常见模式：

```csharp
public struct MyAwaiter : ICriticalNotifyCompletion
{
    public void OnCompleted(Action continuation)
    {
        // 安全版本：捕获 ExecutionContext 再调度
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        // 非安全版本：不捕获 ExecutionContext，直接存储/调用
    }
}
```

- 框架内部（如 `TaskAwaiter`）会：
  - 对普通 `await` 使用安全逻辑（捕获/还原 ExecutionContext）。
  - 对明确标记为“不需要上下文”的调用路径，使用 `UnsafeOnCompleted` 来减少开销。

---

### 5. 你在使用时应该怎么选？

- **如果只是写普通业务 awaiter**：
  - 通常只实现 `INotifyCompletion` 或只实现安全逻辑即可。
- **只有在你非常清楚执行上下文含义、且有明确性能需求时**：
  - 才考虑实现 `ICriticalNotifyCompletion` 并提供 `UnsafeOnCompleted`。
  - 调用时要区分：什么时候必须保持上下文（用 `OnCompleted`），什么时候可以忽略（用 `UnsafeOnCompleted`）。

---

### 小结

- **语义层面**：  
  - `INotifyCompletion` = “标准安全的 continuation 注册接口”。  
  - `ICriticalNotifyCompletion` = “附带一个可以跳过 ExecutionContext 捕获的高性能接口”。

- **核心差异点**就是：**是否捕获/还原执行上下文**，带来的是 **安全性 vs 性能** 的权衡。

能否举一个例子 体现 是否捕获和还原执行上下文？

我是 GPT-5.1 模型。

### 用一个具体例子看“是否捕获和还原执行上下文”

下面这个例子用 `AsyncLocal<T>` 来演示：  
**安全版本会让 `AsyncLocal` 的值“跟着延续代码一起流动”，不安全版本则不会。**

#### 1. 准备一个 `AsyncLocal` 和两个调度函数

```csharp
using System;
using System.Threading;

class Program
{
    // 用来观察 ExecutionContext 是否被“带过去”
    static AsyncLocal<string> UserName = new AsyncLocal<string>();

    static void Main()
    {
        Console.WriteLine("=== Safe（捕获并还原 ExecutionContext）===");
        DemoSafe();

        Console.WriteLine();
        Console.WriteLine("=== Unsafe（不捕获 ExecutionContext）===");
        DemoUnsafe();

        Console.ReadLine();
    }

    // 模拟 OnCompleted 的“安全”行为：捕获 + 还原 ExecutionContext
    static void SafeSchedule(Action continuation)
    {
        // 捕获当前 ExecutionContext（里面包含 AsyncLocal 等）
        ExecutionContext ec = ExecutionContext.Capture();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            // 在捕获的 ExecutionContext 中执行 continuation
            ExecutionContext.Run(ec, __ => continuation(), null);
        });
    }

    // 模拟 UnsafeOnCompleted 的“不安全”行为：不捕获 ExecutionContext
    static void UnsafeSchedule(Action continuation)
    {
        // 直接丢到线程池执行，不做 ExecutionContext 捕获/还原
        ThreadPool.QueueUserWorkItem(_ => continuation());
    }

    static void DemoSafe()
    {
        UserName.Value = "ZhangSan";

        Console.WriteLine($"主线程中 UserName = {UserName.Value}");

        SafeSchedule(() =>
        {
            // 这里会在捕获并还原过的 ExecutionContext 中运行
            Console.WriteLine($"SafeSchedule 中 UserName = {UserName.Value}");
        });
    }

    static void DemoUnsafe()
    {
        UserName.Value = "ZhangSan";

        Console.WriteLine($"主线程中 UserName = {UserName.Value}");

        UnsafeSchedule(() =>
        {
            // 这里在新线程里执行，但没有还原 ExecutionContext
            Console.WriteLine($"UnsafeSchedule 中 UserName = {UserName.Value}");
        });
    }
}
```

#### 2. 预期现象（核心区别）

- **SafeSchedule（模拟 `OnCompleted`）**：
  - 控制台输出大致类似：

    ```text
    主线程中 UserName = ZhangSan
    SafeSchedule 中 UserName = ZhangSan
    ```

  - 说明：**`AsyncLocal` 的值被“带”到了线程池里的 continuation 中**，因为我们捕获了 `ExecutionContext` 并在执行时还原了它。

- **UnsafeSchedule（模拟 `UnsafeOnCompleted`）**：
  - 输出大致类似：

    ```text
    主线程中 UserName = ZhangSan
    UnsafeSchedule 中 UserName =  // 通常是 null 或默认值
    ```

  - 说明：**`AsyncLocal` 的值没有被带过去**，因为我们完全没有捕获/还原 `ExecutionContext`。

---

### 3. 和 `INotifyCompletion` / `ICriticalNotifyCompletion` 的对应关系

- **现实中 `OnCompleted` 的实现**：  
  逻辑上类似上面的 `SafeSchedule`，会在内部做：

  ```csharp
  var ec = ExecutionContext.Capture();
  // 之后在 ec 中执行 continuation
  ```

- **现实中 `UnsafeOnCompleted` 的实现**：  
  逻辑上类似上面的 `UnsafeSchedule`，**不会调用 `ExecutionContext.Capture()`**，直接把 `continuation` 存起来，之后原样调用。

所以，这个例子直观体现了：

- **“捕获+还原执行上下文” ⇒ `AsyncLocal` 等上下文数据会在 continuation 中保持一致。**
- **“不捕获执行上下文” ⇒ continuation 运行时就像“换了环境”，很多上下文信息丢失或不同。**

如果你愿意，我可以再基于一个自定义 `awaiter` 写一版，分别用 `OnCompleted` / `UnsafeOnCompleted` 来调度 continuation，把上面这两个调度函数融合进去，和 `await` 语法直接关联起来看。

愿意，我有几个问题。你的例子
1、AsyncLocal类型有什么特点，是跟随线程的吗？
2、ExecutionContent.capture 是否可以解释一下。他会做哪些事情？

我是 GPT-5.1 模型。

### 1、`AsyncLocal<T>` 有什么特点？是不是“跟随线程”的？

**先说结论：`AsyncLocal<T>` 不是简单的“跟随线程”，而是跟随 `ExecutionContext`（也可以理解为“逻辑调用上下文”）。**

#### 1.1 和 `ThreadLocal<T>` 的对比，可以帮助理解

- **`ThreadLocal<T>`**：
  - 真正意义上的“每个物理线程有一份数据”。
  - 换线程 ⇒ 线程本地数据就变了，因为每个线程有自己的独立存储。
  - 很适合纯同步、固定线程模型。

- **`AsyncLocal<T>`**：
  - 绑定在 **`ExecutionContext`** 上，而不是物理线程本身。
  - `ExecutionContext` 会在 `async/await`、`Task`、线程池切换时 **“流动/复制”**。
  - 因此：
    - 在一个异步调用链（同一个“逻辑请求”）里，即使多次切线程，`AsyncLocal` 里的值看起来是**延续下去的**。
    - 不同请求/不同逻辑链路，各自有自己的 `ExecutionContext`，因此也有独立的 `AsyncLocal` 值。

你在同步场景、固定线程上观察时，会误以为 `AsyncLocal` 是“跟随线程”的，其实底层是“每个 `ExecutionContext` 一份”；只是**通常一个线程在某一时刻只有一个当前 `ExecutionContext`**，看起来就像是线程本地。

#### 1.2 `AsyncLocal` 的几个关键行为

- **设置值时**：当前 `ExecutionContext` 中这条 `AsyncLocal` 的值被设置/更新。
- **创建子任务/异步延续时**：
  - 如果没有抑制流动（没有 `ExecutionContext.SuppressFlow()`），新的异步执行会拿到当前 `ExecutionContext` 的一个“拷贝”，里面包含当前的 `AsyncLocal` 值。
- **恢复旧的 `ExecutionContext` 时**（比如 `ExecutionContext.Run` 结束时）：
  - `AsyncLocal` 的值会自动还原成之前上下文里的值，并且触发它的 `ValueChanged` 回调（如果有）。

**因此：`AsyncLocal` 适合保存“与一次请求/一次逻辑操作绑定”的上下文信息**，例如：
- 日志的 `CorrelationId`
- 当前租户/用户信息（仅作为“上下文提示”，注意不要当权限系统用）
- 某些全局的异步范围状态

---

### 2、`ExecutionContext.Capture()` 到底做了什么？

还是先给一个形象的描述：

> **`ExecutionContext.Capture()`：把“当前这条逻辑调用链的环境快照”拷贝一份，方便以后在别的线程/时间点还原，然后继续在这个环境下执行。**

#### 2.1 它大致会捕获哪些东西？

并不是所有细节都需要你记，但关键有这些（不同 .NET 版本内部实现略有不同，这里用逻辑层面的描述）：

- **所有 `AsyncLocal<T>` 的当前值**
  - 这是你在前面例子里最直观能感受到的部分。
- **逻辑调用上下文（LogicalCallContext）**
  - 早期 .NET 中很多“CallContext.LogicalSetData”之类的东西。
- **安全上下文 / 身份信息 / 某些环境信息**（在 .NET Core 里有所精简，但概念一致）
- **文化信息（CultureInfo）等**（被视为逻辑上下文的一部分时）

> 注意：`SynchronizationContext.Current`、`TaskScheduler.Current` 这些东西相关，但不必强行记住“是不是完全在 ExecutionContext 里”，对理解 `OnCompleted` vs `UnsafeOnCompleted` 不关键。你只需要记住：**`ExecutionContext` = 异步流动的“环境包裹”，里面肯定包含 `AsyncLocal`。**

#### 2.2 `Capture` + `Run` 的典型配合

通常配合是这样的（我用伪代码表达逻辑）：

```csharp
// 1. 在当前线程、当前上下文里
ExecutionContext ec = ExecutionContext.Capture();

// 2. 把工作丢到线程池或别的地方
ThreadPool.QueueUserWorkItem(_ =>
{
    // 3. 在新线程上，先用 Run 临时“切换”到捕获的上下文
    ExecutionContext.Run(ec, state =>
    {
        // 这里看到的 AsyncLocal、LogicalCallContext 等
        // 就和 Capture 时那一刻保持一致
        // ...
    }, null);

    // 4. Run 结束后，自动还原回原来的 ExecutionContext
});
```

**`ExecutionContext.Run` 会做两件事：**

1. **保存当前线程正在使用的 ExecutionContext**（比如线程池原来的环境）。
2. **把刚才 Capture 的那个 ExecutionContext 设为当前**，执行委托，然后再把原来的还原回来。

这就是你在前一个示例中看到的：

- 使用 `SafeSchedule`（模拟 `OnCompleted`）时：
  - 本质就是 `Capture` 当前 context，然后在线程池里用 `Run` 还原它再执行 continuation ⇒ **`AsyncLocal` 值“跟着走了”**。
- 使用 `UnsafeSchedule`（模拟 `UnsafeOnCompleted`）时：
  - 不做 `Capture` / `Run`，而是在“当前线程池默认上下文”下执行 continuation ⇒ **`AsyncLocal` 值就不一定是你之前设置的了**。

#### 2.3 还需要注意的一点：`SuppressFlow`

- 如果代码调用了：

  ```csharp
  using (ExecutionContext.SuppressFlow())
  {
      // 在这个 using 里面排队的任务，不会自动携带当前 ExecutionContext
  }
  ```

- 那么此时 `ExecutionContext.Capture()` 会得到一个“被禁止流动”的状态，后续队列任务一般不会再自动带着原来的 `AsyncLocal` 等环境。
- 这是一些底层高性能 API（例如 `ThreadPool.UnsafeQueueUserWorkItem`）配合使用的手段，用来**显式地切断上下文流动**，换取性能，避免意外的上下文污染。

---

### 3、和 `INotifyCompletion` / `ICriticalNotifyCompletion` 的连接

有了上面的概念，可以把两者对应起来看：

- **`OnCompleted`（安全）** ≈
  - 在内部做类似：

    ```csharp
    var ec = ExecutionContext.Capture();
    // 存下 continuation 和 ec，将来执行时：
    ExecutionContext.Run(ec, _ => continuation(), null);
    ```

  - ⇒ 让 continuation 在**同一个 ExecutionContext** 中运行，所以你的 `AsyncLocal` 值会“延续”。

- **`UnsafeOnCompleted`（不安全 / 高性能）** ≈
  - 在内部**不做 `Capture` / `Run`**，直接存 continuation，将来直接在“那时的当前 ExecutionContext”下调用：
    - 可能是线程池的默认上下文，
    - 也可能是某个其他地方设置过的新上下文。
  - ⇒ `AsyncLocal` 等环境信息**不保证会沿用原来的**。

---

如果你愿意，下一步我可以：
- 用一个**自定义 awaiter**，展示：
  - `OnCompleted` 里用 `SafeSchedule`（带 `ExecutionContext.Capture/Run`）；
  - `UnsafeOnCompleted` 里用 `UnsafeSchedule`（不捕获）；
- 然后在 `async` 方法里直接 `await` 它，直观感受两种实现下 `AsyncLocal` 的不同表现。

先解释一下如何run 自动还原原来的executioncontext 原来是保存在哪里？还原是手动写？还是架构自动实现？

我是 GPT-5.1 模型。

### 一句话先答你问题

**`ExecutionContext.Run` 会在内部做：先把“当前的 ExecutionContext”保存在一个局部变量里，然后把线程的当前 ExecutionContext 切换成传入的那个，执行回调，最后用 `try/finally` 自动把原来的值还原回去。**  
保存和还原都是由运行库（`System.Private.CoreLib` 里的实现）自动完成的，你不需要、也不能自己手动写那部分。

---

### 1. “原来的 ExecutionContext 是保存在哪里？”

从逻辑上可以这样理解（伪代码，只是帮助你建立模型）：

```csharp
public static void Run(ExecutionContext newContext, ContextCallback callback, object state)
{
    // 每个线程都有一个“当前的 ExecutionContext”，类似 thread-static 变量
    ExecutionContext prevContext = Thread.CurrentThread.ExecutionContext;

    try
    {
        // 把当前线程的 ExecutionContext 切换成传入的 newContext
        Thread.CurrentThread.ExecutionContext = newContext;

        // 在这个新的 ExecutionContext 下执行你的回调
        callback(state);
    }
    finally
    {
        // 不管回调里发生什么（包括异常），都还原成之前的 prevContext
        Thread.CurrentThread.ExecutionContext = prevContext;
    }
}
```

重点有两点：

- **“原来的 ExecutionContext”**：  
  就是调用 `Run` 之前，**当前线程**挂着的那个 ExecutionContext，存到一个局部变量 `prevContext` 里。
- **每个线程都有一个当前 ExecutionContext 引用**：  
  你可以把它类比为某个 `ThreadStatic` 字段，比如（只是理解层面）：

  ```csharp
  [ThreadStatic]
  private static ExecutionContext t_current;
  ```

  真正实现比这个复杂些，但概念上就是 **“当前线程上挂着一个 ExecutionContext 引用”**。

---

### 2. “还原是手动写还是框架自动实现？”

**还原完全是框架内部在 `ExecutionContext.Run` 里自动做的**：

- 你调用的只是：

  ```csharp
  ExecutionContext.Run(capturedEc, callback, state);
  ```

- 里面的逻辑（简化理解）就是：

  1. 读出当前线程原来的 ExecutionContext ⇒ `prevContext`
  2. 切换到 `capturedEc`
  3. `try { callback(state); }`
  4. `finally { 恢复 prevContext; }`

- 不管 `callback` 里抛不抛异常，`finally` 都会执行，**确保上下文一定被还原**，否则线程池里的线程会“带着错误的上下文”去执行别的任务，后果很严重，所以这块一定是由 BCL 内部统一处理的。

你自己**不需要写任何还原代码**，也 **无法** 通过公共 API 去直接改实现，只是“使用者”。

---

### 3. 跟你前面问的 `OnCompleted` / `UnsafeOnCompleted` 的联系

结合前面的讨论，你可以把安全版本的 continuation 调度，想象成这样：

```csharp
// 捕获当前 ExecutionContext（里面有 AsyncLocal 等）
var ec = ExecutionContext.Capture();

// 以后要执行 continuation 时：
ExecutionContext.Run(ec, _ => continuation(), null);
```

**`Run` 内部就负责：**

- 暂存当前线程的 ExecutionContext；
- 暂时换成 `ec`；
- 执行 continuation；
- 再自动恢复。

而 `UnsafeOnCompleted` 的实现就不会去 `Capture`，也不会去 `Run`，只是在“那一刻当前线程的 ExecutionContext”下直接调用 continuation，**因此不会自动带上你当初捕获时的那些 `AsyncLocal` / 上下文信息**。

---

如果你愿意，下一步我可以用一个自定义 awaiter，把：

- `OnCompleted` ⇒ 用 `ExecutionContext.Capture + Run`
- `UnsafeOnCompleted` ⇒ 直接调度

都写出来，你可以从编译器生成的状态机角度再看一眼整个过程是怎么串起来的。

愿意

我是 GPT-5.1 模型。

### 总体说明（先说清这段代码的定位）

下面是一个**简化示例**，目的是：  
- 同一个 `await`，内部根据参数选择“**捕获+还原 ExecutionContext**”或“**不捕获 ExecutionContext**”；  
- 用 `AsyncLocal<string>` 清楚看到两种行为的差异。  

> 说明：  
> - 为了演示效果，我在自定义 awaiter 的 `UnsafeOnCompleted` 里，**手工决定走“安全版”还是“非安全版”调度**，这和真实 BCL 的实现略有差别，是**教学用的简化版**。  
> - 真实框架中，编译器会通过 `AwaitOnCompleted` / `AwaitUnsafeOnCompleted` 等配合使用 `OnCompleted` / `UnsafeOnCompleted`，但本质差异仍然是：**是否捕获/还原 ExecutionContext**。

---

### 完整示例代码（控制台程序）

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

class Program
{
    // 用来观察 ExecutionContext 是否被“带过去”
    static AsyncLocal<string> Context = new AsyncLocal<string>();

    static async Task Main()
    {
        Console.WriteLine("=== 使用“安全模式”（模拟捕获 ExecutionContext）===");
        await UseSafeAsync();

        Console.WriteLine();
        Console.WriteLine("=== 使用“非安全模式”（模拟不捕获 ExecutionContext）===");
        await UseUnsafeAsync();

        Console.ReadLine();
    }

    static async Task UseSafeAsync()
    {
        Context.Value = "Safe-Value";
        Console.WriteLine($"[Safe] await 之前，Context = {Context.Value}");

        // 这里的 await 内部会选用“捕获 + 还原 ExecutionContext”的调度方式
        await new CustomAwaitable(useUnsafe: false);

        Console.WriteLine($"[Safe] await 之后，Context = {Context.Value}");
    }

    static async Task UseUnsafeAsync()
    {
        Context.Value = "Unsafe-Value";
        Console.WriteLine($"[Unsafe] await 之前，Context = {Context.Value}");

        // 这里的 await 内部会选用“不捕获 ExecutionContext”的调度方式
        await new CustomAwaitable(useUnsafe: true);

        Console.WriteLine($"[Unsafe] await 之后，Context = {Context.Value}");
    }

    // -------------------- 自定义 Awaitable / Awaiter --------------------

    public readonly struct CustomAwaitable
    {
        private readonly bool _useUnsafe;

        public CustomAwaitable(bool useUnsafe)
        {
            _useUnsafe = useUnsafe;
        }

        public CustomAwaiter GetAwaiter() => new CustomAwaiter(_useUnsafe);
    }

    public struct CustomAwaiter : ICriticalNotifyCompletion
    {
        private readonly bool _useUnsafe;

        public CustomAwaiter(bool useUnsafe)
        {
            _useUnsafe = useUnsafe;
        }

        // 一直返回 false，强制走异步路径
        public bool IsCompleted => false;

        public void GetResult()
        {
            // 这里没有返回值，就什么都不做
        }

        // 这里为了演示，简单地转调到 UnsafeOnCompleted
        public void OnCompleted(Action continuation)
        {
            // 实际框架中，OnCompleted 通常是“安全版本”，会配合 ExecutionContext
            // 这里为了减少重复逻辑，直接调用 UnsafeOnCompleted，
            // 真正的“安全/非安全”差异在 UnsafeOnCompleted 里根据 _useUnsafe 决定。
            UnsafeOnCompleted(continuation);
        }

        // 关键：根据 _useUnsafe 决定是否捕获 ExecutionContext
        public void UnsafeOnCompleted(Action continuation)
        {
            if (_useUnsafe)
            {
                // 非安全版：不捕获 ExecutionContext
                UnsafeSchedule(continuation);
            }
            else
            {
                // 安全版：捕获并还原 ExecutionContext
                SafeSchedule(continuation);
            }
        }

        // ---- 调度函数：一个安全，一个不安全 ----

        private static void SafeSchedule(Action continuation)
        {
            // 捕获当前 ExecutionContext（里面包括 AsyncLocal 等）
            ExecutionContext ec = ExecutionContext.Capture();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                // 在捕获的 ExecutionContext 中执行 continuation
                ExecutionContext.Run(ec, __ =>
                {
                    Console.WriteLine($"[SafeSchedule] continuation 线程：{Environment.CurrentManagedThreadId}, Context = {Program.Context.Value}");
                    continuation();
                }, null);
            });
        }

        private static void UnsafeSchedule(Action continuation)
        {
            // 不捕获 ExecutionContext，直接在线程池执行
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Console.WriteLine($"[UnsafeSchedule] continuation 线程：{Environment.CurrentManagedThreadId}, Context = {Program.Context.Value}");
                continuation();
            });
        }
    }
}
```

---

### 运行时你会看到什么（现象解释）

大致输出（线程号可能不同）会类似这样：

```text
=== 使用“安全模式”（模拟捕获 ExecutionContext）===
[Safe] await 之前，Context = Safe-Value
[SafeSchedule] continuation 线程：9, Context = Safe-Value
[Safe] await 之后，Context = Safe-Value

=== 使用“非安全模式”（模拟不捕获 ExecutionContext）===
[Unsafe] await 之前，Context = Unsafe-Value
[UnsafeSchedule] continuation 线程：10, Context =          // 通常是 null 或旧值
[Unsafe] await 之后，Context =                             // 同上，和 Unsafe-Value 不同
```

**关键对比点：**

- **安全模式（`useUnsafe: false` + `SafeSchedule` + `ExecutionContext.Capture/Run`）**：
  - `AsyncLocal` 的值：  
    - `await` 之前是 `Safe-Value`  
    - 调度线程中的日志里也是 `Safe-Value`  
    - `await` 之后依然是 `Safe-Value`  
  - 说明：**Continuation 在同一个 ExecutionContext 中执行，环境（包括 AsyncLocal）被正确“带过去”并在结束后还原。**

- **非安全模式（`useUnsafe: true` + `UnsafeSchedule` 不捕获 ExecutionContext）**：
  - 调度线程里的 `Context.Value` 通常为 `null` 或其他值；
  - `await` 之后看到的 `Context.Value` 也不再是我们刚设置的 `Unsafe-Value`。  
  - 说明：**Continuation 在“一个没有还原原始 ExecutionContext 的上下文”中执行，`AsyncLocal` 等环境信息没有跟随。**

---

### 和你之前的问题的直接对应关系

- **`ExecutionContext.Capture()` + `ExecutionContext.Run(...)`：**
  - 就像上面 `SafeSchedule` 里做的那样，先捕获当前的 ExecutionContext，再在新的线程里用 `Run` 暂时切换到这个上下文，执行 continuation，最后自动还原。
- **`UnsafeOnCompleted` 里使用 `UnsafeSchedule`：**
  - 完全不做 `Capture` / `Run`，结果就是：  
    **是否能“看到”之前设置的 `AsyncLocal` 值，完全取决于新线程当时的 ExecutionContext，而不是调用处的 ExecutionContext。**

如果你还想更“贴近真实编译器生成的状态机”，我可以再用 `async` 方法的反编译伪代码，画一下 `AwaitOnCompleted` / `AwaitUnsafeOnCompleted` 如何调用 `OnCompleted` / `UnsafeOnCompleted` 的流程图，但那会更偏底层实现。

把对话一字不差的保存下来，取个合适的名字，放在合适的目录
