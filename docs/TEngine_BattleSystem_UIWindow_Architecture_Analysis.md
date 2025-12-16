# TEngine BattleSystem 和 UIWindow 架构分析

## 会话记录

### 第一部分：BattleSystem.cs 代码分析

#### 类的基本结构

`BattleSystem` 继承自 `Singleton<BattleSystem>`（单例模式），负责管理战斗房间的整个生命周期和逻辑。

#### 核心组件

**1. 状态机系统（ESteps 枚举）**

```csharp
private enum ESteps
{
    None,        // 初始状态
    Ready,       // 准备阶段（等待开始）
    Spawn,       // 生成敌人/小行星
    WaitSpawn,   // 生成间隔等待
    WaitWave,    // 波次间隔等待
    GameOver,    // 游戏结束
}
```

**2. 关卡参数配置**

```csharp
// 关卡参数
private const int EnemyCount = 10;        // 每波敌人数量
private const int EnemyScore = 10;        // 击败敌人得分
private const int AsteroidScore = 1;       // 击碎小行星得分
private readonly Vector3 _spawnValues = new Vector3(6, 0, 20);  // 生成范围

private readonly string[] _entityLocations = new string[]
{
    "asteroid01", "asteroid02", "asteroid03", "enemy_ship"
};
```

#### 关键方法

**1. LoadRoom() - 加载战斗房间**

```csharp
public async UniTaskVoid LoadRoom()
{
    _startWaitTimer = 1f;
    await UniTask.Yield();
    
    // 创建房间根对象
    _roomRoot = new GameObject("BattleRoom");
    
    // 加载背景音乐
    GameModule.Audio.Play(AudioType.Music, "music_background", true);
    
    // 创建玩家实体对象
    var handle = PoolManager.Instance.GetGameObject("player_ship", parent: _roomRoot.transform);
    var entity = handle.GetComponent<EntityPlayer>();
    
    // 显示战斗界面
    GameModule.UI.ShowUIAsync<UIBattleWindow>();
    
    // 监听游戏事件
    GameEvent.AddEventListener<Vector3, Quaternion>(ActorEventDefine.PlayerDead, OnPlayerDead);
    GameEvent.AddEventListener<Vector3, Quaternion>(ActorEventDefine.EnemyDead, OnEnemyDead);
    GameEvent.AddEventListener<Vector3, Quaternion>(ActorEventDefine.AsteroidExplosion, OnAsteroidExplosion);
    GameEvent.AddEventListener<Vector3, Quaternion>(ActorEventDefine.PlayerFireBullet, OnPlayerFireBullet);
    GameEvent.AddEventListener<Vector3, Quaternion>(ActorEventDefine.EnemyFireBullet, OnEnemyFireBullet);
    
    _steps = ESteps.Ready;
}
```

**2. UpdateRoom() - 房间状态更新**

状态流转逻辑：
- **Ready**：倒计时1秒后进入 Spawn
- **Spawn**：随机选择实体类型和位置生成，计数+1；达到10个进入 WaitWave，否则进入 WaitSpawn
- **WaitSpawn**：等待0.75秒后继续生成
- **WaitWave**：等待4秒后重置计数，开始下一波

**3. 事件处理**

- `OnPlayerDead`：创建玩家爆炸特效，进入 GameOver 状态，发送游戏结束事件
- `OnEnemyDead`：创建敌人爆炸特效，得分+10，发送分数变更事件
- `OnAsteroidExplosion`：创建小行星爆炸特效，得分+1，发送分数变更事件
- `OnPlayerFireBullet` / `OnEnemyFireBullet`：从对象池获取子弹，设置位置和旋转，初始化实体

#### 设计要点

1. **单例模式**：全局唯一，便于访问
2. **对象池**：使用 `PoolManager` 管理实体，减少创建/销毁开销
3. **事件驱动**：通过 `GameEvent` 解耦实体与系统
4. **状态机**：用 `ESteps` 管理战斗流程
5. **波次生成**：每波10个，间隔0.75秒，波次间隔4秒
6. **资源管理**：通过 `_roomRoot` 统一管理战斗场景对象，便于清理

---

### 第二部分：非 MonoBehaviour 类创建 GameObject 的问题

#### 问题：类不是 MonoBehaviour，可以创建 GameObject 吗？

**答案：可以！**

在 Unity 中，`new GameObject()` 可以在任何 C# 代码中调用，不限于 MonoBehaviour。

#### 创建的 GameObject 放在哪里？

通过 `new GameObject()` 创建的 GameObject 会**自动添加到当前活动场景的根节点**（场景层级视图的顶层）。

在代码中：
- `_roomRoot = new GameObject("BattleRoom")` 会在当前场景根节点创建名为 "BattleRoom" 的 GameObject
- 后续通过 `PoolManager.Instance.GetGameObject(..., parent: _roomRoot.transform)` 创建的对象会作为 `_roomRoot` 的子对象

