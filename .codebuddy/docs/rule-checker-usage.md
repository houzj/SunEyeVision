# 规则检查器使用指南 (Rule Checker Usage Guide)

## 📋 概述

规则检查器（Rule Checker）是 SunEyeVision 项目的自动化规则检查工具，用于确保代码符合项目规范。它能够自动检测代码中的违规行为，并提供详细的修复建议。

## 🎯 功能特性

- ✅ 自动检测 9 条项目规则的违规行为
- ✅ 按优先级分类显示违规（Critical/High/Medium/Low）
- ✅ 提供详细的违规位置和修复建议
- ✅ 支持命令行、VS Code 任务、Git hook 多种使用方式
- ✅ 可集成到 CI/CD 流程中

## 🚀 快速开始

### 1. 编译规则检查器

```batch
build_rule_checker.bat
```

### 2. 运行规则检查器

```batch
utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe
```

## 📖 使用方式

### 方式1：命令行运行

#### 检查整个项目

```batch
utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe
```

#### 检查指定目录

```batch
utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe "d:\MyWork\SunEyeVision_Dev\src"
```

### 方式2：VS Code 任务

#### 方法1：使用快捷键

1. 按 `Ctrl+Shift+P` 打开命令面板
2. 输入 "Tasks: Run Task"
3. 选择 "🔍 检查规则 (Rule Checker)"

#### 方法2：使用命令面板

1. 按 `Ctrl+Shift+P` 打开命令面板
2. 输入 "Run Task"
3. 选择 "🔍 检查规则 (Rule Checker)"

### 方式3：Git pre-commit hook

规则检查器会自动在提交前运行，检测违规行为：

```batch
git commit -m "feat: add new feature"
```

如果发现违规，提交会被阻止，需要修复后重新提交。

#### 跳过规则检查（不推荐）

```batch
git commit --no-verify -m "feat: add new feature"
```

## 📊 检查结果说明

### 🔴 CRITICAL 优先级违规

**影响**：必须立即修复，禁止提交代码

**示例**：
```
🔴 CRITICAL 优先级违规（必须立即修复）：

📄 d:\MyWork\SunEyeVision_Dev\src\UI\Models\WorkflowNode.cs

   [Rule-010] 使用 Newtonsoft.Json，应该使用 System.Text.Json
   📍 行 15: 改为使用 System.Text.Json 和 [JsonPolymorphic] 特性
```

### 🟠 HIGH 优先级违规

**影响**：应该尽快修复，但允许提交

**示例**：
```
🟠 HIGH 优先级违规（应该尽快修复）：

📄 d:\MyWork\SunEyeVision_Dev\src\Core\Services\ImageProcessor.cs

   [Rule-003] 使用 System.Diagnostics.Debug.WriteLine()
   📍 行 42: 使用项目的日志系统：ViewModel 层使用 LogInfo()，Service 层使用 _logger.Log()
```

### 🟡 MEDIUM 优先级违规

**影响**：可以延后修复

### 🟢 LOW 优先级违规

**影响**：可选修复

## 🔧 支持的规则

### Rule-001: 属性更改通知统一规范 🔴

**检查内容**：
- ❌ 直接实现 `INotifyPropertyChanged`（应该继承 `ObservableObject`）
- ❌ 手动实现属性通知逻辑（应该使用 `SetProperty` 方法）
- ❌ 重复实现 `INotifyPropertyChanged`

### Rule-002: 命名规范 🟠

**检查内容**：
- ❌ 类名使用小驼峰（应该使用 PascalCase）
- ❌ 私有字段无下划线前缀（应该使用 `_camelCase`）
- ❌ 布尔值缺少 Is/Has/Can 前缀

### Rule-003: 日志系统使用规范 🟠

**检查内容**：
- ❌ 使用 `System.Diagnostics.Debug.WriteLine()`
- ❌ 使用 `System.Diagnostics.Trace.WriteLine()`
- ❌ 使用 `Console.WriteLine()`

