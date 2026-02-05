# 连接线调试报告

## 问题概述

用户在拖拽连接节点端口时遇到以下问题：

1. **Substring 越界异常** - 已修复 2 处
2. **节点 ID 为空** - 正在诊断中
3. **路径数据未正确创建** - 相关问题

## 错误日志分析

```
[SmartPathConverter] Convert called for connection: conn_1
[SmartPathConverter] Source node: , Target node:
[Path_Loaded] ? 路径数据未正确创建
[ArrowPath] 连接conn_1 箭头渲染完成: 位置(0.0,0.0), 角度0.0°
```

**问题分析**:
- 连接 ID 是正确的 (conn_1, conn_2)
- 但是 SourceNodeId 和 TargetNodeId 可能为空字符串
- 导致 SmartPathConverter 无法找到对应的节点

## 已修复的问题

### 1. Substring 越界异常 (第 1 处 - 已修复)
**文件**: `WorkflowCanvasControl.xaml.cs:773`

**错误**:
```csharp
System.Diagnostics.Debug.WriteLine($"[DragStart] 节点:{node.Name}({node.Id.Substring(0,8)}...) 端口:{portName} 位置:({portPosition.X:F0},{portPosition.Y:F0})");
```

**修复**:
```csharp
System.Diagnostics.Debug.WriteLine($"[DragStart] 节点:{node.Name}({node.Id.Substring(0, Math.Min(8, node.Id.Length))}...) 端口:{portName} 位置:({portPosition.X:F0},{portPosition.Y:F0})");
```

### 2. Substring 越界异常 (第 2 处 - 已修复)
**文件**: `WorkflowCanvasControl.xaml.cs:1531`

**错误**:
```csharp
System.Diagnostics.Debug.WriteLine($"[Canvas]   - {node.Name} (ID:{node.Id.Substring(0,8)}...) 位置:({node.Position.X:F0},{node.Position.Y:F0})");
```

**修复**:
```csharp
System.Diagnostics.Debug.WriteLine($"[Canvas]   - {node.Name} (ID:{node.Id.Substring(0, Math.Min(8, node.Id.Length))}...) 位置:({node.Position.X:F0},{node.Position.Y:F0})");
```

### 3. 增强调试日志 (已修复)
**文件**: `SmartPathConverter.cs:48-65`

**添加的日志**:
- SourceNodeId 和 TargetNodeId 的实际值
- Nodes 集合的大小
- 所有可用的节点 ID（前 5 个）
- 源节点和目标节点的查找结果

## 待诊断的问题

### 节点 ID 为空

**症状**:
```
[SmartPathConverter] SourceNodeId: '', TargetNodeId: ''
```

**可能原因**:
1. WorkflowConnection 构造函数没有正确设置节点 ID
2. MainWindowViewModel 初始化时使用硬编码的 ID "1", "2", "3"，但节点实际 ID 不同
3. WorkflowNode 构造函数的 Id 属性设置有问题

**调查方法**:
1. 检查 MainWindowViewModel 中的节点和连接初始化代码
2. 验证节点的 Id 属性是否正确设置
3. 检查 WorkflowConnection 的 SourceNodeId 和 TargetNodeId 属性值

### SmartPathConverter.Nodes 集合问题

**症状**:
```
[SmartPathConverter] Nodes count: 0
```

**可能原因**:
1. SmartPathConverter.Nodes 静态属性没有被正确设置
2. WorkflowCanvasControl.DataContextChanged 中没有设置 SmartPathConverter.Nodes

**调查方法**:
1. 检查 SmartPathConverter.Nodes 的设置位置
2. 查找 WorkflowCanvasControl.DataContextChanged 方法
3. 验证 Nodes 集合是否在适当的时候被赋值

## 相关代码位置

### WorkflowConnection 创建
**文件**: `MainWindowViewModel.cs:442-452`

```csharp
WorkflowTabViewModel.SelectedTab.WorkflowConnections.Add(new Models.WorkflowConnection("conn_1", "1", "2")
{
    SourcePosition = new System.Windows.Point(240, 145),
    TargetPosition = new System.Windows.Point(300, 145)
});
```

### WorkflowConnection 构造函数
**文件**: `WorkflowNode.cs:577-585`

```csharp
public WorkflowConnection(string id, string sourceNodeId, string targetNodeId)
{
    Id = id;
    SourceNodeId = sourceNodeId;
    TargetNodeId = targetNodeId;
    SourcePosition = new System.Windows.Point(0, 0);
    TargetPosition = new System.Windows.Point(0, 0);
    ArrowPosition = new System.Windows.Point(0, 0);
}
```

### SmartPathConverter Nodes 集合
**文件**: `SmartPathConverter.cs:21`

```csharp
public static ObservableCollection<WorkflowNode>? Nodes { get; set; }
```

### WorkflowCanvasControl DataContextChanged
**文件**: `WorkflowCanvasControl.xaml.cs:250-255`

需要查找 SmartPathConverter.Nodes 的设置位置。

## 下一步操作

1. **运行测试** - 启动应用并观察新的调试日志
2. **分析日志** - 确定 SourceNodeId 和 TargetNodeId 的实际值
3. **修复问题** - 根据日志分析结果修复根本原因

## 预期日志输出（修复后）

```
[SmartPathConverter] Convert called for connection: conn_1
[SmartPathConverter]   SourceNodeId: '1', TargetNodeId: '2'
[SmartPathConverter]   Nodes count: 3
[SmartPathConverter] ✓ Source node: 1, Target node: 2
[SmartPathConverter] Cache hit for connection: conn_1
[Path_Loaded] ? Path加载，连接ID: conn_1
[Path_Loaded] ? 路径数据: 1个Figure, 3个Segment
[ArrowPath] 连接conn_1 箭头渲染完成: 位置(240.0,145.0), 角度180.0°
```

## 修复检查清单

- [x] 修复第 1 处 Substring 越界
- [x] 修复第 2 处 Substring 越界
- [x] 添加 SmartPathConverter 调试日志
- [ ] 运行测试并收集日志
- [ ] 分析节点 ID 为空的原因
- [ ] 修复 SmartPathConverter.Nodes 集合设置
- [ ] 验证连接线路径正确显示
- [ ] 测试拖拽节点后连接线更新
- [ ] 测试创建新连接
