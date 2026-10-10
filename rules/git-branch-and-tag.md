---
description: Git 分支管理与版本发布规则——develop/master 双分支模型、semver 版本号与递增规则、标准发布流程、hotfix 流程、发布前后检查清单，以及 annotated tag / ^{commit} 解引用 / 禁止 force push 等高频踩坑点。涉及「发版、打 tag、创建分支、合并 master、hotfix、升级版本号」等任务时应加载本规则。
alwaysApply: false
enabled: true
provider:
---

# 分支管理与版本发布规则

> 适用范围：采用 develop / master 双分支模型的 Git 仓库。模板中的 `vX.Y.Z` 一律替换为本次实际版本号。

## 一、分支模型

| 分支 | 作用 | 说明 |
| --- | --- | --- |
| `develop` | 集成分支（日常开发） | 所有功能 / 修复提交先合入此分支 |
| `master` | 发布分支 | 只在发布时从 `develop` 合并，直接对外 |

**版本发布 = 把 `develop` 合并进 `master` + 在 `master` 上打 tag。**

惯例上不走独立的 release 临时分支；如确需，命名 `release/vX.Y.Z`，最终仍以「合入 master + 打 tag」作为发布完成标志。

## 二、版本号规则

采用语义化版本：三段式 `v主.次.补`（如 `v1.4.2`）。

- **Tag 命名**：小写 `v` + semver 三段式，必须与仓库既有 tag 格式一致。
- **递增规则**：
  - 纯修复 → 补位 +1（`v1.4.2` → `v1.4.3`）
  - 含新功能 / 较大改动 → 次位 +1（`v1.4.2` → `v1.5.0`）
  - 不兼容变更 → 主位 +1（`v1.4.2` → `v2.0.0`）
- **Tag 说明格式**：`vX.Y.Z：<核心改动摘要，多项用 + 连接>`

## 三、两条铁律：annotated tag 与 hash 口径

### 3.1 必须使用 annotated tag

```bash
git tag -a vX.Y.Z -m "vX.Y.Z：<核心改动摘要>"
```

用 `-a` 创建带附注的 tag，**不用轻量 tag**——轻量 tag 缺少作者、时间与说明，回溯发布历史时拿不到任何元信息。

### 3.2 annotated tag 的 hash 有两类，取用时务必区分

annotated tag 本身是一个**独立的 tag 对象**，它指向真正的提交对象。因此同一个 tag 在不同命令下会返回两个不同的 hash：

| 命令 | 返回的是 | 用途 |
| --- | --- | --- |
| `git rev-parse --short vX.Y.Z` | tag 对象 hash | 无实用价值，容易误用 |
| `git rev-parse --short "vX.Y.Z^{commit}"` | **commit hash** | ✅ 这才是要登记的值 |
| `git ls-remote --tags origin vX.Y.Z` | tag 对象 hash | 仅用于确认 tag 是否已上远程 |

> `^{commit}` **不可省**。两段 hash 不同是正常现象，不是仓库损坏。

### 3.3 因此：merge commit hash 要在合并完成的当下取

```bash
git rev-parse --short master    # master HEAD 即刚生成的 merge commit
```

**不要**等到收尾阶段再从 tag 反查——反查必须带 `^{commit}`，一旦遗漏就会把 tag 对象 hash 当成 commit hash 登记进发布记录。

## 四、标准发布流程

> 命令跨平台一致，路径按本机实际项目根替换。Windows PowerShell 下注意引号差异（`'` 需按 PowerShell 转义规则处理）。

### 步骤 0：前置检查

```bash
git status                              # 工作区必须 clean
git checkout develop
git pull origin develop                 # develop 与远程同步
```

### 步骤 1：确认发布范围

```bash
git --no-pager log <上一版本tag>..develop --oneline
```

逐条确认范围包含的提交，作为 tag 说明的素材。

### 步骤 2：切到 master 并同步

```bash
git fetch origin
git checkout master
git status -sb                          # 必须显示 up to date with 'origin/master'
```

### 步骤 3：合并 develop

