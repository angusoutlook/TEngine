<user_info>
OS Version: win32 10.0.26200
Current Date: Saturday Dec 27, 2025
Shell: C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe
Workspace Path: D:\workspace\unity\alex-rachel\TEngine_Fantasy
Terminals folder: c:\Users\angus\.cursor\projects\d-workspace-unity-alex-rachel-TEngine-Fantasy\terminals
</user_info>

<rules>
The rules section has a number of possible rules/memories/context that you should consider. In each subsection, we provide instructions about what information the subsection contains and how you should consider/follow the contents of the subsection.



<user_rules description="These are rules set by the user that you should follow if appropriate.">
- 如果我让你分析代码的时候，需要做到以下几点：
1、直接分析真实代码：
"请基于我提供的实际代码进行分析"
"不要简化或重写代码"
2、保持准确性：
"请确保解释与真实代码完全一致"
"如果代码复杂，请逐步解释，但不要改变原始逻辑"
3、明确标注：
"如果需要简化说明，请明确标注这是简化版本"
"区分概念解释和代码分析"
4、指出关键实现细节并解释设计意图
- Always respond in Chinese-simplified.
Always start your answer by telling me what AI model you are using.
Do not arbitrarily expand or modify my ideas.
Do not write test code.
if it is a Unity Project , make sure the Unity version is 2022.3.61f1c1.
</user_rules>

</rules>


<project_layout>
Below is a snapshot of the current workspace's file structure at the start of the conversation. This snapshot will NOT update during the conversation.

