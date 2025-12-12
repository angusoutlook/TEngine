我是 GPT-5.1 模型。

===== User 1 =====
<user_info>
OS Version: win32 10.0.26200

Shell: powershell

Workspace Path: d:\workspace\unity\TEngine

Is directory a git repo: Yes, at D:/workspace/unity/TEngine

Today's date: Friday Dec 12, 2025

Terminals folder: C:\Users\angus\.cursor\projects\d-workspace-unity-TEngine/terminals
</user_info>

<git_status>
This is the git status at the start of the conversation. Note that this status is a snapshot in time, and will not update during the conversation.


Git repo: D:/workspace/unity/TEngine

## main...origin/main
 D UnityProject/UserSettings/Layouts/CurrentMaximizeLayout.dwlt
 M UnityProject/UserSettings/Layouts/default-2022.dwlt
?? UnityProject/UserSettings/Search.index
</git_status>

<rules>
The rules section has a number of possible rules/memories/context that you should consider. In each subsection, we provide instructions about what information the subsection contains and how you should consider/follow the contents of the subsection.


<user_rules description="These are rules set by the user that you should follow if appropriate.">
<user_rule>如果我让你分析代码的时候，需要做到以下几点：
1、直接分析真实代码：
"请基于我提供的实际代码进行分析"
"不要简化或重写代码"
2、保持准确性：
"请确保解释与真实代码完全一致"
"如果代码复杂，请逐步解释，但不要改变原始逻辑"
3、明确标注：
"如果需要简化说明，请明确标注这是简化版本"
"区分概念解释和代码分析"
4、指出关键实现细节并解释设计意图</user_rule>

<user_rule>Always respond in Chinese-simplified.
Always start your answer by telling me what AI model you are using.
Do not arbitrarily expand or modify my ideas.
Do not write test code.
if it is a Unity Project , make sure the Unity version is 2022.3.61f1c1.</user_rule>
</user_rules>
</rules>

===== User 2 =====
<user_info>
OS Version: win32 10.0.26200

Shell: powershell

Workspace Path: d:\workspace\unity\TEngine

Is directory a git repo: Yes, at D:/workspace/unity/TEngine

Today's date: Friday Dec 12, 2025

Terminals folder: C:\Users\angus\.cursor\projects\d-workspace-unity-TEngine/terminals
</user_info>

<git_status>
This is the git status at the start of the conversation. Note that this status is a snapshot in time, and will not update during the conversation.


Git repo: D:/workspace/unity/TEngine

## main...origin/main
 D UnityProject/UserSettings/Layouts/CurrentMaximizeLayout.dwlt
 M UnityProject/UserSettings/Layouts/default-2022.dwlt
?? UnityProject/UserSettings/Search.index
</git_status>

<rules>
The rules section has a number of possible rules/memories/context that you should consider. In each subsection, we provide instructions about what information the subsection contains and how you should consider/follow the contents of the subsection.


<user_rules description="These are rules set by the user that you should follow if appropriate.">
<user_rule>如果我让你分析代码的时候，需要做到以下几点：
1、直接分析真实代码：
"请基于我提供的实际代码进行分析"
"不要简化或重写代码"
2、保持准确性：
"请确保解释与真实代码完全一致"
"如果代码复杂，请逐步解释，但不要改变原始逻辑"
3、明确标注：
"如果需要简化说明，请明确标注这是简化版本"
"区分概念解释和代码分析"
4、指出关键实现细节并解释设计意图</user_rule>

<user_rule>Always respond in Chinese-simplified.
Always start your answer by telling me what AI model you are using.
Do not arbitrarily expand or modify my ideas.
Do not write test code.
if it is a Unity Project , make sure the Unity version is 2022.3.61f1c1.</user_rule>
</user_rules>
</rules>

<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\README.md (total lines: 104)

Files that are currently open and visible in the user's IDE:
- c:\Users\angus\.cursor\projects\d-workspace-unity-TEngine\terminals\1.txt (total lines: 268)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
@UnityProject/Assets/TEngine/Runtime/Module/SceneModule 阅读源代码 解释架构和设计意图，并写出使用示例，如果项目中有现成的例子，请务必举出 
</user_query>

===== Assistant 1 =====
我会先查看 `SceneModule` 目录下的源码文件结构，然后逐个阅读核心脚本，基于真实代码解释架构与设计意图，最后从项目中搜索和提炼实际使用示例。接下来开始扫描目录和读取关键文件。

===== Assistant 2 =====
我已经读完 `ISceneModule` 和 `SceneModule` 的源码，接下来会在整个项目里搜索它的调用点，从实际调用代码里抽出典型使用示例并结合刚才的实现来讲设计意图。

===== Assistant 3 =====
我是 GPT-5.1 模型。  

下面的回答分为两类内容，并 **明确区分**：  
- **「代码逐步分析」：完全按你给的真实源码逐行/逐块解释，不改变逻辑。**  
- **「简化概念 & 使用示例」：为了好理解而做的概念化说明或伪代码示例，我会明确标注这是简化版/示例，不在当前项目源码中。**

---

### 一、整体架构概览（简化概念说明）

> 本小节是**简化版概念说明**，帮助快速建立整体印象，不是源码逐行解析。

- **模块体系**：  
  - `SceneModule` 继承自框架的 `Module` 基类（源码不在当前目录，但从命名可知是统一生命周期的模块基类，提供 `OnInit/Shutdown` 等）。  
  - 对外只暴露接口 `ISceneModule`，真正实现类 `SceneModule` 标记为 `internal`，通过 `ModuleSystem.GetModule<ISceneModule>()` 获取。  
  - 在业务层统一通过 `GameModule.Scene` 访问场景模块：

    ```68:72:UnityProject/Assets/GameScripts/HotFix/GameLogic/GameModule.cs
    /// <summary>
    /// 获取场景模块。
    /// </summary>
    public static ISceneModule Scene => _scene ??= Get<ISceneModule>();
    
    private static ISceneModule _scene;
    ```

- **主场景 vs 子场景的区分**：  
  - 非 `LoadSceneMode.Additive` 的加载被视为 **主场景**，用 `_currentMainSceneName` 和 `_currentMainScene` 记录。  
  - `LoadSceneMode.Additive` 加载的被视为 **子场景**，统一放在 `_subScenes : Dictionary<string, SceneHandle>` 里。  
  - 提供 `IsMainScene / IsContainScene` 来判断场景归属。

- **与 YooAssets 的集成**：  
  - 内部全部使用 `YooAssets.LoadSceneAsync` 返回的 `SceneHandle` 来管理加载、激活、卸载场景。  
  - `SceneModule` 其实是「**模块系统 + YooAssets 场景接口**」之间的适配层：  
    - 提供统一的接口（接口化 + 模块化）  
    - 封装 `suspendLoad / ActivateScene / UnloadAsync` 等细节  
    - 在切主场景时顺便调用 `IResourceModule.ForceUnloadUnusedAssets(gcCollect)` 做资源回收。

- **并发/状态保护**：  
  - 用 `_handlingScene : HashSet<string>` 记录当前正在「加载/卸载」中的场景地址，**防止对同一场景的重复操作**（重复加载或同时加载+卸载）。  
  - 同时限制：  
    - 主场景：不允许在一个主场景还没加载完时再切另一个主场景。  
    - 子场景：不允许重复加载已经存在的子场景。

---

### 二、`ISceneModule` 接口 —— 能力面（代码分析）

> 本小节开始为**源码分析**，完全基于你提供的真实代码，不改写逻辑。

