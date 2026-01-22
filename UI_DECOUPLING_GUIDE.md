# 主界面 UI 解耦优化指南

## 概述

本文档说明了如何将工具箱、工作流画布、图像显示和属性面板从主界面解耦，以提高可维护性、可测试性和代码复用性。

## 架构设计

### 当前问题
- 所有 UI 逻辑都集中在 `MainWindow.xaml` 和 `MainWindow.xaml.cs` 中
- 主界面文件过大（> 800 行 XAML，> 600 行代码）
- 代码难以维护和测试
- 无法复用组件

### 解决方案
将主界面拆分为独立的用户控件：
1. **ToolboxControl** - 工具箱
2. **WorkflowCanvasControl** - 工作流画布
3. **ImageDisplayControl** - 图像显示
4. **PropertyPanelControl** - 属性面板

### 解耦层次

```
MainWindow (主窗口)
    ├── ToolboxControl (工具箱控件)
    ├── WorkflowCanvasControl (工作流画布控件)
    ├── ImageDisplayControl (图像显示控件)
    └── PropertyPanelControl (属性面板控件)
            ↓
    EventBus (事件总线)
            ↓
    ViewModel (数据模型)
```

## 组件说明

### 1. ToolboxControl（工具箱控件）

**文件位置**: `SunEyeVision.UI/Controls/ToolboxControl.xaml/.cs`

**职责**:
- 显示工具分类列表
- 工具搜索功能
- 展开/折叠分类
- 工具拖拽开始

**依赖属性**:
- `Categories` - 工具分类集合
- `SearchText` - 搜索文本
- `ExpandAllCommand` - 全部展开命令
- `CollapseAllCommand` - 全部折叠命令

**事件发布**:
- `ParameterChanged` - 分类展开/折叠状态改变

**优点**:
- 独立测试
- 可在其他窗口复用
- 职责单一

### 2. WorkflowCanvasControl（工作流画布控件）

**文件位置**: `SunEyeVision.UI/Controls/WorkflowCanvasControl.xaml/.cs`

**职责**:
- 显示工作流节点
- 显示节点连接线
- 处理节点拖放
- 处理工具拖放
- 处理节点选择和双击

**依赖属性**:
- `Nodes` - 节点集合
- `Connections` - 连接线集合

**事件定义**:
- `NodeAdded` - 节点添加事件
- `NodeSelected` - 节点选中事件
- `NodeDoubleClicked` - 节点双击事件
- `NodeMoved` - 节点移动事件
- `NodeClicked` - 节点点击事件

**事件发布**（通过 EventBus）:
- `NodeAddedEvent`
- `NodeSelectedEvent`
- `NodeMovedEvent`
- `DebugWindowOpenedEvent`

**优点**:
- 节点渲染逻辑独立
- 拖放逻辑集中
- 易于扩展新节点类型

### 3. ImageDisplayControl（图像显示控件）

**文件位置**: `SunEyeVision.UI/Controls/ImageDisplayControl.xaml/.cs`

**职责**:
- 显示图像
- 图像缩放
- 图像标签切换（原图、处理后、检测结果）
- 图像保存

**依赖属性**:
- `ImageSource` - 图像源（BitmapImage）
- `Scale` - 缩放比例

**优点**:
- 图像处理逻辑独立
- 支持图像缓存和性能优化
- 可在调试窗口复用

### 4. PropertyPanelControl（属性面板控件）

**文件位置**: `SunEyeVision.UI/Controls/PropertyPanelControl.xaml/.cs`

**职责**:
- 显示节点属性
- 属性分组显示
- 属性编辑

**依赖属性**:
- `Properties` - 属性分组集合（PropertyGroup）

**数据结构**:
```csharp
public class PropertyGroup
{
    public string Name { get; set; }           // 分组名称
    public bool IsExpanded { get; set; }         // 是否展开
    public ObservableCollection<PropertyItem> Parameters { get; set; }  // 参数列表
}

public class PropertyItem
{
    public string Label { get; set; }            // 显示标签
    public object Value { get; set; }            // 属性值
    public string PropertyName { get; set; }       // 属性名
}
```

**优点**:
- 属性显示逻辑独立
- 支持动态属性加载
- 可用于属性编辑器复用

## EventBus 集成

### 服务定位器

**文件位置**: `SunEyeVision.UI/Services/ServiceLocator.cs`

```csharp
public class ServiceLocator
{
    private static readonly Lazy<ServiceLocator> _instance = 
        new Lazy<ServiceLocator>(() => new ServiceLocator());
    public static ServiceLocator Instance => _instance.Value;

    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public T GetService<T>() where T : class
    {
        return _services[typeof(T)] as T;
    }
}
```

### UI 事件发布服务

**文件位置**: `SunEyeVision.UI/Services/UIEventPublisher.cs`

提供以下方法：
- `PublishNodeAdded` - 发布节点添加事件
- `PublishNodeSelected` - 发布节点选中事件
- `PublishNodeMoved` - 发布节点移动事件
- `PublishParameterChanged` - 发布参数改变事件
- 等等...

### 使用方式

**在 UserControl 中获取 EventBus**:
```csharp
private void Control_Loaded(object sender, RoutedEventArgs e)
{
    _eventPublisher = ServiceLocator.Instance.GetService<UIEventPublisher>();
    if (_eventPublisher != null)
    {
        _eventBus = ServiceLocator.Instance.GetService<IEventBus>();
    }
}
```

