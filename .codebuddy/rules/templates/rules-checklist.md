# 📋 规则检查清单

> **目标**: 确保所有方案设计和代码修改都遵循项目规则
> **生效模式**: 方案生成前必须执行此清单
> **最后更新**: 2026-03-24

---

## 🔍 使用说明

### 何时使用此清单？

在以下场景中，**必须**执行此清单：
- ✅ 设计新的功能方案
- ✅ 修改现有代码
- ✅ 重构代码结构
- ✅ 创建新的类或接口
- ✅ 修改日志输出
- ✅ 修改属性通知逻辑

### 如何使用此清单？

1. **方案生成前**: 根据任务类型，找到相关的规则
2. **逐项检查**: 对照每个规则的检查项，确认是否满足
3. **标记状态**: 使用 `[ ]` 标记未完成，`[x]` 标记已完成
4. **记录例外**: 如果有特殊情况，记录原因和批准人
5. **提交审查**: 检查完成后，提交给审查人员

---

## 🔴 Critical 规则（必须检查）

### rule-001: 属性更改通知统一规范

**规则ID**: rule-001  
**优先级**: 🔴 Critical  
**适用范围**: 全项目  
**相关文件**: `.codebuddy/rules/01-coding-standards/property-notification.mdc`

#### 核心原则
统一属性更改通知机制，消除重复代码，提高代码可维护性和性能。所有需要属性通知的类必须继承 `ObservableObject` 基类，使用 `SetProperty` 方法进行属性设置。

#### 检查项
- [ ] **所有需要属性通知的类都继承了 ObservableObject 或其派生类**
  - Plugin.SDK 层：直接继承 `Plugin.SDK.Models.ObservableObject`
  - UI 层 ViewModel：继承 `UI.ViewModels.ViewModelBase`（已继承 ObservableObject）
  - UI 层 Model：继承 `Plugin.SDK.Models.ObservableObject` 或 `UI.ViewModels.ViewModelBase`

- [ ] **属性使用 SetProperty 方法而不是手动实现**
  - ✅ 正确：`set => SetProperty(ref _field, value);`
  - ❌ 错误：手动实现 `_field = value; OnPropertyChanged(nameof(Prop));`

- [ ] **没有重复的 INotifyPropertyChanged 实现**
  - ObservableObject 已提供实现，不应重复实现

- [ ] **如果直接实现了 INotifyPropertyChanged，有详细注释说明原因**
  - 允许的特殊情况：
    1. 需要支持属性变更批处理（如 WorkflowNode）
    2. 需要扩展事件（如 PropertyChanging）
    3. 有特殊的性能优化需求

- [ ] **正确使用 displayName 参数记录日志（对用户可见的属性）**
  - ✅ 正确：`SetProperty(ref _threshold, value, "阈值");`
  - ❌ 错误：对用户可见属性不带 displayName

- [ ] **对内部属性不记录日志（不带 displayName 参数）**
  - ✅ 正确：`SetProperty(ref _isVisible, value);`

#### 常见错误示例

```csharp
// ❌ 错误1：直接实现 INotifyPropertyChanged
public class MyViewModel : INotifyPropertyChanged { }

// ✅ 正确1：继承 ObservableObject
public class MyViewModel : ObservableObject { }

// ❌ 错误2：手动实现属性设置
private int _value;
public int Value { get; set; }
private void SetValue(int value)
{
    if (_value != value)
    {
        _value = value;
        OnPropertyChanged(nameof(Value));
    }
}

// ✅ 正确2：使用 SetProperty
private int _value;
public int Value
{
    get => _value;
    set => SetProperty(ref _value, value);
}
```

---

### rule-002: 命名规范

**规则ID**: rule-002  
**优先级**: 🟠 High  
**适用范围**: 全项目  
**相关文件**: `.codebuddy/rules/01-coding-standards/naming-conventions.mdc`

#### 核心原则
命名需要符合视觉软件的风格，不要使用互联网风格。统一的命名规范可以提高代码可读性、可维护性，并降低学习成本。