接口定义位置：

```7:86:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/ISceneModule.cs
public interface ISceneModule
{
    public string CurrentMainSceneName { get; }

    public UniTask<Scene> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false, uint priority = 100, bool gcCollect = true,
        Action<float> progressCallBack = null);

    public void LoadScene(string location,
        LoadSceneMode sceneMode = LoadSceneMode.Single,
        bool suspendLoad = false,
        uint priority = 100,
        Action<Scene> callBack = null,
        bool gcCollect = true,
        Action<float> progressCallBack = null);

    public bool ActivateScene(string location);

    public bool UnSuspend(string location);

    public bool IsMainScene(string location);

    public UniTask<bool> UnloadAsync(string location, Action<float> progressCallBack = null);

    public void Unload(string location, Action callBack = null, Action<float> progressCallBack = null);

    public bool IsContainScene(string location);
}
```

逐项说明（与源码一致）：

- **`CurrentMainSceneName`**  
  - 只读属性，返回当前被模块认为的「主场景」名称（注意是字符串名，不一定是 asset 路径）。

- **`LoadSceneAsync`（返回 `UniTask<Scene>`）**  
  - 异步方式加载场景，适合 `async/await` 调用。  
  - 参数：  
    - `location`：场景定位地址（交给 YooAssets 使用，具体是地址或路径由资源配置决定）。  
    - `sceneMode`：`Single` 或 `Additive`。  
    - `suspendLoad`：是否使用 YooAssets 的挂起加载（加载完但不激活，需要后续 `UnSuspend/ActivateScene`）。  
    - `priority`：加载优先级。  
    - `gcCollect`：仅对主场景有效，表示切主场景时是否执行垃圾回收。  
    - `progressCallBack`：进度回调（0~1）。  
  - 返回：加载完毕后的 `UnityEngine.SceneManagement.Scene` 对象。

- **`LoadScene`（回调式）**  
  - 提供回调式 API，不直接 `await`，用 `Action<Scene>` 通知完成。  
  - 逻辑上对应 `LoadSceneAsync` 的非 `async/await` 版本。

- **`ActivateScene`**  
  - 当有多个场景（主场景 + additive 场景）时，用于激活其中一个。  
  - 底层实际调用 YooAssets 返回的 `SceneHandle.ActivateScene()`。

- **`UnSuspend`**  
  - 针对 `suspendLoad = true` 的情况，解除挂起状态。  
  - 底层对应 `SceneHandle.UnSuspend()`。

- **`IsMainScene`**  
  - 判断给定 `location` 是否为「主场景」，同时还会结合当前 Unity 激活场景来判断。

- **`UnloadAsync / Unload`**  
  - **仅对子场景（Additive）** 卸载。  
  - 异步版返回 `UniTask<bool>`；回调版用 `Action` 通知完成。  
  - 都支持卸载进度回调。

- **`IsContainScene`**  
  - 判断某个 `location` 是否已经在当前模块管理的场景集合中（主场景或子场景）。

---

### 三、`SceneModule` 实现 —— 内部状态与关键逻辑（代码分析）

实现类位置：

```10:411:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
internal class SceneModule : Module, ISceneModule
{
    private string _currentMainSceneName = string.Empty;
    private SceneHandle _currentMainScene;
    private readonly Dictionary<string, SceneHandle> _subScenes = new Dictionary<string, SceneHandle>();
    private readonly HashSet<string> _handlingScene = new HashSet<string>();
    ...
}
```

#### 1. 内部字段与初始化/销毁

- **字段含义**：  
  - `_currentMainSceneName`：当前主场景名称（字符串）。  
  - `_currentMainScene`：当前主场景对应的 `SceneHandle`。  
  - `_subScenes`：子场景（Additive 场景）的 `location → SceneHandle` 映射。  
  - `_handlingScene`：记录当前正在执行「加载/卸载」操作的场景地址，用来防止重复操作。

- **`OnInit`**：

```25:29:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public override void OnInit()
{
    _currentMainScene = null;
    _currentMainSceneName = SceneManager.GetSceneByBuildIndex(0).name;
}
```

- 逻辑：初始化时认为 **BuildIndex=0 的场景** 是当前主场景，记录其名称。  
- 设计意图：  
  - 在游戏启动时，如果已经有一个主场景，就用它当作初始值，避免空值。  

- **`Shutdown`**：

```31:47:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public override void Shutdown()
{
    var iter = _subScenes.Values.GetEnumerator();
    while (iter.MoveNext())
    {
        SceneHandle subScene = iter.Current;
        if (subScene != null)
        {
            subScene.UnloadAsync();
        }
    }

    iter.Dispose();
    _subScenes.Clear();
    _handlingScene.Clear();
    _currentMainSceneName = string.Empty;
}
```

- 逻辑：  
  - 遍历所有子场景句柄，调用 `UnloadAsync()` 进行卸载（不关心完成）。  
  - 清空字典、清空处理中集合、清空主场景名。  
- 设计意图：  
  - 保证模块销毁时不会残留 additive 子场景。  
  - 不等待卸载完成，说明这个阶段通常发生在整体退出/切换流程中。

---

#### 2. 异步加载场景：`LoadSceneAsync`

```58:127:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public async UniTask<Scene> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false, uint priority = 100,
    bool gcCollect = true, Action<float> progressCallBack = null)
{
    if (!_handlingScene.Add(location))
    {
        Log.Error($"Could not load scene while loading. Scene: {location}");
        return default;
    }

    if (sceneMode == LoadSceneMode.Additive)
    {
        ...
    }
    else
    {
        ...
    }
}
```

- **并发保护**：  
  - 第一行就尝试 `_handlingScene.Add(location)`，如果失败，说明这个场景正在被加载/卸载，直接报错并返回 `default(Scene)`。  
  - 设计意图：**防止重复对同一场景发起加载**。

- **Additive 子场景分支**（简要关键逻辑）：

```67:95:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
if (sceneMode == LoadSceneMode.Additive)
{
    if (_subScenes.TryGetValue(location, out SceneHandle subScene))
    {
        throw new Exception($"Could not load subScene while already loaded. Scene: {location}");
    }

    subScene = YooAssets.LoadSceneAsync(location, sceneMode, LocalPhysicsMode.None, suspendLoad, priority);

    //Fix 这里前置，subScene.IsDone在UnSupendLoad之后才会是true
    _subScenes.Add(location, subScene);

    if (progressCallBack != null)
    {
        while (!subScene.IsDone && subScene.IsValid)
        {
            progressCallBack.Invoke(subScene.Progress);
            await UniTask.Yield();
        }
    }
    else
    {
        await subScene.ToUniTask();
    }

    _handlingScene.Remove(location);

    return subScene.SceneObject;
}
```

- 关键点与设计意图：  
  - **不允许重复加载同一子场景**：如果 `_subScenes` 中已经有对应 `location`，直接抛异常。  
  - **先加入 `_subScenes` 再等待**：注释说明原因：  
    - `subScene.IsDone` 要在 `UnSupendLoad` 之后才会变为 `true`；  
    - 但这里不等到完全激活才记录到 `_subScenes`，而是先记录句柄，这样后续才能通过 `UnSuspend` 等操作控制它。  
  - **进度回调实现**：  
    - 如果用户提供 `progressCallBack`，就自己轮询 `subScene.Progress` 并 `await UniTask.Yield()`；  
    - 否则直接 `await subScene.ToUniTask()`。  
  - **最后一定 `Remove`**：确保 `_handlingScene` 状态在成功结束后被清理。

