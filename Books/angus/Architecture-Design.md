# TEngine_Fantasy 架构设计与说明

## 一、项目架构概述

### 1.1 核心设计理念

TEngine_Fantasy 是一个采用**前后端共享C#逻辑**的Unity游戏项目，基于TEngine框架构建。项目最大的特色是通过代码共享实现客户端和服务器端逻辑的统一，减少重复开发，提高开发效率。

### 1.2 技术栈

- **Unity版本**: 2022.3.61f1c1
- **热更新方案**: HybridCLR（次世代热更新解决方案）
- **资源管理**: YooAsset（百万DAU验证的资源管理框架）
- **配置表**: Luban（最佳配置表解决方案）
- **服务器框架**: Fantasy（基于ETServer，简洁高性能的C#服务器框架）
- **异步方案**: UniTask（高性能异步/await方案）
- **网络协议**: Protobuf-net（前后端协议统一）

## 二、架构层次设计

### 2.1 三层架构

```
┌─────────────────────────────────────┐
│   AOT层（框架核心层）                 │
│   Assets/TEngine/Runtime             │
│   - 不可热更新                       │
│   - 提供基础框架能力                 │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   热更新层（业务逻辑层）              │
│   Assets/GameScripts/HotFix          │
│   - 可热更新                         │
│   - 游戏业务逻辑                     │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   共享逻辑层（前后端共享）            │
│   Assets/GameScripts/DotNet         │
│   - 网络通信                         │
│   - 消息处理                         │
│   - 协议定义                         │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   服务器层                           │
│   DotNet/                            │
│   - Fantasy服务器框架                │
│   - 业务逻辑处理                     │
└─────────────────────────────────────┘
```

### 2.2 程序集划分

#### AOT层程序集
- `TEngine.Runtime`: 框架核心运行时
- `TEngine.Editor`: 编辑器工具

#### 热更新层程序集（HotFix目录）
- `GameBase`: 游戏基础框架程序集
  - 单例系统
  - Redux架构
  - 基础工具类
- `GameProto`: 游戏配置协议程序集
  - 配置表加载
  - 协议定义
- `BattleCore`: 游戏核心战斗程序集（预留）
- `GameLogic`: 游戏业务逻辑程序集
  - UI系统
  - 游戏玩法逻辑
  - 数据管理

#### 共享逻辑层（DotNet目录）
- `Assets/GameScripts/DotNet/Core`: 核心共享代码
  - 网络通信（Network）
  - 消息处理（Message）
  - 数据结构和工具类
- `DotNet/Logic`: 服务器业务逻辑
- `DotNet/Config`: 配置表定义

## 三、前后端共享逻辑设计

### 3.1 共享机制

项目通过以下方式实现前后端代码共享：

#### 1. 条件编译宏
```csharp
#if TENGINE_NET  // 服务器端代码
    // 服务器专用逻辑
#endif

#if TENGINE_UNITY  // Unity客户端代码
    // Unity客户端专用逻辑
#endif
```

#### 2. 共享代码位置
- **共享核心代码**: `Assets/GameScripts/DotNet/Core`
- **服务器业务逻辑**: `DotNet/Logic`
- **协议定义**: 通过Protobuf统一

### 3.2 网络通信架构

#### 消息处理机制

```csharp
// 消息处理器基类（共享）
public abstract class Message<T> : IMessageHandler
{
    protected abstract FTask Run(Session session, T message);
}

// RPC消息处理器（共享）
public abstract class MessageRPC<TRequest, TResponse> : IMessageHandler
    where TRequest : IRequest 
    where TResponse : IResponse
{
    protected abstract FTask Run(Session session, TRequest request, 
                                 TResponse response, Action reply);
}
```

#### 客户端网络组件
```12:59:Assets/GameScripts/DotNet/Core/Network/Entity/ClientNetworkComponent.cs
public sealed class ClientNetworkComponent : Entity
{
    private AClientNetwork Network { get; set; }
    public Session Session { get; private set; }

    public void Initialize(NetworkProtocolType networkProtocolType, NetworkTarget networkTarget)
    {
        switch (networkProtocolType)
        { 
#if !UNITY_WEBGL
            case NetworkProtocolType.KCP:
            {
                Network = new KCPClientNetwork(Scene, networkTarget);
                return;
            }
            case NetworkProtocolType.TCP:
            {
                Network = new TCPClientNetwork(Scene, networkTarget);
                return;
            }
#endif
            default:
            {
                throw new NotSupportedException($"Unsupported NetworkProtocolType:{networkProtocolType}");
            }
        }
    }

    public void Connect(IPEndPoint remoteEndPoint, Action onConnectComplete, Action onConnectFail,Action onConnectDisconnect, int connectTimeout = 5000)
    {
        if (Network == null || Network.IsDisposed)
        {
            throw new NotSupportedException("Network is null or isDisposed");
        }

        Network.Connect(remoteEndPoint, onConnectComplete, onConnectFail, onConnectDisconnect, connectTimeout);
        Session = Session.Create(Network);
    }
    // ... 更多代码
}
```