#### 检查项
- [ ] **类名使用 PascalCase（名词）**
  - ✅ 正确：`WorkflowEngine`, `ImageProcessor`, `RegionEditor`
  - ❌ 错误：`ProcessWorkflow`（动词）、`ImgProc`（缩写）、`RegionUtil`（缩写）

- [ ] **接口命名（I + PascalCase）**
  - ✅ 正确：`IPluginManager`, `IImageProcessor`
  - ❌ 错误：`PluginManager`, `I_plugin_manager`

- [ ] **方法名使用 PascalCase（动词）**
  - ✅ 正确：`ProcessImage()`, `SaveWorkflow()`, `LoadConfiguration()`
  - ❌ 错误：`processImage()`（小驼峰）、`Save_WF()`（下划线）、`SaveCfg()`（缩写）

- [ ] **属性名使用 PascalCase**
  - ✅ 正确：`UserName`, `ImageWidth`, `IsEnabled`, `BackgroundColor`
  - ❌ 错误：`userName`, `img_width`, `enabled`

- [ ] **变量名使用 camelCase**
  - ✅ 正确：`userName`, `imageWidth`, `isEnabled`
  - ❌ 错误：`_user_name`（下划线）、`_img_w`（缩写）、`IsEnabled`（大写开头）

- [ ] **私有字段前缀下划线**
  - ✅ 正确：`_userName`, `_imageWidth`, `_isVisible`
  - ❌ 错误：`userName`（无下划线）、`__threshold`（双下划线）、`m_threshold`（m_前缀）

- [ ] **常量使用 UPPER_CASE**
  - ✅ 正确：`MAX_RETRY_COUNT`, `DEFAULT_IMAGE_FORMAT`, `MIN_THRESHOLD`
  - ❌ 错误：`MaxRetryCount`, `defaultImageFormat`

- [ ] **布尔值前缀 Is/Has/Can**
  - ✅ 正确：`IsEnabled`, `IsVisible`, `HasChildren`, `CanProcess`, `IsProcessing`
  - ❌ 错误：`Enabled`, `Visible`, `Process`（缺少前缀）

- [ ] **事件使用过去式**
  - ✅ 正确：`PropertyChanged`, `Loaded`, `Saved`, `ValueChanged`
  - ❌ 错误：`PropertyChanging`, `Load`, `Save`

- [ ] **枚举使用单数**
  - ✅ 正确：`LogLevel`, `NodeStatus`
  - ❌ 错误：`LogLevels`, `node_status`, `NStatus`

- [ ] **命名空间使用 PascalCase，点分隔**
  - ✅ 正确：`SunEyeVision.UI.ViewModels`, `SunEyeVision.Plugin.SDK`
  - ❌ 错误：`SunEyeVision.UI_ViewModels`, `SunEyeVision.Plugin.Sdk`

- [ ] **避免使用缩写**
  - ✅ 正确：`ImageProcessor`, `ConfigurationManager`, `WorkflowExecutionEngine`
  - ❌ 错误：`ImgProc`, `CfgMgr`, `WfExecEngine`, `PluginLdr`

---

### rule-003: 日志系统使用规范 ⚠️ **最高优先级**

**规则ID**: rule-003  
**优先级**: 🟠 High  
**适用范围**: 全项目  
**相关文件**: `.codebuddy/rules/01-coding-standards/logging-system.mdc`

#### 核心原则
统一日志输出机制，禁止使用 `System.Diagnostics.Debug.WriteLine()`、`System.Diagnostics.Trace.WriteLine()` 和 `Console.WriteLine()`。所有日志信息必须通过项目的日志系统输出到日志显示器。

#### 检查项
- [ ] **❌ 没有使用 System.Diagnostics.Debug.WriteLine()**
  - ❌ 错误：`System.Diagnostics.Debug.WriteLine("调试信息");`

- [ ] **❌ 没有使用 System.Diagnostics.Trace.WriteLine()**
  - ❌ 错误：`System.Diagnostics.Trace.WriteLine("调试信息");`

- [ ] **❌ 没有使用 Console.WriteLine()**
  - ❌ 错误：`Console.WriteLine("调试信息");`

