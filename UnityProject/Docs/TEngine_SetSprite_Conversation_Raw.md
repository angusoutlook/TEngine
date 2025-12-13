# TEngine SetSprite 架构与实现对话（Raw）

> 日期：2025-01-27  
> 说明：尽量完整记录本次会话中 **用户与助手** 的可见内容（不包含系统与工具调用元数据）。

---

## 1. 会话起点

**User：**  
介绍一下setsprite的架构和设计意图，和使用sample

**Assistant（Auto）：**  
正在搜索代码库中与 setsprite 相关的实现，以了解其架构、设计意图和使用示例。

**Assistant（Auto，分析完成后）：**  
## SetSprite 架构和设计意图

### 一、架构层次

#### 1. 扩展方法层 (`SetSpriteExtensions.cs`)
- 提供 `Image` 和 `SpriteRenderer` 的扩展方法
- 封装调用，简化使用

#### 2. 实现层 (`SetSpriteObject.cs`)
- 实现 `ISetAssetObject` 接口
- 封装设置逻辑，支持 `Image` 和 `SpriteRenderer`
- 使用对象池 (`MemoryPool`) 复用对象

#### 3. 资源管理层 (`ResourceExtComponent`)
- 统一管理资源加载、缓存、释放
- 维护加载状态和引用追踪
- 分帧释放机制，避免卡顿

#### 4. 接口层 (`ISetAssetObject`)
- 定义统一的资源设置接口
- 支持多种资源类型扩展

### 二、设计意图

#### 1. 自动化资源管理
- 自动追踪资源引用
- 自动释放不再使用的资源
- 避免手动管理带来的泄漏风险

#### 2. 防止资源泄漏
- 通过 `IsCanRelease()` 判断资源是否可释放
- 定期检查并释放无引用资源
- 组件销毁时自动清理

#### 3. 异步加载支持
- 基于 UniTask 的异步加载
- 不阻塞主线程
- 支持取消操作 (`CancellationToken`)

#### 4. 资源复用机制
- 相同资源自动复用，避免重复加载
- 使用对象池缓存已加载资源
- 通过 `_assetItemPool` 管理资源缓存

#### 5. 生命周期管理
- 与 GameObject 生命周期绑定
- 自动处理组件销毁场景
- 防止悬空引用

### 三、关键实现细节

#### 1. 资源加载流程 (`SetAssetByResources`)
```csharp
// 1. 取消旧的加载请求
CancelAndCleanupOldRequest(target);

// 2. 检查缓存
if (_assetItemPool.CanSpawn(location)) {
    // 直接使用缓存
} else {
    // 异步加载新资源
    T resource = await _resourceModule.LoadAssetAsync<T>(location, token);
}

// 3. 设置资源并追踪引用
SetAsset(setAssetObject, assetObject);
```

#### 2. 引用追踪机制
- 使用 `LinkedList<LoadAssetObject>` 追踪所有加载的资源
- 每个资源都关联到目标对象 (`TargetObject`)
- 通过 `IsCurrentLocation()` 检查资源是否仍然有效

#### 3. 分帧释放机制 (`ReleaseUnused`)
```csharp
// 每帧最多处理 maxProcessPerFrame 个资源
while (current != null && processedCount < maxProcessPerFrame) {
    if (current.Value.AssetObject.IsCanRelease()) {
        // 自动释放资源
    }
}
```

#### 4. 取消机制
- 使用 `CancellationToken` 支持取消操作
- 自动取消旧的加载请求
- 防止资源加载完成时目标已失效

### 四、使用示例

