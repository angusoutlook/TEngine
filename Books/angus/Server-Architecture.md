# TEngine Fantasy 服务器架构说明

## 一、系统架构概述

### 1.1 项目结构

TEngine Fantasy 服务器采用 **.NET 9.0** 开发，基于 **Fantasy框架**（基于ETServer）构建的分布式游戏服务器系统。项目采用分层架构设计，支持多服务器分布式部署。

**重要说明**：服务器项目采用**前后端代码共享**机制，Core框架代码实际位于Unity项目中，通过项目链接的方式引用。

```
DotNet/                                    # 服务器项目目录
├── App/                                   # 应用程序入口层
│   ├── Program.cs                        # 主程序入口
│   └── TEngineSettings.json              # 框架配置
├── Core/                                  # 核心框架层项目文件（仅.csproj）
│   └── Core.csproj                        # 通过链接引用Unity项目中的Core代码
├── Logic/                                 # 业务逻辑层
│   ├── Logic.csproj                       # 项目文件，链接Unity项目中的共享代码
│   └── src/                               # 服务器专用业务逻辑代码
│       ├── Config/                        # 配置加载
│       ├── Handler/                       # 消息处理器
│       ├── Model/                         # 数据模型
│       ├── Helper/                        # 辅助工具
│       └── Generate/                      # 生成的代码（协议、配置表）
└── ThirdParty/                            # 第三方库封装

Assets/GameScripts/DotNet/                 # Unity项目中的共享代码目录
├── Core/                                  # 核心框架层源代码（实际位置）
│   ├── Network/                           # 网络通信
│   ├── DataBase/                          # 数据库接口
│   ├── Entitas/                           # ECS架构
│   ├── EventSystem/                       # 事件系统
│   └── ...                                # 其他核心模块
└── Logic/                                 # 业务逻辑共享代码（部分）
```

### 1.2 技术栈

- **.NET版本**: 9.0
- **C#版本**: 11.0
- **服务器框架**: Fantasy（基于ETServer）
- **数据库**: MongoDB（通过IDateBase接口）
- **配置表**: Luban（生成二进制配置）
- **网络协议**: Protobuf-net
- **日志系统**: NLog

### 1.3 核心设计理念

1. **分布式架构**: 支持多服务器进程，每个服务器可以独立运行
2. **场景（Scene）驱动**: 以Scene为单位组织服务器逻辑，类似微服务架构
3. **可寻址消息（Addressable）**: 支持跨服务器消息路由，自动定位Unit位置
4. **前后端代码共享**: 通过项目链接和条件编译宏实现代码复用
   - Core框架代码位于Unity项目中（`Assets/GameScripts/DotNet/Core/`）
   - 服务器项目通过.csproj文件链接引用，实现代码共享
   - 使用 `TENGINE_NET` 和 `TENGINE_UNITY` 宏区分服务器端和客户端代码
5. **配置驱动**: 通过Excel配置表驱动服务器启动和运行

### 1.4 代码共享机制详解

TEngine Fantasy 采用**前后端代码共享**设计，核心框架代码位于Unity项目中，服务器项目通过项目链接的方式引用。

#### 1.4.1 Core框架代码共享

**源代码位置**：`Assets/GameScripts/DotNet/Core/`

**服务器项目引用方式**：`DotNet/Core/Core.csproj` 通过以下配置链接引用：

```xml
<Compile Include="..\..\Assets\GameScripts\DotNet\Core\**\*.cs">
  <Link>src\%(RecursiveDir)%(FileName)%(Extension)</Link>
</Compile>
```

**优势**：
- 前后端使用同一套框架代码，保证一致性
- 修改框架代码时，前后端自动同步
- 减少代码重复，提高维护效率

#### 1.4.2 Logic业务逻辑代码共享

**共享代码位置**：`Assets/GameScripts/DotNet/Logic/`（部分）

**服务器专用代码位置**：`DotNet/Logic/src/`

**引用方式**：`DotNet/Logic/Logic.csproj` 同时包含：
- 链接的共享代码：`Assets/GameScripts/DotNet/Logic/`
- 服务器专用代码：`DotNet/Logic/src/`

#### 1.4.3 条件编译区分

通过条件编译宏区分服务器端和客户端代码：

```csharp
#if TENGINE_NET  // 服务器端代码
    // 服务器专用逻辑
#endif

#if TENGINE_UNITY  // Unity客户端代码
    // Unity客户端专用逻辑
#endif

// 共享代码（无宏包裹）
public class SharedClass { }
```

## 二、架构层次详解

### 2.1 四层架构设计

```
┌─────────────────────────────────────┐
│   App层（应用程序入口）               │
│   位置: DotNet/App/                  │
│   - 程序启动入口                     │
│   - 命令行参数解析                   │
│   - 主循环管理                       │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   Logic层（业务逻辑层）               │
│   位置: DotNet/Logic/src/            │
│        + Assets/GameScripts/DotNet/Logic/ │
│   - 消息处理器（Handler）             │
│   - 数据模型（Model）                 │
│   - 配置加载（Config）                │
│   - 辅助工具（Helper）                │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   Core层（框架核心层）                │
│   位置: Assets/GameScripts/DotNet/Core/ │
│   （通过DotNet/Core/Core.csproj链接）│
│   - 网络通信                         │
│   - 场景管理                         │
│   - 数据库接口                       │
│   - 消息路由                         │
│   - ECS架构                          │
│   - 事件系统                         │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│   ThirdParty层（第三方库）            │
│   位置: DotNet/ThirdParty/           │
│   - Unity.Mathematics等              │
└─────────────────────────────────────┘
```

