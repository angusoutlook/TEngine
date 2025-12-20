# 反编译 SourceGenerator.dll 指南

## 方法 1：使用 ILSpy（推荐，免费开源）

### 下载和安装
1. 访问：https://github.com/icsharpcode/ILSpy/releases
2. 下载最新版本的 ILSpy（Windows 版本）
3. 解压即可使用，无需安装

### 使用步骤
1. 打开 ILSpy
2. 菜单：File → Open → 选择 `SourceGenerator.dll`
   - 路径：`UnityProject/Assets/TEngine/Runtime/Core/GameEvent/SourceGenerator.dll`
3. 在左侧树形结构中浏览代码
4. 导出源代码：
   - 右键点击 DLL 或类 → "Save Code..."
   - 选择保存位置
   - 会生成完整的 C# 源代码文件

### 优点
- 完全免费开源
- 支持导出为 C#、IL、XML 等格式
- 界面友好，易于使用
- 支持搜索功能

---

## 方法 2：使用 dnSpy（功能最强大）

### 下载和安装
1. 访问：https://github.com/dnSpy/dnSpy/releases
2. 下载 dnSpy-win64.zip
3. 解压即可使用

### 使用步骤
1. 打开 dnSpy.exe
2. 菜单：File → Open → 选择 `SourceGenerator.dll`
3. 浏览和查看代码
4. 导出源代码：
   - 右键 → "Export to Project..."
   - 选择保存位置
   - 会生成完整的 Visual Studio 项目

### 优点
- 功能最强大
- 支持编辑和重新编译
- 支持调试
- 可以修改代码并保存

---

## 方法 3：使用 dotPeek（JetBrains 官方工具）

### 下载和安装
1. 访问：https://www.jetbrains.com/decompiler/
2. 下载并安装 dotPeek

### 使用步骤
1. 打开 dotPeek
2. 菜单：File → Open → 选择 `SourceGenerator.dll`
3. 浏览代码
4. 导出源代码：
   - 右键 → "Export to Project"
   - 选择保存位置

### 优点
- JetBrains 官方工具
- 界面美观
- 支持多种导出格式

---

## 方法 4：使用 Visual Studio（内置功能）

### 使用步骤
1. 在 Visual Studio 中打开项目
2. 在代码中引用 `SourceGenerator.dll`
3. 使用 F12（Go to Definition）查看反编译代码
4. 或者：
   - 右键点击类型 → "Go to Definition"
   - Visual Studio 会自动反编译并显示代码

### 导出源代码
1. 在反编译的代码窗口中
2. 点击 "Copy" 按钮复制代码
3. 或者使用 "Export to Project" 功能

### 优点
- 无需安装额外工具
- 集成在开发环境中
- 支持调试

---

## 方法 5：使用 ildasm（命令行工具）

### 使用步骤
1. 打开 Developer Command Prompt for VS
2. 运行命令：
   ```bash
   ildasm SourceGenerator.dll /out:SourceGenerator.il
   ```
3. 会生成 IL 代码文件
4. 可以使用 ILSpy 等工具将 IL 转换为 C# 代码

### 优点
- .NET SDK 自带工具
- 命令行操作
- 适合自动化脚本

---

## 方法 6：查看 TEngine GitHub 仓库

### 尝试查找源代码
1. 访问 TEngine 的 GitHub 仓库：
   - https://github.com/ALEXTANGXIAO/TEngine
2. 搜索 "SourceGenerator" 或 "EventGenerator"
3. 查看是否有源代码文件

### 如果找不到源代码
- 可能源代码未公开
- 可能在其他分支或私有仓库
- 需要使用反编译工具

---

## 推荐工作流程

### 快速查看代码
1. 使用 **ILSpy** 快速打开和浏览
2. 使用搜索功能查找关键类和方法

### 需要编辑或调试
1. 使用 **dnSpy** 打开 DLL
2. 可以修改代码并重新编译

### 需要导出完整项目
1. 使用 **dotPeek** 或 **dnSpy** 导出为项目
2. 在 Visual Studio 中打开进行进一步分析

---

## 反编译后的代码结构预期

根据生成的代码，SourceGenerator 可能包含：

```csharp
// 预期的代码结构
namespace TEngine.SourceGenerator
{
    [Generator]
    public class EventInterfaceSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // 扫描所有程序集
            // 查找 [EventInterface] 特性
            // 生成 GameEventHelper 类
            // 生成 *_Gen 类
        }
        
        public void Initialize(GeneratorInitializationContext context)
        {
            // 初始化
        }
    }
}
```

---

## 注意事项

1. **法律问题**：反编译代码仅供学习和研究使用
2. **代码质量**：反编译的代码可能缺少注释和变量名
3. **依赖关系**：可能需要同时反编译依赖的 DLL
4. **版本差异**：反编译的代码可能与原始源代码有差异

---

## 快速开始（最简单的方法）

1. **下载 ILSpy**：https://github.com/icsharpcode/ILSpy/releases
2. **打开 DLL**：File → Open → 选择 `SourceGenerator.dll`
3. **查看代码**：在左侧树形结构中浏览
4. **导出代码**：右键 → Save Code → 保存为 .cs 文件

完成！你现在可以看到 SourceGenerator 的源代码了。