#### 基础用法：UI Image
```csharp
using TEngine;
using UnityEngine.UI;

public class UIIcon : MonoBehaviour
{
    public Image iconImage;
    
    void Start()
    {
        // 方式1：基础用法（异步加载，自动管理引用）
        iconImage.SetSprite("ItemIcon");
        
        // 方式2：设置原始分辨率
        iconImage.SetSprite("ItemIcon", setNativeSize: true);
        
        // 方式3：带回调
        iconImage.SetSprite("ItemIcon", callback: (image) => {
            Debug.Log($"图片加载完成: {image.sprite.name}");
        });
        
        // 方式4：支持取消操作
        var cts = new CancellationTokenSource();
        iconImage.SetSprite("ItemIcon", cancellationToken: cts.Token);
        // 需要取消时：cts.Cancel();
    }
}
```

#### 2D SpriteRenderer
```csharp
using TEngine;
using UnityEngine;

public class CharacterSprite : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    
    void Start()
    {
        // 设置 SpriteRenderer 的图片
        spriteRenderer.SetSprite("Player");
        
        // 带回调
        spriteRenderer.SetSprite("Player", callback: (renderer) => {
            Debug.Log($"角色图片加载完成: {renderer.sprite.name}");
        });
    }
}
```

#### 子图片（图集）支持
```csharp
using TEngine;
using UnityEngine.UI;

public class UIIcon : MonoBehaviour
{
    public Image iconImage;
    
    void Start()
    {
        // 从图集中加载子图片
        // location: 图集资源地址
        // spriteName: 图集中的子图片名称
        iconImage.SetSubSprite("CommonAtlas", "Icon_Item_001", setNativeSize: true);
        
        // SpriteRenderer 也支持子图片
        // spriteRenderer.SetSubSprite("Atlas", "Player_Idle");
    }
}
```

### 五、最佳实践

```csharp
// ✅ 推荐：使用 SetSprite，自动管理资源
iconImage.SetSprite("UI/Icon/ItemIcon");

// ❌ 不推荐：手动加载和管理资源
var sprite = GameModule.Resource.LoadAsset<Sprite>("UI/Icon/ItemIcon");
iconImage.sprite = sprite;
// 需要手动管理资源释放，容易造成内存泄漏
```

### 六、总结

SetSprite 通过分层架构和自动化机制，实现了：
- 零手动管理的资源生命周期
- 防止资源泄漏
- 异步加载，不阻塞主线程
- 资源复用，提升性能
- 与 GameObject 生命周期绑定

该设计让开发者专注于业务逻辑，而无需关心资源管理的细节。

---

## 2. 架构分层与自动化管理

**User：**  
架构是如何分层架构和自动化管理的？

**Assistant（Auto）：**  
分析 SetSprite 的分层架构和自动化管理机制，查看关键代码实现。

**Assistant（Auto，分析完成后）：**  
## SetSprite 分层架构与自动化管理机制详解

### 一、分层架构设计

#### 第1层：扩展方法层 (`SetSpriteExtensions.cs`)
```csharp
// 职责：提供便捷的扩展方法接口
public static void SetSprite(this Image image, string location, ...)
{
    // 创建 SetSpriteObject 并委托给 ResourceExtComponent
    ResourceExtComponent.Instance.SetAssetByResources<Sprite>(
        SetSpriteObject.Create(image, location, ...), 
        cancellationToken
    ).Forget();
}
```
- **职责**：提供简洁的 API，隐藏内部实现
- **设计意图**：降低使用成本，统一调用入口

#### 第2层：实现层 (`SetSpriteObject.cs`)
```csharp
// 职责：封装资源设置的具体逻辑
public class SetSpriteObject : ISetAssetObject
{
    // 使用对象池复用对象
    public static SetSpriteObject Create(Image image, string location, ...)
    {
        SetSpriteObject item = MemoryPool.Acquire<SetSpriteObject>(); // 从内存池获取
        // 初始化...
        return item;
    }
    
    // 判断资源是否可以释放
    public bool IsCanRelease()
    {
        // 检查 Image/SpriteRenderer 是否还存在
        // 检查 sprite 是否已被替换
    }
}
```
- **职责**：封装设置逻辑，实现 `ISetAssetObject` 接口
- **设计意图**：统一资源设置接口，支持多种资源类型扩展

