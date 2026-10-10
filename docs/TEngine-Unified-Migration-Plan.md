# TEngine 统一迁移方案：main + Fantasy → Unity 6.3

> 文档版本：**v1.4**（2026-10-10。v1.1 增补：Fantasy.Unity 剩余部分全扫、GameServer 全量精读、pipeline 全部 147 个命令盘点、.codex references 26 篇全读、HybridCLR 官方支持确认、TMP 内置包实测、Send 影响面确认）
> **v1.2 修订（实证复核后）**：① HybridCLR 支持 6.3 的版本号与日期订正（`R1`/阶段 3.4：修复在 **8.9.0 / 2026-01-12**，非 2026-09-29）并给出可锁定的 tag；② Fantasy 基线 Unity 版本号订正 `2022.3.62f3`；③ §2.2 `.codex` 归属订正（base 已存在，非 main 独有）；④ §4 `merge-tree` 方向订正并补 `.codex` 合并实测结论；⑤ 分支名统一为 `unity6.3`、改用 remote ref 起点、补 worktree 副作用说明；⑥ 新增回滚段与阶段 4 失败判定标准；⑦ **`Assets/Launcher` 订正**（系 Fantasy 删除、main 沿用，合并自动删除，撤销阶段 1 的手工 `git rm` 步骤）；⑧ 确认 `Packages/MCPForUnity` 亦随合并自动删除；⑨ **`.codex` 清单二次订正**（`caveman`/`openspec-*`×4/`tengine-dev/evals/` 实为**删除**而非保留，首版写反，根因是照 `git diff --stat` 符号推断）
> **v1.3 修订（三目录文件系统实测复核，不依赖 git 历史）**：① §2.1 证据行号订正——三分支 `ProjectSettings.asset` 行号**并不相同**，v1.2 标注「同左各分支」系错误，补三分支行号对照表；② §2.1 宏定义订正——Fantasy 的 `ENABLE_HYBRIDCLR` 实为 **5 个平台**，非「各平台」；③ §2.1 补注 YooAsset 为 `Packages/` 本地嵌入包（三份 `manifest.json` 均无此项），版本读自 `Packages/YooAsset/package.json:4`；④ §3⑥ `GameClient.cs` 478 → **479** 行；⑤ §3⑪ Fantasy 版本补订：当前实为 **2026.0.1023**（package.json 与 Fantasy-Net 一致），非列表所记的 1007；⑥ **宏定义订正为「有工具开关、勿手工改配置」**——`ENABLE_HYBRIDCLR` / `ENABLE_LOG` / `ENABLE_OBFUZ` 分别由 `Assets/TEngine/Editor/DefineSymbols/ScriptingDefineSymbols.cs`、`LogScriptingDefineSymbols.cs`、`BuildDLLCommand.cs:19` 管理；工具覆盖 5 个平台正是 `ScriptingDefineSymbols.cs:12-19` 的 `BuildTargetGroups` 全量，PS4/PS5 等不在范围属既定取舍而非遗漏；阶段 3.3 相应改为「走 GUI / 工具，不手工编辑 `ProjectSettings.asset` YAML」；⑦ **新增 Editor 侧 Unity 6 适配扫描**（§8.16，补 §8.8 只扫运行时的缺口）与风险项 **R11**（Editor 侧 obsolete 符号 API，**已实测命中，行号完全吻合**，确认警告非阻断）、**R12**（`EditorTools.cs:738` 私有字段反射可能静默失效）；⑧ **阶段 2 实测无需执行**（`dc27547e`/`745efad9` 的改动在 main 与 Fantasy 中已各自具备，cherry-pick 报 empty，已 `--abort`）；⑨ **ISSUE-003 更新为「已就绪」**（MongoDB 容器实测连通，含选型依据与端口映射坑）；⑩ **Unity 6.3 适配实测**：`Fantasy.Unity.asmdef` 改显式引用 5 个 DLL（自动引用推断失效，见 §8.16/§9.2）；`activeInputHandler:2` 订正为 **Both**（非纯新输入系统）；minSdk 实测 **25**（23 在 6.3 不可选）；`ProjectVersion.txt` 的 revision 由 Unity 自动补全为 `e1dba0a9aba4`；⑪ **新增 §8.17 GameServer 启动实测**（与 §8⑩ 逐项吻合，注册/登录联调通过，`fantasy_main` 生成 `Account`）、**§8.18 Unity 6 API 现代化 warning 清单**与风险项 **R13**；⑫ **新增 §9.2「联调阶段修复的问题」**（NLog.config 错位、心跳未接线、连接复用挂起——三处 Fantasy 分支自身 demo 缺口，均已修复）与风险项 **R14**；⑬ ISSUE-003 补 **.NET 运行时坑**（SDK 能构建 ≠ 运行时齐备，`dotnet run` 须显式 `--framework`）
> **v1.4（2026-10-10，执行完成版）**：**Windows 全链路闭环验证通过**（合并 → 6.3 → HybridCLR → YooAsset HostPlayMode 热更拉取 → GameServer 登录）。新增 **§10 执行记录**：阶段状态总表、执行期踩坑 6 项（autoReferenced 断链、CopyStripped 死锁、Windows 260 路径 × 沙盒 Gradle、HostPlayMode URL 结构、Bee 增量构建判据、目录统一）、构建工具链改造与新编辑器菜单、**R11 清零**（15 处 obsolete API 已修）。剩余事项见 §10.4
> 基线：`origin/main` = `c06820cb`（Unity 2021.3.45f1）、`origin/TEngine_Fantasy` = `7f4d7ca2`（Unity **2022.3.62f3**，v1.2 订正）、`origin/Unity6000` = `745efad9`（Unity 6000.0.43f1）
> 共同祖先：main↔Fantasy = `4f2ec2df`；Unity6000 与两者 = `cc799ce4`
> 分析方式：全量 diff 分类 + 冲突文件逐个精读 + 核心链路全文精读 + `git merge-tree` 权威冲突模拟。所有结论附文件:行号。

---

## 1. 目标

1. Unity 项目升级到 **Unity 6.3（6000.3.25f1，本机已安装）**
2. 引入 **Fantasy 网络框架**（客户端 `Fantasy.Unity` + 服务器 `GameServer`）并完整可跑
3. **三个分支的修改与新功能全部保留**；冲突时以 Fantasy 网络功能为先，AI 工具链以 main 为准

---

## 2. 三个分支全景

### 2.1 版本与关键配置

| 项目 | main | TEngine_Fantasy | TEngine_Unity6000 |
|---|---|---|---|
| Unity 编辑器 | 2021.3.45f1 | 2022.3.62**f3**（v1.2 订正，原写 f1） | 6000.0.43f1 |
| YooAsset（`Packages/` 本地嵌入包，非 manifest 依赖） | 2.3.17 | **2.3.17（采用）** | 2.3.8 |
| companyName / productName | DefaultCompany / Unity | DefaultCompany / Unity | DinStudio / TEngine |
| bundleVersion | 1.0 | 1.0 | 0.1.0 |
| AndroidMinSdkVersion | 22 | 22 | 23 |
| AndroidTargetArchitectures | 1(ARMv7) | 1(ARMv7) | 2(ARM64，采用) |
| scriptingBackend | {}（Mono） | **Standalone: IL2CPP（采用）** | Android: IL2CPP |
| activeInputHandler | 0(旧输入) | 0(旧输入) | **2（＝ Both 新旧并存）**（v1.3 订正，原误写为「新输入系统」） |
| 宏定义（**全部有工具开关**，勿手工改配置，见下方注） | `ENABLE_LOG;TextMeshPro` | **`+ENABLE_HYBRIDCLR`（5 个平台）** | `TextMeshPro`（v1.3 订正，原写「—」） |
| ai.navigation | — | 1.1.6 | 2.0.6（6.x 采用） |
| textmeshpro | — | 3.0.9 显式 | 无（Unity6 内置） |
| HybridCLR 热更列表 | GameProto/GameLogic | **+Fantasy.Unity、Assembly-CSharp** | GameProto/GameLogic |
| Obfuz.asset | 有（enable:0） | 有（同 main） | 无 |
| 证据 | `ProjectVersion.txt:1`；`ProjectSettings.asset` 行号**三分支不同**，见下表 | 见下表 | 见下表 |

