# 规则强制执行提示词（Rule Enforcement Prompt）

## 🔴 核心规则 - 必须遵守

在生成任何代码方案前，必须检查以下规则。**违反这些规则将导致方案被拒绝**。

---

## 🚨 CRITICAL 优先级规则

### Rule-001: 属性更改通知统一规范
- ✅ 所有需要属性通知的类必须继承 `ObservableObject` 或其派生类
- ✅ 属性设置必须使用 `SetProperty` 方法
- ❌ 禁止直接实现 `INotifyPropertyChanged`
- ❌ 禁止手动实现属性通知逻辑
- **检查方法**: 搜索 `SetProperty(` 使用情况

### Rule-008: 原型设计期代码纯净原则
- ✅ **不考虑向后兼容** - 直接删除旧代码
- ✅ **保持代码纯净** - 该重构就重构
- ❌ 禁止使用 `[Obsolete]` 标记
- ❌ 禁止保留注释掉的代码
- ❌ 禁止使用条件编译（`#if DEBUG`）
- **检查方法**: 搜索 `Obsolete`、`#if`、注释掉的代码块

### Rule-010: 方案系统实现规范
- ✅ 优先使用 `System.Text.Json` 的 `[JsonPolymorphic]` 特性
- ✅ 使用 PascalCase 命名（符合视觉软件行业标准）
- ❌ 禁止使用 Newtonsoft.Json
- ❌ 禁止在业务逻辑中直接调用 `ToSerializableDictionary`
- ❌ 禁止嵌套 Dictionary 作为数据模型
- **检查方法**: 搜索 `Newtonsoft.Json`、`ToSerializableDictionary(`

---

## 🟠 HIGH 优先级规则

### Rule-002: 命名规范
- ✅ 类名使用 PascalCase：`WorkflowEngine`
- ✅ 私有字段使用 `_camelCase`：`_threshold`
- ✅ 常量使用 UPPER_CASE：`MAX_RETRY_COUNT`
- ✅ 布尔值使用 Is/Has/Can 前缀：`IsEnabled`
- ✅ 接口前缀 I：`IPluginManager`
- ❌ 禁止使用缩写：`ImgProc` → `ImageProcessor`
- ❌ 禁止使用小驼峰命名类：`workflowEngine`
- **检查方法**: 检查所有新创建的类、方法、变量名

### Rule-003: 日志系统使用规范
- ✅ ViewModel 层使用：`LogInfo()`、`LogSuccess()`、`LogError()`
- ✅ Service 层使用：`_logger.Log(LogLevel.Info, ...)`
- ❌ 禁止使用 `System.Diagnostics.Debug.WriteLine()`
- ❌ 禁止使用 `Console.WriteLine()`
- ❌ 禁止日志输出到 VS 输出窗口
- **检查方法**: 搜索 `Debug.WriteLine`、`Console.WriteLine`、`Trace.WriteLine`

### Rule-011: 临时文件自动清理规则
- ✅ 脚本结束后自动删除临时文件
- ✅ 使用系统临时目录（`%TEMP%` 或 `$env:TEMP`）
- ✅ 临时文件使用随机名称
- ❌ 禁止在项目目录创建临时文件
- ❌ 禁止临时文件不被清理
- **检查方法**: 搜索脚本中的临时文件创建和清理逻辑

### Rule-012: 参数系统约束条件
- ✅ UI 层使用 `Dictionary<string, object>` 存储参数
- ✅ UI 层添加 `ParametersTypeName` 属性
- ✅ 保持工具注册机制不变
- ❌ 禁止修改 UI 层参数存储方式
- ❌ 禁止破坏现有工具注册机制
- **检查方法**: 检查参数转换逻辑和工具注册代码

---

## 📋 强制执行检查清单

在生成任何方案或代码前，必须完成以下检查：

### ✅ 代码规范检查
- [ ] 是否继承了 `ObservableObject`？
- [ ] 是否使用了 `SetProperty` 方法？
- [ ] 命名是否符合规范（PascalCase/camelCase/UPPER_CASE）？
- [ ] 布尔值是否有 Is/Has/Can 前缀？
- [ ] 是否避免了不必要的缩写？

