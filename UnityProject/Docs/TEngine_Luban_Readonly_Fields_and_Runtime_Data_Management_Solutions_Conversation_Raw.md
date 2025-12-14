# TEngine Luban 配置表 Readonly 字段与运行时数据管理方案讨论

> 本文档记录了关于 Luban 配置表生成 readonly 字段的原因、枚举数据填写方法，以及运行时需要修改配置数据时的最佳实践方案讨论。

---

## 第一部分：Luban 为什么生成的都是 readonly 字段？

### 问题
luban 为什么生成的都是 readonly 字段？什么情况下可以不是 readonly？

### 分析结果

#### 为什么 Luban 生成的都是 readonly 字段？

Luban 默认将所有配置类字段生成为 `readonly`，这是有深层设计原因的：

1. **配置数据不可变性**：配置表数据本质上是静态的模板数据，在运行时不应该被修改，这样可以保证数据的一致性和可预测性。

2. **线程安全**：`readonly` 字段在构造后不可变，天然具有线程安全的特性，在多线程环境下不需要额外的同步机制。

3. **设计理念**：配置是静态数据，应该通过重新加载配置来更新，而不是在运行时修改。

从生成的代码可以看到：

```csharp
public sealed partial class Item : Luban.BeanBase
{
    /// <summary>
    /// 这是id
    /// </summary>
    public readonly int Id;
    /// <summary>
    /// 名字
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// 描述
    /// </summary>
    public readonly string Desc;
    /// <summary>
    /// 价格
    /// </summary>
    public readonly int Price;
    // ... 其他字段都是 readonly
}
```

#### 什么情况下可以不是 readonly？

如果需要非 readonly 字段，可以通过自定义模板来实现：

**方法：自定义 bean 模板**

1. 在 `CustomTemplate/CustomTemplate_Client_LazyLoad/cs-bin/` 目录下创建 `bean.sbn` 模板文件
2. 在模板中移除 `readonly` 关键字

当前项目只自定义了 `tables.sbn`，bean 类使用的是 Luban 的默认模板（默认生成 readonly）。

**注意事项：**

- **不推荐**：配置数据应该保持不可变，运行时修改可能导致数据不一致
- 如果确实需要可修改字段，建议：
  1. 使用包装类或扩展类来存储运行时状态
  2. 使用 `partial class` 添加额外的非 readonly 属性
  3. 创建独立的运行时数据类，而不是修改配置类本身

---

## 第二部分：枚举数据如何填写？填写后会生成什么数据？

### 问题
这个枚举数据如何如何填写？填写后会生成什么数据？

### 分析结果

基于项目中的枚举定义和 Luban 配置，以下是枚举数据的填写方法：

### 1. 普通枚举（如 `EQuality`）

对于普通枚举，直接填写枚举成员名称：

**示例：** 如果字段类型是 `EQuality`，可以填写：
- `WHITE` - 最差品质
- `BLUE` - 蓝色的  
- `PURPLE` - 紫色的
- `RED` - 最高品质

**Excel 填写方式：**
```
##var    id    quality
##type   int   EQuality
##       id
1001     WHITE
1002     BLUE
1003     PURPLE
1004     RED
```

### 2. 位标记枚举（Flags，如 `AccessFlag`）

对于带有 `[Flags]` 属性的枚举，有三种填写方式：

#### 方式一：使用枚举成员名称（用 `|` 分隔）

```
##var    id    access
##type   int   AccessFlag
##       id
1001     READ
1002     WRITE
1003     READ|WRITE
1004     WRITE|TRUNCATE|NEW
```

#### 方式二：使用子列标记（推荐用于多选）

在 `##var` 行中，将枚举成员作为子列列出：

```
##var    id    access    READ    WRITE    TRUNCATE    NEW
##type   int   AccessFlag
##       id
1001     1              1
1002     2                     1
1003     3              1       1
1004     4                     1               1           1
```

- 在对应枚举成员的子列下填写 `1` 表示启用该标志
- 第 1003 行表示 `READ|WRITE`（值为 3 = 1+2）
- 第 1004 行表示 `WRITE|TRUNCATE|NEW`（值为 14 = 2+4+8）

#### 方式三：直接填写数值

```
##var    id    access
##type   int   AccessFlag
##       id
1001     1     (表示 READ)
1002     2     (表示 WRITE)
1003     3     (表示 READ|WRITE = 1+2)
1004     6     (表示 WRITE|TRUNCATE = 2+4)
1005     7     (表示 READ|WRITE|TRUNCATE = 1+2+4)
```

### 填写后会生成什么数据？

#### 1. 枚举定义（自动生成）

根据 `__enums__.xlsx` 中的定义，会生成如下枚举类：

```csharp
// 普通枚举
public enum EQuality
{
    WHITE = 1,
    BLUE = 2,
    PURPLE = 3,
    RED = 4,
}

// 位标记枚举
[System.Flags]
public enum AccessFlag
{
    WRITE = 1,
    READ = 2,
    TRUNCATE = 4,
    NEW = 8,
    READ_WRITE = WRITE|READ,  // 组合值
}
```