#### 第3层：资源管理层 (`ResourceExtComponent`)
```csharp
// 职责：统一管理所有资源的加载、缓存、释放
internal partial class ResourceExtComponent : MonoBehaviour
{
    // 资源缓存池（对象池）
    private IObjectPool<AssetItemObject> _assetItemPool;
    
    // 引用追踪链表
    private LinkedList<LoadAssetObject> _loadAssetObjectsLinkedList;
    
    // 正在加载的资源集合（防止重复加载）
    private HashSet<string> _assetLoadingList;
}
```
- **职责**：统一管理资源生命周期
- **设计意图**：集中管理，避免资源泄漏

#### 第4层：对象池层 (`AssetItemObject` + `MemoryPool`)
```csharp
// AssetItemObject：资源对象包装器
public class AssetItemObject : ObjectBase
{
    protected internal override void Release(bool isShutdown)
    {
        // 自动卸载资源
        ResourceExtComponent.ResourceModule?.UnloadAsset(Target);
    }
}
```
- **职责**：资源对象的生命周期管理
- **设计意图**：通过对象池复用，自动释放资源

#### 第5层：资源加载层 (`ResourceModule`)
```csharp
// 职责：实际的资源加载（基于 YooAsset）
_resourceModule.LoadAssetAsync<T>(location, token)
```
- **职责**：底层资源加载
- **设计意图**：与资源系统解耦，可替换底层实现

---

### 二、自动化管理机制

#### 机制1：对象池复用机制

```csharp
// 1. SetSpriteObject 使用内存池复用
SetSpriteObject item = MemoryPool.Acquire<SetSpriteObject>(); // 获取
MemoryPool.Release(current.Value.AssetObject); // 释放

// 2. AssetItemObject 使用对象池缓存资源
_assetItemPool = objectPoolComponent.CreateMultiSpawnObjectPool<AssetItemObject>(
    "SetAssetPool", autoReleaseInterval, 16, 60, 0);

// 3. 资源缓存检查
if (_assetItemPool.CanSpawn(location)) {
    // 直接使用缓存，无需重新加载
    var assetObject = (T)_assetItemPool.Spawn(location).Target;
} else {
    // 加载新资源并注册到对象池
    _assetItemPool.Register(AssetItemObject.Create(location, assetObject), true);
}
```

**自动化点**：
- SetSpriteObject 从内存池获取，减少分配
- 已加载资源缓存到对象池，相同资源复用
- 对象池自动管理过期资源

#### 机制2：引用追踪机制

```csharp
// 1. 所有加载的资源都加入追踪链表
private void SetAsset(ISetAssetObject setAssetObject, Object assetObject)
{
    _loadAssetObjectsLinkedList.AddLast(new LoadAssetObject(setAssetObject, assetObject));
    setAssetObject.SetAsset(assetObject);
}

// 2. LoadAssetObject 封装引用信息
public class LoadAssetObject
{
    public ISetAssetObject AssetObject { get; }  // 设置对象
    public UnityEngine.Object AssetTarget { get; } // 资源对象
}
```

**自动化点**：
- 自动记录所有资源引用
- 通过链表统一管理，便于遍历检查

#### 机制3：自动释放机制

```csharp
// 1. Update 循环定期检查（每 30 秒）
private void Update()
{
    _checkCanReleaseTime += Time.unscaledDeltaTime;
    if (_checkCanReleaseTime >= checkCanReleaseInterval) {
        ReleaseUnused(); // 自动释放
    }
}

// 2. 分帧处理，避免卡顿
public void ReleaseUnused()
{
    int processedCount = 0;
    while (current != null && processedCount < maxProcessPerFrame) {
        if (current.Value.AssetObject.IsCanRelease()) {
            // 自动释放资源
            _assetItemPool.Unspawn(current.Value.AssetTarget);
            MemoryPool.Release(current.Value.AssetObject);
            _loadAssetObjectsLinkedList.Remove(current);
        }
        processedCount++;
    }
}

// 3. IsCanRelease 智能判断
public bool IsCanRelease()
{
    if (_setType == SetType.Image) {
        // 检查 Image 是否被销毁
        // 检查 sprite 是否已被替换
        return _image == null || _image.sprite == null ||
               (_sprite != null && _image.sprite != _sprite);
    }
}
```

