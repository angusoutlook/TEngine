TEngine LocalizationModule 会话摘要（含关键代码与 Sample）
========================================

> 说明：本文件对本次围绕 `LocalizationModule` / `LocalizationManager` / I2Localization & Google WebService 的会话进行简要总结，保留关键代码片段与使用 sample，便于后续查阅。  
> 详细一字不差原文请参见：`TEngine_Localization_Conversation_Raw.md`。

---

## 一、整体架构概览

- **模块层 (`TEngine` 模块系统)**  
  - `ILocalizationModule`：对外接口，暴露当前语言、系统语言、加载语言表和切换语言的方法。  
  - `LocalizationModule : Module, ILocalizationModule`：实现接口但只做“转发 + 生命周期管理”，内部持有一个 `LocalizationManager`。

- **组件层（场景 MonoBehaviour）**  
  - `LocalizationManager : MonoBehaviour, IResourceManager_Bundles`：  
    - 在 `Awake` 中通过 `ModuleSystem.RegisterModule<ILocalizationModule>` 把自己包装进模块系统；  
    - 使用 `IResourceModule` 加载 CSV（总表 / 分表）；  
    - 负责初始化默认语言、维护当前语言字符串 `_currentLanguage`，并驱动 I2Localization。

- **工具与数据层**  
  - `Language`：统一语言枚举；  
  - `LocalizationUtility`：  
    - 映射 `Application.systemLanguage -> Language`；  
    - 提供 `Language <-> string`（如 `Language.ChineseSimplified` ↔ `"Chinese"`）；  
    - 定义 CSV 资源名前缀 `I2_`；  
  - `Core/` 下为 I2Localization 源码：`LanguageSourceData`、`Localization.LocalizationManager`、`GoogleTranslation` 等。

---

## 二、关键代码片段

### 1. 模块接口与转发

**`ILocalizationModule` 主要接口：**

```5:59:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/ILocalizationModule.cs
```

包括：
- `Language Language { get; set; }`
- `Language SystemLanguage { get; }`
- `UniTask LoadLanguageTotalAsset(string assetName)`
- `UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)`
- `bool CheckLanguage(string language)`
- `bool SetLanguage(Language language, bool load = false)`
- `bool SetLanguage(string language, bool load = false)`
- `bool SetLanguage(int languageId)`

**`LocalizationModule` 绑定组件并简单转发：**

```5:112:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationModule.cs
```

- `Bind(LocalizationManager localizationManager)` 持有组件引用；  
- `Shutdown()` 中 `Destroy(_localizationManager);`；  
- 各属性/方法（`Language`、`SystemLanguage`、`LoadLanguage*`、`SetLanguage*` 等）全部直接调用 `_localizationManager` 对应实现。

### 2. `LocalizationManager` 初始化与语言加载

**挂钩模块系统：**

```66:78:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- 从 `ModuleSystem` 获取 `IResourceModule`；  
- 创建 `LocalizationModule`，`Bind(this)`；  
- 注册到模块系统：`ModuleSystem.RegisterModule<ILocalizationModule>(localizationModule);`。

**默认语言与初始化流程：**

```80:119:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- 根据 `RootModule.EditorLanguage` 或 `LocalizationUtility.SystemLanguage` 决定 `_defaultLanguage` 字符串；  
- `AsyncInit()` 中：
  - 编辑器 + `!useRuntimeModule`：`RegisterSourceInEditor()` + `UpdateAllLanguages()` + `SetLanguage(_defaultLanguage)`；  
  - 其他情况：`_sourceData.Awake()` + `LoadLanguage(_defaultLanguage, true, true)`（使用 `innerLocalizationCsv`）。

**加载总表与分表：**

```121:144:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- `LoadLanguageTotalAsset(assetName)`：  
  - 通过 `_resourceModule.LoadAssetAsync<TextAsset>(assetName)` 加载总表 CSV；  
  - 成功后 `UseLocalizationCSV(asset.text, true)`（Merge + `LocalizeAll`）。

```146:193:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- `LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)`：  
  - `fromInit == false`：用 `I2_<language>` 命名的 CSV 通过资源模块加载；  
  - `fromInit == true`：使用 `innerLocalizationCsv`；  
  - 之后 `UseLocalizationCSV(text, !setCurrent)`，如 `setCurrent` 为真再调用 `SetLanguage(language)`。

