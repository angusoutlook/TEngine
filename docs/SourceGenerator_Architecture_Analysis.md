# SourceGenerator 架构分析

## 一、项目结构

```
SourceGenerator/
├── EventInterfaceGenerator.cs    # 核心生成器类
├── Definition.cs                  # 常量定义
├── Analyzer/
│   └── AnalyzerHelper.cs         # 语法分析辅助工具
├── Properties/
│   └── AssemblyInfo.cs           # 程序集信息
└── SourceGenerator.csproj        # 项目配置
```

---

## 二、核心架构

### 2.1 主要组件

#### 1. EventInterfaceGenerator（核心生成器）

**职责**：实现 `ISourceGenerator` 接口，负责代码生成的核心逻辑

**关键方法**：
- `Initialize()` - 初始化（当前为空实现）
- `Execute()` - 执行代码生成的主流程

#### 2. Definition（常量定义）

```csharp
public class Definition
{
    public const string FrameworkNameSpace = "TEngine";
    public const string NameSpace = "GameLogic";
    public const string EventInterface = "EventInterface";
    public const string StringToHash = "RuntimeId.ToRuntimeId";
}
```

**作用**：集中管理所有常量，便于维护和修改

#### 3. AnalyzerHelper（辅助工具类）

**职责**：提供语法树分析、符号查找等辅助功能

**主要功能**：
- 语法节点遍历和查找
- 类型符号分析
- 特性检查
- 命名空间提取

---

## 三、设计意图

### 3.1 核心设计目标

1. **自动化事件绑定**
   - 通过 `[EventInterface]` 特性标记接口
   - 自动生成事件系统的绑定代码
   - 减少手动编写样板代码

2. **类型安全的事件系统**
   - 生成强类型的事件常量
   - 生成接口实现类
   - 确保编译时类型检查

3. **零运行时开销**
   - 编译时生成代码
   - 无需反射
   - 性能最优

### 3.2 工作流程

```
1. Unity 编译时触发 Source Generator
   ↓
2. 扫描 GameLogic 程序集的所有语法树
   ↓
3. 查找带有 [EventInterface] 特性的接口
   ↓
4. 为每个接口生成三个文件：
   - {InterfaceName}_Event.g.cs      (事件常量)
   - {InterfaceName}_Gen.g.cs         (接口实现)
   - GameEventHelper.g.cs             (初始化辅助)
   ↓
5. 生成的代码参与编译
```

---

## 四、代码生成逻辑详解

### 4.1 Execute 方法流程

```csharp
public void Execute(GeneratorExecutionContext context)
{
    // 1. 只处理 GameLogic 程序集
    if (context.Compilation.Assembly.Name != "GameLogic")
        return;
    
    // 2. 遍历所有语法树
    foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
    {
        // 3. 查找带有 [EventInterface] 特性的接口
        var interfaces = FindEventInterfaces(syntaxTree);
        
        // 4. 为每个接口生成代码
        foreach (var interfaceNode in interfaces)
        {
            // 生成事件常量类
            GenerateEventClass(...);
            
            // 生成接口实现类
            GenerateImplementationClass(...);
        }
    }
    
    // 5. 生成 GameEventHelper
    GenerateGameEventHelper(...);
}
```

### 4.2 生成的三种文件

#### 文件 1：`{InterfaceName}_Event.g.cs`（事件常量类）

**生成逻辑**：`GenerateEventClass()`

**作用**：为接口的每个方法生成一个事件 ID 常量

**示例**（以 `ILoginUI` 为例）：
```csharp
namespace GameLogic
{
    public partial class ILoginUI_Event
    {
        public static readonly int ShowLoginUI = RuntimeId.ToRuntimeId("ILoginUI_Event.ShowLoginUI");
        public static readonly int CloseLoginUI = RuntimeId.ToRuntimeId("ILoginUI_Event.CloseLoginUI");
    }
}
```

**设计意图**：
- 使用字符串哈希生成事件 ID
- 提供类型安全的事件常量
- 避免硬编码数字

#### 文件 2：`{InterfaceName}_Gen.g.cs`（接口实现类）

**生成逻辑**：`GenerateImplementationClass()`

**作用**：实现接口，将方法调用转发到 EventDispatcher

**示例**：
```csharp
namespace GameLogic
{
    public partial class ILoginUI_Gen : ILoginUI
    {
        private EventDispatcher _dispatcher;
        
        public ILoginUI_Gen(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            GameEvent.EventMgr.RegWrapInterface("GameLogic.ILoginUI", this);
        }
        
        public void ShowLoginUI()
        {
            _dispatcher.Send(ILoginUI_Event.ShowLoginUI);
        }
        
        public void CloseLoginUI()
        {
            _dispatcher.Send(ILoginUI_Event.CloseLoginUI);
        }
    }
}
```

**设计意图**：
- 实现接口契约
- 自动注册到事件管理器
- 将方法调用转换为事件发送

#### 文件 3：`GameEventHelper.g.cs`（初始化辅助类）

**生成逻辑**：`GenerateGameEventHelper()`

**作用**：统一初始化所有生成的事件接口实现类

**示例**：
```csharp
namespace GameLogic
{
    public static class GameEventHelper
    {
        public static void Init()
        {
            var m_ILoginUI_Gen = new ILoginUI_Gen(GameEvent.EventMgr.GetDispatcher());
            // ... 其他接口的初始化
        }
    }
}
```