**自动化点**：
- 定时自动检查，无需手动调用
- 分帧处理，避免单帧卡顿
- 智能判断资源是否仍在使用

#### 机制4：取消机制

```csharp
// 1. 取消旧的加载请求
private void CancelAndCleanupOldRequest(UnityEngine.Object target)
{
    if (_loadingStates.TryGetValue(target, out var oldState)) {
        oldState.Cts.Cancel(); // 自动取消
        MemoryPool.Release(oldState);
        _loadingStates.Remove(target);
    }
}

// 2. 检查取消状态
public void SetAsset(Object asset)
{
    if (_cancellationToken.IsCancellationRequested)
        return; // 自动忽略已取消的请求
}

// 3. 检查资源是否仍然有效
if (!IsCurrentLocation(target, location)) {
    // 资源已过期，自动卸载
    _resourceModule.UnloadAsset(assetObject);
}
```

**自动化点**：
- 新请求自动取消旧请求
- 加载完成时检查目标是否仍有效
- 过期资源自动卸载

#### 机制5：防重复加载机制

```csharp
// 1. 使用 HashSet 追踪正在加载的资源
private readonly HashSet<string> _assetLoadingList = new HashSet<string>();

// 2. 防止重复加载
if (!_assetLoadingList.Add(location)) {
    // 已经在加载中，等待回调处理
    return;
}

// 3. 等待其他加载完成
private async UniTask TryWaitingLoading(string assetObjectKey)
{
    if (_assetLoadingList.Contains(assetObjectKey)) {
        await UniTask.WaitUntil(() => !_assetLoadingList.Contains(assetObjectKey));
    }
}
```

**自动化点**：
- 相同资源只加载一次
- 后续请求自动等待已进行的加载

---

### 三、完整自动化流程

#### 调用流程：
```
用户调用
  ↓
SetSpriteExtensions.SetSprite()
  ↓
SetSpriteObject.Create() [内存池获取]
  ↓
ResourceExtComponent.SetAssetByResources()
  ↓
1. 取消旧请求（如果有）
2. 检查缓存池
3. 检查是否正在加载
4. 加载资源（ResourceModule）
5. 注册到对象池
6. 设置资源并加入追踪链表
```

#### 释放流程：
```
Update() 每 30 秒触发
  ↓
ReleaseUnused() 分帧处理
  ↓
遍历 _loadAssetObjectsLinkedList
  ↓
IsCanRelease() 检查每个资源
  ↓
释放资源：
  - 从对象池 Unspawn
  - 释放 SetSpriteObject 到内存池
  - 从追踪链表移除
```

---

### 四、设计优势总结

1. **分层清晰**：各层职责明确，易于维护和扩展
2. **自动化**：无需手动管理资源生命周期
3. **性能优化**：对象池复用、资源缓存、分帧处理
4. **安全性**：自动取消、过期检查、防泄漏
5. **易用性**：扩展方法提供简洁 API

该架构通过多层协作和自动化机制，实现了零手动管理的资源生命周期管理。

---

## 3. HashSet 算法成本分析

**User：**  
使用hashset 防止重复加载，这个算法成本是不是有点高？

**Assistant（Auto）：**  
分析 HashSet 防重复加载的实现，评估其算法成本。

**Assistant（Auto，分析完成后）：**  
## HashSet 防重复加载的算法成本分析

### 一、当前实现分析