#### 关键点说明

1. **Unity API 的调用位置**：
   - `GameObject`、`Transform`、`Object` 等 Unity API 可以在任何 C# 代码中使用
   - 不要求调用者必须是 MonoBehaviour

2. **场景归属**：
   - `new GameObject()` 会添加到当前活动场景
   - 如果需要在特定场景创建，可以使用 `SceneManager` 相关 API

3. **生命周期管理**：
   - 代码中通过 `_roomRoot` 统一管理战斗相关对象
   - 销毁时调用 `Object.Destroy(_roomRoot)` 会销毁该对象及其所有子对象

#### 实际运行时的层级结构

```
场景根节点
└── BattleRoom (_roomRoot)
    ├── player_ship
    ├── enemy_ship (多个)
    ├── asteroid01/02/03 (多个)
    ├── player_bullet (多个)
    ├── enemy_bullet (多个)
    └── explosion_* (多个特效)
```

这种设计的好处：
- **统一管理**：所有战斗对象都在 `_roomRoot` 下，便于清理
- **场景隔离**：战斗对象与场景其他对象分离
- **便于调试**：在 Hierarchy 中可以清楚看到战斗相关的对象

---

### 第三部分：UIBattleWindow 的设计原理和架构分析

#### 1. 类继承结构

```
UIBase (普通C#类)
  └── UIWindow (普通C#类)
      └── UIBattleWindow (普通C#类)
```

**关键点**：`UIBattleWindow` 不是 `MonoBehaviour`，而是普通 C# 类。

#### 2. 如何访问 Canvas 下的对象？

##### 核心机制：对象引用绑定

`UIBattleWindow` 通过以下方式访问 UI 对象：

**步骤1：Prefab 实例化并关联**

```csharp
internal async UniTaskVoid InternalLoad(string location, Action<UIWindow> prepareCallback, bool isAsync, System.Object[] userDatas)
{
    _prepareCallback = prepareCallback;
    this._userDatas = userDatas;
    if (!FromResources)
    {
        if (isAsync)
        {
            var uiInstance = await UIModule.Resource.LoadGameObjectAsync(location, parent: UIModule.UIRoot);
            Handle_Completed(uiInstance);
        }
        else
        {
            var uiInstance = UIModule.Resource.LoadGameObject(location, parent: UIModule.UIRoot);
            Handle_Completed(uiInstance);
        }
    }
    else
    {
        GameObject panel = Object.Instantiate(Resources.Load<GameObject>(location), UIModule.UIRoot);
        Handle_Completed(panel);
    }
}
```

**步骤2：保存 GameObject 引用**

```csharp
private void Handle_Completed(GameObject panel)
{
    if (panel == null)
    {
        return;
    }

    IsLoadDone = true;
    
    if (IsDestroyed)
    {
        Object.Destroy(panel);
        return;
    }
    
    panel.name = GetType().Name;
    _panel = panel;  // 保存 GameObject 引用
    _panel.transform.localPosition = Vector3.zero;

    // 获取组件
    _canvas = _panel.GetComponent<Canvas>();
    if (_canvas == null)
    {
        throw new Exception($"Not found {nameof(Canvas)} in panel {WindowName}");
    }

    _canvas.overrideSorting = true;
    _canvas.sortingOrder = 0;
    _canvas.sortingLayerName = "Default";

    // 获取组件
    _raycaster = _panel.GetComponent<GraphicRaycaster>();
    _childCanvas = _panel.GetComponentsInChildren<Canvas>(true);
    _childRaycaster = _panel.GetComponentsInChildren<GraphicRaycaster>(true);

    // 通知UI管理器
    IsPrepare = true;
    _prepareCallback?.Invoke(this);
}
```

**步骤3：通过 rectTransform 查找子对象**

```csharp
public Transform FindChild(string path)
{
    return FindChildImp(rectTransform, path);
}

public T FindChildComponent<T>(string path) where T : Component
{
    return FindChildComponentImp<T>(rectTransform, path);
}

private static Transform FindChildImp(Transform transform, string path)
{
    var findTrans = transform.Find(path);
    return findTrans != null ? findTrans : null;
}

private static T FindChildComponentImp<T>(Transform transform, string path) where T : Component
{
    var findTrans = transform.Find(path);
    if (findTrans != null)
    {
        return findTrans.gameObject.GetComponent<T>();
    }
    return null;
}
```

**rectTransform 属性定义**：

```csharp
public override Transform transform => _panel.transform;

/// <summary>
/// 窗口矩阵位置组件。
/// </summary>
public override RectTransform rectTransform => _panel.transform as RectTransform;

/// <summary>
/// 窗口的实例资源对象。
/// </summary>
public override GameObject gameObject => _panel;
```

