TEngine LocalizationModule 模块讨论记录（原文完整版）
========================================

> 说明：本文件是一字不差地整理当前会话中，围绕 `LocalizationModule` / `LocalizationManager` / I2Localization 与 Google WebService 等相关内容的问答原文（含用户提问与助手回答）。仅做归档，不做任何修改或精简。

---

【对话 1】用户请求阅读 LocalizationModule 模块并说明架构与用法

**用户：**

@UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule 阅读代码，解释架构和设计意图，并举出如何使用的例子和sample

**助手：**

我使用的模型是 **GPT-5.1**。  

### 架构整体（概念解释）

- **模块层（TEngine 模块系统）**  
  - `ILocalizationModule`：对外暴露的本地化模块接口，其他业务代码通过 `ModuleSystem.GetModule<ILocalizationModule>()` 来使用，不直接依赖具体实现。  
  - `LocalizationModule`：实现 `ILocalizationModule` 的模块类，真正工作委托给 `LocalizationManager`，自己只做“转发 + 生命周期管理”。

- **组件层（场景中的 MonoBehaviour）**  
  - `LocalizationManager`：挂在场景 GameObject 上的组件，负责：
    - 与 I2 Localization（`Core` 目录）打交道；
    - 从 `IResourceModule` 加载 CSV 文本；
    - 初始化默认语言、动态加载语言、切换当前语言；
    - 在 `Awake/Start` 中把自己包装成模块并注册到 `ModuleSystem`。

- **工具与数据层**  
  - `Language`：内部统一的语言枚举（含中英日韩等大量语言）。  
  - `LocalizationUtility`：  
    - 把 `Application.systemLanguage` 转换成内部的 `Language`；  
    - 提供 `Language` <-> 字符串（如 `"English"`, `"Chinese"`）的映射；  
    - 定义语言 CSV 资源名前缀 `I2_` 等常量，用于约定 Asset 名。

- **第三方核心（I2 Localization 封装）**  
  - `Core/LanguageSource*`, `LanguageSourceData*`, `Localization.LocalizationManager` 等：这是 I2Localization 的源码，`LocalizationManager` 通过 `_sourceData.Import_CSV`、`Localization.LocalizationManager.CurrentLanguage` 等接口使用它，实现真正的本地化逻辑。

---

### 核心代码解析（基于真实代码）

#### 1. `ILocalizationModule` 接口（对外能力）

```5:59:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/ILocalizationModule.cs
```

- **属性**  
  - `Language Language { get; set; }`：当前使用语言，类型是内部 `Language` 枚举。  
  - `Language SystemLanguage { get; }`：只读，获取系统语言。

- **异步加载能力**（基于 UniTask）  
  - `UniTask LoadLanguageTotalAsset(string assetName)`：加载“语言总表” CSV（一个大表，包含多语言内容）。  
  - `UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)`：加载某个语言的分表，可选是否立刻设为当前语言，以及是否来自初始化（决定用内置 CSV 还是资源模块）。

- **查询与切换能力**  
  - `bool CheckLanguage(string language)`：该语言是否在当前 I2 的语言列表里。  
  - `bool SetLanguage(Language language, bool load = false)`：按枚举切换语言，可选如果未加载则自动去加载。  
  - `bool SetLanguage(string language, bool load = false)`：按字符串切换语言。  
  - `bool SetLanguage(int languageId)`：按语言在列表中的索引切换。

#### 2. `LocalizationModule`：模块包装器

```5:112:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationModule.cs
```

- **内部持有**  
  - `private LocalizationManager _localizationManager;`：真正做事的 MonoBehaviour。

- **绑定与生命周期**  
  - `Bind(LocalizationManager localizationManager)`：在组件层把自己和 `LocalizationManager` 连起来。  
  - `OnInit()`：当前实现为空，模块初始化逻辑由 `LocalizationManager.Awake/Start` 驱动。  
  - `Shutdown()`：`UnityEngine.Object.Destroy(_localizationManager);`，模块卸载时销毁组件。

- **所有接口方法几乎都是一层转发**  
  - `Language` 属性、`SystemLanguage`、各类 `LoadLanguage*` / `SetLanguage*` / `CheckLanguage`，全部简单调用 `_localizationManager` 对应方法，完全保持逻辑一致，不做任何额外处理。  
  - 设计上，这是典型的 **Adapter/Facade**：对上暴露干净的模块接口，对下持有 MonoBehaviour 组件。

#### 3. `LocalizationManager`：实际实现与初始化流程

