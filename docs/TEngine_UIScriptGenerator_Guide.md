# TEngine UI 脚本生成器使用指南

## 一、概述

TEngine 提供了一个**UI 脚本生成器**（ScriptGenerator），可以根据 Prefab 的层级结构自动生成 UI 绑定代码。这个工具可以大大减少手动编写 UI 组件绑定代码的工作量。

**位置**：`UnityProject/Assets/Editor/UIScriptGenerator/ScriptGenerator.cs`

---

## 二、使用方法

### 2.1 基本使用步骤

1. **在 Unity Editor 中打开 Prefab**
   - 选择要生成代码的 Prefab 文件
   - 在 Hierarchy 窗口中选择 Prefab 的根对象

2. **右键菜单生成代码**
   - 右键点击选中的 GameObject
   - 选择菜单项：`GameObject/ScriptGenerator/...`

3. **选择生成模式**
   - `UIProperty` - 只生成属性绑定代码
   - `UIProperty - UniTask` - 生成属性绑定代码（使用 UniTask）
   - `UIPropertyAndListener` - 生成属性绑定 + 事件监听代码
   - `UIPropertyAndListener - UniTask` - 生成属性绑定 + 事件监听代码（使用 UniTask）

4. **复制生成的代码**
   - 代码会自动复制到剪贴板
   - 在 Unity Console 中会显示提示："脚本已生成到剪贴板，请自行Ctl+V粘贴"
   - 粘贴到对应的 C# 脚本文件中

### 2.2 菜单项说明

| 菜单项 | 功能 | 生成内容 |
|--------|------|---------|
| `UIProperty` | 只生成属性绑定 | 字段声明 + `ScriptGenerator()` 方法 |
| `UIProperty - UniTask` | 只生成属性绑定（UniTask） | 字段声明 + `ScriptGenerator()` 方法（UniTask 版本） |
| `UIPropertyAndListener` | 生成属性 + 事件监听 | 字段声明 + `ScriptGenerator()` + 事件回调方法 |
| `UIPropertyAndListener - UniTask` | 生成属性 + 事件监听（UniTask） | 字段声明 + `ScriptGenerator()` + 异步事件回调方法 |

---

## 三、命名规则

### 3.1 UI 元素命名规则

脚本生成器根据 GameObject 的名称前缀来识别 UI 组件类型：

| 前缀 | 组件类型 | 示例 |
|------|---------|------|
| `m_go` | GameObject | `m_goOverView` |
| `m_item` | GameObject | `m_itemShopItem` |
| `m_tf` | Transform | `m_tfContainer` |
| `m_rect` | RectTransform | `m_rectPanel` |
| `m_text` | Text | `m_textScore` |
| `m_richText` | RichTextItem | `m_richTextDescription` |
| `m_btn` | Button | `m_btnLogin` |
| `m_img` | Image | `m_imgIcon` |
| `m_rimg` | RawImage | `m_rimgBackground` |
| `m_scrollBar` | Scrollbar | `m_scrollBarProgress` |
| `m_scroll` | ScrollRect | `m_scrollContent` |
| `m_input` | InputField | `m_inputUsername` |
| `m_grid` | GridLayoutGroup | `m_gridItems` |
| `m_hlay` | HorizontalLayoutGroup | `m_hlayButtons` |
| `m_vlay` | VerticalLayoutGroup | `m_vlayList` |
| `m_slider` | Slider | `m_sliderVolume` |
| `m_group` | ToggleGroup | `m_groupOptions` |
| `m_curve` | AnimationCurve | `m_curveEasing` |
| `m_canvasGroup` | CanvasGroup | `m_canvasGroupPanel` |
| `m_tmp` | TextMeshProUGUI | `m_tmpTitle` |

### 3.2 代码风格

支持两种代码风格（在 `ScriptGeneratorSetting` 中配置）：

1. **UnderscorePrefix**（下划线前缀）
   - 字段名以 `_` 开头，如：`_btnLogin`

2. **MPrefix**（m_ 前缀）
   - 字段名以 `m_` 开头，如：`m_btnLogin`

---

## 四、生成代码示例

### 4.1 只生成属性绑定（UIProperty）

**生成的代码结构**：

```csharp
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class LoginUI : UIWindow
    {
        #region 脚本工具生成的代码

        private Text m_textTitle;
        private Button m_btnLogin;
        private GameObject m_goPanel;

        protected override void ScriptGenerator()
        {
            m_textTitle = FindChildComponent<Text>("m_textTitle");
            m_btnLogin = FindChildComponent<Button>("m_btnLogin");
            m_goPanel = FindChild("m_goPanel").gameObject;
        }
        
        #endregion
    }
}
```