#### 3. 完整创建流程

```
GameModule.UI.ShowUIAsync<UIBattleWindow>
    ↓
UIModule.ShowUIImp
    ↓
CreateInstance 创建类实例
    ↓
读取 Window 特性配置
    ↓
InternalLoad 加载 Prefab
    ↓
LoadGameObjectAsync 实例化到 UIRoot
    ↓
Handle_Completed 保存引用
    ↓
设置 _panel = panel
    ↓
获取 Canvas 组件
    ↓
IsPrepare = true
    ↓
OnWindowPrepare 回调
    ↓
InternalCreate
    ↓
ScriptGenerator 查找子对象
    ↓
FindChildComponent 通过 rectTransform.Find
```

#### 4. 关键设计点

##### 4.1 分离设计模式

- **逻辑类**（`UIBattleWindow`）：普通 C# 类，不依赖 Unity 生命周期
- **UI 资源**（Prefab）：独立的 GameObject 资源
- **运行时关联**：通过 `_panel` 字段关联

##### 4.2 Window 特性配置

```csharp
[Window(UILayer.UI)]
class UIBattleWindow : UIWindow
```

- `UILayer.UI`：窗口层级
- 自动读取资源路径（默认使用类名 `UIBattleWindow`）
- 配置窗口属性（全屏、隐藏时间等）

##### 4.3 UIRoot 管理

```csharp
protected override void OnInit()
{
    var uiRoot = GameObject.Find("UIRoot");
    if (uiRoot != null)
    {
        _instanceRoot = uiRoot.GetComponentInChildren<Canvas>()?.transform;
        _uiCamera = uiRoot.GetComponentInChildren<Camera>();
    }
    else
    {
        Log.Fatal("UIRoot not found !");
        return;
    }
```

- 场景中需要存在名为 `UIRoot` 的 GameObject
- 所有 UI 窗口都作为 `UIRoot` 的子对象
- 使用 `DontDestroyOnLoad` 保持跨场景

##### 4.4 查找子对象的原理

在 `ScriptGenerator` 中：

```csharp
protected override void ScriptGenerator()
{
    m_textScore = FindChildComponent<Text>("ScoreView/m_textScore");
    m_goOverView = FindChild("m_goOverView").gameObject;
    m_btnRestart = FindChildComponent<Button>("m_goOverView/m_btnRestart");
    m_btnHome = FindChildComponent<Button>("m_goOverView/m_btnHome");
    m_btnRestart.onClick.AddListener(UniTask.UnityAction(OnClickRestartBtn));
    m_btnHome.onClick.AddListener(UniTask.UnityAction(OnClickHomeBtn));
}
```

**工作原理**：
1. `FindChildComponent<Text>("ScoreView/m_textScore")` 调用
2. 内部使用 `rectTransform.Find("ScoreView/m_textScore")`
3. `rectTransform` 指向 `_panel.transform`（即 Prefab 实例的根 Transform）
4. Unity 的 `Transform.Find()` 支持路径查找（如 `"Parent/Child"`）

#### 5. 架构优势

1. **逻辑与视图分离**：代码不依赖 MonoBehaviour，便于测试和热更新
2. **统一管理**：通过 `UIModule` 管理窗口生命周期、层级、显示隐藏
3. **资源管理**：统一加载和卸载，支持异步加载
4. **类型安全**：通过泛型和特性提供编译期检查
5. **事件系统**：内置 UI 事件管理，自动注册和清理

#### 6. 总结

`UIBattleWindow` 不是 MonoBehaviour，但可以访问 Canvas 下的对象，因为：

1. **通过 UIModule 加载 Prefab**：Prefab 被实例化到场景中
2. **保存 GameObject 引用**：在 `UIWindow` 中保存 `_panel` 引用（即 Prefab 实例）
3. **暴露 Transform**：通过 `rectTransform` 属性暴露 Transform，用于查找子对象
4. **路径查找**：`FindChild`/`FindChildComponent` 使用 Unity 的 `Transform.Find()` 进行路径查找

这是一种**组合模式**：逻辑类持有 GameObject 引用，通过委托访问 Unity 对象，而非继承 MonoBehaviour。

---

## 代码文件位置

- `BattleSystem.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Demo/BattleSystem.cs`
- `UIBattleWindow.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Demo/UI/UIBattleWindow.cs`
- `UIWindow.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs`
- `UIBase.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIBase.cs`
- `UIModule.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`
- `WindowAttribute.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs`
- `Singleton.cs`: `UnityProject/Assets/GameScripts/HotFix/GameLogic/SingletonSystem/Singleton.cs`

---

## 生成时间

文档生成时间：2024年
