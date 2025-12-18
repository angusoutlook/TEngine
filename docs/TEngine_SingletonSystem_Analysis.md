# TEngine SingletonSystem 架构分析

## 对话记录

### 用户问题1：介绍 SingletonSystem

**用户：** @UnityProject/Assets/GameScripts/HotFix/GameLogic/SingletonSystem 介绍一下SingletonSystem ，说明架构和设计意图，并举几个使用的例子

**AI回答：**

我是 Auto，一个由 Cursor 设计的 AI 编程助手。

基于您提供的实际代码，以下是 `SingletonSystem` 的架构和设计意图分析：

## SingletonSystem 架构分析

### 一、系统概述

`SingletonSystem` 是用于统一管理全局单例对象的系统，主要解决：
1. 统一管理全局对象和 Unity 场景依赖的 `DontDestroyOnLoad` 对象
2. 方便重启游戏时清除所有单例
3. 提供统一的生命周期管理（Update、FixedUpdate、LateUpdate 等）

### 二、核心组件架构

系统包含三个核心文件：

#### 1. **SingletonSystem.cs** - 核心管理系统

**核心数据结构：**
```csharp
- List<ISingleton> _singletons          // 所有单例对象列表
- List<IUpdate> _updates                // 需要Update的单例列表
- List<IFixedUpdate> _fixedUpdates      // 需要FixedUpdate的单例列表
- List<ILateUpdate> _lateUpdates        // 需要LateUpdate的单例列表
- Dictionary<string, GameObject> _gameObjects  // GameObject单例字典
```

**关键接口定义：**
- `ISingleton`: 单例基础接口（Active、Release）
- `IUpdate/IFixedUpdate/ILateUpdate`: 生命周期更新接口
- `IDrawGizmos/IDrawGizmosSelected`: 编辑器调试接口

**核心方法：**
- `Retain(ISingleton)`: 注册单例对象，自动检测并注册生命周期接口
- `Retain(GameObject, object)`: 注册带GameObject的单例，自动设置DontDestroyOnLoad
- `Release()`: 释放所有单例，用于游戏重启
- `BuildLifeCycle()`: 自动检测单例实现的接口，注册到对应的更新列表

**设计意图：**
- 统一管理：所有全局单例通过 `SingletonSystem` 注册，便于统一清理
- 自动生命周期：通过接口检测自动注册到更新循环，无需手动管理
- 场景切换支持：GameObject 单例自动设置 `DontDestroyOnLoad`，跨场景持久化

#### 2. **Singleton.cs** - 普通单例基类

**关键实现细节：**
```csharp
// 第30-39行：构造函数保护机制
protected Singleton()
{
#if UNITY_EDITOR
    // 通过堆栈跟踪检查，确保只能通过Instance属性实例化
    string st = new StackTrace().ToString();
    if (!st.Contains("GameLogic.Singleton`1[T].get_Instance"))
    {
        UnityEngine.Debug.LogError($"请必须通过Instance方法来实例化{typeof(T).FullName}类");
    }
#endif
}
```

**设计意图：**
- 懒加载：首次访问 `Instance` 时创建
- 自动注册：创建后自动调用 `SingletonSystem.Retain()` 注册
- 生命周期钩子：提供 `OnInit()` 和 `OnRelease()` 供子类重写
- 编辑器保护：在编辑器中检测非法实例化

#### 3. **SingletonBehaviour.cs** - MonoBehaviour单例基类

**关键实现细节：**
```csharp
// 第22-31行：实例检查机制
private bool CheckInstance()
{
    if (this == Instance)
        return true;
    Object.Destroy(gameObject);  // 销毁重复实例
    return false;
}
```

**设计意图：**
- Unity 集成：继承 MonoBehaviour，可在场景中挂载或动态创建
- 自动查找：优先从 `SingletonSystem` 获取，找不到则查找场景，最后创建新对象
- 防重复：Awake 时检查并销毁重复实例
- GameObject 管理：自动注册到 `SingletonSystem`，支持 DontDestroyOnLoad

### 三、使用示例

#### 示例1：BattleSystem（普通单例）

```csharp
public class BattleSystem : Singleton<BattleSystem>
{
    protected override void OnInit()
    {
        Utility.Unity.AddUpdateListener(Update);
        base.OnInit();
    }
    // ...
}
```

**使用方式：**
```csharp
// 访问单例
BattleSystem.Instance.LoadRoom();