```bash
git merge develop --no-ff -m "Merge branch 'develop' into master"
```

必须 `--no-ff`，保留发布合并节点，便于回滚与历史回溯。

**合并完成后立刻记下本次 merge commit 短 hash**（见 §3.3），后续登记直接用这个值。

### 步骤 4：打 tag

```bash
git tag -a vX.Y.Z -m "vX.Y.Z：<核心改动摘要，多项用 + 连接>"
```

### 步骤 5：推送

```bash
git push origin master
git push origin vX.Y.Z
```

推送后确认 tag 已上远程：

```bash
git ls-remote --tags origin vX.Y.Z      # 返回值是 tag 对象 hash，仅确认存在即可
```

### 步骤 6：收尾

```bash
git checkout develop                    # 回到开发分支
git status -sb
```

若项目维护发布记录文件（CHANGELOG / 发布登记表等），按该文件既有体例追加本次记录：**版本 / merge commit 短 hash / tag 说明摘要**。摘要取 tag 说明去掉 `vX.Y.Z：` 前缀后的内容（多行说明只取首行）。

```bash
git add <发布记录文件>
git commit -m "docs: 补登 vX.Y.Z 发布记录（Merge <短 hash>）"
git push origin develop
```

> 该补登提交天然不在本次 tag 内，会随下一个版本合并进 master，这是固有结果，不算漏做。

## 五、Hotfix（紧急修复）流程

### 5.0 为什么单独一套流程

线上版本（`master` / 最新 tag）出严重问题需立刻修复，但 `develop` 上还堆着未完成、未验证的改动——此时直接发 `develop` 会把一堆未验证功能带上生产，风险比原 bug 更大。

所以：**从线上那个版本单独拉分支，只修这一个 bug，修完立刻发版；再把修复同步回 `develop`**，否则下次正常发版时该 bug 会原样复现。

| 对比项 | 正常发版（§四） | hotfix（本节） |
| --- | --- | --- |
| 源分支 | `develop`（本次全部改动） | `master` / 最新 tag（线上版本） |
| 改动范围 | 本次所有功能与修复 | 只改一个问题 |
| 发布时机 | 功能完成并验证后 | 立即 |
| 版本号 | 修复→补位+1；功能→次位+1 | 补位 +1 |

### H1：从线上版本切 hotfix 分支

```bash
git fetch --tags
git checkout -b hotfix/vX.Y.Z v<当前线上版本tag>    # ← 基于线上 tag，不是 develop
```

分支命名：`hotfix/vX.Y.Z`（版本号为**即将发布**的版本）。是否推送远程视协作需要：单人快修可不推，需协作或留档时 `git push -u origin hotfix/vX.Y.Z`。

### H2：只做最小修复

只改引发线上问题的那处代码，**不夹带**重构、优化、顺手修的其他问题。提交信息遵循项目既有的 commit message 规范。

### H3：验证（不可省）

至少在测试环境确认问题已解决、且未引入新问题。紧急不等于可以跳过验证——hotfix 直接上生产，出错影响比常规发版更大。

### H4：合回 master 并打 tag

```bash
git checkout master
git pull origin master
git merge hotfix/vX.Y.Z --no-ff -m "Merge branch 'hotfix/vX.Y.Z' into master"
git tag -a vX.Y.Z -m "vX.Y.Z：hotfix <中文说明>"
git push origin master
git push origin vX.Y.Z
```

与 §四 同规则：必须 `--no-ff`、必须 annotated tag、分别 push。tag 说明建议带 `hotfix` 字样便于回溯。合并后**立即**记下 merge hash：`git rev-parse --short master`。

### H5：把修复同步回 develop（最易漏，但必须做）

```bash
git checkout develop
git pull origin develop
git merge vX.Y.Z --no-ff -m "Merge tag 'vX.Y.Z' into develop"
git push origin develop
```

漏掉这一步，下次从 `develop` 正常发版时该 bug 会原样回来。

若 `develop` 已改动过同一处代码，本步会产生冲突：**按常规解决冲突后 `git add` 并 `git commit` 完成合并**（不要 `--abort` 放弃，否则修复就丢了），再 push。