```10:311:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- **关键字段**  
  - `_defaultLanguage = "Chinese"`：默认语言字符串，后面会在 `Start` 中被覆盖为 Editor 设定或系统语言。  
  - `innerLocalizationCsv : TextAsset`：内置多语言 CSV 资源，用于初始化时不通过 Bundle、直接从本地资源加载。  
  - `_languageSource : LanguageSource` + `_sourceData : LanguageSourceData`：I2Localization 的数据入口，第一次访问 `_sourceData` 时自动在 GameObject 上挂 `LanguageSource` 组件。  
  - `List<string> allLanguage`：当前 I2 数据源中所有语言名列表（去掉换行）。  
  - `useRuntimeModule`：编辑器下的控制开关：
    - `false`：完全用 I2 编辑器资源，不走运行时资源系统；
    - `true`：模拟运行时，用资源模块 + CSV。  
  - `_currentLanguage : string`：当前语言名（例如 `"English"`, `"Chinese"`）。  
  - `_resourceModule : IResourceModule`：TEngine 资源模块，用来加载 `TextAsset`。

- **属性**  
  - `Language Language`：  
    - get：`LocalizationUtility.GetLanguage(_currentLanguage)`，从当前字符串转枚举；  
    - set：把枚举转换成字符串 `LocalizationUtility.GetLanguageStr(value)` 再调用 `SetLanguage(LocalizationUtility.GetLanguageStr(value));`。  
  - `Language SystemLanguage => LocalizationUtility.SystemLanguage;`：依赖 `LocalizationUtility` 的系统语言映射。

- **Awake：挂钩模块系统**  

  核心逻辑：

  ```66:78:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - 从 `ModuleSystem` 中取 `IResourceModule`，失败则 `Log.Fatal`；  
  - 创建 `LocalizationModule localizationModule = new LocalizationModule();`；  
  - `localizationModule.Bind(this);`；  
  - `ModuleSystem.RegisterModule<ILocalizationModule>(localizationModule);`  
  → 这样外部所有通过模块系统访问 `ILocalizationModule` 的地方，实际都间接使用到这个组件。

- **Start：选择默认语言并异步初始化**

  ```80:93:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - 获取 `RootModule.Instance`，失败则 fatal。  
  - `_defaultLanguage = LocalizationUtility.GetLanguageStr(rootModule.EditorLanguage != Language.Unspecified ? rootModule.EditorLanguage : SystemLanguage);`  
    - 优先使用编辑器配置的 `EditorLanguage`；  
    - 否则自动用系统语言（`SystemLanguage`）。  
    - 最后统一转成字符串（例如 `"Chinese"`、`"English"`）。  
  - 调用 `AsyncInit().Forget();` 异步初始化。

- **AsyncInit：区分编辑器模式 / 运行时模式**

  ```95:119:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  逻辑要点：

  - 必须有 `_defaultLanguage`，否则 fatal。  
  - `#if UNITY_EDITOR` 下分支：
    - `!useRuntimeModule`：  
      - 直接调用 `Localization.LocalizationManager.RegisterSourceInEditor();` 注册 I2 编辑器 Source；  
      - `UpdateAllLanguages();` 取出当前所有语言；  
      - `SetLanguage(_defaultLanguage);` 切语言，但不通过 CSV/资源模块。
    - `useRuntimeModule == true`：  
      - `_sourceData.Awake();` 初始化 I2 的 `LanguageSourceData`；  
      - `await LoadLanguage(_defaultLanguage, true, true);` 走统一的 CSV 加载（`fromInit = true`）。
  - 非编辑器（真机）：
    - 始终 `_sourceData.Awake();` + `LoadLanguage(_defaultLanguage, true, true);`。

  → 设计意图是：**编辑器可选择 “直接用 I2 编辑器资源” 或 “模拟运行时加载 CSV” 两种模式**。

- **LoadLanguageTotalAsset：加载“总表” CSV**

  ```121:144:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - 编辑器且 `!useRuntimeModule` 时禁止调用（打印警告）；  
  - 否则使用 `_resourceModule.LoadAssetAsync<TextAsset>(assetName)` 加载；  
  - 成功后调用 `UseLocalizationCSV(assetTextAsset.text, true);`：  
    - `isLocalizeAll = true` → 导入 CSV 后立即 `Localization.LocalizationManager.LocalizeAll();` 更新所有本地化目标。

- **LoadLanguage：加载单个语言 CSV（分表）**

  ```146:193:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - 与总表类似，编辑器下 `!useRuntimeModule` 禁止调用。  
  - `fromInit` 控制数据来源：
    - `false`：通过 `GetLanguageAssetName(language)` 生成资源名，如 `I2_Chinese`，然后走 `_resourceModule.LoadAssetAsync<TextAsset>(assetName)`。  
    - `true`：使用 `innerLocalizationCsv` 内置资源作为 TextAsset，如果没有则警告。  
  - 成功后 `UseLocalizationCSV(assetTextAsset.text, !setCurrent);`：  
    - 如果只是预加载语言而不立刻切换（`setCurrent=false`），就导入后立即 `LocalizeAll`；  
    - 如果要马上切换（`setCurrent=true`），则先导入但不全局刷新，再调用 `SetLanguage(language)`，由 I2 内部完成切换更新。