### Rule-008: 原型设计期代码纯净原则 🔴

**检查内容**：
- ❌ 使用 `[Obsolete]` 标记
- ❌ 使用条件编译（`#if`）
- ❌ 保留注释掉的代码

### Rule-010: 方案系统实现规范 🔴

**检查内容**：
- ❌ 使用 `Newtonsoft.Json`
- ❌ 在业务逻辑中直接调用 `ToSerializableDictionary()`

### Rule-011: 临时文件自动清理规则 🟠

**检查内容**：
- ❌ 脚本创建了临时文件但没有清理
- ❌ 脚本创建了临时文件但没有使用 Try-Finally 确保清理

### Rule-012: 参数系统约束条件 🟠

**检查内容**：
- ❌ UI 层没有使用 `Dictionary<string, object>` 存储参数
- ❌ UI 层没有添加 `ParametersTypeName` 属性
- ❌ 工具注册使用字符串匹配（应该使用类型比较）

## 💡 最佳实践

### 1. 开发流程

```batch
# 1. 编写代码
# 2. 运行规则检查
utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe

# 3. 修复发现的违规
# 4. 重新运行规则检查确认修复
# 5. 提交代码（Git hook 会自动检查）
git commit -m "feat: add new feature"
```

### 2. VS Code 集成

#### 方法1：保存时自动运行

在 `.vscode/settings.json` 中添加：

```json
{
  "editor.codeActionsOnSave": {
    "source.fixAll": true
  }
}
```

#### 方法2：使用快捷键

在 `.vscode/keybindings.json` 中添加：

```json
{
  "key": "ctrl+shift+r",
  "command": "workbench.action.tasks.runTask",
  "args": "🔍 检查规则 (Rule Checker)"
}
```

### 3. CI/CD 集成

#### GitHub Actions

```yaml
name: Rule Checker

on: [push, pull_request]

jobs:
  check-rules:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Build Rule Checker
        run: dotnet build utilities/RuleChecker/RuleChecker.csproj --configuration Release
      - name: Run Rule Checker
        run: utilities/RuleChecker/bin/Release/net8.0/SunEyeVision.RuleChecker
```

#### GitLab CI

```yaml
stages:
  - check

rule-checker:
  stage: check
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet build utilities/RuleChecker/RuleChecker.csproj --configuration Release
    - utilities/RuleChecker/bin/Release/net8.0/SunEyeVision.RuleChecker
  only:
    - merge_requests
    - main
```

## 🔍 故障排除

### 问题1：规则检查器未找到

**错误信息**：
```
⚠️  规则检查器未找到，跳过规则检查
```

**解决方案**：
```batch
build_rule_checker.bat
```

### 问题2：检查速度慢

**原因**：扫描整个项目需要时间

**解决方案**：检查指定目录
```batch
utilities\RuleChecker\bin\Release\net8.0\SunEyeVision.RuleChecker.exe "d:\MyWork\SunEyeVision_Dev\src\UI"
```

### 问题3：误报

**原因**：规则检查器基于静态分析，可能存在误报

**解决方案**：
1. 检查代码是否真的违反了规则
2. 如果没有违反，可以添加注释说明原因（某些规则允许有详细注释）
3. 报告误报给团队，改进规则检查器

## 📚 相关文档

- [规则强制执行提示词](../rules/enforcement/rule-enforcement-prompt.md)
- [项目规则文档](../rules/)
- [规则目录说明](../rules/README.md)
- [规则检查器源码](../../utilities/RuleChecker/)

## 🔄 变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2026-03-24 | 1.0 | 初始版本，创建规则检查器和使用文档 | Team |

---

## 🎯 目标

通过规则检查器，实现：

1. **自动化检查**：自动检测代码违规，减少人工审查
2. **及时反馈**：在开发早期发现问题，避免累积
3. **代码质量**：确保代码符合项目规范
4. **团队协作**：统一代码标准，提高协作效率
