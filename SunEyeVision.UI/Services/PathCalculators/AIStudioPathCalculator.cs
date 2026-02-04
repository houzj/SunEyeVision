using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 使用 AIStudio.Wpf.Diagram 概念实现的路径计算器
    /// 注意：AIStudio.Wpf.DiagramDesigner 是一个完整的 UI 控件库，
    /// 本实现提供基于其设计理念的简化正交路径计算
    /// </summary>
    public class AIStudioPathCalculator : IPathCalculator
    {
        private bool _isInitialized;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public AIStudioPathCalculator()
        {
            
        }

        /// <summary>
        /// 确保编辑器已初始化
        /// </summary>
        private void EnsureEditorInitialized()
        {
            if (_isInitialized)
                return;

            lock (_lockObject)
            {
                if (_isInitialized)
                    return;

                try
                {
                    

                    // 检查 AIStudio.Wpf.DiagramDesigner 程序集是否可用
                    var assembly = System.Reflection.Assembly.GetAssembly(typeof(AIStudioPathCalculator));
                    if (assembly != null)
                    {
                        
                    }

                    // 尝试加载 AIStudio.Wpf.DiagramDesigner 程序集
                    try
                    {
                        var aiStudioAssembly = System.Reflection.Assembly.Load("AIStudio.Wpf.DiagramDesigner");
                        if (aiStudioAssembly != null)
                        {
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        
                        // 这不是致命错误，我们仍然可以使用简化实现
                    }

                    _isInitialized = true;
                    
                }
                catch (Exception ex)
                {
                    
                    throw;
                }
            }
        }

        /// <summary>
        /// 计算正交路径（基础版本）
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            EnsureEditorInitialized();

            

            try
            {
                var pathList = CalculateSimpleOrthogonalPath(
                    sourcePosition,
                    targetPosition,
                    sourceDirection,
                    targetDirection);

                return pathList.ToArray();
            }
            catch (Exception ex)
            {
                
                // 返回简单的直线路径
                return new Point[] { sourcePosition, targetPosition };
            }
        }

        /// <summary>
        /// 计算正交路径（增强版本，带节点信息）
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            Rect sourceNodeRect,
            Rect targetNodeRect,
            params Rect[] allNodeRects)
        {
            EnsureEditorInitialized();

            

            try
            {
                List<Point> pathList;

                if (allNodeRects != null && allNodeRects.Length > 0)
                {
                    // 使用避障算法
                    var obstacles = new List<Rect>(allNodeRects);
                    pathList = CalculatePathWithObstacleAvoidance(
                        sourcePosition,
                        targetPosition,
                        sourceDirection,
                        targetDirection,
                        obstacles,
                        20);
                }
                else
                {
                    // 使用简化的正交路径算法
                    pathList = CalculateSimpleOrthogonalPath(
                        sourcePosition,
                        targetPosition,
                        sourceDirection,
                        targetDirection);
                }

                
                return pathList.ToArray();
            }
            catch (Exception ex)
            {
                
                // 返回简单的直线路径
                return new Point[] { sourcePosition, targetPosition };
            }
        }

        /// <summary>
        /// 计算简化的正交路径
        /// </summary>
        private List<Point> CalculateSimpleOrthogonalPath(
            Point source,
            Point target,
            PortDirection sourceDir,
            PortDirection targetDir)
        {
            var path = new List<Point> { source };

            double dx = target.X - source.X;
            double dy = target.Y - source.Y;

            // 根据源方向和目标方向决定路径策略
            if (sourceDir.IsHorizontal() && targetDir.IsHorizontal())
            {
                // 都是水平方向
                if (Math.Abs(dy) < 10)
                {
                    // Y轴接近，直接连接
                    path.Add(target);
                }
                else
                {
                    // 需要中间点
                    double midX = source.X + dx / 2;
                    path.Add(new Point(midX, source.Y));
                    path.Add(new Point(midX, target.Y));
                    path.Add(target);
                }
            }
            else if (sourceDir.IsVertical() && targetDir.IsVertical())
            {
                // 都是垂直方向
                if (Math.Abs(dx) < 10)
                {
                    // X轴接近，直接连接
                    path.Add(target);
                }
                else
                {
                    // 需要中间点
                    double midY = source.Y + dy / 2;
                    path.Add(new Point(source.X, midY));
                    path.Add(new Point(target.X, midY));
                    path.Add(target);
                }
            }
            else
            {
                // 一个水平一个垂直
                if (sourceDir.IsHorizontal())
                {
                    // 源水平，目标垂直
                    path.Add(new Point(target.X, source.Y));
                    path.Add(target);
                }
                else
                {
                    // 源垂直，目标水平
                    path.Add(new Point(source.X, target.Y));
                    path.Add(target);
                }
            }

            return path;
        }

        /// <summary>
        /// 计算带避障的正交路径
        /// </summary>
        private List<Point> CalculatePathWithObstacleAvoidance(
            Point source,
            Point target,
            PortDirection sourceDir,
            PortDirection targetDir,
            List<Rect> obstacles,
            double minSegmentLength)
        {
            var path = new List<Point> { source };

            // 简化的避障算法：检查中间点是否在障碍物内
            double midX = source.X + (target.X - source.X) / 2;
            double midY = source.Y + (target.Y - source.Y) / 2;
            var midPoint = new Point(midX, midY);

            bool midPointInObstacle = false;
            foreach (var obstacle in obstacles)
            {
                if (obstacle.Contains(midPoint))
                {
                    midPointInObstacle = true;
                    break;
                }
            }

            if (midPointInObstacle)
            {
                // 如果中间点在障碍物内，尝试绕行
                // 简单策略：向上或向下绕行
                double offsetY = 50; // 绕行距离

                // 尝试向上绕行
                var upPoint = new Point(midX, midY - offsetY);
                bool upPointInObstacle = false;
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.Contains(upPoint))
                    {
                        upPointInObstacle = true;
                        break;
                    }
                }

                if (!upPointInObstacle)
                {
                    // 向上绕行
                    path.Add(new Point(source.X, upPoint.Y));
                    path.Add(new Point(target.X, upPoint.Y));
                }
                else
                {
                    // 向下绕行
                    var downPoint = new Point(midX, midY + offsetY);
                    path.Add(new Point(source.X, downPoint.Y));
                    path.Add(new Point(target.X, downPoint.Y));
                }
            }
            else
            {
                // 使用简化的正交路径
                if (sourceDir.IsHorizontal())
                {
                    path.Add(new Point(target.X, source.Y));
                }
                else
                {
                    path.Add(new Point(source.X, target.Y));
                }
            }

            path.Add(target);

            // 优化路径
            path = OptimizePath(path, minSegmentLength);

            return path;
        }

        /// <summary>
        /// 优化路径：移除共线的中间点
        /// </summary>
        private List<Point> OptimizePath(List<Point> path, double minSegmentLength)
        {
            if (path == null || path.Count <= 2)
                return path;

            var optimizedPath = new List<Point> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
                var next = path[i + 1];

                // 检查是否共线
                bool isHorizontal = Math.Abs(current.Y - prev.Y) < 0.001 && Math.Abs(next.Y - current.Y) < 0.001;
                bool isVertical = Math.Abs(current.X - prev.X) < 0.001 && Math.Abs(next.X - current.X) < 0.001;

                if (!isHorizontal && !isVertical)
                {
                    optimizedPath.Add(current);
                }
            }

            optimizedPath.Add(path[path.Count - 1]);

            // 检查线段长度
            var finalPath = new List<Point> { optimizedPath[0] };
            for (int i = 1; i < optimizedPath.Count; i++)
            {
                var prev = optimizedPath[i - 1];
                var current = optimizedPath[i];
                double distance = Math.Sqrt(Math.Pow(current.X - prev.X, 2) + Math.Pow(current.Y - prev.Y, 2));

                if (distance >= minSegmentLength || i == optimizedPath.Count - 1)
                {
                    finalPath.Add(current);
                }
            }

            return finalPath;
        }

        /// <summary>
        /// 创建路径几何图形
        /// </summary>
        public PathGeometry CreatePathGeometry(Point[] pathPoints)
        {
            if (pathPoints == null || pathPoints.Length < 2)
                return new PathGeometry();

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = pathPoints[0], IsClosed = false };

            for (int i = 1; i < pathPoints.Length; i++)
            {
                figure.Segments.Add(new LineSegment(pathPoints[i], true));
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        /// <summary>
        /// 计算箭头
        /// 箭头尖端位于目标端口位置，角度基于目标端口方向固定
        /// 路径终点已经是箭头尾部位置
        /// </summary>
        public (Point position, double angle) CalculateArrow(Point[] pathPoints, Point targetPosition, PortDirection targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
                return (new Point(0, 0), 0);

            // 箭头尖端位于目标端口位置
            var arrowPosition = targetPosition;

            // 箭头角度基于目标端口方向固定
            // 角度定义：0度指向右，90度指向下，180度指向左，270度指向上
            var arrowAngle = targetDirection switch
            {
                PortDirection.Left => 0.0,     // 左边端口：箭头向右
                PortDirection.Right => 180.0,   // 右边端口：箭头向左
                PortDirection.Top => 90.0,      // 上边端口：箭头向下
                PortDirection.Bottom => 270.0,  // 下边端口：箭头向上
                _ => 0.0
            };

            // 获取路径最后一点用于调试（箭头尾部位置）
            var lastPoint = pathPoints[pathPoints.Length - 1];

            // 关键日志：记录箭头计算结果
            

            return (arrowPosition, arrowAngle);
        }
    }
}