- **语言列表与检查**

  ```203:222:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - `UpdateAllLanguages()`：通过 `Localization.LocalizationManager.GetAllLanguages()` 拿到所有语言名，去掉换行后存入 `allLanguage`。  
  - `CheckLanguage(string language)`：简单 `allLanguage.Contains(language)`。

- **SetLanguage 系列：切换语言**

  ```225:281:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - `SetLanguage(Language language, bool load = false)`：只是一层包装，调用 `SetLanguage(string, load)`。  
  - `SetLanguage(string language, bool load = false)`：  
    1. 如果当前 `allLanguage` 中不存在该语言：  
       - 若 `load == true`：直接异步 `LoadLanguage(language, true).Forget();` 并返回 `true`（表示已发起加载+切换）；  
       - 否则打印警告并返回 `false`。  
    2. 如果 `_currentLanguage == language`：直接返回 `true`；  
    3. 否则：  
       - `Localization.LocalizationManager.CurrentLanguage = language;` 通知 I2 当前语言；  
       - `_currentLanguage = language;`；  
       - 返回 `true`。  
  - `SetLanguage(int languageId)`：校验索引后从 `allLanguage` 取语言名再调用 `SetLanguage(string)`。

- **导入 CSV 与资源加载**

  ```283:292:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - `UseLocalizationCSV(string text, bool isLocalizeAll = false)`：  
    - `_sourceData.Import_CSV(string.Empty, text, eSpreadsheetUpdateMode.Merge, ',');` 将 CSV 文本合并到 I2 的 `LanguageSourceData`；  
    - `isLocalizeAll` 为 `true` 时调用 `Localization.LocalizationManager.LocalizeAll();` 刷新所有 UI；  
    - 最后 `UpdateAllLanguages();` 更新语言列表。

  ```294:310:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
  ```

  - `LoadFromBundle<T>`：实现 `IResourceManager_Bundles`，内部用 `_resourceModule.LoadAsset<T>(path)` 加载资源，失败会打印错误。

#### 4. `LocalizationUtility`：语言工具与映射

```9:133:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationUtility.cs
```

- **系统语言映射**  
  - `SystemLanguage` 属性内部 `switch (Application.systemLanguage)` 把 Unity 的 `SystemLanguage` 映射成内部 `Language` 枚举，例如：  
    - `UnityEngine.SystemLanguage.Chinese` / `ChineseSimplified` → `Language.ChineseSimplified`  
    - `UnityEngine.SystemLanguage.ChineseTraditional` → `Language.ChineseTraditional`  
    - 其他大量语言一一对应。  
  - 默认返回 `Language.Unspecified`。

- **语言名映射表**  
  - `_languageMap : Dictionary<Language, string>`  
  - `_languageStrMap : Dictionary<string, Language>`  
  - 静态构造函数中注册若干映射（当前只注册了部分核心语言）：

    ```77:84:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationUtility.cs
    ```

    - `RegisterLanguageMap(Language.English);` → `"English"`  
    - `RegisterLanguageMap(Language.ChineseSimplified, "Chinese");` → `"Chinese"`（注意不是 `ChineseSimplified`）  
    - `RegisterLanguageMap(Language.ChineseTraditional);` → `"ChineseTraditional"`  
    - `RegisterLanguageMap(Language.Japanese);`, `RegisterLanguageMap(Language.Korean);`

- **提供统一转换接口**  
  - `GetLanguage(string str)`：  
    - 空串 → `Language.Unspecified`；  
    - 在 `_languageStrMap` 有映射则返回对应枚举；  
    - 否则默认返回 `Language.English`。  
  - `GetLanguageStr(Language language)`：  
    - 在 `_languageMap` 里返回对应字符串；  
    - 否则默认 `"English"`。

- **常量**  
  - `I2ResAssetNamePrefix = "I2_"`：生成语言 CSV 的资源名时使用，例如 `"I2_Chinese"`。

#### 5. `Language` 枚举

```1:262:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Language.cs
```

- 定义了大量语言，从 `Unspecified` 开始，包括中英日韩、俄语、西语、葡语等。  
- 该枚举作为项目中统一的“语言标识”，既用于系统语言，也用于编辑器配置（`RootModule.EditorLanguage`）、以及对外 API（`ILocalizationModule.Language`）。

---

### 设计意图分析（概念解释）

- **通过模块系统隔离 MonoBehaviour / 第三方库细节**  
  - 业务侧只关心 `ILocalizationModule`，通过 `ModuleSystem.GetModule<ILocalizationModule>()` 获取；  
  - 真正的实现细节（I2Localization、CSV 导入、资源模块、场景中的组件）全部封装在 `LocalizationManager` 和 `Core` 目录里；  
  - 好处：更易于替换实现（不用 I2 也可以更换）、更好测试、降低耦合。

- **统一的语言表示层**  
  - 通过 `Language` 枚举 + `LocalizationUtility` 把：
    - Unity 的 `Application.systemLanguage`，  
    - I2 的语言名字符串（如 `"Chinese"`, `"English"`），  
    - 模块接口中的枚举形式，  
    统一抽象起来。  
  - 设计上避免直接散落 `"English"`, `"Chinese"` 这种硬编码字符串，统一通过工具类转换，方便将来新增语言或改名。

- **编辑器友好与运行时友好兼容**  
  - 编辑器下可以不开启 `useRuntimeModule`，直接使用 I2 的编辑器资源（提升迭代效率，不用打包 CSV）；  
  - 运行时或模拟运行时模式（`useRuntimeModule = true`）全部走 `IResourceModule` + CSV，贴合真实发布环境；  
  - 通过 `innerLocalizationCsv` 提供一个内置 CSV，保证在没有网络/Bundle 的情况下也能初始化一套语言内容。

- **异步加载与资源模块集成**  
  - 所有加载接口都是 `UniTask`，与 TEngine 其他异步系统统一；  
  - CSV TextAsset 的加载全部走统一的 `IResourceModule`，让本地化资源成为资源系统的一部分（可热更、可打 AssetBundle、可 Addressables 等）。

---

### 使用方式与 Sample（示例 / 简化说明）

> 下面的示例为**简化说明**，用来展示实际用法思路，代码结构可能与完整项目略有差异。

#### 1. 场景与资源准备（概念步骤）

- **场景中挂载组件**  
  - 在启动场景（例如 `Bootstrap` 场景）创建一个 GameObject，比如 `LocalizationRoot`。  
  - 挂上 `LocalizationManager` 组件。  
  - 在 Inspector 中根据需要设置：
    - `useRuntimeModule`：  
      - 编辑器调试阶段想直接用 I2 编辑器资源，可勾掉；  
      - 想模拟真机运行时资源加载，就勾上。  
    - `innerLocalizationCsv`：  
      - 若走 `fromInit = true` 路线，需要把 I2Localization 导出的 CSV 拖进来作为内置初始多语言资源。  
  - 工程 Unity 版本建议保持为 **2022.3.61f1c1**，与当前项目约定保持一致。

- **准备 I2 CSV 与 Asset 名称**  
  - 使用 I2Localization 编辑器工具配置多语言和 Terms；  
  - 导出 CSV（总表/分表），例如：  
    - 总表：包含所有语言的 CSV，用于 `LoadLanguageTotalAsset("I2_AllLanguages")`；  
    - 分表：每种语言一个 CSV，命名约定如 `I2_Chinese`, `I2_English` 等，对应 `GetLanguageAssetName(language)` 的前缀 `I2_`。  
  - 把这些 CSV 作为 `TextAsset` 打包到资源系统（Bundle/Addressable），确保 `IResourceModule` 能按名字加载到。

#### 2. 在游戏代码中使用模块切换语言（示例代码）

**通过模块系统获取本地化模块并切换语言（枚举方式）**

```csharp
using UnityEngine;
using TEngine;