d:\workspace\unity\alex-rachel\TEngine_Fantasy\
  - Assembly-CSharp-Editor.csproj
  - Assembly-CSharp.csproj
  - Assets\
    - AssetRaw\
      [40 files in subtree: 28 *.meta, 4 *.bytes, 4 *.png, ...]
    - AssetRaw.meta
    - Atlas\
      [2 files in subtree: 1 *.meta, 1 *.spriteatlas]
    - Atlas.meta
    - GameScripts\
      [802 files in subtree: 460 *.meta, 328 *.cs, 5 *.asmdef, ...]
    - GameScripts.meta
    - Resources\
      [2 files in subtree: 1 *.asset, 1 *.meta]
    - Resources.meta
    - Scenes\
      [4 files in subtree: 2 *.meta, 2 *.unity]
    - Scenes.meta
    - TEngine\
      [2020 files in subtree: 1088 *.meta, 709 *.cs, 125 *.png, ...]
    - TEngine.meta
  - BattleCore.Runtime.csproj
  - Books\
    - 0-介绍.md
    - 1-快速开始.md
    - 2-框架概览.md
    - 3-1-资源模块.md
    - 3-2-事件模块.md
    - 3-3-内存池模块.md
    - 3-4-对象池模块.md
    - 3-5-UI模块.md
    - 3-6-配置表模块.md
    - 3-7-流程模块.md
    - 3-8-网络模块.md
    - 99-各平台运行RunAble.md
    - angus\
      [4 files in subtree: 4 *.md]
    - src\
      [17 files in subtree: 17 *.png]
  - DotNet\
    - App\
      [5 files in subtree: 2 *.cs, 1 *.config, 1 *.json, ...]
    - Bin\...
    - Config\
      [24 files in subtree: 5 *.Json, 4 *.bytes, 4 *.cs, ...]
    - Core\
      [3 files in subtree: 2 *.dll, 1 *.json]
    - Logic\
      [65 files in subtree: 61 *.cs, 3 *.dll, 1 *.json]
    - Server.Build.props
    - Server.sln
    - Server.sln.DotSettings
    - start_develop.bat
    - start_export.bat
    - ThirdParty\
      [1 file in subtree: 1 *.cs]
  - DotNet.csproj
  - GameBase.csproj
  - GameLogic.csproj
  - GameProto.csproj
  - HybridCLRData\
    - hybridclr_repo\...
    - il2cpp_plus_repo\...
    - LocalIl2CppData-WindowsEditor\...
  - init-git-remote.ps1
  - Library\
    - AnnotationManager
    - APIUpdater\...
    - ArtifactDB
    - ArtifactDB-lock
    - Artifacts\...
    - AtlasCache\...
    - Bee\...
    - BuildPlayer.prefs
    - BuildSettings.asset
    - BurstCache\...
    - EditorInstance.json
    - EditorOnlyScriptingSettings.json
    - EditorOnlyVirtualTextureState.json
    - EditorSnapSettings.asset
    - EditorUserBuildSettings.asset
    - expandedItems
    - GridBrush\...
    - ilpp.pid
    - InspectorExpandedItems.asset
    - LastSceneManagerSetup.txt
    - LastSceneManagerSetup.txt.meta
    - LibraryFormatVersion.txt
    - MonoManager.asset
    - PackageCache\...
    - PackageManager\...
    - SceneVisibilityState.asset
    - ScriptAssemblies\...
    - ScriptMapper
    - Search\...
    - ShaderCache\...
    - ShaderCache.db
    - SourceAssetDB
    - SourceAssetDB-lock
    - SpriteAtlasDatabase.asset
    - StateCache\...
    - Style.catalog
    - TempArtifacts\...
  - LICENSE
  - Logs\
    - shadercompiler-UnityShaderCompiler.exe0.log
  - Luban\
    - Config\
      [10 files in subtree: 6 *.xlsx, 4 *.xml]
    - CustomTemplate_Client\
      [1 file in subtree: 1 *.cs]
    - CustomTemplate_Client_LazyLoad\
      [4 files in subtree: 3 *.tpl, 1 *.cs]
    - CustomTemplate_Client_UniTask\
      [4 files in subtree: 3 *.tpl, 1 *.cs]
    - CustomTemplate_Server_Task\
      [3 files in subtree: 3 *.tpl]
    - FileServer\
      [12 files in subtree: 7 *.js, 2 *.bat, 1 *.json, ...]
    - gen_code_bin_to_project_lazyload.bat
    - gen_code_bin_to_project_unitask.bat
    - gen_code_bin_to_project.bat
    - gen_code_bin_to_server.bat
    - Luban.ClientServer\
      [157 files in subtree: 108 *.tpl, 34 *.dll, 12 *.json, ...]
  - obj\
    - Debug\...
  - Packages\
    - manifest.json
    - packages-lock.json
  - ProjectSettings\
    - AudioManager.asset
    - AutoStreamingSettings.asset
    - BurstAotSettings_Android.json
    - BurstAotSettings_iOS.json
    - BurstAotSettings_StandaloneLinux64.json
    - BurstAotSettings_StandaloneWindows.json
    - ClusterInputManager.asset
    - CommonBurstAotSettings.json
    - DynamicsManager.asset
    - EditorBuildSettings.asset
    - EditorSettings.asset
    - GraphicsSettings.asset
    - HybridCLRSettings.asset
    - InputManager.asset
    - MemorySettings.asset
    - NavMeshAreas.asset
    - NetworkManager.asset
    - PackageManagerSettings.asset
    - Physics2DSettings.asset
    - PresetManager.asset
    - ProjectSettings.asset
    - ProjectVersion.txt
    - QualitySettings.asset
    - SceneTemplateSettings.json
    - TagManager.asset
    - TimelineSettings.asset
    - TimeManager.asset
    - UnityConnectSettings.asset
    - VersionControlSettings.asset
    - VFXManager.asset
    - XRSettings.asset
  - README.md
  - Sirenix.OdinInspector.Modules.UnityMathematics.csproj
  - Temp\...
  - TEngine_Fantasy.sln
  - TEngine.Editor.csproj
  - TEngine.Runtime.csproj
  - UserSettings\
    [2 files in subtree: 1 *.asset, 1 *.dwlt]
  - YooAsset.Editor.csproj



Note: File extension counts do not include files ignored by .gitignore.</project_layout>

Here are some folder(s) I manually attached to my message:
Folder: /d:/workspace/unity/alex-rachel/TEngine_Fantasy/Assets/GameScripts/DotNet/Core/Singleton
Contents of directory:

[file] Singleton.cs (1.5KB, 57 lines)
[file] Singleton.cs.meta (254B, 12 lines)
[file] SingletonSystem.cs (4.4KB, 142 lines)
[file] SingletonSystem.cs.meta (254B, 12 lines)
[dir]  Interface/
[file] Interface.meta (180B, 9 lines)

<additional_data>
Below are some potentially helpful/relevant pieces of information for figuring out how to respond:
<open_and_recently_viewed_files>
Recently viewed files (recent at the top, oldest at the bottom):
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\DotNet\App\NLog.config (total lines: 91)
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\DotNet\App\App.csproj (total lines: 34)
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\Books\angus\Server-Architecture.md (total lines: 1263)
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\Assets\GameScripts\DotNet\Logic\Share.cs (total lines: 5)
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\Assets\GameScripts\DotNet\Logic\ErrorCode.cs.meta (total lines: 3)
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\Assets\GameScripts\DotNet\Logic\ErrorCode.cs (total lines: 20)