**代码共享机制说明**：
- **Core框架代码**：实际位于 `Assets/GameScripts/DotNet/Core/`，服务器项目通过 `DotNet/Core/Core.csproj` 中的链接引用：
  ```xml
  <Compile Include="..\..\Assets\GameScripts\DotNet\Core\**\*.cs">
    <Link>src\%(RecursiveDir)%(FileName)%(Extension)</Link>
  </Compile>
  ```
- **Logic共享代码**：部分业务逻辑代码位于 `Assets/GameScripts/DotNet/Logic/`，服务器专用代码在 `DotNet/Logic/src/`
- **条件编译**：通过 `TENGINE_NET` 和 `TENGINE_UNITY` 宏区分服务器端和客户端代码

### 2.2 程序集依赖关系

```
App (应用程序)
  ↓ 依赖
Logic (业务逻辑)
  ↓ 依赖
Core (框架核心)
  ↓ 依赖
ThirdParty (第三方库)
```

## 三、核心系统设计

### 3.1 启动流程

#### 3.1.1 程序入口（Program.cs）

```1:23:DotNet/App/Program.cs
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

**启动步骤说明**：
1. **App.Init()**: 初始化框架核心系统
2. **AssemblySystem.Init()**: 加载业务逻辑程序集
3. **ConfigTableSystem.Bind()**: 绑定配置表，将Excel配置转换为框架需要的配置信息
4. **App.Start()**: 启动框架，根据配置启动服务器
5. **Entry.Start()**: 业务逻辑入口（可选）
6. **主循环**: 驱动协程和单例系统更新

#### 3.1.2 启动参数

支持三种启动模式：

```14:25:DotNet/App/ProgramInfo.cs
            // 框架启动需要在命令行后面添加参数才会正常使用分别是:
            // 例如demo的服务器参数: --Mode Develop --AppType Game --AppId 100
            // Mode有两种:
            //      Develop:开发模式 这个是所有在配置定义的服务器都会启动并且在同一个进程下、方便开发调试。
            //      当然如果游戏体量不大也可以用这个模式发布，后期改成Release模式也是没有问题的
            //      Release:发布模式只会启动启动参数传递的Server、也就是只会启动一个Server
            //      您可以做一个Server专门用于管理启动所有Server的工具或脚本、一般都是运维同学来做
            // AppType有两种:
            //      Game:游戏服务器
            //      Export:导出配置表工具
            //      例如我要启动导表工具参数就应该是--AppType Export就可以了Mode和AppId都可以不用设置
            // AppId:告诉框架应该启动哪个服务器、对应ServerConfig.xls的Id 如果Mode使用的Develop的话、这个Id不生效
```

- **Mode**: `Develop`（开发模式，启动所有服务器）或 `Release`（发布模式，只启动指定服务器）
- **AppType**: `Game`（游戏服务器）或 `Export`（导出工具）
- **AppId**: 服务器ID，对应 `ServerConfig.xlsx` 中的Id

### 3.2 程序集加载系统（AssemblySystem）

```15:26:DotNet/Logic/src/Helper/AssemblySystem.cs
public static class AssemblySystem
{
    public static void Init()
    {
        LoadHotfix();
    }