### 3.3 服务器启动流程

```1:27:DotNet/App/Program.cs
using TEngine;
using TEngine.Core;
using TEngine.Logic;

try
{
    App.Init();

    AssemblySystem.Init();

    ConfigTableSystem.Bind();

    App.Start().Coroutine();

    Entry.Start().Coroutine();
    
    while(true)
    {
        Thread.Sleep(1);
        ThreadSynchronizationContext.Main.Update();
        SingletonSystem.Update();
    }
}
catch (Exception e)
{
    Log.Error(e);
}
```

### 3.4 客户端启动流程

客户端通过Unity的Procedure流程系统启动：

1. **ProcedureLaunch**: 启动流程
2. **ProcedureSplash**: 闪屏展示
3. **ProcedureInitPackage**: 初始化资源包
4. **ProcedureUpdateVersion**: 更新版本
5. **ProcedureUpdateManifest**: 更新清单
6. **ProcedureDownloadFile**: 下载文件
7. **ProcedureLoadAssembly**: 加载热更新程序集
8. **ProcedureStartGame**: 启动游戏（进入热更新域）

热更新域入口：
```14:28:Assets/GameScripts/HotFix/GameLogic/GameApp.cs
public static void Entrance(object[] objects)
{
    _hotfixAssembly = (List<Assembly>)objects[0];
    Log.Warning("======= 看到此条日志代表你成功运行了热更新代码 =======");
    Log.Warning("======= Entrance GameApp =======");
    Instance.Init();
    Instance.Start();
    Utility.Unity.AddUpdateListener(Instance.Update);
    Utility.Unity.AddFixedUpdateListener(Instance.FixedUpdate);
    Utility.Unity.AddLateUpdateListener(Instance.LateUpdate);
    Utility.Unity.AddDestroyListener(Instance.OnDestroy);
    Utility.Unity.AddOnDrawGizmosListener(Instance.OnDrawGizmos);
    Utility.Unity.AddOnApplicationPauseListener(Instance.OnApplicationPause);
    GameModule.Procedure.RestartProcedure(new GameLogic.OnEnterGameAppProcedure());
    Instance.StartGameLogic();
}
```

## 四、框架模块系统

### 4.1 模块设计模式

框架采用**面向接口编程**的设计模式：

1. **定义接口规范**: 如 `IResourceManager`
2. **实现具体模块**: `ResourceManager` 继承 `GameFrameworkModule` 并实现接口
3. **Mono调用层**: `GameFrameworkModuleBase` 提供Unity生命周期支持

### 4.2 核心模块

```72:85:Assets/TEngine/Runtime/GameModule.cs
private static void InitFrameWorkModules()
{
    Base = Get<RootModule>();
    Debugger = Get<DebuggerModule>();
    Fsm = Get<FsmModule>();
    ObjectPool = Get<ObjectPoolModule>();
    Resource = Get<ResourceModule>();
    Procedure = Get<ProcedureModule>();
    Setting = Get<SettingModule>();
    Localization = Get<LocalizationModule>();
    UI = Get<UIModule>();
    Audio = Get<AudioModule>();
    Timer = Get<TimerModule>();
}
```

### 4.3 模块注册机制

```114:137:Assets/TEngine/Runtime/GameFramework/GameModuleSystem.cs
internal static void RegisterModule(GameFrameworkModuleBase gameFrameworkModule)
{
    if (gameFrameworkModule == null)
    {
        Log.Error("Game Framework component is invalid.");
        return;
    }

    Type type = gameFrameworkModule.GetType();

    LinkedListNode<GameFrameworkModuleBase> current = s_GameFrameworkModules.First;
    while (current != null)
    {
        if (current.Value.GetType() == type)
        {
            Log.Error("Game Framework component type '{0}' is already exist.", type.FullName);
            return;
        }

        current = current.Next;
    }

    s_GameFrameworkModules.AddLast(gameFrameworkModule);
}
```

