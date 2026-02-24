using OpenCvSharp;
using System;

namespace SunEyeVision.Plugin.SDK.Extensions
{
    /// <summary>
    /// RotatedRect扩展方法
    /// </summary>
    public static class RotatedRectExtensions
    {
        /// <summary>
        /// 获取中心点
        /// </summary>
        public static Point2d Center(this RotatedRect r) => new Point2d(r.Center.X, r.Center.Y);

        /// <summary>
        /// 获取面积
        /// </summary>
        public static double Area(this RotatedRect r) => r.Size.Width * r.Size.Height;

        /// <summary>
        /// 获取宽高比
        /// </summary>
        public static double AspectRatio(this RotatedRect r)
        {
            return r.Size.Height > 0 ? r.Size.Width / r.Size.Height : 0;
        }

        /// <summary>
        /// 获取角度（弧度）
        /// </summary>
        public static double AngleRadians(this RotatedRect r) => r.Angle * Math.PI / 180.0;

        /// <summary>
        /// 获取四个角点
        /// </summary>
        public static Point2d[] GetCornerPoints(this RotatedRect r)
        {
            var points = Cv2.BoxPoints(r);
            var result = new Point2d[4];
            for (int i = 0; i < 4; i++)
            {
                result[i] = new Point2d(points[i].X, points[i].Y);
            }
            return result;
        }

        /// <summary>
        /// 获取边界矩形
        /// </summary>
        public static Rect2d BoundingRect(this RotatedRect r)
        {
            var rect = r.BoundingRect();
            return new Rect2d(rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// 判断是否包含点（考虑旋转）
        /// </summary>
        public static bool Contains(this RotatedRect r, Point2d p)
        {
            var corners = r.GetCornerPoints();
            // 使用射线法判断点是否在多边形内
            return IsPointInPolygon(p, corners);
        }

        /// <summary>
        /// 从中心点和尺寸创建旋转矩形
        /// </summary>
        public static RotatedRect FromCenter(Point2d center, double width, double height, double angleDegrees = 0)
        {
            return new RotatedRect(
                new Point2f((float)center.X, (float)center.Y),
                new Size2f((float)width, (float)height),
                (float)angleDegrees
            );
        }

        /// <summary>
        /// 从四个角点创建旋转矩形
        /// </summary>
        public static RotatedRect FromPoints(Point2d[] points)
        {
            if (points == null || points.Length < 3)
                throw new ArgumentException("至少需要3个点");

            // 转换为Point2f数组
            var points2f = new Point2f[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points2f[i] = new Point2f((float)points[i].X, (float)points[i].Y);
            }

            return Cv2.MinAreaRect(points2f);
        }

        /// <summary>
        /// 平移
        /// </summary>
        public static RotatedRect Offset(this RotatedRect r, double dx, double dy)
        {
            return new RotatedRect(
                new Point2f(r.Center.X + (float)dx, r.Center.Y + (float)dy),
                r.Size,
                r.Angle
            );
        }

        /// <summary>
        /// 缩放
        /// </summary>
        public static RotatedRect Scale(this RotatedRect r, double factor)
        {
            return new RotatedRect(
                r.Center,
                new Size2f(r.Size.Width * (float)factor, r.Size.Height * (float)factor),
                r.Angle
            );
        }

        private static bool IsPointInPolygon(Point2d point, Point2d[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) /
                              (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