    public static void LoadHotfix()
    {
        AssemblyManager.Load(AssemblyName.Hotfix, typeof(AssemblySystem).Assembly);
    }
}
#endif
```

**设计意图**：
- 支持动态加载业务逻辑程序集
- 便于热更新和模块化管理
- 框架核心与业务逻辑分离

### 3.3 配置表系统（ConfigTableSystem）

#### 3.3.1 配置绑定

```8:149:DotNet/Logic/src/Helper/ConfigTableSystem.cs
public static class ConfigTableSystem
{
    public static void Bind()
    {
        LoadConfigAsync().GetAwaiter().GetResult();
        
        // 框架需要一些的配置文件来启动服务器和创建网络服务所以需要ServerConfig.xlsx和MachineConfig.xlsx的配置
        // 由于配置表的代码是生成在框架外面的、框架没办法直接获取到配置文件
        // 考虑到这两个配置文件开发者可能会修改结构、所以提供了一个委托来让开发者开自己定义如何获取框架需要的东西
        // 本来想提供一个接口让玩家把ServerConfig和MachineConfig添加到框架中、但这样就不支持热更
        // 提供委托方式就可以支持配置表热更。因为配置表读取本就是支持热更的
        // 虽然这块稍微麻烦点、但好在配置一次以后基本不会改动、后面有更好的办法我会把这个给去掉
        ConfigTableManage.ServerConfig = serverId =>
        {
            if (!ServerConfigData.Instance.TryGet(serverId, out var serverConfig))
            {
                return null;
            }
        
            return new ServerConfigInfo()
            {
                Id = serverConfig.Id,
                InnerPort = serverConfig.InnerPort,
                MachineId = serverConfig.MachineId
            };
        };
        ConfigTableManage.MachineConfig = machineId =>
        {
            if (!MachineConfigData.Instance.TryGet(machineId, out var machineConfig))
            {
                return null;
            }
        
            return new MachineConfigInfo()
            {
                Id = machineConfig.Id,
                OuterIP = machineConfig.OuterIP,
                OuterBindIP = machineConfig.OuterBindIP,
                InnerBindIP = machineConfig.InnerBindIP,
                ManagementPort = machineConfig.ManagementPort
            };
        };
        ConfigTableManage.WorldConfigInfo = worldId =>
        {
            if (!WorldConfigData.Instance.TryGet(worldId, out var worldConfig))
            {
                return null;
            }
        
            return new WorldConfigInfo()
            {
                Id = worldConfig.Id,
                WorldName = worldConfig.WorldName,
                DbConnection = worldConfig.DbConnection,
                DbName = worldConfig.DbName,
                DbType = worldConfig.DbType
            };
        };
        ConfigTableManage.SceneConfig = sceneId =>
        {
            if (!SceneConfigData.Instance.TryGet(sceneId, out var sceneConfig))
            {
                return null;
            }
        
            return new SceneConfigInfo()
            {
                Id = sceneConfig.Id,
                SceneType = SceneType.SceneTypeDic[sceneConfig.SceneType],
                SceneSubType = SceneSubType.SceneSubTypeDic[sceneConfig.SceneSubType],
                SceneTypeStr = sceneConfig.SceneType,
                SceneSubTypeStr = sceneConfig.SceneSubType,
                NetworkProtocol = sceneConfig.NetworkProtocol,
                ServerConfigId = sceneConfig.ServerConfigId,
                WorldId = sceneConfig.WorldId,
                OuterPort = sceneConfig.OuterPort
            };
        };
        ConfigTableManage.AllServerConfig = () =>
        {
            var list = new List<ServerConfigInfo>();
        
            foreach (var serverConfig in ServerConfigData.Instance.List)
            {
                list.Add(new ServerConfigInfo()
                {
                    Id = serverConfig.Id,
                    InnerPort = serverConfig.InnerPort,
                    MachineId = serverConfig.MachineId
                });
            }
        
            return list;
        };
        ConfigTableManage.AllMachineConfig = () =>
        {
            var list = new List<MachineConfigInfo>();
        
            foreach (var machineConfig in MachineConfigData.Instance.List)
            {
                list.Add(new MachineConfigInfo()
                {
                    Id = machineConfig.Id,
                    OuterIP = machineConfig.OuterIP,
                    OuterBindIP = machineConfig.OuterBindIP,
                    InnerBindIP = machineConfig.InnerBindIP,
                    ManagementPort = machineConfig.ManagementPort
                });
            }
        
            return list;
        };
        ConfigTableManage.AllSceneConfig = () =>
        {
            var list = new List<SceneConfigInfo>();
        
            foreach (var sceneConfig in SceneConfigData.Instance.List)
            {
                list.Add(new SceneConfigInfo()
                {
                    Id = sceneConfig.Id,
                    EntityId = sceneConfig.EntityId,
                    SceneType = SceneType.SceneTypeDic[sceneConfig.SceneType],
                    SceneSubType = SceneSubType.SceneSubTypeDic[sceneConfig.SceneSubType],
                    SceneTypeStr = sceneConfig.SceneType,
                    SceneSubTypeStr = sceneConfig.SceneSubType,
                    NetworkProtocol = sceneConfig.NetworkProtocol,
                    ServerConfigId = sceneConfig.ServerConfigId,
                    WorldId = sceneConfig.WorldId,
                    OuterPort = sceneConfig.OuterPort
                });
            }
        
            return list;
        };
    }

    public static async Task LoadConfigAsync()
    {
        await ConfigLoader.Instance.LoadAsync();
    }
}
#endif
```

**设计意图**：
- **委托模式**: 使用委托而非接口，支持配置表热更新
- **配置转换**: 将Luban生成的配置表转换为框架需要的配置信息
- **核心配置**: 提供四种核心配置的绑定
  - `ServerConfig`: 服务器配置（ID、内网端口、机器ID）
  - `MachineConfig`: 机器配置（IP地址、端口）
  - `WorldConfig`: 世界配置（数据库连接信息）
  - `SceneConfig`: 场景配置（场景类型、网络协议、端口）

#### 3.3.2 配置加载器

```11:63:DotNet/Logic/src/Config/ConfigLoader.cs
public class ConfigLoader:Singleton<ConfigLoader>
{
    private bool _init = false;
    
    private Tables _tables = null!;

    public Tables Tables
    {
        get
        {
            if (!_init)
            {
                Log.Error("Config not loaded.");
            }
            return _tables;
        }
    }

