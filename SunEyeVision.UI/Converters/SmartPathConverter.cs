using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 智能路径转换器 - 计算节点间的最佳连接路径
    /// 优化：使用路径计算服务、四叉树索引、智能缓存
    /// </summary>
    public class SmartPathConverter : IValueConverter
    {
        public static IEnumerable<WorkflowNode> Nodes { get; set; } = Enumerable.Empty<WorkflowNode>();
        public static IEnumerable<WorkflowConnection> Connections { get; set; } = Enumerable.Empty<WorkflowConnection>();

        // 存储连接的箭头角度
        public static Dictionary<string, double> ConnectionAngles { get; set; } = new Dictionary<string, double>();

        // 路径计算服务实例（静态，共享缓存）
        private static PathCalculationService _pathService;

        public double ControlOffset { get; set; } = 60;
        public double GridSize { get; set; } = 20;
        public double NodeMargin { get; set; } = 35;

        // 固定箭头大小为10px
        public double ArrowSize { get; set; } = 10;

        // 是否启用详细日志
        public bool EnableDebugLog { get; set; } = false;

        // 节点尺寸（用于碰撞检测和路径规划）
        private const double NodeWidth = 140;
        private const double NodeHeight = 90;

        // 路径偏移量（用于避开节点）
        public double PathOffset { get; set; } = 25;

        // 构造函数- 初始化路径计算服务
        public SmartPathConverter()
        {
            if (_pathService == null)
            {
                var config = new SunEyeVision.UI.Services.PathConfiguration
                {
                    ControlOffset = ControlOffset,
                    GridSize = GridSize,
                    NodeMargin = NodeMargin,
                    ArrowSize = ArrowSize,
                    PathOffset = PathOffset,
                    NodeWidth = NodeWidth,
                    NodeHeight = NodeHeight,
                    EnableDebugLog = EnableDebugLog
                };
                _pathService = new PathCalculationService(config);
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine($"[SmartPathConverter] 开始路径转换，输入值类型: {value?.GetType().Name ?? "null"}");
            
            // 重建障碍物索引（每次计算前重建，确保索引是最新的）
            if (Nodes != null)
            {
                Console.WriteLine($"[SmartPathConverter] 重建障碍物索引，节点数量: {Nodes.Count()}");
                _pathService?.RebuildObstacleIndex(Nodes);
            }

            if (value is WorkflowConnection connection)
            {
                Console.WriteLine($"[SmartPathConverter] 处理连接: {connection.Id}，源节点: {connection.SourceNodeId}，目标节点: {connection.TargetNodeId}");
                
                var sourceNode = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                var targetNode = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                if (sourceNode != null && targetNode != null)
                {
                    Console.WriteLine($"[SmartPathConverter] 找到源节点: {sourceNode.Name}，目标节点: {targetNode.Name}");
                    
                    // 计算并保存箭头角度 - 基于目标端口方向
                    var angle = _pathService.CalculateArrowAngle(connection.TargetPort);
                    ConnectionAngles[connection.Id] = angle;
                    connection.ArrowAngle = angle;
                    Console.WriteLine($"[SmartPathConverter] 计算箭头角度: {angle:F2}度，目标端口: {connection.TargetPort}");

                    // 获取端口位置（箭头顶点位置）
                    Point targetPortPos = GetTargetPortPosition(targetNode, connection.TargetPort);
                    Console.WriteLine($"[SmartPathConverter] 目标端口位置: ({targetPortPos.X:F2}, {targetPortPos.Y:F2})");

                    // 修复关键：ArrowPosition 应该是箭头顶点的位置（端口位置），而不是箭头尾部位置
                    // XAML 中箭头几何形状的起点 (0,0) 是箭头顶点，通过 Canvas.Left/Top 定位
                    connection.ArrowPosition = targetPortPos;

                    // 计算连接线终点（箭头尾部位置）
                    Point arrowTailPoint = _pathService.CalculateAdjustedEndPoint(connection.SourcePosition, targetPortPos, targetNode, connection.TargetPort);
                    Console.WriteLine($"[SmartPathConverter] 箭头尾部位置: ({arrowTailPoint.X:F2}, {arrowTailPoint.Y:F2})");
                    Console.WriteLine($"[SmartPathConverter] 源位置: ({connection.SourcePosition.X:F2}, {connection.SourcePosition.Y:F2})");

                    // 源位置和目标位置已经是绝对坐标，直接使用
                    // 注意：使用 arrowTailPoint 作为终点，确保连接线终点是箭头尾部位置
                    var pathGeometry = CreateSmartPath(
                                connection.SourcePosition,
                                arrowTailPoint,
                                sourceNode,
                                targetNode,
                                connection.SourcePort,
                                connection.TargetPort,
                                connection);

                    Console.WriteLine($"[SmartPathConverter] 路径转换完成");
                    return pathGeometry;
                }
                else
                {
                    Console.WriteLine($"[SmartPathConverter] 未找到源节点或目标节点");
                }
            }

            Console.WriteLine($"[SmartPathConverter] 返回空路径几何");
            return new PathGeometry();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 清除路径缓存
        /// </summary>
        public static void ClearPathCache()
        {
            _pathService?.ClearCache();
        }

        /// <summary>
        /// 使与节点相关的所有连接失效
        /// </summary>
        public static void InvalidateNode(string nodeId)
        {
            _pathService?.InvalidateNode(nodeId);
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static string GetCacheStatistics()
        {
            return _pathService?.GetCacheStatistics() ?? "PathCalculationService not initialized";
        }

        private PathGeometry CreateSmartPath(Point startPoint, Point endPoint, WorkflowNode sourceNode, WorkflowNode targetNode, string sourcePort, string targetPort, Models.WorkflowConnection connection)
        {
            Console.WriteLine($"[SmartPathConverter] 开始创建智能路径，起点: ({startPoint.X:F2}, {startPoint.Y:F2})，终点: ({endPoint.X:F2}, {endPoint.Y:F2})");
            
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = startPoint };

            // 使用优化的路径计算服务
            Console.WriteLine($"[SmartPathConverter] 调用路径计算服务，源端口: {sourcePort}，目标端口: {targetPort}");
            var pathPoints = _pathService.CalculatePath(
                startPoint,
                endPoint,
                endPoint,
                sourceNode,
                targetNode,
                sourcePort,
                targetPort
            );

            Console.WriteLine($"[SmartPathConverter] 路径计算服务返回 {pathPoints.Count} 个转折点");
            
            // 保存路径点到连接对象（用于显示中间点）
            connection.PathPoints.Clear();
            foreach (var point in pathPoints)
            {
                connection.PathPoints.Add(point);
                Console.WriteLine($"[SmartPathConverter] 添加路径点: ({point.X:F2}, {point.Y:F2})");
            }

            // 将转折点转换为线段
            foreach (var point in pathPoints)
            {
                figure.Segments.Add(new LineSegment(point, true));
            }

            // 添加最后一段到终点（箭头尾部位置）
            figure.Segments.Add(new LineSegment(endPoint, true));
            Console.WriteLine($"[SmartPathConverter] 添加最后一段到终点: ({endPoint.X:F2}, {endPoint.Y:F2})");

            geometry.Figures.Add(figure);
            Console.WriteLine($"[SmartPathConverter] 智能路径创建完成，线段数量: {figure.Segments.Count}");

            return geometry;
        }

        /// <summary>
        /// 获取目标端口的位置
        /// </summary>
        private Point GetTargetPortPosition(WorkflowNode node, string port)
        {
            return port switch
            {
                "TopPort" => node.TopPortPosition,
                "BottomPort" => node.BottomPortPosition,
                "LeftPort" => node.LeftPortPosition,
                "RightPort" => node.RightPortPosition,
                _ => new Point(node.Position.X + 70, node.Position.Y + 45) // 默认中心点
            };
        }
    }
}
