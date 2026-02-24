using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Extensions;
using SunEyeVision.Plugin.SDK.Models.Geometry;
using System;

namespace SunEyeVision.Plugin.SDK.Models.Roi
{
    /// <summary>
    /// ROI类型枚举
    /// </summary>
    public enum RoiType
    {
        /// <summary>
        /// 全图
        /// </summary>
        FullImage = 0,

        /// <summary>
        /// 矩形ROI
        /// </summary>
        Rectangle = 1,

        /// <summary>
        /// 旋转矩形ROI
        /// </summary>
        RotatedRectangle = 2,

        /// <summary>
        /// 圆环ROI
        /// </summary>
        Annulus = 3,

        /// <summary>
        /// 多边形ROI
        /// </summary>
        Polygon = 4,

        /// <summary>
        /// 线卡尺ROI
        /// </summary>
        LineCaliper = 5,

        /// <summary>
        /// 弧卡尺ROI
        /// </summary>
        ArcCaliper = 6,

        /// <summary>
        /// 圆形ROI
        /// </summary>
        Circle = 7,

        /// <summary>
        /// 椭圆ROI
        /// </summary>
        Ellipse = 8,

        /// <summary>
        /// 自定义形状
        /// </summary>
        Custom = 99
    }

    /// <summary>
    /// ROI接口
    /// </summary>
    /// <remarks>
    /// 所有ROI类型的基础接口，定义了ROI的基本功能。
    /// ROI（Region of Interest）是图像处理中常用的概念，
    /// 用于指定算法作用的具体区域。
    /// 使用OpenCvSharp原生类型。
    /// </remarks>
    public interface IRoi
    {
        /// <summary>
        /// ROI类型
        /// </summary>
        RoiType Type { get; }

        /// <summary>
        /// 是否有效
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// 边界矩形（OpenCvSharp类型）
        /// </summary>
        Rect2d BoundingBox { get; }

        /// <summary>
        /// 边界矩形（整数坐标，用于图像操作）
        /// </summary>
        Rect BoundingRect { get; }

        /// <summary>
        /// 中心点
        /// </summary>
        Point2d Center { get; }

        /// <summary>
        /// 面积
        /// </summary>
        double Area { get; }

        /// <summary>
        /// 判断点是否在ROI内
        /// </summary>
        bool Contains(Point2d point);

        /// <summary>
        /// 获取边界采样点（用于绘制）
        /// </summary>
        Point2d[] GetBoundaryPoints();

        /// <summary>
        /// 创建ROI掩码
        /// </summary>
        /// <param name="imageSize">图像尺寸</param>
        /// <returns>掩码图像</returns>
        Mat CreateMask(Size imageSize);

        /// <summary>
        /// 平移ROI
        /// </summary>
        IRoi Offset(double dx, double dy);

        /// <summary>
        /// 缩放ROI
        /// </summary>
        IRoi Scale(double factor);

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        /// <param name="transformMatrix">3x3变换矩阵</param>
        IRoi Transform(Mat transformMatrix);
    }

    /// <summary>
    /// ROI基类
    /// </summary>
    public abstract class RoiBase : IRoi
    {
        public abstract RoiType Type { get; }
        public abstract bool IsValid { get; }
        public abstract Rect2d BoundingBox { get; }
        public abstract Point2d Center { get; }
        public abstract double Area { get; }

        public abstract bool Contains(Point2d point);
        public abstract Point2d[] GetBoundaryPoints();

        /// <summary>
        /// 获取边界矩形（整数坐标）
        /// </summary>
        public virtual Rect BoundingRect => BoundingBox.ToRect();

        /// <summary>
        /// 创建ROI掩码
        /// </summary>
        public virtual Mat CreateMask(Size imageSize)
        {
            Mat mask = new Mat(imageSize, MatType.CV_8UC1, Scalar.Black);
            var points = GetBoundaryPoints();
            if (points.Length < 3) return mask;

            // 转换为Point数组
            Point[] intPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                intPoints[i] = new Point((int)Math.Round(points[i].X), (int)Math.Round(points[i].Y));
            }

            Cv2.FillPoly(mask, new[] { intPoints }, Scalar.White);
            return mask;
        }

        public abstract IRoi Offset(double dx, double dy);
        public abstract IRoi Scale(double factor);
        public abstract IRoi Transform(Mat transformMatrix);

        public override string ToString() => $"{Type} ROI";
    }
}