- [ ] **❌ 没有直接操作 UI 日志显示**
  - ❌ 错误：`AddLogToUI("错误信息");`
  - ❌ 错误：`_viewModel.LogText += "错误信息\n";`
  - ❌ 错误：直接修改 UI 控件的日志属性

- [ ] **✅ ViewModel 层使用 LogInfo/LogError/LogSuccess/LogWarning**
  - ✅ 正确：
    ```csharp
    LogInfo("信息日志");
    LogSuccess("成功日志");
    LogWarning("警告日志");
    LogError("错误日志");
    LogFatal("严重错误");
    ```

- [ ] **✅ Service 层使用 _logger.Log()**
  - ✅ 正确：
    ```csharp
    _logger.Log(LogLevel.Info, "消息", "来源");
    _logger.Log(LogLevel.Error, "错误信息", "来源", exception);
    ```

- [ ] **✅ UI Code-Behind 通过 ViewModel 记录日志**
  - ✅ 正确：`_viewModel.LogError("错误信息");`
  - ✅ 正确：`_viewModel.LogWarning("警告信息");`

- [ ] **✅ 日志级别使用恰当（Info/Success/Warning/Error/Fatal）**
  - **Info**: 一般信息（如 "开始处理图像"、"加载配置文件"）
  - **Success**: 成功操作（如 "插件加载成功"、"工作流执行完成"）
  - **Warning**: 警告信息（如 "图像格式不支持"、"参数超出建议范围"）
  - **Error**: 错误信息（如 "插件加载失败"、"参数验证失败"）
  - **Fatal**: 严重错误（如 "应用程序崩溃"、"关键服务不可用"）

- [ ] **✅ 日志信息包含足够的上下文信息**
  - ✅ 正确：`LogInfo($"开始处理图像: {imagePath}, 尺寸: {width}x{height}");`
  - ❌ 错误：`LogInfo("处理图像");`

- [ ] **✅ 异常信息包含详细的错误描述和堆栈信息**
  - ✅ 正确：`LogError($"处理失败: {ex.Message}", ex);`
  - ❌ 错误：`LogError("处理失败");`

#### 常见错误示例

```csharp
// ❌ 错误1：使用 Debug.WriteLine
System.Diagnostics.Debug.WriteLine("调试信息");

// ❌ 错误2：使用 Trace.WriteLine
System.Diagnostics.Trace.WriteLine("调试信息");

// ❌ 错误3：使用 Console.WriteLine
Console.WriteLine("调试信息");

// ❌ 错误4：直接操作 UI 日志
AddLogToUI("错误信息");
_viewModel.LogText += "错误信息\n";

// ✅ 正确1：ViewModel 层
LogInfo("信息日志");
LogSuccess("成功日志");
LogWarning("警告日志");
LogError("错误日志");
LogFatal("严重错误");

// ✅ 正确2：Service 层
_logger.Log(LogLevel.Info, "消息", "来源");
_logger.Log(LogLevel.Error, "错误信息", "来源", exception);

// ✅ 正确3：UI Code-Behind
_viewModel.LogError("错误信息");
_viewModel.LogWarning("警告信息");
```

---

### rule-008: 原型设计期代码纯净原则

**规则ID**: rule-008  
**优先级**: 🔴 Critical  
**适用范围**: 原型设计阶段  
**相关文件**: `.codebuddy/rules/02-development-process/prototype-design-clean-principle.mdc`

#### 核心原则
原型设计期的核心原则：**不考虑向后兼容，保持代码纯净**。原型阶段是快速迭代、快速验证的阶段，不应该为旧代码保留兼容性，避免代码库臃肿和认知负担。

#### 检查项
- [ ] **不考虑向后兼容**
  - 原型阶段无需为旧 API 保留兼容性
  - 直接删除旧代码，不使用 `[Obsolete]` 标记

- [ ] **保持代码纯净**
  - 该重构就重构，该重写就重写，该删除就删除
  - 遇到不合理的代码立即重构，不拖延