**设计意图**：
- 提供统一的初始化入口
- 自动创建所有事件接口的实现实例
- 简化使用流程

---

## 五、生成代码的位置

### 5.1 生成位置

**关键代码**：
```csharp
context.AddSource("ILoginUI_Event.g.cs", generatedCode);
context.AddSource("ILoginUI_Gen.g.cs", generatedCode);
context.AddSource("GameEventHelper.g.cs", generatedCode);
```

**实际位置**：
- Unity 编译时生成的代码存储在**临时编译目录**中
- 通常位于：`Library/ScriptAssemblies/` 或类似的临时目录
- **不会出现在源码目录**中

### 5.2 如何查看生成的代码

#### 方法 1：Unity 编译日志
- 查看 Unity Console 的编译输出
- Source Generator 会输出生成的文件信息

#### 方法 2：Visual Studio / Rider
- 在 IDE 中查看生成的类型
- 使用 "Go to Definition" (F12) 查看生成的代码
- IDE 会自动反编译显示

#### 方法 3：反编译程序集
- 编译后，生成的代码已经编译进 DLL
- 使用 ILSpy 等工具查看编译后的程序集

#### 方法 4：调试 Source Generator
- 在 `Execute()` 方法中添加日志
- 使用 `Debug.WriteLine()` 输出生成的内容

---

## 六、关键设计细节

### 6.1 程序集过滤

```csharp
if (context.Compilation.Assembly.Name != "GameLogic")
    return;
```

**设计意图**：
- 只处理 `GameLogic` 程序集
- 避免在其他程序集中生成不必要的代码
- 提高编译性能

### 6.2 接口扫描逻辑

```csharp
var interfaces = from i in root.DescendantNodes()
    .OfType<InterfaceDeclarationSyntax>()
    where i.AttributeLists.Count > 0 
        && i.AttributeLists.Any(a => 
            a.Attributes.Any(attr => 
                attr.Name.ToString() == "EventInterface"))
    select i;
```

**设计意图**：
- 使用 LINQ 查询语法树
- 查找带有 `[EventInterface]` 特性的接口
- 高效且易读

### 6.3 命名空间处理

```csharp
string interfaceFullName = namespaceParts
    .Concat(new[] { interfaceName })
    .Aggregate((a, b) => a + "." + b);
```

**设计意图**：
- 自动提取完整的命名空间
- 用于注册接口到事件管理器
- 支持嵌套命名空间

### 6.4 参数处理

```csharp
private string GenerateParameters(MethodDeclarationSyntax method, SemanticModel semanticModel)
{
    return string.Join(", ", 
        method.ParameterList.Parameters.Select(p => 
            $"{GetTypeName(p.Type, semanticModel)} {p.Identifier}"));
}
```

**设计意图**：
- 使用 SemanticModel 获取准确的类型信息
- 处理泛型、嵌套类型等复杂情况
- 生成正确的参数签名

---

## 七、使用示例

### 7.1 定义事件接口

```csharp
using TEngine;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void ShowLoginUI();
        void CloseLoginUI();
    }
}
```

### 7.2 自动生成的代码

编译后会自动生成三个文件（在内存中，不写入磁盘）：

1. **ILoginUI_Event.g.cs** - 事件常量
2. **ILoginUI_Gen.g.cs** - 接口实现
3. **GameEventHelper.g.cs** - 初始化辅助

### 7.3 使用生成的代码

```csharp
// 在 GameApp.cs 中
public static void Entrance(object[] objects)
{
    GameEventHelper.Init();  // 初始化所有事件接口
    
    // 现在可以通过 GameEvent.Get<ILoginUI>() 获取接口实例
    var loginUI = GameEvent.Get<ILoginUI>();
    loginUI.ShowLoginUI();  // 发送事件
}
```

---

## 八、设计优势

### 8.1 类型安全
- ✅ 编译时检查
- ✅ 强类型事件 ID
- ✅ IDE 智能提示

### 8.2 性能优化
- ✅ 零运行时开销
- ✅ 无需反射
- ✅ 直接方法调用

### 8.3 开发效率
- ✅ 自动生成样板代码
- ✅ 减少手动维护
- ✅ 统一代码风格

### 8.4 可维护性
- ✅ 集中管理事件接口
- ✅ 自动同步接口变更
- ✅ 减少人为错误

---

## 九、扩展点

### 9.1 可能的扩展方向

1. **支持更多特性**
   - 自定义事件 ID 生成规则
   - 支持事件优先级
   - 支持事件过滤

2. **代码生成优化**
   - 支持异步方法
   - 支持返回值
   - 支持泛型接口

3. **错误处理**
   - 接口验证
   - 方法签名检查
   - 编译时错误提示

---

## 十、总结

### 10.1 核心价值

SourceGenerator 实现了一个**编译时代码生成系统**，通过：

1. **自动化**：减少手动编写样板代码
2. **类型安全**：编译时检查，避免运行时错误
3. **性能优化**：零运行时开销，直接方法调用
4. **开发效率**：统一管理，自动同步

### 10.2 生成代码位置

- **编译时**：通过 `context.AddSource()` 添加到编译上下文
- **运行时**：生成的代码已编译进程序集
- **查看方式**：通过 IDE 的 "Go to Definition" 或反编译工具

### 10.3 设计模式

- **Source Generator 模式**：编译时代码生成
- **接口代理模式**：通过实现类转发方法调用
- **注册表模式**：统一管理事件接口实例

这是一个**优雅且高效**的事件系统解决方案！