> **v1.3 订正：三分支 `ProjectSettings.asset` 行号并不相同**（v1.2 曾标注「同左各分支」，系错误）。实测：
>
> | 字段 | main | TEngine_Fantasy | TEngine_Unity6000 |
> |---|---|---|---|
> | `companyName` / `productName` | 15 / 16 | 15 / 16 | 15 / 16 |
> | `bundleVersion` | 140 | **144** | **143** |
> | `AndroidMinSdkVersion` | 168 | **174** | **177** |
> | `AndroidTargetArchitectures` | 252 | **266** | **267** |
> | `scriptingBackend` | 785 | **814** | **837** |
> | `activeInputHandler` | 883 | **917** | **931** |
>
> 各字段**取值**与上表一致，仅行号有差异——Fantasy / Unity6000 的 `ProjectSettings.asset` 因新增 `visionOSBundleVersion`、`tvOSBundleVersion` 等字段而整体下移。**引用行号时必须指明分支。**
>
> **v1.3 补注（YooAsset）**：YooAsset 是 `Packages/YooAsset/` 下的**本地嵌入包**，三份 `manifest.json` 的 `dependencies` 中均**无** YooAsset 条目；上表版本读自 `Packages/YooAsset/package.json:4`。排查包版本时不要只查 `manifest.json`。
>
> **v1.3 补注（宏定义）——这 5 个平台不是遗漏，且全部有工具开关，不需要手工改配置**：
> - Fantasy 的 `ENABLE_HYBRIDCLR` 出现在 **Android、Standalone、WebGL、Windows Store Apps、iPhone** 共 5 个平台（v1.2 写「各平台」）。**这 5 个恰为 TEngine 符号工具的管理范围**：`Assets/TEngine/Editor/DefineSymbols/ScriptingDefineSymbols.cs:12-19` 的 `BuildTargetGroups = { Standalone, iOS, Android, WSA, WebGL }`。PS4 / PS5 / EmbeddedLinux 等不在工具范围内，属 TEngine 既定取舍，**不是配置遗漏，无需手工补加**。
> - **开关方式（Unity 菜单，无需手工改配置）**：**`HybridCLR → Define Symbols → Enable HybridCLR`**（`BuildDLLCommand.cs:38` 的 `[MenuItem]`，对应方法 `:39 EnableHybridCLR()`；关闭项在同文件 `:25`）。底层：`BuildDLLCommand.cs:18` 定义 `EnableHybridClrScriptingDefineSymbol`，`:33` / `:28` 调 `ScriptingDefineSymbols.Add/RemoveScriptingDefineSymbol`；无平台参数重载（`ScriptingDefineSymbols.cs:101`）一次写入上述全部 5 个平台。`TEngineSettingsProvider.cs:77-81` 面板中亦有开关状态显示。
> - **同类菜单**：`Obfuz → Define Symbols → Enable/Disable Obfuz`（`BuildDLLCommand.cs:76` / `:66`）；`HybridCLR → Build → BuildAssets And CopyTo AssemblyTextAssetPath`（`:86`，生成并拷贝热更 / AOT DLL——阶段 4「AOT 元数据补齐」即用此项，不必手工拷贝）。
> - ⭐ **该工具已适配 Unity 6**：`ScriptingDefineSymbols.cs:138,153` 含 `UNITY_6000_0_OR_NEWER` 分支（改用 `NamedBuildTarget.FromBuildTargetGroup` 新版 API）→ 阶段 3 在 6.3 下可直接工作，无需担心符号工具失效。
> - **同类工具**（同样不必手工改配置）：`Assets/TEngine/Editor/DefineSymbols/LogScriptingDefineSymbols.cs:10-20` 管 `ENABLE_LOG` 及各级日志宏；`ProfilerDefineSymbols.cs` 管性能宏；`BuildDLLCommand.cs:19` 管 `ENABLE_OBFUZ`。
>
> **v1.3 补注（minSdk）**：表中 Unity6000 的 `23` 是该分支自身的取值，**Unity 6.3 下无法实现**——Player Settings 的 Minimum API Level 下拉最低只到 Android 7.1（API 25）。实测最终取 **25**。影响：Android 7.0 及以下设备无法安装；属 Unity 6 硬性下限，非选型失误。
>
> **v1.3 补注（activeInputHandler 取值）**：`0` = 旧 Input Manager（迁移前实测值）；`2` = **Both 新旧并存**（GUI 选 Both 后实测写入值）。`1` 应为纯新输入系统，**本次未实测，不作断言**。

### 2.2 工程结构差异

| 组件 | main | Fantasy | Unity6000 | 合并裁决 |
|---|---|---|---|---|
| `Assets/TEngine.AOT`（Procedure+Launcher+DisStripCode） | ✗ | ✓ | ✗ | **保留（Fantasy）** |
| `Assets/Launcher`（独立程序集） | 沿用 base（v1.2 订正：非 main 新增） | **删除**（并入 TEngine.AOT） | ✓ | **随合并自动删除，无需手工 `git rm`**（见 §4） |
| `Assets/GameScripts/Procedure` | ✓ | ✗（移入 TEngine.AOT） | ✓ | 随移动自动消失 |
| `Assets/…/HotFix/Fantasy.Unity`（1094 文件） | ✗ | ✓ | ✗ | 保留 |
| `GameServer/`（顶层 .NET 服务器） | ✗ | ✓（53 文件） | ✗ | 保留 |
| `Assets/csc.rsp` | ✗ | ✓ `-define:FANTASY_UNITY` | ✗ | 保留 |
| `Assets/link.xml` | ✗ | ✓ IL2CPP 裁剪保护 | ✗ | 保留 |
| `Assets/Editor/UIScriptGenerator` 双泛型 | 旧 | ✓ 扩展 | — | 取 Fantasy |
| `Packages/MCPForUnity` | ✗ 已删 | ✓（沿用 base） | ✗ | **随合并自动删除**（v1.2 实测：合并树 `Packages/` 仅剩 `UniTask`/`YooAsset`/`com.unity.pipeline` + 2 个 json），无需手工删 |
| `Packages/com.unity.pipeline`（Unity 官方 CLI） | ✓ | ✗ | ✗ | 保留（6.3 下可用） |
| `.codex/` 目录本身 | 改（新版） | 沿用 base + 新增 | 沿用 base | **v1.2 订正**：`.codex` 在共同祖先 `4f2ec2df` 已存在，非 main 独有新增；两侧差异 169 文件 / +21212 −2161，见 §4 |
| `.codex/skills/unity-cli` + `html-to-ugui`、`scripts/workflow_lib/`、`tasks/`、`templates/` | ✓ 新增 | ✗ | ✗ | 保留（main） |
| `.codex/skills/fantasy-net` | ✗ | ✓ **真正新增** | ✗ | **保留**（Fantasy 唯一带入的技能，见 §4） |
| `.codex/skills/caveman`、`openspec-*`×4、`grill-me`、`grill-with-docs`、`improve-codebase-architecture`、`tengine-dev/evals/` | **已删** | 沿用 base（未删） | — | **随合并删除**（base 有 / main 删 / Fantasy 沿用，见 §4）；属 main 的既定取舍，非本次所需 |
| `.codex/skills/tengine-dev/references/mcp-tools.md`(409 行) + `mcp-visual.md`(312 行) | 已删除 | 沿用 base（未删） | — | **随合并删除**（base 有 / main 删 / Fantasy 未改 → 删除生效，见 §4）；不残留，无需手工清理 |
| `.claude/skills/fantasy-net`、`.codex/skills/fantasy-net` | ✗ | ✓×2 | ✗ | 保留（跟 main 删 .claude 也不丢，.codex 有副本） |
| `CLAUDE.md`（UnityProject） | 已删 | 已改 | — | **冲突点，建议删（见 §9-D1）** |
| `Packages/com.unity.pipeline` 之外的 main 新包 | — | — | — | 自动并入 |

### 2.3 Git 演进关系（合并策略依据）

| 对比 | merge-base | ahead/behind（列序：**后者 / main**；v1.2 补注） | diff 规模 |
|---|---|---|---|
| main ↔ Fantasy | 4f2ec2df | +39 / +17 | 2902 files |
| main ↔ Unity6000 | cc799ce4 | **+3 / +288** | 1432 files |
| Fantasy ↔ Unity6000 | cc799ce4 | **+3 / +310**（v1.2 补测，原填「—」） | 3035 files |

**结论**：Unity6000 是停在老提交上只做 Unity 6 升级的旁支（净贡献=3 提交），不能反向 merge；main 与 Fantasy 各自演进了不同领域，`git merge-tree` 权威模拟显示**真冲突仅 2 个文件**。

---

## 3. 大功能调整深度分析（12 组）

### main 分支（17 提交）

**① AI 工具链换代：MCPForUnity → Unity CLI**（e091dfe1 → 0851b6b5 → c06820cb，2026-09-17~22）
- 过程：删 `MCPForUnity`（615 文件/82167 行，社区 MCP 包）→ 删 `.claude/`+`CLAUDE.md` → **改造并在 base 已有的 `.codex/` 上扩建**新体系（新增 skills：unity-cli/html-to-ugui + `workflow_lib/` + `AGENTS.md`；v1.2 订正：`.codex` 在 base 已存在，非从零新建）→ 加 `com.unity.pipeline`（Unity 官方 0.3.1-exp.1，582 文件）
- 机制：`HttpListener`（`BasePipelineServer.cs:41`）；Editor 端口 7800-7849、Runtime 7900-7949；**147 个命令**（分布 53 个命令文件：Editor 45 + Runtime 8，`CommandRegistry.cs:38-49` 扫描注册）；每会话 256 位 Bearer token + `confirm`/`dry_run` 双层授权；`HotReloadInPlaceILPostProcessor` 编译期织入热重载派发前导
- 闭环：`workflow.py doctor/check/verify(docs|code|unity|full)/approve/report`，退出码 0/1/2，写操作需 plan+approval 文件（`workflow.py:15-125`）
- 文档基线即 Unity 6000.0.56f1（`Books/AI-Development-Workflow.md:27`）——该体系为 Unity 6 而建；**仓库根** `AGENTS.md:25` 明确"不使用旧的规范管理工具或 Unity MCP"（v1.2 澄清：是仓库根 `AGENTS.md`，**不是** `UnityProject/AGENTS.md`，后者全文无 "MCP" 字样）

**①-补：替代关系结论（2026-10-09 确认）——`com.unity.pipeline` 1:1 替代 MCPForUnity 全部功能，接入方式升级**

| MCPForUnity 原功能 | 替代者 | 说明 |
|---|---|---|
| Editor 自动化操作（Animation/Build/Cameras/Graphics…） | `com.unity.pipeline` 的 147 个命令 | 完整超集：scene/gameobject/component/prefab/asset/animation/timeline/烘焙/NavMesh/构建/测试/控制台/eval/热重载/包管理/项目设置 |
| MCP 协议接入 | 本地 HTTP API + `unity` CLI 客户端 | 专用协议 → 通用 HTTP/命令行，任何 agent/脚本/人工均可接入 |
| 19 个客户端 Configurator（自动配置各 AI 客户端） | 无对应物，也不再需要 | 接入约定由通用格式的 `AGENTS.md` 承担 |
| （旧体系没有的） | `workflow.py` 验证门禁 | preview→approve→apply、验证分级、证据链——新体系独有 |

**四层分工（互补关系）**：
1. **执行层** `com.unity.pipeline` 包——唯一有执行能力的组件，自包含可独立运行（运行时不读 `.claude`/`.codex`/`AGENTS.md` 任何一处）
2. **客户端层** `unity` CLI（`Tools/unity.exe`）——连接 Editor 的命令行客户端；**git 仓库不含该 exe，需按 README 一行命令安装一次**（`workflow.py doctor` 会校验存在性）
3. **规程层** `.codex/skills`（4 技能，合并后 +fantasy-net 登记）——告诉 AI 何时、按何边界调用哪些命令
4. **门禁层** `.codex/scripts/workflow.py`——写操作审批与验证分级

**结论**：被删掉的 MCPForUnity 没有任何能力落空——操作被 147 命令超集覆盖，接入方式升级为通用 HTTP/CLI；唯一消失的是 19 个客户端自动配置器，其存在前提（MCP 协议）已随架构淘汰而不复存在。

