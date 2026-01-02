# TEngine_Fantasy 架构讨论

## 项目概述

TEngine_Fantasy 是一个基于 TEngine 框架的 Unity 游戏项目，采用 AOT + 热更新混合架构。

### 技术栈
- **Unity版本**: 2022.3.61f1c1
- **热更新方案**: HybridCLR
- **资源管理**: YooAsset
- **配置表**: Luban
- **服务器框架**: Fantasy (基于ETServer)
- **异步方案**: UniTask

## 架构层次

### 1. AOT层（框架核心层）
- **位置**: `Assets/TEngine/Runtime`
- **职责**: 框架核心模块实现
- **特点**: 不可热更新，提供基础框架能力

### 2. 热更新层
- **位置**: `Assets/GameScripts/HotFix`
- **程序集划分**:
  - `GameBase`: 游戏基础框架程序集
  - `GameProto`: 游戏配置协议程序集
  - `BattleCore`: 游戏核心战斗程序集
  - `GameLogic`: 游戏业务逻辑程序集

### 3. 服务器层
- **位置**: `DotNet/`
- **框架**: Fantasy
- **特点**: C# 双端解决方案

## 核心模块

### 框架模块系统
- 资源模块 (ResourceModule)
- 事件模块 (EventModule)
- 内存池模块 (MemoryPoolModule)
- 对象池模块 (ObjectPoolModule)
- UI模块 (UIModule)
- 配置表模块 (ConfigModule)
- 流程模块 (ProcedureModule)
- 网络模块 (NetworkModule)

## 文档索引

### 详细文档

- **[架构设计与说明](./Architecture-Design.md)**: 完整的架构设计文档
  - 架构层次设计
  - 前后端共享逻辑实现
  - 框架模块系统
  - Unity编辑器菜单项完整说明
  - 使用注意事项

## 讨论记录

---

*本文档用于记录架构讨论内容，按时间顺序记录讨论要点。*

