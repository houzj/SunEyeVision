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
        /// 连接线路径缓存（静态）
        /// </summary>
        public static Services.ConnectionPathCache? PathCache { get; set; }

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
                // System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] value is WorkflowConnection: {value is WorkflowConnection}, Nodes is null: {Nodes == null}");
                return string.Empty;
            }

            // 🔥 减少日志输出以提高性能
            // System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] Convert called for connection: {connection.Id}");
            // System.Diagnostics.Debug.WriteLine($"[SmartPathConverter]   SourceNodeId: '{connection.SourceNodeId}', TargetNodeId: '{connection.TargetNodeId}'");
            // System.Diagnostics.Debug.WriteLine($"[SmartPathConverter]   Nodes count: {Nodes.Count}");

            try {
                // 根据 ID 查找源节点和目标节点
                WorkflowNode? sourceNode = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                WorkflowNode? targetNode = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                if (sourceNode == null || targetNode == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] ❌ Source node: {sourceNode?.Id ?? "null"}, Target node: {targetNode?.Id ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"[SmartPathConverter]   Available node IDs: {string.Join(", ", Nodes.Take(5).Select(n => $"'{n.Id}'"))}...");
                    return string.Empty;
                }

                // 计算起点和终点（节点中心，假设节点大小为 180x80）
                const double NodeWidth = 180;
                const double NodeHeight = 80;
                Point startPoint = new Point(sourceNode.Position.X + NodeWidth / 2, sourceNode.Position.Y + NodeHeight / 2);
                Point endPoint = new Point(targetNode.Position.X + NodeWidth / 2, targetNode.Position.Y + NodeHeight / 2);

                // 尝试从缓存获取路径数据
                if (PathCache != null)
                {
                    var cachedPathData = PathCache.GetPathData(connection);
                    if (!string.IsNullOrEmpty(cachedPathData))
                    {
                        // 🔥 减少日志输出以提高性能
                        // System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] Cache hit for connection: {connection.Id}");
                        return cachedPathData;
                    }
                    // else
                    // {
                    //     System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] Cache miss or empty data for connection: {connection.Id}");
                    // }
                }
                // else
                // {
                //     System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] PathCache is null for connection: {connection.Id}");
                // }

                // 生成路径数据
                string pathData = GeneratePathData(startPoint, endPoint, sourceNode, targetNode);

                // System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] Generated path data for connection {connection.Id}: {pathData.Substring(0, Math.Min(50, pathData.Length))}...");

                // 不在这里缓存，由 ConnectionPathService 负责

                return pathData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] Exception for connection {connection.Id}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SmartPathConverter] Stack trace: {ex.StackTrace}");
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 生成路径数据（生成正交折线，而不是贝塞尔曲线）
        /// </summary>
        private string GeneratePathData(Point start, Point end, WorkflowNode sourceNode, WorkflowNode targetNode)
        {
            // 计算节点中心位置（用于确定端口方向）
            double sourceCenterX = sourceNode.Position.X + 180 / 2;  // 节点宽度 180
            double sourceCenterY = sourceNode.Position.Y + 80 / 2;   // 节点高度 80
            double targetCenterX = targetNode.Position.X + 180 / 2;
            double targetCenterY = targetNode.Position.Y + 80 / 2;

            // 判断端口方向（简化逻辑：根据相对位置判断）
            bool isHorizontal = Math.Abs(start.X - sourceCenterX) > Math.Abs(start.Y - sourceCenterY);

            System.Collections.Generic.List<string> points = new System.Collections.Generic.List<string>();
            points.Add($"M {start.X:F1},{start.Y:F1}");

            // 生成正交折线
            if (isHorizontal)
            {
                // 水平优先策略
                double midY = start.Y + (end.Y - start.Y) / 2;
                points.Add($"L {end.X:F1},{midY:F1}");
                points.Add($"L {end.X:F1},{end.Y:F1}");
            }
            else
            {
                // 垂直优先策略
                double midX = start.X + (end.X - start.X) / 2;
                points.Add($"L {midX:F1},{start.Y:F1}");
                points.Add($"L {midX:F1},{end.Y:F1}");
                points.Add($"L {end.X:F1},{end.Y:F1}");
            }

            return string.Join(" ", points);
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
