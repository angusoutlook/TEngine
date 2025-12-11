我是 GPT-5.1 模型。

### 会话总结：TEngine 场景模块（SceneModule）

本总结基于本次会话的全部内容，提炼关键概念与使用方式，**仅为中文说明，文件名为英文**。

---

## 1. 核心角色与架构

- **`ISceneModule` 接口**：抽象场景管理能力，提供：
  - `LoadSceneAsync / LoadScene`：加载主场景或 Additive 子场景，可选 `suspendLoad`（挂起加载）、进度回调。  
  - `ActivateScene`：激活指定场景（主场景或子场景）。  
  - `UnSuspend`：解除挂起状态，让 `suspendLoad` 的场景继续完成加载。  
  - `UnloadAsync / Unload`：卸载 Additive 子场景，支持进度回调。  
  - `IsMainScene` / `IsContainScene`：判断是否是主场景、或是否在当前模块管理的场景集合中。

- **`SceneModule` 实现类**（`internal class SceneModule : Module, ISceneModule`）：
  - 使用 YooAsset 的 `SceneHandle` 管理场景加载与卸载。  
  - 字段：
    - `_currentMainSceneName`：当前主场景名称。  
    - `_currentMainScene`：当前主场景 `SceneHandle`。  
    - `_subScenes`：Additive 子场景字典 `location → SceneHandle`。  
    - `_handlingScene`：正在“加载或卸载中的场景地址集合”，用于**防止重复操作**。
  - 生命周期：
    - `OnInit()`：默认把 BuildIndex=0 的场景名作为当前主场景名。  
    - `Shutdown()`：遍历卸载所有子场景，清空内部状态。

- **与 YooAsset 的关系**：
  - 所有加载都通过 `YooAssets.LoadSceneAsync` 获取 `SceneHandle`。  
  - 激活/解挂/卸载分别调用 `SceneHandle.ActivateScene()`、`SceneHandle.UnSuspend()`、`SceneHandle.UnloadAsync()`。  
  - 切换主场景时调用 `IResourceModule.ForceUnloadUnusedAssets(gcCollect)` 做资源回收。

- **业务访问入口**：
  - 通过 `GameModule.Scene` 静态属性获取 `ISceneModule`：
    - `public static ISceneModule Scene => _scene ??= Get<ISceneModule>();`
  - 业务层不直接依赖 `SceneModule` 实现，而是依赖接口 `ISceneModule`。

---

## 2. 关键概念与细节

### 2.1 `await UniTask.Yield()` 与 `await subScene.ToUniTask()`

- **`await UniTask.Yield()`**：
  - 含义：将执行权让出到下一帧，在下一帧从当前位置继续执行。  
  - 用途：在 `while (!subScene.IsDone && subScene.IsValid)` 轮询中，每帧更新进度条，避免在一帧内死循环。

- **`await subScene.ToUniTask()`**：
  - 含义：将 `SceneHandle` 的异步加载包装为 `UniTask`，直到场景加载完成（`IsDone == true`）才返回。  
  - 用途：不关心进度时，简单地等待加载结束。

两者配合使用的模式：
- 有进度回调：**自己写 `while + Yield` 轮询进度**。  
- 无进度回调：**直接 `await ToUniTask()`，直到完成**。

### 2.2 `ActivateScene()` 与 `UnSuspend()`（YooAsset 的场景句柄 API）

- **`SceneHandle.ActivateScene()`**：
  - 检查：`IsValidWithWarning`、`SceneObject.IsValid()`、`SceneObject.isLoaded`。  
  - 内部调用：`SceneManager.SetActiveScene(SceneObject)`。  
  - 作用：在有多个已加载场景时，把该场景设为 Unity 的**当前激活场景**。

- **`SceneHandle.UnSuspend()`**：
  - 针对是 `SceneProvider` 的异步加载提供者，调用其 `UnSuspendLoad()`。  
  - 作用：解除 `suspendLoad = true` 造成的“挂起”，让加载流程继续直至完成。

`SceneModule` 中的封装：
- `ISceneModule.ActivateScene(string location)`：根据 `location` 查主场景或子场景的 `SceneHandle` 并调用 `ActivateScene()`。  
- `ISceneModule.UnSuspend(string location)`：同理，根据 `location` 调用对应 `SceneHandle.UnSuspend()`。

### 2.3 `subScene.SceneObject == default` 的含义

- `Scene` 是结构体，`default(Scene)`（或 `new Scene()`）表示“空场景”，没有指向任何已加载的场景：
  - `IsValid()` 返回 `false`。  
  - 不对应任何实际存在的 Unity 场景。
- `SceneHandle.SceneObject` 的逻辑：
  - 如果句柄无效 (`IsValidWithWarning == false`)，返回 `new Scene()`（即默认值）。  
  - 只有加载成功后，`Provider.SceneObject` 才会是有效场景。
- 所以在 `UnloadAsync` 中：
  - `if (subScene.SceneObject == default)` 表示当前没有一个真正加载成功的场景可以卸载，因此视为“未加载成功，卸载无效”，直接报错并返回 `false`。

---

## 3. 关键示例代码（必须保留）

### 3.1 场景流程示例脚本：预加载 + 解除挂起 + 激活 + 子场景加载/卸载

> 说明：这是本次会话中给出的**真实示例脚本**，可以直接放入项目（例如 `Assets/GameScripts/SceneFlowController.cs`）。

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TEngine;

