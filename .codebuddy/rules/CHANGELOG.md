# 规则变更日志

本文档记录 SunEyeVision 项目规则的变更历史。

---

## [Unreleased]

### Added
- 新增规则索引系统 (rules-index.md)
- 新增变更日志机制 (CHANGELOG.md)
- 新增优先级系统配置 (config/priority-system.yaml)
- 新增规则豁免申请模板 (templates/rule-exemption-request.md)
- 新增代码审查检查清单 (templates/code-review-checklist.md)

### Changed
- 全面重构规则文件结构
- 建立四层分类体系（编码规范、开发流程、质量控制、工作流指导）
- 为所有规则添加标准元数据 frontmatter
- 扩充规则内容，添加详细示例和检查清单
- 统一规则文档格式
- **修正方案系统实现规范 (rule-005)**：从"禁止Dictionary转换层"改为"特殊场景允许"
  - 明确允许在自定义 JsonConverter 中使用 Dictionary 转换层
  - 明确允许在处理特殊类型（OpenCvSharp.Point/Rect等）时使用
  - 禁止在业务逻辑中直接调用 Dictionary 转换层
  - 要求 Dictionary 转换层方法设为 internal 访问级别

### Fixed
- 修复规则索引文件信息过时的问题
- 修复规则元数据不一致的问题
- 修复 rule-005 规则与实际代码不一致的问题

### Deprecated
- PropertyNotificationStandard.mdc 已标记为废弃（内容已合并到 property-notification.mdc）

---

## [2026-03-20] - Version 2.2.0

### Added
- 新增开发原则规范规则 (rule-009)
  - 整合 YAGNI 原则（You Aren't Gonna Need It）
  - 整合 KISS 原则（Keep It Simple and Smart）- 强调简单、最优、合理
  - 整合按需设计原则
  - 新增视觉软件开发标准章节
  - 参考主流视觉软件（VisionMaster、VisionPro、基恩士等）的设计经验
  - 提供详细的实践指南和案例分析
  - 建立决策检查流程

### Changed
- 更新规则索引，总规则数增加到 9 条
- 更新 Critical 优先级规则数量（从 2 增加到 3）
- 更新"总是应用"规则数量（从 5 增加到 6）
- 优化 KISS 原则的中文表述，去掉"愚蠢"，强调"简单、最优、合理"
- 增加视觉软件行业的参考标准
- 增加检查清单中的视觉软件相关检查项
- 新增"简单但不简陋"的对比示例
- 新增视觉软件常见误区的说明

---

## [2026-03-18] - Version 2.1.0

### Added
- 新增原型设计期代码纯净原则规则 (rule-008)
  - 从文档中提取并规范化
  - 强调原型阶段不考虑向后兼容
  - 保持代码纯净，直接删除旧代码

### Changed
- 更新规则索引，总规则数增加到 8 条
- 更新 Critical 优先级规则数量（从 1 增加到 2）

---

## [2026-03-11] - Version 2.0.0

### Added
- 新增规则分类体系（4个分类文件夹）
- 新增规则元数据标准（id, title, category, priority 等）
- 新增规则索引文件 (rules-index.md)
- 新增变更日志文件 (CHANGELOG.md)
- 新增优先级系统配置 (priority-system.yaml)
- 新增规则豁免申请模板
- 新增代码审查检查清单模板
- 新增质量控制规则占位文件 (rule-007)

### Changed
- 重命名并迁移所有规则文件到新的分类结构
- property_notification_standard.mdc → 01-coding-standards/property-notification.mdc
- 命名要求.mdc → 01-coding-standards/naming-conventions.mdc
- 日志系统使用规范.mdc → 01-coding-standards/logging-system.mdc
- 提供方案要求.mdc → 02-development-process/solution-design.mdc
- 方案实施限制.mdc → 04-workflow-guidance/implementation-approval.mdc
- 文档写入限制.mdc → 04-workflow-guidance/documentation-policy.mdc

- 为所有规则添加完整的元数据 frontmatter
- 扩充所有规则的内容，添加详细示例
- 为所有规则添加检查清单
- 为所有规则添加变更历史
- 完善规则之间的关联关系

### Fixed
- 修复规则元数据不一致的问题
- 修复规则索引文件信息过时的问题
- 修复规则文件格式不统一的问题

### Deprecated
- PropertyNotificationStandard.mdc 已废弃（内容合并到 property-notification.mdc）

---

## [2026-03-10] - Version 1.2.0

### Added
- 新增方案设计要求规则 (rule-004)
- 新增文档管理政策规则 (rule-006)

### Changed
- 更新命名要求规则，标记为总是应用
- 更新日志系统使用规范，添加更多示例
- 更新方案实施限制规则，完善内容

---

## [2026-03-09] - Version 1.1.0

### Added
- 新增属性更改通知统一规范规则
- 新增 README.md 规则索引（基础版本）

---

## [2026-03-08] - Version 1.0.0

### Added
- 初始版本，包含基础规则：
  - 命名要求
  - 日志系统使用规范
  - 方案实施限制
  - 文档写入限制

---

## 版本说明

### 版本号格式
- 主版本号 (Major): 重大变更，可能不兼容
- 次版本号 (Minor): 新增功能，向后兼容
- 修订号 (Patch): Bug 修复，向后兼容

### 变更类型
- **Added**: 新增规则或功能
- **Changed**: 修改现有规则内容
- **Deprecated**: 标记规则为废弃
- **Removed**: 移除规则
- **Fixed**: 修复规则问题

---

## 贡献指南

如需提交规则变更：
1. 准备变更提案
2. 说明变更原因和影响
3. 提交到团队评审
4. 获得批准后实施

---

**最后更新**: 2026-03-20
**当前版本**: 2.3.0