    /// <summary>
    /// 加载配置。
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            _tables = new Tables();
            await _tables.LoadAsync(LoadByteBuf);
            _init = true;
        }
        catch (Exception e)
        {
            Log.Warning($"找不到游戏配置 启动项目前请运行Luban目录gen_code_bin_to_server.bat."+e.Message);
        }
    }


    /// <summary>
    /// 加载二进制配置。
    /// </summary>
    /// <param name="file">FileName</param>
    /// <returns>ByteBuf</returns>
    private async Task<ByteBuf> LoadByteBuf(string file)
    {
#if false
        GameTickWatcher gameTickWatcher = new GameTickWatcher();
#endif
        var ret = await File.ReadAllBytesAsync($"../../../Config/GameConfig/{file}.bytes");
#if false
        Log.Warning($"LoadByteBuf {file} used time {gameTickWatcher.ElapseTime()}");
#endif
        return new ByteBuf(ret);
    }
}
```

**设计意图**：
- 使用Luban加载二进制配置表
- 单例模式管理配置表实例
- 异步加载，不阻塞启动流程

### 3.4 场景（Scene）系统

#### 3.4.1 场景创建回调

```10:37:DotNet/Logic/src/OnCreateScene.cs
public class OnCreateScene : AsyncEventSystem<TEngine.OnCreateScene>
{
    public override async FTask Handler(TEngine.OnCreateScene self)
    {
        // 服务器是以Scene为单位的、所以Scene下有什么组件都可以自己添加定义
        // OnCreateScene这个事件就是给开发者使用的
        // 比如Address协议这里、我就是做了一个管理Address地址的一个组件挂在到Address这个Scene下面了
        // 比如Map下你需要一些自定义组件、你也可以在这里操作
        var scene = self.Scene;
        switch (scene.SceneType)
        {
            case SceneType.Gate:
            {
                self.Scene.AddComponent<AccountComponent>();
                break;
            }
            case SceneType.Addressable:
            {
                // 挂载管理Address地址组件
                scene.AddComponent<AddressableManageComponent>();
                break;
            }
        }
        Log.Info($"scene create: {self.Scene.SceneType} {self.Scene.Name} SceneId:{self.Scene.Id} LocationId:{self.Scene.LocationId} WorldId:{self.Scene.World?.Id}");

        await FTask.CompletedTask;
    }
}
#endif
```

**设计意图**：
- **场景驱动**: 服务器以Scene为单位组织逻辑
- **组件化**: 不同Scene类型可以挂载不同的组件
- **事件驱动**: 通过事件系统在场景创建时初始化组件

#### 3.4.2 场景类型

- **Gate**: 网关服务器，负责客户端连接和消息转发
- **Addressable**: 可寻址服务器，管理Address消息路由
- **Map**: 地图服务器，处理游戏逻辑

### 3.5 消息处理系统

#### 3.5.1 消息处理器类型

框架提供三种消息处理器：

1. **Message**: 单向消息，不需要回复
2. **MessageRPC**: RPC消息，需要回复
3. **Addressable**: 可寻址消息，自动路由到Unit

#### 3.5.2 示例：登录处理器

```9:55:DotNet/Logic/src/Handler/UserHandler/H_C2G_LoginRequestHandler.cs
    public class H_C2G_LoginRequestHandler: MessageRPC<H_C2G_LoginRequest, H_G2C_LoginResponse>
    {
        protected override async FTask Run(Session session, H_C2G_LoginRequest request, H_G2C_LoginResponse response, Action reply)
        {
            IDateBase db = session.Scene.World.DateBase;
            List<AccountInfo> result = await db.Query<AccountInfo>(
                t=>t.UserName == request.UserName && 
                        t.Password == request.Password);

            if (result.Count < 1)
            {
                response.ErrorCode = ErrorCode.ERR_AccountOrPasswordError;
                reply();
                return;
            }

            AccountInfo account = result[0];
            
            if (account.Forbid)
            {
                response.ErrorCode = ErrorCode.ERR_AccountIsForbid;
                reply();
                return;
            }

            AccountComponent accountComponent = session.Scene.GetComponent<AccountComponent>();
            if (accountComponent.Get(account.UID) != null)
            {
                response.ErrorCode = ErrorCode.ERR_AccountIsInGame;
                reply();
                return;
            }
            else
            {
                var accountInfo = session.AddComponent<AccountInfo>();
                accountInfo.UID = account.UID;
                accountInfo.SDKUID = account.SDKUID;
                accountComponent.Add(account);
            }

            Log.Debug($"收到请求登录的消息 request:{request.ToJson()}");
            response.Text = "登录成功";
            response.UID = account.UID;
            await FTask.CompletedTask;
         }
    }
```

**设计要点**：
- **数据库查询**: 使用 `IDateBase` 接口查询账号信息
- **错误处理**: 通过 `ErrorCode` 返回错误码
- **组件管理**: 使用 `AccountComponent` 管理登录账号
- **Session组件**: 在Session上挂载 `AccountInfo` 组件，标识当前登录用户

#### 3.5.3 示例：注册处理器

```7:58:DotNet/Logic/src/Handler/UserHandler/H_C2G_RegisterRequestHandler.cs
    public class H_C2G_RegisterRequestHandler: MessageRPC<H_C2G_RegisterRequest, H_G2C_RegisterResponse>
    {
        protected override async FTask Run(Session session, H_C2G_RegisterRequest request, H_G2C_RegisterResponse response, Action reply)
        {
            IDateBase db = session.Scene.World.DateBase;
            bool isSDKRegister = request.SDKUID != 0;
            
            List<AccountInfo> result = !isSDKRegister ? 
                    await db.Query<AccountInfo>(t=>t.UserName == request.UserName) : 
                    await db.Query<AccountInfo>(t=>t.SDKUID == request.SDKUID) ;

            if (result.Count == 1)
            {
                response.ErrorCode = ErrorCode.ERR_AccountAlreadyRegisted;
                reply();
                return;
            }
            else if (result.Count >= 1)
            {
                response.ErrorCode = ErrorCode.ERR_AccountAlreadyRegisted;
                Log.Error("出现重复账号：" + request.UserName);
                reply();
                return;
            }

            uint uid = await GeneratorUID(db);

            AccountInfo accountInfo = Entity.Create<AccountInfo>(session.Scene);
            accountInfo.UserName = request.UserName;
            accountInfo.Password = request.Password;
            accountInfo.SDKUID = request.SDKUID;
            accountInfo.UID = uid;

            // 等待保存完成，确保数据已写入数据库
            await db.Save(accountInfo);

            Log.Debug($"收到注册的消息 request:{request.ToJson()}");
            response.UID = uid;
            await FTask.CompletedTask;
        }

        public async FTask<uint> GeneratorUID(IDateBase db)
        {
            var ret = await db.Last<AccountInfo>(t=>t.UID != 0);
            if (ret == null)
            {
                return 100000;
            }
            return ret.UID + 1;
        }
    }
