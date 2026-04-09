# SunEyeVision 项目规则索引

## 📚 规则遵守工具

### 快速链接
|- [规则强制执行提示词](rule-enforcement-prompt.md) - AI 助手规则强制执行提示词
|- [规则检查器](../../docs/rule-checker-usage.md) - 自动化规则检查工具
|- [规则检查清单](../rules-checklist.md) - 强制执行的检查项列表
|- [方案设计思维模板](../solution-design-template.md) - 强制执行的思考流程
|- [使用指南](../README.md) - 工具使用说明

### 何时使用？
- ✅ AI 助手生成方案前（阅读规则强制执行提示词）
- ✅ 代码提交前（运行规则检查器）
- ✅ 设计新的功能方案
- ✅ 修改现有代码
- ✅ 重构代码结构
- ✅ 创建新的类或接口
- ✅ 修改日志输出
- ✅ 修改属性通知逻辑

### 使用流程
1. AI 助手：阅读 [规则强制执行提示词](rule-enforcement-prompt.md)
2. AI 助手：按照 [方案设计思维模板](../solution-design-template.md) 思考
3. 开发人员：运行 [规则检查器](../../docs/rule-checker-usage.md)
4. AI 助手：执行自我审查，使用 [规则检查清单](../rules-checklist.md)
5. 开发人员：提交代码（Git hook 会自动检查）
6. 提交方案或文档（需要审批）

---

## 📊 规则统计

- **总规则数**: 12
- **Critical**: 5 | **High**: 4 | **Medium**: 2 | **Low**: 1
- **总是应用**: 9 | **手动应用**: 3
- **已启用**: 12 | **已禁用**: 0

## 🗂️ 按分类浏览

### 01-编码规范 (Coding Standards)

| ID | 规则名称 | 优先级 | 状态 | 更新时间 |
|----|---------|-------|------|----------|
| [rule-001](01-coding-standards/property-notification.mdc) | 属性更改通知统一规范 | 🔴 Critical | ✅ | 2026-03-11 |
| [rule-002](01-coding-standards/naming-conventions.mdc) | 命名规范 | 🟠 High | ✅ | 2026-03-11 |
| [rule-003](01-coding-standards/logging-system.mdc) | 日志系统使用规范 | 🟠 High | ✅ | 2026-03-11 |

**规则概述**:
- 统一属性更改通知机制，使用 ObservableObject 基类
- 遵循视觉软件风格的命名规范
- 统一日志输出机制，禁止使用 Debug/Trace/Console

---

### 02-开发流程 (Development Process)

| ID | 规则名称 | 优先级 | 状态 | 更新时间 |
|----|---------|-------|------|----------|
| [rule-004](02-development-process/solution-design.mdc) | 方案设计要求 | 🟠 High | ✅ | 2026-03-11 |
| [rule-008](02-development-process/prototype-design-clean-principle.mdc) | 原型设计期代码纯净原则 | 🔴 Critical | ✅ | 2026-03-18 |
| [rule-009](02-development-process/development-principles.mdc) | 开发原则规范 | 🔴 Critical | ✅ | 2026-03-20 |
| [rule-010](02-development-process/solution-system-implementation.mdc) | 方案系统实现规范 | 🔴 Critical | ✅ | 2026-03-20 |
| [rule-011](02-development-process/temp-file-cleanup.mdc) | 临时文件自动清理规则 | 🟠 High | ✅ | 2026-03-20 |
| [rule-012](02-development-process/parameter-system-constraints.mdc) | 参数系统约束条件 | 🟠 High | ✅ | 2026-03-21 |

**规则概述**:
- 生成方案时需要根据软件的完整上下文
- 考虑整体架构、现有基础设施、可维护性和扩展性
- 提供完整的方案框架和风险评估
- 原型阶段不考虑向后兼容，保持代码纯净
- 遵循YAGNI、KISS、按需设计三大开发原则
- 优先使用JsonPolymorphic，特殊场景允许Dictionary转换层
- 临时文件自动清理，避免项目文件夹污染
- 参数系统约束条件：UI层Dictionary存储、工具注册机制、参数绑定系统

---

### 03-质量控制 (Quality Control)

| ID | 规则名称 | 优先级 | 状态 | 更新时间 |
|----|---------|-------|------|----------|
| [rule-007](03-quality-control/README.md) | 质量控制规则 | 🟡 Medium | ✅ | 2026-03-11 |

**规则概述**:
- 性能优化指导（待完善）
- 错误处理规范（待完善）
- 测试要求（待完善）

---

### 04-工作流指导 (Workflow Guidance)

| ID | 规则名称 | 优先级 | 状态 | 更新时间 |
|----|---------|-------|------|----------|
| [rule-005](04-workflow-guidance/implementation-approval.mdc) | 实施方案审批流程 | 🟠 High | ⚙️ | 2026-03-11 |
| [rule-006](04-workflow-guidance/documentation-policy.mdc) | 文档管理政策 | 🟡 Medium | ⚙️ | 2026-03-11 |

**规则概述**:
- 方案实施前需要请求确认
- 文档创建前需要请求确认
- 禁止随意生成本地文档

---

## 🔍 快速查找

### 按优先级

#### 🔴 Critical (5条)
- [rule-001: 属性更改通知统一规范](01-coding-standards/property-notification.mdc)
- [rule-008: 原型设计期代码纯净原则](02-development-process/prototype-design-clean-principle.mdc)
- [rule-009: 开发原则规范](02-development-process/development-principles.mdc)
- [rule-010: 方案系统实现规范](02-development-process/solution-system-implementation.mdc)