Files that are currently open and visible in the user's IDE:
- d:\workspace\unity\alex-rachel\TEngine_Fantasy\DotNet\App\NLog.config (currently focused file, cursor is on line 1, total lines: 91)

Note: these files may or may not be relevant to the current conversation. Use the read_file tool if you need to get the contents of some of them.
</open_and_recently_viewed_files>
</additional_data>

<user_query>
@Assets/GameScripts/DotNet/Core/Singleton 阅读代码，分析架构和设计意图，并举例使用说明，最好项目中有现成的例子，请尽量举例
</user_query>

我使用的 AI 模型：**GPT-5.2**。

我会先把 `Assets/GameScripts/DotNet/Core/Singleton` 里的真实代码完整读一遍（含接口与系统管理器），再全项目搜索它的实际用法（继承/调用点），最后基于这些“现成代码”解释架构与设计意图，并给出可直接照抄的使用方式与注意点（不改你项目逻辑）。

我使用的 AI 模型：**GPT-5.2**。

Nice：你这个单例体系不是“懒加载 static Instance”那种，而是**由 `AssemblyManager` 驱动的“按程序集加载/卸载”单例生命周期系统**，并且支持把实现了 `IUpdateSingleton` 的单例自动纳入全局 Update 队列。接下来我会把接口也读出来，并全局搜一遍有哪些现成单例实现与调用点，给你直接引用项目内例子。

我使用的 AI 模型：**GPT-5.2**。

我已经把单例框架三件套（`ISingleton` / `Singleton<T>` / `SingletonSystem`）和它在 Unity/Server 启动链路中的挂载点都定位出来了；下一步我会再把 `AssemblyManager` 的加载/卸载事件机制读清楚，并从项目里抓几处 `.Instance` 的真实调用点，方便你看到“怎么用、什么时候可用”。

我使用的 AI 模型：**GPT-5.2**。

我已经把这套单例系统的“生命周期入口”（`GameContext.Init` / `GameSystem.Update` / `App.Init`）和几种典型单例实现（`EventSystem`、`TimerScheduler`、`EntitiesSystem`、`LogCore`）都找到了；接下来我会再读几处真实调用点（`.Instance` 的使用现场），这样你能直观看到它在项目里是怎么被依赖、以及为什么要做按程序集装卸载。

我使用的 AI 模型：**GPT-5.2**。

### Unity 版本确认
你这个 Unity 工程版本是 **2022.3.61f1c1**（来自 `ProjectSettings/ProjectVersion.txt`）。

```1:3:ProjectSettings/ProjectVersion.txt
m_EditorVersion: 2022.3.61f1c1
m_EditorVersionWithRevision: 2022.3.61f1c1 (327989805ccf)
```

### 这套 Singleton 的总体架构（谁创建、何时创建、如何销毁）
- **创建时机**：不是“第一次访问 `Instance` 才 new”，而是 **`AssemblyManager` 触发装载事件后，由 `SingletonSystem` 扫描程序集并 `Activator.CreateInstance` 批量创建**。
- **销毁时机**：当某个程序集卸载/重载时，`AssemblyManager` 先触发 UnLoad，再触发 Load；`SingletonSystem` 会把该程序集对应的单例全部 `Dispose()`，再重新创建。
- **Update 驱动**：实现 `IUpdateSingleton` 的单例会被自动加入 `SingletonSystem.Update()` 的队列，每帧（Unity）或每次主循环（Server）被调用一次。

Unity 侧入口非常明确：先初始化 `SingletonSystem`（必须最前），再初始化 `AssemblyManager`，每帧调用 `SingletonSystem.Update()`：

```14:33:Assets/GameScripts/DotNet/Core/Unity/GameContext.cs
public static class GameContext
{
    public static void Init()
    {
        // ...略...
        SingletonSystem.Initialize();
        AssemblyManager.Initialize();
    }
    
    public static void Close()
    {
        SingletonSystem.Dispose();
        AssemblyManager.Dispose();
        Scene.DisposeAllScene();
    }
}
```

```12:37:Assets/GameScripts/DotNet/Core/Unity/GameSystem.cs
public static Scene Init()
{
    GameContext.Init();
    new GameObject("[EntitySystem]").AddComponent<GameSystem>();
    return Scene.Create("Unity");
}

private void Update()
{
    ThreadSynchronizationContext.Main.Update();
    SingletonSystem.Update(); 
}
```