### 4.2 生成属性 + 事件监听（UIPropertyAndListener）

**生成的代码结构**：

```csharp
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class LoginUI : UIWindow
    {
        #region 脚本工具生成的代码

        private Text m_textTitle;
        private Button m_btnLogin;
        private Toggle m_toggleRemember;
        private Slider m_sliderVolume;

        protected override void ScriptGenerator()
        {
            m_textTitle = FindChildComponent<Text>("m_textTitle");
            m_btnLogin = FindChildComponent<Button>("m_btnLogin");
            m_btnLogin.onClick.AddListener(OnClickLoginBtn);
            m_toggleRemember = FindChildComponent<Toggle>("m_toggleRemember");
            m_toggleRemember.onValueChanged.AddListener(OnToggleRememberChange);
            m_sliderVolume = FindChildComponent<Slider>("m_sliderVolume");
            m_sliderVolume.onValueChanged.AddListener(OnSliderVolumeChange);
        }
        
        #endregion

        #region 事件

        private void OnClickLoginBtn()
        {
        }

        private void OnToggleRememberChange(bool isOn)
        {
        }

        private void OnSliderVolumeChange(float value)
        {
        }
        
        #endregion
    }
}
```

### 4.3 使用 UniTask 版本

**生成的代码结构**（异步事件回调）：

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class LoginUI : UIWindow
    {
        #region 脚本工具生成的代码

        private Button m_btnLogin;

        protected override void ScriptGenerator()
        {
            m_btnLogin = FindChildComponent<Button>("m_btnLogin");
            m_btnLogin.onClick.AddListener(UniTask.UnityAction(OnClickLoginBtn));
        }
        
        #endregion

        #region 事件

        private async UniTaskVoid OnClickLoginBtn()
        {
            await UniTask.Yield();
            // 异步逻辑
        }
        
        #endregion
    }
}
```

---

## 五、配置设置

### 5.1 ScriptGeneratorSetting

脚本生成器的配置通过 `ScriptGeneratorSetting` ScriptableObject 来管理。

**创建配置**：
- 菜单：`TEngine/Create ScriptGeneratorSetting`
- 会在 `Assets/Editor/ScriptGeneratorSetting.asset` 创建配置文件

**配置项**：
- `CodePath` - 代码保存路径（可选）
- `Namespace` - 命名空间（默认：`GameLogic`）
- `WidgetName` - Widget 名称前缀（默认：`item`）
- `CodeStyle` - 代码风格（`UnderscorePrefix` 或 `MPrefix`）
- `ScriptGenerateRule` - UI 元素识别规则列表

### 5.2 查看规则

**查看当前规则**：
- 菜单：`GameObject/ScriptGenerator/About`
- 会打开一个窗口显示所有识别规则

---

## 六、实际使用示例

### 6.1 为 LoginUI Prefab 生成代码

**步骤**：

1. **创建 Prefab 结构**：
   ```
   LoginUI (GameObject)
   ├─ Canvas
   ├─ GraphicRaycaster
   ├─ m_textTitle (Text)
   ├─ m_btnLogin (Button)
   ├─ m_btnClose (Button)
   └─ m_goPanel (GameObject)
   ```

2. **在 Unity Editor 中**：
   - 选择 `LoginUI` Prefab 的根对象
   - 右键 → `GameObject/ScriptGenerator/UIPropertyAndListener - UniTask`

3. **生成的代码**：
   ```csharp
   using Cysharp.Threading.Tasks;
   using UnityEngine;
   using UnityEngine.UI;
   using TEngine;

   namespace GameLogic
   {
       [Window(UILayer.UI)]
       class LoginUI : UIWindow
       {
           #region 脚本工具生成的代码

           private Text m_textTitle;
           private Button m_btnLogin;
           private Button m_btnClose;
           private GameObject m_goPanel;

           protected override void ScriptGenerator()
           {
               m_textTitle = FindChildComponent<Text>("m_textTitle");
               m_btnLogin = FindChildComponent<Button>("m_btnLogin");
               m_btnLogin.onClick.AddListener(UniTask.UnityAction(OnClickLoginBtn));
               m_btnClose = FindChildComponent<Button>("m_btnClose");
               m_btnClose.onClick.AddListener(UniTask.UnityAction(OnClickCloseBtn));
               m_goPanel = FindChild("m_goPanel").gameObject;
           }
           
           #endregion

           #region 事件

           private async UniTaskVoid OnClickLoginBtn()
           {
               await UniTask.Yield();
           }

           private async UniTaskVoid OnClickCloseBtn()
           {
               await UniTask.Yield();
           }
           
           #endregion
       }
   }
   ```

4. **完善代码**：
   - 将生成的代码粘贴到 `LoginUI.cs`
   - 实现事件处理方法
   - 添加 `RegisterEvent()` 方法（如果需要）

---

## 七、工作原理

### 7.1 代码生成流程

```
1. 用户选择 Prefab 根对象
   ↓