// 检查有效性
if (BattleSystem.IsValid)
{
    // 使用单例
}

// 释放单例
BattleSystem.Instance.Release();
```

**特点：**
- 非 MonoBehaviour，纯逻辑类
- 通过 `OnInit()` 进行初始化
- 需要 Update 时手动添加监听（或实现 `IUpdate` 接口）

#### 示例2：UIModule（带Update的单例）

```csharp
public sealed partial class UIModule : Singleton<UIModule>, IUpdate
{
    protected override void OnInit()
    {
        var uiRoot = GameObject.Find("UIRoot");
        // ... 初始化逻辑 ...
    }
    
    public void OnUpdate()
    {
        // 每帧更新UI窗口
        if (_uiStack == null) return;
        // ... 更新逻辑 ...
    }
}
```

**使用方式：**
```csharp
// 访问单例
UIModule.Instance.ShowUIAsync<UIBattleWindow>();

// 自动Update：因为实现了IUpdate接口，SingletonSystem会自动调用OnUpdate()
```

**特点：**
- 实现 `IUpdate` 接口，自动注册到更新循环
- 在 `OnInit()` 中查找场景中的 UIRoot
- 通过 `OnRelease()` 清理资源

#### 示例3：使用 SingletonBehaviour（MonoBehaviour单例）

```csharp
// 假设有一个AudioManager需要MonoBehaviour功能
public class AudioManager : SingletonBehaviour<AudioManager>
{
    protected override void OnLoad()
    {
        // 初始化音频系统
        base.OnLoad();
    }
    
    // 可以使用MonoBehaviour的所有功能
    private void OnAudioFilterRead(float[] data, int channels)
    {
        // 音频处理
    }
}
```

**使用方式：**
```csharp
// 访问单例（自动创建GameObject）
AudioManager.Instance.PlaySound("click");

// 检查有效性
if (AudioManager.IsValid)
{
    // 使用
}

