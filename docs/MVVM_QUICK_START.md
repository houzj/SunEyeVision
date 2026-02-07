# MVVM架构快速开始指南

## 快速体验

### 1. 双击Algorithm节点
- 在工作流画布中双击任何算法节点（如"高斯模糊"）
- 会自动打开对应的调试窗口
- 可以在窗口中调整参数并运行测试

### 2. 双击Subroutine节点
- 在工作流中双击子程序节点
- 会自动创建新的工作流标签页
- 标签页名称 = 子程序节点名称
- 可以在新标签页中添加节点，定义子程序逻辑

## 添加新工具的调试界面

### 步骤1：创建ViewModel（1分钟）
```csharp
// 文件：SunEyeVision.PluginSystem/Tools/MyTool/ViewModels/MyToolViewModel.cs
public class MyToolViewModel : ToolDebugViewModelBase
{
    public string MyParameter { get; set; } = "默认值";

    public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
    {
        ToolId = toolId;
        ToolName = toolMetadata?.DisplayName ?? "MyTool";
        LoadParameters(toolMetadata);
    }

    public override void LoadParameters(ToolMetadata? toolMetadata)
    {
        // 从toolMetadata加载参数
        var param = toolMetadata?.InputParameters?.FirstOrDefault(p => p.Name == "MyParameter");
        MyParameter = param?.DefaultValue?.ToString() ?? "默认值";
    }

    public override Dictionary<string, object> SaveParameters()
    {
        // 保存参数到字典
        return new Dictionary<string, object>
        {
            { "MyParameter", MyParameter }
        };
    }
}
```

### 步骤2：创建调试窗口（5分钟）
```xml
<!-- 文件：SunEyeVision.UI/Controls/MyToolDebugWindow.xaml -->
<Controls:BaseToolDebugWindow x:Class="SunEyeVision.UI.Controls.MyToolDebugWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Controls="clr-namespace:SunEyeVision.UI.Controls">
    <!-- 自定义UI内容 -->
    <StackPanel Margin="16">
        <TextBlock Text="MyParameter:" FontSize="12"/>
        <TextBox Text="{Binding MyParameter, UpdateSourceTrigger=PropertyChanged}" Padding="6" FontSize="12"/>
    </StackPanel>
</Controls:BaseToolDebugWindow>
```

```csharp
// 文件：SunEyeVision.UI/Controls/MyToolDebugWindow.xaml.cs
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

### 步骤3：注册到工厂（1分钟）
```csharp
// 文件：SunEyeVision.PluginSystem/MVVM/ToolDebugWindowFactory.cs
public static Window CreateDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
{
    switch (toolId)
    {
        case "MyTool":
            return new MyToolDebugWindow(toolId, toolPlugin, toolMetadata);
        // ... 其他工具
        default:
            return new DebugWindow(toolId, toolPlugin ?? new DefaultToolPlugin(), toolMetadata);
    }
}
```

完成！总共只需要7分钟！

## 文件组织结构

### 推荐的工具目录结构
```
SunEyeVision.PluginSystem/Tools/ToolName/
├── Algorithms/              # 算法实现
│   └── ToolNameAlgorithm.cs
├── Models/                  # 数据模型
│   └── ToolNameModel.cs
├── ViewModels/              # 视图模型
│   └── ToolNameViewModel.cs
├── DTOs/                    # 序列化DTO
│   └── ToolNameDTO.cs
└── ToolNamePlugin.cs        # 插件实现

SunEyeVision.UI/Controls/
├── ToolNameDebugWindow.xaml
└── ToolNameDebugWindow.xaml.cs
```

## 示例代码

### 完整的ViewModel示例
参考 `ImageSaveToolViewModel.cs`:
- 参数属性定义
- 参数验证
- 业务逻辑实现
- 状态管理

### 完整的调试窗口示例
参考 `ImageSaveToolDebugWindow.xaml`:
- 参数分组布局
- 输入控件
- 执行按钮
- 状态显示

## 节点界面类型

根据节点类型，双击会打开不同的界面：

| 节点类型 | 界面类型 | 说明 |
|---------|---------|------|
| Algorithm | DebugWindow | 打开工具调试窗口 |
| Subroutine | NewWorkflowCanvas | 创建新的工作流标签页 |
| Condition | SubroutineEditor | 打开条件配置界面（待实现） |
| Start | DebugWindow | 打开工具调试窗口 |

## 常见问题

### Q1: 如何让我的工具使用专用调试窗口？
A: 在 `ToolDebugWindowFactory.CreateDebugWindow` 中添加对应的case即可。

### Q2: 如何在子程序节点双击时传递参数？
A: 目前不支持，后续会添加参数映射界面。

### Q3: 如何测试新的调试窗口？
A:
1. 编译项目
2. 运行应用程序
3. 在工作流中添加对应工具的节点
4. 双击节点测试调试窗口

### Q4: ViewModel中的属性如何绑定到XAML？
A: 确保ViewModel实现了`INotifyPropertyChanged`，并且属性使用`SetProperty`方法。

```csharp
private string _myParameter = "";
public string MyParameter
{
    get => _myParameter;
    set => SetProperty(ref _myParameter, value);
}
```

## 进阶主题

### 添加参数验证
```csharp
public override Dictionary<string, object> SaveParameters()
{
    if (string.IsNullOrWhiteSpace(MyParameter))
    {
        StatusMessage = "❌ 参数不能为空";
        return new Dictionary<string, object>();
    }

    return new Dictionary<string, object>
    {
        { "MyParameter", MyParameter }
    };
}
```

### 添加自定义命令
```csharp
public class MyToolViewModel : ToolDebugViewModelBase
{
    public ICommand BrowseCommand { get; }

    public MyToolViewModel()
    {
        BrowseCommand = new RelayCommand(ExecuteBrowse);
    }

    private void ExecuteBrowse()
    {
        var dialog = new OpenFileDialog();
        if (dialog.ShowDialog() == true)
        {
            MyParameter = dialog.FileName;
        }
    }
}
```

### 添加性能统计
```csharp
public override void RunTool()
{
    var stopwatch = Stopwatch.StartNew();

    // 执行工具逻辑
    ExecuteToolLogic();

    stopwatch.Stop();
    ExecutionTime = $"{stopwatch.ElapsedMilliseconds} ms";
    StatusMessage = "✅ 执行完成";
}
```

## 参考资源

- **实施指南**: `docs/MVVM_IMPLEMENTATION_GUIDE.md`
- **实施总结**: `docs/MVVM_IMPLEMENTATION_SUMMARY.md`
- **示例代码**: `SunEyeVision.PluginSystem/Tools/ImageSaveTool/`

## 下一步

1. 为现有工具创建专用调试窗口
2. 实现子程序编辑器（条件配置界面）
3. 添加参数映射界面
4. 完善工具配置验证
5. 添加性能统计和历史记录

---

**最后更新**: 2026-02-07