- **主场景分支**：

```98:127:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
if (_currentMainScene is { IsDone: false })
{
    throw new Exception($"Could not load MainScene while loading. CurrentMainScene: {_currentMainSceneName}.");
}

_currentMainSceneName = location;

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
#if UNITY_EDITOR&&EditorFixedMaterialShader
    Utility.MaterialHelper.WaitGetRootGameObjects(_currentMainScene).Forget();
#endif
ModuleSystem.GetModule<IResourceModule>().ForceUnloadUnusedAssets(gcCollect);

_handlingScene.Remove(location);

return _currentMainScene.SceneObject;
```

- 关键点与设计意图：  
  - **主场景只允许串行切换**：  
    - 如果 `_currentMainScene` 存在且 `IsDone == false`，直接抛异常。  
  - **记录主场景名**：  
    - `_currentMainSceneName = location`，后续很多判断基于这个字段。  
  - **资源回收钩子**：  
    - 加载完成后调用 `IResourceModule.ForceUnloadUnusedAssets(gcCollect)`。  
    - `gcCollect` 参数控制是否同时执行 GC。  
    - 这是场景切换时释放旧资源的重要一环。  
  - **Editor 条件代码**：  
    - `Utility.MaterialHelper.WaitGetRootGameObjects(_currentMainScene).Forget();`  
    - 推测用于编辑器模式下修复 Shader / Material 等问题（从命名上推断，逻辑本身不在本文件）。

---

#### 3. 回调式加载：`LoadScene`

整体结构与 `LoadSceneAsync` 类似，只是换成「通过回调通知完成」。  

```140:199:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public void LoadScene(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false, uint priority = 100,
    Action<Scene> callBack = null,
    bool gcCollect = true, Action<float> progressCallBack = null)
{
    if (!_handlingScene.Add(location))
    {
        Log.Error($"Could not load scene while loading. Scene: {location}");
        return;
    }

    if (sceneMode == LoadSceneMode.Additive)
    {
        ...
    }
    else
    {
        ...
    }
}
```

- **Additive 分支**：  
  - 检查 `_subScenes` 是否已有该场景；已有则 `Log.Warning` 并 `return`。  
  - 调用 `YooAssets.LoadSceneAsync(...)`。  
  - 注册 `Completed` 回调：  
    - 完成时从 `_handlingScene` 移除标记，调用 `callBack(handle.SceneObject)`。  
  - 如有进度回调，则调用 `InvokeProgress(subScene, progressCallBack).Forget()`。  
  - 最后 `_subScenes.Add(location, subScene)`。

- **主场景分支**：  
  - 如果 `_currentMainScene` 还在 loading（`IsDone == false`） → 打 `Log.Warning`，直接 `return`。  
  - 赋值 `_currentMainSceneName`、调用 `YooAssets.LoadSceneAsync`。  
  - 注册 `Completed`：移除 `_handlingScene` 标记，执行 `callBack(Scene)`。  
  - 有进度回调则通过 `InvokeProgress`。  
  - 再次调用 `ForceUnloadUnusedAssets(gcCollect)` 做资源回收。  

- **`InvokeProgress` 辅助方法**：

```202:215:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
private async UniTaskVoid InvokeProgress(SceneHandle sceneHandle, Action<float> progress)
{
    if (sceneHandle == null)
    {
        return;
    }

    while (!sceneHandle.IsDone && sceneHandle.IsValid)
    {
        await UniTask.Yield();

        progress?.Invoke(sceneHandle.Progress);
    }
}
```

- 逻辑：  
  - 只要还没 `IsDone` 且 `IsValid`，就每帧刷新一次进度。  
- 设计意图：  
  - 把「轮询进度」这段模式从两个加载方法中抽出来重用。

---

#### 4. 激活、解除挂起：`ActivateScene / UnSuspend`

两者逻辑结构高度相似，只是调用的 `SceneHandle` 方法不同。

- **`ActivateScene`**：

```222:241:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public bool ActivateScene(string location)
{
    if (_currentMainSceneName.Equals(location))
    {
        if (_currentMainScene != null)
        {
            return _currentMainScene.ActivateScene();
        }

        return false;
    }

    _subScenes.TryGetValue(location, out SceneHandle subScene);
    if (subScene != null)
    {
        return subScene.ActivateScene();
    }

    Log.Warning($"IsMainScene invalid location:{location}");
    return false;
}
```

- 重点：  
  - 优先认为 `location` 有可能是主场景；  
  - 若不是主场景名，则在 `_subScenes` 里找；  
  - 找不到就 Warning 并返回 `false`。

- **`UnSuspend`**：

```249:268:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public bool UnSuspend(string location)
{
    if (_currentMainSceneName.Equals(location))
    {
        if (_currentMainScene != null)
        {
            return _currentMainScene.UnSuspend();
        }

        return false;
    }

    _subScenes.TryGetValue(location, out SceneHandle subScene);
    if (subScene != null)
    {
        return subScene.UnSuspend();
    }

    Log.Warning($"IsMainScene invalid location:{location}");
    return false;
}
```

- 设计意图：  
  - 将 YooAssets 提供的 `ActivateScene / UnSuspend` 封装成统一的接口，并根据「主场景 / 子场景」自动分发到对应句柄。

---

#### 5. 主场景判断：`IsMainScene`

```276:305:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public bool IsMainScene(string location)
{
    // 获取当前激活的场景  
    Scene currentScene = SceneManager.GetActiveScene();

    if (_currentMainSceneName.Equals(location))
    {
        if (_currentMainScene == null)
        {
            return false;
        }

        // 判断当前场景是否是主场景  
        if (currentScene.name == _currentMainScene.SceneName)
        {
            return true;
        }

        return _currentMainScene.SceneName == currentScene.name;
    }

    // 判断当前场景是否是主场景  
    if (currentScene.name == _currentMainScene?.SceneName)
    {
        return true;
    }

    Log.Warning($"IsMainScene invalid location:{location}");
    return false;
}
```

- 实际行为：  
  - 如果 `location == _currentMainSceneName`：  
    - 先看 `_currentMainScene` 是否存在；  
    - 再判断当前激活场景名是否等于 `_currentMainScene.SceneName`。  
  - 如果 `location != _currentMainSceneName`：  
    - 仍然只判断「当前激活场景是否等于 `_currentMainScene.SceneName`」。  
    - 若是，则直接返回 `true`，并不关心 `location`。  
    - 否则 Warning 并返回 `false`。  
- 也就是说：**这个方法本质上是在判断「当前激活的场景是不是模块记录的主场景」，而不是严格比较 `location` 对应哪个场景。**

---

#### 6. 卸载子场景：`UnloadAsync / Unload`

- **`UnloadAsync`**（异步等待版）：

```312:353:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public async UniTask<bool> UnloadAsync(string location, Action<float> progressCallBack = null)
{
    _subScenes.TryGetValue(location, out SceneHandle subScene);
    if (subScene != null)
    {
        if (subScene.SceneObject == default)
        {
            Log.Error($"Could not unload Scene while not loaded. Scene: {location}");
            return false;
        }

        if (!_handlingScene.Add(location))
        {
            Log.Warning($"Could not unload Scene while loading. Scene: {location}");
            return false;
        }

        var unloadOperation = subScene.UnloadAsync();

        if (progressCallBack != null)
        {
            while (!unloadOperation.IsDone && unloadOperation.Status != EOperationStatus.Failed)
            {
                progressCallBack.Invoke(unloadOperation.Progress);
                await UniTask.Yield();
            }
        }
        else
        {
            await unloadOperation.ToUniTask();
        }

        _subScenes.Remove(location);

        _handlingScene.Remove(location);

        return true;
    }

    Log.Warning($"UnloadAsync invalid location:{location}");
    return false;
}
```

