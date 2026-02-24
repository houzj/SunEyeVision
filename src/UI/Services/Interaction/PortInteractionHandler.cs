using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Services.Rendering;

namespace SunEyeVision.UI.Services.Interaction
{
    /// <summary>
    /// 工作流端口交互处理器
    /// 负责端口的鼠标事件处理、连接创建等交互
    /// </summary>
    public class WorkflowPortInteractionHandler
    {
        private readonly WorkflowCanvasControl _canvasControl;
        private readonly MainWindowViewModel? _viewModel;
        private readonly WorkflowConnectionManager _connectionManager;
        private readonly WorkflowNodeInteractionHandler _nodeInteractionHandler;

        // 连接拖拽相关
        private bool _isDraggingConnection;
        private Ellipse? _dragSourcePort;
        private WorkflowNode? _dragSourceNode;
        private System.Windows.Point _dragConnectionStartPoint;
        private int _dragMoveCounter;

        // 目标端口高亮相关
        private Ellipse? _highlightedTargetPort;

        public WorkflowPortInteractionHandler(
            WorkflowCanvasControl canvasControl,
            MainWindowViewModel? viewModel,
            WorkflowConnectionManager connectionManager,
            WorkflowNodeInteractionHandler nodeInteractionHandler)
        {
            _canvasControl = canvasControl;
            _viewModel = viewModel;
            _connectionManager = connectionManager;
            _nodeInteractionHandler = nodeInteractionHandler;
        }

        /// <summary>
        /// 端口鼠标左键按下 - 开始拖拽连�?
        /// </summary>
        public void Port_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse port || port.Tag is not WorkflowNode node)
                return;

            _isDraggingConnection = true;
            _dragSourcePort = port;
            _dragSourceNode = node;
            _dragMoveCounter = 0;

            // 获取端口位置
            var portPosition = port.TransformToVisual(_canvasControl.WorkflowCanvas)
                .Transform(new System.Windows.Point(port.ActualWidth / 2, port.ActualHeight / 2));
            _dragConnectionStartPoint = portPosition;

            // 显示临时连接�?
            _canvasControl.TempConnectionLine.Visibility = Visibility.Visible;
            
