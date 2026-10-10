# 程序集与启动

| 路径（Unity 项目内） | 边界 |
|---|---|
| `Assets/TEngine/Runtime` | TEngine.Runtime 框架 |
| `Assets/TEngine/Editor` | TEngine.Editor 编辑器工具 |
| `Assets/TEngine.AOT` | **TEngine.AOT 主包启动器**（`GameEntry.cs`、`Procedure/`）——2026-10 合并后结构（原 `Assets/Launcher` 已随 Fantasy 合并删除） |
| `Assets/GameScripts/HotFix/Fantasy.Unity` | **Fantasy.Unity**，热更主程序集（框架集成与网络封装，`FANTASY_UNITY` 门控） |
| `Assets/GameScripts/HotFix/GameProto` | GameProto，协议和配置 |
| `Assets/GameScripts/HotFix/GameLogic` | GameLogic，业务和 UI |
| `Assets/TEngine/Extension/HtmlToUGUI` | 共用配置及 Editor 烘焙工具 |

主包（AOT，进 IL2CPP 构建）：TEngine.Runtime、TEngine.AOT。热更（解释器加载，不进主包）：Fantasy.Unity、GameProto、GameLogic、Assembly-CSharp。
不要发明 `GameScripts/Main` 或 `GameScripts.Main`。程序集事实来自 asmdef，而不是目录名"看起来像"。
GameLogic 可依赖 GameProto/Fantasy.Unity/TEngine.Runtime；底层与 GameProto 不反向引用 GameLogic。

入口：[GameEntry](../../../../Assets/TEngine.AOT/GameEntry.cs)、
[GameApp](../../../../Assets/GameScripts/HotFix/GameLogic/GameApp.cs)、
[GameModule](../../../../Assets/GameScripts/HotFix/GameLogic/GameModule.cs)。
GameApp 的热更入口是 `Entrance(object[] objects)`，首项为程序集列表。
主包通过 Procedure 和当前设置加载热更程序集，检查真实调用链，不将示例流程图当成固定状态机。
框架初始化可调用 ModuleSystem；业务层使用 GameModule。

YooAsset 收集规则、HybridCLR 列表、当前 BuildTarget 和定义符共同决定运行产物。
`.NET` 编译不验证资源地址、IL2CPP/AOT 或真机热更。