**② 事件系统重构**（559506dd + eda7503b）
- EventCenter → 四层：`GameEvent`（静态门面，`GameEvent.cs:13/18`）+ `EventMgr`（接口 wrap 表 `Dictionary<Type,...>`，`EventMgr.cs:23/47`；旧字符串版直接 throw，`:64` 适配混淆）+ `EventDispatcher`（事件表+0-6 参 Send，`EventDispatcher.cs:14`）+ `EventDelegateData`（**遍历中延迟增删**：`_isExecute/_dirty` + 暂存队列，回调后 `CheckModify()` 落盘，`EventDelegateData.cs:13-16/96-116`）
- 新 API：`HasEventListener`（1-6 参）；生成器产出 `GameEventHelper` 静态分部类；全仓 `EventCenter` 0 残留

**③ 资源加载竞态防护**（a5e4c356 + 559506dd）
- `ResourceExtComponent.Resource.cs`：`LoadingState`（池化 CTS+Location，`:14-38`）；同目标新请求 → 旧请求 `Cancel`（`ReplaceLoadingState :169-176`）；await 返回后 `ReferenceEquals` 身份双检（`:82/:118`）；`_assetLoadingList` 防重复加载（`:98`）；finally 统一回收（`:138-157`）；OnDestroy 快照全取消（`:191-200`）
- `SetSprite` 增加可选 `callback`/`CancellationToken`、新增 `SetSubSprite`

**④ 日志修复**（4bc7f5ad + 8fdc3a10）：`ColorizePerLine` 逐行包裹颜色并兼容 CRLF（`DefaultLogHelper.cs:131-170`）；`FILTER_LEVEL`（`:31`）；Warning+ 追加堆栈（`:183-196`）；`LogRedirection` 预埋 `UNITY_6000_1_OR_NEWER` 分支（`:229-239`）

**⑤ 图集工具改版**（675bd119）：SpriteAtlas V2 支持、原地重生成 GUID 不变（`AtlasEditorWindow.cs:302-306` 文案）、`NeedsAtlasUpdate` 变更检测（packables+文件/meta/AtlasConfiguration 时间戳）

### Fantasy 分支（39 提交）

**⑥ Fantasy 网络集成（核心）**
- 客户端库：`HotFix/Fantasy.Unity/`（OpenUPM com.fantasy.unity v2026.0.1007 完整拷贝 + Roslyn SourceGenerator DLL）；零 Unity 引用、`allowUnsafeCode`、`defineConstraints:["FANTASY_UNITY"]` 门控
- 初始化：`Entry.Initialize` → 常驻 `FantasyObject` GameObject（每帧泵 ThreadScheduler）→ `SerializerManager`（ProtoBuf/MemoryPack/Bson）（`Platform/Unity/Entry.cs:42-63`）
- 连接：`Scene.Connect` → 协议工厂（TCP/KCP/WebSocket；**WebGL 强制 WebSocket**，`NetworkProtocolFactory.cs:87-92`）
- 消息：`Send` fire-and-forget；`Call` = rpcId + FTask RPC（`Session.cs:206-218`）；收包按 opcode 分流（Message→业务/生成 handler；Response→按 RpcId 完成 FTask）；业务 `RegisterMsgHandler` 命中即短路（`MessageDispatcherComponent.cs:174-189`）
- 反射消除：6 大 Registrar（Assembly/EntitySystem/EventSystem/EntityTypeCollection/MessageHandlerResolver/NetworkProtocol）由 SourceGenerator 生成委托数组，`AssemblyManifest.Register` 汇集——IL2CPP/热更下可用的根基
- 业务封装 `GameClient.cs`（**479 行**，v1.3 订正，原写 478）：KCP；心跳 3000/3000/4000（实际超时 6s<服务器 8s，`:69-80`）；状态机白名单放行消息（`IsStatusCanSendMsg :352-380`）；`ConnectAsync` 防重（`m_connectTask`）；协议日志（SafeToJson，忽略 Ping）
- 重连：`ClientConnectWatcher` 状态机，最多 2 次自动重连、重连前 YooAsset 版本比对
- 服务器：`GameServer/`（Fantasy.Net + NLog + Fantasy.config + 协议导出工具），独立 .NET 工程

**⑦ 启动流程 AOT 化**：`GameScripts/Procedure` → `TEngine.AOT/Procedure`（R100 纯移动）+ Launcher 一并并入 TEngine.AOT——HybridCLR 标准：加载热更 DLL 的流程自身不可热更
**⑧ UI 生成器双泛型**：`bothGeneric`/`widgetTypeName`/`dataTypeName` 五处联动（ScriptAutoGenerator/ScriptGenerateRuler/Inspector/SettingsProvider/UIBindComponent）
**⑨ 新 SourceGenerator ×2**：UIControllerGenerator（扫 `IUIController` 实现 → 生成 `UIModule.RegisterAllController` partial）、DataCenterModuleGenerator（扫 `DataCenterModule*` 基类 → 生成 `DataCenterSys.InitModule`）
**⑩ 新 UI 组件**：LoopGridView（SuperScrollView 第三方整包 + `UILoopGridWidget` 封装）、SwitchPageMgr 页签——两者当前**无业务调用**（纯新增能力，零冲突）
**⑪ Fantasy 版本升级**：Unity/Server v2026.0.1002/1003 → 1007 → **1023**（**v1.3 订正**：当前 `Fantasy.Unity/package.json` 与 `GameServer` 的 `Fantasy-Net` PackageReference 实测均为 **2026.0.1023**，v1.2 列表止于 1007，疑似漏记后续一次升级）、心跳开关

### Unity6000 分支（3 提交）

**⑫ Unity 6 适配**：`TEngine.Runtime.asmdef` +InputSystem GUID 引用 + `versionDefines` 条件宏 `ENABLE_INPUT_SYSTEM`（`dc27547e`/`745efad9`）；配置包 21 文件（`780766e2`：manifest/lock、删 boot.config/NetworkManager/AutoStreamingSettings、增 MultiplayerManager/ShaderGraphSettings、改 9 个 ProjectSettings）

---

## 4. 合并冲突权威结论

`git merge-tree --write-tree TEngine_Fantasy main` 模拟结果（**v1.2 订正**：参数顺序须与阶段 1「站 Fantasy 上 `git merge main`」同向；v1.1 写作 `main TEngine_Fantasy`，方向相反。按正确方向复跑，冲突结论不变）：

| 文件 | 类型 | 裁决 |
|---|---|---|
| `UnityProject/.gitignore` | content 冲突（同一位置：main 加 .codex 缓存忽略 / Fantasy 加 .vs） | **两段都保留** |
| `UnityProject/CLAUDE.md` | modify/delete（main 删 / Fantasy 改） | **删（见 §9-D1）** |
| `README.md` | 自动合并成功 | 合并后复核 |
| `Assets/Launcher/` vs `Assets/TEngine.AOT/Launcher/` | ~~不报冲突但语义重复（两套 LauncherMgr）~~ **v1.2 订正**：不存在两套并存——`Assets/Launcher` 在 base 已存在，**Fantasy 删除**（AOT 化）、main 沿用未改 → 3-way 合并删除生效，合并树中已无该路径 | **无需手工删除**（阶段 1 相应步骤已撤）；仅保留 `TEngine.AOT/Launcher`。**不要执行 `git rm -r Assets/Launcher`，该路径合并后已不存在，误操作可能波及 `TEngine.AOT/Launcher`** |

语义冲突排查结论：
- `EventCenter→GameEventHelper` 改名：main 引用清零；Fantasy 唯一命中在 `SourceGenerator.dll`（二进制，main 侧已更新、Fantasy 未改）→ 自动取 main 版，无风险
- Fantasy 全部新代码使用的是 `GameEvent.Get<T>()` 新 API → 无悬空引用
- main 的 Procedure 未改、Fantasy 是纯移动 → 自动生效

**`.codex` 合并实测结论（v1.2 新增，针对 169 文件 / +21212 −2161 的双向差异区）**：

以合并结果树 `e496805d834e50f2f7ad7930d51fe63bc0d0b0b8` 直接查验（非推演）：

**完整清单（逐项 `git ls-tree` 查验，含 base 对照，非 diff 符号推断）**：

| 路径 | base | main | Fantasy | 合并树 | 结论 |
|---|---|---|---|---|---|
| `.codex/skills/unity-cli` | 无 | 新增 | 无 | 存在 | ✅ 保留 |
| `.codex/skills/html-to-ugui` / `luban-dev` | 有 | 保留 | 沿用 | 存在 | ✅ 保留 |
| `.codex/skills/tengine-dev` | 有 | 改 | 沿用 | 存在（取 main） | ✅ 保留 |
| `.codex/skills/tengine-dev/agents/openai.yaml` | 无 | 新增 | 无 | 存在 | ✅ 保留 |
| `.codex/scripts/{workflow.py, workflow_lib/, requirements.txt}` | 无 | 新增 | 无 | 存在 | ✅ 保留 |
| `.codex/evals/scenarios.json` | 无 | 新增 | 无 | 存在 | ✅ 保留 |
| `.codex/tasks/20260917-workflow-modernization.md` | 无 | 新增 | 无 | 存在 | ✅ 保留 |
| `.codex/templates/task.md` | 无 | 新增 | 无 | 存在 | ✅ 保留 |
| `.codex/skills/fantasy-net` | 无 | 无 | **新增** | 存在 | ✅ 保留（Fantasy 唯一真正新增的技能） |
| `.codex/skills/caveman` | **有** | **删除** | 沿用 | **不存在** | ⚠️ **删除** |
| `.codex/skills/openspec-*`×4 | **有** | **删除** | 沿用 | **不存在** | ⚠️ **删除** |
| `.codex/skills/grill-me` / `grill-with-docs` / `improve-codebase-architecture` | **有** | **删除** | 沿用 | **不存在** | ⚠️ **删除** |
| `.codex/skills/tengine-dev/evals/` | **有** | **删除** | 沿用 | **不存在** | ⚠️ **删除** |
| `.codex/skills/tengine-dev/references/mcp-{tools,visual}.md` | **有** | **删除** | 沿用 | **不存在** | ⚠️ **删除** |

> **v1.2 二次订正**：首版此表曾把 `caveman`、`openspec-*`×4、`tengine-dev/evals/evals.json` 误记为"Fantasy 新增 → 保留"，并把 `scripts/requirements.txt`、`evals/scenarios.json`、`tengine-dev/agents/openai.yaml` 误记入"删除清单"。二者**恰好写反**：前一组是 base 已有、main 删除、Fantasy 沿用 → 删除生效；后一组是 base 无、main 新增 → 保留。根因是照 `git diff --stat` 的 `+/-` 符号下结论，而非查结果树与 base（见 `rules/merge-verification.md`）。