- [ ] **不保留旧代码**
  - 直接删除，不保留旧代码路径
  - 禁止使用条件编译（如 `#if DEBUG`、`#if LEGACY`）
  - 禁止保留注释掉的代码

- [ ] **避免干扰**
  - 清晰的代码库，减少后续开发的认知负担
  - 删除无用的类、方法、注释

- [ ] **最优方案优先**
  - 选择最优技术方案，不为兼容而妥协
  - 优先考虑可维护性、可扩展性、性能

- [ ] **直接迁移**
  - 如需迁移，一步到位，不保留旧代码路径
  - 新旧切换直接完成，不保留过渡代码

#### 常见错误示例

```csharp
// ❌ 错误1：使用 [Obsolete] 标记
[Obsolete("使用 NewMethod 替代")]
public void OldMethod() { }

// ✅ 正确1：直接删除旧方法
// 删除 OldMethod，保留 NewMethod

// ❌ 错误2：使用条件编译保留旧代码
#if LEGACY
    public class OldModel { }
#else
    public class NewModel { }
#endif

// ✅ 正确2：直接删除旧代码
// 只保留 NewModel

// ❌ 错误3：注释掉代码
// public void OldMethod()
// {
//     // 旧实现
// }

// ✅ 正确3：删除注释掉的代码
public void NewMethod()
{
    // 新实现
}
```

---

## 🟠 High 规则（应该检查）

### rule-004: 方案设计要求

**规则ID**: rule-004  
**优先级**: 🟠 High  
**适用范围**: 方案设计和实现  
**相关文件**: `.codebuddy/rules/02-development-process/solution-design.mdc`

#### 核心原则
生成方案时需要尽量根据软件的完整上下文，不要点对点的仅仅解决当前问题。方案设计应考虑整体架构、现有基础设施、可维护性和扩展性。

#### 检查项
- [ ] **分析了涉及的模块和依赖关系**
  - 识别所有受影响的模块
  - 分析模块之间的依赖关系

- [ ] **考虑了对现有功能的影响**
  - 评估变更对现有功能的影响
  - 确保不会破坏现有功能

- [ ] **评估了性能和可维护性影响**
  - 评估性能影响
  - 评估可维护性影响

- [ ] **识别了潜在的技术风险**
  - 识别可能的技术风险
  - 提供风险缓解措施

- [ ] **优先使用项目现有的基础设施**
  - 复用现有的框架和工具
  - 避免重复造轮子

- [ ] **遵循已建立的架构模式**
  - 遵循项目的架构分层
  - 遵循项目的设计模式

- [ ] **考虑了后续扩展性**
  - 为未来扩展预留接口
  - 设计灵活的扩展点

- [ ] **提供了清晰的实现步骤**
  - 列出详细的实施步骤
  - 明确每个步骤的优先级

- [ ] **考虑添加单元测试**
  - 为新功能添加单元测试
  - 确保测试覆盖率

- [ ] **考虑添加必要的注释和文档**
  - 添加必要的代码注释
  - 更新相关文档

- [ ] **考虑代码的可测试性**
  - 设计可测试的代码
  - 避免紧密耦合

---

### rule-010: 方案系统实现规范

**规则ID**: rule-010  
**优先级**: 🔴 Critical  
**适用范围**: 方案系统设计和实现  
**相关文件**: `.codebuddy/rules/02-development-process/solution-system-implementation.mdc`

#### 核心原则
方案系统实现必须遵循最优和最合理的原则。优先使用 System.Text.Json 的多态序列化能力，在特殊场景（如自定义 JsonConverter、处理特殊类型）下允许使用 Dictionary 转换层作为辅助工具。

#### 检查项
- [ ] **使用 System.Text.Json 而非 Newtonsoft.Json**
  - ✅ 正确：`JsonSerializer.Serialize(solution, jsonOptions);`
  - ❌ 错误：`JsonConvert.SerializeObject(solution, new JsonSerializerSettings());`

- [ ] **使用 [JsonPolymorphic] 特性支持多态**
  - ✅ 正确：
    ```csharp
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(GenericToolParameters), "Generic")]
    public abstract class ToolParameters : ObservableObject { }
    ```

