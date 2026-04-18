using System;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 形状类型枚举
    /// </summary>
    public enum ShapeType
    {
        /// <summary>
        /// 点
        /// </summary>
        Point,

        /// <summary>
        /// 直线
        /// </summary>
        Line,

        /// <summary>
        /// 圆形
        /// </summary>
        Circle,

        /// <summary>
        /// 矩形
        /// </summary>
        Rectangle,

        /// <summary>
        /// 旋转矩形
        /// </summary>
        RotatedRectangle,

        /// <summary>
        /// 多边形
        /// </summary>
        Polygon,

        /// <summary>
        /// 圆环
        /// </summary>
        Annulus,

        /// <summary>
        /// 弧形
        /// </summary>
        Arc
    }

    /// <summary>
    /// 区域来源模式
    /// </summary>
    public enum RegionSourceMode
    {
        /// <summary>
        /// 绘制模式 - 区域来源于用户手动绘制
        /// </summary>
        Drawing,

        /// <summary>
        /// 订阅模式 - 区域来源于其他节点的输出
        /// </summary>
        Subscribe
    }

    /// <summary>
    /// 点结构
    /// </summary>
    public struct Point2D : IEquatable<Point2D>
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Point2D Empty => new Point2D(0, 0);

        public bool IsEmpty => X == 0 && Y == 0;

        public double DistanceTo(Point2D other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool Equals(Point2D other)
        {
            return Math.Abs(X - other.X) < 0.001 && Math.Abs(Y - other.Y) < 0.001;
        }

        public override bool Equals(object? obj)
        {
            return obj is Point2D point && Equals(point);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2})";
        }

        public static bool operator ==(Point2D left, Point2D right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point2D left, Point2D right)
        {
            return !left.Equals(right);
        }
    }
}
