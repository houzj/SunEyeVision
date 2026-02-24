using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Extensions;
using System;

namespace SunEyeVision.Plugin.SDK.Models.Roi
{
    /// <summary>
    /// 矩形ROI
    /// </summary>
    public sealed class RectangleRoi : RoiBase
    {
        private readonly Rect2d _rect;
        private readonly double _angleRadians;

        /// <summary>
        /// 创建矩形ROI
        /// </summary>
        public RectangleRoi(double x, double y, double width, double height)
        {
            _rect = new Rect2d(x, y, width, height);
            _angleRadians = 0;
        }

        /// <summary>
        /// 创建矩形ROI（带旋转）
        /// </summary>
        public RectangleRoi(double x, double y, double width, double height, double angleRadians)
        {
            _rect = new Rect2d(x, y, width, height);
            _angleRadians = angleRadians;
        }

        /// <summary>
        /// 从Rect2d创建
        /// </summary>
        public RectangleRoi(Rect2d rect, double angleRadians = 0)
        {
            _rect = rect;
            _angleRadians = angleRadians;
        }

        /// <summary>
        /// 从RotatedRect创建
        /// </summary>
        public static RectangleRoi FromRotatedRect(RotatedRect rotatedRect)
        {
            var boundingRect = rotatedRect.BoundingRect();
            return new RectangleRoi(
                new Rect2d(boundingRect.X, boundingRect.Y, boundingRect.Width, boundingRect.Height),
                rotatedRect.AngleRadians()
            );
        }

        /// <summary>
        /// 从中心点创建
        /// </summary>
        public static RectangleRoi FromCenter(Point2d center, double width, double height, double angleRadians = 0)
        {
            return new RectangleRoi(Rect2dExtensions.FromCenter(center, width, height), angleRadians);
        }

        public override RoiType Type => _angleRadians == 0 ? RoiType.Rectangle : RoiType.RotatedRectangle;

        public override bool IsValid => !_rect.IsEmpty();

        public override Rect2d BoundingBox => _rect;

        public override Point2d Center => _rect.Center();

        public override double Area => _rect.Area();

        /// <summary>
        /// 矩形数据
        /// </summary>
        public Rect2d Rect => _rect;

        /// <summary>
        /// 获取RotatedRect
        /// </summary>
        public RotatedRect RotatedRect => new RotatedRect(
            new Point2f((float)Center.X, (float)Center.Y),
            new Size2f((float)_rect.Width, (float)_rect.Height),
            (float)(_angleRadians * 180 / Math.PI)
        );

        /// <summary>
        /// X坐标
        /// </summary>
        public double X => _rect.X;

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y => _rect.Y;

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width => _rect.Width;

        /// <summary>
        /// 高度
        /// </summary>
        public double Height => _rect.Height;

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public double Angle => _angleRadians;

        public override bool Contains(Point2d point)
        {
            if (_angleRadians == 0)
                return _rect.Contains(point);

            // 将点转换到矩形的局部坐标系
            Point2d center = Center;
            Point2d localPoint = point - center;

            double cos = Math.Cos(-_angleRadians);
            double sin = Math.Sin(-_angleRadians);

            double rotatedX = localPoint.X * cos - localPoint.Y * sin;
            double rotatedY = localPoint.X * sin + localPoint.Y * cos;

            return Math.Abs(rotatedX) <= _rect.Width / 2 && Math.Abs(rotatedY) <= _rect.Height / 2;
        }

        public override Point2d[] GetBoundaryPoints()
        {
            if (_angleRadians == 0)
            {
                return new[]
                {
                    _rect.TopLeft(),
                    _rect.TopRight(),
                    _rect.BottomRight(),
                    _rect.BottomLeft()
                };
            }

            // 计算旋转后的角点
            Point2d center = Center;
            Point2d[] corners = new[]
            {
                new Point2d(-_rect.Width / 2, -_rect.Height / 2),
                new Point2d(_rect.Width / 2, -_rect.Height / 2),
                new Point2d(_rect.Width / 2, _rect.Height / 2),
                new Point2d(-_rect.Width / 2, _rect.Height / 2)
            };

            double cos = Math.Cos(_angleRadians);
            double sin = Math.Sin(_angleRadians);

            for (int i = 0; i < corners.Length; i++)
            {
                double x = corners[i].X * cos - corners[i].Y * sin;
                double y = corners[i].X * sin + corners[i].Y * cos;
                corners[i] = new Point2d(x + center.X, y + center.Y);
            }

            return corners;
        }

        public override IRoi Offset(double dx, double dy)
        {
            return new RectangleRoi(_rect.Offset(dx, dy), _angleRadians);
        }

        public override IRoi Scale(double factor)
        {
            return new RectangleRoi(
                _rect.X * factor,
                _rect.Y * factor,
                _rect.Width * factor,
                _rect.Height * factor,
                _angleRadians
            );
        }

        public override IRoi Transform(Mat transformMatrix)
        {
            if (transformMatrix == null || transformMatrix.Empty())
                return this;

            // 应用变换到四个角点
            var corners = GetBoundaryPoints();
            Point2d[] transformedCorners = new Point2d[4];

            for (int i = 0; i < 4; i++)
            {
                transformedCorners[i] = TransformPoint(corners[i], transformMatrix);
            }

            // 计算最小外接矩形
            var rotatedRect = RotatedRectExtensions.FromPoints(transformedCorners);
            return FromRotatedRect(rotatedRect);
        }

        private static Point2d TransformPoint(Point2d point, Mat matrix)
        {
            // 假设matrix是3x3齐次变换矩阵
            double x = point.X * matrix.Get<double>(0, 0) +
                      point.Y * matrix.Get<double>(0, 1) +
                      matrix.Get<double>(0, 2);
            double y = point.X * matrix.Get<double>(1, 0) +
                      point.Y * matrix.Get<double>(1, 1) +
                      matrix.Get<double>(1, 2);
            return new Point2d(x, y);
        }

        public override string ToString() =>
            _angleRadians == 0
                ? $"RectangleROI(X={X:F1}, Y={Y:F1}, W={Width:F1}, H={Height:F1})"
                : $"RotatedRectROI(X={X:F1}, Y={Y:F1}, W={Width:F1}, H={Height:F1}, θ={_angleRadians:F2})";
    }
}