- 关键点：  
  - 只能卸载 `_subScenes` 中存在的场景（即被视为子场景）。  
  - 要求 `subScene.SceneObject != default`，否则表示其实没真正加载成功。  
  - 再次使用 `_handlingScene` 防止并发卸载。  
  - 对 `unloadOperation` 提供进度回调轮询。  
  - 成功卸载后，从 `_subScenes` 和 `_handlingScene` 中都移除。

- **`Unload`**（回调版）：

```361:395:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public void Unload(string location, Action callBack = null, Action<float> progressCallBack = null)
{
    _subScenes.TryGetValue(location, out SceneHandle subScene);
    if (subScene != null)
    {
        if (subScene.SceneObject == default)
        {
            Log.Error($"Could not unload Scene while not loaded. Scene: {location}");
            return;
        }

        if (!_handlingScene.Add(location))
        {
            Log.Warning($"Could not unload Scene while loading. Scene: {location}");
            return;
        }

        subScene.UnloadAsync();
        subScene.UnloadAsync().Completed += @base =>
        {
            _subScenes.Remove(location);
            _handlingScene.Remove(location);
            callBack?.Invoke();
        };

        if (progressCallBack != null)
        {
            InvokeProgress(subScene, progressCallBack).Forget();
        }

        return;
    }

    Log.Warning($"UnloadAsync invalid location:{location}");
}
```

- 关键点：  
  - 与异步版逻辑一致，只是通过 `Completed` 回调通知。  
  - 这里对 `UnloadAsync` 调用了两次（一次 fire-and-forget，一次再注册 Completed）；这是实现上的小冗余，但行为上仍然符合「卸载后回调」。  

---

#### 7. 场景是否存在：`IsContainScene`

```402:410:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public bool IsContainScene(string location)
{
    if (_currentMainSceneName.Equals(location))
    {
        return true;
    }

    return _subScenes.TryGetValue(location, out var _);
}
```

- 逻辑非常直接：  
  - 如果 `location` 等于当前主场景名，返回 `true`；  
  - 否则查看 `_subScenes` 字典中是否存在对应 key。

---

### 四、项目中现有的使用入口（代码分析）

从整个仓库中搜索，**唯一直接引用 `ISceneModule` 的地方**是 `GameModule` 的静态属性：  

```68:72:UnityProject/Assets/GameScripts/HotFix/GameLogic/GameModule.cs
/// <summary>
/// 获取场景模块。
/// </summary>
public static ISceneModule Scene => _scene ??= Get<ISceneModule>();

private static ISceneModule _scene;
```

- 设计意图：  
  - 为 HotFix/GameLogic 层提供一个统一入口：`GameModule.Scene`。  
  - 业务代码不直接依赖 `ModuleSystem` 或 `SceneModule` 实现，而是依赖 `ISceneModule` 接口。  

我在当前 C# 代码中 **没有找到对 `GameModule.Scene` 的直接调用示例**（可能在其他脚本语言或未提交部分中有用例）。因此，下面的使用示例将是我基于接口定义给出的**示例代码，不在当前项目源码中**。

---

### 五、使用示例（简化版示例代码，不在当前项目源码中）

> 下面所有代码都是**使用方式示例**，用于说明接口的典型用法，**不是当前仓库里的真实代码**。

#### 示例 1：切换主场景（await 异步版）

```csharp
// 示例代码：演示如何切换主场景（不在当前项目源码中）
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using TEngine;

public class SceneSwitcher
{
    public async UniTask SwitchToBattleSceneAsync()
    {
        // location 通常是 YooAssets 配置的场景地址，如 "Scenes/Battle"
        string location = "Scenes/Battle";

        // 切换主场景，加载完成后执行 GC（gcCollect = true）
        Scene scene = await GameModule.Scene.LoadSceneAsync(
            location,
            sceneMode: LoadSceneMode.Single,
            suspendLoad: false,
            priority: 100,
            gcCollect: true,
            progressCallBack: progress =>
            {
                Log.Info($"Loading battle scene progress: {progress:P0}");
            });

        Log.Info($"Battle scene loaded: {scene.name}");
    }
}
```

要点对应源码：

- 使用 `GameModule.Scene` 获取 `ISceneModule`。  
- 通过 `LoadSceneAsync` 切换主场景，`gcCollect` 会触发 `IResourceModule.ForceUnloadUnusedAssets`。  
- 使用 `progressCallBack` 对应 `LoadSceneAsync` 内部的进度轮询逻辑。

---

#### 示例 2：加载/卸载一个 Additive 子场景

```csharp
// 示例代码：加载一个 Additive 子场景，并在需要时卸载（不在当前项目源码中）
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using TEngine;

public class SubSceneController
{
    private const string SubSceneLocation = "Scenes/Environment";

    public async UniTask LoadEnvironmentAsync()
    {
        // 以 Additive 模式加载，作为子场景
        await GameModule.Scene.LoadSceneAsync(
            SubSceneLocation,
            sceneMode: LoadSceneMode.Additive,
            suspendLoad: false,
            priority: 50,
            gcCollect: false,
            progressCallBack: p => Log.Info($"Env scene loading: {p:P0}"));

        // 此时 IsContainScene 应该为 true
        bool contains = GameModule.Scene.IsContainScene(SubSceneLocation);
        Log.Info($"Environment loaded, contains: {contains}");
    }

    public async UniTask UnloadEnvironmentAsync()
    {
        // 卸载子场景（只针对 Additive 场景）
        bool result = await GameModule.Scene.UnloadAsync(
            SubSceneLocation,
            progressCallBack: p => Log.Info($"Env scene unloading: {p:P0}"));

        Log.Info($"Environment unload result: {result}");
    }
}
```

- 对应源码：  
  - Additive 分支使用 `_subScenes` 记录句柄。  
  - `UnloadAsync` 只对 `_subScenes` 中的场景起作用。

---

#### 示例 3：挂起加载 + 手动激活（suspendLoad + UnSuspend / ActivateScene）

```csharp
// 示例代码：使用挂起加载，然后手动激活（不在当前项目源码中）
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using TEngine;

public class SuspendedSceneLoader
{
    private const string SuspendedLocation = "Scenes/BigWorld";

    public async UniTask PreloadBigWorldAsync()
    {
        // 挂起加载：加载资源但不立即激活场景
        await GameModule.Scene.LoadSceneAsync(
            SuspendedLocation,
            sceneMode: LoadSceneMode.Single,
            suspendLoad: true,
            priority: 100,
            gcCollect: false,
            progressCallBack: p => Log.Info($"Preload big world: {p:P0}"));

        Log.Info("BigWorld scene preloaded (suspended).");
    }

    public void ActivateBigWorld()
    {
        // 解除挂起（YooAssets UnSuspend）
        bool unsuspendOk = GameModule.Scene.UnSuspend(SuspendedLocation);
        if (!unsuspendOk)
        {
            Log.Warning("UnSuspend BigWorld failed.");
        }

        // 激活场景
        bool activateOk = GameModule.Scene.ActivateScene(SuspendedLocation);
        Log.Info($"Activate BigWorld result: {activateOk}");
    }
}
```

