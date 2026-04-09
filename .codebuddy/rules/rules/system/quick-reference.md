# 🚀 AI 助手快速参考指南

> **版本**: 1.0 | **更新**: 2026-03-24 | **状态**: ✅ 已启用

---

## 🤖 AI 助手系统提示词 - 已启用！

**AI 助手现在会自动遵循项目规则！**

### 📋 强制执行流程

```
收到请求 → 理解问题 → 检索规则 → 执行检查 → 设计方案 → 自我审查 → 输出方案
```

### 🔴 核心规则（必须遵守）

| 规则 | 核心要求 | 优先级 |
|------|---------|--------|
| **rule-001** | 继承 `ObservableObject`，使用 `SetProperty` | 🔴 Critical |
| **rule-003** | 使用 `LogInfo()`，禁止 `Debug.WriteLine()` | 🟠 High |
| **rule-008** | 直接删除旧代码，禁止 `[Obsolete]` | 🔴 Critical |
| **rule-010** | 使用 `System.Text.Json`，禁止 `Newtonsoft.Json` | 🔴 Critical |
| **rule-002** | PascalCase 命名，禁止缩写 | 🟠 High |

---

## 🚨 常见违规示例

### ❌ 日志系统违规

```csharp
// ❌ 错误：违反 rule-003
System.Diagnostics.Debug.WriteLine("调试信息");
Console.WriteLine("调试信息");
AddLogToUI("日志信息");  // 绕过了日志系统

// ✅ 正确：遵循 rule-003
LogInfo("信息日志");
LogWarning("警告日志");
LogError("错误日志");
```

### ❌ 属性通知违规

```csharp
// ❌ 错误：违反 rule-001
public class MyViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    // 手动实现
}

// ✅ 正确：遵循 rule-001
public class MyViewModel : ObservableObject
{
    private int _value;
    public int Value
    {
        get => _value;
        set => SetProperty(ref _value, value, "数值");
    }
}
```

### ❌ JSON 序列化违规

```csharp
// ❌ 错误：违反 rule-010
using Newtonsoft.Json;
var json = JsonConvert.SerializeObject(obj);

// ✅ 正确：遵循 rule-010
using System.Text.Json;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MyType), "MyType")]
public class MyType
{
    public string Name { get; set; }  // JSON: "Name" (PascalCase)
}
```

### ❌ 代码纯净度违规

```csharp
// ❌ 错误：违反 rule-008
[Obsolete("使用 NewMethod 替代")]
public void OldMethod() { }

// ❌ 错误：违反 rule-008
// 旧代码
// public void OldMethod() { }

// ✅ 正确：遵循 rule-008
// 直接删除，不保留旧代码
```

---

## 📋 完整规则检查清单

### ✅ 代码规范
- [ ] 继承 `ObservableObject`？
- [ ] 使用 `SetProperty` 方法？
- [ ] PascalCase 命名？
- [ ] `_camelCase` 私有字段？
- [ ] Is/Has/Can 布尔前缀？

### ✅ 日志系统
- [ ] 使用 `LogInfo()` / `LogError()`？
- [ ] 避免 `Debug.WriteLine()`？
- [ ] 避免 `Console.WriteLine()`？
- [ ] 日志级别恰当？

### ✅ 方案系统
- [ ] 使用 `System.Text.Json`？
- [ ] 使用 `[JsonPolymorphic]`？
- [ ] PascalCase JSON 命名？
- [ ] 避免 `Newtonsoft.Json`？

### ✅ 代码纯净度
- [ ] 删除所有旧代码？
- [ ] 删除注释代码？
- [ ] 删除 `[Obsolete]`？
- [ ] 避免条件编译？

---

## 🎯 方案设计模板

### 必须包含的内容：

```markdown
## 🎯 问题分析
- 用户需求
- 涉及文件
- 问题层级
- 相关规则

## 🚨 违反规则分析
- 列出违反的规则
- 分析影响
- 确定正确做法

## 💡 解决方案
- 方案概述
- 涉及的规则（明确标注）
- 技术方案（详细步骤）
- 实施步骤
- 验证清单
- 风险控制

## ✅ 自我审查
- 规则遵守审查
- 方案质量审查
- 代码质量审查
```

---

## 📚 相关文档

| 文档 | 说明 |
|------|------|
| [系统提示词](./system-prompt.md) | AI 助手主系统提示词 |
| [系统提示词指南](./system-prompt-README.md) | 详细使用指南 |
| [规则强制执行提示词](./rules/rule-enforcement-prompt.md) | 规则强制执行 |
| [方案设计模板](./solution-design-template.md) | 方案设计思维模板 |
| [规则检查清单](./rules-checklist.md) | 完整检查清单 |
| [规则索引](./rules/rules-index.md) | 所有规则索引 |

---

## 🔧 使用工具

### 读取规则

```bash
read_rules ruleNames="logging-system"
```

### 搜索代码

```bash
# 搜索日志违规
search_content pattern="Debug.WriteLine" directory="src/"

# 搜索属性通知
search_content pattern="SetProperty" directory="src/"
```

### 读取文件

```bash
read_file filePath="src/Path/To/File.cs"
```

---

## 🚨 违规处理流程

```
发现违规 → 立即停止 → 识别违规 → 提供修正 → 重新检查
```

### 示例：

```markdown
❌ 发现违规：
- Rule-003: 使用了 System.Diagnostics.Debug.WriteLine()

✅ 正确做法：
使用 LogInfo() 替代：
- ViewModel 层: LogInfo("信息日志");
- Service 层: _logger.Log(LogLevel.Info, "信息", "来源");

正在重新生成方案...
```

---

## 💡 最佳实践

### ✅ 必须做

- 每次生成方案前，必须先执行规则检查清单
- 明确标注每个步骤遵循的规则
- 发现违规时，立即停止并修正
- 提供详细的方案，包括验证清单

### ❌ 不能做

- 不能跳过规则检查清单
- 不能违反任何项目规则
- 不能点对点解决问题，不考虑整体架构
- 不能使用已被禁止的做法

---

## 🎯 目标

通过系统提示词配置，实现：

1. **强制执行**：规则不再是可选的建议，而是必须遵守的要求
2. **自动检查**：通过检查清单自动验证，减少遗漏
3. **及时修正**：发现违规立即修正，避免累积
4. **质量保障**：确保代码质量和项目标准一致性

---

**最后更新**: 2026-03-24
**维护者**: SunEyeVision Team
**版本**: 1.0
**状态**: ✅ 已启用