            // 使用 PathGeometry 更新临时连接�?
            var pathGeometry = new System.Windows.Media.PathGeometry();
            var pathFigure = new System.Windows.Media.PathFigure();
            pathFigure.StartPoint = portPosition;
            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(portPosition, true));
            pathGeometry.Figures.Add(pathFigure);
            _canvasControl.TempConnectionLine.Data = pathGeometry;

            port.CaptureMouse();
            e.Handled = true;
        }

        /// <summary>
        /// 端口鼠标左键释放 - 结束拖拽连接
        /// </summary>
        public void Port_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingConnection || _dragSourceNode == null || _dragSourcePort == null)
                return;

            _isDraggingConnection = false;
            _canvasControl.TempConnectionLine.Visibility = Visibility.Collapsed;

            // 清除目标端口高亮
            ClearTargetPortHighlight();

            // 查找鼠标位置下的目标节点
            var mousePos = e.GetPosition(_canvasControl.WorkflowCanvas);
            var targetNode = HitTestForNode(mousePos);

            if (targetNode != null && targetNode != _dragSourceNode)
            {
                // 查找目标端口
                var targetPort = HitTestForPort(mousePos, targetNode);

                if (targetPort != null)
                {
                    // 使用指定端口创建连接
                    string sourcePortName = _dragSourcePort.Name ?? "RightPort";
                    string targetPortName = targetPort.Name ?? "LeftPort";
                    _connectionManager.CreateConnectionWithSpecificPort(
                        _dragSourceNode, targetNode, targetPortName, sourcePortName);
                }
                else
                {
                    // 智能选择目标端口
                    string sourcePortName = _dragSourcePort.Name ?? "RightPort";
                    _connectionManager.CreateConnection(_dragSourceNode, targetNode, sourcePortName);
                }
            }

            _dragSourcePort.ReleaseMouseCapture();
            _dragSourcePort = null;
            _dragSourceNode = null;
            e.Handled = true;
        }

        /// <summary>
        /// 端口鼠标移动 - 更新临时连接�?
        /// </summary>
        public void Port_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingConnection || _dragSourcePort == null)
                return;

            _dragMoveCounter++;
            if (_dragMoveCounter < 3) // 性能优化：跳过前几次移动
                return;

            var currentPos = e.GetPosition(_canvasControl.WorkflowCanvas);

            // 更新临时连接�?
            var pathGeometry = new System.Windows.Media.PathGeometry();
            var pathFigure = new System.Windows.Media.PathFigure();
            pathFigure.StartPoint = _dragConnectionStartPoint;
            pathFigure.Segments.Add(new System.Windows.Media.LineSegment(currentPos, true));
            pathGeometry.Figures.Add(pathFigure);
            _canvasControl.TempConnectionLine.Data = pathGeometry;

            // 查找目标节点和端�?
            var targetNode = HitTestForNode(currentPos);
            if (targetNode != null && targetNode != _dragSourceNode)
            {
                var targetPort = HitTestForPort(currentPos, targetNode);

                if (targetPort != null)
                {
                    // 高亮指定端口
                    HighlightSpecificPort(targetPort);
                }
                else
                {
                    // 高亮最近的目标端口
                    HighlightTargetPort(targetNode, _dragSourcePort);
                }
            }
            else
            {
                ClearTargetPortHighlight();
            }

            e.Handled = true;
        }

        /// <summary>
        /// HitTest 查找目标节点
        /// </summary>
        private WorkflowNode? HitTestForNode(System.Windows.Point position)
        {
            var hitResult = VisualTreeHelper.HitTest(_canvasControl.WorkflowCanvas, position);
            if (hitResult?.VisualHit is DependencyObject obj)
            {
                // 向上查找 Border（节点容器）
                while (obj != null)
                {
                    if (obj is Border border && border.Tag is WorkflowNode node)
                    {
                        return node;
                    }
                    obj = VisualTreeHelper.GetParent(obj);
                }
            }
            return null;
        }

        /// <summary>
        /// HitTest 查找目标端口
        /// </summary>
        private Ellipse? HitTestForPort(System.Windows.Point position, WorkflowNode targetNode)
        {
            var hitResult = VisualTreeHelper.HitTest(_canvasControl.WorkflowCanvas, position);
            if (hitResult?.VisualHit is DependencyObject obj)
            {
                // 向上查找 Ellipse（端口）
                while (obj != null)
                {
                    if (obj is Ellipse ellipse && ellipse.Name?.Contains("Port") == true)
                    {
                        // 检查是否属于目标节�?
                        var parent = VisualTreeHelper.GetParent(ellipse);
                        while (parent != null)
                        {
                            if (parent is Border border && border.Tag == targetNode)
                            {
                                return ellipse;
                            }
                            parent = VisualTreeHelper.GetParent(parent);
                        }
                        break;
                    }
                    obj = VisualTreeHelper.GetParent(obj);
                }
            }
            return null;
        }

        /// <summary>
        /// 高亮目标端口
        /// </summary>
        private void HighlightTargetPort(WorkflowNode targetNode, Ellipse sourcePort)
        {
            var sourcePortName = sourcePort.Name ?? "";
            bool isSourceInput = sourcePortName.Contains("Input");

            // 获取目标节点的所有端�?
            var targetNodeElement = _canvasControl.WorkflowCanvas.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Tag == targetNode);

            if (targetNodeElement == null) return;

            var targetPorts = WorkflowVisualHelper.FindAllVisualChildren<Ellipse>(targetNodeElement)
                .Where(e => e.Name?.Contains("Port") == true)
                .ToList();

            if (targetPorts.Count == 0) return;

            // 根据源端口方向选择目标端口
            Ellipse? targetPort = null;
            if (isSourceInput)
            {
                // 源是输入端口，目标应该是输出端口
                targetPort = targetPorts.FirstOrDefault(p => p.Name?.Contains("Output") == true);
            }
            else
            {
                // 源是输出端口，目标应该是输入端口
                targetPort = targetPorts.FirstOrDefault(p => p.Name?.Contains("Input") == true);
            }

            // 如果没有找到对应方向的端口，选择第一个端�?
            targetPort ??= targetPorts.First();

            HighlightSpecificPort(targetPort);
        }

        /// <summary>
        /// 高亮指定端口
        /// </summary>
        private void HighlightSpecificPort(Ellipse port)
        {
            if (_highlightedTargetPort == port) return;

            ClearTargetPortHighlight();

            _highlightedTargetPort = port;
            _highlightedTargetPort.StrokeThickness = 3;
            _highlightedTargetPort.Stroke = new SolidColorBrush(Colors.LimeGreen);
        }

        /// <summary>
        /// 清除目标端口高亮
        /// </summary>
        private void ClearTargetPortHighlight()
        {
            if (_highlightedTargetPort != null)
            {
                _highlightedTargetPort.StrokeThickness = 1;
                _highlightedTargetPort.Stroke = new SolidColorBrush(Colors.Gray);
                _highlightedTargetPort = null;
            }
        }
    }
}