2. 递归遍历所有子对象
   ↓
3. 根据命名规则识别 UI 组件类型
   ↓
4. 生成字段声明
   ↓
5. 生成 FindChildComponent 绑定代码
   ↓
6. 如果是 Button/Toggle/Slider，生成事件监听代码
   ↓
7. 生成事件回调方法模板
   ↓
8. 复制到剪贴板
```

### 7.2 路径计算

生成器会计算每个 UI 元素相对于根对象的路径：

```csharp
// 例如：LoginUI/m_goPanel/m_btnLogin
// 生成的路径：m_goPanel/m_btnLogin
string varPath = GetRelativePath(child, root);
```

### 7.3 事件方法命名规则

- **Button**：`OnClick{Name}Btn`
  - 例如：`m_btnLogin` → `OnClickLoginBtn`
  
- **Toggle**：`OnToggle{Name}Change`
  - 例如：`m_toggleRemember` → `OnToggleRememberChange`
  
- **Slider**：`OnSlider{Name}Change`
  - 例如：`m_sliderVolume` → `OnSliderVolumeChange`

---

## 八、注意事项

### 8.1 Prefab 命名要求

- **UIWindow**：Prefab 名称应该与类名一致（如 `LoginUI`）
- **UIWidget**：Prefab 名称应该以 `m_item` 或 `_item` 开头（根据代码风格）

### 8.2 命名规则必须遵守

- UI 元素必须按照规则命名（如 `m_btn`、`m_text` 等）
- 不符合命名规则的 GameObject 不会被识别

### 8.3 特殊处理

- **`m_item` 开头的对象**：不会继续向下遍历子对象（用于列表项）
- **Widget 识别**：如果 Prefab 名称以 Widget 前缀开头，会生成 `CreateWidgetByType` 代码

### 8.4 代码风格一致性

- 确保 Prefab 中的命名与 `ScriptGeneratorSetting` 中的代码风格一致
- 如果使用 `MPrefix`，所有 UI 元素应该以 `m_` 开头
- 如果使用 `UnderscorePrefix`，所有 UI 元素应该以 `_` 开头

---

## 九、常见问题

### Q1: 生成的代码在哪里？

**A**: 代码会复制到剪贴板，需要手动粘贴到对应的 C# 脚本文件中。

### Q2: 如何修改命名规则？

**A**: 
1. 创建或打开 `ScriptGeneratorSetting.asset`
2. 在 Inspector 中修改 `ScriptGenerateRule` 列表
3. 添加或修改规则（`uiElementRegex` 和 `componentName`）

### Q3: 如何支持新的 UI 组件类型？

**A**: 
1. 在 `ScriptGeneratorSetting` 中添加新规则
2. 例如：`new ScriptGenerateRuler("m_dropdown", "Dropdown")`

### Q4: 生成的代码不完整？

**A**: 
- 检查 Prefab 中的 GameObject 命名是否符合规则
- 确保选择了正确的根对象
- 检查 `ScriptGeneratorSetting` 配置是否正确

### Q5: 如何区分 UIWindow 和 UIWidget？

**A**: 
- **UIWindow**：Prefab 名称不以 Widget 前缀开头
- **UIWidget**：Prefab 名称以 `m_item` 或 `_item` 开头（根据代码风格）

---

## 十、总结

TEngine 的 UI 脚本生成器是一个**强大的工具**，可以：

1. ✅ **自动生成** UI 组件绑定代码
2. ✅ **自动生成** 事件监听代码
3. ✅ **支持** 多种 UI 组件类型
4. ✅ **支持** 两种代码风格
5. ✅ **支持** UniTask 异步事件

**使用建议**：
- 在创建 UI Prefab 时，严格按照命名规则命名
- 使用 `UIPropertyAndListener - UniTask` 模式生成完整代码
- 生成后手动完善业务逻辑代码

这样可以大大提高 UI 开发效率！