**合并后 `.codex` 实际形态**：`skills/` 仅剩 **5 项**——`fantasy-net`、`html-to-ugui`、`luban-dev`、`tengine-dev`、`unity-cli`；另有 `evals/`、`scripts/`、`tasks/`、`templates/`。

> 机制：①「一方删除 + 另一方沿用 base」→ 删除生效（上表 ⚠️ 行）；②「一方新增 + 另一方无」→ 保留；③「一方修改 + 另一方沿用 base」→ 取修改方（main）。故 `.codex` 虽零冲突，结果是 **main 版优先**的干净并集——**无需在阶段 1 追加手工清理**。
>
> 影响评估：`caveman`/`openspec-*` 被删是 main 的既定取舍（其 AI 体系已收敛到 `.codex` 4 技能 + `unity-cli`），**不损失本次迁移所需能力**；`fantasy-net` 是唯一需要带入的 Fantasy 技能且已保留。

---

## 5. Fantasy 网络完整性链（合并后必查清单）

| # | 项 | 位置 | 内容 |
|---|---|---|---|
| 1 | 编译符号 | `Assets/csc.rsp` | `-define:FANTASY_UNITY` |
| 2 | 程序集门控 | `Fantasy.Unity.asmdef` | `defineConstraints:["FANTASY_UNITY"]`、`allowUnsafeCode:true` |
| 3 | IL2CPP 保护 | `Assets/link.xml` | Fantasy.Unity preserve=all；Assembly-CSharp 的 6 个 `Fantasy.Generated` 类；GameLogic 的 Fantasy.Generated；NetworkProtocol |
| 4 | 程序集引用 | `GameLogic.asmdef` / `GameProto.asmdef` | 引用 GUID `0b7224b83ba514121aa026f3857f820a` |
| 5 | 热更注册 | `HybridCLRSettings.asset` + `UpdateSetting.asset` | hotUpdateAssemblies 含 `Fantasy.Unity`、`Assembly-CSharp`（双侧一致） |

启动全链路（合并后验证路径）：
`ProcedureLaunch → Splash → InitPackage(YooAsset) → InitResources → CreateDownloader → DownloadFile → DownloadOver → Preload → LoadAssembly(补 AOT 元数据 + 加载热更 DLL + 反射调 GameApp.Entrance) → StartGame` → 热更域 `GameEventHelper.Init() → GameClient.InitAsync（assembly.EnsureLoaded() 触发 Fantasy 注册 → Entry.Initialize → Scene.Create）→ LoginUI → DataCenterSys.Call(C2A_*) → 服务器 A2C_* / G2C_* 推送`
协议定义：`GameProto/Generate/NetworkProtoco/Outer{Opcode,Message}.cs`（客户端）与 `GameServer/Server/Entity/Generate/NetworkProtocol/`（服务端）同源。

---

## 6. 迁移执行方案

### 阶段 0：退路（L2）

> **v1.2 前提更正**：`TEngine/`、`TEngine_Fantasy/`、`TEngine_Unity6000/` 是**同一 git 仓库的三个 worktree**（`git worktree list` 实证），不是三个独立仓库。
> 后果：在 `TEngine/` 上切到 Fantasy 基底会使该目录由 3177 文件变为 4483 文件，**main 的本地独立副本随之消失**（可从 `origin/main` 或下方备份 tag 恢复）。
> 且 `TEngine_Fantasy` 分支正被 `TEngine_Fantasy/` 这个 worktree 占用，故起点改用 **remote ref** 规避占用问题。

```powershell
cd d:\workspace\unity\tengine_research\TEngine
git status            # 必须 clean
git fetch origin
git tag -a backup/main-before-merge -m "backup：合并前 main 基线 c06820cb"   # 用 annotated tag 而非分支：分支会被后续提交移动
git switch -c unity6.3 origin/TEngine_Fantasy
```

- 分支名统一为 **`unity6.3`**（与 §9-D3 一致；v1.1 阶段 0 曾写作 `integrate/fantasy-unity63`，已废弃）。
- 备份 tag 仅存本地（遵循 D3「不推远程」）；若需更强保障，单独 `git push origin backup/main-before-merge` 一次，与 D3 不冲突（D3 约束的是工作分支，非备份点）。

**回滚（任何阶段失败时用）**：

```powershell
cd d:\workspace\unity\tengine_research\TEngine
git merge --abort                      # 阶段 1 冲突处理中途失败
git cherry-pick --abort                # 阶段 2 中途失败
git switch main                        # 放弃 unity6.3，回到 main
# 彻底重来（会丢弃 unity6.3 上全部工作）：
# git branch -D unity6.3
# git switch main
```

### 阶段 1：合入 main（预期冲突 2 处 + 1 处语义重复）
```powershell
git merge main --no-ff
```
- `.gitignore`：两段都保留
- `CLAUDE.md`：删除（§9-D1）
- ~~合并后删除 `Assets/Launcher/` 整目录~~ → **v1.2 撤销该步骤**：合并会自动删除（详见 §4），执行 `git rm` 反而会报错
- **v1.2 新增核验**：`Packages/MCPForUnity` 已随合并自动删除（合并树 `Packages/` 仅剩 `UniTask`、`YooAsset`、`com.unity.pipeline` + 两个 json），无需手工删
- 复核 README 自动合并；按 §5 清单核验五件套

### 阶段 2：应用 Unity6000 的 Unity6 适配 —— **v1.3 订正：实测无需执行，已取消**

**实际执行结果（2026-10-10）**：`git cherry-pick dc27547e 745efad9` → 第一个 commit 报 `The previous cherry-pick is now empty`（补丁应用后无变化）；随即 `git cherry-pick --abort` 取消，分支保持在 `93963356`，工作区 clean。

**原因（实测查证，非推断）**：这两个 commit 的改动**在合并后的 `unity6.3` 上已经存在**。

| 对象 | `GUID:3fe1a3e7…`（InputSystem 引用） | `versionDefines` → `ENABLE_INPUT_SYSTEM` |
|---|---|---|
| 共同祖先 `cc799ce4` | ❌ 无 | ❌ 无 |
| `main` | ✅ 有 | ✅ 有 |
| `TEngine_Fantasy` | ✅ 有 | ✅ 有 |
| `Unity6000`（cherry-pick 源） | ✅ 有 | ✅ 有 |

即：base 没有，但**三个分支各自平行做了同一改动**（convergent change）。`dc27547e` / `745efad9` 是 Unity6000 基于老 base 重做一遍，而 main 与 Fantasy 早已具备——**合并 main 后天然就带上了**。

合并后实测（`TEngine.Runtime.asmdef`）：
- `:6` `"GUID:3fe1a3e70da50184f9897101cad7e4f2"` ← `dc27547e` 想加的
- `:25` `"define": "ENABLE_INPUT_SYSTEM"` ← `745efad9` 想加的

**教训**：§3⑫ 曾把这两个 commit 当作 Unity6000 的"净贡献"。判断"某改动为某分支独有"**必须查 merge-base**，不能只看两分支 diff——见 `rules/merge-verification.md`。§2.3 称 Unity6000「净贡献=3 提交」同样未排除这类重复劳动。

`780766e2` 仍不 cherry-pick（基于 6000.0），其配置变更由阶段 3 的编辑器自动升级覆盖，仅参考删除项（boot.config / NetworkManager.asset / AutoStreamingSettings.asset）。

### 阶段 3：升级 Unity 6.3（6000.3.25f1）
1. `ProjectVersion.txt` → `6000.3.25f1 (97272b72f107 对应 6000.0 的 revision，6.3 的 revision 以本机安装为准)`
2. `manifest.json`：删 `com.unity.textmeshpro` 显式依赖（Unity6 内置）与 `com.unity.ide.vscode`；`ai.navigation` 升至 6.3 对应版本；其余保留
3. **Player Settings —— 优先走 Unity GUI / TEngine 设置面板，不要手工编辑 `ProjectSettings.asset` YAML**：`scriptingBackend` 取 Fantasy 基础（Standalone IL2CPP）、`activeInputHandler:2`、Android ARM64、**minSdk 25**（v1.3 订正：原写 23，实测 Unity 6.3 下拉菜单最低只到 Android 7.1＝API 25，23 已不可选）；`ENABLE_HYBRIDCLR` 用工具开启（见下）
   - **宏用工具开**：`BuildDLLCommand.cs:18,33`（或 TEngine 设置面板），一次写入 Standalone / iOS / Android / WSA / WebGL 5 个平台。`ScriptingDefineSymbols.cs:138,153` 已含 `UNITY_6000_0_OR_NEWER` 分支，6.3 下可用
   - **同类工具**：`Assets/TEngine/Editor/DefineSymbols/LogScriptingDefineSymbols.cs`（`ENABLE_LOG` 及各级日志宏）、`ProfilerDefineSymbols.cs`、`BuildDLLCommand.cs:19`（`ENABLE_OBFUZ`）——**这些宏都不必手工改配置**
   - **为什么不手工改 `ProjectSettings.asset`**：① YAML 缩进 / 字段顺序 / 版本专有字段改错，会导致 Unity 打开时静默重置或报错；② 三分支行号不同（见 §2.1），无法对照；③ 上述各项在 Player Settings GUI 中均有对应控件
4. **锁定 HybridCLR 版本**（属 L2 依赖变更）——**v1.2 全面订正**

   **当前状态（风险）**：`Packages/manifest.json:3` 为 `https://gitee.com/focus-creative-games/hybridclr_unity.git`，**无 `#tag` 后缀**，是浮动 git HEAD；`packages-lock.json` 仅记录解析到的 commit `03083ad8f96253f84ed4cf2018c5f32690e29b7b`。→ 版本不可重现，"要用哪个版本"目前无法在仓库中表达。

   **版本事实**（hybridclr_unity `RELEASELOG.md` 原文核验）：

   | 版本 | 发布日 | 与 6.3 的关系 |
   |---|---|---|
   | **8.9.0** | **2026-01-12** | ✅ 含两条 Unity 6.3.x 专属修复：`GlobalMetadata::GetNestedTypes` 未处理 interpreter type；`GetMethodHandleFromMethodInfo` 在 setup vtable 时可能传入 null `methodInfo` 导致崩溃。**最低可用版本** |
   | **8.15.0** | 2026-09-29 | ✅ 含 `feat: support 6000.5.x`（覆盖 6000.3）。**推荐锁定** |
   | 9.0.1 | 2026-10-09 | 跨大版本且发布仅 1 天，本次不采用 |

   > v1.1 把两条 6.3.x 修复的日期误记为「2026-09-29」——该日期实为 8.15.0 的发布日，修复本身在 **8.9.0（2026-01-12）**。tag 命名统一为 `vX.Y.Z`（带小写 `v`）。

   **执行**：把 `manifest.json` 依赖改为带 tag 的固定引用：

   ```json
   "com.code-philosophy.hybridclr": "https://gitee.com/focus-creative-games/hybridclr_unity.git#v8.15.0"
   ```

   **关于 il2cpp_plus**：它**不在** `manifest.json` 中，由 HybridCLR installer 按 hybridclr_unity 的包版本自动匹配并写入本地 Unity 安装目录，**无需单独指定版本**（v1.1 将二者并列，易误导执行者去 manifest 里找 il2cpp_plus）。