```

**设计要点**：
- **UID生成**: 自动生成递增的用户ID
- **账号验证**: 支持用户名和SDKUID两种注册方式
- **数据持久化**: 使用 `db.Save()` 保存账号信息

### 3.6 可寻址消息（Addressable）系统

#### 3.6.1 设计意图

可寻址消息系统解决了分布式服务器中的两个核心问题：

1. **客户端连接稳定性**: 客户端始终连接到Gate服务器，切换场景时不会掉线
2. **消息路由**: 自动定位Unit所在服务器，无需手动管理连接

#### 3.6.2 架构模型

```
客户端 -> Gate -> 其他服务器（Map等）
其他服务器 -> Gate -> 客户端
```

#### 3.6.3 登录Address流程

```6:45:DotNet/Logic/src/Handler/H_C2G_LoginAddressRequestHandler.cs
public class H_C2G_LoginAddressRequestHandler : MessageRPC<H_C2G_LoginAddressRequest, H_G2C_LoginAddressResponse>
{
    protected override async FTask Run(Session session, H_C2G_LoginAddressRequest request, H_G2C_LoginAddressResponse response, Action reply)
    {
        // 什么是可寻址消息
        // 此服务器是一个分布式框架、所以肯定会有多个服务器相互通信
        // 游戏里一个玩家这里统称Unit、随着游戏场景的越来越多、为了让游戏服务器能承载更多人
        // 所以大家都会把服务器给分成多个、比如一个地图一个服务器、从而能提升服务器的负载能力、也更方便开发、类似微服务
        // 但这样就会有几个问题出现了:
        // 1、客户端是直接连接到服务器、如果切换服务器的时候客户端就会掉线、怎么解决不让用户掉线
        // 2、从A服务器到B服务器后、如果能正常发送到B服务器而不是发送到A服务器中
        // 为了解决上面的问题、大多服务器架构都采用了一个中转服务器（Gate）来收发消息
        // 比如客户端一直连接的一个服务器这里统称Gate
        // 那网络通讯的管道模型是客户端->Gate->其他服务器、客户端接收消息是其他服务器->Gate->客户端
        // 这样的好处是无论玩家在什么服务器只需要改变Gate到其他服务器的连接就可以了、中间客户端是一直连接到Gate的所以客户端不会掉线
        // 这个问题解决了、但还有一个问题就是Gate跟其他服务器的连接会随着玩家的逻辑变动
        // 所以框架提供的可寻址消息（Address）消息、使用Address消息后会自动寻找到Unit的正确位置并发送到、不需要开发者再处理这个逻辑了
        // 下面就是一个例子
        // 1、首选分配一个可用、负载比较低的服务器给这个Unit、我这里就在ServerConfig.xsl表里拿一个MAP了、但实际开发过程可能比这个要复杂
        // 我这里就简单些一个做为演示、其实这些逻辑开发者完全可以自己封装一个接口来做。
        // 在ServerConfig.xsl里找到MAP的进程、看到ID是3072通过这个Id在SceneConfig.xsl里找到对应的Scene的EntityId
        var sceneEntityId = Helper.AddressableSceneHelper.GetSceneEntityId();
        
        // 2、在InnerMessage里定义一个协议、用于Gate跟Map通讯的协议I_G2M_LoginAddress
        var loginAddressResponse = (I_M2G_LoginAddressResponse)await MessageHelper.CallInnerRoute(session.Scene,
            sceneEntityId,
            new I_G2M_LoginAddressRequest()
            {
                AddressableId = session.Id,
                GateRouteId = session.RuntimeId,
            });
        if (loginAddressResponse.ErrorCode != 0)
        {
            Log.Error($"注册到Map的Address发生错误 ErrorCode:{loginAddressResponse.ErrorCode}");
            return;
        }
        // 3、可寻址消息组件、挂载了这个组件可以接收和发送Addressable消息
        session.AddComponent<AddressableRouteComponent>().SetAddressableId(loginAddressResponse.AddressableId);
    }
}
#endif
```

**流程说明**：
1. 获取目标Scene的EntityId（通过配置表查找）
2. 调用内部路由消息 `I_G2M_LoginAddressRequest` 注册Address
3. 在Session上挂载 `AddressableRouteComponent` 组件，支持可寻址消息

#### 3.6.4 Map服务器注册Address

```31:44:DotNet/Logic/src/Handler/I_G2M_LoginAddressRequestHandler.cs
    public class I_G2M_LoginAddressRequestHandler : RouteRPC<Scene,I_G2M_LoginAddressRequest,I_M2G_LoginAddressResponse>
    {
        protected override async FTask Run(Scene scene, I_G2M_LoginAddressRequest request, I_M2G_LoginAddressResponse response, Action reply)
        {
            // 现在这里是MAP服务器了、玩家进入这里如果是首次进入会有玩家的所有信息
            // 一般这个信息是数据库里拿到或者其他服务器给传递过来了、这里主要演示怎么注册Address、所以这些步骤这里就不做了
            // 这里我就模拟一个假的Unit数据使用
            // 1、首先创建一个Unit
            var unit = AddressManage.Add(scene, request.AddressableId, request.GateRouteId);
            // 2、挂在AddressableMessageComponent组件、让这个Unit支持Address、并且会自动注册到网格中
            await unit.AddComponent<AddressableMessageComponent>().Register();
            response.AddressableId = unit.Id;
        }
    }
