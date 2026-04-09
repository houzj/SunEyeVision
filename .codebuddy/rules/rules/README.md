# 📚 规则文件目录结构说明

> **最后更新**: 2026-03-24 | **版本**: 1.0

---

## 📋 目录结构

```
.codebuddy/rules/
├── README.md                                    # 本文件：目录结构说明
├── CHANGELOG.md                                # 系统提示词变更日志
├── rules-index.md                              # 规则索引（主入口）
│
├── system/                                     # 🤖 系统提示词相关
│   ├── system-prompt.md                        # AI 助手主系统提示词
│   ├── system-prompt-README.md                # 系统提示词使用指南
│   └── quick-reference.md                     # 快速参考指南
│
├── templates/                                  # 📝 模板文件
│   ├── solution-design-template.md            # 方案设计思维模板
│   └── rules-checklist.md                     # 规则检查清单
│
├── enforcement/                                # 🚨 强制执行相关
│   └── rule-enforcement-prompt.md             # 规则强制执行提示词
│
├── 01-coding-standards/                       # 编码规范
│   ├── property-notification.mdc              # rule-001: 属性更改通知统一规范
│   ├── naming-conventions.mdc                 # rule-002: 命名规范
│   └── logging-system.mdc                     # rule-003: 日志系统使用规范
│
├── 02-development-process/                     # 开发流程
│   ├── solution-design.mdc                    # rule-004: 方案设计要求
│   ├── prototype-design-clean-code.mdc        # rule-008: 原型设计期代码纯净原则
│   ├── solution-system-implementation.mdc    # rule-010: 方案系统实现规范
│   ├── temp-file-cleanup.mdc                  # rule-011: 临时文件自动清理规则
│   └── parameter-system-constraints.mdc       # rule-012: 参数系统约束条件
│
├── 03-quality-control/                        # 质量控制
│   └── README.md                              # 质量控制规则说明
│
└── 04-workflow-guidance/                      # 工作流指导
    ├── documentation-policy.mdc               # 文档写入限制
    └── implementation-approval.mdc            # 方案实施限制
```

---

## 📁 文件分类说明

### 1. 系统提示词（system/）

**用途**: AI 助手的系统提示词和相关文档

| 文件 | 说明 | 优先级 |
|------|------|--------|
| `system-prompt.md` | AI 助手主系统提示词（必须阅读） | 🔴 Critical |
| `system-prompt-README.md` | 系统提示词详细使用指南 | 🟠 High |
| `quick-reference.md` | 快速参考指南，常见违规示例 | 🟠 High |

---

### 2. 模板文件（templates/）

**用途**: 方案设计和规则检查的模板

| 文件 | 说明 | 使用场景 |
|------|------|---------|
| `solution-design-template.md` | 方案设计思维模板 | AI 助手设计方案时使用 |
| `rules-checklist.md` | 完整的规则检查清单 | 代码审查和自我检查 |

---

### 3. 强制执行（enforcement/）

**用途**: 规则强制执行相关配置

| 文件 | 说明 |
|------|------|
| `rule-enforcement-prompt.md` | 规则强制执行提示词 |

---

### 4. 编码规范（01-coding-standards/）

**用途**: 编码规范相关规则

| 规则ID | 文件 | 核心要求 | 优先级 |
|--------|------|---------|--------|
| rule-001 | `property-notification.mdc` | 继承 ObservableObject，使用 SetProperty | 🔴 Critical |
| rule-002 | `naming-conventions.mdc` | PascalCase 命名，禁止缩写 | 🟠 High |
| rule-003 | `logging-system.mdc` | 使用 LogInfo()，禁止 Debug.WriteLine() | 🟠 High |

---

### 5. 开发流程（02-development-process/）

**用途**: 开发流程相关规则

| 规则ID | 文件 | 核心要求 | 优先级 |
|--------|------|---------|--------|
| rule-004 | `solution-design.mdc` | 方案设计要求，考虑整体架构 | 🟠 High |
| rule-008 | `prototype-design-clean-code.mdc` | 直接删除旧代码，禁止 [Obsolete] | 🔴 Critical |
| rule-010 | `solution-system-implementation.mdc` | 使用 System.Text.Json，禁止 Newtonsoft.Json | 🔴 Critical |
| rule-011 | `temp-file-cleanup.mdc` | 临时文件自动清理 | 🟠 High |
| rule-012 | `parameter-system-constraints.mdc` | 参数系统约束条件 | 🟠 High |

---

### 6. 质量控制（03-quality-control/）

**用途**: 质量控制相关规则

| 文件 | 说明 |
|------|------|
| `README.md` | 质量控制规则说明 |

---

### 7. 工作流指导（04-workflow-guidance/）

**用途**: 工作流指导相关规则

| 文件 | 说明 |
|------|------|
| `documentation-policy.mdc` | 文档写入限制 |
| `implementation-approval.mdc` | 方案实施限制 |

---

## 🔍 快速导航

