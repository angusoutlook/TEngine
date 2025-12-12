# TEngine UniTask & YooAsset & SceneModule 会话总结（Summary）

> 日期：2025-12-12
> 说明：本文件为本次关于 UniTask 与 YooAsset 设计、以及在 `SceneModule` 中使用方式的总结与关键样例代码。

---

## 一、本次会话主要结论概览

- **UniTask 的设计目的与实现方式**
  - 主要解决：
    - 标准 `Task` 在 Unity 场景下的 GC 分配开销大；
    - 与 Unity PlayerLoop/帧循环结合不够自然（难以做 `NextFrame/DelayFrame` 等）；
    - 与现有协程、`Task` 生态互操作不便。
  - 实现方式：
    - 使用值类型 `readonly struct UniTask` / `UniTask<T>` 搭配 `IUniTaskSource`，实现轻量任务;
    - 利用 `[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]` 把 `async UniTask` 变成编译器一等公民；
    - 通过 `PlayerLoopHelper` + 各种 `*Promise`（例如 `YieldPromise` / `DelayPromise` / `DelayRealtimePromise`）在不同 `PlayerLoopTiming` 下实现 `Yield/Delay/NextFrame` 等；
    - `UniTaskExtensions` 提供 `Task` ↔ `UniTask` 互转、`Timeout`、`AttachExternalCancellation`、`ToCoroutine`、`Forget` 等高级功能。

- **YooAsset 的异步模型与 OperationSystem 分层设计**
  - YooAsset 内部不依赖 C# `async/await` 或 `Coroutine`，而是：
    - 定义统一的 `AsyncOperationBase`，所有下载、解压、导入、加载、场景、卸载等操作都继承它；
    - 通过全局 `OperationSystem` 持有一个 `_operations` 列表，每帧在 `Update()` 中调用每个 Operation 的 `UpdateOperation()`；
    - `AsyncOperationBase` 内部持有 `Status`、`Error`、`Progress`、`TaskCompletionSource` 等信息，并提供 `Completed` 事件和 `Task` 封装。
  - 分层意图：
    - **底层内核层**：`OperationSystem` + `AsyncOperationBase` 负责统一调度和时间片控制；
    - **中间资源层**：`ProviderOperation` / `SceneProvider` 负责一个资源（例如场景）的完整异步流程（bundle 加载 → 场景加载）；
    - **最外 API/Handle 层**：`ResourcePackage` / `ResourceManager` + `HandleBase` / `SceneHandle` 对业务暴露友好的句柄和接口。

- **SceneModule 中 UniTask 与 YooAsset 的协同方式**
  - 业务模块 `SceneModule`：
    - 通过 `YooAssets.LoadSceneAsync` 获取 `SceneHandle` 作为底层异步载体；
    - 再通过：
      - 有进度时：`while (!handle.IsDone) { progress(handle.Progress); await UniTask.Yield(); }`；
      - 无进度时：`await handle.ToUniTask();`
    - 将 YooAsset 的 Operation 世界适配进 `async UniTask` 编程模型中。
  - 关键点：
    - **状态推进完全由 YooAsset 的 OperationSystem 和 Provider/FSOperation 完成**；
    - **UniTask 只负责业务层的等待表达与 per-frame 挂起（Yield），并不参与 YooAsset 内部状态机的推进。**

---

## 二、关键调用链：一次场景加载的三层结构

以主场景异步加载为例：

```csharp
// SceneModule.cs 中的核心调用
_currentMainScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);

if (progressCallBack != null)
{
    while (!_currentMainScene.IsDone && _currentMainScene.IsValid)
    {
        progressCallBack.Invoke(_currentMainScene.Progress);
        await UniTask.Yield();
    }
}
else
{
    await _currentMainScene.ToUniTask();
}

return _currentMainScene.SceneObject;
```

