# Unity UI Components Complete Reference

> 本文档详细记录了 Unity UI 系统中各个组件的参数说明、使用注意事项和实际应用场景。
> 基于 TEngine 项目中的实际配置进行分析。

**Unity 版本**: 2022.3.61f1c1

---

## 目录

1. [StandaloneInputModule 参数说明](#1-standaloneinputmodule-参数说明)
2. [EventSystem 参数说明](#2-eventsystem-参数说明)
3. [Canvas 和 Canvas Scaler 参数说明](#3-canvas-和-canvas-scaler-参数说明)
4. [GraphicRaycaster 参数说明](#4-graphicraycaster-参数说明)
5. [UICamera (Camera 组件) 参数说明](#5-uicamera-camera-组件-参数说明)
6. [Clear Flags 详解](#6-clear-flags-详解)
7. [Transform 和 RectTransform 区别](#7-transform-和-recttransform-区别)

---

## 1. StandaloneInputModule 参数说明

### 1.1 Send Pointer Hover To Parent (发送指针悬停到父对象)

- **含义**: 当启用后，如果子UI元素没有处理悬停事件，会将事件传递给父对象。
- **项目配置**: 已启用（值为1）
- **注意事项**:
  - 适合嵌套UI结构，需要父容器响应悬停
  - 禁用可减少事件传递开销
  - 复杂UI层级可能产生意外事件冒泡

### 1.2 Horizontal Axis (水平轴)

- **含义**: 用于UI导航的水平输入轴名称，对应 Input Manager 中的轴。
- **项目配置**: `"Horizontal"`
- **对应 Input Manager 配置**:
  - 键盘：左/右方向键，或 A/D
  - 手柄：左摇杆水平轴
- **注意事项**:
  - 必须在 Input Manager 中存在同名轴
  - 用于按钮、滑块等UI元素的左右导航
  - 轴不存在会导致导航失效

### 1.3 Vertical Axis (垂直轴)

- **含义**: 用于UI导航的垂直输入轴名称。
- **项目配置**: `"Vertical"`
- **对应 Input Manager 配置**:
  - 键盘：上/下方向键，或 W/S
  - 手柄：左摇杆垂直轴
- **注意事项**:
  - 必须在 Input Manager 中存在同名轴
  - 用于按钮、列表等UI元素的上下导航
  - 与 Horizontal Axis 配合实现四方向导航

### 1.4 Submit Button (提交按钮)

- **含义**: 确认/提交操作的按钮名称。
- **项目配置**: `"Submit"`
- **对应 Input Manager 配置**:
  - 键盘：Return/Enter 或 Space
  - 手柄：Joystick Button 0（通常是 A/X）
- **注意事项**:
  - 必须在 Input Manager 中存在同名按钮
  - 用于触发按钮点击、确认对话框等
  - 与 UI 元素的 `Selectable` 组件配合使用

### 1.5 Cancel Button (取消按钮)

- **含义**: 取消/返回操作的按钮名称。
- **项目配置**: `"Cancel"`
- **对应 Input Manager 配置**:
  - 键盘：Escape
  - 手柄：Joystick Button 1（通常是 B/Circle）
- **注意事项**:
  - 必须在 Input Manager 中存在同名按钮
  - 用于关闭窗口、返回上一级等
  - 通常与 `IPointerClickHandler` 或自定义逻辑配合

### 1.6 Input Actions Per Second (每秒输入操作数)

- **含义**: 按住方向键时，每秒触发的导航操作次数。
- **项目配置**: `10`
- **注意事项**:
  - 值越大，导航越快，但可能过快
  - 值越小，导航越慢，但更易控制
  - 建议范围：5-20
  - 与 `Repeat Delay` 配合控制导航节奏

### 1.7 Repeat Delay (重复延迟)

- **含义**: 按住方向键后，首次重复触发前的延迟时间（秒）。
- **项目配置**: `0.5`
- **注意事项**:
  - 延迟越长，首次重复越慢
  - 延迟越短，响应越快，但可能误触
  - 建议范围：0.3-1.0 秒
  - 与 `Input Actions Per Second` 共同控制导航体验

### 1.8 Force Module Active (强制模块激活)

- **状态**: 在 Unity 2022.3 中已弃用（Deprecated），并从 Inspector 中移除
- **说明**: 虽然 prefab 文件中可能仍保留 `m_ForceModuleActive`（用于向后兼容），但已不再生效
- **注意**: 在 Inspector 中看不到此参数是正常的，因为已被移除

---

## 2. EventSystem 参数说明

### 2.1 First Selected (首次选中对象)

- **含义**: 场景启动时默认选中的 UI 元素（GameObject）。
- **项目配置**: `None`（值为 `{fileID: 0}`）
- **注意事项**:
  - 通常设置为场景中第一个可交互的 UI 元素（如主菜单的第一个按钮）
  - 留空时，启动时不会有默认选中项
  - 必须是一个带有 `Selectable` 组件的 GameObject（如 Button、Toggle、Slider 等）
  - 用于键盘/手柄导航的起始点
  - 适合菜单、对话框等需要明确初始焦点的场景

### 2.2 Send Navigation Events (发送导航事件)

- **含义**: 是否启用键盘/手柄导航事件。
- **项目配置**: 已启用（值为 `1`）
- **注意事项**:
  - 启用后，方向键/手柄摇杆可在 UI 元素间导航
  - 禁用后，键盘/手柄导航失效，但鼠标/触摸仍可用
  - 通常保持启用，除非仅使用鼠标/触摸
  - 与 `StandaloneInputModule` 的 `Horizontal Axis`、`Vertical Axis` 配合使用
  - 项目已启用，支持键盘和手柄导航

### 2.3 Drag Threshold (拖拽阈值)

- **含义**: 判定为拖拽操作所需的最小像素移动距离。
- **项目配置**: `10` 像素
- **注意事项**:
  - 值越小，越容易触发拖拽，但可能误判点击
  - 值越大，需要更大移动才判定为拖拽，但可能影响拖拽体验
  - 建议范围：5-20 像素
  - 高分辨率屏幕可能需要适当增大
  - 触摸设备建议稍大（如 15-20），鼠标可稍小（如 5-10）
  - 项目当前为 10，属于常用值

### 2.4 使用注意事项总结

#### 2.4.1 场景中 EventSystem 的数量

- 每个场景应只有一个 EventSystem
- 多个 EventSystem 可能导致事件处理冲突
- 项目中的 `UIRoot.prefab` 和 `UICanvas.prefab` 都包含 EventSystem，注意避免同时激活

#### 2.4.2 与 StandaloneInputModule 的配合

- EventSystem 负责事件系统管理
- StandaloneInputModule 负责具体输入处理
- 两者需同时存在并启用才能正常工作
- 项目配置中两者都已启用

#### 2.4.3 First Selected 的使用场景

- 菜单系统：设置为第一个菜单按钮
- 对话框：设置为确认或取消按钮
- 表单：设置为第一个输入框
- 项目当前为 None，适合动态设置或不需要默认焦点的场景

#### 2.4.4 代码中动态设置示例

如果需要动态设置 First Selected，可以这样操作：

```csharp
// 获取EventSystem组件
EventSystem eventSystem = EventSystem.current;

// 设置首次选中的对象
eventSystem.firstSelectedGameObject = yourButtonGameObject;

// 或者通过代码选中
eventSystem.SetSelectedGameObject(yourButtonGameObject);
```

#### 2.4.5 项目中的实际使用

从代码中可以看到，项目在 `Debugger.cs` 中有对 EventSystem 的引用：
- 通过 `GameObject.Find("UIRoot/EventSystem")` 查找 EventSystem
- 在调试器显示/隐藏时控制 EventSystem 的激活状态

---

## 3. Canvas 和 Canvas Scaler 参数说明

### 3.1 Canvas 组件参数

#### 3.1.1 Render Mode (渲染模式)

- **含义**: 决定 Canvas 上 UI 元素的渲染方式
- **项目配置**:
  - `UICanvas.prefab`: `Screen Space - Overlay` (值为 0)
  - `UIRoot.prefab`: `Screen Space - Camera` (值为 1)
- **三种模式**:
  - `Screen Space - Overlay` (0): UI 始终渲染在最上层，不受摄像机影响
  - `Screen Space - Camera` (1): UI 由指定摄像机渲染，可与 3D 场景进行深度排序
  - `World Space` (2): UI 作为 3D 场景中的一个平面存在
- **注意事项**:
  - `Screen Space - Overlay`: 最简单，性能好，适合大多数 UI
  - `Screen Space - Camera`: 需要指定 `Render Camera`，可应用后处理，UI 可能被 3D 物体遮挡
  - `World Space`: 用于 3D UI（如血条、世界空间 UI）

#### 3.1.2 Pixel Perfect (像素完美)

- **含义**: 强制 UI 元素对齐到像素网格，减少模糊
- **项目配置**: 未启用（值为 0）
- **注意事项**:
  - 适合像素艺术风格
  - 可能导致位置微调，影响布局
  - 现代 UI 通常不启用，允许平滑缩放

#### 3.1.3 Render Camera (渲染摄像机)

- **含义**: 当 `Render Mode` 为 `Screen Space - Camera` 时，指定用于渲染的摄像机
- **项目配置**:
  - `UIRoot.prefab`: 指定了 `UICamera`
  - `UICanvas.prefab`: 未指定（因为使用 Overlay 模式）
- **注意事项**:
  - 必须指定有效的 Camera
  - 建议使用专用 UI 摄像机，设置 `Clear Flags` 为 `Depth Only`，`Culling Mask` 仅包含 UI 层
  - 设置 `Depth` 大于主摄像机，避免冲突

#### 3.1.4 Plane Distance (平面距离)

- **含义**: Canvas 平面距离 `Render Camera` 的距离
- **项目配置**: `100`
- **注意事项**:
  - 影响 UI 大小和与 3D 物体的深度关系
  - 距离越大，UI 在屏幕上看起来越小
  - 用于调整 UI 与 3D 场景的遮挡关系

#### 3.1.5 Receives Events (接收事件)

- **含义**: Canvas 是否接收 UI 事件（点击、悬停等）
- **项目配置**: 已启用（值为 1）
- **注意事项**:
  - 通常保持启用
  - 禁用后 UI 无法交互

#### 3.1.6 Override Sorting (覆盖排序)

- **含义**: 是否覆盖父 Canvas 的排序设置
- **项目配置**:
  - `UICanvas.prefab`: 未启用（值为 0）
  - `UIHome.prefab`: 已启用（值为 1）
- **注意事项**:
  - 子 Canvas 需要独立排序时启用
  - 项目中的 `UIWindow.cs` 会动态设置：`_canvas.overrideSorting = true`

**Override Sorting 详细说明**:

##### 核心概念

`Override Sorting` 决定子 Canvas 是否继承父 Canvas 的排序设置。

- `Override Sorting = false`（未启用）：子 Canvas 继承父 Canvas 的 `Sorting Layer` 和 `Order in Layer`，无法独立设置。
- `Override Sorting = true`（启用）：子 Canvas 可以独立设置自己的排序，不受父 Canvas 影响。

##### 具体例子说明

**例子1：Override Sorting = false（继承父Canvas）**

假设有以下层级结构：

```
UIRoot (Canvas)
├─ Sorting Layer: "Default"
├─ Order in Layer: 0
└─ Override Sorting: false
   └─ MainMenuWindow (Canvas)
      ├─ Sorting Layer: (继承父Canvas)
      ├─ Order in Layer: (继承父Canvas，实际也是0)
      └─ Override Sorting: false
         └─ SubMenuPanel (Canvas)
            ├─ Sorting Layer: (继承父Canvas)
            └─ Order in Layer: (继承父Canvas，实际也是0)
```

**结果**：
- 所有 Canvas 的排序值都是 0
- 子 Canvas 无法独立设置排序
- 无法实现窗口层级管理

**例子2：Override Sorting = true（独立排序）**

基于项目中的实际代码，每个 UIWindow 都会设置 `overrideSorting = true`：

```csharp
// UIWindow.cs 第422行
_canvas.overrideSorting = true;
_canvas.sortingOrder = 0;
_canvas.sortingLayerName = "Default";
```

层级结构示例：

```
UIRoot (Canvas)
├─ Sorting Layer: "Default"
├─ Order in Layer: 0
└─ Override Sorting: false

├─ MainMenuWindow (Canvas) - UIWindow实例
│  ├─ Sorting Layer: "Default"
│  ├─ Order in Layer: 100  ← 独立设置
│  └─ Override Sorting: true  ← 关键！
│     └─ ButtonPanel (Canvas) - 子Canvas
│        ├─ Sorting Layer: "Default"
│        ├─ Order in Layer: 105  ← 基于父Canvas计算
│        └─ Override Sorting: true
│
└─ BattleWindow (Canvas) - UIWindow实例
   ├─ Sorting Layer: "Default"
   ├─ Order in Layer: 200  ← 独立设置，显示在MainMenu之上
   └─ Override Sorting: true  ← 关键！
      └─ HUDPanel (Canvas) - 子Canvas
         ├─ Sorting Layer: "Default"
         ├─ Order in Layer: 205  ← 基于父Canvas计算
         └─ Override Sorting: true
```

**结果**：
- 每个窗口可以独立设置排序值
- BattleWindow (200) 会显示在 MainMenuWindow (100) 之上
- 子 Canvas 的排序基于父 Canvas 计算（见代码第268行）

##### 项目中的实际应用

**UIWindow 的排序机制**：

从 `UIWindow.cs` 的代码可以看到：

```csharp
// 第422-424行：每个窗口创建时都设置独立排序
_canvas.overrideSorting = true;
_canvas.sortingOrder = 0;
_canvas.sortingLayerName = "Default";

// 第99-129行：Depth属性控制窗口深度
public int Depth
{
    get { return _canvas.sortingOrder; }
    set
    {
        _canvas.sortingOrder = value;  // 设置父Canvas
        
        // 设置子Canvas，基于父Canvas的值递增
        for (int i = 0; i < _childCanvas.Length; i++)
        {
            var canvas = _childCanvas[i];
            if (canvas != _canvas)
            {
                depth += 5;  // 注意递增值
                canvas.sortingOrder = depth;
            }
        }
    }
}
```

**子Canvas的排序计算**：

在 `UIWidget.cs` 的 `RestChildCanvas` 方法中：

```csharp
// 第268行：子Canvas的排序 = 父Canvas排序 + 子Canvas原始值
childCanvas.sortingOrder = parentCanvas.sortingOrder + 
                           childCanvas.sortingOrder % UIModule.WINDOW_DEEP;
```

##### 实际场景举例

**场景：游戏中有多个UI窗口**

```
场景结构：
UIRoot (Canvas, Order=0, Override=false)
│
├─ MainMenuWindow (Canvas, Order=100, Override=true)
│  └─ 主菜单按钮等UI元素
│
├─ SettingsWindow (Canvas, Order=200, Override=true)
│  └─ 设置面板UI元素
│
└─ PopupDialog (Canvas, Order=300, Override=true)
   └─ 弹出对话框UI元素
```

**渲染顺序（从后到前）**：
1. MainMenuWindow (100)
2. SettingsWindow (200) - 显示在MainMenu之上
3. PopupDialog (300) - 显示在最上层

**如果 `Override Sorting = false`**：
- 所有窗口的 Order 都是 0
- 无法控制窗口的显示顺序
- 窗口可能相互遮挡，顺序混乱

**如果 `Override Sorting = true`**：
- 每个窗口可以设置独立的 Order
- 通过设置不同的 Order 值控制显示顺序
- 实现窗口层级管理

##### 关键要点总结

1. **"覆盖父Canvas"的含义**
   - `Override Sorting = false`：子 Canvas 使用父 Canvas 的排序值，无法独立设置
   - 适用于：子 Canvas 不需要独立排序的场景

2. **"独立排序"的含义**
   - `Override Sorting = true`：子 Canvas 可以设置自己的排序值，不受父 Canvas 影响
   - 适用于：需要独立管理窗口层级的场景（如项目中的 UIWindow）

3. **项目中的设计意图**
   - 每个 UIWindow 都设置 `overrideSorting = true`，实现窗口的独立排序管理
   - 通过 `Depth` 属性动态控制窗口的显示顺序
   - 子 Canvas 的排序基于父 Canvas 计算，保持层级关系

4. **实际使用建议**
   - 根 Canvas（UIRoot）：通常 `Override Sorting = false`
   - UI 窗口 Canvas：设置 `Override Sorting = true`，便于独立管理排序
   - 窗口内的子 Canvas：根据是否需要独立排序决定是否启用

#### 3.1.7 Sorting Layer (排序层) 和 Order in Layer (层内排序)

- **含义**: 控制 UI 的渲染顺序
- **项目配置**: `Default` 层，`Order in Layer` 为 `0`
- **注意事项**:
  - 用于管理多层 UI 的显示顺序
  - 数值越大，越后渲染（显示在前面）
  - 项目代码中通过 `sortingOrder` 动态控制窗口深度

#### 3.1.8 Additional Shader Channels (额外的 Shader 通道)

- **含义**: 指定传递给 Shader 的额外数据（UV、法线、切线等）
- **项目配置**: `UICanvas.prefab` 为 `0`（无额外通道）
- **注意事项**:
  - 仅在自定义 Shader 需要时启用
  - 会增加顶点数据，影响性能
  - 按需启用

#### 3.1.9 Vertex Color Always In Gamma Color Space (顶点颜色始终在伽马色彩空间)

- **含义**: 强制顶点颜色在伽马空间处理
- **项目配置**: 未启用（值为 0）
- **注意事项**:
  - 主要用于颜色校正
  - 通常保持默认（不启用）

#### 3.1.10 Update Rect Transform For Standalone (更新 RectTransform 用于 Standalone)

- **含义**: 在 Standalone 平台（PC/Mac/Linux）上，当执行手动 `Camera.Render` 调用时，是否自动更新 Canvas 的 RectTransform 大小以匹配渲染目标。
- **项目配置**: 未启用（值为 0）
- **使用场景**:
  - 手动渲染场景
  - 使用 `Camera.Render()` 手动渲染时
  - 需要 Canvas 自动适应渲染目标大小
- **注意事项**:
  - 大多数情况下不需要启用，除非有特殊的手动渲染需求
  - 项目配置为 `0`（禁用），符合常规用法

### 3.2 Canvas Scaler 组件参数

#### 3.2.1 UI Scale Mode (UI 缩放模式)

- **含义**: 决定 UI 如何根据屏幕大小进行缩放
- **项目配置**:
  - `UICanvas.prefab`: `Constant Pixel Size` (值为 0)
  - `UIRoot.prefab`: `Scale With Screen Size` (值为 1)
- **三种模式**:
  - `Constant Pixel Size` (0): 固定像素大小，不随屏幕变化
  - `Scale With Screen Size` (1): 根据参考分辨率缩放（推荐）
  - `Constant Physical Size` (2): 按物理尺寸缩放
- **注意事项**:
  - `Scale With Screen Size` 最常用，适合多设备适配
  - `Constant Pixel Size` 适合固定分辨率或简单 UI

#### 3.2.2 Reference Resolution (参考分辨率)

- **含义**: 设计 UI 时的理想分辨率基准
- **项目配置**:
  - `UICanvas.prefab`: `800 x 600`
  - `UIRoot.prefab`: `750 x 1334`（常见手机分辨率）
- **注意事项**:
  - 选择接近目标设备的常见分辨率
  - 在此分辨率下进行 UI 设计和布局
  - 过高或过低可能导致某些设备上过度缩放或不清晰

#### 3.2.3 Screen Match Mode (屏幕匹配模式)

- **含义**: 当屏幕宽高比与参考分辨率不匹配时的缩放策略
- **项目配置**: `Match Width Or Height` (值为 0)
- **三种模式**:
  - `Match Width Or Height` (0): 根据宽度或高度匹配（或两者之间）
  - `Expand` (1): 扩展以完全填充屏幕
  - `Shrink` (2): 收缩以确保完全显示
- **注意事项**:
  - `Match Width Or Height` 最灵活，通过 `Match` 滑块控制

#### 3.2.4 Match Width Or Height (匹配宽度或高度)

- **含义**: 在 `Match Width Or Height` 模式下，控制宽度和高度匹配的权重
- **项目配置**: `0`（完全匹配宽度）
- **取值范围**: 0-1
  - `0`: 完全匹配宽度（横向游戏常用）
  - `1`: 完全匹配高度（纵向游戏常用）
  - `0.5`: 宽度和高度平衡
- **注意事项**:
  - 横向 UI 优先保持宽度，高度可能被裁剪或留白
  - 纵向 UI 优先保持高度，宽度可能被裁剪或留白
  - 根据 UI 布局特点选择

#### 3.2.5 Reference Pixels Per Unit (每单位参考像素)

- **含义**: Canvas 上一个 Unity 单位对应的像素数
- **项目配置**: `100`（默认值）
- **注意事项**:
  - 通常保持默认 100
  - 与 Sprite 的 `Pixels Per Unit` 匹配时，大小映射更准确

#### 3.2.6 Scale Factor (缩放因子)

- **含义**: 手动缩放因子（在 `Constant Pixel Size` 模式下使用）
- **项目配置**: `1`
- **注意事项**:
  - 仅在 `Constant Pixel Size` 模式下有效
  - 用于手动调整整体 UI 大小

---

## 4. GraphicRaycaster 参数说明

### 4.1 Ignore Reversed Graphics (忽略反向图形)

- **含义**: 是否忽略法线背向射线检测器的 UI 元素。
- **项目配置**: 已启用（值为 `1`）
- **工作原理**:
  - 启用：只检测法线朝向射线检测器的 UI 元素（正面），忽略背面
  - 禁用：正反面都检测
- **使用场景**:
  - 启用：常见情况，避免点击到背面元素
  - 禁用：需要检测背面时（如翻转的 UI）
- **注意事项**:
  - 大多数情况下保持启用
  - 项目配置为启用，符合常规用法

### 4.2 Blocking Objects (阻挡对象类型)

- **含义**: 指定哪些类型的对象可以阻挡对 UI 元素的射线检测。
- **项目配置**: `None` (值为 `0`)
- **可选值**:
  - `None` (0): 不阻挡（项目配置）
  - `TwoD` (1): 2D 对象（2D 碰撞体）可阻挡
  - `ThreeD` (2): 3D 对象（3D 碰撞体）可阻挡
  - `All` (3): 2D 和 3D 对象都可阻挡
- **使用场景**:
  - `None`: UI 始终可点击，不受 3D/2D 物体影响
  - `TwoD`: 2D 精灵可阻挡 UI 点击
  - `ThreeD`: 3D 物体可阻挡 UI 点击
  - `All`: 2D 和 3D 物体都可阻挡
- **注意事项**:
  - 项目配置为 `None`，UI 不会被场景物体阻挡
  - 需要 UI 与 3D 场景交互时，可设置为 `ThreeD` 或 `All`

### 4.3 Blocking Mask (阻挡遮罩)

- **含义**: 指定哪些 Layer 的对象可以阻挡射线检测（需配合 `Blocking Objects` 使用）。
- **项目配置**:
  - `UICanvas.prefab`: `m_Bits: 55` (二进制 `110111`，对应 Layer 0, 1, 2, 4, 5)
  - `UIRoot.prefab`: `m_Bits: 4294967295` (0xFFFFFFFF，所有 Layer)
- **工作原理**:
  - 与 `Blocking Objects` 配合使用
  - 只有 `Blocking Objects` 不为 `None` 时，此参数才生效
  - 勾选的 Layer 中的对象会阻挡 UI 射线检测
- **使用场景**:
  - 需要精确控制哪些 Layer 阻挡 UI 时使用
  - 例如：只让 "Enemy" Layer 阻挡，不让 "Environment" Layer 阻挡
- **注意事项**:
  - 项目配置为 `None`，此参数当前不生效
  - 如果设置 `Blocking Objects` 为 `ThreeD` 或 `All`，需要正确配置此遮罩

### 4.4 项目中的实际使用

从 `UIWindow.cs` 可以看到：

```csharp
// 第427行：获取 GraphicRaycaster 组件
_raycaster = _panel.GetComponent<GraphicRaycaster>();

// 第429行：获取所有子 GraphicRaycaster
_childRaycaster = _panel.GetComponentsInChildren<GraphicRaycaster>(true);
```

### 4.5 实际应用场景举例

**场景1：纯 UI 游戏（项目当前配置）**

```
配置：
- Ignore Reversed Graphics: true
- Blocking Objects: None
- Blocking Mask: (不生效)

结果：
- UI 元素可以正常点击
- 不会被任何 3D/2D 物体阻挡
- 只检测正面的 UI 元素
```

**场景2：UI 与 3D 场景交互**

```
配置：
- Ignore Reversed Graphics: true
- Blocking Objects: ThreeD
- Blocking Mask: 只勾选 "Enemy" Layer

结果：
- UI 元素可以正常点击
- 但如果 "Enemy" Layer 的 3D 物体在 UI 前面，会阻挡点击
- 其他 Layer 的物体不会阻挡
```

**场景3：需要检测背面 UI**

```
配置：
- Ignore Reversed Graphics: false
- Blocking Objects: None
- Blocking Mask: (不生效)

结果：
- 正反面的 UI 元素都可以点击
- 适用于翻转的 UI 面板
```

---

## 5. UICamera (Camera 组件) 参数说明

### 5.1 Clear Flags (清除标志)

- **含义**: 决定摄像机渲染前如何清除屏幕缓冲区
- **项目配置**: `Depth only` (值为 `3`)
- **选项说明**:
  - `Skybox` (1): 清除为天空盒
  - `Solid Color` (2): 清除为纯色
  - `Depth only` (3): 只清除深度缓冲区（项目配置）
  - `Don't Clear` (4): 不清除
- **注意事项**:
  - `Depth only` 适合 UI 摄像机，只清除深度，保留主摄像机渲染的内容
  - 这样 UI 会叠加在主场景之上，不会遮挡场景

**Clear Flags 详细说明**:

#### Skybox（天空盒）

- 清除颜色缓冲区，填充为天空盒
- 清除深度缓冲区
- 如果摄像机未指定天空盒，使用 Lighting 设置中的天空盒
- 如果没有天空盒，回退到 Background Color
- **使用场景**: 主摄像机（MainCamera）的默认设置

#### Solid Color（纯色）

- 清除颜色缓冲区为指定的纯色（Background Color）
- 清除深度缓冲区
- **使用场景**: 不需要天空盒的场景、2D 游戏、性能敏感场景

#### Depth Only（仅深度）

- 不清除颜色缓冲区（保留上一帧的颜色）
- 只清除深度缓冲区
- 新渲染的内容会叠加在上一帧之上
- **使用场景**: UI 摄像机、多摄像机叠加渲染、武器/道具渲染

**工作原理（同一帧内的渲染步骤）**:

假设当前是第 N 帧，Unity 的渲染流程如下：

```
第 N 帧开始
│
├─ 步骤1：MainCamera 渲染 (Depth = 0，先渲染)
│  ├─ Clear Flags: Skybox
│  ├─ 清除颜色缓冲区 → 填充天空盒
│  ├─ 清除深度缓冲区 → 重置深度信息
│  ├─ 渲染场景物体（地面、建筑、角色等）
│  └─ 结果：颜色缓冲区 = [天空盒 + 场景物体]
│      深度缓冲区 = [场景物体的深度信息]
│
├─ 步骤2：UICamera 渲染 (Depth = 2，后渲染)
│  ├─ Clear Flags: Depth Only
│  ├─ 不清除颜色缓冲区 → 保留 MainCamera 渲染的内容
│  ├─ 清除深度缓冲区 → 重置深度信息（关键！）
│  ├─ 渲染 UI 元素（按钮、文字、面板等）
│  └─ 结果：颜色缓冲区 = [天空盒 + 场景物体 + UI元素]
│      深度缓冲区 = [UI元素的深度信息]
│
└─ 第 N 帧结束，显示最终画面
```

**为什么需要清除深度缓冲区？**

深度缓冲区用于判断哪些像素应该被渲染（深度测试）。如果不清除深度缓冲区：

```
问题场景：
1. MainCamera 渲染了一个远处的物体（深度值 = 500）
2. UICamera 不清除深度缓冲区
3. UI 按钮在近处（深度值 = 10）
4. 结果：由于深度缓冲区中还有 500，UI 可能被错误地认为在远处，导致不显示或被遮挡
```

使用 Depth Only 清除深度缓冲区后：

```
正确流程：
1. MainCamera 渲染场景（深度值 = 0-1000）
2. UICamera 清除深度缓冲区
3. UI 元素重新写入深度值（深度值 = 10-100）
4. 结果：UI 正确显示，不会被场景物体的深度信息干扰
```

#### Don't Clear（不清除）

- 不清除颜色缓冲区
- 不清除深度缓冲区
- 新帧直接绘制在上一帧之上
- **使用场景**: 特殊视觉效果（拖尾、残影、运动模糊）、自定义 Shader 效果

### 5.2 深度缓冲区的实际状态和对游戏的影响

#### 5.2.1 每一帧结束时的状态

```
第 N 帧渲染完成：
颜色缓冲区：[场景 + UI] ✓
深度缓冲区：[只有UI的深度信息] ⚠️

场景物体的深度信息已经被清除了！
```

#### 5.2.2 对下一帧渲染的影响（通常无影响）

**为什么通常无影响？**

```
第 N+1 帧开始：
├─ MainCamera 先渲染 (Depth=0)
│  ├─ Clear Flags: Skybox
│  ├─ 清除颜色缓冲区 → [天空盒]
│  ├─ 清除深度缓冲区 → [重置] ← 关键！
│  └─ 重新渲染场景 → 重新写入场景深度
│
└─ UICamera 后渲染 (Depth=2)
   └─ 清除深度缓冲区 → [重置]
   └─ 渲染UI → 写入UI深度

结论：每一帧都是独立的，上一帧的深度信息会被清除
```

**关键点**：
- MainCamera 使用 Skybox 清除，会清除深度缓冲区
- 每一帧都会重新写入深度信息
- 上一帧的深度信息不会影响下一帧

#### 5.2.3 对后处理效果的影响（可能有影响）

如果 MainCamera 使用了后处理效果（如景深、雾效、边缘检测），这些效果可能需要深度信息：

```
问题示例：景深效果（Depth of Field）

第 N 帧：
├─ MainCamera 渲染场景
│  └─ 深度缓冲区：[场景深度 0-1000]
│
├─ UICamera 渲染UI
│  └─ 清除深度缓冲区 → [UI深度 10-100]
│
└─ 后处理效果尝试读取深度信息
   └─ 只能读取到UI的深度，场景深度丢失！
   └─ 结果：景深效果可能不正确
```

**解决方案**：
1. 后处理在 UICamera 渲染之前执行
   - Unity 的后处理通常在 MainCamera 渲染完成后立即执行
   - 此时深度缓冲区还包含场景信息

2. 使用 RenderTexture 保存深度
   - 将场景深度保存到 RenderTexture
   - 后处理从 RenderTexture 读取深度

3. 分离后处理摄像机
   - 使用独立的摄像机进行后处理
   - 在 UI 渲染之前执行

#### 5.2.4 对透明物体的影响（可能有影响）

透明物体通常不写入深度缓冲区，依赖背景物体的深度信息：

```
问题示例：透明玻璃效果

场景：
├─ 不透明物体（墙壁）→ 写入深度
├─ 透明物体（玻璃）→ 不写入深度
└─ UI元素

渲染流程：
1. MainCamera 渲染墙壁 → 深度缓冲区：[墙壁深度]
2. MainCamera 渲染玻璃 → 玻璃不写入深度，但可以读取墙壁深度
3. UICamera 清除深度 → 深度缓冲区：[重置]
4. UICamera 渲染UI → 深度缓冲区：[UI深度]

如果后续需要渲染其他透明物体：
└─ 无法读取到墙壁的深度信息！
└─ 可能导致渲染顺序错误
```

**实际影响**：
- 大多数情况下影响较小，因为透明物体通常在 MainCamera 渲染阶段完成
- 如果需要在 UI 之后渲染透明物体，可能会有问题

#### 5.2.5 对点击检测的影响

**重要结论**: 深度缓冲区不影响点击检测

**原因**：
1. 点击检测使用射线检测，不是深度缓冲区
2. 射线从摄像机发射，检测碰撞体（Collider）
3. 深度缓冲区用于渲染，不用于点击检测

**EventSystem 的检测流程**：

```
玩家点击屏幕
│
├─ 步骤1：GraphicRaycaster 检测 UI
│  ├─ 从 UICamera 发射射线
│  ├─ 检测点击位置是否有 UI 元素
│  └─ 如果检测到 UI → 事件被消费，停止检测
│
└─ 步骤2：PhysicsRaycaster 检测 3D 物体（如果步骤1没有检测到UI）
   ├─ 从 MainCamera 发射射线
   ├─ 检测点击位置是否有 3D 物体（带 Collider）
   └─ 如果检测到 3D 物体 → 触发 3D 物体的事件
```

**项目配置的影响**：
- GraphicRaycaster.BlockingObjects = None
- 这意味着 UI 不会阻挡 3D 物体的检测
- 如果点击位置有 UI → UI 响应（因为检测顺序）
- 如果点击位置没有 UI → 3D 物体可以响应（如果有 PhysicsRaycaster）

**实际场景分析**：

**场景1：点击位置有 UI**

```
点击位置：屏幕中央（有一个按钮）

检测流程：
1. GraphicRaycaster 检测
   └─ 检测到按钮 → 按钮响应点击
   └─ 事件被消费，不再检测 3D 物体

结果：按钮响应，3D 物体不响应
```

**场景2：点击位置没有 UI**

```
点击位置：屏幕空白区域（没有UI元素）

检测流程：
1. GraphicRaycaster 检测
   └─ 没有检测到 UI 元素
   
2. PhysicsRaycaster 检测（如果MainCamera有PhysicsRaycaster组件）
   └─ 检测到 3D 物体（带Collider）
   └─ 3D 物体响应点击

结果：3D 物体响应，UI 不响应
```

**场景3：点击位置有 UI，但 UI 不响应**

```
点击位置：UI 背景图片（Raycast Target = false）

检测流程：
1. GraphicRaycaster 检测
   └─ 检测到背景图片，但 Raycast Target = false
   └─ 事件没有被消费
   
2. PhysicsRaycaster 检测
   └─ 检测到 3D 物体
   └─ 3D 物体响应点击

结果：3D 物体响应
```

**如何让 3D 物体可以被点击**：

**必要条件**：

1. MainCamera 需要 PhysicsRaycaster 组件
   ```csharp
   // 在 MainCamera 上添加 PhysicsRaycaster
   Camera mainCamera = Camera.main;
   PhysicsRaycaster raycaster = mainCamera.GetComponent<PhysicsRaycaster>();
   if (raycaster == null)
   {
       raycaster = mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
   }
   ```

2. 3D 物体需要有 Collider
   - BoxCollider、SphereCollider 等
   - 或者 MeshCollider

3. 3D 物体需要实现 IPointerClickHandler
   ```csharp
   public class ClickableObject : MonoBehaviour, IPointerClickHandler
   {
       public void OnPointerClick(PointerEventData eventData)
       {
           Debug.Log("3D物体被点击了！");
       }
   }
   ```

### 5.3 Culling Mask (剔除遮罩)

- **含义**: 指定摄像机渲染哪些 Layer
- **项目配置**: `UI` (值为 `32`，对应 Layer 5)
- **注意事项**:
  - 只渲染 UI 层，不渲染其他层
  - 与 MainCamera 分离，避免冲突
  - 确保 UI 元素在 UI 层（Layer 5）

### 5.4 Projection (投影模式)

- **含义**: 摄像机的投影方式
- **项目配置**: `Orthographic` (正交投影)
- **注意事项**:
  - 正交投影适合 UI，无透视变形
  - 透视投影会产生近大远小效果，不适合 UI

### 5.5 Size (正交大小)

- **含义**: 正交投影的视口大小（单位：Unity 单位）
- **项目配置**: `5`
- **注意事项**:
  - 影响 UI 的显示范围
  - 需要与 Canvas Scaler 的参考分辨率配合
  - 值越大，可见范围越大

### 5.6 Clipping Planes (裁剪平面)

- **Near (近裁剪面)**: `10`
- **Far (远裁剪面)**: `1000`
- **注意事项**:
  - Near 通常大于 MainCamera，避免与场景物体冲突
  - Far 足够远即可，确保 UI 在范围内

### 5.7 Depth (深度)

- **含义**: 摄像机的渲染顺序（数值越大越后渲染）
- **项目配置**: `2`
- **注意事项**:
  - 必须大于 MainCamera 的 Depth，确保 UI 渲染在场景之上
  - 项目配置为 `2`，MainCamera 通常为 `0` 或 `-1`

### 5.8 Viewport Rect (视口矩形)

- **含义**: 摄像机渲染到屏幕的区域（归一化坐标 0-1）
- **项目配置**: `X: 0, Y: 0, W: 1, H: 1`（全屏）
- **注意事项**:
  - 可用于分屏或画中画
  - 全屏 UI 保持 `(0, 0, 1, 1)`

### 5.9 Target Texture (目标纹理)

- **含义**: 决定摄像机将渲染结果输出到哪里
- **项目配置**: `None`（值为 `{fileID: 0}`）
- **两种模式**:
  - `None`: 直接输出到屏幕（默认）
  - `RenderTexture`: 输出到指定的 RenderTexture（纹理）

**使用场景**:

#### 场景1：画中画（Picture-in-Picture）

```
应用场景：小地图、监控画面、分屏游戏

实现方式：
1. 创建 RenderTexture
2. 将 MainCamera 的 Target Texture 设置为该 RenderTexture
3. 在 UI 中使用 RawImage 显示该 RenderTexture

效果：
- 场景渲染到纹理
- 纹理显示在 UI 上（如小地图）
```

**代码示例**：
```csharp
// 创建 RenderTexture
RenderTexture rt = new RenderTexture(512, 512, 24);
Camera.main.targetTexture = rt;

// 在 UI 中显示
RawImage rawImage = GetComponent<RawImage>();
rawImage.texture = rt;
```

#### 场景2：后处理效果

```
应用场景：需要先渲染到纹理，再进行后处理

实现方式：
1. 渲染到 RenderTexture
2. 对 RenderTexture 进行后处理
3. 将处理后的结果显示到屏幕

效果：
- 可以对渲染结果进行各种处理
- 如模糊、调色、特效等
```

#### 场景3：安全摄像头/监控系统

```
应用场景：游戏中显示多个摄像机的画面

实现方式：
1. 多个摄像机分别渲染到不同的 RenderTexture
2. 在 UI 中同时显示多个 RenderTexture

效果：
- 可以同时看到多个视角的画面
```

#### 场景4：反射效果

```
应用场景：镜子、水面反射

实现方式：
1. 反射摄像机渲染到 RenderTexture
2. 将 RenderTexture 作为反射贴图使用

效果：
- 实现真实的反射效果
```

#### 场景5：截图/录制

```
应用场景：游戏截图、视频录制

实现方式：
1. 渲染到 RenderTexture
2. 读取 RenderTexture 的像素数据
3. 保存为图片或视频

效果：
- 可以保存渲染结果
```

**代码示例**：
```csharp
public class Screenshot : MonoBehaviour
{
    public Camera screenshotCamera;
    private RenderTexture screenshotRT;

    public void TakeScreenshot()
    {
        // 创建 RenderTexture
        screenshotRT = new RenderTexture(Screen.width, Screen.height, 24);
        
        // 设置摄像机的 Target Texture
        screenshotCamera.targetTexture = screenshotRT;
        
        // 渲染一帧
        screenshotCamera.Render();
        
        // 读取像素数据
        RenderTexture.active = screenshotRT;
        Texture2D tex = new Texture2D(Screen.width, Screen.height);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        
        // 保存为图片
        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes("screenshot.png", bytes);
        
        // 恢复设置
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        
        // 清理
        Destroy(tex);
        screenshotRT.Release();
    }
}
```

**性能影响**：

**内存开销**：
```
Target Texture = None:
- 内存开销：最小（直接渲染到屏幕）

Target Texture = RenderTexture:
- 内存开销：较大
  - RenderTexture 需要占用显存
  - 大小 = 宽度 × 高度 × 字节数（取决于格式）
  - 例如：1920×1080 RGBA32 = 8MB
```

**性能开销**：
```
Target Texture = None:
- 性能开销：最小
- 直接渲染到屏幕，无需额外处理

Target Texture = RenderTexture:
- 性能开销：较大
  - 需要额外的渲染目标切换
  - 需要将纹理从显存传输到内存（如果需要读取）
  - 如果显示在 UI 上，需要额外的绘制调用
```

**注意事项**：

1. **内存管理**
   ```csharp
   // 创建 RenderTexture
   RenderTexture rt = new RenderTexture(512, 512, 24);
   
   // 使用完毕后必须释放
   rt.Release();
   Destroy(rt);
   ```

2. **性能优化**
   - 尽量使用较小的分辨率（如小地图用 512×512）
   - 不需要时及时释放 RenderTexture
   - 避免频繁创建和销毁

3. **多摄像机配合**
   ```
   如果 MainCamera 使用 RenderTexture：
   - 场景不会直接显示在屏幕上
   - 需要额外的步骤才能显示
   - 通常配合其他摄像机使用
   ```

### 5.10 UICamera 与 MainCamera 的区别对比

| 参数 | UICamera | MainCamera | 说明 |
|------|----------|------------|------|
| **Clear Flags** | `Depth only` (3) | `Skybox` (2) | UICamera 只清除深度，保留主场景内容 |
| **Culling Mask** | `UI` (Layer 5) | `Everything` (所有层) | UICamera 只渲染 UI 层 |
| **Depth** | `2` | `0` 或 `-1` | UICamera 深度更高，后渲染 |
| **Projection** | `Orthographic` | `Orthographic` | 两者都使用正交投影 |
| **Size** | `5` | `10` | UICamera 视口更小 |
| **Near** | `10` | `0.3` | UICamera 近裁剪面更远 |
| **Far** | `1000` | `1000` | 相同 |
| **Tag** | `Untagged` | `MainCamera` | MainCamera 有特殊标签 |
| **Layer** | `UI` (5) | `Default` (0) | 不同层 |
| **用途** | 渲染 UI 元素 | 渲染游戏场景 | 职责分离 |

### 5.11 使用注意事项总结

#### 5.11.1 必须配置的参数

- Clear Flags：必须为 `Depth only`
- Culling Mask：只勾选 `UI` 层
- Depth：必须大于 MainCamera 的 Depth
- Projection：使用 `Orthographic`

#### 5.11.2 与 Canvas 的配合

- Canvas 的 `Render Mode` 为 `Screen Space - Camera` 时，需要指定 UICamera
- 项目代码中会查找 UICamera：
```csharp
// UIModule.cs 第55行
_uiCamera = uiRoot.GetComponentInChildren<Camera>();
```

#### 5.11.3 性能优化

- UICamera 只渲染 UI 层，减少不必要的渲染
- 使用 `Depth only` 清除，避免重复渲染场景
- 可禁用 Occlusion Culling（UI 不需要）

#### 5.11.4 常见问题

- UI 不显示：检查 UICamera 是否启用，Culling Mask 是否包含 UI 层
- UI 被场景遮挡：检查 Depth 是否大于 MainCamera
- UI 模糊：检查 Orthographic Size 和 Canvas Scaler 的配置

---

## 6. Clear Flags 详解

### 6.1 缓冲区清除对比表

| Clear Flags | 颜色缓冲区 | 深度缓冲区 | 视觉效果 | 典型用途 |
|-------------|-----------|-----------|---------|---------|
| **Skybox** | 清除为天空盒 | 清除 | 场景+天空盒 | 主摄像机 |
| **Solid Color** | 清除为纯色 | 清除 | 场景+纯色背景 | 2D游戏、简单背景 |
| **Depth Only** | 保留 | 清除 | 叠加渲染 | UI摄像机、叠加效果 |
| **Don't Clear** | 保留 | 保留 | 累积效果 | 特殊视觉效果 |

### 6.2 多摄像机配合示例

#### 示例1：UI 叠加场景（项目中的实际配置）

```
摄像机配置：
1. MainCamera
   - Clear Flags: Skybox (2)
   - Depth: 0
   - Culling Mask: Everything
   → 渲染：场景 + 天空盒

2. UICamera
   - Clear Flags: Depth Only (3)
   - Depth: 2
   - Culling Mask: UI
   → 渲染：UI元素（叠加在场景之上）

最终画面：
[天空盒背景] + [场景物体] + [UI元素]
```

#### 示例2：武器渲染在场景之上

```
摄像机配置：
1. MainCamera
   - Clear Flags: Skybox
   - Depth: 0
   → 渲染：场景、敌人、环境

2. WeaponCamera
   - Clear Flags: Depth Only
   - Depth: 1
   - Culling Mask: Weapon
   → 渲染：玩家武器（显示在场景之上，不被环境遮挡）

最终画面：
[场景] + [武器（始终可见）]
```

#### 示例3：分屏游戏

```
摄像机配置：
1. Player1Camera
   - Clear Flags: Skybox
   - Depth: 0
   - Viewport Rect: (0, 0.5, 1, 0.5)  # 上半屏
   → 渲染：玩家1视角

2. Player2Camera
   - Clear Flags: Skybox
   - Depth: 0
   - Viewport Rect: (0, 0, 1, 0.5)  # 下半屏
   → 渲染：玩家2视角
```

### 6.3 使用注意事项总结

#### 6.3.1 渲染顺序

- Depth 值决定渲染顺序（从小到大）
- Clear Flags 决定如何清除缓冲区
- 两者配合实现正确的叠加效果

#### 6.3.2 性能考虑

- Skybox：需要渲染天空盒，性能开销中等
- Solid Color：性能开销最小
- Depth Only：只清除深度，性能开销较小
- Don't Clear：不清除，但可能产生意外效果

#### 6.3.3 常见错误

- UI 不显示：检查 UICamera 的 Clear Flags 是否为 Depth Only
- UI 遮挡场景：检查 Depth 值是否正确
- 画面闪烁：检查 Clear Flags 设置是否正确

#### 6.3.4 最佳实践

- 主摄像机：使用 Skybox 或 Solid Color
- UI 摄像机：使用 Depth Only
- 特殊效果：根据需求选择，谨慎使用 Don't Clear

---

## 7. Transform 和 RectTransform 区别

### 7.1 基本关系

- **Transform**: 所有 GameObject 的基础变换组件（3D 和 2D）
- **RectTransform**: 继承自 Transform，专门用于 UI 元素（Canvas 下的元素）

### 7.2 继承关系

```
Component
└─ Transform (基础变换组件)
   └─ RectTransform (UI专用变换组件)
```

### 7.3 主要区别对比

| 特性 | Transform | RectTransform |
|------|-----------|---------------|
| **使用场景** | 3D/2D 物体 | UI 元素 |
| **坐标系统** | 世界/本地坐标 | 屏幕坐标（像素） |
| **位置属性** | `position`, `localPosition` | `anchoredPosition` |
| **大小属性** | `localScale` | `sizeDelta`, `rect` |
| **特有功能** | 旋转、缩放 | 锚点、响应式布局 |
| **继承关系** | 基类 | 继承自 Transform |

### 7.4 RectTransform 特有属性

#### 7.4.1 Anchor（锚点）

```csharp
// 锚点定义 UI 元素相对于父对象的位置关系
rectTransform.anchorMin = new Vector2(0, 0);  // 左下角 (0, 0)
rectTransform.anchorMax = new Vector2(1, 1);  // 右上角 (1, 1)

// 常见锚点配置：
// 左上角：(0, 1) 到 (0, 1)
// 右上角：(1, 1) 到 (1, 1)
// 左下角：(0, 0) 到 (0, 0)
// 右下角：(1, 0) 到 (1, 0)
// 中心：(0.5, 0.5) 到 (0.5, 0.5)
// 全屏：(0, 0) 到 (1, 1)
```

#### 7.4.2 AnchoredPosition（锚点位置）

```csharp
// 相对于锚点的位置偏移
rectTransform.anchoredPosition = new Vector2(100, 50);

// 注意：
// - 如果锚点是点（anchorMin == anchorMax），表示相对于锚点的偏移
// - 如果锚点是矩形（anchorMin != anchorMax），表示相对于锚点矩形的偏移
```

#### 7.4.3 SizeDelta（大小增量）

```csharp
// 相对于锚点的大小
rectTransform.sizeDelta = new Vector2(200, 100);

// 注意：
// - 如果锚点是点，sizeDelta 就是实际大小
// - 如果锚点是矩形，sizeDelta 是相对于锚点矩形的增量
```

#### 7.4.4 Pivot（中心点）

```csharp
// 定义 UI 元素的中心点位置（归一化坐标）
rectTransform.pivot = new Vector2(0.5f, 0.5f);  // 中心
rectTransform.pivot = new Vector2(0, 0);        // 左下角
rectTransform.pivot = new Vector2(1, 1);        // 右上角

// 影响：
// - 旋转中心
// - 缩放中心
// - 位置计算
```

### 7.5 实际配置示例（来自项目）

#### 示例1：居中按钮

```yaml
# 来自 TestUI.prefab
m_AnchorMin: {x: 0.5, y: 0.5}      # 锚点在中心
m_AnchorMax: {x: 0.5, y: 0.5}      # 锚点在中心
m_AnchoredPosition: {x: 5.3, y: -96.0}  # 相对于中心的偏移
m_SizeDelta: {x: 329.8, y: 55.8}   # 实际大小
m_Pivot: {x: 0.5, y: 0.5}          # 中心点
```

#### 示例2：顶部对齐

```yaml
# 来自 UIBattleWindow.prefab
m_AnchorMin: {x: 0.5, y: 1}        # 锚点在顶部中心
m_AnchorMax: {x: 0.5, y: 1}        # 锚点在顶部中心
m_AnchoredPosition: {x: 0, y: -49.6}  # 向下偏移
m_SizeDelta: {x: 256, y: 100}      # 实际大小
m_Pivot: {x: 0.5, y: 0.5}          # 中心点
```

#### 示例3：全屏

```yaml
# 来自 UIHome.prefab
m_AnchorMin: {x: 0, y: 0}          # 左下角
m_AnchorMax: {x: 1, y: 1}          # 右上角
m_AnchoredPosition: {x: 0, y: 0}   # 无偏移
m_SizeDelta: {x: 0, y: 0}          # 无增量（填满父对象）
m_Pivot: {x: 0.5, y: 0.5}          # 中心点
```

### 7.6 使用注意事项

#### 7.6.1 类型转换

```csharp
// ✅ 正确：UI 元素可以转换为 RectTransform
RectTransform rectTransform = uiElement.transform as RectTransform;

// ❌ 错误：3D 物体不能转换为 RectTransform
RectTransform rectTransform = cube.transform as RectTransform;  // 返回 null

// ✅ 正确：检查是否为 RectTransform
if (transform is RectTransform)
{
    RectTransform rectTransform = transform as RectTransform;
    // 使用 RectTransform 特有属性
}
```

#### 7.6.2 位置设置方式

**Transform 方式（不适用于 UI）**：
```csharp
// ❌ 不推荐：直接设置 position（UI 元素）
transform.position = new Vector3(100, 50, 0);  // 可能导致位置不正确

// ✅ 推荐：使用 RectTransform
rectTransform.anchoredPosition = new Vector2(100, 50);
```

**RectTransform 方式（适用于 UI）**：
```csharp
// ✅ 正确：设置锚点位置
rectTransform.anchoredPosition = new Vector2(100, 50);

// ✅ 正确：设置大小
rectTransform.sizeDelta = new Vector2(200, 100);

// ✅ 正确：设置锚点
rectTransform.anchorMin = new Vector2(0, 0);
rectTransform.anchorMax = new Vector2(1, 1);
```

#### 7.6.3 坐标系统

```
Transform：
- 使用世界坐标或本地坐标
- 3D 空间 (x, y, z)

RectTransform：
- 使用屏幕坐标（像素）
- 2D 空间 (x, y)
- 相对于 Canvas 或父 RectTransform
```

#### 7.6.4 响应式布局

```csharp
// RectTransform 支持响应式布局
// 通过锚点实现自适应

// 示例：按钮始终在屏幕右上角
rectTransform.anchorMin = new Vector2(1, 1);  // 右上角
rectTransform.anchorMax = new Vector2(1, 1);  // 右上角
rectTransform.anchoredPosition = new Vector2(-50, -50);  // 偏移

// 示例：面板填满父对象
rectTransform.anchorMin = new Vector2(0, 0);  // 左下角
rectTransform.anchorMax = new Vector2(1, 1);  // 右上角
rectTransform.sizeDelta = Vector2.zero;  // 无增量
```

### 7.7 项目中的实际使用

**UIWindow.cs 中的使用**：
```csharp
// 第38行：使用 Transform
public override Transform transform => _panel.transform;

// 第43行：使用 RectTransform（通过类型转换）
public override RectTransform rectTransform => _panel.transform as RectTransform;
```

**UIWidget.cs 中的使用**：
```csharp
// 第243行：类型转换
rectTransform = transform as RectTransform;
Log.Assert(rectTransform != null, $"{go.name} ui base element need to be RectTransform");
```

**关键点**：
1. UI 元素同时有 Transform 和 RectTransform
2. `transform` 是基类引用
3. `rectTransform` 是派生类引用
4. 可以通过 `as RectTransform` 转换

### 7.8 常见错误

#### 错误1：在 UI 上使用 Transform.position

```csharp
// ❌ 错误
RectTransform rectTransform = GetComponent<RectTransform>();
rectTransform.position = new Vector3(100, 50, 0);  // 可能导致位置不正确

// ✅ 正确
rectTransform.anchoredPosition = new Vector2(100, 50);
```

#### 错误2：混淆 sizeDelta 和 rect.size

```csharp
// sizeDelta：相对于锚点的大小
Vector2 sizeDelta = rectTransform.sizeDelta;

// rect.size：实际大小（只读）
Vector2 actualSize = rectTransform.rect.size;

// 注意：两者可能不同，取决于锚点配置
```

#### 错误3：在非 UI 元素上使用 RectTransform

```csharp
// ❌ 错误：3D 物体没有 RectTransform
RectTransform rectTransform = cube.GetComponent<RectTransform>();  // 返回 null

// ✅ 正确：3D 物体使用 Transform
Transform transform = cube.transform;
```

---

## 总结

### 关键要点总结

1. **StandaloneInputModule**: 处理 UI 输入，需要与 Input Manager 配合
2. **EventSystem**: 管理 UI 事件系统，控制导航和拖拽
3. **Canvas**: UI 渲染容器，支持多种渲染模式
4. **Canvas Scaler**: UI 缩放适配，实现多设备兼容
5. **GraphicRaycaster**: UI 射线检测，处理点击交互
6. **UICamera**: 专用 UI 摄像机，使用 Depth Only 清除模式
7. **Clear Flags**: 控制缓冲区清除方式，Depth Only 用于 UI 叠加
8. **Transform vs RectTransform**: UI 元素使用 RectTransform，支持锚点和响应式布局

### 最佳实践

1. UI 元素统一使用 RectTransform
2. UICamera 使用 Depth Only 清除模式
3. Canvas 使用 Scale With Screen Size 实现响应式布局
4. GraphicRaycaster 的 BlockingObjects 根据需求配置
5. EventSystem 保持 Send Navigation Events 启用
6. 使用锚点实现响应式 UI 布局
7. UIWindow 设置 overrideSorting = true 实现独立排序管理

### 项目配置总结

**UIRoot.prefab 配置**：
- Canvas: Render Mode = Screen Space - Camera, Render Camera = UICamera
- Canvas Scaler: UI Scale Mode = Scale With Screen Size, Reference Resolution = 750x1334
- GraphicRaycaster: BlockingObjects = None
- UICamera: Clear Flags = Depth Only, Depth = 2, Culling Mask = UI

**UICanvas.prefab 配置**：
- Canvas: Render Mode = Screen Space - Overlay
- Canvas Scaler: UI Scale Mode = Constant Pixel Size, Reference Resolution = 800x600
- GraphicRaycaster: BlockingObjects = None

---

**文档生成时间**: 2024年
**基于项目**: TEngine
**Unity 版本**: 2022.3.61f1c1
