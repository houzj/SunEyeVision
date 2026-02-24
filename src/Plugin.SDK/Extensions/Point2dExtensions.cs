using OpenCvSharp;
using System;

namespace SunEyeVision.Plugin.SDK.Extensions
{
    /// <summary>
    /// Point2d扩展方法
    /// </summary>
    public static class Point2dExtensions
    {
        /// <summary>
        /// 计算到另一点的距离
        /// </summary>
        public static double DistanceTo(this Point2d p, Point2d other)
        {
            double dx = p.X - other.X;
            double dy = p.Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算向量长度
        /// </summary>
        public static double Length(this Point2d p) => Math.Sqrt(p.X * p.X + p.Y * p.Y);

        /// <summary>
        /// 向量归一化
        /// </summary>
        public static Point2d Normalize(this Point2d p)
        {
            double len = p.Length();
            if (len == 0) return new Point2d(0, 0);
            return new Point2d(p.X / len, p.Y / len);
        }

        /// <summary>
        /// 向量角度（弧度）
        /// </summary>
        public static double Angle(this Point2d p) => Math.Atan2(p.Y, p.X);

        /// <summary>
        /// 向量点积
        /// </summary>
        public static double Dot(this Point2d a, Point2d b) => a.X * b.X + a.Y * b.Y;

        /// <summary>
        /// 向量叉积（返回标量）
        /// </summary>
        public static double Cross(this Point2d a, Point2d b) => a.X * b.Y - a.Y * b.X;

        /// <summary>
        /// 旋转向量
        /// </summary>
        /// <param name="p">原始向量</param>
        /// <param name="angleRadians">旋转角度（弧度）</param>
        public static Point2d Rotate(this Point2d p, double angleRadians)
        {
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            return new Point2d(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        public static Point2d RotateAround(this Point2d p, Point2d center, double angleRadians)
        {
            return (p - center).Rotate(angleRadians) + center;
        }

        /// <summary>
        /// 计算点到直线的距离
        /// </summary>
        public static double DistanceToLine(this Point2d p, Point2d linePoint, Point2d lineDirection)
        {
            var dir = lineDirection.Normalize();
            // 点到直线的距离 = |叉积| / |方向向量|
            var v = p - linePoint;
            return Math.Abs(v.Cross(dir));
        }

        /// <summary>
        /// 计算点到线段的距离
        /// </summary>
        public static double DistanceToSegment(this Point2d p, Point2d segStart, Point2d segEnd)
        {
            var v = segEnd - segStart;
            var w = p - segStart;
            double c1 = w.Dot(v);
            if (c1 <= 0) return p.DistanceTo(segStart);

            double c2 = v.Dot(v);
            if (c2 <= c1) return p.DistanceTo(segEnd);

            double b = c1 / c2;
            var pb = segStart + v * b;
            return p.DistanceTo(pb);
        }

        /// <summary>
        /// 从极坐标创建点
        /// </summary>
        public static Point2d FromPolar(double radius, double angleRadians)
        {
            return new Point2d(radius * Math.Cos(angleRadians), radius * Math.Sin(angleRadians));
        }

        /// <summary>
        /// 转换为Point（整数坐标）
        /// </summary>
        public static Point ToPoint(this Point2d p) => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));

        /// <summary>
        /// 转换为Point2f
        /// </summary>
        public static Point2f ToPoint2f(this Point2d p) => new Point2f((float)p.X, (float)p.Y);
    }
}
