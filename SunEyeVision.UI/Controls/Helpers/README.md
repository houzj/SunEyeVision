# WorkflowCanvasControl 重构说明

## 概述

本次重构将 `WorkflowCanvasControl.xaml.cs` 中的复杂功能模块化，提取到独立的辅助类中，以提高代码的可维护性和可测试性。

## 重构的辅助类

### 1. WorkflowConnectionManager.cs
**职责：** 连接管理相关功能
- 连接的添加、删除、更新
- 连接状态管理
- 连接验证

### 2. WorkflowNodeInteractionHandler.cs
**职责：** 节点交互处理相关功能
- 节点鼠标事件处理（按下、移动、释放）
- 节点拖拽逻辑
- 节点选择和取消选择
- 双击打开调试窗口

### 3. WorkflowPortInteractionHandler.cs
**职责：** 端口交互处理相关功能
- 端口鼠标事件处理
- 连接拖拽开始和结束
- 目标端口高亮
- 临时连接线管理

### 4. WorkflowSelectionHandler.cs
**职责：** 框选和选择处理相关功能
- Canvas鼠标事件处理
- 框选矩形更新
- 节点选择逻辑
- 清除选中状态
- 记录选中节点位置

### 5. WorkflowDragDropHandler.cs
**职责：** 拖放处理相关功能
- Canvas_DragEnter、Canvas_DragOver、Canvas_DragLeave、Canvas_Drop事件处理
- 从工具箱拖放节点到画布的逻辑

### 6. WorkflowVisualHelper.cs
**职责：** 视觉树查找等辅助功能
- FindVisualChild<T> - 查找第一个匹配的子元素
- FindAllVisualChildren<T> - 查找所有匹配的子元素
- Distance - 计算两点之间的距离
- FindNearestPort - 查找最近的端口
- GetVisualParent - 获取视觉父级

### 7. WorkflowPathCalculator.cs
**职责：** 路径计算相关功能
- CalculateSmartPath - 计算智能直角折线路径
- CalculateArrowAngle - 计算箭头的旋转角度
- RefreshAllConnectionPaths - 刷新所有连接路径

### 8. WorkflowConnectionCreator.cs
**职责：** 连接创建相关功能
- CreateConnectionWithSpecificPort - 使用指定端口创建连接
- CreateConnection - 智能选择端口创建连接
- CalculateSmartPortPositions - 计算智能端口位置
- ConnectionExists - 检查连接是否已存在
- IsSelfConnection - 检查是否为自连接

### 9. WorkflowPortHighlighter.cs
**职责：** 端口高亮相关功能
- HighlightTargetPort - 高亮目标端口（智能选择）
- HighlightSpecificPort - 高亮指定端口
- ClearTargetPortHighlight - 清除端口高亮
- GetPortElement - 获取端口元素

## 使用方式

### 在 WorkflowCanvasControl 中使用辅助类

```csharp
public partial class WorkflowCanvasControl : UserControl
{
    private WorkflowConnectionManager _connectionManager;
    private WorkflowNodeInteractionHandler _nodeInteractionHandler;
    private WorkflowPortInteractionHandler _portInteractionHandler;
    private WorkflowSelectionHandler _selectionHandler;
    private WorkflowDragDropHandler _dragDropHandler;
    private WorkflowPortHighlighter _portHighlighter;

    public WorkflowCanvasControl()
    {
        InitializeComponent();
        
        // 初始化辅助类
        _connectionManager = new WorkflowConnectionManager(_viewModel);
        _nodeInteractionHandler = new WorkflowNodeInteractionHandler(this, _viewModel);
        _portInteractionHandler = new WorkflowPortInteractionHandler(this, _viewModel);
        _selectionHandler = new WorkflowSelectionHandler(this, _viewModel);
        _dragDropHandler = new WorkflowDragDropHandler(this, _viewModel);
        _portHighlighter = new WorkflowPortHighlighter(_viewModel);
    }
}
```

### 使用示例

#### 1. 创建连接
```csharp
var connectionCreator = new WorkflowConnectionCreator(_viewModel);
var connection = connectionCreator.CreateConnection(
    sourceNode, 
    targetNode, 
    "RightPort", 
    CurrentWorkflowTab);
```

#### 2. 高亮端口
```csharp
_portHighlighter.HighlightTargetPort(nodeBorder, sourceNode, "RightPort");
```

#### 3. 计算路径
```csharp
var pathPoints = WorkflowPathCalculator.CalculateSmartPath(
    sourcePosition, 
    targetPosition, 
    "RightPort", 
    "LeftPort");
```

#### 4. 查找视觉元素
```csharp
var portElement = WorkflowVisualHelper.FindVisualChild<Ellipse>(nodeBorder, "LeftPortEllipse");
```

## 重构的优势

1. **单一职责原则**：每个辅助类只负责一个特定的功能领域
2. **提高可维护性**：代码更易于理解和修改
3. **提高可测试性**：可以独立测试每个辅助类
4. **降低耦合度**：减少 WorkflowCanvasControl 的复杂度
5. **代码复用**：辅助类可以在其他地方复用

## 后续工作

1. 将 WorkflowCanvasControl 中的相关代码迁移到辅助类
2. 更新 WorkflowCanvasControl 以使用新的辅助类
3. 添加单元测试
4. 更新文档和注释

## 注意事项

- 所有辅助类都通过构造函数接收必要的依赖（如 ViewModel）
- 辅助类之间保持低耦合，通过接口或事件进行通信
- 保留原有的日志记录功能，便于调试