## 五、设计意图说明

### 5.1 为什么采用前后端共享逻辑？

1. **减少重复开发**: 网络协议、消息处理、数据结构等逻辑只需编写一次
2. **保证一致性**: 前后端使用相同的协议和数据结构，避免不一致导致的bug
3. **提高开发效率**: 修改协议或逻辑时，前后端同步更新
4. **降低维护成本**: 统一的代码库，减少维护工作量

### 5.2 为什么使用HybridCLR？

1. **真热更新**: 支持C#代码热更新，无需Lua等脚本语言
2. **性能优势**: 原生C#性能，无需解释执行
3. **开发体验**: 使用熟悉的C#语言，无需学习新语言
4. **商业级方案**: 经过大量项目验证的成熟方案

### 5.3 为什么使用YooAsset？

1. **资源管理**: 完善的资源引用计数和生命周期管理
2. **内存优化**: 支持LRU、ARC等缓存策略
3. **商业验证**: 百万DAU项目验证
4. **功能完善**: 支持资源加密、分包、版本管理等

### 5.4 为什么使用Luban？

1. **类型安全**: 强类型配置表，编译期检查
2. **多语言支持**: 支持多种编程语言代码生成
3. **性能优化**: 支持懒加载、异步加载、同步加载
4. **工具完善**: 提供Excel编辑和代码生成工具

## 六、Unity编辑器菜单项说明

### 6.1 TEngine菜单

#### TEngine/Settings/TEngineSettings
- **功能**: 打开TEngine框架设置面板
- **位置**: Project Settings → TEngine/TEngineSettings
- **用途**: 配置框架全局设置、HybridCLR设置、热更新程序集等
- **注意事项**: 
  - 首次使用需要创建 `TEngineGlobalSettings.asset` 文件
  - 修改设置后需要点击"Refresh HotUpdateAssemblies"同步程序集

#### TEngine/导出网络Proto|Gen Proto
- **功能**: 导出网络协议文件（.proto → C#）
- **实现**: 调用 `DotNet/start_export.bat` 脚本
- **用途**: 将Protobuf协议文件转换为C#代码
- **注意事项**: 
  - 首次使用需要先编译DotNet服务器解决方案
  - 确保DotNet目录下的Server.sln已正确配置

#### TEngine/导出Config|Export Config
- **功能**: 导出配置表（Excel → C#）
- **实现**: 调用 `Luban/gen_code_bin_to_project.bat` 脚本
- **用途**: 将Excel配置表转换为C#代码和二进制文件
- **注意事项**: 
  - 确保Luban配置正确
  - 配置表文件需要符合Luban格式要求

#### TEngine/Open Folder/Data Path
- **功能**: 打开Unity的Data Path文件夹
- **路径**: `Application.dataPath`
- **用途**: 快速访问项目资源目录

#### TEngine/Open Folder/Persistent Data Path
- **功能**: 打开持久化数据路径
- **路径**: `Application.persistentDataPath`
- **用途**: 访问游戏运行时数据目录（如存档、下载资源等）

#### TEngine/Open Folder/Streaming Assets Path
- **功能**: 打开StreamingAssets文件夹
- **路径**: `Application.streamingAssetsPath`
- **用途**: 访问只读资源目录

#### TEngine/Open Folder/Temporary Cache Path
- **功能**: 打开临时缓存路径
- **路径**: `Application.temporaryCachePath`
- **用途**: 访问临时文件目录

#### TEngine/Open Folder/Console Log Path
- **功能**: 打开控制台日志路径
- **路径**: `Application.consoleLogPath` 所在目录
- **用途**: 查看Unity编辑器日志文件
- **注意**: 仅Unity 2018.3及以上版本支持

#### TEngine/Log Scripting Define Symbols/Disable All Logs
- **功能**: 禁用所有日志宏定义
- **用途**: 发布版本时关闭所有日志输出，提升性能

#### TEngine/Log Scripting Define Symbols/Enable All Logs
- **功能**: 启用所有日志宏定义
- **用途**: 开发调试时启用所有日志级别

#### TEngine/Log Scripting Define Symbols/Enable Debug And Above Logs
- **功能**: 启用Debug及以上级别的日志
- **宏定义**: `ENABLE_DEBUG_AND_ABOVE_LOG`