// 释放
AudioManager.Release();
```

**特点：**
- 继承 MonoBehaviour，可使用 Unity 生命周期
- 自动创建 GameObject，自动设置 DontDestroyOnLoad
- 支持在场景中预置或动态创建

### 四、设计亮点

1. **接口自动检测**：通过反射检测实现的接口，自动注册到对应生命周期列表
2. **统一清理**：`SingletonSystem.Release()` 可一次性清理所有单例，便于重启
3. **类型安全**：泛型约束确保类型正确
4. **编辑器保护**：在编辑器中检测非法实例化
5. **灵活的生命周期**：支持 Update、FixedUpdate、LateUpdate 等
6. **场景持久化**：GameObject 单例自动设置 DontDestroyOnLoad

### 五、使用建议

- **纯逻辑管理器**：使用 `Singleton<T>`
- **需要 MonoBehaviour 功能**：使用 `SingletonBehaviour<T>`
- **需要每帧更新**：实现 `IUpdate` 接口
- **需要物理更新**：实现 `IFixedUpdate` 接口
- **需要延迟更新**：实现 `ILateUpdate` 接口

该设计统一了单例管理，简化了全局对象的使用和清理。

---

### 用户问题2：Restart 函数中 LoadScene(0) 的含义

**用户：** singletonsystem 的Restart 函数中 locascene[0]是什么含义？

**AI回答：**

## `SceneManager.LoadScene(0)` 的含义

在 `Restart` 函数中：

```csharp
public static void Restart()
{
    if (Camera.main != null)
    {
        Camera.main.gameObject.SetActive(false);
    }

    Release();
    SceneManager.LoadScene(0);
}
```

### 参数说明

`SceneManager.LoadScene(0)` 中的 `0` 是场景索引，表示 Build Settings 中索引为 0 的场景（通常是第一个场景，即启动场景/主场景）。

### 详细解释

1. **场景索引**：Unity 的 Build Settings 中，场景按顺序排列，索引从 0 开始。
2. **索引 0**：通常是游戏的入口场景（如主菜单或初始化场景）。
3. **使用索引而非名称**：避免硬编码场景名称，但依赖 Build Settings 的顺序。

### 设计意图

- **重启到初始状态**：清理所有单例后，回到启动场景。
- **统一清理**：先调用 `Release()` 清理所有单例，再加载场景，避免残留对象。

### 注意事项

- **依赖 Build Settings**：索引 0 的场景必须是启动场景，否则可能加载错误场景。
- **更安全的做法**：可以改为使用场景名称，例如：
  ```csharp
  SceneManager.LoadScene("MainScene");  // 使用场景名称更明确
  ```
  或使用常量：
  ```csharp
  private const int MAIN_SCENE_INDEX = 0;
  SceneManager.LoadScene(MAIN_SCENE_INDEX);
  ```

### 总结

`LoadScene(0)` 表示加载 Build Settings 中索引为 0 的场景，用于重启时回到启动场景。确保该场景是启动场景，或改用场景名称以提高可维护性。

---

### 用户问题3：SingletonBehaviour 类详细解释

**用户：** @UnityProject/Assets/GameScripts/HotFix/GameLogic/SingletonSystem/SingletonBehaviour.cs 详细解释一下这个类

**AI回答：**

## SingletonBehaviour 类详细解析

### 一、类的基本结构

```csharp
public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    private static T _instance;
```

**设计要点：**
- **泛型单例**：`T` 必须是 `SingletonBehaviour<T>` 的子类（CRTP 模式）
- **继承 MonoBehaviour**：可使用 Unity 生命周期和组件系统
- **静态实例**：`_instance` 存储唯一实例

### 二、Awake 生命周期方法

```csharp
private void Awake()
{
    if (CheckInstance())
    {
        OnLoad();
    }
}
```

**执行流程：**
1. 调用 `CheckInstance()` 验证是否为有效实例
2. 通过验证则调用 `OnLoad()` 进行初始化

**设计意图：**
- 在 Unity 生命周期中自动验证，避免重复实例

### 三、实例检查机制（核心逻辑）

```csharp
private bool CheckInstance()
{
    if (this == Instance)
    {
        return true;
    }

    Object.Destroy(gameObject);
    return false;
}
```

**逻辑分析：**
1. 比较当前对象与 `Instance`（会触发实例创建/获取）
2. 如果相等，返回 true
3. 否则销毁当前对象，返回 false

**场景说明：**
- **场景中已存在该组件**：`Instance` 返回已存在的实例，当前对象被销毁
- **动态创建**：首次创建时，`Instance` 返回当前对象，通过检查

**设计意图：**
- 确保全局唯一，自动清理重复实例

### 四、OnLoad 初始化钩子

```csharp
protected virtual void OnLoad()
{
}
```

**用途：**
- 子类重写，在实例验证通过后执行初始化
- 替代在 `Awake` 中直接初始化，保证只在有效实例上执行

### 五、OnDestroy 清理机制

```csharp
protected virtual void OnDestroy()
{
    if (this == _instance)
    {
        Release();
    }
}
```

**逻辑：**
- 仅在当前对象是 `_instance` 时调用 `Release()`
- 避免重复实例销毁时误清理

**设计意图：**
- 确保只有真正的单例实例被销毁时才清理

### 六、IsValid 有效性检查

```csharp
/// <summary>
/// 判断对象是否有效
/// </summary>
public static bool IsValid
{
    get { return _instance != null; }
}
```

**用途：**
- 检查单例是否已创建，避免空引用

### 七、Active 激活方法

```csharp
public static T Active()
{
    return Instance;
}
```

**用途：**
- 显式激活/获取实例，语义更清晰

### 八、Release 释放方法

```csharp
public static void Release()
{
    if (_instance != null)
    {
        SingletonSystem.Release(_instance.gameObject, _instance);
        _instance = null;
    }
}
```

**执行流程：**
1. 通过 `SingletonSystem.Release()` 统一清理（移除生命周期注册、销毁 GameObject）
2. 将 `_instance` 置为 null

**设计意图：**
- 统一清理，避免资源泄漏

### 九、Instance 属性（核心实现）

```csharp
/// <summary>
/// 实例
/// </summary>
public static T Instance
{
    get
    {
        if (_instance == null)
        {
            System.Type thisType = typeof(T);
            string instName = thisType.Name;
            GameObject go = SingletonSystem.GetGameObject(instName);
            if (go == null)
            {
                go = GameObject.Find($"/{instName}");
                if (go == null)
                {
                    go = new GameObject(instName);
                    go.transform.position = Vector3.zero;
                }
            }

            if (go != null)
            {
                _instance = go.GetComponent<T>();
                if (_instance == null)
                {
                    _instance = go.AddComponent<T>();
                }
            }

            if (_instance == null)
            {
                Log.Fatal($"Can't create SingletonBehaviour<{typeof(T)}>");
            }
            
            SingletonSystem.Retain(go, _instance);
        }

        return _instance;
    }
}
```

**查找与创建流程（详细步骤）：**

1. **从 SingletonSystem 获取**（第78行）：
   - 调用 `SingletonSystem.GetGameObject(instName)` 获取已注册的 GameObject
   - 如果之前注册过，直接获取

2. **场景中查找**（第81行）：
   - 如果未找到，使用 `GameObject.Find($"/{instName}")` 在场景中查找
   - 路径 `/` 表示根路径，查找名为类型名的 GameObject

3. **创建新对象**（第84行）：
   - 如果仍未找到，创建新的 GameObject
   - 命名为类型名，位置设置为 `Vector3.zero`

4. **获取或添加组件**（第91-95行）：
   - 在找到/创建的 GameObject 上获取组件 `T`
   - 如果没有，则添加组件

5. **错误处理**（第98-101行）：
   - 如果最终仍无法创建实例，记录致命错误

6. **注册到系统**（第103行）：
   - 调用 `SingletonSystem.Retain(go, _instance)` 注册
   - 这会设置 DontDestroyOnLoad，并注册生命周期接口

**设计意图：**
- **优先复用**：优先使用已注册对象，避免重复创建
- **场景支持**：支持在场景中预置该组件
- **自动创建**：如果都不存在，自动创建
- **统一管理**：注册到 `SingletonSystem` 统一管理

### 十、整体设计模式与协作

**设计模式：**
- **单例模式**：全局唯一实例
- **CRTP（Curiously Recurring Template Pattern）**：通过泛型约束实现类型安全
- **模板方法模式**：`OnLoad()`、`OnDestroy()` 供子类扩展

**与 SingletonSystem 的协作：**
- **注册**：`SingletonSystem.Retain(go, _instance)` 统一管理
- **释放**：`SingletonSystem.Release()` 统一清理
- **生命周期**：自动注册到 Update/FixedUpdate 等（如实现对应接口）

### 十一、使用示例

```csharp
// 示例：音频管理器
public class AudioManager : SingletonBehaviour<AudioManager>
{
    protected override void OnLoad()
    {
        base.OnLoad();
        // 初始化音频系统
        InitializeAudio();
    }
    
