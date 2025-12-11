## I2Localization 与 LocalizationModule 配置说明

### 一、问题背景

- **现象**：运行 `main` 场景时，某个挂了 `I2 Localize` 的 `Text (Legacy)` 显示警告：`Key 'ok' is not Localized or it is in a different Language Source`。
- **场景位置**：`UnityProject/Assets/Scenes/main.unity` 中的 `Text (Legacy)` 对象，`I2 Localize` 组件的 `Term` 为 `ok`。
- **语言源资源**：`UnityProject/Assets/Editor/I2Localization/I2Languages.asset`，在其中已经配置了 Term `ok`，中文为“确定”，英文为 `OKOK`。

### 二、原因分析（基于当前真实代码与配置）

1. **main 场景中的 I2 Localize 配置**  
   - 在 `main.unity` 里，`Text (Legacy)` 上挂载的 I2 Localize 组件：
     - `mTerm: ok`。
     - 其 Language Source 指向运行时的 `LocalizationModule (Language Source)`。

2. **I2 语言源中确实存在 `ok`**  
   - `I2Languages.asset` 中：
     - `mTerms` 列表包含一条：`Term: ok`。
     - `Languages[0] = "确定"`（Chinese），`Languages[1] = "OKOK"`（English）。

3. **运行时使用的并不是 `I2Languages.asset` 本身**  
   - 工程中有自定义本地化模块：
     - `TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs`
     - `TEngine/Runtime/Module/LocalizationModule/LocalizationModule.cs`
   - `GameEntry.prefab` 中包含一个 `LocalizationModule` GameObject，挂载 `LocalizationManager` 组件：
     - 位置：`UnityProject/Assets/TEngine/Settings/Prefab/GameEntry.prefab`。
     - 关键字段：
       - `innerLocalizationCsv: {fileID: 0}`（未配置）
       - `allLanguage: []`（初始化前为空）
       - `useRuntimeModule: 1`（启用运行时模式）

4. **LocalizationManager 初始化逻辑**  
   - `LocalizationManager.Start()`：
     - 根据 `RootModule` 的 `EditorLanguage` 或系统语言，得到 `_defaultLanguage` 字符串，例如 `Chinese` 或 `English`。
     - 调用 `AsyncInit()`。
   - `AsyncInit()` 关键分支：

     ```csharp
     #if UNITY_EDITOR
     if (!useRuntimeModule)
     {
         Localization.LocalizationManager.RegisterSourceInEditor();
         UpdateAllLanguages();
         SetLanguage(_defaultLanguage);
     }
     else
     {
         _sourceData.Awake();
         await LoadLanguage(_defaultLanguage, true, true);
     }
     #else
     _sourceData.Awake();
     await LoadLanguage(_defaultLanguage, true, true);
     #endif
     ```

   - 当前配置是 `useRuntimeModule = true`，所以编辑器和运行时都会走 `LoadLanguage(_defaultLanguage, true, true)` 这条路径。

5. **LoadLanguage(fromInit: true) 对 `innerLocalizationCsv` 的依赖**  
   - 当 `fromInit == true` 时，代码片段：

     ```csharp
     if (innerLocalizationCsv == null)
     {
         Log.Warning("请使用I2Localization.asset导出CSV创建内置多语言.");
         return;
     }

     assetTextAsset = innerLocalizationCsv;
     ```

   - 因为 `GameEntry` 中 `innerLocalizationCsv` 为空，所以：
     - 不会从任何 CSV 导入语言数据。
     - `LocalizationModule` 挂载的 `LanguageSource` 在运行时是空的，没有 `ok` 这个 Term。

6. **I2 Localize 报错的直接原因**  
   - I2 Localize 组件在运行时从当前语言源（`LocalizationModule (Language Source)`）中查找 Key：
     - 因为该 Language Source 没有导入 CSV，自然不存在 Key `ok`。
     - 于是出现提示：**`Key 'ok' is not Localized or it is in a different Language Source`**。
   - 虽然 `I2Languages.asset` 有 `ok`，但它只是一个编辑期的 Language Source 资源，默认并不会自动挂到运行时的 `LocalizationManager` 上。

### 三、如何修好这个问题（正确配置方式）

> 目标：让 `LocalizationManager` 在运行时加载 `I2Languages.asset` 中已经配置好的多语言数据，从而让 I2 Localize 能在 `LocalizationModule (Language Source)` 中找到 `ok`。