#### 使用场景：
```csharp
// 1. 添加正在加载的资源（O(1) 平均）
if (!_assetLoadingList.Add(location)) {
    return; // 已经在加载中
}

// 2. 检查是否正在加载（O(1) 平均）
if (_assetLoadingList.Contains(assetObjectKey)) {
    await UniTask.WaitUntil(() => !_assetLoadingList.Contains(assetObjectKey));
}

// 3. 移除已完成的资源（O(1) 平均）
_assetLoadingList.Remove(assetName);
```

### 二、成本分析

#### 1. 时间复杂度
- **HashSet.Add()**: O(1) 平均，O(n) 最坏（哈希冲突）
- **HashSet.Contains()**: O(1) 平均，O(n) 最坏
- **HashSet.Remove()**: O(1) 平均，O(n) 最坏

#### 2. 实际成本
- **字符串哈希计算**：对 `string` 的哈希有一定开销，但通常可忽略
- **内存开销**：每个字符串引用 + 哈希表结构，通常很小
- **等待循环开销**：`WaitUntil` 每帧调用 `Contains`，但异步不阻塞主线程

#### 3. 潜在问题
```csharp
// 问题点：WaitUntil 每帧都会调用 Contains
await UniTask.WaitUntil(() => !_assetLoadingList.Contains(assetObjectKey));
```
- 如果等待时间长，会多次调用 `Contains`
- 但这是异步等待，不会阻塞主线程

### 三、成本对比

| 操作 | 成本 | 频率 | 总成本 |
|------|------|------|--------|
| 资源加载 I/O | 高（毫秒级） | 低 | **主要成本** |
| 字符串哈希计算 | 低（微秒级） | 中 | 可忽略 |
| HashSet 查找 | 极低（纳秒级） | 高 | 可忽略 |

**结论**：相比资源加载的 I/O 成本，HashSet 操作成本可忽略。

### 四、可能的优化方案

#### 方案1：使用 Dictionary 记录引用计数（更复杂）
```csharp
// 优点：可以追踪有多少个请求在等待
// 缺点：代码更复杂，需要管理引用计数
private readonly Dictionary<string, int> _assetLoadingCount = new Dictionary<string, int>();

if (!_assetLoadingCount.ContainsKey(location)) {
    _assetLoadingCount[location] = 1;
    // 开始加载
} else {
    _assetLoadingCount[location]++; // 增加等待计数
    // 等待加载完成
}
```

#### 方案2：使用 ConcurrentDictionary（线程安全，但这里不需要）
```csharp
// 优点：线程安全
// 缺点：性能略低于 HashSet，且这里不需要线程安全
private readonly ConcurrentDictionary<string, bool> _assetLoadingList = new();
```

#### 方案3：优化 WaitUntil 的检查频率（推荐）
```csharp
// 当前实现：每帧检查
await UniTask.WaitUntil(() => !_assetLoadingList.Contains(assetObjectKey));

// 优化：可以添加延迟检查
await UniTask.WaitUntil(() => !_assetLoadingList.Contains(assetObjectKey), 
    cancellationToken: cancellationToken, 
    timing: PlayerLoopTiming.Update); // 明确指定更新时机
```

#### 方案4：使用 TaskCompletionSource 替代轮询（最佳优化）
```csharp
// 使用 TaskCompletionSource 替代轮询检查
private readonly Dictionary<string, TaskCompletionSource<bool>> _loadingTasks = new();

// 开始加载时
if (_loadingTasks.TryGetValue(location, out var existingTask)) {
    await existingTask.Task; // 等待已有任务完成
    return;
}

var tcs = new TaskCompletionSource<bool>();
_loadingTasks[location] = tcs;

try {
    var resource = await _resourceModule.LoadAssetAsync<T>(location, token);
    // ... 处理资源
    tcs.SetResult(true);
} finally {
    _loadingTasks.Remove(location);
}
```

### 五、性能测试建议

如果担心性能，可以添加性能监控：

