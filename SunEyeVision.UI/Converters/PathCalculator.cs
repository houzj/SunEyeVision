using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 线段辅助类，提供亚像素级相交检测
    /// </summary>
    public static class LineSegmentHelper
    {
        /// <summary>
        /// 检查线段是否与矩形相交（亚像素级）
        /// </summary>
        /// <param name="p1">线段起点</param>
        /// <param name="p2">线段终点</param>
        /// <param name="rect">矩形</param>
        /// <param name="tolerance">容差值，默认0.1像素</param>
        /// <returns>是否相交</returns>
        public static bool LineIntersectsRect(Point p1, Point p2, Rect rect, double tolerance = 0.1)
        {
            // 获取线段的端点
            // Point p1 = segment.StartPoint;
            // Point p2 = segment.EndPoint;

            // 检查线段是否完全在矩形内
            if (p1.X >= rect.Left && p1.X <= rect.Right && p1.Y >= rect.Top && p1.Y <= rect.Bottom &&
                p2.X >= rect.Left && p2.X <= rect.Right && p2.Y >= rect.Top && p2.Y <= rect.Bottom)
            {
                return true;
            }

            // 检查线段的端点是否在矩形内（考虑容差）
            bool p1Inside = p1.X >= rect.Left - tolerance && p1.X <= rect.Right + tolerance &&
                          p1.Y >= rect.Top - tolerance && p1.Y <= rect.Bottom + tolerance;
            bool p2Inside = p2.X >= rect.Left - tolerance && p2.X <= rect.Right + tolerance &&
                          p2.Y >= rect.Top - tolerance && p2.Y <= rect.Bottom + tolerance;

            if (p1Inside || p2Inside)
            {
                return true;
            }

            // 检查线段与矩形四条边的交点（考虑容差）
            Point[] edges = new Point[]
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            for (int i = 0; i < 4; i++)
            {
                if (SegmentsIntersectWithTolerance(p1, p2, edges[i], edges[(i + 1) % 4], tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查两条线段是否相交（考虑容差）
        /// </summary>
        /// <param name="p1">第一条线段的起点</param>
        /// <param name="p2">第一条线段的终点</param>
        /// <param name="p3">第二条线段的起点</param>
        /// <param name="p4">第二条线段的终点</param>
        /// <param name="tolerance">容差值</param>
        /// <returns>是否相交</returns>
        private static bool SegmentsIntersectWithTolerance(Point p1, Point p2, Point p3, Point p4, double tolerance)
        {
            // 扩展线段以考虑容差
            Point extP1 = new Point(p1.X - tolerance, p1.Y - tolerance);
            Point extP2 = new Point(p2.X + tolerance, p2.Y + tolerance);
            Point extP3 = new Point(p3.X - tolerance, p3.Y - tolerance);
            Point extP4 = new Point(p4.X + tolerance, p4.Y + tolerance);

            return SegmentsIntersect(extP1, extP2, extP3, extP4);
        }

        /// <summary>
        /// 检查两条线段是否相交
        /// </summary>
        private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double cross1 = CrossProduct(p3, p4, p1);
            double cross2 = CrossProduct(p3, p4, p2);
            double cross3 = CrossProduct(p1, p2, p3);
            double cross4 = CrossProduct(p1, p2, p4);

            if (((cross1 > 0 && cross2 < 0) || (cross1 < 0 && cross2 > 0)) &&
                ((cross3 > 0 && cross4 < 0) || (cross3 < 0 && cross4 > 0)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算叉积
        /// </summary>
        private static double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
        }
    }

    /// <summary>
    /// 路径计算器- 集成所有路径计算组件
    /// 使用策略模式消除代码冗余，提供统一的路径计算接口
    /// </summary>
    public class PathCalculator
    {
        private readonly List<IPathStrategy> _strategies;
        private readonly PathConfiguration _config;
        private readonly PathCacheManager _cacheManager;
        private readonly ObstacleDetector _obstacleDetector;
        private readonly NodeRelationshipAnalyzer _relationshipAnalyzer;
        private readonly PathValidator _pathValidator;
        private IEnumerable<WorkflowNode> _allNodes = Enumerable.Empty<WorkflowNode>();

        public PathCalculator(PathConfiguration config = null)
        {
            _config = config ?? new PathConfiguration();
            _cacheManager = new PathCacheManager(100);
            _obstacleDetector = new ObstacleDetector(_config);
            _relationshipAnalyzer = new NodeRelationshipAnalyzer(_config);
            _pathValidator = new PathValidator(_config);

            // 初始化策略链
            _strategies = new List<IPathStrategy>
            {
                new DirectPathStrategy(_config, _relationshipAnalyzer, _pathValidator),
                new DetourPathStrategy(_config, _relationshipAnalyzer, _pathValidator)
            };
        }

        /// <summary>
        /// 重建障碍物索引（存储所有节点）
        /// </summary>
        public void RebuildObstacleIndex(IEnumerable<WorkflowNode> nodes)
        {
            _allNodes = nodes;
            _obstacleDetector.RebuildIndex(nodes);
        }

        /// <summary>
        /// 计算路径（带缓存）
        /// </summary>
        public List<Point> CalculatePath(
            Point startPoint,
            Point endPoint,
            Point arrowTailPoint,
            WorkflowNode sourceNode,
            WorkflowNode targetNode,
            string sourcePortStr,
            string targetPortStr)
        {
            // 转换端口类型
            PortType sourcePort = ParsePortType(sourcePortStr);
            PortType targetPort = ParsePortType(targetPortStr);

            // 生成缓存键
            string cacheKey = PathCacheManager.GenerateCacheKey(
                sourceNode.Id,
                targetNode.Id,
                startPoint,
                arrowTailPoint,
                sourcePort, // 使用实际源端口
                targetPort
            );

            // 尝试从缓存获取
            if (_cacheManager.TryGetPath(cacheKey, out var cachedPath))
            {
                return cachedPath;
            }

            // 查找障碍物（直接使用所有节点进行过滤）
            var obstacles = _obstacleDetector.FindObstacleNodes(
                startPoint,
                arrowTailPoint,
                sourceNode,
                targetNode,
                _allNodes
            );

            // 创建路径上下文
            var context = new PathContext
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                ArrowTailPoint = arrowTailPoint,
                SourcePort = sourcePort, // 使用实际源端口
                TargetPort = targetPort,
                SourceNode = sourceNode,
                TargetNode = targetNode,
                Obstacles = obstacles,
                Config = _config
            };

            // ========== 关键调试信息：策略选择 ==========
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] ========== 策略选择 ==========");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 源节点: {sourceNode.Name} (ID={sourceNode.Id})");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 目标节点: {targetNode.Name} (ID={targetNode.Id})");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 源端口: {sourcePortStr}");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 目标端口: {targetPortStr}");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 障碍节点数量: {obstacles?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 路径起点: {startPoint}");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 连接终点: {arrowTailPoint}");

            // 选择并执行策略
            List<Point> pathPoints = null;
            bool pathValidated = false;
            string selectedStrategy = "无";

            foreach (var strategy in _strategies)
            {
                string strategyName = strategy.GetType().Name;
                bool canHandle = strategy.CanHandle(context);
                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 策略 {strategyName} CanHandle: {canHandle}");

                if (canHandle)
                {
                    selectedStrategy = strategyName;
                    System.Diagnostics.Debug.WriteLine($"[PathCalculator] ✓ 选择策略: {strategyName}");
                    pathPoints = strategy.CalculatePath(context);

                    // 验证路径是否有效
                    if (pathPoints != null && pathPoints.Count > 0)
                    {
                        pathValidated = _pathValidator.QuickValidate(pathPoints, context);
                        System.Diagnostics.Debug.WriteLine($"[PathCalculator] 路径验证结果: {pathValidated}");

                        // 注意：A*回退在这种场景下不适用（源/目标节点会作为障碍物）
                        // DirectPathStrategy应该自身生成正确的直线路径
                    }

                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 最终选择的策略: {selectedStrategy}");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] 生成的路径点数: {pathPoints?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[PathCalculator] =======================================");

            // 缓存结果
            if (pathPoints != null && pathValidated)
            {
                _cacheManager.CachePath(cacheKey, pathPoints);
            }

            // 添加穿透检测调试信息
            if (_config.EnableDebugLog && pathPoints != null && pathPoints.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("[PathCalculator] ========== 穿透检测 ==========");
                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 路径点数: {pathPoints.Count}");

                double targetLeft = context.TargetNode.Position.X;
                double targetRight = context.TargetNode.Position.X + _config.NodeWidth;
                double targetTop = context.TargetNode.Position.Y;
                double targetBottom = context.TargetNode.Position.Y + _config.NodeHeight;

                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 目标节点边界: X=[{targetLeft:F1}, {targetRight:F1}], Y=[{targetTop:F1}, {targetBottom:F1}]");

                // 构建完整路径
                var fullPath = new List<Point> { context.StartPoint };
                fullPath.AddRange(pathPoints);
                fullPath.Add(context.ArrowTailPoint);

                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 完整路径包含 {fullPath.Count} 个点:");
                for (int i = 0; i < fullPath.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"[PathCalculator]   点{i + 1}: {fullPath[i]}");
                }

                int penetrationCount = 0;
                for (int i = 0; i < fullPath.Count - 1; i++)
                {
                    Point p1 = fullPath[i];
                    Point p2 = fullPath[i + 1];
                    bool penetrates = CheckPenetration(p1, p2, targetLeft, targetRight, targetTop, targetBottom);

                    if (penetrates)
                    {
                        penetrationCount++;
                        System.Diagnostics.Debug.WriteLine($"[PathCalculator] ❌ 线段{i + 1}: ({p1} -> {p2}) 穿过目标节点!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PathCalculator] ✓ 线段{i + 1}: ({p1} -> {p2}) 正常");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 检测结果: 发现{penetrationCount}个线段穿过目标节点");
                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 路径验证结果: {pathValidated}");
                System.Diagnostics.Debug.WriteLine($"[PathCalculator] 最终返回点数: {pathPoints.Count}");
                System.Diagnostics.Debug.WriteLine($"[PathCalculator] ========================================");
            }

            // 修复：即使验证失败也返回路径，而不是返回空列表
            // 这样至少能显示计算的直角折线
            if (pathPoints != null && pathPoints.Count > 0)
            {
                return pathPoints;
            }

            return new List<Point>();
        }

        /// <summary>
        /// 检查线段是否穿过矩形
        /// </summary>
        private bool CheckPenetration(Point p1, Point p2, double left, double right, double top, double bottom)
        {
            // 检查端点是否在矩形内
            bool p1Inside = p1.X >= left && p1.X <= right && p1.Y >= top && p1.Y <= bottom;
            bool p2Inside = p2.X >= left && p2.X <= right && p2.Y >= top && p2.Y <= bottom;

            if (p1Inside || p2Inside)
            {
                return true;
            }

            // 检查线段与边界的交点
            Point[] corners = new Point[]
            {
                new Point(left, top),
                new Point(right, top),
                new Point(right, bottom),
                new Point(left, bottom)
            };

            for (int i = 0; i < 4; i++)
            {
                if (LineSegmentsIntersect(p1, p2, corners[i], corners[(i + 1) % 4]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查两条线段是否相交
        /// </summary>
        private bool LineSegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double cross1 = CrossProduct(p3, p4, p1);
            double cross2 = CrossProduct(p3, p4, p2);
            double cross3 = CrossProduct(p1, p2, p3);
            double cross4 = CrossProduct(p1, p2, p4);

            if (((cross1 > 0 && cross2 < 0) || (cross1 < 0 && cross2 > 0)) &&
                ((cross3 > 0 && cross4 < 0) || (cross3 < 0 && cross4 > 0)))
            {
                return true;
            }

            return false;
        }

        private double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
        }

        /// <summary>
        /// 使节点相关的所有连接失效
        /// </summary>
        public void InvalidateNode(string nodeId)
        {
            _cacheManager.InvalidateNode(nodeId);
        }

        /// <summary>
        /// 使连接失效
        /// </summary>
        public void InvalidateConnection(string connectionId)
        {
            _cacheManager.InvalidateConnection(connectionId);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            _cacheManager.Clear();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStatistics()
        {
            return _cacheManager.GetStatistics();
        }

        /// <summary>
        /// 解析端口类型
        /// </summary>
        private PortType ParsePortType(string portStr)
        {
            return portStr switch
            {
                "TopPort" => PortType.TopPort,
                "BottomPort" => PortType.BottomPort,
                "LeftPort" => PortType.LeftPort,
                "RightPort" => PortType.RightPort,
                _ => PortType.Unknown
            };
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            _cacheManager?.Clear();
        }
    }
}