    protected override void OnDestroy()
    {
        // 清理音频资源
        CleanupAudio();
        base.OnDestroy();
    }
    
    public void PlaySound(string soundName)
    {
        // 播放音效
    }
}

// 使用方式：
AudioManager.Instance.PlaySound("click");  // 自动创建实例
if (AudioManager.IsValid) { ... }          // 检查有效性
AudioManager.Release();                     // 手动释放
```

### 十二、关键设计亮点

1. **自动去重**：`CheckInstance()` 在 Awake 中自动销毁重复实例
2. **多路径查找**：优先从系统获取，其次场景查找，最后创建
3. **统一管理**：与 `SingletonSystem` 集成，支持 DontDestroyOnLoad 和生命周期
4. **类型安全**：泛型约束确保类型正确
5. **生命周期钩子**：`OnLoad()`、`OnDestroy()` 供子类扩展

### 十三、注意事项

1. **场景中预置**：如果场景中已有该组件，会被自动识别为单例
2. **命名规则**：GameObject 名称使用类型名，避免冲突
3. **线程安全**：未考虑多线程，应在主线程使用
4. **释放时机**：通常由 `SingletonSystem.Release()` 统一释放，也可手动调用

该类提供了基于 MonoBehaviour 的单例实现，与 Unity 生命周期和 `SingletonSystem` 集成，适合需要 Unity 组件功能的全局管理器。

---

## 总结

本次对话详细分析了 TEngine 框架中的 SingletonSystem 架构，包括：

1. **SingletonSystem 整体架构**：核心管理系统、普通单例基类、MonoBehaviour 单例基类
2. **Restart 函数**：`LoadScene(0)` 的含义和使用场景
3. **SingletonBehaviour 类**：完整的实现细节、设计模式和协作关系

所有分析均基于实际代码，保持了代码的原始逻辑和设计意图。
