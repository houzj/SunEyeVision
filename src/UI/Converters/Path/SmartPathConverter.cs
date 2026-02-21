using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services;
using SunEyeVision.UI.Services.Connection;
using SunEyeVision.UI.Services.Node;

namespace SunEyeVision.UI.Converters.Path
{
    /// <summary>
    /// 智能路径转换器 - 将 WorkflowConnection 转换为 Path Data
    /// </summary>
    public class SmartPathConverter : IValueConverter
    {
        /// <summary>
        /// 节点集合（静态）
        /// </summary>
        public static ObservableCollection<WorkflowNode>? Nodes { get; set; }

        /// <summary>
        /// 连接集合（静态）
        /// </summary>
        public static ObservableCollection<WorkflowConnection>? Connections { get; set; }

        /// <summary>
        /// 连接线路径缓存（静态）
        /// </summary>
        public static ConnectionPathCache? PathCache { get; set; }

        /// <summary>
        /// 控件偏移量
        /// </summary>
        public double ControlOffset { get; set; } = 60;

        /// <summary>
        /// 网格大小
        /// </summary>
        public double GridSize { get; set; } = 20;

        /// <summary>
        /// 节点边距
        /// </summary>
        public double NodeMargin { get; set; } = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not WorkflowConnection connection || Nodes == null)
            {
                return string.Empty;
            }

            try {
                // 根据 ID 查找源节点和目标节点
                WorkflowNode? sourceNode = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                WorkflowNode? targetNode = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                if (sourceNode == null || targetNode == null)
                {
                    return string.Empty;
                }

                // 修复：优先使用PathCache获取路径数据（PathCache使用BezierPathCalculator）
                if (PathCache != null)
                {
                    var cachedPathData = PathCache.GetPathData(connection);
                    if (!string.IsNullOrEmpty(cachedPathData))
                    {
                        return cachedPathData;
                    }
                }

                // 降级方案：如果没有PathCache或缓存未命中，使用GeneratePathData生成简单路径
                // 计算起点和终点（节点中心，假设节点大小为 180x80）
                const double NodeWidth = 180;
                const double NodeHeight = 80;
                Point startPoint = new Point(sourceNode.Position.X + NodeWidth / 2, sourceNode.Position.Y + NodeHeight / 2);
                Point endPoint = new Point(targetNode.Position.X + NodeWidth / 2, targetNode.Position.Y + NodeHeight / 2);

                // 生成路径数据
                string pathData = GeneratePathData(startPoint, endPoint, sourceNode, targetNode);

                return pathData;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 生成路径数据（生成贝塞尔曲线）
        /// </summary>
        private string GeneratePathData(Point start, Point end, WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            // 计算节点中心位置（用于确定端口方向）
            double sourceCenterX = sourceNode.Position.X + 180 / 2;  // 节点宽度 180
            double sourceCenterY = sourceNode.Position.Y + 80 / 2;   // 节点高度 80

            // 判断端口方向（简化逻辑：根据相对位置判断）
            PortDirection sourceDirection = DeterminePortDirection(start, new Point(sourceCenterX, sourceCenterY));

            // 计算贝塞尔曲线控制点
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // 控制点偏移比例（与BezierPathCalculator保持一致）
            const double ControlPointOffsetRatio = 0.4;
            const double MinOffset = 20.0;
            double controlOffset = Math.Max(distance * ControlPointOffsetRatio, MinOffset);

            // 计算控制点1（靠近源点）
            Point controlPoint1 = sourceDirection switch
            {
                PortDirection.Right => new Point(start.X + controlOffset, start.Y),
                PortDirection.Left => new Point(start.X - controlOffset, start.Y),
                PortDirection.Top => new Point(start.X, start.Y - controlOffset),
                PortDirection.Bottom => new Point(start.X, start.Y + controlOffset),
                _ => new Point(start.X + controlOffset, start.Y)
            };

            // 简化：控制点2使用与控制点1对称的位置
            Point controlPoint2 = new Point(
                end.X - (controlPoint1.X - start.X),
                end.Y - (controlPoint1.Y - start.Y)
            );

            // 生成贝塞尔曲线路径数据
            // 格式：M start C controlPoint1 controlPoint2 end
            return $"M {start.X:F1},{start.Y:F1} C {controlPoint1.X:F1},{controlPoint1.Y:F1} {controlPoint2.X:F1},{controlPoint2.Y:F1} {end.X:F1},{end.Y:F1}";
        }

        /// <summary>
        /// 确定端口方向
        /// </summary>
        private PortDirection DeterminePortDirection(Point portPosition, Point nodeCenter)
        {
            double dx = portPosition.X - nodeCenter.X;
            double dy = portPosition.Y - nodeCenter.Y;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? PortDirection.Right : PortDirection.Left;
            }
            else
            {
                return dy > 0 ? PortDirection.Bottom : PortDirection.Top;
            }
        }

        /// <summary>
        /// 判断两个点是否过近
        /// </summary>
        private bool ArePointsClose(Point p1, Point p2, double threshold = 5)
        {
            return Math.Abs(p1.X - p2.X) < threshold && Math.Abs(p1.Y - p2.Y) < threshold;
        }
    }
}