- [ ] **派生类添加 [JsonDerivedType] 特性**
  - ✅ 正确：
    ```csharp
    [JsonDerivedType(typeof(ThresholdParameters), "Threshold")]
    public class ThresholdParameters : ToolParameters { }
    ```

- [ ] **方案层（Solution）不使用 ToSerializableDictionary/LoadFromDictionary**
  - ❌ 禁止：在业务逻辑中直接调用转换层
  - ✅ 正确：直接序列化对象实例

- [ ] **Dictionary 转换层方法应为 internal 或 private 访问级别**
  - ❌ 禁止：暴露给业务层（public）
  - ✅ 正确：限制访问范围（internal/private）

- [ ] **仅在自定义 JsonConverter 或处理特殊类型时使用 Dictionary 转换层**
  - ✅ 允许：在 WorkflowJsonConverter 内部使用
  - ✅ 允许：处理 OpenCvSharp.Point/Rect/Size 等特殊类型

- [ ] **JSON 格式直观易读**
  - ✅ 正确：清晰的嵌套结构
  - ❌ 错误：复杂的嵌套结构

- [ ] **使用 PascalCase 命名（符合视觉软件行业标准）**
  - ✅ 正确：`"Id"`, `"Name"`, `"Workflows"`
  - ❌ 错误：`"id"`, `"name"`, `"workflows"`

- [ ] **包含类型信息（$type 字段）**
  - ✅ 正确：`"$type": "Threshold"`
  - ❌ 错误：没有类型信息

---

### rule-012: 参数系统约束条件

**规则ID**: rule-012  
**优先级**: 🟠 High  
**适用范围**: 参数系统全链路开发  
**相关文件**: `.codebuddy/rules/02-development-process/parameter-system-constraints.mdc`

#### 核心原则
参数系统涉及UI层、Workflow层、Plugin.SDK层的复杂交互。为确保系统稳定性和可维护性,必须遵循以下约束条件。

#### 检查项
- [ ] **UI层使用 Dictionary<string, object> 存储参数**
  - ✅ 正确：`public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();`
  - 原因：便于绑定到属性面板，与 PropertyPanelViewModel 集成良好

- [ ] **UI层添加 ParametersTypeName 属性**
  - ✅ 正确：
    ```csharp
    public string? ParametersTypeName { get; set; }
    ```
  - 原因：Dictionary 只存储值，不存储类型，需要类型信息才能创建强类型参数

- [ ] **参数转换方法支持强类型创建**
  - ✅ 正确：根据 ParametersTypeName 创建强类型参数
  - ❌ 错误：总是创建 GenericToolParameters

- [ ] **参数转换失败时回退到 GenericToolParameters**
  - ✅ 正确：创建强类型参数失败时，使用 GenericToolParameters

- [ ] **保持工具注册机制不变**
  - ✅ 正确：保持 ToolRegistry 的注册机制
  - 原因：现有注册机制设计良好，集中了工具元数据管理

- [ ] **使用类型比较而非字符串匹配**
  - ✅ 正确：`iface.GetGenericTypeDefinition() == typeof(IToolPlugin<,>)`
  - ❌ 错误：`iface.GetGenericTypeDefinition().Name.StartsWith("IToolPlugin")`

- [ ] **添加参数类型提取的日志输出**
  - ✅ 正确：记录参数类型提取的成功和失败信息

---

### rule-011: 临时文件自动清理规则

**规则ID**: rule-011  
**优先级**: 🟠 High  
**适用范围**: 全项目  
**相关文件**: `.codebuddy/rules/temporary-file-cleanup.mdc`

#### 核心原则
所有编译脚本、开发工具产生的临时文件必须在完成后自动删除，避免项目文件夹污染。

#### 检查项
- [ ] **脚本结束时删除所有临时文件**
  - ✅ 正确：使用 Try-Finally 确保清理
  - ❌ 错误：临时文件留在项目目录

