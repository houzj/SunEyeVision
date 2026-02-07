# SunEyeVision MVVM架构迁移实施指南

## 概述

本文档描述了SunEyeVision项目迁移到MVVM架构的完整实施方案，包括节点界面路由机制、按工具组织的文件结构、以及子程序工作流标签页的创建。

## 实施阶段

### 阶段一：MVVM基础架构（已完成）

#### 1.1 节点界面类型枚举
**文件**: `SunEyeVision.PluginSystem/MVVM/NodeInterfaceType.cs`

定义了4种节点界面类型：
- `None`: 无界面
- `DebugWindow`: 传统调试窗口
- `SubroutineEditor`: 子程序编辑器（条件配置界面）
- `NewWorkflowCanvas`: 新工作流画布

#### 1.2 节点界面工厂
**文件**: `SunEyeVision.PluginSystem/MVVM/NodeInterfaceFactory.cs`

根据节点类型决定打开哪个界面：
```csharp
var interfaceType = NodeInterfaceFactory.GetInterfaceType(node, toolMetadata);
```

**路由规则**：
- `Subroutine` 类型节点 → `NewWorkflowCanvas`（创建新的工作流标签页）
- `Condition` 类型节点 → `SubroutineEditor`（条件配置界面）
- `Algorithm`/`Start` 类型节点 → `DebugWindow`（传统调试窗口）

#### 1.3 工具调试ViewModel基类
**文件**: `SunEyeVision.UI/ViewModels/ToolDebugViewModelBase.cs`

提供工具调试界面的通用功能：
- 属性：`ToolName`, `ToolId`, `ToolStatus`, `StatusMessage`, `ExecutionTime`, `FPS`
- 抽象方法：
  - `Initialize(string, IToolPlugin, ToolMetadata)` - 初始化界面
  - `LoadParameters(ToolMetadata)` - 加载参数
  - `SaveParameters()` - 保存参数
- 虚方法：
  - `ResetParameters()` - 重置参数
  - `RunTool()` - 运行工具

#### 1.4 基础调试窗口
**文件**: `SunEyeVision.UI/Controls/BaseToolDebugWindow.xaml` 和 `.xaml.cs`

工具调试窗口的基类，提供标准布局：
- 标题栏（工具名称）
- 主内容区（由子类填充）
- 底部状态栏（状态消息、执行时间、FPS）

#### 1.5 工具调试窗口工厂
**文件**: `SunEyeVision.PluginSystem/MVVM/ToolDebugWindowFactory.cs`

根据工具ID创建对应的调试窗口：
```csharp
var debugWindow = ToolDebugWindowFactory.CreateDebugWindow(toolId, toolPlugin, toolMetadata);
```

支持专用调试窗口（如`ImageSaveTool`）和默认调试窗口。

### 阶段二：MainWindowViewModel集成（已完成）

#### 2.1 修改ExecuteOpenDebugWindow方法
**文件**: `SunEyeVision.UI/ViewModels/MainWindowViewModel.cs`

使用NodeInterfaceFactory决定打开哪个界面：
```csharp
var interfaceType = NodeInterfaceFactory.GetInterfaceType(node, toolMetadata);

switch (interfaceType)
{
    case NodeInterfaceType.DebugWindow:
        var debugWindow = ToolDebugWindowFactory.CreateDebugWindow(toolId, toolPlugin, toolMetadata);
        debugWindow.ShowDialog();
        break;

    case NodeInterfaceType.NewWorkflowCanvas:
        CreateSubroutineWorkflowTab(node);
        break;

    case NodeInterfaceType.SubroutineEditor:
        // TODO: 实现子程序编辑器
        break;
}
```

#### 2.2 创建子程序工作流标签页
新增 `CreateSubroutineWorkflowTab` 方法：

```csharp
private void CreateSubroutineWorkflowTab(Models.WorkflowNode subroutineNode)
{
    // 使用子程序节点名称作为工作流名称
    string workflowName = subroutineNode.Name;

    // 创建新的工作流标签页
    var newWorkflowTab = new WorkflowTabViewModel
    {
        Name = workflowName,
        Id = Guid.NewGuid().ToString()
    };

    // 添加到标签页集合并选中
    WorkflowTabViewModel.Tabs.Add(newWorkflowTab);
    WorkflowTabViewModel.SelectedTab = newWorkflowTab;
}
```

**关键特性**：
- 工作流名称 = 子程序节点名称
- 自动选中新建的标签页
- 独立的节点和连接集合
- 独立的撤销/重做命令管理器

### 阶段三：文件结构改造（已完成示例）

#### 3.1 按工具组织的目录结构

```
SunEyeVision.PluginSystem/Tools/[ToolName]/
├── Algorithms/          # 算法实现
│   └── [ToolName]Algorithm.cs
├── Models/              # 数据模型
│   └── [ToolName]Model.cs
├── ViewModels/          # 视图模型
│   └── [ToolName]ViewModel.cs
├── DTOs/                # 序列化DTO
│   └── [ToolName]DTO.cs
└── [ToolName]Plugin.cs  # 插件实现
```

#### 3.2 ImageSaveTool示例

**DTOs** (`ImageSaveToolDTO.cs`):
- 用于JSON序列化和持久化
- 提供 `FromToolMetadata` 和 `ToToolMetadata` 方法
- 包含工具的所有参数

**ViewModels** (`ImageSaveToolViewModel.cs`):
- 继承自 `ToolDebugViewModelBase`
- 实现参数加载和保存逻辑
- 提供工具特定的属性和方法