5. 用 Unity 6.3 打开工程，让编辑器自动完成剩余序列化升级；逐个修复编译报错
6. 验证 `FANTASY_UNITY` 门控仍生效（csc.rsp 未被覆盖；FantasySettingsProvider 只在手动点击时装/卸符号，不会自动改写）
7. 执行 Fantasy Settings → **Generate link.xml**（IL2CPP 打包前必须：心跳/Timer 反射链路依赖它，缺失时运行时直接报错，`UnitySessionHeartbeatComponent.cs:83`）

### 阶段 4：验收（DoD）
- [ ] Unity 6.3 打开零编译错误
- [ ] §5 五件套逐项核验
- [ ] 客户端连接 GameServer 登录/注册流程实测通过
- [ ] HybridCLR 热更全链路（含热更 DLL 加载、AOT 元数据补齐）
- [ ] Android（ARM64/IL2CPP）构建通过
- [ ] 新输入系统下 TEngine 输入封装行为验证（**R5**：先切 Both 再实测；不通过则须单独立项改造，勿夹带在其他改动中）
- [ ] 安装 `unity` CLI 客户端（README 一行命令，git 不含 exe，一次性）
- [ ] `workflow.py doctor` 通过（AI 工具链在 6.3 下可用）
- [ ] Fantasy Settings → Generate link.xml 已执行（IL2CPP 前置）
- [ ] GameServer 联调：.NET 8/9 SDK + 本机 MongoDB → `dotnet build Server.sln` → `dotnet run --project Main`（监听 20001/20002/20003 + 20051/20052）→ 客户端 LoginUI 注册/登录实测
- [ ] 合并后同步 AI 文档（main 侧盲区）：`tengine-dev/references/architecture.md` 程序集表补 `TEngine.AOT`、`hotfix-workflow.md` 热更边界表述、`troubleshooting.md` 主包/热更版本配对表述
- [ ] **（v1.2 新增）HybridCLR 依赖已 pin 为 `#v8.15.0`，且 `packages-lock.json` 已重新解析**
- [ ] **（v1.2 新增）核验 `.codex` 合并结果**：`skills/` 恰为 5 项（`fantasy-net`、`html-to-ugui`、`luban-dev`、`tengine-dev`、`unity-cli`）；**无** `mcp-tools.md`/`mcp-visual.md`/`caveman`/`openspec-*`
- [ ] **（v1.3 新增）Editor 工具在 6.3 下实测**：`HybridCLR → Define Symbols → Enable HybridCLR` 菜单可用；`Fantasy Settings` 面板可正常开关符号（R11）；**本地化工具未静默失效**（R12——私有反射失效不报错，须主动点开验证）

> **GameServer 联调项当前被阻塞**：本机无 MongoDB 服务（ISSUE-003），"注册/登录实测"需先装 MongoDB 或起 Docker 实例。该阻塞不影响合并与 Unity 编译验证。

**失败判定与回退标准（v1.2 新增）**：

| 触发条件 | 处置 |
|---|---|
| 阶段 1 冲突超出 §4 已知的 2 个文件 | 停止 → `git merge --abort` → 回本文档补裁决后再来 |
| 阶段 3 打开 Unity 6.3 后编译错误 > 20 个，或集中落在 HybridCLR / YooAsset 等第三方包 | 判定为包版本不适配 → 退回阶段 3 起点重选版本；**不要逐个硬改第三方源码** |
| `FANTASY_UNITY` 符号失效或 `link.xml` 无法生成 | 停止后续打包，先修符号链（§5 五件套） |
| 阶段 4 任一 DoD 项失败 | 不得交付；明确记录未通过项，不略过不提 |

---

## 7. 风险与待验证项（代码无法判定，需实测）

| # | 项 | 说明（v1.1 已查证） | 缓解 |
|---|---|---|---|
| R1 | HybridCLR × Unity 6.3 | ~~v1.1：RELEASELOG 2026-09-29 含两条修复~~ **v1.2 订正**：官方已支持，两条 6.3.x 修复在 **8.9.0（2026-01-12）**；2026-09-29 实为 8.15.0 发布日（含 `support 6000.5.x`） | **已收敛为可执行动作**：`manifest.json` 显式 pin `#v8.15.0`（阶段 3.4），小工程验证 |
| R2 | MemoryPack × Unity 6.3 IL2CPP | 净 DLL（deps.json 标 1.0.0/netstandard2.1，依赖 `System.Runtime.CompilerServices.Unsafe 6.0.0`）；AOT 泛型靠手写静态 Formatter 注册（`MemoryPackHelper.cs:52-55`）+ link.xml 兜底 | 打包验证 + 确保已生成 link.xml |
| R3 | TMP × `UniTask.TextMeshPro` | Unity 6.3 内置 `com.unity.textmeshpro` **5.0.0**（实测 BuiltInPackages）；UniTask 用**程序集名称引用** + 双 versionDefines（`com.unity.textmeshpro` 或 `com.unity.ugui 2.0.0` 任一存在即定义 UNITASK_TEXTMESHPRO_SUPPORT）→ 兼容机制完好 | TMP 3.0→5.0 API 差异编译验证。**v1.3 实测已命中**：`Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBaker.cs` 多处报 `TMP_Text.enableWordWrapping` 已弃用（应改 `textWrappingMode`），确认系 TMP 5.0 变化；仅警告，编译与运行正常 |
| R4 | `GameClient.Send` rpcID 传参 | **影响面=0**：GameLogic 中 `GameClient.Instance.Send(` 调用点 0 个（仅 2 处 `Call`）→ 属潜在 API 陷阱而非现行 bug（`GameClient.cs:397` 应传 `message.OpCode()`）。**v1.2 复核：全仓 `GameClient.Instance.Send(` 调用点确为 0** | **维持 D4：本次暂不修**，登记 ISSUE-001；触发条件（业务首次使用 `GameClient.Send`）满足前必须修 |
| R5 | TEngine 输入封装新输入系统完整性 | 条件块存在 ≠ 全覆盖（main 3 处 + Fantasy 1 处） | **v1.3 补充：本条是方案缺口**——原仅安排「PlayMode 实测」，**未为"实测不通过需要改代码"预留任何阶段或工作量**。缓解三层：① 阶段 3.3 切换时先选 **Both**（新旧并存）而非直接 New，降低实测失败概率并留缓冲；② PlayMode 实测（阶段 4 验收项）；③ **若实测不通过，需新增「TEngine 输入封装适配」改造项——单独报备、单独验收，不得夹带在其他改动中** |
| R6 | main 版 `SourceGenerator.dll` 与 Fantasy 生成器产出一致性 | 二进制不可 diff | 编译产物行为验证 |
| R7 | `FANTASY_UNITY` 门控稳定性 | FantasySettingsProvider **只在手动点击时装/卸** ScriptingDefineSymbols（`FantasySettingsProvider.cs:906,924`），不会自动写 csc.rsp（全 Editor 目录 0 命中） | 升级后确认 csc.rsp 仍在 |
| R8 | GameServer 生产化差距 | 服务器为 demo 级：账号明文入库/比对（`AuthenticationComponentSystem.cs:36/104`）、Gate 场景无业务逻辑、`A2C_LoginResponse.Token` 未赋值 | 联调可用；生产化改造超出本次范围 |
| R9 | **6.0 → 6.3 的 minor 跨越与包版本依据缺失**（v1.2 新增） | Unity6000 分支停在 `6000.0.43f1`，目标是 `6000.3.25f1`，跨 3 个 minor。阶段 2 仅 cherry-pick 2 个 asmdef commit，配置升级全交"编辑器自动升级"；YooAsset 2.3.17、`ai.navigation`、HybridCLR 在 6.3 下的**具体版本号无锁定依据** | 阶段 3 对每个包显式写死版本（HybridCLR 已定 `#v8.15.0`）；其余在 Unity 6.3 打开后按 Package Manager 实际解析值回填本文档 |
| R10 | **worktree 副作用**（v1.2 新增） | 三目录是同一仓库的 3 个 worktree，在 `TEngine/` 切基底会使 main 本地副本消失（3177 → 4483 文件） | 阶段 0 建 annotated 备份 tag + 改用 `origin/TEngine_Fantasy` 为起点；回滚命令见阶段 0 |
| R11 | **Editor 侧 5 处 obsolete 符号 API**（v1.3 新增，**已实测命中，行号完全吻合**） | `FantasySettingsProvider.cs:864,906,924` 与 `Localization/UpgradeManager.cs:115,136` 直接调 `PlayerSettings.Get/SetScriptingDefineSymbolsForGroup`。该 API 在 Unity 6000.3 官方脚本文档中标 **Obsolete（Deprecated，未移除）**，仍可正常工作 | **警告，非阻断**：只产生弃用警告；`Assets/csc.rsp` 无 `-warnaserror`，不会升级为编译错误。升级后若要消除警告，改用 `NamedBuildTarget` 新 API——可直接复用**已适配**的 `ScriptingDefineSymbols.cs:138,153` |
| R12 | **`EditorTools.cs:738` 私有字段反射**（v1.3 新增） | 反射取 `EditorGUI.s_RecycledEditorInternal`（`BindingFlags.NonPublic \| Static`）。虽有 `UNITY_6000_0_OR_NEWER` 分支，但反射私有字段不保证跨版本稳定 | 6.3 下若该字段改名会**静默失效**（不报错、功能无效果，最危险的一类）。升级后须实测一次本地化工具；失效则改用官方 API 或按 6.3 实际字段名调整 |
| R13 | **Unity 6 API 现代化 warning 清单**（v1.3 新增，**全部为警告、0 error，登记备查不在本次迁移范围**） | 编译通过后实测收集（详见 §8.17）：`YooAsset` 的 `UxmlFactory`/`UxmlTraits` 已弃用；`HtmlToUGUIBaker.cs` 的 `TMP_Text.enableWordWrapping`（R3 命中）与 `FindObjectOfType`；`GameModule.cs:14` 的 `FindObjectOfType`；`EditorTools.cs:443` 的 `HierarchyProperty`；`SBPBuildTasks.cs:32` 的 `CreateBuiltInShadersBundle` | **不动**。均为 Unity 6 的 API 演进，短期不会失效。按《决策篇》§六禁止过度设计，不在迁移途中顺手清理；若后续统一治理，须单独立项、单独验收 |
| R14 | **Fantasy 客户端两处 demo 缺口**（v1.3 新增，**已修复**） | ① `OnConnectComplete` 未调用 `StartHeartbeat()`——`StartHeartbeat` 在全 HotFix 目录**只有定义、零调用点**；服务器 `idle timeout=8000ms` 因此主动断开。② `ConnectAsync` 在 `Scene.Session` 已存在时创建了 `m_connectTask` 却跳过 `Scene.Connect`，导致 `await` **永久挂起**，后续请求全被防重分支拦截（注册后无法登录） | **已修**（提交 `1a273c74`）：① 连接成功后启动心跳；② Session 可用且目标地址一致时直接复用返回，地址不同先 `Disconnect` 再新建。**均属 Fantasy 分支原有缺口，非迁移引入**。详见 §9.2 |