public class LanguageSwitcher : MonoBehaviour
{
    private ILocalizationModule _loc;

    private void Start()
    {
        // 从模块系统获取 ILocalizationModule
        _loc = ModuleSystem.GetModule<ILocalizationModule>();

        // 例：进入游戏时强制切到英文，如果没有加载则自动加载
        _loc.SetLanguage(Language.English, load: true);
    }

    public void OnClickChinese()
    {
        // UI 按钮回调：切换到简体中文
        _loc.SetLanguage(Language.ChineseSimplified, load: true);
    }

    public void OnClickJapanese()
    {
        // 切到日文，假设已存在 "Japanese" 语言且对应 CSV 已打包
        _loc.SetLanguage(Language.Japanese, load: true);
    }
}
```

**异步加载总表 + 再设定当前语言**

```csharp
using Cysharp.Threading.Tasks;
using TEngine;

public class LocalizationInitializer
{
    public async UniTask InitAsync()
    {
        var loc = ModuleSystem.GetModule<ILocalizationModule>();

        // 先加载语言总表（例如包含多国语言）
        await loc.LoadLanguageTotalAsset("I2_AllLanguages");

        // 然后设定当前语言，例如系统语言或玩家设置
        loc.SetLanguage(loc.SystemLanguage);
    }
}
```

**按字符串名称切换语言**

```csharp
var loc = ModuleSystem.GetModule<ILocalizationModule>();

