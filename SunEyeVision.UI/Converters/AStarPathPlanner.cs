using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// A*路径规划�?- 用于复杂场景的网格化路径搜索
    /// </summary>
    public class AStarPathPlanner
    {
        private const double GridSize = 20;
        private readonly PathConfiguration _config;

        public AStarPathPlanner(PathConfiguration config)
        {
            _config = config ?? new PathConfiguration();
        }

        /// <summary>
        /// 查找路径（带路径平滑�?
        /// </summary>
        public List<Point> FindPath(Point start, Point end, List<WorkflowNode> obstacles, PortType targetPort)
        {
            // 转换为网格坐�?
            GridPoint gridStart = WorldToGrid(start);
            GridPoint gridEnd = WorldToGrid(end);

            // 创建障碍物网�?
            bool[,] obstacleGrid = CreateObstacleGrid(obstacles, gridStart, gridEnd);

            // A*搜索
            List<GridPoint> gridPath = AStarSearch(gridStart, gridEnd, obstacleGrid);

            if (gridPath.Count == 0)
            {
                // 搜索失败，返回空路径
                return new List<Point>();
            }

            // 转换回世界坐�?
            var worldPath = gridPath.Select(gp => GridToWorld(gp)).ToList();

            // 路径平滑
            return SmoothPath(worldPath, obstacles, targetPort);
        }

        /// <summary>
        /// 创建障碍物网�?
        /// </summary>
        private bool[,] CreateObstacleGrid(List<WorkflowNode> obstacles, GridPoint start, GridPoint end)
        {
            // 计算网格大小（包含边界）
            int minX = Math.Min(start.X, end.X) - 5;
            int maxX = Math.Max(start.X, end.X) + 5;
            int minY = Math.Min(start.Y, end.Y) - 5;
            int maxY = Math.Max(start.Y, end.Y) + 5;

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            bool[,] grid = new bool[width, height];

            // 标记障碍�?
            foreach (var obstacle in obstacles)
            {
                // 将节点边界转换为网格范围
                int obsMinX = (int)(obstacle.Position.X / GridSize) - minX;
                int obsMaxX = (int)((obstacle.Position.X + _config.NodeWidth) / GridSize) - minX;
                int obsMinY = (int)(obstacle.Position.Y / GridSize) - minY;
                int obsMaxY = (int)((obstacle.Position.Y + _config.NodeHeight) / GridSize) - minY;

                // 标记障碍物单元格
                for (int x = obsMinX - 1; x <= obsMaxX + 1; x++)
                {
                    for (int y = obsMinY - 1; y <= obsMaxY + 1; y++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            grid[x, y] = true;
                        }
                    }
                }
            }

            return grid;
        }

        /// <summary>
        /// A*搜索算法
        /// </summary>
        private List<GridPoint> AStarSearch(GridPoint start, GridPoint end, bool[,] obstacles)
        {
            // 优先队列（使用简单的列表模拟�?
            var openSet = new List<GridPoint> { start };
            var cameFrom = new Dictionary<GridPoint, GridPoint>();
            var gScore = new Dictionary<GridPoint, double> { { start, 0 } };
            var fScore = new Dictionary<GridPoint, double> { { start, Heuristic(start, end) } };

            while (openSet.Count > 0)
            {
                // 获取fScore最小的节点
                GridPoint current = openSet.OrderBy(p => fScore.ContainsKey(p) ? fScore[p] : double.MaxValue).First();

                // 到达目标
                if (current.Equals(end))
                {
                    return ReconstructPath(cameFrom, current);
                }

                openSet.Remove(current);

                // 四方向邻居（上、下、左、右�?
                GridPoint[] neighbors = new GridPoint[]
                {
                    new GridPoint(current.X, current.Y - 1),
                    new GridPoint(current.X, current.Y + 1),
                    new GridPoint(current.X - 1, current.Y),
                    new GridPoint(current.X + 1, current.Y)
                };

                foreach (var neighbor in neighbors)
                {
                    // 检查边界和障碍�?
                    if (neighbor.X < 0 || neighbor.X >= obstacles.GetLength(0) ||
                        neighbor.Y < 0 || neighbor.Y >= obstacles.GetLength(1) ||
                        obstacles[neighbor.X, neighbor.Y])
                    {
                        continue;
                    }

                    // 计算新的gScore
                    double tentativeGScore = gScore[current] + 1;

                    // 如果新路径更�?
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, end);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            // 未找到路�?
            return new List<GridPoint>();
        }

        /// <summary>
        /// 重构路径
        /// </summary>
        private List<GridPoint> ReconstructPath(Dictionary<GridPoint, GridPoint> cameFrom, GridPoint current)
        {
            var path = new List<GridPoint> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// 启发式函数（曼哈顿距离）
        /// </summary>
        private double Heuristic(GridPoint a, GridPoint b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        /// <summary>
        /// 路径平滑 - 减少不必要的转折�?
        /// </summary>
        private List<Point> SmoothPath(List<Point> path, List<WorkflowNode> obstacles, PortType targetPort)
        {
            if (path.Count <= 2)
            {
                return path;
            }

            var smoothedPath = new List<Point> { path[0] };
            int current = 0;

            while (current < path.Count - 1)
            {
                int furthest = current + 1;

                // 找到最远的可达�?
                for (int i = path.Count - 1; i > current; i--)
                {
                    if (!PathIntersectsObstacles(path[current], path[i], obstacles))
                    {
                        furthest = i;
                        break;
                    }
                }

                smoothedPath.Add(path[furthest]);
                current = furthest;
            }

            return smoothedPath;
        }

        /// <summary>
        /// 检查路径是否与障碍物相�?
        /// </summary>
        private bool PathIntersectsObstacles(Point start, Point end, List<WorkflowNode> obstacles)
        {
            foreach (var obstacle in obstacles)
            {
                Rect obstacleBounds = new Rect(
                    obstacle.Position.X - _config.NodeMargin,
                    obstacle.Position.Y - _config.NodeMargin,
                    _config.NodeWidth + 2 * _config.NodeMargin,
                    _config.NodeHeight + 2 * _config.NodeMargin
                );

                if (LineSegmentIntersectsRect(start, end, obstacleBounds))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查线段是否与矩形相交
        /// </summary>
        private bool LineSegmentIntersectsRect(Point p1, Point p2, Rect rect)
        {
            // 快速边界检�?
            double minX = Math.Min(p1.X, p2.X);
            double maxX = Math.Max(p1.X, p2.X);
            double minY = Math.Min(p1.Y, p2.Y);
            double maxY = Math.Max(p1.Y, p2.Y);

            if (maxX < rect.Left || minX > rect.Right ||
                maxY < rect.Top || minY > rect.Bottom)
            {
                return false;
            }

            // 检查四个角�?
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

            return false;
        }

        /// <summary>
        /// 检查两条线段是否相�?
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

        /// <summary>
        /// 世界坐标转网格坐�?
        /// </summary>
        private GridPoint WorldToGrid(Point worldPoint)
        {
            return new GridPoint(
                (int)(worldPoint.X / GridSize),
                (int)(worldPoint.Y / GridSize)
            );
        }

        /// <summary>
        /// 网格坐标转世界坐�?
        /// </summary>
        private Point GridToWorld(GridPoint gridPoint)
        {
            return new Point(
                gridPoint.X * GridSize + GridSize / 2,
                gridPoint.Y * GridSize + GridSize / 2
            );
        }

        /// <summary>
        /// 网格�?
        /// </summary>
        private class GridPoint
        {
            public int X { get; }
            public int Y { get; }

            public GridPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(GridPoint other)
            {
                return X == other.X && Y == other.Y;
            }

        public override bool Equals(object obj)
        {
            return obj is GridPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
        }
    }
}