- **最外层**：`SceneModule` 拿到 `SceneHandle`，使用 UniTask 表达等待逻辑。  
- **中间层**：`ResourceManager.LoadSceneAsync` 创建 `SceneProvider`（继承 `ProviderOperation`），负责：
  - 通过 `CreateMainBundleFileLoader` / `CreateDependBundleFileLoaders` 加载所有相关 AssetBundle；
  - 在 `ProcessBundleResult` 中创建 `FSLoadSceneOperation`（`AssetBundleLoadSceneOperation` 或 `VirtualBundleLoadSceneOperation`），驱动其完成；
  - 成功后设置 `SceneObject`，并调用 `InvokeCompletion` 通知所有 `SceneHandle`。  
- **最内层**：`OperationSystem.Update` 每帧调用 `AsyncOperationBase.UpdateOperation`，内部又调用：
  - `ProviderOperation.InternalUpdate` → `SceneProvider.ProcessBundleResult` → `AssetBundleLoadSceneOperation.InternalUpdate`；
  - 后者实质调用 `SceneManager.LoadSceneAsync`，根据 `AsyncOperation.progress` / `isDone` 设置 `Progress` 与 `Status`。

当 `SceneProvider.InvokeCompletion` 被调用时，所有关联的 `SceneHandle` 会触发自己的 `Completed` 回调，从而唤醒上层的 `UniTask`/`Task` 等等待逻辑。

---

## 三、不使用 UniTask 的两个替代方案

### 1. 协程（Coroutine）版本

当不使用 UniTask 时，在 Unity 中最自然的写法是协程：

```csharp
public void LoadSceneAsync_Coroutine(
    MonoBehaviour runner,
    string location,
    LoadSceneMode sceneMode = LoadSceneMode.Single,
    bool suspendLoad = false,
    uint priority = 100,
    bool gcCollect = true,
    Action<Scene> completed = null,
    Action<float> progressCallback = null)
{
    runner.StartCoroutine(LoadSceneCoroutine(location, sceneMode, suspendLoad, priority, gcCollect, completed, progressCallback));
}

private IEnumerator LoadSceneCoroutine(
    string location,
    LoadSceneMode sceneMode,
    bool suspendLoad,
    uint priority,
    bool gcCollect,
    Action<Scene> completed,
    Action<float> progressCallback)
{
    var handle = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);

    if (progressCallback != null)
    {
        while (!handle.IsDone && handle.IsValid)
        {
            progressCallback(handle.Progress);
            yield return null;
        }
    }
    else
    {
        while (!handle.IsDone && handle.IsValid)
        {
            yield return null;
        }
    }

    completed?.Invoke(handle.SceneObject);
}
```

以及进度协程：

```csharp
private IEnumerator InvokeProgressCoroutine(SceneHandle sceneHandle, Action<float> progress)
{
    if (sceneHandle == null)
        yield break;

    while (!sceneHandle.IsDone && sceneHandle.IsValid)
    {
        yield return null;
        progress?.Invoke(sceneHandle.Progress);
    }
}
```

### 2. Task/Task<T> 版本

另一种是不依赖 UniTask，而使用标准 `Task`：

#### YooAsset → Task 适配扩展

```csharp
public static class YooAssetTaskExtensions
{
    public static Task<Scene> ToTask(this SceneHandle handle)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));

        if (handle.IsDone && handle.IsValid)
            return Task.FromResult(handle.SceneObject);

        var tcs = new TaskCompletionSource<Scene>();

        handle.Completed += h =>
        {
            if (!h.IsValid)
                tcs.TrySetException(new Exception("SceneHandle is invalid when completed."));
            else
                tcs.TrySetResult(h.SceneObject);
        };

        return tcs.Task;
    }

    public static Task ToTask(this GameAsyncOperation op)
    {
        if (op == null)
            throw new ArgumentNullException(nameof(op));

        if (op.IsDone)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<object>();

        op.Completed += _ =>
        {
            if (op.Status == EOperationStatus.Succeed)
                tcs.TrySetResult(null);
            else
                tcs.TrySetException(new Exception(op.Error));
        };

        return tcs.Task;
    }
}
```

#### SceneModule 的 Task 版接口