#### 2. 配置数据类（自动生成）

如果配置表中有使用枚举的字段，会生成如下代码：

```csharp
public sealed partial class SomeConfig : Luban.BeanBase
{
    public SomeConfig(ByteBuf _buf) 
    {
        Id = _buf.ReadInt();
        Quality = (EQuality)_buf.ReadInt();  // 枚举类型
        Access = (AccessFlag)_buf.ReadInt();  // Flags枚举类型
    }

    public readonly int Id;
    public readonly EQuality Quality;        // 普通枚举字段
    public readonly AccessFlag Access;       // Flags枚举字段
}
```

#### 3. 实际数据值

根据 Excel 中的填写，生成的数据实例：

- `Quality = EQuality.WHITE` (值为 1)
- `Quality = EQuality.BLUE` (值为 2)
- `Access = AccessFlag.READ` (值为 2)
- `Access = AccessFlag.READ | AccessFlag.WRITE` (值为 3)
- `Access = (AccessFlag)6` (值为 6，表示 `WRITE | TRUNCATE`)

#### 4. 使用示例

```csharp
// 获取配置
var config = ConfigSystem.Instance.Tables.TbSomeConfig.Get(1001);

// 使用普通枚举
if (config.Quality == EQuality.RED)
{
    // 最高品质处理
}

// 使用 Flags 枚举
if ((config.Access & AccessFlag.READ) != 0)
{
    // 有读取权限
}

if (config.Access.HasFlag(AccessFlag.READ | AccessFlag.WRITE))
{
    // 同时有读写权限
}
```

### 注意事项

1. 枚举成员名称必须与 `__enums__.xlsx` 中定义的完全一致（区分大小写）
2. Flags 枚举的位值通常是 2 的幂次方（1, 2, 4, 8, 16...）
3. 使用 `|` 组合多个标志时，最终值是各个标志值的按位或结果
4. 直接填写数值时，确保数值是有效的组合值

---

## 第三部分：运行时需要修改配置数据的最佳方案

### 问题
如果游戏的配置的类的字段都是 readonly，但是在运行的时候，会改变内容。并且需要保存。结合 luban 的配置表功能，有哪些最佳的方案？我们只是讨论方案，不要动代码！

### 分析结果

以下是几种常见的最佳实践方案，适用于运行时需要修改并保存配置数据的场景：

## 方案对比

### 方案一：配置数据 + 运行时数据分离（推荐）⭐⭐⭐⭐⭐

**核心思路：** 配置表只存储静态模板数据，运行时数据单独管理。

#### 实现方式：
```
配置层（Luban生成，readonly）：
- ItemConfig (Id, Name, Price, ...)  // 静态配置

运行时层（手动编写，可修改）：
- ItemRuntimeData (ItemId, Count, Level, ...)  // 运行时状态
- PlayerData (保存所有运行时数据)
```

#### 优点：
- ✅ **职责清晰**：配置与运行时数据完全分离
- ✅ **类型安全**：配置保持 readonly，运行时数据可修改
- ✅ **易于维护**：配置更新不影响运行时数据
- ✅ **易于扩展**：运行时数据可以独立扩展
- ✅ **性能优秀**：配置只读，运行时数据可以优化

#### 缺点：
- ❌ 需要维护两套数据结构
- ❌ 需要建立配置ID到运行时数据的映射关系

#### 适用场景：
- 道具系统（配置：道具模板；运行时：玩家拥有的道具及数量）
- 角色系统（配置：角色模板；运行时：玩家角色等级、经验）
- 技能系统（配置：技能模板；运行时：技能等级、冷却时间）

---

### 方案二：配置数据 + 运行时包装类

**核心思路：** 用包装类封装配置数据，添加运行时字段。

#### 实现方式：
```csharp
// Luban生成的配置（readonly）
public class ItemConfig { ... }

// 运行时包装类
public class ItemInstance
{
    public ItemConfig Config { get; }  // 只读配置引用
    public int Count { get; set; }     // 可修改的运行时数据
    public int Level { get; set; }
    // ... 其他运行时字段
}
```

#### 优点：
- ✅ 配置与运行时数据关联清晰
- ✅ 通过 Config 访问静态配置，通过实例访问运行时数据
- ✅ 代码使用直观

#### 缺点：
- ❌ 需要为每个配置类型创建包装类
- ❌ 内存占用略高（每个实例都持有配置引用）

#### 适用场景：
- 道具实例系统
- 装备系统（配置：装备模板；运行时：装备实例的属性）

---

### 方案三：配置数据 + 运行时数据字典

**核心思路：** 用字典存储运行时数据，以配置ID为键。

#### 实现方式：
```csharp
// 配置层
public class ItemConfig { public int Id; ... }

// 运行时数据管理
public class ItemRuntimeManager
{
    private Dictionary<int, ItemRuntimeData> _runtimeData = new();
    
    public ItemRuntimeData GetOrCreate(int itemId)
    {
        if (!_runtimeData.TryGetValue(itemId, out var data))
        {
            var config = ConfigSystem.Instance.Tables.TbItem.Get(itemId);
            data = new ItemRuntimeData { ItemId = itemId };
            _runtimeData[itemId] = data;
        }
        return data;
    }
}

public class ItemRuntimeData
{
    public int ItemId;
    public int Count;
    public int Level;
    // ... 可修改字段
}
```