#### TEngine/Log Scripting Define Symbols/Enable Info And Above Logs
- **功能**: 启用Info及以上级别的日志
- **宏定义**: `ENABLE_INFO_AND_ABOVE_LOG`

#### TEngine/Log Scripting Define Symbols/Enable Warning And Above Logs
- **功能**: 启用Warning及以上级别的日志
- **宏定义**: `ENABLE_WARNING_AND_ABOVE_LOG`

#### TEngine/Log Scripting Define Symbols/Enable Error And Above Logs
- **功能**: 启用Error及以上级别的日志
- **宏定义**: `ENABLE_ERROR_AND_ABOVE_LOG`

#### TEngine/Log Scripting Define Symbols/Enable Fatal And Above Logs
- **功能**: 启用Fatal及以上级别的日志
- **宏定义**: `ENABLE_FATAL_AND_ABOVE_LOG`

#### TEngine/Profiler Define Symbols/Disable All Profiler
- **功能**: 禁用所有性能分析器宏定义
- **用途**: 发布版本时关闭性能分析，减少开销

#### TEngine/Profiler Define Symbols/Enable All Profiler
- **功能**: 启用所有性能分析器宏定义
- **用途**: 开发时启用性能分析功能

### 6.2 HybridCLR菜单

#### HybridCLR/Define Symbols/Disable HybridCLR
- **功能**: 禁用HybridCLR宏定义
- **宏定义**: 移除 `ENABLE_HYBRIDCLR`
- **用途**: 不需要热更新功能时禁用

#### HybridCLR/Define Symbols/Enable HybridCLR
- **功能**: 启用HybridCLR宏定义
- **宏定义**: 添加 `ENABLE_HYBRIDCLR`
- **用途**: 启用热更新功能

#### HybridCLR/Build/BuildAssets And CopyTo AssemblyTextAssetPath
- **功能**: 构建热更新程序集并复制到资源路径
- **步骤**:
  1. 编译热更新DLL（`CompileDllCommand.CompileDll`）
  2. 复制AOT补充元数据DLL到资源路径
  3. 复制热更新DLL到资源路径
- **用途**: 打包前准备热更新资源
- **注意事项**: 
  - 需要先构建一次游戏App才能生成裁剪后的AOT DLL
  - 确保 `AssemblyTextAssetPath` 配置正确

### 6.3 YooAsset菜单

#### YooAsset/Home Page
- **功能**: 打开YooAsset主页
- **用途**: 访问YooAsset文档和资源

#### YooAsset/AssetBundle Collector
- **功能**: 打开资源包收集器窗口
- **用途**: 配置资源收集规则，定义哪些资源需要打包

#### YooAsset/AssetBundle Builder
- **功能**: 打开资源包构建工具
- **用途**: 构建AssetBundle资源包
- **配置项**:
  - 构建平台
  - 构建管道（Builtin/可编程构建管道）
  - 构建模式（强制重建/增量构建）
  - 压缩格式
  - 加密服务

#### YooAsset/AssetBundle Reporter
- **功能**: 打开资源包报告工具
- **用途**: 查看资源包构建报告，分析资源依赖关系

#### YooAsset/AssetBundle Debugger
- **功能**: 打开资源包调试器
- **用途**: 运行时调试资源加载情况

#### YooAsset/ShaderVariant Collector
- **功能**: 打开Shader变体收集器
- **用途**: 收集项目使用的Shader变体，减少运行时变体收集开销

#### YooAsset/补丁包导入工具
- **功能**: 导入补丁包工具
- **用途**: 导入已构建的资源包到项目

#### YooAsset/补丁包比对工具
- **功能**: 补丁包比对工具
- **用途**: 比对不同版本的资源包差异

### 6.4 GameObject菜单

#### GameObject/ScriptGenerator/UIProperty
- **功能**: 生成UI属性代码
- **用途**: 自动生成UI组件的属性绑定代码
- **生成内容**: 
  - UI组件属性声明
  - 组件绑定代码
  - `ScriptGenerator()` 方法

#### GameObject/ScriptGenerator/UIProperty - UniTask
- **功能**: 生成支持UniTask的UI属性代码
- **用途**: 生成异步友好的UI代码

#### GameObject/ScriptGenerator/UIPropertyAndListener
- **功能**: 生成UI属性和事件监听代码
- **用途**: 自动生成UI组件属性和事件回调代码
- **生成内容**: 
  - UI组件属性
  - 事件监听代码
  - 事件回调方法框架

#### GameObject/ScriptGenerator/UIPropertyAndListener - UniTask
- **功能**: 生成支持UniTask的UI属性和事件代码