```csharp
#if UNITY_EDITOR
private int _hashSetOperationCount = 0;
private System.Diagnostics.Stopwatch _hashSetWatch = new System.Diagnostics.Stopwatch();

// 在关键操作处添加监控
_hashSetWatch.Start();
if (!_assetLoadingList.Add(location)) {
    _hashSetWatch.Stop();
    _hashSetOperationCount++;
    if (_hashSetOperationCount % 100 == 0) {
        Log.Info($"HashSet operations: {_hashSetOperationCount}, Total time: {_hashSetWatch.ElapsedMilliseconds}ms");
    }
    return;
}
_hashSetWatch.Stop();
#endif
```

### 六、结论与建议

#### 当前实现评估：
1. **成本合理**：HashSet 操作成本远低于资源加载成本
2. **实现简单**：代码清晰，易于维护
3. **性能足够**：对于大多数游戏场景，性能足够

#### 是否需要优化：
- **不需要优化的情况**：
  - 资源加载频率不高（每秒 < 100 次）
  - 资源路径长度适中（< 100 字符）
  - 没有性能瓶颈
  
- **需要优化的情况**：
  - 高频加载（每秒 > 1000 次）
  - 路径很长（> 200 字符）
  - 性能分析显示 HashSet 是瓶颈（通常不会）

#### 推荐做法：
1. **保持当前实现**：对于大多数场景，当前实现已足够
2. **如果确实需要优化**：使用 TaskCompletionSource 方案，避免轮询
3. **添加性能监控**：在开发阶段监控，确认是否有问题

**总结**：当前使用 HashSet 的成本很低，相比资源加载的 I/O 成本可忽略。除非有明确的性能瓶颈，否则无需优化。

---

## 4. SetSprite 回调实现

**User：**  
    public delegate void LoadAssetSuccessCallback(string assetName, object asset, float duration, object userData);
这个的关于setsprite 的回调实现，请从项目中找出来，或者举个例子

**Assistant（Auto）：**  
查找 SetSprite 的回调实现，特别是 `LoadAssetSuccessCallback` 的使用。

**Assistant（Auto，分析完成后）：**  
## SetSprite 回调实现机制详解

### 一、回调架构

SetSprite 的回调有两层：

#### 1. 用户层回调（Action<Image>/Action<SpriteRenderer>）
- 在 `SetSpriteObject.SetAsset()` 中调用
- 资源设置完成后立即触发
- 提供简洁的 API

#### 2. 底层回调（LoadAssetSuccessCallback）
- 在 `ResourceExtComponent.OnLoadAssetSuccess()` 中调用
- 资源加载完成后触发
- 用于内部资源管理

### 二、完整回调流程

```
用户调用 SetSprite(callback)
  ↓
SetSpriteObject.Create() 保存 callback
  ↓
ResourceExtComponent.SetAssetByResources()
  ↓
资源加载完成 → OnLoadAssetSuccess()
  ↓
SetAsset(setAssetObject, assetObject)
  ↓
SetSpriteObject.SetAsset() 
  ↓
_imageCallback?.Invoke(_image)  ← 用户回调在这里触发
```

### 三、代码实现

#### 1. 用户层回调定义（SetSpriteObject.cs）
```csharp
public class SetSpriteObject : ISetAssetObject
{
    // 保存用户传入的回调
    Action<Image> _imageCallback;
    Action<SpriteRenderer> _spriteCallback;
    
    public void SetAsset(Object asset)
    {
        _sprite = (Sprite)asset;
        
        if (_image != null)
        {
            _image.sprite = _sprite;
            if (_setNativeSize)
            {
                _image.SetNativeSize();
            }
            // 用户回调在这里触发
            _imageCallback?.Invoke(_image);
        }
        else if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = _sprite;
            // 用户回调在这里触发
            _spriteCallback?.Invoke(_spriteRenderer);
        }
    }
}
```

