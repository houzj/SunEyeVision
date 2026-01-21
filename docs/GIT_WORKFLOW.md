# SunEyeVision 项目代码管理指南

## 📋 概述

本项目已成功初始化 Git 仓库，建立了完整的代码管理体系。

## 🔄 当前 Git 状态

### 提交历史
```
08c7b51 - 首次提交: SunEyeVision 项目完整代码库 (刚刚)
e1d98b2 - 初始化: 添加 Git 仓库和 .gitignore 文件
```

### 当前分支: main

## 📁 项目结构

```
SunEyeVision/
├── SunEyeVision.Algorithms/    # 算法模块
├── SunEyeVision.Core/         # 核心模块（接口、模型、服务）
├── SunEyeVision.DeviceDriver/ # 设备驱动模块
├── SunEyeVision.PluginSystem/ # 插件系统模块
├── SunEyeVision.UI/           # UI 界面模块
├── SunEyeVision.Workflow/     # 工作流模块
├── SunEyeVision.Demo/         # 演示项目
├── SunEyeVision.Test/         # 测试项目
├── SunEyeVision.sln           # 解决方案文件
└── docs/                      # 文档目录
```

## 🛠️ 代码管理操作

### 1. 日常开发流程

```bash
# 查看当前状态
git status

# 查看修改内容
git diff

# 添加修改的文件
git add .

# 提交代码
git commit -m "描述信息"

# 推送到远程（如已配置）
git push origin main
```

### 2. 分支管理

```bash
# 查看所有分支
git branch -a

# 创建新分支
git checkout -b feature/新功能名称

# 切换分支
git checkout 分支名

# 合并分支
git merge 分支名

# 删除分支
git branch -d 分支名
```

### 3. 提交规范

使用清晰的提交信息：

```bash
# 格式: <类型>: <描述>

# 功能开发
git commit -m "feat: 添加边缘检测算法"

# Bug 修复
git commit -m "fix: 修复设备驱动连接超时问题"

# 文档更新
git commit -m "docs: 更新 API 使用说明"

# 重构
git commit -m "refactor: 优化工作流引擎结构"

# 性能优化
git commit -m "perf: 提升图像处理性能"

# 测试
git commit -m "test: 添加单元测试"
```

### 4. 查看历史

```bash
# 查看提交历史（简洁）
git log --oneline

# 查看详细历史
git log --graph --pretty=format:'%h - %an, %ar : %s'

# 查看文件修改历史
git log -p 文件名

# 查看特定作者的提交
git log --author="作者名"
```

### 5. 撤销操作

```bash
# 撤销工作区的修改（未暂存）
git checkout -- 文件名

# 撤销暂存区的修改（未提交）
git reset HEAD 文件名

# 撤销最近的提交（保留修改）
git reset --soft HEAD~1

# 撤销最近的提交（不保留修改）
git reset --hard HEAD~1
```

## 🔧 构建和测试

### 构建项目

```bash
# 构建整个解决方案
dotnet build SunEyeVision.sln

# 清理并重新构建
dotnet clean SunEyeVision.sln
dotnet build SunEyeVision.sln

# 发布版本
dotnet publish SunEyeVision.UI/SunEyeVision.UI.csproj -c Release
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定项目
dotnet test SunEyeVision.Test/SunEyeVision.Test.csproj
```

### 运行应用

```bash
# 运行 UI 项目
dotnet run --project SunEyeVision.UI/SunEyeVision.UI.csproj
```

## 📊 代码审查检查清单

- [ ] 代码遵循项目命名规范
- [ ] 添加了必要的注释和文档
- [ ] 通过了所有单元测试
- [ ] 构建成功无警告
- [ ] 提交信息清晰明确
- [ ] 修改内容与提交信息一致

## 🚀 推荐工作流

### 功能开发流程

1. **创建功能分支**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **开发功能**
   ```bash
   # 编写代码
   # 本地测试
   dotnet build
   dotnet run
   ```

3. **提交代码**
   ```bash
   git add .
   git commit -m "feat: 添加新功能描述"
   ```

4. **合并到主分支**
   ```bash
   git checkout main
   git merge feature/your-feature-name
   git push origin main
   ```

### Bug 修复流程

1. **创建修复分支**
   ```bash
   git checkout -b bugfix/issue-number
   ```

2. **修复并测试**
   ```bash
   # 修复代码
   # 验证修复
   dotnet test
   ```

3. **提交修复**
   ```bash
   git add .
   git commit -m "fix: 修复问题描述"
   ```

4. **合并并清理**
   ```bash
   git checkout main
   git merge bugfix/issue-number
   git branch -d bugfix/issue-number
   ```

## 📝 注意事项

1. **忽略文件**: `.gitignore` 已配置，自动忽略构建输出、临时文件等
2. **编码规范**: 遵循 .NET 命名约定和项目现有风格
3. **提交前**: 确保 `dotnet build` 成功通过
4. **分支管理**: 主分支（main）保持稳定，新功能在分支开发

## 🔗 常用链接

- [Git 官方文档](https://git-scm.com/doc)
- [.NET CLI 参考](https://docs.microsoft.com/zh-cn/dotnet/core/tools/)
- [项目文档](./Help/Output/)

---

**最后更新**: 2026-01-21