**发布事件**:
```csharp
_eventPublisher.PublishNodeAdded(node.Id, node.Name, algorithmType, x, y);
```

**订阅事件**:
```csharp
_eventBus.Subscribe<NodeAddedEvent>(OnNodeAdded);

private void OnNodeAdded(NodeAddedEvent eventData)
{
    // 处理节点添加
}
```

## 如何使用解耦控件

### 1. 在 MainWindow.xaml 中引用控件

```xml
<Window x:Class="SunEyeVision.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:SunEyeVision.UI.Controls">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="260" x:Name="LeftColumn"/>
            <ColumnDefinition Width="5" x:Name="LeftSplitterColumn"/>
            <ColumnDefinition Width="*" x:Name="MiddleColumn"/>
            <ColumnDefinition Width="5" x:Name="RightSplitterColumn"/>
            <ColumnDefinition Width="500" x:Name="RightColumn"/>
        </Grid.ColumnDefinitions>

        <!-- 左侧：工具箱 -->
        <controls:ToolboxControl Grid.Column="0"
                               Categories="{Binding Toolbox.Categories}"
                               SearchText="{Binding Toolbox.SearchText, Mode=TwoWay}"
                               ExpandAllCommand="{Binding Toolbox.ExpandAllCommand}"
                               CollapseAllCommand="{Binding Toolbox.CollapseAllCommand}"/>

        <!-- 中间：工作流画布 -->
        <controls:WorkflowCanvasControl Grid.Column="2"
                                    Nodes="{Binding WorkflowNodes}"
                                    Connections="{Binding WorkflowConnections}"
                                    NodeAdded="OnWorkflowCanvas_NodeAdded"
                                    NodeSelected="OnWorkflowCanvas_NodeSelected"
                                    NodeDoubleClicked="OnWorkflowCanvas_NodeDoubleClicked"/>

        <!-- 右侧：属性面板 + 图像显示 -->
        <Grid Grid.Column="4">
            <Grid.RowDefinitions>
                <RowDefinition Height="500"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- 图像显示 -->
            <controls:ImageDisplayControl Grid.Row="0"
                                       ImageSource="{Binding DisplayImage}"
                                       Scale="{Binding ImageScale}"/>

            <!-- 属性面板 -->
            <controls:PropertyPanelControl Grid.Row="1"
                                       Properties="{Binding SelectedNodeProperties}"/>
        </Grid>
    </Grid>
</Window>
```

### 2. 在 MainWindow.xaml.cs 中处理控件事件

```csharp
private void OnWorkflowCanvas_NodeAdded(object sender, WorkflowNode node)
{
    // 将节点添加到 ViewModel
    _viewModel.WorkflowNodes.Add(node);
    _viewModel.StatusText = $"添加节点: {node.Name}";
}

private void OnWorkflowCanvas_NodeSelected(object sender, WorkflowNode node)
{
    // 更新选中状态
    _viewModel.SelectedNode = node;
    _viewModel.LoadNodeProperties(node);
}

private void OnWorkflowCanvas_NodeDoubleClicked(object sender, WorkflowNode node)
{
    // 打开调试窗口
    _viewModel.OpenDebugWindowCommand.Execute(node);
}
```

## 优化建议

### 1. 数据绑定优先

- ✅ 使用 `DependencyProperty` 和数据绑定
- ❌ 避免直接访问控件的内部字段

### 2. 通过 EventBus 通信

- ✅ 控件间通过 EventBus 通信
- ❌ 避免控件间直接方法调用

### 3. 单一职责原则

- ✅ 每个控件只负责自己的 UI 和交互
- ❌ 避免在一个控件中处理多种职责

### 4. 命令模式

- ✅ 使用 ICommand 实现命令
- ❌ 避免在 Code-Behind 中写逻辑

### 5. 数据驱动

- ✅ UI 由数据驱动
- ❌ 避免直接操作 UI 元素

## 测试建议

### 单元测试

可以单独测试每个控件：
- 测试工具箱的展开/折叠功能
- 测试工作流画布的拖放功能
- 测试属性面板的数据绑定

### 集成测试

测试控件与 EventBus 的集成：
- 测试事件发布
- 测试事件订阅
- 测试跨控件通信

## 性能优化

1. **虚拟化**: 对于大量节点，考虑使用虚拟化控件（VirtualizingStackPanel）
2. **节流**: 对频繁触发的事件（如节点移动）进行节流处理
3. **异步**: 耗时操作使用 async/await
4. **缓存**: 图像和属性数据考虑缓存

## 迁移步骤

1. 创建独立的用户控件（已完成）
2. 将主界面中的相关 XAML 移动到控件中
3. 将主界面中的相关代码移动到控件中
4. 在主界面中使用控件并设置绑定
5. 测试功能是否正常
6. 删除主界面中的旧代码
7. 构建并验证

## 总结

通过将主界面解耦为独立的用户控件，我们获得了以下优势：

1. **可维护性提升**: 每个控件代码量小，易于理解和修改
2. **可测试性提升**: 可以单独测试每个控件
3. **可复用性提升**: 控件可以在其他窗口或项目中复用
4. **协作友好**: 不同开发者可以同时开发不同控件
5. **架构清晰**: 通过 EventBus 实现松耦合通信

## 参考资料

- [EventBus 使用指南](./docs/EventBus使用指南.md)
- [MVVM 模式](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern)
