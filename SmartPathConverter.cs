using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 智能路径转换器 - 基于亚像素优化的路径计算和转换
    /// </summary>
    public class SmartPathConverter : IMultiValueConverter
    {
        // 静态节点和连接集合，用于路径计算
        public static List<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
        public static List<WorkflowConnection> Connections { get; set; } = new List<WorkflowConnection>();

        // 路径计算器
        private readonly PathCalculator _pathCalculator;

        public SmartPathConverter()
        {
            _pathCalculator = new PathCalculator();
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 4)
                return null;

            // 获取连接信息
            var connection = values[0] as WorkflowConnection;
            if (connection == null)
                return null;

            // 获取源节点和目标节点
            var sourceNode = FindNodeById(connection.SourceNodeId);
            var targetNode = FindNodeById(connection.TargetNodeId);

            if (sourceNode == null || targetNode == null)
                return null;

            // 计算路径点
            var pathPoints = CalculatePathWithSubPixelOptimization(connection, sourceNode, targetNode);

            // 创建路径几何
            var pathGeometry = CreatePathGeometry(pathPoints, connection.ArrowTailPoint);

            return pathGeometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 使用亚像素优化计算路径
        /// </summary>
        private List<Point> CalculatePathWithSubPixelOptimization(WorkflowConnection connection, WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            var pathPoints = new List<Point>();
            
            // 使用亚像素优化的路径计算
            var startPoint = connection.SourcePosition;
            var endPoint = connection.TargetPosition;
            var arrowTailPoint = connection.ArrowTailPoint;

            // 计算智能直角折线路径
            pathPoints = CalculateSmartPath(startPoint, endPoint);

            // 应用亚像素平滑
            pathPoints = SmoothPathPoints(pathPoints);

            // 确保路径包含箭头尾点
            if (pathPoints.Count > 0 && pathPoints[pathPoints.Count - 1] != arrowTailPoint)
            {
                pathPoints.Add(arrowTailPoint);
            }

            return pathPoints;
        }

        /// <summary>
        /// 计算智能直角折线路径（与实际连接使用相同的逻辑）
        /// </summary>
        private List<Point> CalculateSmartPath(Point start, Point end)
        {
            var points = new List<Point>();
            double deltaX = Math.Abs(end.X - start.X);
            double deltaY = Math.Abs(end.Y - start.Y);

            bool isHorizontal = deltaX > deltaY;

            if (isHorizontal)
            {
                // 水平方向：先水平移动到中间点，再垂直移动到目标Y，最后水平移动到目标X
                double midX = (start.X + end.X) / 2;
                points.Add(new Point(midX, start.Y));
                points.Add(new Point(midX, end.Y));
            }
            else
            {
                // 垂直方向：先垂直移动到中间点，再水平移动到目标X，最后垂直移动到目标Y
                double midY = (start.Y + end.Y) / 2;
                points.Add(new Point(start.X, midY));
                points.Add(new Point(end.X, midY));
            }

            points.Add(end);
            return points;
        }

        /// <summary>
        /// 使用贝塞尔曲线平滑路径点
        /// </summary>
        private List<Point> SmoothPathPoints(List<Point> originalPoints)
        {
            if (originalPoints == null || originalPoints.Count < 2)
                return originalPoints;

            var smoothedPoints = new List<Point>();
            smoothedPoints.Add(originalPoints[0]);

            for (int i = 1; i < originalPoints.Count - 1; i++)
            {
                Point prev = originalPoints[i - 1];
                Point curr = originalPoints[i];
                Point next = originalPoints[i + 1];

                // 计算平滑点（贝塞尔曲线控制点）
                double smoothX = curr.X + (next.X - prev.X) * 0.25;
                double smoothY = curr.Y + (next.Y - prev.Y) * 0.25;

                smoothedPoints.Add(new Point(smoothX, smoothY));
            }

            smoothedPoints.Add(originalPoints[originalPoints.Count - 1]);
            return smoothedPoints;
        }

        /// <summary>
        /// 创建路径几何
        /// </summary>
        private PathGeometry CreatePathGeometry(List<Point> pathPoints, Point arrowTailPoint)
        {
            if (pathPoints == null || pathPoints.Count == 0)
                return new PathGeometry();

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = pathPoints[0],
                IsClosed = false
            };

            // 添加路径段
            for (int i = 1; i < pathPoints.Count; i++)
            {
                pathFigure.Segments.Add(new LineSegment(pathPoints[i], true));
            }

            pathGeometry.Figures.Add(pathFigure);

            // 添加箭头
            if (pathPoints.Count > 1)
            {
                AddArrowToPathGeometry(pathGeometry, pathPoints[pathPoints.Count - 2], arrowTailPoint);
            }

            return pathGeometry;
        }

        /// <summary>
        /// 添加箭头到路径几何
        /// </summary>
        private void AddArrowToPathGeometry(PathGeometry pathGeometry, Point lastPathPoint, Point arrowPosition)
        {
            var arrowFigure = new PathFigure
            {
                StartPoint = arrowPosition,
                IsClosed = true
            };

            // 简单的三角形箭头
            arrowFigure.Segments.Add(new LineSegment(new Point(arrowPosition.X - 8, arrowPosition.Y - 5), true));
            arrowFigure.Segments.Add(new LineSegment(new Point(arrowPosition.X - 8, arrowPosition.Y + 5), true));

            pathGeometry.Figures.Add(arrowFigure);
        }

        /// <summary>
        /// 根据ID查找节点
        /// </summary>
        private WorkflowNode FindNodeById(string nodeId)
        {
            return Nodes.Find(node => node.Id == nodeId);
        }

        /// <summary>
        /// 刷新所有连接的路径
        /// </summary>
        public static void RefreshAllConnections()
        {
            // 触发所有连接的属性变化，重新计算路径
            foreach (var connection in Connections)
            {
                connection.SourcePosition = new Point(connection.SourcePosition.X + 0.001, connection.SourcePosition.Y);
                connection.SourcePosition = new Point(connection.SourcePosition.X - 0.001, connection.SourcePosition.Y);
            }
        }
    }
}