- 对应源码：  
  - `suspendLoad = true` 时，加载完成但不自动激活。  
  - 之后通过 `UnSuspend(location)` 和 `ActivateScene(location)` 驱动 YooAssets 的对应行为。

---

### 六、小结（架构与设计意图）

- **架构上**：  
  - `ISceneModule` 统一抽象「场景管理能力」，`SceneModule` 作为内部实现，挂在框架模块系统中。  
  - 对业务层暴露的是 `GameModule.Scene`，实现了**接口+模块化+全局访问**的组合模式。  

- **设计意图上**：  
  - 明确区分主场景与子场景，分别用专门字段与字典管理。  
  - 用 `_handlingScene` 防止同一个场景被重复加载/卸载或在加载中被再次操作。  
  - 深度集成 YooAssets 场景系统（SceneHandle），并在主场景切换时联动 `IResourceModule` 做资源回收。  
  - 同时提供 `async/await` 风格 (`UniTask`) 和回调风格两套 API，方便在不同调用场景中使用。  

如果你希望，我可以在下一步帮你基于当前 `SceneModule` 的设计，**设计一套「高层流程」接口（比如：关卡管理流程/主菜单—游戏—结算场景切换封装）**，但会严格基于现有模块，不会随意改变你的架构。

===== User 3 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\README.md (total lines: 104)

Files that are currently open and visible in the user's IDE:
- c:\Users\angus\.cursor\projects\d-workspace-unity-TEngine\terminals\1.txt (total lines: 267)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
await UniTask.yield()和await subscene.tounitask是什么含义，有什么作用？
</user_query>

===== Assistant 4 =====
我是 GPT-5.1 模型。  

### 1. `await UniTask.Yield()` 是什么？

结合你代码里的用法（比如）：

```csharp
while (!subScene.IsDone && subScene.IsValid)
{
    progressCallBack.Invoke(subScene.Progress);
    await UniTask.Yield();
}
```

**含义与作用：**

- **含义**：  
  - `UniTask.Yield()` 表示：**马上把控制权还给当前帧/当前线程，让协程在「下一帧」继续从这里往下执行**。  
  - `await` 之后，这一帧的后续逻辑就先停下，等到下一帧再从这行后继续。

- **在这里的作用**：  
  - 这段 `while` 是用来「**一帧一帧轮询加载进度**」的：  
    - 每帧调用一次 `progressCallBack(subScene.Progress)`，更新 UI 进度条之类。  
    - `await UniTask.Yield()` 避免在一个帧里死循环把 CPU 卡死，而是 **每帧只跑一次循环**。  
  - 换句话说：  
    - 它**并不等待场景加载完成**，只等待「下一帧再继续检查进度」。  

---

### 2. `await subScene.ToUniTask()` 是什么？

对应代码（无进度回调时的分支）：

```csharp
if (progressCallBack != null)
{
    // 自己 while + Yield 轮询
}
else
{
    await subScene.ToUniTask();
}
```

**含义与作用：**

- **含义**：  
  - `subScene` 是一个 `SceneHandle`（YooAssets 的异步操作句柄）。  
  - `ToUniTask()` 会把这个「场景加载句柄」**包装成一个 `UniTask`**：  
    - 这个 `UniTask` 会在 **场景真正加载完成（`IsDone == true`）** 时才结束。  
  - `await subScene.ToUniTask()` = **一直等到这个场景加载完再往下执行**，中途不自己手动轮询进度。

- **在这里的作用**：  
  - 当你**不关心进度，只关心“什么时候加载完”** 时，直接 `await subScene.ToUniTask()`：  
    - 代码简单；  
    - 框架内部会自己在合适的时机调度，不需要你一帧一帧 `Yield`。  
  - 和上面的 `while + Yield` 相比：  
    - **`while + Yield`**：自己控制每帧轮询，用来更新进度条。  
    - **`ToUniTask`**：交给框架，**直到加载完成才返回**，过程对你是「黑盒等待」。

---

### 3. 简单对比总结

- **`await UniTask.Yield()`：**
  - 等「一小步」（通常是到下一帧）。  
  - 用来**拆帧执行**、避免卡死，同时实现**逐帧逻辑**（比如每帧更新进度）。  
  - 在你的代码中，是在「加载**过程中**」一帧一帧刷新进度。

- **`await subScene.ToUniTask()`：**
  - 等「整个异步操作完成」（场景加载结束）。  
  - 用来**直到操作完成再继续**，不管中途经历了多少帧。  
  - 在你的代码中，是在「加载**结束点**」才继续执行后续逻辑。

如果你愿意，我可以基于一个具体的「进度条 UI」示例，把这两种写法放在一个完整脚本里对比展示（仍然不会改你现有架构）。

===== User 4 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\README.md (total lines: 104)

Files that are currently open and visible in the user's IDE:
- c:\Users\angus\.cursor\projects\d-workspace-unity-TEngine\terminals\1.txt (total lines: 267)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
activatescene和 unsuspend 是什么函数，有什么作用？
</user_query>

===== Assistant 5 =====
我来基于当前项目真实源码，先找到 `SceneHandle` 的定义和 `ActivateScene/UnSuspend` 的实现，然后结合你 `SceneModule` 的用法，解释它们到底做了什么、在什么场景下调用。

我是 GPT-5.1 模型。  

下面全部基于你项目里 YooAsset 的真实代码来解释。

---

### 1. 这两个函数在哪里？

`SceneModule` 里调用的是 `SceneHandle` 的方法，真实代码在：  

```5:138:UnityProject/Packages/YooAsset/Runtime/ResourceManager/Handle/SceneHandle.cs
public class SceneHandle : HandleBase
{
    ...
    /// <summary>
    /// 激活场景（当同时存在多个场景时用于切换激活场景）
    /// </summary>
    public bool ActivateScene()
    {
        ...
        if (SceneObject.IsValid() && SceneObject.isLoaded)
        {
            return SceneManager.SetActiveScene(SceneObject);
        }
        else
        {
            YooLogger.Warning($"Scene is invalid or not loaded : {SceneObject.name}");
            return false;
        }
    }

    /// <summary>
    /// 解除场景加载挂起操作
    /// </summary>
    public bool UnSuspend()
    {
        if (IsValidWithWarning == false)
            return false;

        if (Provider is SceneProvider)
        {
            var provider = Provider as SceneProvider;
            provider.UnSuspendLoad();
        }
        else
        {
            throw new System.NotImplementedException();
        }
        return true;
    }
    ...
}
```

---

### 2. `ActivateScene()` 是什么，有什么作用？

**源码关键逻辑：**

```79:92:UnityProject/Packages/YooAsset/Runtime/ResourceManager/Handle/SceneHandle.cs
public bool ActivateScene()
{
    if (IsValidWithWarning == false)
        return false;

    if (SceneObject.IsValid() && SceneObject.isLoaded)
    {
        return SceneManager.SetActiveScene(SceneObject);
    }
    else
    {
        YooLogger.Warning($"Scene is invalid or not loaded : {SceneObject.name}");
        return false;
    }
}
```

**含义：**

- 它是 YooAsset 对 Unity 场景激活的一个封装。  
- 本质上是调用 `SceneManager.SetActiveScene(SceneObject)`。  
- 只有当：
  - 句柄有效（`IsValidWithWarning` 为 true）
  - `SceneObject`（对应的 `UnityEngine.SceneManagement.Scene`）**有效且已经加载** (`IsValid()` & `isLoaded`)  
  时，才会去切换当前激活场景。

**作用：**