Server 侧也是同一套路（同样强调 `SingletonSystem.Initialize()` 必须在 `AssemblyManager.Initialize()` 之前）：

```69:75:Assets/GameScripts/DotNet/Core/App.cs
// ...略...
SingletonSystem.Initialize();
// 加载核心程序集
AssemblyManager.Initialize();
```

---

### 核心代码：`ISingleton` / `Singleton<T>` / `SingletonSystem`
#### 1) `ISingleton`：统一生命周期接口
```6:10:Assets/GameScripts/DotNet/Core/Singleton/Interface/ISingleton.cs
public interface ISingleton : IDisposable
{
    public bool IsDisposed { get; set; }
    public Task Initialize();
}
```
设计意图：所有单例都必须**可释放**，并有一个统一的初始化入口（可异步）。

#### 2) `Singleton<T>`：提供 `Instance`、装载/卸载回调、以及“临时注入”
```7:55:Assets/GameScripts/DotNet/Core/Singleton/Singleton.cs
public abstract class Singleton<T> : ISingleton where T : ISingleton, new()
{
    public bool IsDisposed { get; set; }
    public static T Instance { get; private set; }

    private void RegisterSingleton(ISingleton singleton)
    {
        Instance = (T) singleton;
        AssemblyManager.OnLoadAssemblyEvent += OnLoad;
        AssemblyManager.OnUnLoadAssemblyEvent += OnUnLoad;
    }

    public static T TempInstance
    {
        get => Instance;
        set
        {
            if (Instance != null) return;
            Instance = value;
        }
    }

    public virtual Task Initialize() => Task.CompletedTask;
    protected virtual void OnLoad(int assemblyName) { }
    protected virtual void OnUnLoad(int assemblyName) { }

    public virtual void Dispose()
    {
        IsDisposed = true;
        Instance = default;
        AssemblyManager.OnLoadAssemblyEvent -= OnLoad;
        AssemblyManager.OnUnLoadAssemblyEvent -= OnUnLoad;
    }
}
```

关键点（真实代码行为）：
- `RegisterSingleton` 是 **private**，外部不能直接调用；它会设置 `Instance` 并订阅程序集装卸载事件。
- `TempInstance` 的注释写了“特殊情况调用设置”，并且**只允许第一次设置**（如果 `Instance != null` 就直接 return）。

#### 3) `SingletonSystem`：按程序集批量创建、按程序集批量销毁、Update 调度
```23:73:Assets/GameScripts/DotNet/Core/Singleton/SingletonSystem.cs
public static void Initialize()
{
    AssemblyManager.OnLoadAssemblyEvent += Load;
    AssemblyManager.OnUnLoadAssemblyEvent += UnLoad;
}

private static void Load(int assemblyName)
{
    UnLoad(assemblyName);
    foreach (var singletonType in AssemblyManager.ForEach(assemblyName, typeof(ISingleton)))
    {
        var instance = (ISingleton) Activator.CreateInstance(singletonType);
        var registerMethodInfo = singletonType.BaseType?.GetMethod("RegisterSingleton", BindingFlags.Instance | BindingFlags.NonPublic);
        var initializeMethodInfo = singletonType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);
        var onLoadMethodInfo = singletonType.GetMethod("OnLoad", BindingFlags.Instance | BindingFlags.NonPublic);

        if (initializeMethodInfo != null)
        {
            task.Add((Task) initializeMethodInfo.Invoke(instance, null));
        }

        registerMethodInfo?.Invoke(instance, new object[] {instance});
        onLoadMethodInfo?.Invoke(instance, new object[] {assemblyName});

        if (instance is IUpdateSingleton iUpdateSingleton) Updates.Enqueue(iUpdateSingleton);
        Singletons.Enqueue(assemblyName, instance);
    }

    Task.WaitAll(task.ToArray());
}
```

这里有一个非常重要的时序事实（从上面真实代码直接得出）：
- **`Initialize()` 是在 `RegisterSingleton()` 之前调用的**  
  也就是说：在 `Initialize()` 执行期间，`Singleton<T>.Instance` 可能还没被设置好（因为 `RegisterSingleton` 还没反射调用）。如果你在 `Initialize()` 里写 `XxxSingleton.Instance`，要特别小心时序。

Update 队列的“自动清除”也值得注意：如果某个 `IUpdateSingleton` 已经 `IsDisposed == true`，它会被丢弃且不再入队：

