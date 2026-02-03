# AIStudio.Wpf.DiagramDesigner 控件库集成指南

## 概述

本指南详细说明如何将 AIStudio.Wpf.DiagramDesigner 控件库集成到 SunEyeVision 项目中，实现流程图设计功能。

## 集成状态

✅ **已完成**: AIStudioDiagramControl控件已成功创建并编译通过
- 位置: `SunEyeVision.UI/Controls/AIStudioDiagramControl.xaml`
- 后端代码: `SunEyeVision.UI/Controls/AIStudioDiagramControl.xaml.cs`
- 编译状态: 成功（0错误，289警告）

## 集成方案

### 方案一：渐进式集成（推荐）

**优点：**
- 保留现有架构，风险低
- 可以逐步引入AIStudio功能
- 便于对比测试和回滚

**步骤：**
1. ✅ 创建新的用户控件 `AIStudioDiagramControl` 封装AIStudio功能
2. ⏳ 在MainWindow中添加切换按钮，在现有画布和AIStudio画布之间切换
3. ⏳ 逐步将现有功能迁移到AIStudio控件
4. ⏳ 最终完全替换现有画布实现

### 方案二：完全替换

**优点：**
- 代码更简洁，没有冗余
- 直接使用AIStudio的完整功能

**缺点：**
- 需要大量重构现有代码
- 风险较高，需要充分测试

## MainWindow集成步骤

### 1. 添加命名空间引用

在 `MainWindow.xaml` 中添加：

```xml
xmlns:controls="clr-namespace:SunEyeVision.UI.Controls"
```

### 2. 在工作流编辑区域添加AIStudio控件

在现有的 `WorkflowCanvasControl` 旁边或替换它：

```xml
<!-- 方案一：并排显示（推荐） -->
<Grid>
    <controls:WorkflowCanvasControl x:Name="WorkflowCanvas" 
                                   Visibility="{Binding UseAIStudioDiagram, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
    
    <controls:AIStudioDiagramControl x:Name="AIStudioDiagram" 
                                    Visibility="{Binding UseAIStudioDiagram, Converter={StaticResource BoolToVisibilityConverter}}"/>
</Grid>

<!-- 方案二：完全替换 -->
<controls:AIStudioDiagramControl x:Name="AIStudioDiagram"/>
```

### 3. 在ViewModel中添加属性

```csharp
private bool _useAIStudioDiagram = false;

public bool UseAIStudioDiagram
{
    get => _useAIStudioDiagram;
    set
    {
        if (_useAIStudioDiagram != value)
        {
            _useAIStudioDiagram = value;
            OnPropertyChanged();
            
            // 切换画布时同步数据
            if (value)
            {
                SyncToAIStudioDiagram();
            }
            else
            {
                SyncFromAIStudioDiagram();
            }
        }
    }
}

private void SyncToAIStudioDiagram()
{
    // 将现有工作流数据同步到AIStudio画布
    if (AIStudioDiagram != null && CurrentTab != null)
    {
        var json = SerializeWorkflowToJson();
        AIStudioDiagram.LoadFromJson(json);
    }
}

private void SyncFromAIStudioDiagram()
{
    // 从AIStudio画布同步数据到现有工作流
    if (AIStudioDiagram != null && CurrentTab != null)
    {
        var json = AIStudioDiagram.SaveToJson();
        DeserializeWorkflowFromJson(json);
    }
}
```

### 4. 添加切换命令

```csharp
public ICommand ToggleDiagramCommand { get; }

private void ExecuteToggleDiagram()
{
    UseAIStudioDiagram = !UseAIStudioDiagram;
    StatusText = UseAIStudioDiagram ? "已切换到AIStudio画布" : "已切换到标准画布";
}
```

### 5. 在工具栏添加切换按钮

```xml
<Button Content="切换画布" 
        Command="{Binding ToggleDiagramCommand}"
        ToolTip="在标准画布和AIStudio画布之间切换"/>
```

## 📚 AIStudio功能特性

### 核心功能

- ✅ 四合一绘图引擎（自由画笔/几何图形/流程图/思维导图）
- ✅ 内置4种连线算法（直角、正交、曲线、直线）
- ✅ 智能避障路径规划
- ✅ 拖拽、缩放、旋转
- ✅ 多选、对齐、分布
- ✅ 撤销/重做
- ✅ 复制/粘贴
- ✅ 键盘快捷键支持
- ✅ 动画与交互效果