```

**设计要点**：
- **Unit创建**: 在Map服务器创建Unit实体
- **组件注册**: 挂载 `AddressableMessageComponent` 并注册到网格
- **路由管理**: 通过 `AddressManage` 管理Unit映射关系

#### 3.6.5 可寻址消息处理器示例

```7:14:DotNet/Logic/src/Handler/Address/H_C2M_MessageHandler.cs
public class H_C2M_MessageHandler : Addressable<Unit,H_C2M_Message>
{
    protected override async FTask Run(Unit unit, H_C2M_Message message)
    {
        Log.Debug($"接收到一个Address消息 Unit:{unit.Id} message:{message.ToJson()}");
        await FTask.CompletedTask;
    }
}
#endif
```

**特点**：
- 继承 `Addressable<Unit, MessageType>`
- 自动路由到正确的Unit
- 无需手动管理服务器连接

### 3.7 数据模型系统

#### 3.7.1 AccountInfo（账号信息）

```3:39:DotNet/Logic/src/Model/AccountInfo.cs
/// <summary>
/// 账号信息
/// </summary>
public class AccountInfo : Entity
{
    /// <summary>
    /// 用户唯一ID。
    /// </summary>
    public uint UID { get; set; }
    
    /// <summary>
    /// 用户名。
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码。
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 渠道唯一ID。
    /// </summary>
    public uint SDKUID { get; set; }
    
    /// <summary>
    /// 是否禁用账号。
    /// </summary>
    public bool Forbid { get; set; }

    public override void Dispose()
    {
        Log.Debug($"UID:{this.UID}");
        this.Parent?.Scene?.GetComponent<AccountComponent>()?.Remove(this);
        base.Dispose();
    }
}
```

**设计要点**：
- 继承 `Entity`，支持ECS架构
- 自动管理生命周期，Dispose时从AccountComponent移除

#### 3.7.2 AccountComponent（账号管理组件）

```7:68:DotNet/Logic/src/Model/AccountComponent.cs
public class AccountComponent:Entity, INotSupportedPool
{
    /// <summary>
    /// 当前登陆的账号。
    /// </summary>
    public readonly Dictionary<uint, AccountInfo?> accountInfoMap = new Dictionary<uint, AccountInfo?>();

    /// <summary>
    /// 获取账号信息。
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public AccountInfo? Get(uint uid)
    {
        this.accountInfoMap.TryGetValue(uid, out AccountInfo? ret);
        return ret;
    }

    /// <summary>
    /// 添加当前登录的账号。
    /// </summary>
    /// <param name="accountInfo">账号信息。</param>
    /// <returns></returns>
    public bool Add(AccountInfo? accountInfo)
    {
        if (accountInfo == null)
        {
            return false;
        }

        if (this.accountInfoMap.ContainsKey(accountInfo.UID))
        {
            return false;
        }
        
        this.accountInfoMap[accountInfo.UID] = accountInfo;
        
        return true;
    }
    
    /// <summary>
    /// 移除当前登录的账号。
    /// </summary>
    /// <param name="accountInfo">账号信息。</param>
    /// <returns></returns>
    public bool Remove(AccountInfo? accountInfo)
    {
        if (accountInfo == null)
        {
            return false;
        }

        if (!this.accountInfoMap.ContainsKey(accountInfo.UID))
        {
            return false;
        }

        this.accountInfoMap.Remove(accountInfo.UID);
        
        return true;
    }
}
```

**设计要点**：
- 挂载在Gate Scene上，管理所有登录账号
- 使用字典快速查找账号信息
- 实现 `INotSupportedPool`，不使用对象池

## 四、配置系统

### 4.1 配置文件结构

```
Config/
├── Excel/              # Excel配置表源文件
│   └── Server/
│       ├── ServerConfig.xlsx    # 服务器配置
│       ├── MachineConfig.xlsx  # 机器配置
│       ├── SceneConfig.xlsx    # 场景配置
│       └── WorldConfig.xlsx    # 世界配置
├── Binary/             # 二进制配置（Luban生成）
├── Json/               # JSON配置（可选）
└── ProtoBuf/           # 协议定义
    ├── Outer/          # 客户端-服务器协议
    └── Inner/          # 服务器内部协议
```

### 4.2 核心配置表

#### 4.2.1 ServerConfig（服务器配置）

```74:86:DotNet/Logic/src/Generate/ConfigTable/Entity/ServerConfig.cs
    [ProtoContract]
    public sealed partial class ServerConfig : AProto
    {
		[ProtoMember(1, IsRequired  = true)]
		public uint Id { get; set; } // 路由Id
		[ProtoMember(2, IsRequired  = true)]
		public uint MachineId { get; set; } // 机器ID
		[ProtoMember(3, IsRequired  = true)]
		public int InnerPort { get; set; } // 内网端口
		[ProtoMember(4, IsRequired  = true)]
		public bool ReleaseMode { get; set; } // Release下运行        		     
    } 