```103:127:Assets/GameScripts/DotNet/Core/Singleton/SingletonSystem.cs
public static void Update()
{
    var updatesCount = Updates.Count;

    while (updatesCount-- > 0)
    {
        var updateSingleton = Updates.Dequeue();

        if (updateSingleton.IsDisposed)
        {
            continue;
        }
        
        Updates.Enqueue(updateSingleton);

        updateSingleton.Update();
    }
}
```

---

### “按程序集装卸载”的设计意图（为什么这么做）
`AssemblyManager.LoadAssembly` 在重载时会先触发卸载事件，再触发装载事件：

```24:55:Assets/GameScripts/DotNet/Core/Assembly/AssemblyManager.cs
if (isReLoad)
{
    assemblyInfo.Unload();
    OnUnLoadAssemblyEvent(assemblyName);
}

assemblyInfo.Load(assembly);

OnLoadAssemblyEvent(assemblyName);

if (isReLoad) OnReLoadAssemblyEvent(assemblyName);
```

这套设计的核心价值（结合你项目结构最常见的诉求）：
- **支持 HotFix/模块化**：某个程序集（例如热更逻辑）可以重载时，把它相关的单例/系统、注册表、事件处理器等全部卸载干净再重建。
- **隔离生命周期**：单例不再是“进程级永生”，而是“跟随程序集生命周期”，避免旧逻辑残留。
- **集中调度**：Update、Dispose 都集中到 `SingletonSystem`，减少散落的 MonoBehaviour 驱动点。

---

### 项目里的“现成例子”（怎么用、用来干什么）
#### 例子 A：`EventSystem` —— 单例 + 利用 `OnLoad/OnUnLoad` 做“按程序集注册”
`EventSystem` 正是用 `Singleton<T>` 的 `OnLoad/OnUnLoad` 机制，在每次程序集加载时扫描并注册 `IEvent/IAsyncEvent` 实现；卸载时按程序集清理：

```24:80:Assets/GameScripts/DotNet/Core/EventSystem/EventSystem.cs
public sealed class EventSystem : Singleton<EventSystem>
{
    protected override void OnLoad(int assemblyName)
    {
        foreach (var type in AssemblyManager.ForEach(assemblyName, typeof(IEvent)))
        {
            var obj = (IEvent) Activator.CreateInstance(type);
            // 注册到 _events，并记录到 _assemblyEvents(assemblyName, ...)
        }

        foreach (var type in AssemblyManager.ForEach(assemblyName, typeof(IAsyncEvent)))
        {
            var obj = (IAsyncEvent) Activator.CreateInstance(type);
            // 注册到 _asyncEvents，并记录到 _assemblyAsyncEvents(assemblyName, ...)
        }
    }

    protected override void OnUnLoad(int assemblyName)
    {
        // 按 assemblyName 把前面记录的事件实现移除
    }
}
```

这就是“按程序集装卸载”的典型用途：**热更程序集重载时，事件监听自动跟着重建**。

#### 例子 B：`TimerScheduler` —— 实现 `IUpdateSingleton`，自动被每帧调用
```7:20:Assets/GameScripts/DotNet/Core/TimerScheduler/TimerScheduler.cs
public sealed class TimerScheduler : Singleton<TimerScheduler>, IUpdateSingleton
{
    public readonly TimerSchedulerCore Core = new TimerSchedulerCore(() => TimeHelper.Now);
    public void Update()
    {
        Core.Update();
#if TENGINE_UNITY
        Unity.Update();
#endif
    }
}
```

它在项目中被大量直接使用（全是现成调用点）：

- TCP 连接超时用 `TimerScheduler.Instance.Core.OnceTimer(...)`：

```70:76:Assets/GameScripts/DotNet/Core/Network/NetworkProtocol/TCP/Client/TCPClientNetwork.cs
_connectTimeoutId = TimerScheduler.Instance.Core.OnceTimer(connectTimeout, () =>
{
    OnConnectFail?.Invoke();
    Dispose();
});
```

- Unity 心跳组件用 `TimerScheduler.Instance.Unity.RepeatedTimer(...)`，并在 Stop 时做安全移除（注意这里用了 `?.`）：

```25:41:Assets/GameScripts/DotNet/Core/Modules/Session/SessionHeartbeatComponent.cs
_timerId = TimerScheduler.Instance.Unity.RepeatedTimer(interval, () => RepeatedSend().Coroutine());

TimerScheduler.Instance?.Unity.RemoveByRef(ref _timerId);
```