- 在同时存在多个已加载场景（例如主场景 + 多个 Additive 子场景）时，  
  - `ActivateScene()` 用来**把这个 handle 对应的场景设为 Unity 的“激活场景”**。  
- 「激活场景」意味着：
  - `SceneManager.GetActiveScene()` 会返回这个场景；  
  - 新实例化的对象（不指定父）通常会落在激活场景中；  
  - 某些系统或逻辑会依赖当前激活场景。

**在 `SceneModule` 中的使用：**

```222:241:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public bool ActivateScene(string location)
{
    if (_currentMainSceneName.Equals(location))
    {
        if (_currentMainScene != null)
        {
            return _currentMainScene.ActivateScene();
        }
        return false;
    }

    _subScenes.TryGetValue(location, out SceneHandle subScene);
    if (subScene != null)
    {
        return subScene.ActivateScene();
    }

    Log.Warning($"IsMainScene invalid location:{location}");
    return false;
}
```

- 场景模块根据 `location` 找到对应的 `SceneHandle`（主场景或子场景），再调它的 `ActivateScene()`。  
- 设计意图：**你只关心 “按地址激活哪个场景”**，具体怎么调用 `SetActiveScene` 由 YooAsset 处理。

---

### 3. `UnSuspend()` 是什么，有什么作用？

**源码关键逻辑：**

```98:113:UnityProject/Packages/YooAsset/Runtime/ResourceManager/Handle/SceneHandle.cs
public bool UnSuspend()
{
    if (IsValidWithWarning == false)
        return false;

    if (Provider is SceneProvider)
    {
        var provider = Provider as SceneProvider;
        provider.UnSuspendLoad();
    }
    else
    {
        throw new System.NotImplementedException();
    }
    return true;
}
```

**含义：**

- YooAsset 支持场景的「**挂起加载（suspendLoad）**」：  
  - 在调用 `YooAssets.LoadSceneAsync(location, ..., suspendLoad: true, ...)` 时，场景会被加载到一定阶段但**不会直接完成/激活**，相当于暂停在中间状态。  
- `UnSuspend()` 的作用就是：  
  - 通知底层的 `SceneProvider` 调用 `UnSuspendLoad()`，**解除这个“挂起状态”**，让加载流程继续走到完成（并最终让场景真正加载好/可用）。

**作用场景：**

- 一般的流程是：  
  1. 先调用带 `suspendLoad = true` 的 `LoadSceneAsync` / `LoadScene`，预加载场景。  
  2. 在合适的时间点（比如：过完过场动画、数据准备好）再调用 `UnSuspend()`。  
  3. 必要时再配合 `ActivateScene()` 把它设为当前激活场景。

**在 `SceneModule` 中的使用：**

```249:268:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
public bool UnSuspend(string location)
{
    if (_currentMainSceneName.Equals(location))
    {
        if (_currentMainScene != null)
        {
            return _currentMainScene.UnSuspend();
        }
        return false;
    }

    _subScenes.TryGetValue(location, out SceneHandle subScene);
    if (subScene != null)
    {
        return subScene.UnSuspend();
    }

    Log.Warning($"IsMainScene invalid location:{location}");
    return false;
}
```

- 场景模块根据 `location` 找到对应的 `SceneHandle`，然后调用 `UnSuspend()`。  
- 设计意图：  
  - 在接口层只暴露 `ISceneModule.UnSuspend(string location)`，不用暴露 YooAsset 的细节。  
  - 你只管传入「场景地址」，模块会在对应的 `SceneHandle` 上解挂。

---

### 4. 小结对比（配合前一个问题一起理解）

综合你之前问的 `suspendLoad` / `UniTask.Yield` / `ToUniTask`，可以这样理解这两个函数在整体流程中的角色：

- **`LoadSceneAsync(... suspendLoad: true ...)`**  
  - 启动一个「会在中途暂停」的加载流程；  
  - 场景资源会被加载到一定阶段，但不会立刻完全完成/激活。

- **`UnSuspend()`**  
  - 解除这个暂停，**让加载继续跑完**。  
  - 不负责切换激活场景，只是让这个场景真正准备好。

- **`ActivateScene()`**  
  - 把已经加载好的场景（`SceneObject` 有效且 `isLoaded`）设置成 Unity 的**当前激活场景**。  
  - 影响 `SceneManager.GetActiveScene()` 结果以及后续对象创建的默认场景。

如果你接下来想实现一个「预加载+黑屏过场+切场景」的小流程，我可以根据你当前 `SceneModule` 的 API，用一个完整但简洁的伪代码流程帮你把这几个函数串起来。

===== User 5 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\README.md (total lines: 104)

Files that are currently open and visible in the user's IDE:
- c:\Users\angus\.cursor\projects\d-workspace-unity-TEngine\terminals\1.txt (total lines: 267)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
好的，帮我真实的代码 把几个函数穿起来
</user_query>

===== Assistant 6 =====
我是 GPT-5.1 模型。  

下面给你一段 **真实可直接放进项目里的 C# 脚本**，把这几个函数串在一起使用：  
- `LoadSceneAsync` + `suspendLoad`  
- `UnSuspend`  
- `ActivateScene`  
- `UnloadAsync`（卸载 Additive 子场景）  

> 说明：这是**新增脚本**示例，不修改你现有源码，代码风格尽量贴近你项目现有写法。

---

### 场景流程示例脚本（真实代码）

你可以把这个脚本放到 `UnityProject/Assets/GameScripts/` 里，比如命名为 `SceneFlowController.cs`，挂在一个常驻物体上使用。

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

### 关键点对应关系（和你现有代码一一对应）

- **预加载主场景**  
  - 调用：`GameModule.Scene.LoadSceneAsync(... suspendLoad: true ...)`  
  - 对应你 `SceneModule.LoadSceneAsync` 主场景分支 + `suspendLoad` 参数。

- **解除挂起**  
  - 调用：`GameModule.Scene.UnSuspend(mainSceneLocation)`  
  - 内部对应：`SceneHandle.UnSuspend()` → `SceneProvider.UnSuspendLoad()`。

- **激活场景**  
  - 调用：`GameModule.Scene.ActivateScene(mainSceneLocation)`  
  - 内部对应：`SceneHandle.ActivateScene()` → `SceneManager.SetActiveScene(SceneObject)`。

- **加载 Additive 子场景**  
  - 调用：`GameModule.Scene.LoadSceneAsync(... LoadSceneMode.Additive ...)`  
  - 内部使用 `_subScenes` 字典管理多个子场景。

- **卸载子场景**  
  - 调用：`GameModule.Scene.UnloadAsync(subSceneLocation, ...)`  
  - 对应 `SceneModule.UnloadAsync` 对 `_subScenes` 的处理逻辑。

如果你希望下一步，我可以再给你一个「只针对主菜单 → 游戏 → 结算」的更精简版本，把上面脚本裁剪成专门的关卡切换管理器，但会继续严格使用你现有的 `ISceneModule` API，不会改动框架设计。

===== User 6 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\README.md (total lines: 104)

Files that are currently open and visible in the user's IDE:
- c:\Users\angus\.cursor\projects\d-workspace-unity-TEngine\terminals\1.txt (total lines: 267)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
subScene.SceneObject != default，否则表示其实没真正加载成功。为什么==default 表示没有真正加载成功
</user_query>

===== Assistant 7 =====
我是 GPT-5.1 模型。  

### 1. 先看真实代码里是怎么写的

在 `SceneModule.UnloadAsync` 中有这一段：  