### ✅ 日志系统检查
- [ ] 是否使用了项目的日志系统？
- [ ] 是否避免了 `Debug.WriteLine` 和 `Console.WriteLine`？
- [ ] 日志级别是否恰当（Info/Success/Warning/Error/Fatal）？

### ✅ 方案系统检查
- [ ] 是否使用了 `System.Text.Json`？
- [ ] 是否使用了 `[JsonPolymorphic]` 特性？
- [ ] JSON 命名是否使用 PascalCase？
- [ ] 是否避免了 Newtonsoft.Json？

### ✅ 代码纯净度检查
- [ ] 是否删除了所有旧代码（不保留兼容）？
- [ ] 是否删除了所有注释掉的代码？
- [ ] 是否删除了所有 `[Obsolete]` 标记？
- [ ] 是否避免了条件编译（`#if`）？

### ✅ 临时文件检查
- [ ] 脚本是否自动清理临时文件？
- [ ] 临时文件是否使用系统临时目录？
- [ ] 临时文件是否有随机名称？
- [ ] .gitignore 是否覆盖所有临时文件？

### ✅ 参数系统检查
- [ ] UI 层是否保持 Dictionary 存储方式？
- [ ] UI 层是否添加了 ParametersTypeName？
- [ ] 工具注册机制是否保持不变？
- [ ] 参数转换逻辑是否支持强类型？

---

## 🚨 违规处理流程

### 发现违规时的处理：

1. **立即停止** - 停止生成当前方案
2. **识别违规** - 明确指出违反了哪个规则
3. **提供修正** - 说明如何修正以符合规则
4. **重新检查** - 在修正后重新执行检查清单

### 示例：

```
❌ 发现违规：
- Rule-003: 使用了 System.Diagnostics.Debug.WriteLine()

✅ 正确做法：
使用 LogInfo() 替代：
- ViewModel 层: LogInfo("信息日志");
- Service 层: _logger.Log(LogLevel.Info, "信息", "来源");

正在重新生成方案...
```

---

## 💡 最佳实践提醒

### 1. 方案设计阶段
- ✅ 先分析现有架构和基础设施
- ✅ 优先使用现有的框架和模式
- ✅ 考虑整体影响，不要点对点解决问题

### 2. 代码实现阶段
- ✅ 遵循项目命名规范
- ✅ 使用项目的日志系统
- ✅ 保持代码纯净，删除旧代码

### 3. 测试和验证
- ✅ 生成代码前进行规则检查
- ✅ 生成代码后验证规则遵守情况
- ✅ 发现违规立即修正

---

## 🔗 规则文档索引

详细的规则文档位于 `.codebuddy/rules/` 目录：

- [rule-001: 属性更改通知统一规范](./01-coding-standards/property-notification.mdc)
- [rule-002: 命名规范](./01-coding-standards/naming-conventions.mdc)
- [rule-003: 日志系统使用规范](./01-coding-standards/logging-system.mdc)
- [rule-004: 方案设计要求](./02-development-process/solution-design.mdc)
- [rule-007: 质量控制规则](./02-development-process/quality-control.mdc)
- [rule-008: 原型设计期代码纯净原则](./02-development-process/prototype-design-clean-code.mdc)
- [rule-010: 方案系统实现规范](./02-development-process/solution-system-implementation.mdc)
- [rule-011: 临时文件自动清理规则](./02-development-process/temp-file-cleanup.mdc)
- [rule-012: 参数系统约束条件](./02-development-process/parameter-system-constraints.mdc)

---

## 📚 使用方法

### 对于 AI 助手：
1. **每次生成方案前**，必须先执行规则检查清单
2. **生成代码时**，确保每一条规则都被遵守
3. **发现违规时**，立即停止并修正

### 对于开发人员：
1. **代码审查时**，使用检查清单验证代码质量
2. **遇到违规**，参考规则文档了解正确做法
3. **持续改进**，不断优化规则和检查流程

---

## 🎯 目标

通过本提示词和规则检查工具，实现：

1. **强制执行**：规则不再是可选的建议，而是必须遵守的要求
2. **自动检查**：通过工具自动检查违规，减少人工遗漏
3. **及时修正**：发现违规立即修正，避免累积
4. **质量保障**：确保代码质量和项目标准一致性

---

## 🔄 变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2026-03-24 | 1.0 | 初始版本，创建规则强制执行提示词 | Team |
