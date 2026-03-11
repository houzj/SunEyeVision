# SunEyeVision CodeBuddy 规则系统

## 📋 概述

SunEyeVision 项目的 CodeBuddy 规则系统，用于指导代码开发、方案设计和工作流程。

## 🚀 快速开始

### 查看所有规则
- **规则索引**: [rules/rules-index.md](rules/rules-index.md)
- **变更日志**: [rules/CHANGELOG.md](rules/CHANGELOG.md)

### 规则分类

#### 01-编码规范 (Coding Standards)
- [property-notification.mdc](rules/01-coding-standards/property-notification.mdc) - 属性更改通知统一规范
- [naming-conventions.mdc](rules/01-coding-standards/naming-conventions.mdc) - 命名规范
- [logging-system.mdc](rules/01-coding-standards/logging-system.mdc) - 日志系统使用规范

#### 02-开发流程 (Development Process)
- [solution-design.mdc](rules/02-development-process/solution-design.mdc) - 方案设计要求

#### 03-质量控制 (Quality Control)
- [README.md](rules/03-quality-control/README.md) - 质量控制规则

#### 04-工作流指导 (Workflow Guidance)
- [implementation-approval.mdc](rules/04-workflow-guidance/implementation-approval.mdc) - 实施方案审批流程
- [documentation-policy.mdc](rules/04-workflow-guidance/documentation-policy.mdc) - 文档管理政策

## 📊 规则统计

- **总规则数**: 7
- **Critical**: 1 | **High**: 3 | **Medium**: 2 | **Low**: 1
- **总是应用**: 4 | **手动应用**: 3

## 📁 目录结构

```
.codebuddy/
├── config/                      # 配置文件
│   └── priority-system.yaml     # 优先级系统配置
├── rules/                       # 规则文件
│   ├── 01-coding-standards/    # 编码规范
│   ├── 02-development-process/ # 开发流程
│   ├── 03-quality-control/    # 质量控制
│   ├── 04-workflow-guidance/   # 工作流指导
│   ├── rules-index.md          # 规则索引
│   └── CHANGELOG.md            # 变更日志
└── templates/                   # 模板文件
    ├── code-review-checklist.md    # 代码审查检查清单
    └── rule-exemption-request.md   # 规则豁免申请表
```

## 🎯 使用指南

### 开发前
1. 阅读相关规则，了解编码规范和最佳实践
2. 查阅规则索引，了解所有规则
3. 了解优先级系统，知道哪些规则最重要

### 方案设计
1. 遵循方案设计要求（rule-004）
2. 考虑整体架构和现有基础设施
3. 提供完整的方案和风险评估

### 代码编写
1. 遵循编码规范（rule-001, rule-002, rule-003）
2. 使用正确的日志系统
3. 使用统一的命名规范
4. 使用 ObservableObject 进行属性通知

### 方案实施
1. 提交实施方案审批（rule-005）
2. 获得批准后开始实施
3. 及时反馈进展

### 文档创建
1. 提交文档创建请求（rule-006）
2. 获得批准后创建文档
3. 确保文档质量和格式规范

### 代码审查
1. 使用代码审查检查清单
2. 检查所有相关规则
3. 记录发现的问题

## 🔧 配置和模板

### 优先级系统
- [priority-system.yaml](config/priority-system.yaml) - 优先级系统配置

### 模板文件
- [code-review-checklist.md](templates/code-review-checklist.md) - 代码审查检查清单
- [rule-exemption-request.md](templates/rule-exemption-request.md) - 规则豁免申请表

## 📈 规则遵循率目标

- **Critical**: 100%
- **High**: ≥95%
- **Medium**: ≥90%
- **Low**: ≥80%

## 🤝 贡献指南

### 规则改进
1. 识别规则问题或改进需求
2. 联系技术负责人
3. 参与规则评审会议
4. 提交规则变更提案

### 规则豁免
1. 如需豁免规则，填写豁免申请表
2. 提交技术负责人审批
3. 获得批准后在代码中添加豁免注释
4. 定期评估豁免必要性

## 📚 参考资料

- [规则索引](rules/rules-index.md)
- [变更日志](rules/CHANGELOG.md)
- [优先级系统配置](config/priority-system.yaml)

## 🔄 版本历史

- **v2.0.0** (2026-03-11): 全面优化规则系统
- **v1.2.0** (2026-03-10): 新增方案设计和文档管理规则
- **v1.1.0** (2026-03-09): 新增属性更改通知统一规范
- **v1.0.0** (2026-03-08): 初始版本

---

**当前版本**: 2.0.0
**最后更新**: 2026-03-11
**维护者**: SunEyeVision Team