- 甚至有静态构造里直接用（意味着它希望 TimerScheduler 很早就可用）：

```19:23:Assets/GameScripts/DotNet/Core/Network/Message/MessageHelper.cs
static MessageHelper()
{
    TimerScheduler.Instance.Core.RepeatedTimer(10000, new NetworkMessageUpdate());
}
```

#### 例子 C：`EntitiesSystem` —— 单例 + `IUpdateSingleton` + “按程序集注册 ECS 系统”
`EntitiesSystem` 在 `OnLoad` 中扫描程序集里所有 `IEntitiesSystem`，并按接口类型分发到不同字典；这样热更程序集重载时系统表也能更新：

```19:50:Assets/GameScripts/DotNet/Core/Entitas/EntitiesSystem.cs
protected override void OnLoad(int assemblyName)
{
    foreach (var entitiesSystemType in AssemblyManager.ForEach(assemblyName, typeof(IEntitiesSystem)))
    {
        var entity = Activator.CreateInstance(entitiesSystemType);

        switch (entity)
        {
            case IAwakeSystem iAwakeSystem: _awakeSystems.Add(iAwakeSystem.EntitiesType(), iAwakeSystem); break;
            case IUpdateSystem iUpdateSystem: _updateSystems.Add(iUpdateSystem.EntitiesType(), iUpdateSystem); break;
            // ... Destroy/Deserialize ...
        }

        _assemblyList.Add(assemblyName, entitiesSystemType);
    }
}
```

并且 `Entity.Create` 里直接依赖它（这是你项目里最直观的“怎么用”）：

```220:224:Assets/GameScripts/DotNet/Core/Entitas/Entity.cs
if (isRunEvent)
{
    EntitiesSystem.Instance.Awake(entity);
    EntitiesSystem.Instance.StartUpdate(entity);
}
```

#### 例子 D：`LogCore.TempInstance` —— “特殊情况”的真实用法
`Log` 的静态构造函数里，在 `LogCore.Instance == null` 时用 `TempInstance` 手动塞一个实例进去（非常符合 `TempInstance` 注释“特殊情况调用设置”）：

```6:13:Assets/GameScripts/DotNet/Core/Log/Log.cs
static Log()
{
    if (LogCore.Instance == null)
    {
        LogCore.TempInstance = new LogCore();
    }
    LogCore.Instance.ILog = new NLog("Server");
}
```

这通常是为了处理：**日志在框架初始化链路（`SingletonSystem/AssemblyManager`）之前就被调用** 的场景。

---

### 使用时的关键注意点（结合你项目真实实现）
- **初始化顺序必须保证**：Unity 的 `GameContext.Init()` / Server 的 `App.Init()` 都已经写了注释：`SingletonSystem.Initialize()` 必须先于 `AssemblyManager.Initialize()`（否则加载事件触发时，单例系统还没订阅，创建就会漏掉）。
- **`Initialize()` 里不要依赖 `Instance` 已经存在**：因为 `SingletonSystem.Load()` 里是先 `Initialize()`，后反射调用 `RegisterSingleton()` 才把 `Instance` 设上。
- **单例类型必须能被 `Activator.CreateInstance` 创建**：也就是 **非抽象、且有无参构造**（你这里的创建方式决定的）。
- **要参与每帧/每轮 Update，就实现 `IUpdateSingleton`**：例如 `TimerScheduler` / `EntitiesSystem`；它们会被 `SingletonSystem.Update()` 自动驱动。

如果你希望我“尽量举例”，我还可以继续把上面 grep 出来的其他单例（如 `NetworkThread`、`MessageDispatcherSystem`、`MongoHelper`、`GameApp`、`GameClient`、`ConfigLoader` 等）各挑一个，逐个对照它们的 `Initialize/OnLoad/OnUnLoad` 具体干了什么、以及项目里谁在调用它们的 `.Instance`。你更关心 **HotFix 目录下的单例（`Assets/GameScripts/HotFix/...`）** 还是 **DotNet/Core 下的基础设施单例**？

<additional_data>
Below are some potentially helpful/relevant pieces of information for figuring out how to respond:
<current_file>
Path: DotNet/App/NLog.config
Currently selected line: 1
Line 1 content: `<?xml version="1.0" encoding="utf-8" ?>`
</current_file>
</additional_data>

<user_query>
把 会话内容一字不差的取个英文名字，保存到books angus 目录
</user_query>