---

## 8. 深挖附录：已知问题与特性

1. `EventMgr.RegWrapInterface` 旧字符串重载直接抛异常（`EventMgr.cs:64-68`）——为代码混淆适配的刻意设计
2. `EventDelegateData` 遍历中增删走暂存队列，回调结束统一落盘——事件回调内可安全 Add/Remove
3. Fantasy `RegisterMsgHandler` 命中后短路生成器 handler（`MessageDispatcherComponent.cs:174-189`）——业务回调优先
4. Fantasy 默认心跳 2000/30000ms，GameClient 覆盖为 3000/3000/4000ms（快于服务器 8s 检测）
5. WebGL 下 Fantasy 强制 WebSocket（`NetworkProtocolFactory.cs:87-92`）——含 WebGL 目标时不能选 KCP
6. SwitchPageMgr / LoopGridView 当前无业务调用方，属"提供能力、待接线"
7. main 的 AI 技能文档已与新事件命名同步（`tengine-dev/references/event-system.md:13` 写 `GameEventHelper.Init()`）
8. **Fantasy.Unity 剩余全扫**（v1.1）：FTask 为 ThreadStatic 池化自研 async（MaxPoolSize 2000，与 UniTask 无命名冲突但不能在同一方法内混 await）；Entitas 系统接口 8 个，Entity/组件共用池（容量 4096，池空走生成器工厂免反射）；unsafe 集中于 LightProto/包解析/KCP/Helper（26 文件）；Unity 版本敏感 API 全扫描：运行时 0 风险，唯一 `UNITY_6000_0_OR_NEWER` 分支在 `WSocket/Implementation/WebGL/WebSocketManager.cs:79`——**框架已明确适配 Unity 6**
9. WebGL：`WebSocket.jslib` 内置 `support6000` 分支（:28,35-37），无 WWW/老 Network API
10. **GameServer 全貌**（v1.1）：.NET 8/9 双目标 + Fantasy-Net 2026.0.1023（NuGet）+ NLog 6.1.4 + MongoDB `fantasy_main`；单机 3 进程 5 Scene（Authentication :20001 KCP；Gate :20051/20052 已在拓扑但无业务逻辑）；客户端与服务器协议文件逐字节同源（`C2A_LoginRequest=268445458` 两侧一致）；Hotfix 经可卸载 AssemblyLoadContext 加载，支持热重载；服务器 Luban 目录仅运行时支撑库、无业务配置表
11. **pipeline 实际 147 个命令**（53 个命令文件，Editor 45 + Runtime 8）；需 `confirm=true` 的显式破坏性命令 13+（8 个 `set_*_settings`、`delete_asset`、3 个 `clear_*` bake 清理等）；`eval`/`eval_file` 等价任意 C# 执行；capture 类写 PNG 不在只读白名单
12. **AI references 一致性**（26 篇全读）：`event-system.md` 与 GameEventHelper 新代码一致、无过时；**`TEngine.AOT` 是 main 文档盲区**——合并后必须同步 architecture.md 程序集表、hotfix-workflow.md 边界、troubleshooting.md 配对表述。**v1.2 实测追加**：合并结果树中 `event-system.md`/`architecture.md` 的 blob 与 main 版逐位相同（Fantasy 沿用 base 旧版被覆盖），故上述盲区在合并后**仍然存在**，§9 同步项照旧执行；同时 `mcp-tools.md`/`mcp-visual.md` 未回归（见 §4）
13. `AssetCommands.cs:52-66` 已含 `UNITY_6000_0_OR_NEWER` 的 PhysicMaterial/PhysicsMaterial 双拼写分支——pipeline 包已为 Unity 6 做版本分叉
14. Fantasy.Unity `package.json`：version **2026.0.1023**、无 `unity` 字段；代码级最低版本声明 `UNITY_2022_3_OR_NEWER`（`CheckUnityVersion.cs:11-12`）→ Unity 6.3 不会被版本拦截
15. `GameEvent.cs` 全文（810 行）确认：门面为纯转发（int/string 双套重载 + `Get<T>()` :585 + `Shutdown()` :805），无隐藏逻辑
16. **Editor 侧 Unity 6 适配扫描**（v1.3 新增——§8.8 只扫了**运行时**，本条补 Editor 缺口）
    - `Assets/GameScripts/HotFix/Fantasy.Unity/Editor/`（**9 文件 / 2252 行**）的 `UNITY_6000` 条件编译为 **0 处**；但废弃 API 扫描未发现 6.x 下会失效的调用——所用 `UnityEngine.Networking`（`UnityWebRequest`）在 6.x 正常，无 `WWW` 类、无已删除的老 `Network` API
    - `Assets/TEngine/Editor/` 共 **4 处**适配：`ScriptingDefineSymbols.cs:138,153`（符号 API，**已改用 `NamedBuildTarget.FromBuildTargetGroup` 新版 API**）、`Localization/EditorTools.cs:433`（`UNITY_6000_5_OR_NEWER`）、`:738`
    - 未适配但仅 obsolete 的两处见 **R11**；私有反射风险见 **R12**
    - **结论：Editor 侧预期可编译**。真正无法从本仓代码判定的是 **hybridclr 包 API**——`BuildDLLCommand.cs` 调用的 `HybridCLR.Editor.SettingsUtil`、`CompileDllCommand`、`SettingsUtil.GetAssembliesPostIl2CppStripDir` 全部来自该包，其形态取决于阶段 3.4 锁定的 `v8.15.0`
17. **GameServer 启动实测（v1.3 新增，阶段 4 联调通过）**
    - 启动命令：`cd GameServer\Server` → `dotnet build Server.sln` → `dotnet run --project ..\Main --framework net8.0`（路径与框架见下方注）
    - 实测输出与 §8⑩ **逐项吻合**：
      - `Fantasy 2026.0.1023 Official version` ✅
      - `dbName:fantasy_main Database connection successful.` ✅（MongoDB 容器连通）
      - **3 进程 5 Scene**：Process 1 = Scene 1001/1002/1003（Authentication），Process 2001 = Scene 1051（Gate），Process 2002 = Scene 1052（Gate）
      - 端口：Outer KCP `20001/20002/20003` + `20051/20052`；Inner TCP `11001/11002/11003` + `11051/11052`
      - `Current session idle timeout:8000`、`Session-check interval:5000` ← **这两个值直接导致了 R14 的断连问题**
    - **联调结果**：注册 → 登录全链路通过；`fantasy_main` 库生成 `Account` collection（硬证据）
    - ⚠️ **.NET 运行时坑（已补入 ISSUE-003）**：`Main.csproj` 为 `net8.0;net9.0` 双目标，`dotnet run` 默认挑 net9.0，若本机无 9.0 运行时则报 "You must install or update .NET"；实测本机有 8.0.0 与 10.0.11，**指定 `--framework net8.0` 即可**，无需安装
18. **Unity 6 API 现代化 warning 清单**（v1.3 新增，对应 R13，**全部为警告、不计入 error**）
    - `Packages/YooAsset/Editor/UIElements/*`（`TableViewer`、`TreeViewer`、`ReorderableListView`、`ResizeHandle`、`ToggleDisplay`、`ToggleFoldout`、`ToggleRecord`）：`UxmlFactory` / `UxmlTraits` 已弃用 → 改用 `UxmlElementAttribute`
    - `Assets/TEngine/Extension/HtmlToUGUI/Editor/HtmlToUGUIBaker.cs`：`TMP_Text.enableWordWrapping` → `textWrappingMode`（**R3 实测命中**）；`FindObjectOfType` → `FindFirstObjectByType` / `FindAnyObjectByType`
    - `Assets/GameScripts/HotFix/GameLogic/GameModule.cs:14`：`FindObjectOfType` 已弃用
    - `Assets/TEngine/Editor/Localization/EditorTools.cs:443`：`HierarchyProperty` → `HierarchyIterator`
    - `Packages/YooAsset/Editor/AssetBundleBuilder/.../SBPBuildTasks.cs:32`：`CreateBuiltInShadersBundle` → `CreateBuiltInBundle`
    - 另：GameServer 侧 `Server/Entity/Luban/`（SimpleJSON、ByteBuf）产生大量 CS86xx 可空性警告——**第三方生成代码，非本项目业务代码，忽略**

---

## 9. 决策记录（ADR）

