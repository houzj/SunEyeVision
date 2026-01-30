using System;
using System.Windows;
using System.Windows.Media;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 正交折线路径计算器 - 实现基于端口方向的智能正交路径算法
    /// </summary>
    public class OrthogonalPathCalculator : IPathCalculator
    {
        private const double MinSegmentLength = 30.0; // 最小线段长度，避免过短的折线
        private const double ArrowLength = 15.0; // 箭头长度

        /// <summary>
        /// 路径策略枚举
        /// </summary>
        private enum PathStrategy
        {
            /// <summary>
            /// 水平优先策略 - 优先从源端口沿水平方向延伸
            /// </summary>
            HorizontalFirst,

            /// <summary>
            /// 垂直优先策略 - 优先从源端口沿垂直方向延伸
            /// </summary>
            VerticalFirst,

            /// <summary>
            /// 三段式策略 - 简单的三段折线（水平-垂直-水平或垂直-水平-垂直）
            /// </summary>
            ThreeSegment,

            /// <summary>
            /// 五段式策略 - 复杂的五段折线，适用于特殊场景
            /// </summary>
            FiveSegment
        }

        /// <summary>
        /// 计算正交折线路径点集合
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            // 1. 计算源节点和目标节点的相对位置关系
            var dx = targetPosition.X - sourcePosition.X;
            var dy = targetPosition.Y - sourcePosition.Y;
            var horizontalDistance = Math.Abs(dx);
            var verticalDistance = Math.Abs(dy);

            // 2. 调整目标位置，考虑箭头长度（路径终点应该减去箭头长度）
            var adjustedTargetPosition = AdjustTargetPositionForArrow(
                targetPosition,
                targetDirection);

            // 关键日志：记录目标位置调整
            if (Math.Abs(targetPosition.X - adjustedTargetPosition.X) > 0.1 ||
                Math.Abs(targetPosition.Y - adjustedTargetPosition.Y) > 0.1)
            {
                System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 目标位置调整: 原始({targetPosition.X:F1},{targetPosition.Y:F1}) -> 调整后({adjustedTargetPosition.X:F1},{adjustedTargetPosition.Y:F1}), 箭头长度:{ArrowLength}");
            }

            // 3. 重新计算相对位置关系
            dx = adjustedTargetPosition.X - sourcePosition.X;
            dy = adjustedTargetPosition.Y - sourcePosition.Y;

            // 4. 选择最佳路径策略
            var strategy = SelectPathStrategy(
                sourceDirection,
                targetDirection,
                dx,
                dy,
                horizontalDistance,
                verticalDistance);

            // 5. 根据策略计算路径点
            return CalculatePathByStrategy(
                sourcePosition,
                adjustedTargetPosition,
                sourceDirection,
                targetDirection,
                strategy,
                dx,
                dy);
        }

        /// <summary>
        /// 调整目标位置以考虑箭头长度
        /// 路径终点应该在箭头尾部位置（远离目标端口），这样箭头从路径终点指向目标端口
        /// 箭头从路径终点延伸到目标端口
        /// </summary>
        private Point AdjustTargetPositionForArrow(Point targetPosition, PortDirection targetDirection)
        {
            // 路径终点应该在目标端口外侧（远离节点），箭头尾部位于路径终点，尖端指向目标端口
            return targetDirection switch
            {
                // 右端口：箭头向右（0°），路径终点在目标端口右侧，向右偏移15px
                PortDirection.Right => new Point(targetPosition.X + ArrowLength, targetPosition.Y),

                // 左端口：箭头向左（180°），路径终点在目标端口左侧，向左偏移15px
                PortDirection.Left => new Point(targetPosition.X - ArrowLength, targetPosition.Y),

                // 上端口：箭头向上（270°），路径终点在目标端口上方，向上偏移15px
                PortDirection.Top => new Point(targetPosition.X, targetPosition.Y - ArrowLength),

                // 下端口：箭头向下（90°），路径终点在目标端口下方，向下偏移15px
                PortDirection.Bottom => new Point(targetPosition.X, targetPosition.Y + ArrowLength),

                _ => targetPosition
            };
        }

        /// <summary>
        /// 选择最佳路径策略
        /// </summary>
        private PathStrategy SelectPathStrategy(
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx, double dy,
            double horizontalDistance,
            double verticalDistance)
        {
            PathStrategy strategy;

            // 场景1: 相邻对向端口（如Left->Right, Top->Bottom）且距离较近
            if (IsOppositeDirection(sourceDirection, targetDirection) &&
                IsCloseDistance(horizontalDistance, verticalDistance))
            {
                strategy = PathStrategy.ThreeSegment;
            }

            // 场景2: 同向端口（如Right->Right）或正交端口（如Right->Bottom）
            if (IsSameDirection(sourceDirection, targetDirection) ||
                IsPerpendicularDirection(sourceDirection, targetDirection))
            {
                // 根据相对位置决定优先方向
                if (sourceDirection.IsHorizontal())
                {
                    // 源端口是水平的，看是否需要先水平延伸
                    if (horizontalDistance > verticalDistance)
                    {
                        return PathStrategy.HorizontalFirst;
                    }
                    else
                    {
                        return PathStrategy.VerticalFirst;
                    }
                }
                else
                {
                    // 源端口是垂直的
                    if (verticalDistance > horizontalDistance)
                    {
                        return PathStrategy.VerticalFirst;
                    }
                    else
                    {
                        return PathStrategy.HorizontalFirst;
                    }
                }
            }

            // 场景3: 默认使用源端口方向的优先策略
            else
            {
                if (sourceDirection.IsHorizontal())
                {
                    strategy = PathStrategy.HorizontalFirst;
                }
                else
                {
                    strategy = PathStrategy.VerticalFirst;
                }
            }

            // 关键日志：记录路径策略选择
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 路径策略: {strategy}, 源方向:{sourceDirection}, 目标方向:{targetDirection}, 距离H:{horizontalDistance:F0} V:{verticalDistance:F0}");

            return strategy;
        }

        /// <summary>
        /// 判断两个端口方向是否相反（对向）
        /// </summary>
        private bool IsOppositeDirection(PortDirection dir1, PortDirection dir2)
        {
            return (dir1 == PortDirection.Left && dir2 == PortDirection.Right) ||
                   (dir1 == PortDirection.Right && dir2 == PortDirection.Left) ||
                   (dir1 == PortDirection.Top && dir2 == PortDirection.Bottom) ||
                   (dir1 == PortDirection.Bottom && dir2 == PortDirection.Top);
        }

        /// <summary>
        /// 判断距离是否较近（小于最小线段长度）
        /// </summary>
        private bool IsCloseDistance(double horizontalDistance, double verticalDistance)
        {
            return horizontalDistance < MinSegmentLength * 2 && verticalDistance < MinSegmentLength * 2;
        }

        /// <summary>
        /// 判断两个端口方向是否相同
        /// </summary>
        private bool IsSameDirection(PortDirection dir1, PortDirection dir2)
        {
            return dir1 == dir2;
        }

        /// <summary>
        /// 判断两个端口方向是否正交（一个水平一个垂直）
        /// </summary>
        private bool IsPerpendicularDirection(PortDirection dir1, PortDirection dir2)
        {
            return (dir1.IsHorizontal() && dir2.IsVertical()) ||
                   (dir1.IsVertical() && dir2.IsHorizontal());
        }

        /// <summary>
        /// 根据策略计算路径点
        /// </summary>
        private Point[] CalculatePathByStrategy(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            PathStrategy strategy,
            double dx,
            double dy)
        {
            switch (strategy)
            {
                case PathStrategy.HorizontalFirst:
                    return CalculateHorizontalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy);

                case PathStrategy.VerticalFirst:
                    return CalculateVerticalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy);

                case PathStrategy.ThreeSegment:
                    return CalculateThreeSegmentPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy);

                case PathStrategy.FiveSegment:
                    return CalculateFiveSegmentPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy);

                default:
                    return CalculateHorizontalFirstPath(sourcePosition, targetPosition, sourceDirection, targetDirection, dx, dy);
            }
        }

        /// <summary>
        /// 计算水平优先路径（水平-垂直-水平）
        /// </summary>
        private Point[] CalculateHorizontalFirstPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy)
        {
            // 计算第一个拐点：从源点沿源方向延伸一段距离
            var p1 = CalculateFirstPoint(sourcePosition, sourceDirection, dx, dy);

            // 计算第二个拐点：从第一个拐点沿垂直方向延伸到目标Y
            var p2 = new Point(p1.X, targetPosition.Y);

            // 最终路径点
            return new Point[]
            {
                sourcePosition,
                p1,
                p2,
                targetPosition
            };
        }

        /// <summary>
        /// 计算垂直优先路径（垂直-水平-垂直）
        /// </summary>
        private Point[] CalculateVerticalFirstPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy)
        {
            // 计算第一个拐点：从源点沿源方向延伸一段距离
            var p1 = CalculateFirstPoint(sourcePosition, sourceDirection, dx, dy);

            // 计算第二个拐点：从第一个拐点沿水平方向延伸到目标X
            var p2 = new Point(targetPosition.X, p1.Y);

            // 最终路径点
            return new Point[]
            {
                sourcePosition,
                p1,
                p2,
                targetPosition
            };
        }

        /// <summary>
        /// 计算三段式路径
        /// </summary>
        private Point[] CalculateThreeSegmentPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy)
        {
            // 三段式路径：水平-垂直 或 垂直-水平
            var midPoint1 = new Point(sourcePosition.X, targetPosition.Y);
            var midPoint2 = new Point(targetPosition.X, sourcePosition.Y);

            // 选择更优的中间点
            var betterMidPoint = sourceDirection.IsHorizontal() ? midPoint2 : midPoint1;

            return new Point[]
            {
                sourcePosition,
                betterMidPoint,
                targetPosition
            };
        }

        /// <summary>
        /// 计算五段式路径（用于复杂场景）
        /// </summary>
        private Point[] CalculateFiveSegmentPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection,
            double dx,
            double dy)
        {
            // 五段式路径：从源方向延伸 - 转向 - 延伸到目标X或Y - 转向 - 到达目标
            var p1 = CalculateFirstPoint(sourcePosition, sourceDirection, dx, dy);

            // 中间延伸段
            var midX = (sourcePosition.X + targetPosition.X) / 2;
            var midY = (sourcePosition.Y + targetPosition.Y) / 2;

            var p2 = new Point(midX, p1.Y);
            var p3 = new Point(midX, targetPosition.Y);

            return new Point[]
            {
                sourcePosition,
                p1,
                p2,
                p3,
                targetPosition
            };
        }

        /// <summary>
        /// 计算第一个拐点
        /// </summary>
        private Point CalculateFirstPoint(Point sourcePosition, PortDirection sourceDirection, double dx, double dy)
        {
            var offset = Math.Max(MinSegmentLength, Math.Abs(dx) * 0.3);

            return sourceDirection switch
            {
                PortDirection.Right => new Point(sourcePosition.X + offset, sourcePosition.Y),
                PortDirection.Left => new Point(sourcePosition.X - offset, sourcePosition.Y),
                PortDirection.Bottom => new Point(sourcePosition.X, sourcePosition.Y + offset),
                PortDirection.Top => new Point(sourcePosition.X, sourcePosition.Y - offset),
                _ => new Point(sourcePosition.X, sourcePosition.Y)
            };
        }

        /// <summary>
        /// 根据路径点创建路径几何
        /// </summary>
        public PathGeometry CreatePathGeometry(Point[] pathPoints)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return new PathGeometry();
            }

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = pathPoints[0],
                IsClosed = false
            };

            // 添加线段
            for (int i = 1; i < pathPoints.Length; i++)
            {
                pathFigure.Segments.Add(new LineSegment(pathPoints[i], true));
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        /// <summary>
        /// 计算箭头位置和角度
        /// 箭头尖端位于目标端口位置，角度基于端口方向固定
        /// 箭头几何：尖端在(0,0)，尾部(-8,-5)和(-8,5)，旋转原点在(0,0)
        /// 0度时尖端指向左侧，尾部在右侧
        /// </summary>
        /// <param name="pathPoints">路径点数组</param>
        /// <param name="targetPosition">目标端口位置（箭头尖端应该到达的位置）</param>
        /// <param name="targetDirection">目标端口方向，决定箭头的固定角度</param>
        /// <returns>箭头位置和角度（角度为度数）</returns>
        public (Point position, double angle) CalculateArrow(Point[] pathPoints, Point targetPosition, PortDirection targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return (new Point(0, 0), 0);
            }

            // 箭头角度基于目标端口方向固定（与箭头几何定义匹配）
            var arrowAngle = targetDirection switch
            {
                PortDirection.Right => 180.0,   // 向右：需要旋转180度（从指向左转为指向右）
                PortDirection.Left => 0.0,       // 向左：0度（箭头几何默认指向左）
                PortDirection.Top => 90.0,        // 向上：旋转90度
                PortDirection.Bottom => 270.0,     // 向下：旋转270度
                _ => 0.0
            };

            // 箭头尖端位于目标端口位置
            // 这样箭头会刚好到达目标端口中心
            var arrowPosition = targetPosition;

            // 路径终点已经在CalculateOrthogonalPath中调整过了
            // 不需要再次修改，只返回箭头位置和角度
            var lastPoint = pathPoints[pathPoints.Length - 1];
            var secondLastPoint = pathPoints[pathPoints.Length - 2];

            // 关键日志：记录箭头计算结果
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath] 箭头计算: 尖端位置({arrowPosition.X:F1},{arrowPosition.Y:F1}), 角度{arrowAngle:F1}°");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath]   路径最后两点: ({secondLastPoint.X:F1},{secondLastPoint.Y:F1}) -> ({lastPoint.X:F1},{lastPoint.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath]   目标端口位置:({targetPosition.X:F1},{targetPosition.Y:F1}), 路径终点:({lastPoint.X:F1},{lastPoint.Y:F1})");
            System.Diagnostics.Debug.WriteLine($"[OrthogonalPath]   目标端口方向:{targetDirection}");

            return (arrowPosition, arrowAngle);
        }
    }
}