**切换语言与可用性检查：**

```203:222:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- `UpdateAllLanguages()`：利用 I2 的 `Localization.LocalizationManager.GetAllLanguages()` 填充 `allLanguage` 列表；  
- `CheckLanguage(language)`：`allLanguage.Contains(language)`。

```225:281:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- `SetLanguage(string language, bool load = false)`：  
  - 不存在且 `load == true`：发起 `LoadLanguage(language, true).Forget()` 并返回 `true`；  
  - 不存在且 `load == false`：警告并返回 `false`；  
  - 已是当前语言：直接返回 `true`；  
  - 否则设置 `Localization.LocalizationManager.CurrentLanguage` 与 `_currentLanguage`。

**导入 CSV 至 I2：**

```283:292:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
```

- `_sourceData.Import_CSV(string.Empty, text, eSpreadsheetUpdateMode.Merge, ',');`  
- 根据 `isLocalizeAll` 决定是否立刻 `Localization.LocalizationManager.LocalizeAll();`；  
- 最后 `UpdateAllLanguages();`。

### 3. `LocalizationUtility` 与 `Language` 映射

```9:133:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationUtility.cs
```

- `SystemLanguage` 把 `Application.systemLanguage` 映射为 `Language` 枚举；  
- 静态构造中 `RegisterLanguageMap` 预置 `"English"`, `"Chinese"`, `"ChineseTraditional"`, `"Japanese"`, `"Korean"` 等映射；  
- `GetLanguage(str)` / `GetLanguageStr(Language)` 提供字符串和枚举的双向转换；  
- `I2ResAssetNamePrefix = "I2_"` 用于生成单语言 CSV 资源名。

```1:262:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Language.cs
```

- 定义了丰富的语言枚举（中英日韩及多国语言），作为模块对外统一语言标识。

---

## 三、Editor 端 Google WebService 报错的根因与配置要点

### 1. 报错来源：`CanTranslate()` 条件不满足

当在 Localization Editor 中点 “Translate / Translate All” 时，调用链：

- `LocalizationEditor.Translate(...)` → `GoogleTranslation.CanTranslate()`  

```11:15:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/Core/Google/GoogleTranslation.cs
public static bool CanTranslate ()
{
	return LocalizationManager.Sources.Count > 0 && 
	       !string.IsNullOrEmpty (LocalizationManager.GetWebServiceURL());
}
```

`GetWebServiceURL` 实现：

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

**因此只有同时满足：**

1. 有至少一个 `LanguageSourceData` 在 `LocalizationManager.Sources` 中；  
2. 至少有一个 Source 的 `Google_WebServiceURL` 非空；  

时，Editor 自动翻译才不会报 `WebService is not set correctly or needs to be reinstalled`。

### 2. `I2Languages.asset` 与 Editor Source

在 Editor 中通过下列逻辑从 `Assets/Editor/I2Localization/I2Languages.asset` 加载全局 Source：

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

**建议：**

- 确保项目中存在 `Assets/Editor/I2Localization/I2Languages.asset`，否则 Source 加载失败，`Sources.Count` 可能为 0；  
- 若缺失，可从原始 I2Localization 包重新导入，或手动创建新的 `LanguageSourceAsset` 作为全局 Source。

### 3. 在 Editor 中配置 / 验证 Google WebService

在 `Localization` 编辑器的 `Spreadsheets → Google` 页签中：

- 在 `Web Service URL` 文本框中填写已部署的 Google Apps Script Web 应用 URL（`.../exec`）；  
- 使用 `Install` 按钮会打开官方 V5 脚本副本链接，按步骤部署 WebApp；  
- 使用 `Verify` 按钮通过 `?action=Ping` 检测版本：

```422:471:UnityProject/Assets/TEngine/Editor/Localization/Localization/LocalizationEditor_Spreadsheet_Google.cs
void OnVerifyGoogleService( string Result, string Error )
{
	...
	int requiredVersion = LocalizationManager.GetRequiredWebServiceVersion(); // 5
	...
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
}
```

