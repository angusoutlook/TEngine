# TEngine AssetsReference 资源引用自动化管理对话（Raw）

> 日期：2025-01-27  
> 说明：尽量完整记录本次会话中 **用户与助手** 的可见内容（不包含系统与工具调用元数据）。

---

## 1. 会话起点

**User：**  
AssetsReference - 资源引用自动化管理
是如何做到的？

**Assistant（Auto）：**  
我是 Auto，一个由 Cursor 设计的智能路由代理。正在查找 AssetsReference 资源引用自动化管理的实现。

**Assistant（Auto，分析完成后）：**  
## AssetsReference 资源引用自动化管理的实现原理

基于实际代码，实现机制如下：

### 1. 核心组件结构

`AssetsReference` 是一个 `MonoBehaviour` 组件，通过 Unity 的生命周期自动管理资源引用：

```csharp
[Serializable]
public struct AssetsRefInfo
{
    public int instanceId;      // 资源实例ID，用于唯一标识资源
    public Object refAsset;     // 资源对象引用
}

[DisallowMultipleComponent]
public sealed class AssetsReference : MonoBehaviour
{
    [SerializeField]
    private GameObject sourceGameObject;  // 保存源预制体引用

    [SerializeField]
    private List<AssetsRefInfo> refAssetInfoList;  // 保存其他资源引用（Material、Texture等）

    private static IResourceModule _resourceModule;  // 静态资源模块引用（延迟初始化）

    private static Dictionary<GameObject, AssetsReference> _originalRefs = new();  // 追踪原始实例
}
```

- `sourceGameObject`：保存源预制体引用（用于 GameObject 实例）
- `refAssetInfoList`：保存其他资源引用（Material、Texture 等）
- `instanceId`：在 `AssetsRefInfo` 中用于唯一标识资源实例，通过 `GetInstanceID()` 获取
- `_originalRefs`：静态字典，用于追踪原始 GameObject 实例，区分克隆对象

### 2. 自动释放机制

在 `OnDestroy` 生命周期中自动释放所有资源：

```csharp
private void OnDestroy()
{
    CheckInit();  // 延迟初始化 ResourceModule
    if (sourceGameObject != null)
    {
        CheckRelease();  // 释放源预制体
    }
    ReleaseRefAssetInfoList();  // 释放所有其他资源引用
}

private void CheckInit()
{
    if (_resourceModule != null)
    {
        return;
    }
    else
    {
        _resourceModule = ModuleSystem.GetModule<IResourceModule>();
    }

    if (_resourceModule == null)
    {
        throw new GameFrameworkException($"resourceModule is null.");
    }
}

private void CheckRelease()
{
    if (sourceGameObject != null)
    {
        _resourceModule.UnloadAsset(sourceGameObject);
    }
    else
    {
        Log.Warning($"sourceGameObject is not invalid.");
    }
}

private void ReleaseRefAssetInfoList()
{
    if (refAssetInfoList != null)
    {
        foreach (var refInfo in refAssetInfoList)
        {
            _resourceModule.UnloadAsset(refInfo.refAsset);
        }
        refAssetInfoList.Clear();
    }
}
```

- **延迟初始化**：`CheckInit()` 在首次使用时才获取 `ResourceModule`，避免循环依赖
- **释放源预制体**：通过 `CheckRelease()` 调用 `UnloadAsset` 释放 `sourceGameObject`
- **释放其他资源**：遍历 `refAssetInfoList` 并释放所有引用的资源，最后清空列表

### 3. 自动绑定机制

`ResourceModule` 在加载 GameObject 时自动添加 `AssetsReference` 组件：

```csharp
// ResourceModule.cs 中的 LoadGameObject 方法
return AssetsReference.Instantiate(assetObject.Target as GameObject, parent, this).gameObject;
```

`Instantiate` 方法的实现：

```csharp
internal static AssetsReference Instantiate(GameObject source, Transform parent = null, IResourceModule resourceModule = null)
{
    if (source == null)
    {
        throw new GameFrameworkException($"Source gameObject is null.");
    }

    if (source.scene.name != null)
    {
        throw new GameFrameworkException($"Source gameObject is in scene.");
    }

    GameObject instance = Object.Instantiate(source, parent);
    return instance.AddComponent<AssetsReference>().Ref(source, resourceModule);
}
```

