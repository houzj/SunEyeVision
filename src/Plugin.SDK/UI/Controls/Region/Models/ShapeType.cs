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
    /// 区域定义模式
    /// </summary>
    public enum RegionDefinitionMode
    {
        /// <summary>
        /// 绘制模式 - 用户手动绘制形状
        /// </summary>
        Drawing,

        /// <summary>
        /// 订阅模式 - 统一订阅，支持区域和参数订阅
        /// </summary>
        Subscribe
    }

    /// <summary>
    /// 参数绑定类型
    /// </summary>
    public enum ParameterBindingType
    {
        /// <summary>
        /// 常量值
        /// </summary>
        Constant,

        /// <summary>
        /// 节点输出
        /// </summary>
        NodeOutput,

        /// <summary>
        /// 表达式
        /// </summary>
        Expression,

        /// <summary>
        /// 变量
        /// </summary>
        Variable
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