```csharp
public async Task<Scene> LoadSceneAsync_Task(
    string location,
    LoadSceneMode sceneMode = LoadSceneMode.Single,
    bool suspendLoad = false,
    uint priority = 100,
    bool gcCollect = true,
    Action<float> progressCallback = null)
{
    if (!_handlingScene.Add(location))
    {
        Log.Error($"Could not load scene while loading. Scene: {location}");
        return default;
    }

    if (sceneMode == LoadSceneMode.Additive)
    {
        if (_subScenes.TryGetValue(location, out SceneHandle subScene))
        {
            throw new Exception($"Could not load subScene while already loaded. Scene: {location}");
        }

        subScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);
        _subScenes.Add(location, subScene);

        if (progressCallback != null)
        {
            while (!subScene.IsDone && subScene.IsValid)
            {
                progressCallback(subScene.Progress);
                await Task.Yield();
            }
        }
        else
        {
            await subScene.ToTask();
        }

        _handlingScene.Remove(location);
        return subScene.SceneObject;
    }
    else
    {
        if (_currentMainScene is { IsDone: false })
        {
            throw new Exception($"Could not load MainScene while loading. CurrentMainScene: {_currentMainSceneName}.");
        }

        _currentMainSceneName = location;
        _currentMainScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);

        if (progressCallback != null)
        {
            while (!_currentMainScene.IsDone && _currentMainScene.IsValid)
            {
                progressCallback(_currentMainScene.Progress);
                await Task.Yield();
            }
        }
        else
        {
            await _currentMainScene.ToTask();
        }

        ModuleSystem.GetModule<IResourceModule>().ForceUnloadUnusedAssets(gcCollect);

        _handlingScene.Remove(location);
        return _currentMainScene.SceneObject;
    }
}
```

---

## 四、YooAsset 状态推进与 ToTask/ToUniTask 的关系

- `ToTask` / `ToUniTask` **本身不推进 YooAsset 的状态机**，只是订阅 `SceneHandle.Completed` 或 `AsyncOperationBase.Completed` 事件，在完成时设置 TCS/UniTaskCompletionSource 的结果。  
- 场景加载状态的推进路径实际是：
  1. `OperationSystem.Update()` 每帧调用 `AsyncOperationBase.UpdateOperation()`；
  2. 对 `SceneProvider` 而言，就是执行 bundle 加载 + FS 场景加载 Operation 的 `InternalUpdate()`；
  3. 当 FS Operation（如 `AssetBundleLoadSceneOperation`）结束时，设置自身的 `Status`；
  4. `SceneProvider.ProcessBundleResult` 检测到结束，调用 `InvokeCompletion`，
     - 从而遍历所有 `SceneHandle`，执行 `InvokeCallback` → 触发 `Completed`；
  5. `ToTask` / `ToUniTask` 中挂的回调被触发，外层 `await` 才从挂起恢复。  

因此：**UniTask/Task 只负责“怎么等”，而 OperationSystem/Provider/FSOperation 负责“怎么跑”。**

---

## 五、这一设计对 TEngine 的意义

- 资源域（YooAsset）与业务异步模型（UniTask/Task）彼此解耦：
  - 资源系统可以继续围绕 `OperationSystem` 演进（例如新增下载类型、调优时间片、改进调试工具）；
  - 业务层可以独立选择 `UniTask`、`Task` 或 `Coroutine` 作为自己的异步表达方式，只需在边界上做一次适配（`ToUniTask` / `ToTask` 等）。
- `SceneModule` 作为 TEngine 的场景门面模块，正好站在这两个世界的交界：
  - 下接 YooAsset 的 `SceneHandle` / `xxxOperation`；
  - 上接 UniTask/Task 的 async/await 逻辑与模块系统（`ModuleSystem`）。

这也是本次会话的核心：**看懂 YooAsset 的三层结构（OperationSystem / Provider / Handle）之后，再用 UniTask/Task 包装，才能在 TEngine 里写出既高性能又好维护的异步场景流程。**