```312:321:UnityProject/Assets/TEngine/Runtime/Module/SceneModule/SceneModule.cs
_subScenes.TryGetValue(location, out SceneHandle subScene);
if (subScene != null)
{
    if (subScene.SceneObject == default)
    {
        Log.Error($"Could not unload Scene while not loaded. Scene: {location}");
        return false;
    }
    ...
}
```

`SceneObject` 属性的实现在 `SceneHandle` 里：  

```66:73:UnityProject/Packages/YooAsset/Runtime/ResourceManager/Handle/SceneHandle.cs
public Scene SceneObject
{
    get
    {
        if (IsValidWithWarning == false)
            return new Scene();
        return Provider.SceneObject;
    }
}
```

这里的 `Scene` 是 `UnityEngine.SceneManagement.Scene`（一个 **struct 结构体**）。

---

### 2. 为什么 `SceneObject == default` 表示「没有真正加载成功」？

**关键点有两个：**

1. **`Scene` 是结构体，`default(Scene)` 就是一个“空场景”**  
   - 在 C# 里，`default(Scene)` 等价于 `new Scene()`，内部的句柄是 0。  
   - 这个场景：  
     - `IsValid()` 为 `false`  
     - 不对应任何已加载的 Unity 场景。  
   - 所以它代表「**未初始化/无效场景**」。

2. **`SceneHandle.SceneObject` 的返回策略**  
   - 如果句柄无效 (`IsValidWithWarning == false`)，直接返回 `new Scene()`，也就是 **默认值**。  
   - 在正常加载流程中，只有当场景真正加载成功后，`Provider.SceneObject` 才会被赋成一个有效的 `Scene`。  
   - 换句话说：  
     - **还没加载成功 / 句柄无效 → `SceneObject` 为默认值（空场景）**  
     - **加载成功且句柄有效 → `SceneObject` 为 Unity 里真实存在的场景对象**

因此，`subScene.SceneObject == default` 这句判断的含义就是：  

> 这个 `SceneHandle` 当前拿到的只是一个「空的默认 Scene」，  
> 说明**要么场景还没真正加载好，要么句柄无效**，  
> 所以此时调用卸载没有意义（也不安全），直接报错并返回。

---

### 3. 直观理解

- 你可以把 `Scene` 想成一个「场景句柄结构体」，  
- **默认值（`default(Scene)`） = 没指向任何场景**，就像 `null` 之于引用类型。  
- 所以检查 `== default`，本质是在问：  
  - 「这个场景句柄有没有真正指向一个已经加载好的场景？」  

如果你想，我可以再帮你把 `Scene.IsValid()` 的用法和 `SceneManager.GetActiveScene()` 的关系也串一下，让你更清楚什么时候一个 `Scene` 被认为是“真正存在”的。

===== User 7 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\README.md (total lines: 104)

Files that are currently open and visible in the user's IDE:
- c:\Users\angus\.cursor\projects\d-workspace-unity-TEngine\terminals\1.txt (total lines: 267)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
请把这个会话的内容保存下来
1、一字不差的保留下来，去个合适的名字，放到合适的目录
2、把summary保存下来，关键的sample代码一定要加上。
文件名英文，内容中文
</user_query>

===== User 8 =====
<user_info>
OS Version: win32 10.0.26200

Shell: powershell

Workspace Path: d:\workspace\unity\TEngine

Is directory a git repo: Yes, at D:/workspace/unity/TEngine

Today's date: Friday Dec 12, 2025

Terminals folder: C:\Users\angus\.cursor\projects\d-workspace-unity-TEngine/terminals
</user_info>

<git_status>
This is the git status at the start of the conversation. Note that this status is a snapshot in time, and will not update during the conversation.


Git repo: D:/workspace/unity/TEngine

## main...origin/main
 D UnityProject/UserSettings/Layouts/CurrentMaximizeLayout.dwlt
 M UnityProject/UserSettings/Layouts/default-2022.dwlt
?? UnityProject/Assets/TEngine/ResourceModule_QA_full_conversation.md.meta
?? UnityProject/Assets/TEngine/SceneModule_QA_full_conversation.md.meta
?? UnityProject/Assets/TEngine/SceneModule_QA_summary.md.meta
?? UnityProject/UserSettings/Search.index
</git_status>

<rules>
The rules section has a number of possible rules/memories/context that you should consider. In each subsection, we provide instructions about what information the subsection contains and how you should consider/follow the contents of the subsection.


<user_rules description="These are rules set by the user that you should follow if appropriate.">
<user_rule>如果我让你分析代码的时候，需要做到以下几点：
1、直接分析真实代码：
"请基于我提供的实际代码进行分析"
"不要简化或重写代码"
2、保持准确性：
"请确保解释与真实代码完全一致"
"如果代码复杂，请逐步解释，但不要改变原始逻辑"
3、明确标注：
"如果需要简化说明，请明确标注这是简化版本"
"区分概念解释和代码分析"
4、指出关键实现细节并解释设计意图</user_rule>

<user_rule>Always respond in Chinese-simplified.
Always start your answer by telling me what AI model you are using.
Do not arbitrarily expand or modify my ideas.
Do not write test code.
if it is a Unity Project , make sure the Unity version is 2022.3.61f1c1.</user_rule>
</user_rules>
</rules>

===== Assistant 8 =====
我是 GPT-5.1 模型。

**结论：**  
`_asyncOperation = SceneManager.LoadSceneAsync(_assetInfo.AssetPath, _loadParams);` 这一行本身**不会“停下来”阻塞主线程或卡在这里不动**，它会立刻返回一个 `AsyncOperation` 对象，然后后续代码继续执行，场景加载过程由 Unity 在后台异步完成。

### 结合你这段真实代码具体说明

这段逻辑里（你给的 `22-116` 行）主要流程是：

- 当不是 `IsWaitForAsyncComplete` 时，走异步分支：

```csharp
_asyncOperation = SceneManager.LoadSceneAsync(_assetInfo.AssetPath, _loadParams);
if (_asyncOperation != null)
{
    _asyncOperation.allowSceneActivation = !_suspendLoad;
    _asyncOperation.priority = 100;
    Result = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
    _steps = ESteps.CheckResult;
}
```

- **关键点：**

  - `LoadSceneAsync` 是 Unity 引擎的**异步加载接口**：  
    - 调用时只是在这一帧**发起加载请求**，马上返回一个 `AsyncOperation`。  
    - 真正的磁盘读取、资源准备在后续若干帧中由引擎自己完成。
  - 你这个 `InternalUpdate()` 在同一帧里马上继续往下走到：

    ```csharp
    if (_steps == ESteps.CheckResult)
    {
        if (_asyncOperation != null)
        {
            ...
            Progress = _asyncOperation.progress;
            if (_asyncOperation.isDone == false)
                return;
        }
        ...
    }
    ```

    - 这里会读 `progress`，发现 `isDone == false` 就 `return`，**这一帧结束**，什么也不阻塞。
    - 之后由 YooAsset 的更新循环在**后续帧再次调用 `InternalUpdate`**，每帧检查一次进度，直到 `isDone == true`。

- 只有在 `IsWaitForAsyncComplete == true` 时，它才走同步分支：

```csharp
Result = SceneManager.LoadScene(_assetInfo.AssetPath, _loadParams);
_steps = ESteps.CheckResult;
```

并且你在 `CheckResult` 里也写了注释：

```csharp
if (IsWaitForAsyncComplete)
{
    //注意：场景加载无法强制异步转同步
    YooLogger.Error("The scene is loading asyn !");
}
```

