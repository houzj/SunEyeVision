using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Extensions;
using System;

namespace SunEyeVision.Plugin.SDK.Models.Roi
{
    /// <summary>
    /// 全图ROI
    /// </summary>
    /// <remarks>
    /// 表示使用整个图像作为ROI。
    /// 当不需要限制处理区域时使用。
    /// </remarks>
    public sealed class FullImageRoi : RoiBase
    {
        private readonly int _width;
        private readonly int _height;

        /// <summary>
        /// 创建全图ROI
        /// </summary>
        public FullImageRoi(int width, int height)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        /// 创建未知尺寸的全图ROI
        /// </summary>
        public FullImageRoi()
        {
            _width = 0;
            _height = 0;
        }

        public override RoiType Type => RoiType.FullImage;

        public override bool IsValid => true;

        public override Rect2d BoundingBox => _width > 0 && _height > 0
            ? new Rect2d(0, 0, _width, _height)
            : new Rect2d(0, 0, 0, 0);

        public override Rect BoundingRect => _width > 0 && _height > 0
            ? new Rect(0, 0, _width, _height)
            : new Rect();

        public override Point2d Center => _width > 0 && _height > 0
            ? new Point2d(_width / 2.0, _height / 2.0)
            : new Point2d(0, 0);

        public override double Area => _width * _height;

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height => _height;

        public override bool Contains(Point2d point)
        {
            // 全图ROI总是返回true（除非指定了尺寸且点在范围外）
            if (_width <= 0 || _height <= 0) return true;

            return point.X >= 0 && point.X < _width &&
                   point.Y >= 0 && point.Y < _height;
        }

        public override Point2d[] GetBoundaryPoints()
        {
            if (_width <= 0 || _height <= 0)
            {
                return Array.Empty<Point2d>();
            }

            return new Point2d[]
            {
                new Point2d(0, 0),
                new Point2d(_width, 0),
                new Point2d(_width, _height),
                new Point2d(0, _height)
            };
        }

        public override Mat CreateMask(Size imageSize)
        {
            Mat mask = new Mat(imageSize, MatType.CV_8UC1, Scalar.White);
            return mask;
        }

        public override IRoi Offset(double dx, double dy)
        {
            // 全图ROI不响应平移
            return this;
        }

        public override IRoi Scale(double factor)
        {
            return new FullImageRoi((int)(_width * factor), (int)(_height * factor));
        }

        public override IRoi Transform(Mat transformMatrix)
        {
            // 全图ROI不响应变换
            return this;
        }

        public override string ToString() =>
            _width > 0 && _height > 0
                ? $"FullImageROI({Width}x{Height})"
                : "FullImageROI";
    }
}
