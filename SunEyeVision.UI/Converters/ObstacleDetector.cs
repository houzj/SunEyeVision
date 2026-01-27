using System;
using System.Collections.Generic;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 障碍物检测器 - 直接使用所有节点作为障碍物
    /// </summary>
    public class ObstacleDetector
    {
        private readonly PathConfiguration _config;
        private readonly AStarPathPlanner _aStarPlanner;

        public ObstacleDetector(PathConfiguration config)
        {
            _config = config ?? new PathConfiguration();
            _aStarPlanner = new AStarPathPlanner(_config);
        }

        /// <summary>
        /// 重建索引（不再需要，保留接口兼容性）
        /// </summary>
        public void RebuildIndex(IEnumerable<WorkflowNode> nodes)
        {
            // 不再需要四叉树索引，直接使用所有节点
            System.Diagnostics.Debug.WriteLine($"[ObstacleDetector] 跳过四叉树索引构建，将直接使用所有节点作为障碍物");
        }

        /// <summary>
        /// 查找障碍节点（排除源节点和目标节点）
        /// 返回所有可能作为障碍的节点（由调用方根据实际需求进行过滤）
        /// </summary>
        public List<WorkflowNode> FindObstacleNodes(
            Point startPoint,
            Point endPoint,
            WorkflowNode sourceNode,
            WorkflowNode targetNode,
            IEnumerable<WorkflowNode> allNodes = null)
        {
            System.Diagnostics.Debug.WriteLine($"[ObstacleDetector] ========== FindObstacleNodes 开始 ==========");
            System.Diagnostics.Debug.WriteLine($"[ObstacleDetector] 源节点: {sourceNode.Name} (ID={sourceNode.Id})");
            System.Diagnostics.Debug.WriteLine($"[ObstacleDetector] 目标节点: {targetNode.Name} (ID={targetNode.Id})");

            var obstacles = new List<WorkflowNode>();

            // 如果传入了所有节点，则返回所有节点（除了源节点和目标节点）
            if (allNodes != null)
            {
                int candidateCount = 0;
                foreach (var node in allNodes)
                {
                    candidateCount++;

                    // 跳过源节点和目标节点
                    if (node.Id == sourceNode.Id || node.Id == targetNode.Id)
                    {
                        continue;
                    }

                    // 将所有其他节点都作为可能的障碍物
                    obstacles.Add(node);
                }

                System.Diagnostics.Debug.WriteLine($"[ObstacleDetector] 检查了 {candidateCount} 个节点，返回 {obstacles.Count} 个障碍节点（所有节点）");
            }

            System.Diagnostics.Debug.WriteLine($"[ObstacleDetector] ========== FindObstacleNodes 结束 ==========");

            return obstacles;
        }

        /// <summary>
        /// 使用A*查找复杂路径
        /// </summary>
        public List<Point> FindPathWithAStar(
            Point start,
            Point end,
            List<WorkflowNode> obstacles,
            PortType targetPort)
        {
            return _aStarPlanner.FindPath(start, end, obstacles, targetPort);
        }

        /// <summary>
        /// 检查线段是否与任意障碍物相交
        /// </summary>
        public bool HasObstacleIntersection(Point start, Point end, List<WorkflowNode> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
                return false;

            foreach (var obstacle in obstacles)
            {
                double obsLeft = obstacle.Position.X;
                double obsRight = obstacle.Position.X + _config.NodeWidth;
                double obsTop = obstacle.Position.Y;
                double obsBottom = obstacle.Position.Y + _config.NodeHeight;

                Rect obstacleBounds = new Rect(obsLeft, obsTop, _config.NodeWidth, _config.NodeHeight);

                if (LineSegmentIntersectsRect(start, end, obstacleBounds))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 分层检测：先快速检测，再精确检测
        /// </summary>
        public bool LayeredDetection(
            Point start,
            Point end,
            Rect obstacleBounds,
            out bool isDefinite)
        {
            isDefinite = false;

            // 第一层：快速边界框检测
            double minX = Math.Min(start.X, end.X);
            double maxX = Math.Max(start.X, end.X);
            double minY = Math.Min(start.Y, end.Y);
            double maxY = Math.Max(start.Y, end.Y);

            if (maxX < obstacleBounds.Left || minX > obstacleBounds.Right ||
                maxY < obstacleBounds.Top || minY > obstacleBounds.Bottom)
            {
                isDefinite = true;
                return false; // 确定不相交
            }

            // 第二层：精确相交检测
            bool intersects = LineSegmentIntersectsRect(start, end, obstacleBounds);
            isDefinite = true;
            return intersects;
        }

        /// <summary>
        /// 检查线段是否与矩形相交
        /// </summary>
        private bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // 快速边界框检测
            if (!rect.Contains(p1) && !rect.Contains(p2))
            {
                double minX = Math.Min(p1.X, p2.X);
                double maxX = Math.Max(p1.X, p2.X);
                double minY = Math.Min(p1.Y, p2.Y);
                double maxY = Math.Max(p1.Y, p2.Y);

                if (maxX < rect.Left || minX > rect.Right ||
                    maxY < rect.Top || minY > rect.Bottom)
                {
                    return false;
                }
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
        private bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
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

        private double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
        }
    }
}