说明这里的设计就是：  
- **不支持把异步场景加载“强制转为同步等待”**，  
- `InternalWaitForAsyncComplete()` 也只是再次调用了一次 `InternalUpdate()`，没有 while 循环阻塞当前线程。

### 回答“会不会停下来？”

如果你说的“停下来”指的是：

- **阻塞当前执行线程 / 卡死这一帧 / 像同步加载那样长时间卡顿在这一行：**  
  - **不会。**`LoadSceneAsync` 会立刻返回，`InternalUpdate` 这一帧执行完就返回，由 Unity 在后台继续加载。

如果你说的是：

- **整个操作对象 `_steps` 会不会在某个状态上“等着加载完成”再继续：**  
  - 是的，逻辑上会在 `ESteps.CheckResult` 状态上「等待」，  
  - 但这个等待是通过**每帧轮询 `_asyncOperation.isDone`** 实现的，**不是这行代码阻塞**，而是靠后续多次调用 `InternalUpdate()` 慢慢推进。

如需，我可以再帮你画一张这个 `AssetBundleLoadSceneOperation` 的状态流转时序图，专门解释 `suspendLoad`、`UnSuspendLoad()` 和 `allowSceneActivation` 的关系。

===== User 9 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Packages\YooAsset\Runtime\FileSystem\BundleResult\AssetBundleResult\Operation\AssetBundleLoadSceneOperation.cs (total lines: 117)
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\Runtime\Module\SceneModule\SceneModule.cs (total lines: 413)

Files that are currently open and visible in the user's IDE:
- d:\workspace\unity\TEngine\UnityProject\Packages\YooAsset\Runtime\FileSystem\BundleResult\AssetBundleResult\Operation\AssetBundleLoadSceneOperation.cs (currently focused file, cursor is on line 116, total lines: 117)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
这个函数，如果checkresult 不满足条件return后，operationsystem再次调用internalupdate后，会从哪里开始执行？
</user_query>

===== Assistant 9 =====
我是 GPT-5.1 模型。

**关键点：** `InternalUpdate()` 每次被 OperationSystem 调用时，都是**从函数开头重新执行一遍整段代码**，并不是从上次 `return` 的地方“继续往下跑”。真正让它“接着上次状态往下走”的，是类里面的成员变量，尤其是 `_steps` 和 `_asyncOperation`。

### 结合你这段代码，按时间顺序说明

1. **第一次调用 `InternalUpdate()`**（假设是异步加载分支）  

   - 进来先判断：

     ```csharp
     if (_steps == ESteps.None || _steps == ESteps.Done)
         return;
     ```

     一开始 `_steps` 是 `None`，但你在 `InternalStart()` 里已经把它设成了 `LoadScene`：

     ```csharp
     _steps = ESteps.LoadScene;
     ```

   - 因为 `_steps == ESteps.LoadScene`，会进入：

     ```csharp
     if (_steps == ESteps.LoadScene)
     {
         // 走到 LoadSceneAsync 分支
         _asyncOperation = SceneManager.LoadSceneAsync(_assetInfo.AssetPath, _loadParams);
         if (_asyncOperation != null)
         {
             _asyncOperation.allowSceneActivation = !_suspendLoad;
             _asyncOperation.priority = 100;
             Result = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
             _steps = ESteps.CheckResult;
         }
         else
         {
             // 失败流程，设 Failed
         }
     }
     ```

   - 这一帧结束时的状态大致是：
     - `_steps == ESteps.CheckResult`
     - `_asyncOperation != null`
     - 加载在引擎那边后台进行中

   - 然后继续往下执行 `if (_steps == ESteps.CheckResult)` 这一段，进入到：

     ```csharp
     if (_steps == ESteps.CheckResult)
     {
         if (_asyncOperation != null)
         {
             ...
             Progress = _asyncOperation.progress;
             if (_asyncOperation.isDone == false)
                 return;
         }
         ...
     }
     ```

   - **此时如果 `isDone == false`，函数 `return`，这次调用结束。**

2. **下一帧 / 下一次 OperationSystem 再次调用 `InternalUpdate()` 时，从哪开始？**

   - 调用入口仍然是整个方法的开头，也就是再次从：

     ```csharp
     internal override void InternalUpdate()
     {
         if (_steps == ESteps.None || _steps == ESteps.Done)
             return;
     
         if (_steps == ESteps.LoadScene)
         {
             ...
         }
     
         if (_steps == ESteps.CheckResult)
         {
             ...
         }
     }
     ```

     **从第一行重新执行。**C# 方法本身没有“记住上次停在哪行”的机制。

   - 但这一次，成员变量的状态是：
     - `_steps == ESteps.CheckResult`
     - `_asyncOperation != null`
   - 所以执行流程是：
     1. 先跑 `if (_steps == ESteps.None || _steps == ESteps.Done)`，条件不满足，不 return。
     2. 跑 `if (_steps == ESteps.LoadScene)`，因为现在是 `CheckResult`，**不会进 `LoadScene` 这块**。
     3. 跑到 `if (_steps == ESteps.CheckResult)`，条件满足，**直接从 CheckResult 这段逻辑开始工作**：

        ```csharp
        if (_steps == ESteps.CheckResult)
        {
            if (_asyncOperation != null)
            {
                ...
                Progress = _asyncOperation.progress;
                if (_asyncOperation.isDone == false)
                    return;
            }

            if (Result.IsValid())
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
            else
            {
                ...
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
            }
        }
        ```

   - 如果这一帧还是 `isDone == false`，又 `return`，**下一帧再来，又是从方法顶部执行一遍，上面的流程再重复一次**。

3. **什么时候不再进入？**

   - 当某一帧检测到场景加载完成并且有效时：

     ```csharp
     _steps = ESteps.Done;
     Status = EOperationStatus.Succeed;
     ```

   - 下次再调用 `InternalUpdate()`，一进来就命中：

     ```csharp
     if (_steps == ESteps.None || _steps == ESteps.Done)
         return;
     ```

     直接 `return`，整个操作结束。

### 总结你问的那句

> 如果 CheckResult 不满足条件 `return` 后，OperationSystem 再次调用 `InternalUpdate` 后，会从哪里开始执行？

- **从函数开头开始重新执行整个 `InternalUpdate`。**
- 但因为 `_steps` 已经变成了 `ESteps.CheckResult`，所以：
  - 会跳过 `LoadScene` 那一段
  - 直接进入 `if (_steps == ESteps.CheckResult)` 这段逻辑
- 是否继续循环等待，完全由 `_asyncOperation.isDone` 和 `_steps` 的当前值决定。

===== User 10 =====
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\TEngine\UnityProject\Packages\YooAsset\Runtime\FileSystem\BundleResult\AssetBundleResult\Operation\AssetBundleLoadSceneOperation.cs (total lines: 117)
- d:\workspace\unity\TEngine\UnityProject\Assets\TEngine\Runtime\Module\SceneModule\SceneModule.cs (total lines: 413)

Files that are currently open and visible in the user's IDE:
- d:\workspace\unity\TEngine\UnityProject\Packages\YooAsset\Runtime\FileSystem\BundleResult\AssetBundleResult\Operation\AssetBundleLoadSceneOperation.cs (currently focused file, cursor is on line 116, total lines: 117)

Note: these files may or may not be relevant to the current conversation. Use the read file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files><user_query>
把这个对话一字不漏的记录下来，娶个适合的名字，放到合适的目录
</user_query>