/// <summary>
/// 用 TEngine 的 SceneModule 串起：预加载主场景 → 解除挂起并激活 → 加载/卸载子场景。
/// </summary>
public class SceneFlowController : MonoBehaviour
{
    [Header("主场景（YooAssets 场景地址）")]
    [SerializeField] private string mainSceneLocation = "Scenes/Main";

    [Header("子场景（Additive，YooAssets 场景地址）")]
    [SerializeField] private string subSceneLocation = "Scenes/Environment";

    private void Start()
    {
        // 使用 UniTask 驱动一个异步流程
        StartFlow().Forget();
    }

    /// <summary>
    /// 整体流程：预加载主场景(挂起) → 解除挂起并激活 → 加载子场景 → 卸载子场景。
    /// </summary>
    private async UniTaskVoid StartFlow()
    {
        // 1. 预加载主场景，使用 suspendLoad = true
        await PreloadMainSceneAsync();

        // 2. 解除挂起并激活主场景
        ActivateMainScene();

        // 3. 加载一个 Additive 子场景
        await LoadSubSceneAdditiveAsync();

        // 这里可以做一些游戏逻辑...

        // 4. 需要时卸载子场景
        await UnloadSubSceneAsync();
    }

    /// <summary>
    /// 使用 suspendLoad = true 预加载主场景，但暂时不激活。
    /// 对应 SceneModule.LoadSceneAsync(... suspendLoad: true ...)
    /// </summary>
    private async UniTask PreloadMainSceneAsync()
    {
        Debug.Log($"开始预加载主场景：{mainSceneLocation}");

        await GameModule.Scene.LoadSceneAsync(
            location: mainSceneLocation,
            sceneMode: LoadSceneMode.Single,
            suspendLoad: true,         // 关键：挂起加载
            priority: 100,
            gcCollect: false,          // 切主场景时是否顺便做资源 GC，可按需要调整
            progressCallBack: progress =>
            {
                // 这里可以更新加载 UI（进度条等）
                Debug.Log($"主场景预加载进度：{progress:P0}");
            });

        Debug.Log("主场景预加载完成（仍处于挂起状态）");
    }

    /// <summary>
    /// 解除挂起并激活主场景。
    /// 对应 SceneModule.UnSuspend + SceneModule.ActivateScene
    /// </summary>
    private void ActivateMainScene()
    {
        Debug.Log($"尝试解除挂起并激活主场景：{mainSceneLocation}");

        // 1) 解除挂起（让 YooAsset 继续完成场景加载流程）
        bool unSuspendOk = GameModule.Scene.UnSuspend(mainSceneLocation);
        if (!unSuspendOk)
        {
            Debug.LogWarning("UnSuspend 主场景失败，可能场景句柄无效或未挂起。");
            return;
        }

        // 2) 激活场景（设置为 Unity 当前激活场景）
        bool activateOk = GameModule.Scene.ActivateScene(mainSceneLocation);
        if (!activateOk)
        {
            Debug.LogWarning("激活主场景失败，请检查场景是否已加载完成。");
            return;
        }

        Debug.Log($"主场景已激活：{GameModule.Scene.CurrentMainSceneName}");
    }

    /// <summary>
    /// 以 Additive 模式加载一个子场景。
    /// 对应 SceneModule.LoadSceneAsync(... LoadSceneMode.Additive ...)
    /// </summary>
    private async UniTask LoadSubSceneAdditiveAsync()
    {
        Debug.Log($"开始以 Additive 方式加载子场景：{subSceneLocation}");

        await GameModule.Scene.LoadSceneAsync(
            location: subSceneLocation,
            sceneMode: LoadSceneMode.Additive,
            suspendLoad: false,
            priority: 50,
            gcCollect: false,
            progressCallBack: progress =>
            {
                Debug.Log($"子场景加载进度：{progress:P0}");
            });

        bool contain = GameModule.Scene.IsContainScene(subSceneLocation);
        Debug.Log($"子场景加载完成，IsContainScene = {contain}");
    }

    /// <summary>
    /// 卸载 Additive 子场景。
    /// 对应 SceneModule.UnloadAsync
    /// </summary>
    private async UniTask UnloadSubSceneAsync()
    {
        if (!GameModule.Scene.IsContainScene(subSceneLocation))
        {
            Debug.Log($"子场景未加载，无需卸载：{subSceneLocation}");
            return;
        }

        Debug.Log($"开始卸载子场景：{subSceneLocation}");

        bool result = await GameModule.Scene.UnloadAsync(
            location: subSceneLocation,
            progressCallBack: progress =>
            {
                Debug.Log($"子场景卸载进度：{progress:P0}");
            });

        Debug.Log($"子场景卸载完成，结果：{result}");
    }
}
```

---

## 4. 实际使用建议

- 在业务层，统一通过 `GameModule.Scene` 使用场景模块，避免直接依赖 `SceneModule` 实现。  
- 若需要加载进度 UI，优先使用带 `progressCallBack` 的接口，并在回调中更新 UI。  
- 对于大场景切换，推荐：
  1. 使用 `suspendLoad = true` 预加载。  
  2. 在过场/准备完毕后调用 `UnSuspend`。  
  3. 再通过 `ActivateScene` 切换激活场景。  
  4. 对旧场景或子场景使用 `UnloadAsync` 释放内存。