### 连线算法

1. **直角连线** - 简单的直角折线
2. **正交连线** - 智能避障的正交路径
3. **曲线连线** - 平滑的贝塞尔曲线
4. **直线连线** - 直接连接

### 路径规划

- 自动避障
- 最短路径
- 智能拐点
- 可配置参数

## 🔧 配置选项

```csharp
// 配置编辑器选项
_flowchartEditor.Options.ShowGrid = true;              // 显示网格
_flowchartEditor.Options.GridCellSize = 20;            // 网格大小
_flowchartEditor.Options.AllowConnection = true;       // 允许连接
_flowchartEditor.Options.AllowDrag = true;             // 允许拖拽
_flowchartEditor.Options.AllowResize = true;           // 允许调整大小
_flowchartEditor.Options.AllowDelete = true;           // 允许删除
_flowchartEditor.Options.AllowRotate = true;           // 允许旋转
_flowchartEditor.Options.AllowCopy = true;             // 允许复制
_flowchartEditor.Options.AllowPaste = true;            // 允许粘贴
_flowchartEditor.Options.AllowUndo = true;             // 允许撤销
_flowchartEditor.Options.AllowRedo = true;             // 允许重做
```

## 📖 API参考

### AIStudioDiagramControl

#### 公共方法

- `LoadFromWorkflow(nodes, connections)` - 从工作流数据加载
- `ExportToWorkflow()` - 导出为工作流数据
- `SaveToJson()` - 保存为JSON字符串
- `LoadFromJson(json)` - 从JSON字符串加载
- `SaveToFile(filePath)` - 保存到文件
- `LoadFromFile(filePath)` - 从文件加载
- `Clear()` - 清空画布
- `SetZoom(zoom)` - 设置缩放级别
- `FitToWindow()` - 适应窗口大小

#### 公共属性

- `WorkflowNodes` - 工作流节点列表
- `WorkflowConnections` - 工作流连接列表

#### 公共事件

- `NodeSelected` - 节点选中事件
- `ConnectionCreated` - 连接创建事件
- `ConnectionDeleted` - 连接删除事件
- `NodeMoved` - 节点移动事件

## 🎨 自定义样式

### 节点样式

```csharp
var node = new DiagramNode
{
    Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
    BorderThickness = new Thickness(2),
    HeaderBackground = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
    HeaderForeground = new SolidColorBrush(Colors.White),
    Width = 150,
    Height = 100
};
```

### 连线样式

```csharp
var connection = new DiagramConnection
{
    Stroke = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
    StrokeThickness = 2,
    StrokeDashArray = null  // 实线
};
```

## 🔍 调试技巧

### 查看内部数据

```csharp
// 查看所有节点
foreach (var node in AIStudioDiagram.WorkflowNodes)
{
    Debug.WriteLine($"节点: {node.Name} ({node.X}, {node.Y})");
}

// 查看所有连接
foreach (var connection in AIStudioDiagram.WorkflowConnections)
{
    Debug.WriteLine($"连接: {connection.SourceNodeId} -> {connection.TargetNodeId}");
}
```

### 导出JSON查看

```csharp
string json = AIStudioDiagram.SaveToJson();
Debug.WriteLine(json);
```

## 🌐 参考资源

- **Gitee开源地址**: https://gitee.com/akwkevin/aistudio.-wpf.-diagram
- **开源协议**: LGPL-3.0
- **当前版本**: 1.3.1
- **最新版本**: 2.0（建议升级）

## ⚠️ 注意事项

1. **版本兼容性**: 当前使用1.3.1版本，建议升级到2.0以获得更多功能
2. **数据同步**: 切换画布时需要同步数据
3. **事件处理**: 需要正确订阅和处理事件
4. **性能优化**: 大量节点时考虑虚拟化
5. **样式自定义**: 可以通过样式和模板自定义外观

## 🚀 下一步

1. 在MainWindow中集成AIStudioDiagramControl
2. 添加画布切换功能
3. 测试数据同步
4. 自定义样式和行为
5. 根据需要升级到2.0版本

## 💡 最佳实践

1. **渐进式集成**: 先在测试环境验证，再逐步推广
2. **数据备份**: 切换画布前备份数据
3. **错误处理**: 添加适当的异常处理
4. **用户提示**: 切换画布时提示用户
5. **性能监控**: 监控性能指标，优化用户体验