- [ ] **使用系统临时目录（%TEMP% 或 $env:TEMP）**
  - ✅ 正确：`set TEMP_FILE=%TEMP%\myapp_temp_%RANDOM%.txt`
  - ❌ 错误：在项目目录创建临时文件

- [ ] **临时文件使用随机名称**
  - ✅ 正确：使用 %RANDOM% 或 [Guid]::NewGuid()
  - ❌ 错误：使用固定的文件名

- [ ] **使用 Try-Finally（PowerShell）或错误处理确保清理**
  - ✅ 正确：
    ```powershell
    try {
        # 执行操作
    }
    finally {
        # 确保临时文件被删除
        if (Test-Path $tempFile) {
            Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
        }
    }
    ```

- [ ] **日志文件不被提交到版本控制**
  - ✅ 正确：日志文件在 .gitignore 中

- [ ] **覆盖所有临时文件模式**
  - *.tmp
  - *.temp
  - temp_*
  - build_*.txt
  - log_*.txt

- [ ] **避免使用无意义的命名**
  - ✅ 正确：`build_*.txt`, `debug_*.txt`, `test_*.cs`
  - ❌ 错误：`temp1.txt`, `temp2.txt`

---

## 🟡 Medium 规则（建议检查）

### rule-007: 质量控制规则

**规则ID**: rule-007  
**优先级**: 🟡 Medium  
**适用范围**: 全项目  
**相关文件**: `.codebuddy/rules/03-quality-control/README.md`

#### 核心原则
质量控制规则包括性能优化指导、错误处理规范、测试要求等内容，确保代码质量和系统稳定性。

#### 检查项
- [ ] **性能优化指导**
  - 性能基准测试要求
  - 常见性能问题及优化方案
  - 性能监控和分析工具使用

- [ ] **错误处理规范**
  - 异常处理最佳实践
  - 错误日志记录规范
  - 用户友好的错误提示

- [ ] **测试要求**
  - 单元测试覆盖率要求
  - 集成测试策略
  - 测试命名规范

---

## 📋 方案设计模板

### 方案设计框架

```markdown
## 🎯 问题分析

### 用户意图
[理解用户的真实需求]

### 涉及的代码
[列出涉及的文件和方法]

### 相关规则检查
- [ ] 检查 rule-001: 属性更改通知统一规范
- [ ] 检查 rule-002: 命名规范
- [ ] 检查 rule-003: 日志系统使用规范 ⚠️ **最高优先级**
- [ ] 检查 rule-008: 原型设计期代码纯净原则
- [ ] 检查 rule-004: 方案设计要求

### 违反的规则
- ❌ rule-XXX: [规则名称]
  - 具体问题：[描述]
  - 正确做法：[描述]

## 💡 解决方案

### 方案设计（遵循所有相关规则）

#### 1. [步骤1]
[详细描述]

#### 2. [步骤2]
[详细描述]

### 实施步骤
1. [步骤1]
2. [步骤2]

### 验证清单
- [ ] 遵循 rule-001
- [ ] 遵循 rule-002
- [ ] 遵循 rule-003 ✅
- [ ] 遵循 rule-008
- [ ] 遵循 rule-004

### 风险控制
- [ ] 识别风险
- [ ] 缓解措施
```

---

## ✅ 方案提交前的自我审查

### 规则遵守审查
- [ ] 是否对照了所有相关规则？
- [ ] 是否明确标注了遵循的规则？
- [ ] 是否违反了任何规则？

### 方案质量审查
- [ ] 方案是否考虑了整体架构？
- [ ] 是否复用了现有基础设施？
- [ ] 是否遵循了原型设计期原则？

### 代码质量审查
- [ ] 命名是否符合规范？
- [ ] 日志使用是否正确？
- [ ] 是否删除了旧代码？

### 验证清单审查
- [ ] 验证清单是否完整？
- [ ] 所有检查项是否通过？

---

## 🔄 变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2026-03-24 | 1.0 | 初始版本，建立规则检查清单 | Team |

---

**最后更新**: 2026-03-24  
**维护者**: SunEyeVision Team  
**版本**: 1.0
