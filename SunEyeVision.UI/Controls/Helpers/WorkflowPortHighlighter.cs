using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.Helpers
{
    /// <summary>
    /// 工作流端口高亮管理器 - 负责端口的高亮显示和清除
    /// </summary>
    public class WorkflowPortHighlighter
    {
        private readonly MainWindowViewModel? _viewModel;
        private Ellipse? _highlightedTargetPort;
        private Border? _highlightedTargetBorder;
        private Brush? _originalPortFill;
        private Brush? _originalPortStroke;
        private double _originalPortStrokeThickness;
        private string _lastHighlightedPort = "";
        private int _highlightCounter = 0;

        public WorkflowPortHighlighter(MainWindowViewModel? viewModel)
        {
            _viewModel = viewModel;
        }

        /// <summary>
        /// 高亮显示目标端口（根据源端口和目标节点位置智能选择）
        /// </summary>
        public void HighlightTargetPort(Border? nodeBorder, WorkflowNode? sourceNode, string sourcePortName)
        {
            // 先取消之前的高亮
            ClearTargetPortHighlight();

            if (nodeBorder == null || sourceNode == null) return;

            // 获取源端口的实际位置
            Point sourcePos = GetPortPosition(sourceNode, sourcePortName);

            var targetNode = nodeBorder.Tag as WorkflowNode;
            if (targetNode == null) return;

            var targetPos = targetNode.Position;
            var deltaX = targetPos.X - sourcePos.X;
            var deltaY = targetPos.Y - sourcePos.Y;

            // 根据源端口方向和相对位置选择目标端口
            string targetPortName = DetermineTargetPort(sourcePortName, deltaX, deltaY);

            // 只在端口变化或每10次高亮时输出日志
            bool shouldLog = _lastHighlightedPort != targetPortName || _highlightCounter % 10 == 0;
            if (shouldLog)
            {
                _lastHighlightedPort = targetPortName;
            }
            _highlightCounter++;

            // 获取端口元素并高亮
            HighlightSpecificPort(nodeBorder, targetPortName);
        }

        /// <summary>
        /// 高亮指定的端口（用于直接命中端口的情况）
        /// </summary>
        public void HighlightSpecificPort(Border nodeBorder, string portName)
        {
            ClearTargetPortHighlight();

            if (nodeBorder == null) return;

            var portElement = GetPortElement(nodeBorder, portName);
            if (portElement != null)
            {
                // 确保端口可见且响应鼠标事件
                portElement.Visibility = Visibility.Visible;
                portElement.Opacity = 1.0;

                _highlightedTargetPort = portElement;
                _highlightedTargetBorder = nodeBorder;

                // 保存原始样式
                _originalPortFill = portElement.Fill;
                _originalPortStroke = portElement.Stroke;
                _originalPortStrokeThickness = portElement.StrokeThickness;

                // 设置高亮样式
                portElement.Fill = new SolidColorBrush(Color.FromRgb(255, 200, 0)); // 金色填充
                portElement.Stroke = new SolidColorBrush(Color.FromRgb(255, 100, 0)); // 深橙色边框
                portElement.StrokeThickness = 3;

                // 只在端口变化时记录日志
                if (_lastHighlightedPort != portName && _highlightCounter % 5 == 0)
                {
    
                }
            }
        }

        /// <summary>
        /// 清除目标端口的高亮
        /// </summary>
        public void ClearTargetPortHighlight()
        {
            if (_highlightedTargetPort != null && _originalPortFill != null)
            {
                // 恢复原始样式
                _highlightedTargetPort.Fill = _originalPortFill;
                _highlightedTargetPort.Stroke = _originalPortStroke ?? new SolidColorBrush(Colors.Transparent);
                _highlightedTargetPort.StrokeThickness = _originalPortStrokeThickness;

                // 隐藏端口，确保不响应鼠标事件
                _highlightedTargetPort.Visibility = Visibility.Collapsed;
            }

            _highlightedTargetPort = null;
            _originalPortFill = null;
            _originalPortStroke = null;
            _originalPortStrokeThickness = 0;
        }

        /// <summary>
        /// 获取节点指定端口的Ellipse元素
        /// </summary>
        private Ellipse? GetPortElement(Border nodeBorder, string portName)
        {
            if (nodeBorder == null) return null;

            // 根据端口名称构造Ellipse名称（例如："LeftPort" -> "LeftPortEllipse"）
            string ellipseName = portName + "Ellipse";

            // 在节点Border的视觉树中查找指定名称的端口
            var visualChildren = WorkflowVisualHelper.FindAllVisualChildren<DependencyObject>(nodeBorder);

            // 只在第一次查找失败时输出日志
            bool found = false;
            // 查找包含端口名称的元素（通过Name属性或Tag）
            foreach (var child in visualChildren)
            {
                if (child is FrameworkElement element && element.Name == ellipseName)
                {
                    if (!found && _highlightCounter % 20 == 0) // 每20次高亮才输出一次
                    {
        
                    }
                    return element as Ellipse;
                }
            }

            if (_highlightCounter % 20 == 0) // 每20次高亮才输出一次
            {

            }
            return null;
        }

        /// <summary>
        /// 获取节点指定端口的位置
        /// </summary>
        private Point GetPortPosition(WorkflowNode node, string portName)
        {
            return portName switch
            {
                "TopPort" => node.TopPortPosition,
                "BottomPort" => node.BottomPortPosition,
                "LeftPort" => node.LeftPortPosition,
                "RightPort" => node.RightPortPosition,
                _ => node.RightPortPosition
            };
        }

        /// <summary>
        /// 根据源端口和相对位置确定目标端口
        /// </summary>
        private string DetermineTargetPort(string sourcePortName, double deltaX, double deltaY)
        {
            string targetPortName = "LeftPort"; // 默认

            // 根据源端口方向和相对位置选择目标端口
            // 策略：优先选择与源端口方向对应的目标端口，但允许根据实际位置调整
            string direction = "";
            bool isVerticalDominant = sourcePortName == "TopPort" || sourcePortName == "BottomPort";

            if (isVerticalDominant)
            {
                // 源端口是垂直方向（Top/Bottom），优先选择垂直方向的目标端口
                // 但如果水平偏移远大于垂直偏移（2倍以上），则选择水平方向
                if (Math.Abs(deltaX) > 2 * Math.Abs(deltaY))
                {
                    direction = "水平（源垂直但水平偏移过大）";
                    if (deltaX > 0)
                        targetPortName = "LeftPort";
                    else
                        targetPortName = "RightPort";
                }
                else
                {
                    direction = "垂直（源端口主导）";
                    if (deltaY > 0)
                        targetPortName = "TopPort";
                    else
                        targetPortName = "BottomPort";
                }
            }
            else
            {
                // 源端口是水平方向（Left/Right），优先选择水平方向的目标端口
                // 但如果垂直偏移远大于水平偏移（2倍以上），则选择垂直方向
                if (Math.Abs(deltaY) > 2 * Math.Abs(deltaX))
                {
                    direction = "垂直（源水平但垂直偏移过大）";
                    if (deltaY > 0)
                        targetPortName = "TopPort";
                    else
                        targetPortName = "BottomPort";
                }
                else
                {
                    direction = "水平（源端口主导）";
                    if (deltaX > 0)
                        targetPortName = "LeftPort";
                    else
                        targetPortName = "RightPort";
                }
            }

            return targetPortName;
        }
    }
}