**UI层** (`SunEyeVision.UI/Controls/ImageSaveToolDebugWindow.xaml/.xaml.cs`):
- 继承自 `BaseToolDebugWindow`
- 提供工具特定的UI界面
- 使用自定义的ViewModel

## 使用方法

### 1. 为现有工具添加专用调试窗口

#### 步骤1：创建ViewModel
```csharp
public class MyToolViewModel : ToolDebugViewModelBase
{
    // 定义工具特定的属性
    public string MyParameter { get; set; }

    // 实现抽象方法
    public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
    {
        ToolId = toolId;
        ToolName = toolMetadata?.DisplayName ?? "MyTool";
        LoadParameters(toolMetadata);
    }

    public override void LoadParameters(ToolMetadata? toolMetadata)
    {
        // 从toolMetadata加载参数
    }

    public override Dictionary<string, object> SaveParameters()
    {
        // 保存参数到字典
    }
}
```

#### 步骤2：创建调试窗口XAML
```xml
<Controls:BaseToolDebugWindow x:Class="SunEyeVision.UI.Controls.MyToolDebugWindow"
        xmlns:Controls="clr-namespace:SunEyeVision.UI.Controls"
        xmlns:vm="clr-namespace:SunEyeVision.PluginSystem.Tools.MyTool.ViewModels">
    <!-- 自定义UI内容 -->
</Controls:BaseToolDebugWindow>
```

#### 步骤3：创建调试窗口Code-Behind
```csharp
public partial class MyToolDebugWindow : BaseToolDebugWindow
{
    public MyToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        : base(toolId, toolPlugin, toolMetadata)
    {
        InitializeComponent();
    }

    protected override ToolDebugViewModelBase CreateViewModel()
    {
        return new MyToolViewModel();
    }
}
```

#### 步骤4：注册到工厂
在 `ToolDebugWindowFactory.CreateDebugWindow` 中添加case：
```csharp
switch (toolId)
{
    case "MyTool":
        return new MyToolDebugWindow(toolId, toolPlugin, toolMetadata);
    // ...
}
```

### 2. 创建子程序节点并双击打开工作流

1. 在工作流中添加一个 `Subroutine` 类型的节点
2. 设置节点名称（例如："图像检测子程序"）
3. 双击该节点
4. 系统会自动创建一个新的工作流标签页，名称为"图像检测子程序"
5. 在新标签页中添加节点，定义子程序的逻辑

## 关键设计决策

### 1. 为什么使用工厂模式而不是直接创建窗口？

- **解耦**: 窗口创建逻辑与业务逻辑分离
- **可扩展**: 添加新工具时只需在工厂中添加case
- **可测试**: 工厂方法可以轻松mock
- **统一入口**: 所有调试窗口通过同一个工厂创建

### 2. 为什么子程序节点创建新标签页而不是新窗口？

- **用户体验**: 标签页切换更流畅，符合工作流编辑器的UI模式
- **资源管理**: 共享主窗口资源，减少内存占用
- **上下文**: 用户可以在不同工作流间快速切换，保持上下文
- **一致性**: 与现有的工作流标签页机制一致

### 3. 为什么按工具组织文件结构？

- **模块化**: 每个工具是一个独立的模块
- **可维护性**: 工具相关文件集中管理，易于查找和修改
- **可复用**: 工具可以独立开发和测试
- **扩展性**: 新增工具只需复制目录结构

## 后续任务

### 高优先级
1. ✅ 创建子程序工作流标签页
2. ⏳ 实现子程序编辑器（条件配置界面）
3. ⏳ 为其他现有工具创建专用调试窗口
4. ⏳ 实现子程序参数映射界面

### 中优先级
1. ⏳ 添加工具配置验证
2. ⏳ 实现工具参数的序列化/反序列化
3. ⏳ 添加工具性能统计功能
4. ⏳ 实现工具调试历史记录

### 低优先级
1. ⏳ 支持自定义工具界面模板
2. ⏳ 实现工具界面主题切换
3. ⏳ 添加工具界面国际化支持
4. ⏳ 实现工具界面自动化测试

## 测试清单

### 单元测试
- [ ] `NodeInterfaceFactory.GetInterfaceType` 测试各种节点类型
- [ ] `ToolDebugWindowFactory.CreateDebugWindow` 测试各种工具ID
- [ ] `ToolDebugViewModelBase` 子类的参数加载和保存测试
- [ ] `CreateSubroutineWorkflowTab` 测试各种节点名称

### 集成测试
- [ ] 双击Algorithm节点打开调试窗口
- [ ] 双击Subroutine节点创建新工作流标签页
- [ ] 双击Condition节点打开子程序编辑器（待实现）
- [ ] 子程序工作流标签页的独立性和隔离性
- [ ] 专用调试窗口的参数加载和保存

### UI测试
- [ ] 调试窗口的布局和响应性
- [ ] 参数控件的交互和验证
- [ ] 工作流标签页的切换和关闭
- [ ] 错误处理和用户提示

## 参考文档

- [SunEyeVision架构设计](../docs/ARCHITECTURE.md)
- [工作流节点类型实现](../docs/WORKFLOW_NODETYPE_IMPLEMENTATION.md)
- [工作流执行开发计划](../docs/工作流执行开发计划.md)
- [插件系统设计](../docs/PLUGIN_SYSTEM_DESIGN.md)

## 变更历史

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-07 | 1.0 | 初始版本，完成MVVM基础架构、MainWindowViewModel集成、文件结构示例 |

## 联系方式

如有问题或建议，请联系开发团队。