#### 方案一：导出 CSV 到 `innerLocalizationCsv`（推荐，用于正式运行）

1. **选中语言源资源**  
   - 在 Project 视图中选中：`Editor/I2Localization/I2Languages.asset`。
   - Inspector 中会显示 `Language Source` 面板：上方有 `Spreadsheets / Terms / Languages / Tools / Assets` 等 Tab。

2. **通过 Language Source 面板导出 CSV/TextAsset**  
   - 在 `Spreadsheets` Tab 下的 `Local` 子 Tab：
     - 在右侧 `Export` 区域点击 **`Add New`** 或 **`Replace`** 按钮。
     - Unity 会弹出保存对话框：
       - 选择一个位于 `Assets` 目录下的文件夹，例如 `Assets/AssetRaw/Configs/` 或其他你喜欢的位置。
       - 文件名可以取为：`I2Languages.csv` 或 `I2Languages.txt`。
       - 确认保存后，工程中会生成一个 `TextAsset` 资源（即导出的 CSV 文件）。

3. **把导出的 CSV 绑定到 LocalizationModule**  
   - 打开 `GameEntry.prefab`：
     - 位置：`TEngine/Settings/Prefab/GameEntry.prefab`。
   - 选中子节点 `LocalizationModule`（GameObject 名称即“LocalizationModule”）。
   - 在 Inspector 中找到挂载的 `LocalizationManager` 组件：
     - 将刚刚导出的 `I2Languages.csv`（或 `.txt`）资源，拖拽到字段 **`innerLocalizationCsv`** 上。

4. **运行验证**  
   - 回到 `main` 场景，点击播放：
     - `LocalizationManager` 会在 `AsyncInit()` 里调用 `LoadLanguage(_defaultLanguage, true, true)`，并使用 `innerLocalizationCsv` 导入 I2 数据。
     - 导入成功后，`LocalizationModule (Language Source)` 中将包含 `ok` 这个 Term。
     - 场景中的 I2 Localize 组件就能正确找到 `ok`，不再出现警告。

#### 方案二：编辑器模式下关闭 Runtime 模式（仅方便调试）

1. 打开 `GameEntry.prefab`，选中 `LocalizationModule`。
2. 在 `LocalizationManager` 组件上，将 **`useRuntimeModule` 勾选取消（设为 false）**。
3. 再次在编辑器里运行 `main` 场景：
   - `AsyncInit()` 会走 `!useRuntimeModule` 分支：

     ```csharp
     Localization.LocalizationManager.RegisterSourceInEditor();
     UpdateAllLanguages();
     SetLanguage(_defaultLanguage);
     ```

   - 这时会直接使用 I2 编辑器的语言源（包括 `I2Languages.asset` 中的数据）。
   - `ok` 能被正确解析，不再报警。

> 注意：
> - 方案二适合在编辑器里快速调试，但最终打包或在正式运行时，仍建议使用 **方案一**，让运行时配置与 CSV 资源保持一致。

### 四、相关路径与字段一览

- **场景**：`UnityProject/Assets/Scenes/main.unity`
- **语言源资源**：`UnityProject/Assets/Editor/I2Localization/I2Languages.asset`
- **运行入口预制体**：`UnityProject/Assets/TEngine/Settings/Prefab/GameEntry.prefab`
- **本地化运行模块脚本**：
  - `UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs`
  - `UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationModule.cs`
- **关键字段**：
  - `LocalizationManager.innerLocalizationCsv`：运行时初始化时使用的 CSV/TextAsset 语言表。
  - `LocalizationManager.useRuntimeModule`：是否走运行时 CSV 加载逻辑（true）或编辑器直接注册逻辑（false）。

### 五、排查类似问题的思路

1. 先确认 **Term 在某个 Language Source 资产里是否存在**（比如 `I2Languages.asset` 中的 Terms）。
2. 再确认 **运行时实际使用的 Language Source 是哪个**（例如本项目中是 `LocalizationModule` 动态创建的 LanguageSource）。
3. 检查 **运行时 Language Source 是否从 CSV / 其他资源成功导入数据**：
   - 是否有绑定 `innerLocalizationCsv`；
   - 日志中是否有类似“请使用I2Localization.asset导出CSV创建内置多语言.” 或 “没有加载到目标语言资源 XX” 的警告。
4. 若编辑器下与运行时行为不同，重点检查 `#if UNITY_EDITOR` 条件分支与 `useRuntimeModule` 等开关的配置。