#### GameObject/ScriptGenerator/About
- **功能**: 显示脚本生成器说明

### 6.5 Assets菜单（右键菜单）

#### Assets/Get Asset Path
- **功能**: 获取选中资源的路径
- **用途**: 复制资源路径到剪贴板
- **使用**: 在Project窗口选中资源，右键选择

#### Assets/Get Addressable Path
- **功能**: 获取资源的Addressable地址
- **用途**: 复制资源的Addressable地址到剪贴板
- **使用**: 在Project窗口选中资源，右键选择

### 6.6 Window菜单

#### Window/UniTask Tracker
- **功能**: 打开UniTask跟踪器窗口
- **用途**: 监控和调试UniTask异步任务
- **功能**: 
  - 查看当前运行的异步任务
  - 监控任务状态
  - 调试异步流程

#### Window/PlayerPrefs Editor
- **功能**: 打开PlayerPrefs编辑器
- **用途**: 可视化编辑和管理PlayerPrefs数据
- **功能**: 
  - 查看所有PlayerPrefs键值对
  - 添加/删除/修改数据
  - 导入/导出数据

### 6.7 Tools菜单

#### Tools/UniTaskEditorRunnerChecker
- **功能**: UniTask编辑器运行检查器
- **用途**: 检查编辑器中的UniTask使用情况

## 七、使用注意事项

### 7.1 前后端共享代码注意事项

1. **条件编译宏使用**
   - 服务器端代码使用 `#if TENGINE_NET`
   - Unity客户端代码使用 `#if TENGINE_UNITY`
   - 共享代码不使用条件编译，或同时包含两个宏

2. **Unity API限制**
   - 共享代码中不能直接使用Unity API（如 `GameObject`、`MonoBehaviour`）
   - 需要使用抽象接口或通过事件系统解耦

3. **程序集引用**
   - 确保共享代码不引用Unity特定程序集
   - 服务器端代码需要能够独立编译

### 7.2 热更新注意事项

1. **AOT补充元数据**
   - 首次打包前必须先构建一次游戏App
   - 确保AOT补充元数据DLL正确生成和复制

2. **程序集配置**
   - 修改热更新程序集后，需要在TEngineSettings中同步
   - 使用"Refresh HotUpdateAssemblies"按钮同步程序集列表

3. **代码限制**
   - 热更新代码不能使用反射调用AOT代码中的私有成员
   - 需要热更新的类型必须在热更新程序集中定义

### 7.3 资源管理注意事项

1. **资源引用**
   - 使用 `AssetReference` 管理资源引用
   - 及时释放不需要的资源，避免内存泄漏

2. **资源组管理**
   - 使用 `AssetGroup` 进行资源分组管理
   - 资源组的生命周期由根节点控制

3. **缓存策略**
   - 根据项目需求选择合适的缓存策略（LRU/ARC）
   - 注意缓存大小限制，避免内存溢出

### 7.4 网络通信注意事项

1. **消息处理**
   - 消息处理器需要在服务器端和客户端分别注册
   - 确保消息类型在前后端一致

2. **协议版本**
   - 修改协议时注意向后兼容性
   - 使用版本号管理协议变更

3. **网络线程**
   - 网络操作在独立线程中执行
   - 需要使用 `ThreadSynchronizationContext` 切换到主线程

### 7.5 配置表注意事项

1. **配置表格式**
   - 确保Excel配置表符合Luban格式要求
   - 类型定义必须正确

2. **配置表加载**
   - 支持同步、异步、懒加载三种方式
   - 根据使用场景选择合适的加载方式

3. **配置表更新**
   - 修改配置表后需要重新导出
   - 确保前后端使用相同版本的配置表

### 7.6 开发调试注意事项

1. **日志级别**
   - 开发时启用详细日志
   - 发布版本时关闭日志以提升性能

2. **性能分析**
   - 开发时启用性能分析
   - 发布版本时关闭性能分析

3. **宏定义管理**
   - 使用菜单项统一管理宏定义
   - 避免手动修改宏定义导致不一致

## 八、总结

TEngine_Fantasy项目通过前后端共享C#逻辑的设计，实现了高效的开发模式。通过合理的架构分层、模块化设计和完善的工具支持，为游戏开发提供了强大的基础框架。在使用过程中，需要注意前后端代码的兼容性、热更新的限制以及资源管理的规范，才能充分发挥框架的优势。

