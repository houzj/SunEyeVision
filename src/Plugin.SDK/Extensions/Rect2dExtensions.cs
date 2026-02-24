using OpenCvSharp;
using System;

namespace SunEyeVision.Plugin.SDK.Extensions
{
    /// <summary>
    /// Rect2d扩展方法
    /// </summary>
    public static class Rect2dExtensions
    {
        /// <summary>
        /// 获取中心点
        /// </summary>
        public static Point2d Center(this Rect2d r) => new Point2d(r.X + r.Width / 2, r.Y + r.Height / 2);

        /// <summary>
        /// 获取左上角
        /// </summary>
        public static Point2d TopLeft(this Rect2d r) => new Point2d(r.X, r.Y);

        /// <summary>
        /// 获取右上角
        /// </summary>
        public static Point2d TopRight(this Rect2d r) => new Point2d(r.X + r.Width, r.Y);

        /// <summary>
        /// 获取左下角
        /// </summary>
        public static Point2d BottomLeft(this Rect2d r) => new Point2d(r.X, r.Y + r.Height);

        /// <summary>
        /// 获取右下角
        /// </summary>
        public static Point2d BottomRight(this Rect2d r) => new Point2d(r.X + r.Width, r.Y + r.Height);

        /// <summary>
        /// 获取面积
        /// </summary>
        public static double Area(this Rect2d r) => r.Width * r.Height;

        /// <summary>
        /// 获取周长
        /// </summary>
        public static double Perimeter(this Rect2d r) => 2 * (r.Width + r.Height);

        /// <summary>
        /// 获取宽高比
        /// </summary>
        public static double AspectRatio(this Rect2d r) => r.Height > 0 ? r.Width / r.Height : 0;

        /// <summary>
        /// 判断是否为空
        /// </summary>
        public static bool IsEmpty(this Rect2d r) => r.Width <= 0 || r.Height <= 0;

        /// <summary>
        /// 判断是否包含点
        /// </summary>
        public static bool Contains(this Rect2d r, Point2d p)
        {
            return p.X >= r.X && p.X <= r.X + r.Width &&
                   p.Y >= r.Y && p.Y <= r.Y + r.Height;
        }

        /// <summary>
        /// 扩展矩形
        /// </summary>
        public static Rect2d Inflate(this Rect2d r, double amount)
        {
            return new Rect2d(r.X - amount, r.Y - amount, r.Width + 2 * amount, r.Height + 2 * amount);
        }

        /// <summary>
        /// 平移矩形
        /// </summary>
        public static Rect2d Offset(this Rect2d r, double dx, double dy)
        {
            return new Rect2d(r.X + dx, r.Y + dy, r.Width, r.Height);
        }

        /// <summary>
        /// 从中心点和尺寸创建矩形
        /// </summary>
        public static Rect2d FromCenter(Point2d center, double width, double height)
        {
            return new Rect2d(center.X - width / 2, center.Y - height / 2, width, height);
        }

        /// <summary>
        /// 从两点创建边界矩形
        /// </summary>
        public static Rect2d FromPoints(Point2d p1, Point2d p2)
        {
            double minX = Math.Min(p1.X, p2.X);
            double minY = Math.Min(p1.Y, p2.Y);
            double maxX = Math.Max(p1.X, p2.X);
            double maxY = Math.Max(p1.Y, p2.Y);
            return new Rect2d(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 两矩形求交集
        /// </summary>
        public static Rect2d Intersect(this Rect2d a, Rect2d b)
        {
            double x = Math.Max(a.X, b.X);
            double y = Math.Max(a.Y, b.Y);
            double right = Math.Min(a.X + a.Width, b.X + b.Width);
            double bottom = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x < right && y < bottom)
                return new Rect2d(x, y, right - x, bottom - y);

            return new Rect2d(0, 0, 0, 0);
        }

        /// <summary>
        /// 两矩形求并集
        /// </summary>
        public static Rect2d Union(this Rect2d a, Rect2d b)
        {
            double x = Math.Min(a.X, b.X);
            double y = Math.Min(a.Y, b.Y);
            double right = Math.Max(a.X + a.Width, b.X + b.Width);
            double bottom = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rect2d(x, y, right - x, bottom - y);
        }

        /// <summary>
        /// 转换为Rect（整数坐标）
        /// </summary>
        public static Rect ToRect(this Rect2d r)
        {
            return new Rect((int)Math.Floor(r.X), (int)Math.Floor(r.Y),
                           (int)Math.Ceiling(r.Width), (int)Math.Ceiling(r.Height));
        }

        /// <summary>
        /// 转换为Rect2f
        /// </summary>
        public static Rect2f ToRect2f(this Rect2d r)
        {
            return new Rect2f((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
        }
    }
}