```

**字段说明**：
- `Id`: 服务器唯一标识
- `MachineId`: 所属机器ID
- `InnerPort`: 内网通信端口
- `ReleaseMode`: 是否Release模式运行

#### 4.2.2 SceneConfig（场景配置）

```74:94:DotNet/Logic/src/Generate/ConfigTable/Entity/SceneConfig.cs
    [ProtoContract]
    public sealed partial class SceneConfig : AProto
    {
		[ProtoMember(1, IsRequired  = true)]
		public uint Id { get; set; } // ID
		[ProtoMember(2, IsRequired  = true)]
		public long EntityId { get; set; } // 实体Id
		[ProtoMember(3, IsRequired  = true)]
		public uint ServerConfigId { get; set; } // 路由Id
		[ProtoMember(4, IsRequired  = true)]
		public uint WorldId { get; set; } // 世界Id
		[ProtoMember(5, IsRequired  = true)]
		public string SceneType { get; set; } // Scene类型
		[ProtoMember(6, IsRequired  = true)]
		public string SceneSubType { get; set; } // Scene子类型
		[ProtoMember(7, IsRequired  = true)]
		public string NetworkProtocol { get; set; } // 协议类型
		[ProtoMember(8, IsRequired  = true)]
		public int OuterPort { get; set; } // 外网端口        		     
    } 
```

**字段说明**：
- `Id`: 场景ID
- `EntityId`: 场景实体ID（用于路由）
- `ServerConfigId`: 所属服务器ID
- `WorldId`: 所属世界ID
- `SceneType`: 场景类型（Gate、Map、Addressable等）
- `SceneSubType`: 场景子类型
- `NetworkProtocol`: 网络协议类型
- `OuterPort`: 外网端口（客户端连接端口）

### 4.3 框架配置（TEngineSettings.json）

```1:82:DotNet/App/TEngineSettings.json
{
    "Export": {
        "ProtoBufTemplatePath": {
            "Value": "../../../Config/Template/ProtoTemplate.txt",
            "Comment": "ProtoBuf生成代码模板的位置"
        },
        "ProtoBufDirectory": {
            "Value": "../../../Config/ProtoBuf/",
            "Comment": "ProtoBuf文件所在的位置文件夹位置"
        },
        "ProtoBufServerDirectory": {
            "Value": "../../../Logic/src/Generate/NetworkProtocol/",
            "Comment": "ProtoBuf生成到服务端的文件夹位置"
        },
        "ProtoBufClientDirectory": {
            "Value": "../../../../Assets/GameScripts/HotFix/GameProto/GameProtocol/",
            "Comment": "ProtoBuf生成到客户端的文件夹位置"
        },
        "ExcelProgramPath": {
            "Value": "../../../Config/Excel/",
            "Comment": "Excel配置文件根目录"
        },
        "ExcelVersionFile": {
            "Value": ".../../../../../Config/Excel/Version.txt",
            "Comment": "Excel版本文件的位置"
        },
        "ExcelServerFileDirectory": {
            "Value": "../../../Logic/src/Generate/ConfigTable/Entity/",
            "Comment": "Excel生成服务器代码的文件夹位置"
        },
        "ExcelClientFileDirectory": {
            "Value": "../../../Config/Client/Entity/",
            "Comment": "Excel生成客户端代码文件夹位置"
        },
        "ExcelServerBinaryDirectory": {
            "Value": "../../../Config/Binary/",
            "Comment": "Excel生成服务器二进制数据文件夹位置"
        },
        "ExcelClientBinaryDirectory": {
            "Value": "../../Config/Client/Binary/",
            "Comment": "Excel生成客户端二进制数据文件夹位置"
        },
        "ExcelServerJsonDirectory": {
            "Value": "../../../Config/Json/Server/",
            "Comment": "Excel生成服务器Json数据文件夹位置"
        },
        "ExcelClientJsonDirectory": {
            "Value": "../../../Config/Json/Client/",
            "Comment": "Excel生成客户端Json数据文件夹位置"
        },
        "ExcelTemplatePath": {
            "Value": "../../../Config/Template/ExcelTemplate.txt",
            "Comment": "Excel生成代码模板的位置"
        },
        "ServerCustomExportDirectory": {
            "Value": "../../../Logic/src/Generate/CustomExport/",
            "Comment": "服务器自定义导出代码文件夹位置"
        },
        "ClientCustomExportDirectory": {
            "Value": "../../Client/Unity/Assets/Scripts/Hotfix/Generate/CustomExport/",
            "Comment": "客户端自定义导出代码文件夹位置"
        },
        "SceneConfigPath": {
            "Value": "../../../Config/Excel/Server/SceneConfig.xlsx",
            "Comment": "SceneConfig.xlsx的位置"
        },
        "CustomExportAssembly": {
            "Value": "Logic",
            "Comment": "自定义导出代码存放的程序集"
        }
    },
    "Network": {
        "SessionIdleCheckerInterval": {
            "Value": 10000,
            "Comment": "每隔多久检查一个Session的对话时间"
        },
        "SessionIdleCheckerTimeout": {
            "Value": 30000,
            "Comment": "距上一次接收对话的时间如果超过设定的时间会自定断开Session"
        }
    }
}
```

**配置说明**：
- **Export**: 代码生成相关配置（ProtoBuf、Excel）
- **Network**: 网络相关配置（Session超时检查）

## 五、使用示例

### 5.1 启动服务器

#### 开发模式启动

```bash
# 使用批处理文件启动
start_develop.bat

