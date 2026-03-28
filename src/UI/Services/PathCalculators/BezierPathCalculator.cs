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

        /// <summary>基础控制点偏移比例（降低到30%，减少曲线张力）</summary>
        private const double BaseControlRatio = 0.3;

        /// <summary>最小控制点偏移（防止短距离时曲线过平）</summary>
        private const double MinControlOffset = 50.0;

        /// <summary>最大控制点偏移（防止长距离时曲线过度拉伸）</summary>
        private const double MaxControlOffset = 150.0;

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
        /// 算法原理（Miro+Figma混合策略）：
        /// 1. 使用主轴距离（非总距离）计算基础偏移量
        /// 2. 基础比例为30%，范围限制在50px-150px
        /// 3. 根据端口方向相对关系动态调整
        /// 4. 目标：曲线自然流畅，避免"太着急"的转弯
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

            // 智能计算控制点偏移量
            double offset = CalculateSmartControlOffset(
                sourcePosition,
                targetPosition,
                sourceDirection,
                targetDirection);

            // 根据端口方向计算控制点
            var controlPoint1 = GetControlPoint(sourcePosition, sourceDirection, offset);
            var controlPoint2 = GetControlPoint(targetPosition, targetDirection, offset);

            return new Point[] { sourcePosition, controlPoint1, controlPoint2, targetPosition };
        }

        /// <summary>
        /// 智能计算控制点偏移量
        ///
        /// 策略：
        /// 1. 使用主轴距离（水平/垂直距离较大者）而非总距离
        /// 2. 基础偏移 = 主轴距离 × 30%
        /// 3. 偏移量限制在50px-150px之间
        /// 4. 根据端口方向关系动态调整：
        ///    - 相对方向（如Right→Left）：降低偏移量（×0.8）
        ///    - 相同方向（如Right→Right）：增加偏移量（×1.1）
        /// </summary>
        private double CalculateSmartControlOffset(
            Point sourcePosition,
            Point targetPosition,
            PortDirection sourceDirection,
            PortDirection targetDirection)
        {
            double dx = targetPosition.X - sourcePosition.X;
            double dy = targetPosition.Y - sourcePosition.Y;
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);

            // 使用主轴距离（水平或垂直距离较大者）
            double mainAxisDistance = Math.Max(absDx, absDy);

            // 计算基础偏移量（30%）
            double ratioOffset = mainAxisDistance * BaseControlRatio;

            // 限制偏移量范围（50px-150px）
            double finalOffset = Math.Clamp(ratioOffset, MinControlOffset, MaxControlOffset);

            // 根据端口方向关系动态调整
            if (AreOppositeDirections(sourceDirection, targetDirection))
            {
                // 相对方向（如Right→Left, Top→Bottom），降低偏移量使曲线更紧凑
                finalOffset *= 0.8;
            }
            else if (AreSameDirection(sourceDirection, targetDirection))
            {
                // 相同方向（如Right→Right），增加偏移量使曲线更平滑
                finalOffset *= 1.1;
            }

            return finalOffset;
        }

        /// <summary>
        /// 判断两个端口方向是否相对（如Right→Left, Top→Bottom）
        /// </summary>
        private bool AreOppositeDirections(PortDirection dir1, PortDirection dir2)
        {
            return (dir1 == PortDirection.Left && dir2 == PortDirection.Right) ||
                   (dir1 == PortDirection.Right && dir2 == PortDirection.Left) ||
                   (dir1 == PortDirection.Top && dir2 == PortDirection.Bottom) ||
                   (dir1 == PortDirection.Bottom && dir2 == PortDirection.Top);
        }

        /// <summary>
        /// 判断两个端口方向是否相同
        /// </summary>
        private bool AreSameDirection(PortDirection dir1, PortDirection dir2)
        {
            return dir1 == dir2;
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