| ID | 决策 | 理由 | 状态 |
|---|---|---|---|
| A1 | 以 Fantasy 为合并基底，而非 main | 网络功能是核心目标且完整；main 仅领先 17 提交；Unity6000 落后 288 提交不可反向 merge | 已定 |
| A2 | AI 工具链以 main 为准（删 MCPForUnity，保留 com.unity.pipeline） | main 明确禁用 Unity MCP（**仓库根** `AGENTS.md:25`）；官方包要求 ≥2022.3，6.3 下正好启用。**v1.2 补充：合并后 `.codex` 中 MCP 文档不回归，A2 仍成立（实测见 §4）** | 已定 |
| A3 | YooAsset 用 2.3.17、输入系统用新（activeInputHandler:2）、Android 用 ARM64 | 取更先进的分支值；与 Unity 6.3 默认方向一致。**v1.3 实测订正：`2` 是 Both（新旧并存），不是纯新输入系统——在 GUI 选 Both 后写入的正是 `activeInputHandler: 2`** | 已定 |
| D1 | CLAUDE.md 删除 | AI 指导已由 AGENTS.md+.codex+pipeline 承接；fantasy-net skill 在 .codex 有副本不丢 | **已定（2026-10-09 用户确认：删除）** |
| D2 | 只保留 `TEngine.AOT/Launcher`，不要 main 版 `Assets/Launcher/` | 两者内容同源重复定义；TEngine.AOT 版与 Fantasy 的 AOT 化启动流程一致 | **已定（2026-10-09 用户确认：删 main 版）**；**v1.2 补充：裁决不变，但落地方式改为「自动」**——`Assets/Launcher` 在 base 已存在、Fantasy 删除、main 沿用未改，3-way 合并自动删除（实测合并树无此路径），**阶段 1 无需手工 `git rm`** |
| D3 | 合并结果落地方式 | **用户确认：在本地创建新分支 `unity6.3`，不推远程**；tag 规则 `vX.Y.Z：<摘要>` 留作后续备用 | **已定** |
| D4 | `Send` rpcID 传参修复 | 已确认 GameLogic 中 `Send` 调用点=0（无现行影响） | **已定（用户确认：暂不修，登记备查，详见 §9.1）** |
| D5 | GameServer 随合并自动带入 | 独立 .NET 工程，与 Unity 工程无合并耦合；联调需 .NET 8/9 SDK + 本机 MongoDB | 已定（自动带入） |

### 9.1 遗留问题登记（暂不修复，备查）

**ISSUE-001：`GameClient.Send` 状态门传参错误**
- 位置：`GameClient.cs:397`（`Send<T>` 方法内）
- 现状：`IsStatusCanSendMsg(rpcID)` 传入的是 **rpcID 参数（默认 0）**，而非消息协议码；对照 `Call` 侧的正确写法 `IsStatusCanSendMsg(request.OpCode())`（`:417`）
- 影响：**当前影响面=0**（GameLogic 全仓 `GameClient.Instance.Send(` 调用点 0 个，仅 2 处 `Call`）；但一旦业务在 `StatusLogin`/`StatusRegister` 态调用 `Send`，会因 `rpcID=0` 不匹配任何白名单 opcode 而被状态机误拦并刷错误日志（`GameClient.cs:369-377`，5 秒节流）
- 修复方案：`Send<T>` 内改为 `IsStatusCanSendMsg(message.OpCode())`
- 触发条件：业务首次使用 `GameClient.Send` 发送网络消息时必须先修此处
- 决策：2026-10-09 用户确认暂不修，登记备查

**ISSUE-002：`.claude` 目录合并后残留**
- 现状：merge 模拟确认——main 删除 base 时代 `.claude` 下 77 个文件，但 Fantasy 在 `.claude/skills/fantasy-net` **新增**的 83 个文件不会被对方的删除覆盖 → 合并后 `.claude/skills/fantasy-net`（83 文件）残留
- 依据：D1 决策精神（AI 配置统一到 `.codex`）；`.codex/skills/fantasy-net` 存在内容相同的 83 文件副本，删 `.claude` 副本不丢能力
- 建议处理（**已定，2026-10-09 用户确认：删除**），执行时落实两个动作：
  1. 合并提交中 `git rm -r UnityProject/.claude`（`.codex/skills/fantasy-net` 有相同 83 文件副本，不丢能力）
  2. 在根 `AGENTS.md` 技能菜单表补一行：`| Fantasy 网络开发 | [fantasy-net](UnityProject/.codex/skills/fantasy-net/SKILL.md) |`——让所有 agent（含 Claude Code）都能发现该技能，弥补删 `.claude` 后失去的自动发现
- 依赖澄清：`com.unity.pipeline`（unity cli）运行时不读取 `.claude`/`.codex`/`AGENTS.md` 任何一处（自包含 HTTP server + 命令）；`workflow.py` 与 4 技能全部在 `.codex/` 下；删除 `.claude` 对三者零影响

**ISSUE-003：GameServer 联调环境**（**v1.3 更新：已就绪——原「缺口」状态解除**）
- 本机已具备：.NET SDK 10.0.400（可构建 net8.0/net9.0 目标）
- **MongoDB 已就绪（2026-10-10 实测）**：Docker 容器 `mongodb`，镜像 `mongodb/mongodb-community-server:9.0-ubi8-slim`，端口 `0.0.0.0:27017->27017/tcp`，`Test-NetConnection 127.0.0.1 -Port 27017` → `TcpTestSucceeded = True`；**无认证**，GameServer 的 `mongodb://127.0.0.1` 可直接连
- **选型依据**：选 MongoDB **官方**镜像而非 `bitnami/mongodb`——Bitnami 自 **2025-08-28** 起将版本化 tag 移入 `bitnamilegacy`，官方标注 *unsupported, will no longer receive updates or patches*，仅 `:latest` 免费，其余转 Bitnami Secure Images 付费订阅
- **踩过的坑（复用时注意）**：首次 `docker run` 漏了 `-p 27017:27017`，容器虽在 `Up` 状态但主机 27017 **无监听**，GameServer 仍连不上。**判据**：`docker ps` 的 PORTS 列显示 `27017/tcp` 即为未映射，必须是 `0.0.0.0:27017->27017/tcp` 才算通
- **重建命令**（如需重置）：`docker stop mongodb` → `docker rm mongodb` → `docker run --name mongodb -p 27017:27017 -v mongodb-data:/data/db -d mongodb/mongodb-community-server:9.0-ubi8-slim`
- **数据持久化**：当前为镜像自带的匿名卷（`/data/db`、`/data/configdb`），非具名卷；本地联调无影响，重建时如需保留数据改用上面的 `-v mongodb-data:/data/db`
- **待验证**：MongoDB **9.0** 与 Fantasy-Net 2026.0.1023 内置驱动的兼容性**未验证**；若联调报驱动/协议错误，退回 `8.0-ubi8-slim` 即可，无需改代码
- **v1.3 补充：.NET 运行时（踩过的坑，务必看清「SDK」与「运行时」的区别）**
  - `Main.csproj` / `Entity.csproj` 均为 `net8.0;net9.0` 双目标。本机 .NET **SDK 10.0.400 能构建这两个目标**，但**运行**需要对应**运行时**
  - 实测本机运行时：`8.0.0` ✅、`10.0.11` ✅、**`9.0.0` ❌ 缺失**
  - `dotnet run --project Main` 默认挑 net9.0 → 报 `You must install or update .NET to run this application`
  - **解法**：`dotnet run --project .\Server\Main --framework net8.0`（零安装）；或安装 9.0 运行时；或 `DOTNET_ROLL_FORWARD=LatestMajor`（有跨大版本兼容风险）
  - 正确路径：`GameServer\Server\Server.sln`、`GameServer\Server\Main\Main.csproj`（不是 `GameServer\main.sln`）；GameServer 是 **.NET 项目，不是 Go**，用 `dotnet` 而非 `go build`
- **v1.3 更新：联调已通过** —— 注册 → 登录全链路实测成功，`fantasy_main` 生成 `Account` collection；MongoDB 9.0 与 Fantasy-Net 2026.0.1023 **实测兼容，无驱动/协议错误**

**备注**：Unity 6000.3.25f1 的 revision hash 迁移前不可查（Hub 的 `metadata.hub.json` 中 `revisionHash` 为 `null`，`Unity.exe -version` 仅回显版本号）。**v1.3 实测确认**：写入占位的 `000000000000` 已被 Unity 首次打开时自动补全为 **`e1dba0a9aba4`**——「由编辑器自动补全」的说法得到验证 ✅。另登记：`Fantasy.Unity.asmdef` 的 `defineConstraints` 把 `FANTASY_UNITY` 写了两遍（无害冗余，可顺手清理）

### 9.2 联调阶段修复的问题（v1.3 新增，**均已修复并验证**）

| # | 现象 | 根因 | 修复 |
|---|---|---|---|
| 1 | GameServer 启动即崩：`Fantasy.Log.Initialize` 空引用 | `Main.csproj` 写的是 `<None Update="NLog.config">`，但文件实际在 `Server/Entity/`——对不存在的文件 `Update` 是空操作，NLog.config 从未进入输出目录 | 改为 `<None Include="..\Entity\NLog.config" Link="NLog.config">` |
| 2 | 连接建立约 8 秒后被服务器踢，随后 `session is dispose` | `StartHeartbeat()` 在**全 HotFix 目录只有定义、零调用点**；服务器 `session idle timeout = 8000ms` | `OnConnectComplete()` 中调用 `StartHeartbeat()` |
| 3 | 注册成功后点登录卡死，反复报 `Connecting... Please do not initiate the connection again.` | `ConnectAsync` 在 `Scene.Session` 已存在时创建了 `m_connectTask` 却跳过 `Scene.Connect` → 无人 `SetResult` → `await` **永久挂起**，后续请求全被防重分支拦截 | Session 可用且目标地址一致时直接 `return true`；地址不同先 `Disconnect()` 再新建 |

三者均落在提交 **`1a273c74`**。

**性质说明**：三处都是 **Fantasy 分支自身的 demo 缺口，不是本次迁移（main + Fantasy → Unity 6.3）引入的**。但任何人照此方案执行都会撞到，故完整记录。

**排查方法论沉淀**（这几条比结论更值钱）：
- **服务器没日志 ≠ 没发生**：idle 超时断开是正常逻辑，Fantasy 不为它打日志。此时要拿服务器**启动参数**（`idle timeout`、`Session-check interval`）与客户端代码交叉比对，而不是一味加大日志级别
- **区分 SDK 与运行时**：SDK 能构建多目标，不代表运行时齐备；`dotnet run` 对多目标项目必须显式 `--framework`
- **Status 是业务状态，不能当连接状态用**：`IsStatusCanSendMsg` 的白名单按 Status 严格限定（Login 只放行 LoginRequest、Register 只放行 RegisterRequest），而 `ConnectAsync` 却拿 Status 判断"是否已连上"，两者混用正是 #3 的深层成因；判连接应看 `Scene.Session`
- **重复点击会掩盖真因**：防重机制让后续点击全部 await 同一个悬空 Task，日志里刷满同一句警告，容易把人引向错误方向