### 按用途查找

#### 🤖 对于 AI 助手
- [系统提示词](./system/system-prompt.md) - 主系统提示词
- [快速参考](./system/quick-reference.md) - 常见违规示例
- [方案设计模板](./templates/solution-design-template.md) - 方案设计指南

#### 👨‍💻 对于开发人员
- [规则索引](./rules-index.md) - 所有规则索引
- [规则检查清单](./templates/rules-checklist.md) - 代码检查清单
- [快速参考](./system/quick-reference.md) - 快速参考指南

#### 📝 代码审查时
- [规则检查清单](./templates/rules-checklist.md) - 逐项检查
- [编码规范](./01-coding-standards/) - 编码规范
- [开发流程](./02-development-process/) - 开发流程

---

### 按规则ID查找

| 规则ID | 规则名称 | 优先级 | 文件路径 |
|--------|---------|--------|---------|
| rule-001 | 属性更改通知统一规范 | 🔴 Critical | [01-coding-standards/property-notification.mdc](./01-coding-standards/property-notification.mdc) |
| rule-002 | 命名规范 | 🟠 High | [01-coding-standards/naming-conventions.mdc](./01-coding-standards/naming-conventions.mdc) |
| rule-003 | 日志系统使用规范 | 🟠 High | [01-coding-standards/logging-system.mdc](./01-coding-standards/logging-system.mdc) |
| rule-004 | 方案设计要求 | 🟠 High | [02-development-process/solution-design.mdc](./02-development-process/solution-design.mdc) |
| rule-008 | 原型设计期代码纯净原则 | 🔴 Critical | [02-development-process/prototype-design-clean-code.mdc](./02-development-process/prototype-design-clean-code.mdc) |
| rule-010 | 方案系统实现规范 | 🔴 Critical | [02-development-process/solution-system-implementation.mdc](./02-development-process/solution-system-implementation.mdc) |
| rule-011 | 临时文件自动清理规则 | 🟠 High | [02-development-process/temp-file-cleanup.mdc](./02-development-process/temp-file-cleanup.mdc) |
| rule-012 | 参数系统约束条件 | 🟠 High | [02-development-process/parameter-system-constraints.mdc](./02-development-process/parameter-system-constraints.mdc) |

---

## 🎯 使用建议

### 1. 首次使用

1. 阅读 [系统提示词使用指南](./system/system-prompt-README.md)
2. 查看 [快速参考指南](./system/quick-reference.md)
3. 了解 [规则索引](./rules-index.md)

### 2. 代码审查时

1. 打开 [规则检查清单](./templates/rules-checklist.md)
2. 逐项检查代码
3. 根据违规情况查阅具体规则

### 3. 设计方案时

1. 使用 [方案设计模板](./templates/solution-design-template.md)
2. 参考 [规则强制执行提示词](./enforcement/rule-enforcement-prompt.md)
3. 使用 [规则检查清单](./templates/rules-checklist.md) 验证

---

## 📝 整理说明

### 整理目标

将所有规则文件统一到 `.codebuddy/rules` 目录下，建立清晰的目录结构。

### 整理前

```
.codebuddy/
├── system-prompt.md                           # 根目录
├── system-prompt-README.md                    # 根目录
├── quick-reference.md                         # 根目录
├── rules-checklist.md                         # 根目录
├── solution-design-template.md                # 根目录
├── CHANGELOG.md                               # 根目录
└── rules/                                     # 规则目录
    ├── rule-enforcement-prompt.md            # 混在根目录
    └── ...
```

### 整理后

```
.codebuddy/
└── rules/                                     # 统一目录
    ├── system/                                # 系统提示词
    ├── templates/                             # 模板文件
    ├── enforcement/                           # 强制执行
    ├── 01-coding-standards/                  # 编码规范
    ├── 02-development-process/                # 开发流程
    ├── 03-quality-control/                   # 质量控制
    └── 04-workflow-guidance/                 # 工作流指导
```

### 整理原则

1. **统一性**: 所有规则文件在 `rules/` 目录下
2. **分类清晰**: 按用途分类到不同子目录
3. **易于查找**: 通过 README 和索引快速定位
4. **向后兼容**: 更新相关文件的引用路径

---

## 🔄 引用路径更新

需要更新引用的文件：

- [ ] `.codebuddy/README.md` - 更新规则文件路径
- [ ] `.codebuddy/config/system-prompt-loader.yaml` - 更新系统提示词路径
- [ ] `.codebuddy/rules/system/system-prompt.md` - 更新规则文档索引
- [ ] `.codebuddy/rules/system/system-prompt-README.md` - 更新文档索引
- [ ] `.codebuddy/rules/system/quick-reference.md` - 更新文档链接

---

## 📞 联系方式

如有问题或建议，请联系：

- **维护者**: SunEyeVision Team
- **最后更新**: 2026-03-24
- **版本**: 1.0

---

**状态**: ✅ 整理完成
**生效时间**: 2026-03-24