### H6：清理临时分支

```bash
git branch -d hotfix/vX.Y.Z
git push origin --delete hotfix/vX.Y.Z    # 仅当该分支曾推送到远程
```

### H7：登记发布记录

同 §四 步骤 6，合并方式一列填 `Merge branch 'hotfix/vX.Y.Z' into master`。

## 六、发布检查清单

### 发布前

- [ ] 工作区 clean；未跟踪文件需先确认是否入库，未入库的一律不参与发布
- [ ] `develop` 与 `origin/develop` 同步（本地无未推送提交）
- [ ] `master` 与 `origin/master` 同步
- [ ] 发布范围已通过 `git log <上一tag>..develop --oneline` 明确列出
- [ ] tag 名称不与现有 tag 重复
- [ ] 版本号递增符合 §二 规则

### 发布后

- [ ] `master` 与本次 tag 均已推送远程
- [ ] merge commit 短 hash 已记录（且确认是 commit hash，非 tag 对象 hash）
- [ ] 发布记录已补登，且补登提交已 `git push origin develop`
- [ ] `git status -sb` 显示 develop 与远程同步

## 七、禁止项

| 禁止 | 原因 |
| --- | --- |
| `git push --force` / `+refs` | 覆盖远程历史，多协作场景不可逆 |
| `git push --tags` 一把梭 | 会把本地残留的历史 / 实验 tag 全部推上去，只推本次 tag |
| 用轻量 tag 代替 annotated tag | 丢失作者、时间与发布说明 |
| 发布合并用 fast-forward | 丢失发布合并节点 |
| hotfix 不回灌 develop | bug 在下次正常发版时复现 |
| 从 tag 反查 hash 时省略 `^{commit}` | 拿到的是 tag 对象 hash，会污染发布记录 |

## 八、多 worktree 场景

> 适用：同一仓库被检出到多个目录（`git worktree add`）。本仓库即为此结构——`TEngine/`、`TEngine_Fantasy/`、`TEngine_Unity6000/` 是**同一仓库的三个 worktree**。

### 1. 先确认是不是 worktree

```bash
git worktree list
```

第一行是主工作树，其余是链接工作树。**判别信号**：多个目录执行 `git branch -a` 得到完全相同的分支列表 → 它们是同一仓库，**不是多个独立仓库**。误判会直接导致把「各目录的文件集合」当成「各分支的改动集合」，进而推错合并结果。

### 2. 三条约束

| 约束 | 说明 |
| --- | --- |
| 分支独占 | 一个分支同时只能被一个 worktree 检出；对已被其他 worktree 检出的分支执行 `git checkout` 会失败 |
| 切换重写整个工作区 | 在某 worktree 切换分支会重写该目录**全部**文件（实测 3177 → 4483 个），该目录原有内容的独立副本随之消失 |
| 操作作用于共享仓库 | 任一 worktree 中的 commit / 分支 / tag 操作都作用于同一仓库，其他 worktree 立即可见 |

### 3. 做法

- 新建工作分支时以 **remote ref 为起点**规避占用：`git switch -c <新分支> origin/<源分支>`（优于 `git checkout -b <新分支> <本地分支>`）
- 做整合 / 发布类操作前，先 `git worktree list` 确认目标分支未被其他 worktree 检出
- 切换会大幅改变工作区的分支前，先建 **annotated tag** 作备份点——普通分支会被后续提交移动，不适合当备份
- 需要在多个分支间反复对照时，**新增 worktree**，而不是在同一个 worktree 里来回切换

### 4. 禁止项

| 禁止 | 原因 |
| --- | --- |
| 把多个 worktree 目录当作独立仓库 | 误判分支改动范围，推错合并结果 |
| 未确认占用情况就切换共享分支 | 可能失败；或导致另一目录的工作区被整体重建 |
| 用普通分支做备份点 | 会被后续提交移动；用 annotated tag |
| 以「某目录现在有哪些文件」推断分支内容 | 工作区快照 ≠ 该分支相对 base 的改动；见 `merge-verification.md` |