// 注意这里的 "Chinese"、"English" 要和 I2 里的语言名一致
loc.SetLanguage("Chinese", load: true);
loc.SetLanguage("English");
```

#### 3. UI 层如何拿到本地化文本（概念说明）

- 具体 UI 文本不通过这个模块直接获取，而是通过 **I2Localization 的 `Localize` 组件**：  
  - 在 `Text` / `TMP_Text` 等 UI 元素上挂 `Localize`；  
  - 在 I2 编辑器里配置 Term（键值）和各语言内容；  
  - 当 `Localization.LocalizationManager.CurrentLanguage` 改变时（`LocalizationManager.SetLanguage` 中设置），I2 会自动刷新带 `Localize` 的 UI。  
- 这个 TEngine 本地化模块的主要职责是：  
  - 负责 **加载/合并 CSV 数据** 到 I2；  
  - 负责 **选择 / 切换当前语言**；  
  - 负责通过模块系统给业务层提供统一入口。

---

【对话 2】Editor 中自动翻译时报错 “WebService is not set correctly or needs to be reinstalled”

**用户：**

WebService is not set correctly or needs to be reinstalled
UnityEngine.Debug:LogError (object)
TEngine.Localization.LocalizationEditor:ShowMessage (string,UnityEditor.MessageType,bool) (at Assets/TEngine/Editor/Localization/Localization/LocalizationEditor.cs:242)
TEngine.Localization.LocalizationEditor:ShowError (string,bool) (at Assets/TEngine/Editor/Localization/Localization/LocalizationEditor.cs:227)
TEngine.Localization.LocalizationEditor:Translate (string,TEngine.Localization.TermData&,string,TEngine.Localization.GoogleTranslation/fnOnTranslated,string) (at Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Terms_Description.cs:750)
TEngine.Localization.LocalizationEditor:TranslateTerm (string,TEngine.Localization.TermData,TEngine.Localization.LanguageSourceData,int) (at Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Terms_Description.cs:592)
TEngine.Localization.LocalizationEditor/<>c__DisplayClass184_1:<OnGUI_Keys_LanguageTranslations>b__0 () (at Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Terms_Description.cs:456)
TEngine.Localization.GUITools:OnDelayedCall () (at Assets/TEngine/Editor/Localization/EditorTools.cs:42)
UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()


**助手：**

我使用的模型是 **GPT-5.1**。  

### 1. 这个错误在代码里是怎么来的（基于真实代码）

报错信息：

> WebService is not set correctly or needs to be reinstalled  

堆栈中关键调用链是 `LocalizationEditor.Translate -> GoogleTranslation.CanTranslate`。  
在源码中：

```742:752:UnityProject/Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Terms_Description.cs
static void Translate ( string Key, ref TermData termdata, string TargetLanguageCode, GoogleTranslation.fnOnTranslated onTranslated, string overrideSpecialization )
{
	#if UNITY_WEBPLAYER
	ShowError ("Contacting google translation is not yet supported on WebPlayer" );
	#else

	if (!GoogleTranslation.CanTranslate())
	{
		ShowError ("WebService is not set correctly or needs to be reinstalled");
		return;
	}
	...
```

`GoogleTranslation.CanTranslate()` 的实现：

```11:15:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Google/GoogleTranslation.cs
public static bool CanTranslate ()
{
	return LocalizationManager.Sources.Count > 0 && 
	       !string.IsNullOrEmpty (LocalizationManager.GetWebServiceURL());
}
```

`GetWebServiceURL` 的实现：

```46:55:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Manager/LocalizationManager.cs
public static string GetWebServiceURL( LanguageSourceData source = null )
{
	if (source != null && !string.IsNullOrEmpty(source.Google_WebServiceURL))
		return source.Google_WebServiceURL;

    InitializeIfNeeded();
	for (int i = 0; i < Sources.Count; ++i)
		if (Sources[i] != null && !string.IsNullOrEmpty(Sources[i].Google_WebServiceURL))
			return Sources[i].Google_WebServiceURL;
	return string.Empty;
}
```

**结论（精确对应代码）：**

- 只有在 **两个条件都满足** 时，`CanTranslate()` 才会返回 `true`：  
  1. `LocalizationManager.Sources.Count > 0` —— 至少有一个 `LanguageSourceData` 被注册；  
  2. 至少有一个 `LanguageSourceData.Google_WebServiceURL` 不为空（即配置了 WebService URL）。  
- 你点击 Editor 里的“Translate / Translate All” 按钮时，如果上面任意一条不满足，就会弹出你看到的这句错误。

---

### 2. WebService 在编辑器里的配置位置（概念说明）

WebService 的配置界面在 **本地化编辑器的 Google 表格设置里**。对应代码：

```150:236:UnityProject/Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Spreadsheet_Google.cs
void OnGUI_GoogleCredentials()
{
	...
	GUILayout.Label (new GUIContent("Web Service URL:", WebServiceHelp),  GUILayout.Width(110));
	mProp_Google_WebServiceURL.stringValue = EditorGUILayout.TextField(mProp_Google_WebServiceURL.stringValue);
	...
	if (GUILayout.Button(new GUIContent("Install", ...)))
	{
		ClearErrors();
		Application.OpenURL("https://script.google.com/d/1zcsLSmq3Oddd8AsLuoKNDG1Y0eYBOHzyvGT7v94u1oN6igmsZb_PJzEm/newcopy");  // V5
	}
	if (GUILayout.Button("Verify", EditorStyles.toolbarButton))
	{
		ClearErrors();
		VerifyGoogleService(mProp_Google_WebServiceURL.stringValue);
		GUI.changed = false;
	}
	...
```

**也就是说：**

- 打开 `Localization` 编辑器（你已经在用它翻译词条）。  
- 切到顶部 Tab：`Spreadsheets` -> `Google`。  
- 在 `Web Service URL:` 这一行填写 URL，并且可以通过旁边的 `Install`/`Verify` 按钮进行安装和校验。

---

### 3. 正确配置 / 重新安装 WebService 的具体步骤（按实际代码逻辑）

> 以下是对代码行为的**简化说明**，帮助你操作 Unity 和浏览器，不修改原有逻辑。

#### 步骤 1：确保有 LanguageSource（避免 Sources.Count == 0）

`LocalizationManager.UpdateSources()` 在编辑器模式下会自动从 `Assets/Editor/I2Localization/I2Languages.asset` 里加载一个全局语言源：

```45:78:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Manager/LocalizationManager_Sources.cs
public static void RegisterSourceInEditor()
{
	var sourceAsset = GetEditorAsset();
	if (sourceAsset == null) return;
	...
	AddSource(sourceAsset.mSource);
}

public static LanguageSourceAsset GetEditorAsset(bool force = false)
{
	...
	Debug.Log("I2LocalizationManager 加载编辑器资源数据");
	var sourceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(LocalizationUtility.I2GlobalSourcesEditorPath);
	if (sourceAsset == null)
	{
		Debug.LogError($"错误 没有找到编辑器下的资源 {LocalizationUtility.I2GlobalSourcesEditorPath}");
		return null;
	}
	...
}
```

- 请确认工程里存在：`Assets/Editor/I2Localization/I2Languages.asset`  
- 如果这个 asset 丢失，编辑器日志会有你看到的这条中文错误提示；此时 `Sources.Count` 可能为 0，会导致 `CanTranslate()` 失败。  
- 如果丢了，需要从备份或原始 I2Localization 包里把该 asset 恢复到同一路径。

#### 步骤 2：在浏览器中安装 / 更新 Google WebService

在 Localization 编辑器的 **Spreadsheets → Google** 页面：

1. 在 `Web Service URL:` 下面，点击 **`Install`** 按钮：  
   - 代码里调用 `Application.OpenURL("https://script.google.com/d/1zcsLSmq3Oddd8AsLuoKNDG1Y0eYBOHzyvGT7v94u1oN6igmsZb_PJzEm/newcopy");`，会在浏览器打开一个 Google Apps Script 的副本（版本 V5，与 `GetRequiredWebServiceVersion()` 中的 `5` 对应）。
2. 在浏览器页面中：
   - 登录你的 Google 帐号；
   - 在 Apps Script 页面里，按照 I2 文档指引，**部署为 Web 应用 (Web App)**：  
     - 执行身份：你的帐号；  
     - 访问权限：**任何拥有链接的人** 或者相应可访问的范围；
   - 部署后，Google 会给一个类似 `https://script.google.com/macros/s/XXXX/exec` 的 URL。
3. **复制这个 Web 应用的 URL**（注意是以 `/exec` 结尾的地址）。

#### 步骤 3：在 Unity 里填回 WebService URL 并验证

回到 Unity，仍然在 `Spreadsheets → Google`：

1. 把刚才复制的 `/exec` URL 粘贴到 `Web Service URL:` 文本框（`mProp_Google_WebServiceURL`）中。  
2. 点击旁边的 **`Verify`** 按钮：  
   - 这会向 `?action=Ping` 发请求，并用 `LocalizationManager.GetRequiredWebServiceVersion()` 检查版本：  

   ```422:471:UnityProject/Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Spreadsheet_Google.cs
   void VerifyGoogleService( string WebServiceURL )
   {
       ...
       mConnection_WWW = UnityWebRequest.Get(WebServiceURL + "?action=Ping");
       ...
   }

   void OnVerifyGoogleService( string Result, string Error )
   {
       ...
       int requiredVersion = LocalizationManager.GetRequiredWebServiceVersion(); // 返回 5
       if (requiredVersion == version)
       {
           mWebService_Status = "Online";
           ClearErrors();
       }
       else
       {
           mWebService_Status = "UnsupportedVersion";
           ShowError("The current Google WebService is not supported.\nPlease, delete the WebService from the Google Drive and Install the latest version.");
       }
       ...
   }
   ```

3. 如果 WebService 安装正确且版本匹配：
   - `mWebService_Status` 会变为 `"Online"`，在 URL 文本框右侧会显示绿色对勾；  
   - 此时 `GetWebServiceURL()` 返回的就是你填的 URL，`CanTranslate()` 就能通过。  

如果你看到 “UnsupportedVersion” 的相关提示，说明你当前 Google Drive 上的脚本不是 V5，需要按上面的 **Install** 步骤重新复制最新脚本并重新部署。

---

### 4. 不想用 Google 自动翻译时怎么避免这个错误（概念说明）

- 这个错误**只会在你点击 Editor 里的“Translate / Translate All”按钮时触发**（即调用 `Translate(...)` 那几个方法时）。  
- 如果你不打算配置 Google 表格 + WebService，只是想手动维护所有语言内容，可以：  
  - **不点**这些自动翻译按钮，直接在每种语言的 TextArea 里输入翻译文本；  
  - 或者在团队规范里明确“本项目不使用 Google 自动翻译”，只用 I2 的本地编辑功能。

---

### 5. 小结（设计 vs 你的问题）

- 代码设计上，**Google 翻译功能是 I2Localization 的“进阶功能”**，依赖一个部署在 Google Apps Script 上的 WebService；  
- 你的错误信息说明当前环境满足不了 `CanTranslate()` 的条件：  
  - 要么没有合法的 `LanguageSourceData`（`I2Languages.asset` 丢失 / 未加载）；  
  - 要么没在 `Google_WebServiceURL` 中填入一个已部署、版本正确的 WebService 地址。  
- 按上面 3 个步骤（检查 `I2Languages.asset` → 安装 WebService → 在 Unity 中填 URL 并 Verify）完成后，错误就会消失，你就可以在 Editor 里使用 “Translate / Translate All” 自动翻译。  

如果你希望，我可以再帮你检查一下项目里 `Assets/Editor/I2Localization/I2Languages.asset` 的存在情况，以及给一个“只使用本地 CSV、不依赖 Google WebService”的团队工作流建议。

---

【对话 3】用户希望获得“不依赖 WebService 的工作流”和 `I2Languages.asset` 排查建议

**用户：**

希望

**助手：**

我使用的模型是 **GPT-5.1**。  

### 一、帮你确认 `I2Languages.asset` 是否正常（不改代码，只是排查建议）

**1. 在 Unity 里检查资产是否存在**

- **确认路径**：根据代码，编辑器期望的资源在：  
  `Assets/Editor/I2Localization/I2Languages.asset`  
- 在 Unity 的 `Project` 窗口中：
  - 展开 `Assets/Editor/I2Localization/` 文件夹；
  - 看是否能看到 `I2Languages.asset`，图标通常是一个自定义 ScriptableObject（`LanguageSourceAsset`）。

**2. 结合日志验证**

- 这段代码会在编辑器加载 Source 时打日志：

```68:73:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Manager/LocalizationManager_Sources.cs
Debug.Log("I2LocalizationManager 加载编辑器资源数据");
var sourceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(LocalizationUtility.I2GlobalSourcesEditorPath);
if (sourceAsset == null)
{
	Debug.LogError($"错误 没有找到编辑器下的资源 {LocalizationUtility.I2GlobalSourcesEditorPath}");
	return null;
}
```

- 打开 Unity 的 `Console` 面板：
  - 如果能看到 `"I2LocalizationManager 加载编辑器资源数据"` 且没有后面那条 **错误日志**，说明 `I2Languages.asset` 能正常加载；  
  - 如果有那条 `错误 没有找到编辑器下的资源 Assets/Editor/I2Localization/I2Languages.asset`，说明文件确实丢了或路径不对。

**3. 如果 `I2Languages.asset` 丢失，怎么修复**

- **方式 A：从原始 I2Localization 包重新导入**  
  - 找到你项目最初集成 I2Localization 的 UnityPackage 或 Git 子模块；
  - 只重新导入 `Assets/Editor/I2Localization` 目录（尤其包含 `I2Languages.asset`）；
  - 确保导入后路径保持完全一致：`Assets/Editor/I2Localization/I2Languages.asset`。

- **方式 B：手动创建新的全局 Language Source（折中方案）**  
  - 在 `Project` 窗口右键 → `Create` → 找到 I2Localization 的 `Language Source Asset`（如果菜单有）；
  - 命名为 `I2Languages`，并放在 `Assets/Editor/I2Localization/` 目录下；
  - 打开该 asset 的 Inspector，填好你需要的语言和 Terms；
  - 这样 `GetEditorAsset()` 会加载到这个新的 Source，`Sources.Count` > 0，避免 `CanTranslate()` 因无 Source 失败。  
  - 注意：如果你之前有旧数据，需要手动导入或从备份恢复到这个新的 Source。

---

### 二、不依赖 Google WebService 的团队工作流建议（只用本地 CSV）

> 下面是对工作流的**简化说明**，不改变现有代码逻辑，只是推荐用法。

#### 1. 编辑器内：以 `I2Languages.asset` 为“单一真源”

- 团队统一约定：  
  - 所有多语言的 **Term / 翻译文本** 都在 `I2Languages.asset`（或其他 LanguageSourceAsset）里维护；  
  - 不通过 Google Sheets 编辑内容，只使用内置 Editor；
  - 不使用 “Spreadsheets → Google” 里的 Import/Export/Translate 按钮。
- 具体操作：
  - 打开 Localization 编辑器（你现在已经在用）；  
  - 在 `Terms` Tab 下添加/修改 Key；  
  - 在 `Languages` Tab 下添加或禁用语言；  
  - 在每种语言对应的 TextArea 中直接填翻译文本。  
- 这样无需配置 `Google_WebServiceURL`，不会再去走 WebService。

#### 2. 导出 CSV 给运行时使用（配合 `TEngine.LocalizationManager`）

你当前 Runtime 的 `LocalizationManager` 使用 **CSV TextAsset** 作为数据来源：

- 初始内置表：`innerLocalizationCsv : TextAsset`（Inspector 里挂的那个）；  
- 动态加载表：`LoadLanguageTotalAsset(assetName)` / `LoadLanguage(language, ...)` 内部都是读取 `TextAsset.text` 然后：

```283:292:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
_sourceData.Import_CSV(string.Empty, text, eSpreadsheetUpdateMode.Merge, ',');
if (isLocalizeAll)
{
	Localization.LocalizationManager.LocalizeAll();
}
UpdateAllLanguages();
```

**推荐工作流：**

- **步骤 A：在编辑器中用 I2 的本地 CSV 导出功能**  
  - 在 Localization 编辑器中切到 `Spreadsheets → Local`（而不是 Google）；  
  - 使用 I2 提供的本地 CSV Export 功能，把当前项目多语言导出为 CSV 文件（总表或分表）。  
  - 这部分代码在 `LanguageSourceData_Export_CSV.cs` 等文件中实现，你已经有了。

- **步骤 B：把 CSV 作为 TextAsset 导入 Unity**  
  - 将导出的 CSV 文件拖入 Unity 工程中某个资源目录（例如 `Assets/Localization/CSV/`）；  
  - 选中它们，在 Inspector 里确保它们是 `TextAsset` 类型；  
  - 这些 CSV 就可以打入 AssetBundle 或 Addressables，由 `IResourceModule` 加载。

- **步骤 C：绑定到 `LocalizationManager`**

  - **内置初始化语言**：  
    - 选中场景里的 `LocalizationManager` 组件，把一个“总表 CSV”或“默认语言 CSV”拖到 `innerLocalizationCsv` 字段里；  
    - 确保 `useRuntimeModule = true` 时，`AsyncInit()` 会用这个 CSV 做首轮初始化。  

  - **运行时追加/切换语言**（如果你需要）：  
    - 为每个语言单独导出一个 CSV，并命名为 `I2_<Language>`（例如 `I2_Chinese`, `I2_English`），以便 `GetLanguageAssetName(language)` 找到：
      ```195:198:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
      private string GetLanguageAssetName(string language)
      {
          return $"{LocalizationUtility.I2ResAssetNamePrefix}{language}";
      }
      ```
    - 在资源系统里保证这些 CSV 能按对应名字被 `IResourceModule.LoadAssetAsync<TextAsset>()` 加载到；
    - 运行时通过 `ILocalizationModule.LoadLanguage("Chinese", setCurrent: true)` 等接口来加载或切换。

#### 3. 团队注意事项（避免再碰 WebService 报错）

- 编辑器使用约定：  
  - 不再使用 “Translate / Translate All”（Google 自动翻译）按钮 —— 它们会触发 `Translate(...)`，进而要求 WebService；  
  - 不再使用 “Spreadsheets → Google” 页签上的 Import/Export/Translate 按钮；  
  - 只用 “Spreadsheets → Local” 和 “Terms/Languages” 来维护和导出 CSV。
- 如果以后确实需要 Google 自动翻译：  
  - 再按前一个回答里的步骤配置 WebService URL + 安装脚本即可，当前工作流不妨碍之后启用。

---

（本次与 LocalizationModule 相关的会话原文到此结束）


