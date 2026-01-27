using System;
using System.Collections.Generic;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 路径日志记录器 - 用于调试路径计算问题
    /// </summary>
    public static class PathLogger
    {
        public static bool EnableDebugLog { get; set; } = true;

        /// <summary>
        /// 记录路径计算开始
        /// </summary>
        public static void LogPathCalculationStart(Point startPoint, Point arrowTailPoint,
            WorkflowNode sourceNode, WorkflowNode targetNode, string targetPort)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine("[PathLogger] ==========================================");
            System.Diagnostics.Debug.WriteLine("[PathLogger] ========== 路径计算开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[PathLogger] 源节点: {sourceNode?.Name} (ID: {sourceNode?.Id})");
            System.Diagnostics.Debug.WriteLine($"[PathLogger] 目标节点: {targetNode?.Name} (ID: {targetNode?.Id})");
            System.Diagnostics.Debug.WriteLine($"[PathLogger] 目标端口: {targetPort}");
            System.Diagnostics.Debug.WriteLine($"[PathLogger] 路径起点: {startPoint}");
            System.Diagnostics.Debug.WriteLine($"[PathLogger] 箭头尾部位置(连接线终点): {arrowTailPoint}");
            if (targetNode != null)
            {
                System.Diagnostics.Debug.WriteLine($"[PathLogger] 目标节点边界: X={targetNode.Position.X:F1}, Y={targetNode.Position.Y:F1}");
            }
            System.Diagnostics.Debug.WriteLine("[PathLogger] ==========================================");
        }

        /// <summary>
        /// 记录障碍物信息
        /// </summary>
        public static void LogObstacles(List<WorkflowNode> obstacles)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine($"[PathLogger] 发现障碍物数量: {obstacles.Count}");
            foreach (var obs in obstacles)
            {
                System.Diagnostics.Debug.WriteLine($"[PathLogger]   障碍: {obs.Name} (ID: {obs.Id}, Pos: {obs.Position})");
            }
        }

        /// <summary>
        /// 记录使用的策略
        /// </summary>
        public static void LogStrategyUsed(string strategyName)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine($"[PathLogger] 使用策略: {strategyName}");
        }

        /// <summary>
        /// 记录策略返回的路径点
        /// </summary>
        public static void LogStrategyResult(List<Point> pathPoints)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine($"[PathLogger] 策略返回路径点数: {pathPoints?.Count ?? 0}");
            if (pathPoints != null)
            {
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"[PathLogger]   转折点{i + 1}: {pathPoints[i]}");
                }
            }
        }

        /// <summary>
        /// 记录路径验证结果
        /// </summary>
        public static void LogPathValidation(bool isValid)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine($"[PathLogger] 路径验证结果: {(isValid ? "通过" : "失败")}");
            if (!isValid)
            {
                System.Diagnostics.Debug.WriteLine($"[PathLogger] ⚠️ 警告: 路径验证失败，将返回空路径");
            }
        }

        /// <summary>
        /// 记录最终结果
        /// </summary>
        public static void LogFinalResult(List<Point> pathPoints, bool isValid)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine($"[PathLogger] 最终返回: {(isValid ? "有效路径" : "空路径")}，点数: {pathPoints?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine("[PathLogger] ==========================================");
        }

        /// <summary>
        /// 执行穿透检测
        /// </summary>
        public static void CheckPathPenetration(Point startPoint, List<Point> pathPoints, Point arrowTailPoint,
            WorkflowNode targetNode, double nodeWidth, double nodeHeight)
        {
            if (!EnableDebugLog) return;

            System.Diagnostics.Debug.WriteLine("[PathLogger] ========== 穿透检测 ==========");

            if (targetNode == null)
            {
                System.Diagnostics.Debug.WriteLine("[PathLogger] 目标节点为null，跳过检测");
                return;
            }

            var targetBounds = new Rect(targetNode.Position.X, targetNode.Position.Y, nodeWidth, nodeHeight);
            System.Diagnostics.Debug.WriteLine($"[PathLogger] 目标节点边界: X=[{targetBounds.Left:F1}, {targetBounds.Right:F1}], Y=[{targetBounds.Top:F1}, {targetBounds.Bottom:F1}]");

            // 构建完整路径
            var fullPath = new List<Point> { startPoint };
            fullPath.AddRange(pathPoints);
            fullPath.Add(arrowTailPoint);

            int penetrationCount = 0;

            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                bool intersects = LineSegmentIntersectsRect(fullPath[i], fullPath[i + 1], targetBounds);
                if (intersects)
                {
                    penetrationCount++;
                    System.Diagnostics.Debug.WriteLine($"[PathLogger] ⚠️ 线段{i + 1}: ({fullPath[i]} -> {fullPath[i + 1]}) 穿过目标节点!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PathLogger] ✓ 线段{i + 1}: ({fullPath[i]} -> {fullPath[i + 1]}) 正常");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[PathLogger] 穿透检测结果: 发现{penetrationCount}个线段穿过目标节点");
            System.Diagnostics.Debug.WriteLine("[PathLogger] ==========================================");
        }

        /// <summary>
        /// 检查线段是否与矩形相交
        /// </summary>
        private static bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // 快速边界检查
            Rect segmentBounds = new Rect(p1, p2);
            if (segmentBounds.Right < rect.Left || segmentBounds.Left > rect.Right ||
                segmentBounds.Bottom < rect.Top || segmentBounds.Top > rect.Bottom)
            {
                return false;
            }

            // 检查四个角点
            Point[] corners = new Point[]
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            for (int i = 0; i < 4; i++)
            {
                if (SegmentsIntersect(p1, p2, corners[i], corners[(i + 1) % 4]))
                {
                    return true;
                }
            }

            // 检查线段端点是否在矩形内
            if (rect.Contains(p1) || rect.Contains(p2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查两条线段是否相交
        /// </summary>
        private static bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double d1 = CrossProduct(p3, p4, p1);
            double d2 = CrossProduct(p3, p4, p2);
            double d3 = CrossProduct(p1, p2, p3);
            double d4 = CrossProduct(p1, p2, p4);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            return false;
        }

        private static double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
        }
    }
}
