using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
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
                return string.Empty;

            try
            {
                // 根据 ID 查找源节点和目标节点
                WorkflowNode? sourceNode = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                WorkflowNode? targetNode = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                if (sourceNode == null || targetNode == null)
                    return string.Empty;

                // 计算起点和终点（节点中心，假设节点大小为 180x80）
                const double NodeWidth = 180;
                const double NodeHeight = 80;
                Point startPoint = new Point(sourceNode.Position.X + NodeWidth / 2, sourceNode.Position.Y + NodeHeight / 2);
                Point endPoint = new Point(targetNode.Position.X + NodeWidth / 2, targetNode.Position.Y + NodeHeight / 2);

                // 生成路径数据
                string pathData = GeneratePathData(startPoint, endPoint, sourceNode, targetNode);
                return pathData;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 生成路径数据
        /// </summary>
        private string GeneratePathData(Point start, Point end, WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            // 简单的 L 形路径
            Point midPoint1 = new Point(start.X, start.Y + (end.Y - start.Y) / 2);
            Point midPoint2 = new Point(end.X, start.Y + (end.Y - start.Y) / 2);

            return $"M {start.X},{start.Y} C {midPoint1.X},{midPoint1.Y} {midPoint2.X},{midPoint2.Y} {end.X},{end.Y}";
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
