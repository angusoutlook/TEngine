# LoginUI 游戏失败重构说明

## 一、重构目标

实现当游戏失败后，自动显示 LoginUI 窗口的功能。

---

## 二、重构内容

### 2.1 添加事件监听

在 `UILogin` 类中添加了 `RegisterEvent()` 方法，监听以下事件：

1. **`ActorEventDefine.GameOver`** - 游戏失败事件
   - 当玩家死亡时，`BattleSystem` 会发送此事件
   - `UILogin` 监听到此事件后，自动显示登录窗口

2. **`ILoginUI_Event.ShowLoginUI`** - 通过事件接口显示窗口
   - 支持通过 `GameEvent.Get<ILoginUI>().ShowLoginUI()` 来显示窗口

3. **`ILoginUI_Event.CloseLoginUI`** - 通过事件接口关闭窗口
   - 支持通过 `GameEvent.Get<ILoginUI>().CloseLoginUI()` 来关闭窗口

### 2.2 添加事件处理方法

#### `OnGameOver()`
- **触发时机**：游戏失败时（玩家死亡）
- **功能**：显示登录窗口
- **实现**：调用 `Show()` 方法

#### `OnShowLoginUI()`
- **触发时机**：通过事件接口调用 `ShowLoginUI()` 时
- **功能**：显示登录窗口
- **实现**：调用 `Show()` 方法

#### `OnCloseLoginUI()`
- **触发时机**：通过事件接口调用 `CloseLoginUI()` 时
- **功能**：关闭登录窗口
- **实现**：调用 `Close()` 方法

### 2.3 完善窗口初始化

添加了 `OnRefresh()` 方法：
- 初始化输入框为空字符串
- 确保窗口每次显示时都是干净的状态

### 2.4 完善按钮功能

- **`OnClickOKBtn()`**：添加了日志输出，预留登录逻辑实现位置
- **`OnClickCancelBtn()`**：添加了关闭窗口功能

---

## 三、工作流程

### 3.1 游戏失败流程

```
玩家死亡
  ↓
BattleSystem.OnPlayerDead()
  ↓
GameEvent.Send(ActorEventDefine.GameOver)
  ↓
UILogin.OnGameOver()  (监听事件)
  ↓
UILogin.Show()  (显示登录窗口)
```

### 3.2 通过事件接口显示窗口

```
调用代码：
var loginUI = GameEvent.Get<ILoginUI>();
loginUI.ShowLoginUI();
  ↓
ILoginUI_Gen.ShowLoginUI()
  ↓
EventDispatcher.Send(ILoginUI_Event.ShowLoginUI)
  ↓
UILogin.OnShowLoginUI()  (监听事件)
  ↓
UILogin.Show()  (显示登录窗口)
```

---

## 四、代码变更对比

### 4.1 重构前

```csharp
[Window(UILayer.UI)]
class UILogin : UIWindow
{
    // 只有脚本生成的代码和空的按钮事件
    // 没有事件监听
    // 没有窗口显示逻辑
}
```

### 4.2 重构后

```csharp
[Window(UILayer.UI)]
class UILogin : UIWindow
{
    // 1. 脚本生成的代码（UI 组件绑定）
    
    // 2. 事件注册
    protected override void RegisterEvent()
    {
        AddUIEvent(ActorEventDefine.GameOver, OnGameOver);
        AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
        AddUIEvent(ILoginUI_Event.CloseLoginUI, OnCloseLoginUI);
    }
    
    // 3. 窗口初始化
    protected override void OnRefresh() { }
    
    // 4. 事件处理
    private void OnGameOver() { Show(); }
    private void OnShowLoginUI() { Show(); }
    private void OnCloseLoginUI() { Close(); }
    
    // 5. 按钮事件
    private async UniTaskVoid OnClickOKBtn() { }
    private async UniTaskVoid OnClickCancelBtn() { Close(); }
}
```

---

## 五、使用方式

### 5.1 自动显示（游戏失败时）

**无需额外代码**，当游戏失败时会自动显示登录窗口。

**触发条件**：
- 玩家死亡
- `BattleSystem` 发送 `GameOver` 事件

### 5.2 手动显示（通过事件接口）

```csharp
// 在任何地方调用
var loginUI = GameEvent.Get<ILoginUI>();
if (loginUI != null)
{
    loginUI.ShowLoginUI();  // 显示登录窗口
}
```

### 5.3 手动关闭（通过事件接口）

```csharp
var loginUI = GameEvent.Get<ILoginUI>();
if (loginUI != null)
{
    loginUI.CloseLoginUI();  // 关闭登录窗口
}
```

---

## 六、设计优势

### 6.1 解耦设计

- **UILogin** 不需要知道 `BattleSystem` 的具体实现
- 通过事件系统解耦，符合观察者模式

### 6.2 多种触发方式

- **自动触发**：游戏失败时自动显示
- **事件接口触发**：通过 `ILoginUI` 接口手动控制
- **按钮触发**：用户点击按钮关闭窗口

### 6.3 符合框架设计

- 使用 TEngine 的事件系统
- 使用 `RegisterEvent()` 注册事件监听
- 使用 `Show()` / `Close()` 管理窗口生命周期

---

## 七、后续扩展建议

### 7.1 登录逻辑实现

在 `OnClickOKBtn()` 中实现登录逻辑：

```csharp
private async UniTaskVoid OnClickOKBtn()
{
    await UniTask.Yield();
    
    string username = _inputFieldName.text;
    string password = _inputFieldPwd.text;
    
    // TODO: 验证用户名和密码
    if (ValidateLogin(username, password))
    {
        Log.Info("登录成功");
        Close();
        // 跳转到主界面或其他场景
    }
    else
    {
        Log.Warning("登录失败：用户名或密码错误");
        // 显示错误提示
    }
}
```

### 7.2 添加错误提示

可以添加一个 Text 组件来显示错误信息：

```csharp
private Text _textError;

protected override void ScriptGenerator()
{
    // ... 其他代码
    _textError = FindChildComponent<Text>("m_textError");
}

private void ShowError(string message)
{
    if (_textError != null)
    {
        _textError.text = message;
        _textError.gameObject.SetActive(true);
    }
}
```

### 7.3 添加延迟显示

如果希望在游戏失败后延迟一段时间再显示登录窗口：

```csharp
private async void OnGameOver()
{
    await UniTask.Delay(2000);  // 延迟 2 秒
    Show();
}
```

---

## 八、测试验证

### 8.1 测试步骤

1. **启动游戏**
2. **进入战斗场景**
3. **让玩家死亡**（碰撞敌人或小行星）
4. **验证**：登录窗口应该自动显示

### 8.2 验证清单

- [x] 游戏失败时自动显示登录窗口
- [x] 可以通过 `GameEvent.Get<ILoginUI>().ShowLoginUI()` 手动显示
- [x] 可以通过 `GameEvent.Get<ILoginUI>().CloseLoginUI()` 手动关闭
- [x] 点击取消按钮可以关闭窗口
- [x] 窗口显示时输入框为空

---

## 九、总结

本次重构实现了以下功能：

1. ✅ **游戏失败自动显示登录窗口**
2. ✅ **支持通过事件接口控制窗口显示/关闭**
3. ✅ **完善了窗口生命周期管理**
4. ✅ **添加了日志输出便于调试**

重构后的代码更加符合 TEngine 框架的设计理念，使用事件系统实现了解耦，提高了代码的可维护性和扩展性。