#### 2. 底层回调实现（ResourceExtComponent.Resource.cs）
```csharp
// 初始化时注册底层回调
private void InitializedResources()
{
    _resourceModule = ModuleSystem.GetModule<IResourceModule>();
    _loadAssetCallbacks = new LoadAssetCallbacks(OnLoadAssetSuccess, OnLoadAssetFailure);
}

// 底层资源加载成功回调
private void OnLoadAssetSuccess(string assetName, object asset, float duration, object userdata)
{
    _assetLoadingList.Remove(assetName);
    ISetAssetObject setAssetObject = (ISetAssetObject)userdata; // userdata 就是 SetSpriteObject
    
    UnityEngine.Object assetObject = asset as UnityEngine.Object;
    
    if (assetObject != null)
    {
        // 检查资源是否仍然是当前需要的
        if (IsCurrentLocation(setAssetObject.TargetObject, setAssetObject.Location))
        {
            ClearLoadingState(setAssetObject.TargetObject);
            
            // 注册到对象池
            _assetItemPool.Register(AssetItemObject.Create(setAssetObject.Location, assetObject), true);
            
            // 设置资源，这里会触发用户回调
            SetAsset(setAssetObject, assetObject);
        }
        else
        {
            // 资源已经过期，卸载
            _resourceModule.UnloadAsset(assetObject);
        }
    }
}

// 在异步加载完成后手动触发回调
T resource = await _resourceModule.LoadAssetAsync<T>(location, linkedTokenSource.Token);
if (resource != null)
{
    // 手动触发底层回调（因为使用了 await，不会走 ResourceModule 的回调）
    _loadAssetCallbacks?.LoadAssetSuccessCallback.Invoke(location, resource, 0f, setAssetObject);
}
```

#### 3. LoadAssetSuccessCallback 定义
```csharp
/// <summary>
/// 加载资源成功回调函数。
/// </summary>
/// <param name="assetName">要加载的资源名称。</param>
/// <param name="asset">已加载的资源。</param>
/// <param name="duration">加载持续时间。</param>
/// <param name="userData">用户自定义数据（这里是 SetSpriteObject）。</param>
public delegate void LoadAssetSuccessCallback(string assetName, object asset, float duration, object userData);
```

### 四、使用示例

#### 示例1：基础回调使用
```csharp
using TEngine;
using UnityEngine.UI;

public class UIIcon : MonoBehaviour
{
    public Image iconImage;
    
    void Start()
    {
        // 方式1：使用 Lambda 表达式
        iconImage.SetSprite("ItemIcon", callback: (image) => {
            Debug.Log($"图片加载完成: {image.sprite.name}");
            Debug.Log($"图片尺寸: {image.sprite.rect.width} x {image.sprite.rect.height}");
        });
        
        // 方式2：使用方法引用
        iconImage.SetSprite("ItemIcon", callback: OnSpriteLoaded);
        
        // 方式3：带原始尺寸设置
        iconImage.SetSprite("ItemIcon", setNativeSize: true, callback: (image) => {
            Debug.Log("图片已设置并调整为原始尺寸");
        });
    }
    
    private void OnSpriteLoaded(Image image)
    {
        Debug.Log($"回调方法：图片 {image.sprite.name} 加载完成");
    }
}
```

#### 示例2：SpriteRenderer 回调
```csharp
using TEngine;
using UnityEngine;

public class CharacterSprite : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer.SetSprite("Player", callback: (renderer) => {
            Debug.Log($"角色图片加载完成: {renderer.sprite.name}");
            
            // 可以在这里做一些额外的处理
            if (renderer.sprite != null)
            {
                // 调整碰撞体大小
                var collider = GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    collider.size = renderer.sprite.bounds.size;
                }
            }
        });
    }
}
```

