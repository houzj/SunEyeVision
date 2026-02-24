using System;
using System.Windows;
using System.Windows.Media;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Path;

namespace SunEyeVision.UI.Services.PathCalculators
{
    /// <summary>
    /// 贝塞尔曲线路径计算器 - 实现平滑的贝塞尔曲线连接
    /// </summary>
    public class BezierPathCalculator : IPathCalculator
    {
        private const double ControlPointOffsetRatio = 0.4;  // 控制点偏移比例（相对于距离）
        private const double MinCurveDistance = 30.0;         // 最小曲线距离，低于此距离使用直�?
        private const double ArrowLength = 15.0;             // 箭头长度

        /// <summary>
        /// 计算正交路径（贝塞尔计算器不使用此方法，返回简化路径）
        /// </summary>
        public Point[] CalculateOrthogonalPath(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            // 调用增强方法
            return CalculateOrthogonalPath(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection,
                Rect.Empty,
                Rect.Empty);
        }

        /// <summary>
        /// 计算正交路径（贝塞尔计算器不使用此方法，返回简化路径）
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
            // 计算贝塞尔曲线控制点
            var controlPoints = CalculateBezierControlPoints(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection);

            return controlPoints;
        }

        /// <summary>
        /// 计算贝塞尔曲线的控制�?
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

            // 如果距离太近，使用直�?
            if (distance < MinCurveDistance)
            {
                return new Point[] { sourcePosition, targetPosition };
            }

            // 根据端口方向和距离计算控制点
            double controlOffset = distance * ControlPointOffsetRatio;

            // 确保控制点偏移量不小于最小�?
            controlOffset = Math.Max(controlOffset, 20.0);

            // 计算控制�?（靠近源点）
            var controlPoint1 = CalculateControlPoint(
                sourcePosition,
                sourceDirection,
                controlOffset);

            // 计算控制�?（靠近目标点�?
            var controlPoint2 = CalculateControlPoint(
                targetPosition,
                targetDirection,
                controlOffset);

            return new Point[]
            {
                sourcePosition,
                controlPoint1,
                controlPoint2,
                targetPosition
            };
        }

        /// <summary>
        /// 根据位置和方向计算控制点
        /// </summary>
        private Point CalculateControlPoint(Point position, PortDirection direction, double offset)
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
        /// 创建贝塞尔曲线路径几�?
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
                // 直线（两点）
                pathFigure.Segments.Add(new LineSegment(pathPoints[1], true));
            }
            else if (pathPoints.Length == 3)
            {
                // 二次贝塞尔曲线（三点�?
                pathFigure.Segments.Add(new QuadraticBezierSegment(pathPoints[1], pathPoints[2], true));
            }
            else if (pathPoints.Length == 4)
            {
                // 三次贝塞尔曲线（四点�?
                pathFigure.Segments.Add(new BezierSegment(pathPoints[1], pathPoints[2], pathPoints[3], true));
            }
            else
            {
                // 多点，使用直线连�?
                for (int i = 1; i < pathPoints.Length; i++)
                {
                    pathFigure.Segments.Add(new LineSegment(pathPoints[i], true));
                }
            }

            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        /// <summary>
        /// 计算箭头位置和角度（针对贝塞尔曲线）
        /// 箭头位于目标端口位置，角度基于曲线在该点的切线方�?
        /// </summary>
        public (Point position, double angle) CalculateArrow(Point[] pathPoints, Point targetPosition, PortDirection targetDirection)
        {
            if (pathPoints == null || pathPoints.Length < 2)
            {
                return (new Point(0, 0), 0);
            }

            // 箭头尖端位于目标端口位置
            var arrowPosition = targetPosition;

            // 计算箭头角度
            double arrowAngle;

            if (pathPoints.Length == 4)
            {
                // 三次贝塞尔曲线，计算目标点的切线角度
                arrowAngle = CalculateBezierTangentAngle(
                    pathPoints[0],
                    pathPoints[1],
                    pathPoints[2],
                    pathPoints[3],
                    1.0);  // t = 1.0 表示终点
            }
            else if (pathPoints.Length == 3)
            {
                // 二次贝塞尔曲线，计算目标点的切线角度
                arrowAngle = CalculateQuadraticBezierTangentAngle(
                    pathPoints[0],
                    pathPoints[1],
                    pathPoints[2],
                    1.0);  // t = 1.0 表示终点
            }
            else
            {
                // 直线，使用端点方向的固定角度
                arrowAngle = GetFixedArrowAngle(targetDirection);
            }

            return (arrowPosition, arrowAngle);
        }

        /// <summary>
        /// 计算三次贝塞尔曲线在t处的切线角度
        /// 三次贝塞尔曲线导数：B'(t) = 3*(1-t)²*(P1-P0) + 6*(1-t)*t*(P2-P1) + 3*t²*(P3-P2)
        /// </summary>
        private double CalculateBezierTangentAngle(Point p0, Point p1, Point p2, Point p3, double t)
        {
            // 计算切线向量
            double tx = 3 * (1 - t) * (1 - t) * (p1.X - p0.X) +
                       6 * (1 - t) * t * (p2.X - p1.X) +
                       3 * t * t * (p3.X - p2.X);

            double ty = 3 * (1 - t) * (1 - t) * (p1.Y - p0.Y) +
                       6 * (1 - t) * t * (p2.Y - p1.Y) +
                       3 * t * t * (p3.Y - p2.Y);

            // 计算角度（转换为度数�?
            double angle = Math.Atan2(ty, tx) * 180 / Math.PI;

            // 转换为WPF坐标系角�?
            return NormalizeAngle(angle);
        }

        /// <summary>
        /// 计算二次贝塞尔曲线在t处的切线角度
        /// 二次贝塞尔曲线导数：B'(t) = 2*(1-t)*(P1-P0) + 2*t*(P2-P1)
        /// </summary>
        private double CalculateQuadraticBezierTangentAngle(Point p0, Point p1, Point p2, double t)
        {
            // 计算切线向量
            double tx = 2 * (1 - t) * (p1.X - p0.X) + 2 * t * (p2.X - p1.X);
            double ty = 2 * (1 - t) * (p1.Y - p0.Y) + 2 * t * (p2.Y - p1.Y);

            // 计算角度（转换为度数�?
            double angle = Math.Atan2(ty, tx) * 180 / Math.PI;

            // 转换为WPF坐标系角�?
            return NormalizeAngle(angle);
        }

        /// <summary>
        /// 获取固定箭头角度（基于目标端口方向）
        /// 用于直线连接的情�?
        /// </summary>
        private double GetFixedArrowAngle(PortDirection targetDirection)
        {
            return targetDirection switch
            {
                PortDirection.Left => 0.0,     // 左边端口：箭头向�?
                PortDirection.Right => 180.0,   // 右边端口：箭头向�?
                PortDirection.Top => 90.0,      // 上边端口：箭头向�?
                PortDirection.Bottom => 270.0,  // 下边端口：箭头向�?
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