流程：
1. 实例化 GameObject
2. 自动添加 `AssetsReference` 组件
3. 调用 `Ref` 方法绑定源预制体引用

### 4. 克隆处理机制

处理 GameObject 克隆场景，避免克隆时错误释放资源：

```csharp
private void Awake()
{
    // If it is a clone, clear the reference records before cloning
    if (!IsOriginalInstance())
    {
        ClearCloneReferences();
    }
}

private bool IsOriginalInstance()
{
    return _originalRefs.TryGetValue(gameObject, out var originalComponent) &&
           originalComponent == this;
}

private void ClearCloneReferences()
{
    sourceGameObject = null;
    refAssetInfoList?.Clear();
}
```

- 使用静态字典 `_originalRefs` 追踪原始实例
- 克隆时在 `Awake` 中清空引用，避免克隆对象释放原始资源

### 5. 手动绑定其他资源

支持手动绑定 Material、Texture 等资源，提供两种静态方法：

```csharp
// 实例方法：绑定到当前组件所在的 GameObject
public AssetsReference Ref<T>(T source, IResourceModule resourceModule = null) where T : Object
{
    if (source == null)
    {
        throw new GameFrameworkException($"Source gameObject is null.");
    }

    _resourceModule = resourceModule;
    if (refAssetInfoList == null)
    {
        refAssetInfoList = new List<AssetsRefInfo>();
    }

    refAssetInfoList.Add(new AssetsRefInfo(source));
    return this;
}

// 静态方法1：绑定 GameObject 源到指定实例
public static AssetsReference Ref(GameObject source, GameObject instance, IResourceModule resourceModule = null)
{
    if (source == null)
    {
        throw new GameFrameworkException($"Source gameObject is null.");
    }

    if (source.scene.name != null)
    {
        throw new GameFrameworkException($"Source gameObject is in scene.");
    }

    var comp = instance.GetComponent<AssetsReference>();
    return comp ? comp.Ref(source, resourceModule) : instance.AddComponent<AssetsReference>().Ref(source, resourceModule);
}

// 静态方法2：绑定其他资源类型到指定实例
public static AssetsReference Ref<T>(T source, GameObject instance, IResourceModule resourceModule = null) where T : Object
{
    if (source == null)
    {
        throw new GameFrameworkException($"Source gameObject is null.");
    }

    var comp = instance.GetComponent<AssetsReference>();
    return comp ? comp.Ref(source, resourceModule) : instance.AddComponent<AssetsReference>().Ref(source, resourceModule);
}
```

**使用场景示例**：

1. **在 `AssetsSetHelper` 中绑定 Material**：
```csharp
Material material = _resourceManager.LoadAsset<Material>(location, packageName);
image.material = material;
AssetsReference.Ref(material, image.gameObject);  // 自动添加组件并绑定
```

2. **手动绑定预制体引用**：
```csharp
GameObject instance = Instantiate(prefab);
AssetsReference.Ref(prefab, instance, GameModule.Resource);  // 绑定预制体引用
```

3. **绑定多个资源到同一个 GameObject**：
```csharp
Material mat = GameModule.Resource.LoadAsset<Material>("Materials/MyMaterial");
Texture tex = GameModule.Resource.LoadAsset<Texture>("Textures/MyTexture");

var comp = go.GetComponent<AssetsReference>();
comp.Ref(mat, GameModule.Resource);
comp.Ref(tex, GameModule.Resource);  // 可以多次调用绑定多个资源
```

### 6. 关键实现细节

#### 6.1 资源模块延迟初始化

```csharp
private void CheckInit()
{
    if (_resourceModule != null)
    {
        return;
    }
    else
    {
        _resourceModule = ModuleSystem.GetModule<IResourceModule>();
    }
}
```

- 使用静态字段 `_resourceModule` 避免重复获取
- 在 `OnDestroy` 时才初始化，避免组件初始化时的循环依赖问题

#### 6.2 原始实例追踪机制