#### 🟠 High (5条)
- [rule-002: 命名规范](01-coding-standards/naming-conventions.mdc)
- [rule-003: 日志系统使用规范](01-coding-standards/logging-system.mdc)
- [rule-004: 方案设计要求](02-development-process/solution-design.mdc)
- [rule-011: 临时文件自动清理规则](02-development-process/temp-file-cleanup.mdc)
- [rule-012: 参数系统约束条件](02-development-process/parameter-system-constraints.mdc)

#### 🟡 Medium (2条)
- [rule-006: 文档管理政策](04-workflow-guidance/documentation-policy.mdc)
- [rule-007: 质量控制规则](03-quality-control/README.md)

#### 🟢 Low (0条)
- 暂无

### 按状态

#### ✅ 已启用 (11条)
- rule-001: 属性更改通知统一规范
- rule-002: 命名规范
- rule-003: 日志系统使用规范
- rule-004: 方案设计要求
- rule-005: 实施方案审批流程
- rule-006: 文档管理政策
- rule-007: 质量控制规则
- rule-008: 原型设计期代码纯净原则
- rule-009: 开发原则规范
- rule-010: 方案系统实现规范
- rule-011: 临时文件自动清理规则
- rule-012: 参数系统约束条件

#### ❌ 已禁用 (0条)
- 暂无

#### ⚠️ 已废弃 (0条)
- 暂无

### 按生效模式

#### 总是应用 (9条)
- rule-001: 属性更改通知统一规范
- rule-002: 命名规范
- rule-003: 日志系统使用规范
- rule-004: 方案设计要求
- rule-007: 质量控制规则
- rule-008: 原型设计期代码纯净原则
- rule-009: 开发原则规范
- rule-010: 方案系统实现规范
- rule-011: 临时文件自动清理规则
- rule-012: 参数系统约束条件

#### 手动应用 (3条)
- rule-005: 实施方案审批流程
- rule-006: 文档管理政策

---

## 📈 规则执行趋势

### 代码审查统计
- 2026-03-24: 新增规则强制执行机制（提示词 + 规则检查器）
- 2026-03-21: 新增 rule-012 参数系统约束条件
- 2026-03-20: 规则优化完成，共11条规则（新增rule-011）
- 2026-03-10: 新增方案设计要求和文档管理政策
- 2026-03-09: 新增属性更改通知统一规范

### 规则遵循率目标
- Critical 规则: 100%
- High 规则: ≥95%
- Medium 规则: ≥90%
- Low 规则: ≥80%

---

## 🔄 最近更新

### 2026-03-24
- ✅ 新增规则强制执行机制
  - 规则强制执行提示词（Rule Enforcement Prompt）
  - 规则检查器（Rule Checker）- 自动化规则检查工具
  - Git pre-commit hook 集成
  - VS Code 任务集成

### 2026-03-21
- ✅ 新增规则
  - rule-012: 参数系统约束条件（基于参数序列化优化方案总结）

### 2026-03-20
- ✅ 新增规则
  - rule-010: 方案系统实现规范（从"禁止Dictionary转换层"改为"特殊场景允许"）
  - rule-009: 开发原则规范（整合YAGNI、KISS、按需设计三大原则）
  - rule-011: 临时文件自动清理规则
- ✅ 修正规则
  - rule-010: 修正规则内容，明确允许和禁止的使用场景

### 2026-03-18
- ✅ 新增规则
  - rule-008: 原型设计期代码纯净原则（从文档中提取并规范化）

### 2026-03-11
- ✅ 全面优化规则系统
  - 新增规则元数据格式
  - 建立分类体系
  - 完善规则内容和示例
  - 创建规则索引和变更日志
  - 建立优先级系统

### 2026-03-10
- ✅ 新增规则
  - 方案设计要求
  - 文档管理政策

### 2026-03-09
- ✅ 新增规则
  - 属性更改通知统一规范

---

## 📋 规则使用指南

### 如何使用规则

1. **AI 助手生成方案前**: 阅读规则强制执行提示词，确保方案符合所有规则
2. **开发前**: 查阅相关规则，了解编码规范和最佳实践
3. **方案设计**: 参考方案设计要求，确保方案完整性
4. **代码编写**: 遵循编码规范，使用正确的日志和命名
5. **代码审查**: 使用规则检查器和检查清单，确保代码质量
6. **方案实施**: 提交审批请求，获得批准后实施
7. **文档创建**: 提交文档创建请求，获得批准后创建
8. **代码提交**: Git hook 会自动运行规则检查器

### 规则优先级

1. **Critical**: 必须遵守，违反会导致代码审查不通过
2. **High**: 强烈建议遵守，有充分理由才能违反
3. **Medium**: 一般性指导原则
4. **Low**: 最佳实践，可灵活处理

### 规则冲突处理

当规则之间出现冲突时：
1. 优先级高的规则生效
2. 总是应用的规则优于手动应用
3. 具体规则优于通用规则
4. 无法解决时，标记为需要人工审查

---

## 📚 相关资源

- [规则强制执行提示词](rule-enforcement-prompt.md)
- [规则检查器使用指南](../../docs/rule-checker-usage.md)
- [变更日志](CHANGELOG.md)
- [优先级系统配置](../config/priority-system.yaml)
- [规则豁免申请模板](../templates/rule-exemption-request.md)
- [代码审查检查清单](../templates/code-review-checklist.md)

---

## 🤝 规则反馈

如果发现规则存在问题或有改进建议，请：
1. 记录问题或建议
2. 联系技术负责人
3. 参与规则评审会议
4. 提交规则变更提案

---

**最后更新**: 2026-03-24
**维护者**: SunEyeVision Team
**版本**: 3.0