# 或直接运行
cd Bin/App/net9.0
App.exe --Mode Develop --AppType Game --AppId 1025
```

**说明**：
- 开发模式会启动所有配置的服务器（在同一进程）
- 方便调试和开发

#### 发布模式启动

```bash
App.exe --Mode Release --AppType Game --AppId 1025
```

**说明**：
- 只启动指定ID的服务器
- 适合生产环境部署

### 5.2 创建消息处理器

#### 示例1：单向消息处理器

```csharp
#if TENGINE_NET
using TEngine.Core.Network;
using TEngine.Core;

namespace TEngine.Logic;

public class H_C2G_MyMessageHandler : Message<H_C2G_MyMessage>
{
    protected override async FTask Run(Session session, H_C2G_MyMessage message)
    {
        Log.Debug($"收到消息: {message.ToJson()}");
        // 处理消息逻辑
        await FTask.CompletedTask;
    }
}
#endif
```

#### 示例2：RPC消息处理器

```csharp
#if TENGINE_NET
using TEngine.Core.Network;

namespace TEngine.Logic;

public class H_C2G_MyRequestHandler : MessageRPC<H_C2G_MyRequest, H_G2C_MyResponse>
{
    protected override async FTask Run(Session session, H_C2G_MyRequest request, H_G2C_MyResponse response, Action reply)
    {
        // 处理请求
        response.Result = "处理成功";
        
        // 必须调用reply()才能发送响应
        reply();
        await FTask.CompletedTask;
    }
}
#endif
```

#### 示例3：可寻址消息处理器

```csharp
#if TENGINE_NET
using TEngine.Core.Network;
using TEngine.Core;

namespace TEngine.Logic;

public class H_C2M_MyAddressHandler : Addressable<Unit, H_C2M_MyAddressMessage>
{
    protected override async FTask Run(Unit unit, H_C2M_MyAddressMessage message)
    {
        Log.Debug($"Unit {unit.Id} 收到可寻址消息: {message.ToJson()}");
        // 处理消息逻辑
        await FTask.CompletedTask;
    }
}
#endif
```

### 5.3 数据库操作

```csharp
// 查询
IDateBase db = session.Scene.World.DateBase;
List<AccountInfo> accounts = await db.Query<AccountInfo>(
    t => t.UserName == "test" && t.UID > 1000);

// 保存
AccountInfo account = Entity.Create<AccountInfo>(scene);
account.UID = 100001;
account.UserName = "test";
await db.Save(account);

// 获取最后一条记录
AccountInfo last = await db.Last<AccountInfo>(t => t.UID != 0);
```

### 5.4 场景组件管理

```csharp
// 在OnCreateScene中添加组件
public class OnCreateScene : AsyncEventSystem<TEngine.OnCreateScene>
{
    public override async FTask Handler(TEngine.OnCreateScene self)
    {
        var scene = self.Scene;
        if (scene.SceneType == SceneType.Map)
        {
            // 添加自定义组件
            scene.AddComponent<MyCustomComponent>();
        }
        await FTask.CompletedTask;
    }
}

// 获取组件
var component = scene.GetComponent<MyCustomComponent>();
```

### 5.5 发送可寻址消息

```csharp
// 从客户端发送到Unit
await MessageHelper.CallAddressable(session, addressableId, new H_C2M_MyMessage());

// 从服务器发送到Unit
await MessageHelper.CallAddressable(scene, addressableId, new I_M2M_MyMessage());
```

### 5.6 内部服务器通信

```csharp
// 调用内部路由RPC
var response = (I_M2G_Response)await MessageHelper.CallInnerRoute(
    scene,
    targetSceneEntityId,
    new I_G2M_Request()
    {
        // 请求参数
    });

// 检查错误
if (response.ErrorCode != 0)
{
    Log.Error($"调用失败: {response.ErrorCode}");
    return;
}
```

## 六、关键设计意图总结

### 6.1 分布式架构设计

- **场景驱动**: 以Scene为单位组织服务器逻辑，类似微服务
- **配置驱动**: 通过Excel配置表驱动服务器启动和运行
- **自动路由**: 可寻址消息系统自动处理跨服务器消息路由

### 6.2 可扩展性设计

- **组件化**: 通过ECS架构实现组件化设计
- **事件系统**: 通过事件系统实现松耦合
- **委托模式**: 配置表绑定使用委托，支持热更新

### 6.3 开发效率设计

- **代码生成**: ProtoBuf和Excel配置表自动生成代码
- **前后端共享**: 通过条件编译宏实现代码复用
- **开发模式**: 支持开发模式，所有服务器在同一进程运行

### 6.4 性能设计

- **异步处理**: 所有消息处理都是异步的
- **协程系统**: 使用协程管理异步流程
- **对象池**: 支持对象池（Entity实现INotSupportedPool可禁用）

## 七、注意事项

1. **配置表生成**: 启动前需要运行Luban生成配置表代码和二进制文件
2. **协议生成**: 修改ProtoBuf文件后需要重新生成协议代码
3. **程序集加载**: 新增程序集需要在 `AssemblySystem` 中注册
4. **数据库连接**: 需要在 `WorldConfig` 中配置数据库连接信息
5. **消息命名**: 消息处理器命名规则为 `H_` + 消息类型 + `Handler`
6. **条件编译**: 服务器端代码需要使用 `#if TENGINE_NET` 包裹

## 八、总结

TEngine Fantasy 服务器架构采用分布式设计，通过Scene驱动、可寻址消息、配置驱动等机制，实现了高性能、可扩展的游戏服务器系统。框架提供了完整的消息处理、数据库操作、场景管理等基础设施，开发者只需关注业务逻辑实现即可。

