using System;
using System.Collections.Generic;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// Libavoid路由配置
    /// </summary>
    public class AvoidRouterConfiguration
    {
        public double IdealSegmentLength { get; set; } = 50.0;
        public double SegmentPenalty { get; set; } = 0.0;
        public double RegionPenalty { get; set; } = 0.0;
        public double CrossingPenalty { get; set; } = 0.0;
        public double FixedSharedPathPenalty { get; set; } = 0.0;
        public double PortDirectionPenalty { get; set; } = 0.0;
        public bool UseOrthogonalRouting { get; set; } = true;
        public bool ImproveHyperedges { get; set; } = true;
        public int RoutingTimeLimit { get; set; } = 5000;
    }

    /// <summary>
    /// Libavoid路由器核心类
    /// 实现正交路径路由和避障算法
    /// </summary>
    public class AvoidRouter
    {
        private readonly AvoidRouterConfiguration _config;
        private readonly List<ShapeRef> _shapes;
        private readonly List<ConnRef> _connectors;
        private uint _nextShapeId = 1;
        private uint _nextConnId = 1;

        public AvoidRouter(AvoidRouterConfiguration config = null)
        {
            _config = config ?? new AvoidRouterConfiguration();
            _shapes = new List<ShapeRef>();
            _connectors = new List<ConnRef>();
        }

        /// <summary>
        /// 创建并添加一个形状（障碍物）
        /// </summary>
        public ShapeRef CreateShape(AvoidPolygon polygon)
        {
            var shape = new ShapeRef(_nextShapeId++, this, polygon);
            _shapes.Add(shape);
            return shape;
        }

        /// <summary>
        /// 创建并添加一个矩形形状
        /// </summary>
        public ShapeRef CreateRectangleShape(AvoidRectangle rectangle)
        {
            var polygon = AvoidPolygon.FromRectangle(rectangle);
            return CreateShape(polygon);
        }

        /// <summary>
        /// 删除形状
        /// </summary>
        public void DeleteShape(ShapeRef shape)
        {
            if (shape != null && _shapes.Contains(shape))
            {
                _shapes.Remove(shape);
                InvalidateConnectorRoutes();
            }
        }

        /// <summary>
        /// 创建并添加一个连接器
        /// </summary>
        public ConnRef CreateConnector(AvoidPoint source, AvoidPoint target)
        {
            var conn = new ConnRef(_nextConnId++, this, source, target);
            _connectors.Add(conn);
            return conn;
        }

        /// <summary>
        /// 删除连接器
        /// </summary>
        public void DeleteConnector(ConnRef connector)
        {
            if (connector != null && _connectors.Contains(connector))
            {
                _connectors.Remove(connector);
            }
        }

        /// <summary>
        /// 获取所有形状
        /// </summary>
        public IReadOnlyList<ShapeRef> GetShapes()
        {
            return _shapes.AsReadOnly();
        }

        /// <summary>
        /// 获取所有连接器
        /// </summary>
        public IReadOnlyList<ConnRef> GetConnectors()
        {
            return _connectors.AsReadOnly();
        }

        /// <summary>
        /// 移动形状
        /// </summary>
        public void MoveShape(ShapeRef shape, AvoidPolygon newPolygon)
        {
            if (shape != null && _shapes.Contains(shape))
            {
                shape.Polygon = newPolygon;
                InvalidateConnectorRoutes();
            }
        }

        /// <summary>
        /// 移动形状（相对移动）
        /// </summary>
        public void MoveShape(ShapeRef shape, double dx, double dy)
        {
            if (shape != null && _shapes.Contains(shape))
            {
                var offset = new AvoidPoint(dx, dy);
                for (int i = 0; i < shape.Polygon.Count; i++)
                {
                    shape.Polygon[i] = shape.Polygon[i] + offset;
                }
                InvalidateConnectorRoutes();
            }
        }

        /// <summary>
        /// 处理事务（批量移动优化）
        /// </summary>
        public void ProcessTransaction()
        {
            InvalidateConnectorRoutes();
        }

        /// <summary>
        /// 使所有连接器的路径失效
        /// </summary>
        private void InvalidateConnectorRoutes()
        {
            foreach (var conn in _connectors)
            {
                conn.Invalidate();
            }
        }

        /// <summary>
        /// 计算路径（正交路由）
        /// </summary>
        internal List<AvoidPoint> RoutePath(AvoidPoint source, AvoidPoint target)
        {
            if (_config.UseOrthogonalRouting)
            {
                return RouteOrthogonalPath(source, target);
            }
            else
            {
                return RoutePolylinePath(source, target);
            }
        }

        /// <summary>
        /// 正交路径路由算法
        /// </summary>
        private List<AvoidPoint> RouteOrthogonalPath(AvoidPoint source, AvoidPoint target)
        {
            var path = new List<AvoidPoint> { source };
            var obstacles = new List<AvoidRectangle>();

            // 收集所有障碍物（排除源和目标所在的形状）
            foreach (var shape in _shapes)
            {
                if (!shape.Polygon.Bounds.Contains(source) && !shape.Polygon.Bounds.Contains(target))
                {
                    obstacles.Add(shape.Polygon.Bounds.Expand(5)); // 添加一点边距
                }
            }

            // 计算初始路径
            CalculateInitialOrthogonalPath(path, source, target);

            // 应用障碍物避让
            if (obstacles.Count > 0)
            {
                ApplyObstacleAvoidance(path, obstacles);
            }

            return path;
        }

        /// <summary>
        /// 计算初始正交路径
        /// </summary>
        private void CalculateInitialOrthogonalPath(List<AvoidPoint> path, AvoidPoint source, AvoidPoint target)
        {
            // 简单的三段式正交路径
            double midX = (source.X + target.X) / 2;
            double midY = (source.Y + target.Y) / 2;

            // 根据相对位置选择策略
            double dx = Math.Abs(target.X - source.X);
            double dy = Math.Abs(target.Y - source.Y);

            if (dx > dy)
            {
                // 水平优先
                path.Add(new AvoidPoint(midX, source.Y));
                path.Add(new AvoidPoint(midX, target.Y));
            }
            else
            {
                // 垂直优先
                path.Add(new AvoidPoint(source.X, midY));
                path.Add(new AvoidPoint(target.X, midY));
            }

            path.Add(target);
        }

        /// <summary>
        /// 应用障碍物避让
        /// </summary>
        private void ApplyObstacleAvoidance(List<AvoidPoint> path, List<AvoidRectangle> obstacles)
        {
            int maxIterations = 10;
            bool hasCollision = true;
            int iteration = 0;

            while (hasCollision && iteration < maxIterations)
            {
                hasCollision = false;
                iteration++;

                // 检查每个线段
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var p1 = path[i];
                    var p2 = path[i + 1];

                    foreach (var obstacle in obstacles)
                    {
                        if (LineIntersectsRectangle(p1, p2, obstacle))
                        {
                            hasCollision = true;
                            var avoidPoint = CalculateAvoidancePoint(p1, p2, obstacle);

                            // 插入避障点
                            path.Insert(i + 1, avoidPoint);
                            i++; // 跳过新插入的点
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 计算避障点
        /// </summary>
        private AvoidPoint CalculateAvoidancePoint(AvoidPoint p1, AvoidPoint p2, AvoidRectangle obstacle)
        {
            double offsetX = obstacle.Width / 2 + 10;
            double offsetY = obstacle.Height / 2 + 10;

            // 判断线段方向
            double dx = Math.Abs(p2.X - p1.X);
            double dy = Math.Abs(p2.Y - p1.Y);

            if (dx > dy)
            {
                // 水平线段，垂直避让
                double y = p1.Y < obstacle.Top ? obstacle.Top - offsetY : obstacle.Bottom + offsetY;
                return new AvoidPoint((p1.X + p2.X) / 2, y);
            }
            else
            {
                // 垂直线段，水平避让
                double x = p1.X < obstacle.Left ? obstacle.Left - offsetX : obstacle.Right + offsetX;
                return new AvoidPoint(x, (p1.Y + p2.Y) / 2);
            }
        }

        /// <summary>
        /// 检查线段是否与矩形相交
        /// </summary>
        private bool LineIntersectsRectangle(AvoidPoint p1, AvoidPoint p2, AvoidRectangle rect)
        {
            // 检查端点是否在矩形内
            if (rect.Contains(p1) || rect.Contains(p2))
                return true;

            // 检查线段是否与矩形边界相交
            return LineIntersectsLine(p1, p2,
                new AvoidPoint(rect.Left, rect.Top),
                new AvoidPoint(rect.Right, rect.Top)) ||
                   LineIntersectsLine(p1, p2,
                new AvoidPoint(rect.Right, rect.Top),
                new AvoidPoint(rect.Right, rect.Bottom)) ||
                   LineIntersectsLine(p1, p2,
                new AvoidPoint(rect.Right, rect.Bottom),
                new AvoidPoint(rect.Left, rect.Bottom)) ||
                   LineIntersectsLine(p1, p2,
                new AvoidPoint(rect.Left, rect.Bottom),
                new AvoidPoint(rect.Left, rect.Top));
        }

        /// <summary>
        /// 检查两条线段是否相交
        /// </summary>
        private bool LineIntersectsLine(AvoidPoint p1, AvoidPoint p2, AvoidPoint p3, AvoidPoint p4)
        {
            double d = (p2.X - p1.X) * (p4.Y - p3.Y) - (p4.X - p3.X) * (p2.Y - p1.Y);

            if (d == 0)
                return false;

            double u = ((p3.X - p1.X) * (p4.Y - p3.Y) - (p4.X - p3.X) * (p3.Y - p1.Y)) / d;
            double v = ((p3.X - p1.X) * (p2.Y - p1.Y) - (p2.X - p1.X) * (p3.Y - p1.Y)) / d;

            return u >= 0 && u <= 1 && v >= 0 && v <= 1;
        }

        /// <summary>
        /// 折线路径路由（非正交）
        /// </summary>
        private List<AvoidPoint> RoutePolylinePath(AvoidPoint source, AvoidPoint target)
        {
            return new List<AvoidPoint> { source, target };
        }
    }
}