```csharp
private static Dictionary<GameObject, AssetsReference> _originalRefs = new();

// 在 Ref 方法中注册原始实例
if (!_originalRefs.ContainsKey(gameObject))
{
    _originalRefs.Add(gameObject, this);
}

// 在 Awake 中判断是否为克隆
private bool IsOriginalInstance()
{
    return _originalRefs.TryGetValue(gameObject, out var originalComponent) &&
           originalComponent == this;
}
```

- 使用静态字典追踪通过 `AssetsReference.Instantiate` 创建的原始实例
- 克隆对象在 `Awake` 时检测到不是原始实例，自动清空引用
- 防止克隆对象错误释放原始资源

#### 6.3 场景对象检查

```csharp
if (source.scene.name != null)
{
    throw new GameFrameworkException($"Source gameObject is in scene.");
}
```

- 确保只能绑定资源包中的预制体，不能绑定场景中的对象
- 场景对象由 Unity 管理生命周期，不需要通过 `AssetsReference` 管理

### 7. 设计要点

1. **利用 Unity 生命周期**：通过 `OnDestroy` 自动释放，无需手动调用
2. **引用追踪**：使用序列化字段保存引用，支持场景保存和序列化
3. **克隆安全**：区分原始实例与克隆，避免误释放
4. **多资源支持**：可同时绑定多个资源到一个 GameObject
5. **零手动管理**：开发者无需关心资源释放
6. **延迟初始化**：避免组件初始化时的循环依赖
7. **类型安全**：使用泛型方法支持不同类型的资源绑定

### 8. 与其他模块的集成

#### 8.1 ResourceModule 集成

`ResourceModule` 的 `LoadGameObject` 和 `LoadGameObjectAsync` 方法自动使用 `AssetsReference.Instantiate`：

```csharp
// 同步加载
public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "")
{
    // ... 从对象池获取或加载资源
    return AssetsReference.Instantiate(assetObject.Target as GameObject, parent, this).gameObject;
}

// 异步加载
public async UniTask<GameObject> LoadGameObjectAsync(...)
{
    // ... 异步加载逻辑
    GameObject gameObject = AssetsReference.Instantiate(handle.AssetObject as GameObject, parent, this).gameObject;
    return gameObject;
}
```

#### 8.2 AssetsSetHelper 集成

`AssetsSetHelper` 提供了 Material 设置的扩展方法，自动绑定资源引用：

```csharp
public static void SetMaterial(this Image image, string location, bool isAsync = false, string packageName = "")
{
    Material material = _resourceManager.LoadAsset<Material>(location, packageName);
    image.material = material;
    AssetsReference.Ref(material, image.gameObject);  // 自动绑定引用
}
```

### 总结

`AssetsReference` 通过以下方式实现自动化管理：
- **自动添加**：`ResourceModule` 加载 GameObject 时自动添加组件
- **自动追踪**：组件保存所有资源引用（源预制体 + 其他资源）
- **自动释放**：`OnDestroy` 时自动释放所有引用资源
- **克隆安全**：正确处理 GameObject 克隆场景，避免误释放
- **延迟初始化**：避免初始化时的循环依赖问题
- **类型安全**：支持泛型方法，可绑定任意类型的资源

开发者只需调用 `LoadGameObject` 或 `LoadGameObjectAsync`，框架会自动处理资源生命周期，无需手动 `Dispose`。对于动态加载的资源（如 Material、Texture），使用 `AssetsReference.Ref` 方法绑定即可。

---

## 2. 保存对话

**User：**  
把 AssetsReference相关的对话去个合适的名字，保存到合适的目录

**Assistant（Auto）：**  
已将 AssetsReference 相关对话保存到 `UnityProject/Docs/TEngine_AssetsReference_Conversation_Raw.md`。

---

## 相关文件位置

- **核心实现**：`UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Reference/AssetsReference.cs`
- **资源模块**：`UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/ResourceModule.cs`
- **扩展辅助**：`UnityProject/Assets/TEngine/Runtime/Module/ResourceModule/Reference/AssetsSetHelper.cs`
- **文档说明**：`Books/3-1-资源模块.md`