---

## 10. 执行记录（v1.4，2026-10-10 执行完成）

### 10.1 阶段状态总表

| 阶段 | 状态 | 实测结果 |
|---|---|---|
| 0 退路 | ✅ | `backup/main-before-merge` tag（annotated）+ `unity6.3` 分支（自 `origin/TEngine_Fantasy`，规避 worktree 占用） |
| 1 合入 main | ✅ | 冲突与 §4 预测一致（`.gitignore` + `CLAUDE.md`），`Assets/Launcher`/`MCPForUnity`/`.codex` 废弃技能随合并自动处理 |
| 2 cherry-pick | ✅（取消） | v1.3 ⑧：实测 empty，无需执行 |
| 3 升级 6.3 | ✅ | `6000.3.25f1` 打开编译通过；HybridCLR pin `#v8.15.0` |
| 4 验收（Windows） | ✅ | exe 构建 → 内置模式运行 + 登录（GameServer + MongoDB 9.0）→ **HostPlayMode 热更拉取闭环**（内置 1153 旧包 → 拉取 1164 清单 + 差异 bundle → 新热更代码生效） |
| 5 验收（Android 真机） | ✅ | APK（`adb install`）→ HostPlayMode 拉取（真机 WiFi → 电脑 HTTP）→ **KCP 登录成功**（GameServer 监听 0.0.0.0:20001）→ **热更下发闭环实测**（改热更代码 → F8 → 同步 ServerRoot → 真机拉取新版本生效）。踩坑见 10.2 #7-10 |

### 10.2 执行期踩坑与解决（计划之外，均为实测）

| # | 现象 | 根因 | 修复 |
|---|---|---|---|
| 1 | Android 构建崩于 `ManagedStripped`：`UnityLinker Fatal error: Failed to resolve assembly: 'Fantasy.Unity'`（2022.3 同配置不崩） | `Fantasy.Unity.asmdef` 的 `autoReferenced: true` 使 Unity 自动给所有预定义程序集（firstpass/Assembly-CSharp）注入 Fantasy.Unity 的 AssemblyRef；6.3 的新 UnityLinker 对「已引用但被 HybridCLR filter 移除的程序集」从静默容忍变为抛 `AssemblyResolutionException`。诊断链：rsp 的 root/allowed 列表 → 47 个 DLL 字节扫描定位 firstpass → 源码 0 引用（排除 InternalsVisibleTo）→ 唯一解释 = autoReferenced | `autoReferenced: false`（全仓引用 Fantasy 类型的 305 个源码 100% 在 HotFix 且有显式 GUID 引用，零副作用）；顺带删 `defineConstraints` 重复项。社区同款报错的官方解法一致 |
| 2 | 双轮构建死锁：`Generate/All` 无输入 | `CopyStrippedAOTAssemblies` 是 `IPostprocessBuildWithReport`——第一轮构建因 C++ `#error`（预期失败）中止 → 回调不触发 → `AssembliesPostIl2CppStrip` 空 | 手动等价拷贝：`Copy-Item Library\Bee\artifacts\Android\ManagedStripped\*.dll HybridCLRData\AssembliesPostIl2CppStrip\Android\`（77 个 dll）；构建成功后回调恢复自动 |
| 3 | Android Gradle 阶段 `ninja: error: ... Filename longer than 260 characters`（swappy_static.a） | Unity 进程的 `GRADLE_USER_HOME` 被 CodeBuddy 沙盒注入到 `Temp\cursor-sandbox-cache\<hash>\gradle`，叠加 prefab 解包长路径超 Windows MAX_PATH。证据：`Editor.log` 打印 `GRADLE_USER_HOME = ...cursor-sandbox-cache...`；注册表 User/Machine 级均无此变量 | **彻底关闭 Unity Hub**（污染链中间层）→ 从桌面干净环境重启 Hub → Unity；缓存回落默认 `~\.gradle`。判据：查 `Editor.log` 里的 GRADLE_USER_HOME，别猜进程来源 |
| 4 | Host 模式 404：请求 `.../Demo/Windows64/DefaultPackage.version`，而服务器根只有 `DefaultPackage.version` | ① TEngine URL = `{host}/{projectName}/{platform}/{fileName}`（`UpdateSetting.cs:169`）；② YooAsset 2.x 远端文件名**扁平**（`YooAssetSettingsData.GetPackageVersionFileName` 无子路径）→ 服务器根必须**直接指版本目录内容**，且要带 `Demo/Windows64` 两级 | 发布视图 `Builds/ServerRoot/{projectName}/{platform}/`，HTTP 根指 `ServerRoot`。结构由新菜单 `CreateServerRoot` 自动创建（运行时解析 URL 强同源，projectName/平台改名自动跟随） |
| 5 | 误判「exe 未更新」：构建成功但 exe 时间戳不变 | Bee **增量构建**不重写无变化的文件——exe 启动器本体（0.6MB 原生壳）不变，变化在 `Release_Windows_Data/`（StreamingAssets/level 数据） | 验收判据：查 `Release_Windows_Data\StreamingAssets\package\DefaultPackage\DefaultPackage.version` 与 Data 目录时间戳，**不看 exe 文件时间戳** |
| 6 | 构建输出目录混乱：三个入口四种路径（`Build/`、`Builds/`、`Builds/Windows/`、`Bundles/`） | 各入口 OutputRoot 写死且不一致（`BuildConfig.cs:16/45`、`BuildPipelineWindow.cs:589`、`ReleaseTools.cs:81/93/105` 共 7 处） | 全部统一：Bundle → `Builds/{平台}/DefaultPackage/<版本>`（平台层为 YooAsset 写死，`BuildParameters.cs:193`，无法去除）；Player → `Build/`；发布视图 → `Builds/ServerRoot/`。窗口 EditorPrefs 通过换 key（`_V3`）使旧缓存失效 |
| 7 | 真机报 `InvalidOperationException: Insecure connection not allowed`（HTTP 拉取被拒） | Android 9+ 默认禁止明文 HTTP，Unity 的开关在 Player Settings 里且**打进 APK** | `Project Settings → Player → Android → Other Settings → Allow downloads over HTTP = Always allowed` → 重新打包。上线接 HTTPS CDN 后可收紧 |
| 8 | 真机 HTTP 通了但登录失败 | 双重地址问题：① `LoginUI.cs:29/34` 的 GameServer 地址硬编码 `127.0.0.1`（热更代码，真机上指向手机自己）；② `GameServer/Server/Entity/Fantasy.config:16` 的 `outerBindIP="127.0.0.1"`（服务器只监听本机） | ① LoginUI 改 `192.168.1.105:20001`（随热更下发——正好实战验证热更闭环）；② `outerBindIP="0.0.0.0"` + **`outerIP="192.168.1.105"`**（outerIP 是对外宣告地址，下发客户端用于后续连接，127.0.0.1 会把真机引向手机自己；`innerBindIP` 保留 127.0.0.1） |
| 9 | 打包工具窗口报 `EndLayoutGroup: BeginLayoutGroup must be called first`，且该次构建未产出 | 三个构建按钮在 **OnGUI 调用栈内同步执行数分钟长构建**，IMGUI 布局栈被中断破坏（TEngine 窗口既有缺陷） | 按钮回调改 `EditorApplication.delayCall`（`BuildPipelineWindow.cs` 三处），构建脱离 GUI 上下文执行 |
| 10 | `dotnet run` 报缺 x64 `Microsoft.AspNetCore.App 8.0.0` | `launchSettings.json` 的启动配置依赖 AspNetCore 运行时，本机只有 x86 | 绕过：直接 `dotnet bin\Debug\net9.0\Main.dll`（工作目录 = bin 目录，NLog.config 在此） |

### 10.3 构建工具链改造

- **`Fantasy.Unity.asmdef`**：`autoReferenced: false` + `defineConstraints` 去重（见 10.2#1）
- **新菜单 `TEngine/Build/CreateServerRoot {平台}`**（`ServerRootMenu.cs`）：创建发布视图目录；平台名为编译期常量（切 Build Target 自动变）；目录已存在时菜单置灰（validate 函数）
- **新菜单 `TEngine/Build/ShowHttpCmd`**（`ShowHttpCmdWindow.cs`）：动态生成 python / node 启动命令（根目录、主备端口实时解析自 `UpdateSetting`），一键复制 + 主端口状态探测
- **obsolete API 清零（R11 关闭）**：15 处废弃 API 全部替换——`FantasySettingsProvider.cs` ×3、`UpgradeManager.cs` ×2（`NamedBuildTarget` 新 API）、`HtmlToUGUIBaker.cs` ×9（`FindObjectsByType`/`FindFirstObjectByType`/`textWrappingMode`）、`EditorTools.cs` `#if` 条件修正（`HierarchyProperty` 死代码不再编译）。`ScriptingDefineSymbols.cs:142,156` 的 2 处为**故意保留的旧版兼容层**（6.3 下不编译、不产生警告），未动
- **YooAsset pin**：`manifest.json` → `#v8.15.0`（v1.2 规划，已执行）

### 10.4 剩余事项

| # | 事项 | 说明 |
|---|---|---|
| ~~1~~ | ~~Android 真机验证~~ | ✅ **已完成**（见 §10.1 阶段 5） |
| ~~2~~ | ~~R12：本地化工具 6.3 实测~~ | ✅ **已关闭**：① 反射所在方法 `EditorTools.TextField` 全仓 0 调用方（死代码路径，影响面为零）；② 活路径 `Tools → I2 Localization → Refresh Localizations` 实测正常（仅 I2 自身 `Handles.Repaint` cosmetic 警告，无 NRE/反射失败） |
| ~~3~~ | ~~清理构建残留~~ | ✅ **已完成**（`Bundles/`、两平台旧版本已删；各保留最新：Windows 1164 / Android 1274） |
| 4 | 生产化备选 | 本地 python/nx http.server 仅限联调；上线换 Nginx/OSS/CDN（目录结构不变，`{projectName}/{platform}/` 即 CDN 路径约定） |
| 5 | `GameClient.Send` 陷阱（ISSUE-001/D4） | 维持「暂不修」；业务首次使用 `GameClient.Instance.Send` 前必须修 `GameClient.cs:397` |