**只有当 `mWebService_Status == "Online"` 时，`GetWebServiceURL()` 返回有效地址，`CanTranslate()` 才能通过。**

---

## 四、不依赖 Google WebService 的推荐工作流（仅本地 CSV）

若团队不想配置 Google 表格同步与在线翻译，可以采用 **纯本地 CSV + Runtime CSV 导入** 的流程：

1. **编辑器内维护所有多语言内容**  
   - 把 `I2Languages.asset` 作为“单一真源”，所有 Term / 翻译都在其中维护；  
   - 仅使用 `Spreadsheets → Local` 的 CSV Import/Export，不点击 Google 的 Import/Export/Translate 按钮。

2. **导出 CSV 供运行时使用**  
   - 在 Editor 中从 `I2Languages.asset` 导出 CSV（总表或分语言表）；  
   - 把 CSV 拖入 Unity 工程、设置为 `TextAsset`，并打入 AssetBundle / Addressables；  
   - 对单语言表使用 `I2_<Language>` 命名（如 `I2_Chinese`、`I2_English`），以匹配：

   ```195:198:UnityProject/Assets/TEngine/Runtime/Module/LocalizationModule/LocalizationManager.cs
   private string GetLanguageAssetName(string language)
   {
       return $"{LocalizationUtility.I2ResAssetNamePrefix}{language}";
   }
   ```

3. **运行时加载与切换**  
   - 启动时通过 `innerLocalizationCsv` 或 `LoadLanguageTotalAsset` 导入初始 CSV；  
   - 后续按需通过 `ILocalizationModule.LoadLanguage("Chinese", setCurrent: true)` 等接口加载并切换语言。

4. **避免再触发 WebService 报错**  
   - 约定团队不使用 Editor 里的自动翻译按钮；  
   - 所有翻译由人工维护或通过外部工具离线生成，再通过 CSV 导入。

---

## 五、实用调用 Sample 汇总

### 1. 启动时强制切到英文（如未加载则自动加载）

```csharp
using UnityEngine;
using TEngine;

public class LanguageSwitcher : MonoBehaviour
{
    private ILocalizationModule _loc;

    private void Start()
    {
        _loc = ModuleSystem.GetModule<ILocalizationModule>();
        _loc.SetLanguage(Language.English, load: true);
    }
}
```

### 2. 基于系统语言选择默认语言

```csharp
using Cysharp.Threading.Tasks;
using TEngine;

public class LocalizationInitializer
{
    public async UniTask InitAsync()
    {
        var loc = ModuleSystem.GetModule<ILocalizationModule>();

        // 可选：先加载一个包含多语言的总表
        await loc.LoadLanguageTotalAsset("I2_AllLanguages");

        // 使用系统语言或玩家设置
        loc.SetLanguage(loc.SystemLanguage);
    }
}
```

### 3. UI 按钮切换多语言

```csharp
using UnityEngine;
using TEngine;

public class LanguageButtonPanel : MonoBehaviour
{
    private ILocalizationModule _loc;

    private void Awake()
    {
        _loc = ModuleSystem.GetModule<ILocalizationModule>();
    }

    public void OnClickChinese()
    {
        _loc.SetLanguage(Language.ChineseSimplified, load: true);
    }

    public void OnClickEnglish()
    {
        _loc.SetLanguage(Language.English, load: true);
    }
}
```

### 4. 只依赖 Editor 本地 CSV，不使用 WebService 的团队约定示例

- **工程约定：**
  - 所有翻译维护在 `Assets/Editor/I2Localization/I2Languages.asset`；  
  - 使用 `Spreadsheets → Local` 导出 CSV 给运行时；  
  - **禁止**在项目中使用 “Spreadsheets → Google” 页签上的 Import/Export/Translate 按钮。

- **运行时约定：**
  - 场景中有 `LocalizationManager` 组件，`useRuntimeModule` 按需要配置；  
  - 所有业务代码只通过 `ILocalizationModule` 访问本地化（不直接操作 I2 静态类），便于未来替换实现。

---

（摘要完）


