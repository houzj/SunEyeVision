using System;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Services.Path;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 贝塞尔曲线路径计算器 - 实现平滑的贝塞尔曲线连接
    /// </summary>
    public class BezierPathCalculator : IPathCalculator
    {
        /// <summary>最小曲线距离</summary>
        private const double MinCurveDistance = 15.0;
        
        /// <summary>控制点偏移比例</summary>
        private const double ControlPointOffsetRatio = 0.5;

        /// <summary>
        /// 计算路径点集合（基础方法）
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            return CalculateBezierControlPoints(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection);
        }

        /// <summary>
        /// 计算路径点集合（增强方法，带节点信息）
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
            return CalculateBezierControlPoints(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection);
        }

        /// <summary>
        /// 计算贝塞尔曲线控制点
        /// 
        /// 算法原理：
        /// 1. 控制点偏移量 = 两点距离 × 0.5
        /// 2. 控制点沿端口切线方向延伸
        /// 3. 保持曲线平滑自然
        /// </summary>
        private Point[] CalculateBezierControlPoints(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            double dx = targetPosition.X - sourcePosition.X;
            double dy = targetPosition.Y - sourcePosition.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // 如果距离太近，使用直线
            if (distance < MinCurveDistance)
            {
                return new Point[] { sourcePosition, targetPosition };
            }

            // 统一使用距离的一半作为偏移量
            double offset = distance * ControlPointOffsetRatio;

            // 根据端口方向计算控制点
            var controlPoint1 = GetControlPoint(sourcePosition, sourceDirection, offset);
            var controlPoint2 = GetControlPoint(targetPosition, targetDirection, offset);

            return new Point[] { sourcePosition, controlPoint1, controlPoint2, targetPosition };
        }

        /// <summary>
        /// 根据位置和方向计算控制点
        /// </summary>
        private Point GetControlPoint(Point position, PortDirection direction, double offset)
        {
            return direction switch
            {
                PortDirection.Right => new Point(position.X + offset, position.Y),
                PortDirection.Left => new Point(position.X - offset, position.Y),
                PortDirection.Top => new Point(position.X, position.Y - offset),
                PortDirection.Bottom => new Point(position.X, position.Y + offset),
                _ => position
            };
        }

        /// <summary>
        /// 创建贝塞尔曲线路径几何
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

            if (pathPoints.Length == 2)
            {
                // 直线
                pathFigure.Segments.Add(new LineSegment(pathPoints[1], true));
            }
            else if (pathPoints.Length == 3)
            {
                // 二次贝塞尔曲线
                pathFigure.Segments.Add(new QuadraticBezierSegment(pathPoints[1], pathPoints[2], true));
            }
            else if (pathPoints.Length == 4)
            {
                // 三次贝塞尔曲线
                pathFigure.Segments.Add(new BezierSegment(pathPoints[1], pathPoints[2], pathPoints[3], true));
            }
            else
            {
                // 多点，使用直线连接
                for (int i = 1; i < pathPoints.Length; i++)
                {
                    pathFigure.Segments.Add(new LineSegment(pathPoints[i], true));
                }
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        /// <summary>
        /// 计算箭头位置和角度
        /// </summary>
        public (Point position, double angle) CalculateArrow(
            Point[] pathPoints,
            Point targetPosition,
            PortDirection targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return (new Point(0, 0), 0);
            }

            // 箭头尖端位于目标端口位置
            var arrowPosition = targetPosition;

            double arrowAngle;

            if (pathPoints.Length == 4)
            {
                // 三次贝塞尔曲线，计算目标点的切线角度
                arrowAngle = CalculateBezierTangentAngle(
                    pathPoints[0],
                    pathPoints[1],
                    pathPoints[2],
                    pathPoints[3],
                    1.0);
            }
            else if (pathPoints.Length == 3)
            {
                // 二次贝塞尔曲线
                arrowAngle = CalculateQuadraticBezierTangentAngle(
                    pathPoints[0],
                    pathPoints[1],
                    pathPoints[2],
                    1.0);
            }
            else
            {
                // 直线
                arrowAngle = GetFixedArrowAngle(targetDirection);
            }

            return (arrowPosition, arrowAngle);
        }

        /// <summary>
        /// 计算三次贝塞尔曲线在t处的切线角度
        /// </summary>
        private double CalculateBezierTangentAngle(Point p0, Point p1, Point p2, Point p3, double t)
        {
            double tx = 3 * (1 - t) * (1 - t) * (p1.X - p0.X) +
                       6 * (1 - t) * t * (p2.X - p1.X) +
                       3 * t * t * (p3.X - p2.X);

            double ty = 3 * (1 - t) * (1 - t) * (p1.Y - p0.Y) +
                       6 * (1 - t) * t * (p2.Y - p1.Y) +
                       3 * t * t * (p3.Y - p2.Y);

            double angle = Math.Atan2(ty, tx) * 180 / Math.PI;
            return NormalizeAngle(angle);
        }

        /// <summary>
        /// 计算二次贝塞尔曲线在t处的切线角度
        /// </summary>
        private double CalculateQuadraticBezierTangentAngle(Point p0, Point p1, Point p2, double t)
        {
            double tx = 2 * (1 - t) * (p1.X - p0.X) + 2 * t * (p2.X - p1.X);
            double ty = 2 * (1 - t) * (p1.Y - p0.Y) + 2 * t * (p2.Y - p1.Y);

            double angle = Math.Atan2(ty, tx) * 180 / Math.PI;
            return NormalizeAngle(angle);
        }

        /// <summary>
        /// 获取固定箭头角度（基于目标端口方向）
        /// </summary>
        private double GetFixedArrowAngle(PortDirection targetDirection)
        {
            return targetDirection switch
            {
                PortDirection.Left => 0.0,
                PortDirection.Right => 180.0,
                PortDirection.Top => 90.0,
                PortDirection.Bottom => 270.0,
                _ => 0.0
            };
        }

        /// <summary>
        /// 标准化角度到 [0, 360) 范围
        /// </summary>
        private double NormalizeAngle(double angle)
        {
            while (angle < 0)
                angle += 360;
            while (angle >= 360)
                angle -= 360;
            return angle;
        }
    }
}
