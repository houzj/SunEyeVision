# SunEyeVision 项目规则索引

本目录包含 SunEyeVision 项目的所有开发规范和编码规则。

## 📋 规则列表

### 1. 日志系统使用规范
**文件**: `日志系统使用规范.mdc`  
**类型**: 总是应用 (Always)  
**启用状态**: ✅ 已启用  
**创建日期**: 2026-03-08  

**规则概述**:
- 禁止使用 `System.Diagnostics.Debug.WriteLine()`
- 禁止使用 `System.Diagnostics.Trace.WriteLine()`
- 禁止使用 `Console.WriteLine()`
- 所有日志必须通过项目日志系统输出到日志显示器

**适用范围**: 全项目

**阅读规则**: [查看详情](./日志系统使用规范.mdc)

---

### 2. 属性更改通知统一规范
**文件**: `PropertyNotificationStandard.mdc`  
**类型**: 总是应用 (Always)  
**启用状态**: ✅ 已启用  
**创建日期**: 2026-03-09  

**规则概述**:
- 所有需要属性通知的类必须继承 ObservableObject 或其派生类
- 使用 SetProperty 方法替代手动属性设置
- 禁止直接实现 INotifyPropertyChanged（除非特殊需求）
- 需要同时实现多个接口时的处理规范

**适用范围**: 全项目

**阅读规则**: [查看详情](./PropertyNotificationStandard.mdc)

---

## 📊 规则统计

| 规则类型 | 数量 |
|---------|------|
| 总是应用 (Always) | 2 |
| 手动应用 (Manual) | 0 |
| 请求时应用 (Requested) | 0 |
| **总计** | **2** |

---

## 🎯 使用指南

### 代码审查时
1. 查阅相关的规则文件
2. 对照规则中的代码审查检查清单
3. 参考正确示例和错误示例
4. 确保新代码符合规范

### 创建新规则时
1. 确定规则类型（Always/Manual/Requested）
2. 编写规则内容，包含：
   - 清晰的规则说明
   - 正确代码示例 ✅
   - 错误代码示例 ❌
   - 代码审查检查清单
3. 更新本索引文件
4. 文件命名规范：`规则名称.mdc` 或 `中文规则名称.mdc`

---

## 🔄 规则管理

### 添加新规则
1. 在 `create_rule` 工具中创建规则
2. 规则文件会自动保存到本目录
3. 在本索引文件中添加规则说明

### 更新规则
1. 读取现有规则文件
2. 使用 `update_rule` 工具更新内容
3. 更新索引文件中的描述

### 禁用规则
在规则文件的 frontmatter 中设置 `enabled: false`

---

## 📚 相关文档

- 项目架构文档: `docs/`
- API 文档: `docs/api/`
- 开发指南: `docs/guides/`
- 迁移总结: `docs/property_notification_migration_summary.md`

---

## 📞 反馈与建议

如果需要：
- 添加新规则
- 修改现有规则
- 报告规则冲突

请联系项目维护者或通过 CodeBuddy 工具进行操作。

---

**最后更新**: 2026-03-09  
**规则总数**: 2  
**维护者**: SunEyeVision Team