#### 优点：
- ✅ 按需创建，节省内存
- ✅ 结构简单，易于管理
- ✅ 支持懒加载

#### 缺点：
- ❌ 需要手动维护字典
- ❌ 类型安全性略弱（需要确保ID匹配）

#### 适用场景：
- 大量配置项的运行时数据管理
- 需要按需加载的场景

---

### 方案四：配置数据 + 扩展数据表

**核心思路：** 运行时数据也使用 Luban 管理，但作为独立的数据表。

#### 实现方式：
```
配置表（静态）：
- __tables__.xlsx -> ItemConfig

运行时数据表（可修改，需要持久化）：
- PlayerItemData.xlsx -> PlayerItem (ItemId, Count, Level, ...)
- 通过代码修改 Excel 或使用 JSON/数据库存储
```

#### 优点：
- ✅ 统一使用 Luban 管理
- ✅ 数据结构规范
- ✅ 支持数据校验

#### 缺点：
- ❌ 运行时修改 Excel 不现实，通常需要 JSON/数据库
- ❌ 需要额外的序列化/反序列化逻辑
- ❌ 灵活性较低

#### 适用场景：
- 需要版本控制和数据校验的运行时数据
- 服务器端数据管理

---

### 方案五：配置数据 + 持久化服务层

**核心思路：** 配置数据只读，运行时数据通过专门的持久化服务管理。

#### 实现方式：
```csharp
// 配置层（Luban，readonly）
public class ItemConfig { ... }

// 持久化服务
public class SaveDataService
{
    // 保存运行时数据
    public void SavePlayerData(PlayerData data) { ... }
    
    // 加载运行时数据
    public PlayerData LoadPlayerData() { ... }
}

// 运行时数据
public class PlayerData
{
    public Dictionary<int, ItemData> Items;  // ItemId -> ItemData
    // ... 其他运行时数据
}
```

#### 优点：
- ✅ 职责清晰：配置加载 vs 数据持久化
- ✅ 易于扩展：可支持多种存储方式（PlayerPrefs、文件、数据库）
- ✅ 支持数据迁移和版本管理

#### 缺点：
- ❌ 需要设计持久化格式
- ❌ 需要处理数据兼容性

#### 适用场景：
- 需要复杂持久化逻辑的项目
- 多平台数据存储需求

---

## 推荐方案组合

**建议采用方案一 + 方案五的组合：**

1. **配置数据（Luban，readonly）**：静态配置模板
2. **运行时数据（手动编写）**：可修改的游戏状态
3. **持久化服务**：统一管理运行时数据的保存和加载

### 典型架构示例：

```
┌─────────────────────────────────────┐
│  配置层（Luban，readonly）          │
│  - ItemConfig                       │
│  - CharacterConfig                  │
│  - SkillConfig                      │
└─────────────────────────────────────┘
              ↓ 引用
┌─────────────────────────────────────┐
│  运行时数据层（可修改）             │
│  - ItemInstance (Config + Count)   │
│  - CharacterInstance (Config + Exp) │
│  - PlayerData (聚合所有运行时数据)  │
└─────────────────────────────────────┘
              ↓ 保存/加载
┌─────────────────────────────────────┐
│  持久化服务层                       │
│  - SaveDataService                  │
│  - 支持 PlayerPrefs/文件/数据库     │
└─────────────────────────────────────┘
```

## 方案选择建议

- **简单项目**：方案一（配置+运行时分离）
- **中等项目**：方案一 + 方案二（配置+包装类）
- **复杂项目**：方案一 + 方案五（配置+运行时+持久化服务）
- **服务器项目**：方案四（配置+扩展数据表）

## 注意事项

1. **不要修改 Luban 生成的配置类**：保持 readonly，避免破坏设计
2. **明确数据边界**：哪些是配置，哪些是运行时数据
3. **设计持久化格式**：考虑版本兼容性和迁移
4. **性能考虑**：大量运行时数据时考虑按需加载和缓存
5. **数据一致性**：确保配置ID与运行时数据的关联正确

这种设计既能保持配置数据的不可变性，又能满足运行时修改和持久化的需求。

---

## 项目相关文件参考

### Luban 配置文件
- `Configs/GameConfig/luban.conf` - Luban 配置文件
- `Configs/GameConfig/CustomTemplate/` - 自定义模板目录
- `Configs/GameConfig/CustomTemplate/CustomTemplate_Client_LazyLoad/cs-bin/tables.sbn` - Tables 模板

### 生成的配置类示例
- `UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/item/Item.cs` - 道具配置类（readonly 字段）
- `UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/item/EQuality.cs` - 品质枚举
- `UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/test/AccessFlag.cs` - Flags 枚举

### 配置系统
- `UnityProject/Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs` - 配置加载器

---

**文档生成时间：** 2024年
**讨论主题：** Luban 配置表 readonly 字段设计、枚举数据填写、运行时数据管理方案