#### 示例3：复杂业务逻辑
```csharp
using TEngine;
using UnityEngine.UI;
using System.Collections;

public class ItemSlot : MonoBehaviour
{
    public Image iconImage;
    public Image loadingImage;
    
    void Start()
    {
        // 显示加载动画
        StartCoroutine(ShowLoadingAnimation());
        
        // 加载图标
        iconImage.SetSprite($"UI/Icon/Item/{itemId}", callback: (image) => {
            // 隐藏加载动画
            if (loadingImage != null)
            {
                loadingImage.gameObject.SetActive(false);
            }
            
            // 播放加载完成特效
            PlayLoadEffect();
            
            // 更新UI状态
            UpdateItemState();
        });
    }
    
    private void PlayLoadEffect()
    {
        // 播放特效逻辑
    }
    
    private void UpdateItemState()
    {
        // 更新状态逻辑
    }
    
    private IEnumerator ShowLoadingAnimation()
    {
        // 加载动画逻辑
        yield return null;
    }
}
```

#### 示例4：批量加载带回调
```csharp
using TEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IconManager : MonoBehaviour
{
    public List<Image> iconImages;
    public List<string> iconLocations;
    
    private int _loadedCount = 0;
    
    void Start()
    {
        LoadAllIcons();
    }
    
    private void LoadAllIcons()
    {
        for (int i = 0; i < iconImages.Count && i < iconLocations.Count; i++)
        {
            int index = i; // 闭包变量
            iconImages[i].SetSprite(iconLocations[i], callback: (image) => {
                _loadedCount++;
                Debug.Log($"图标 {index} 加载完成，总进度: {_loadedCount}/{iconImages.Count}");
                
                if (_loadedCount >= iconImages.Count)
                {
                    OnAllIconsLoaded();
                }
            });
        }
    }
    
    private void OnAllIconsLoaded()
    {
        Debug.Log("所有图标加载完成！");
    }
}
```

### 五、回调时机说明

1. **回调触发时机**：
   - 资源加载完成后
   - Sprite 已设置到 Image/SpriteRenderer
   - 如果设置了 `setNativeSize`，已调整尺寸

2. **回调参数**：
   - `Image` 回调：传入已设置好 Sprite 的 Image 组件
   - `SpriteRenderer` 回调：传入已设置好 Sprite 的 SpriteRenderer 组件

3. **注意事项**：
   - 回调在主线程执行
   - 如果资源加载失败，回调不会触发
   - 如果请求被取消，回调不会触发
   - 回调中的 Image/SpriteRenderer 可能已被销毁，需要检查

### 六、底层 LoadAssetSuccessCallback 的使用

如果需要监听底层资源加载事件，可以扩展 `ResourceExtComponent`：

```csharp
// 注意：这是内部实现，不建议直接使用
// 但可以了解其工作原理

// ResourceExtComponent 内部使用
_loadAssetCallbacks = new LoadAssetCallbacks(
    OnLoadAssetSuccess,  // LoadAssetSuccessCallback 类型
    OnLoadAssetFailure
);

// OnLoadAssetSuccess 的签名匹配 LoadAssetSuccessCallback
private void OnLoadAssetSuccess(string assetName, object asset, float duration, object userdata)
{
    // userdata 就是 SetSpriteObject
    // asset 就是加载的 Sprite 资源
    // 这里会调用 SetAsset，进而触发用户回调
}
```

### 七、总结

- **用户回调**：`Action<Image>` 或 `Action<SpriteRenderer>`，在资源设置完成后触发
- **底层回调**：`LoadAssetSuccessCallback`，用于内部资源管理
- **回调流程**：资源加载 → 底层回调 → SetAsset → 用户回调
- **使用建议**：在回调中处理加载完成后的业务逻辑，注意检查对象是否有效

该设计将底层资源管理与用户业务逻辑分离，使用简单且灵活。

---

## 5. 保存会话

**User：**  
把这个会话的内容 一字不差的 取个合适的名字，放在合适的目录

**Assistant（Auto）：**  
检查现有文档的命名规范，然后保存当前会话内容。

**Assistant（Auto，保存完成）：**  
已将当前会话内容保存到 `UnityProject/Docs/TEngine_SetSprite_Conversation_Raw.md`。
