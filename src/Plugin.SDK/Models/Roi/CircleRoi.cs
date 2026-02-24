using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Extensions;
using SunEyeVision.Plugin.SDK.Models.Geometry;
using System;

namespace SunEyeVision.Plugin.SDK.Models.Roi
{
    /// <summary>
    /// 圆形ROI
    /// </summary>
    public sealed class CircleRoi : RoiBase
    {
        private readonly Circle2d _circle;

        /// <summary>
        /// 创建圆形ROI
        /// </summary>
        public CircleRoi(double centerX, double centerY, double radius)
        {
            _circle = new Circle2d(centerX, centerY, radius);
        }

        /// <summary>
        /// 从Circle2d结构创建
        /// </summary>
        public CircleRoi(Circle2d circle)
        {
            _circle = circle;
        }

        /// <summary>
        /// 从圆心创建
        /// </summary>
        public static CircleRoi FromCenter(Point2d center, double radius)
        {
            return new CircleRoi(new Circle2d(center, radius));
        }

        public override RoiType Type => RoiType.Circle;

        public override bool IsValid => _circle.Radius > 0;

        public override Rect2d BoundingBox => _circle.BoundingBox;

        public override Point2d Center => _circle.Center;

        public override double Area => _circle.Area;

        /// <summary>
        /// 圆形数据
        /// </summary>
        public Circle2d Circle => _circle;

        /// <summary>
        /// 圆心X坐标
        /// </summary>
        public double CenterX => _circle.Center.X;

        /// <summary>
        /// 圆心Y坐标
        /// </summary>
        public double CenterY => _circle.Center.Y;

        /// <summary>
        /// 半径
        /// </summary>
        public double Radius => _circle.Radius;

        /// <summary>
        /// 直径
        /// </summary>
        public double Diameter => _circle.Diameter;

        /// <summary>
        /// 周长
        /// </summary>
        public double Circumference => _circle.Circumference;

        public override bool Contains(Point2d point)
        {
            return _circle.Contains(point);
        }

        public override Point2d[] GetBoundaryPoints()
        {
            const int pointCount = 64;
            Point2d[] points = new Point2d[pointCount];
            double angleStep = 2 * Math.PI / pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                points[i] = _circle.GetPointOnCircle(i * angleStep);
            }

            return points;
        }

        public override Mat CreateMask(Size imageSize)
        {
            Mat mask = new Mat(imageSize, MatType.CV_8UC1, Scalar.Black);
            Cv2.Circle(mask, new Point((int)CenterX, (int)CenterY), (int)Radius, Scalar.White, -1);
            return mask;
        }

        public override IRoi Offset(double dx, double dy)
        {
            return new CircleRoi(_circle.Offset(dx, dy));
        }

        public override IRoi Scale(double factor)
        {
            return new CircleRoi(_circle.Scale(factor));
        }

        public override IRoi Transform(Mat transformMatrix)
        {
            if (transformMatrix == null || transformMatrix.Empty())
                return this;

            // 圆经过非均匀缩放会变成椭圆
            // 简化处理：使用平均缩放因子
            double scaleX = Math.Sqrt(
                transformMatrix.Get<double>(0, 0) * transformMatrix.Get<double>(0, 0) +
                transformMatrix.Get<double>(1, 0) * transformMatrix.Get<double>(1, 0)
            );
            double scaleY = Math.Sqrt(
                transformMatrix.Get<double>(0, 1) * transformMatrix.Get<double>(0, 1) +
                transformMatrix.Get<double>(1, 1) * transformMatrix.Get<double>(1, 1)
            );

            double avgScale = (scaleX + scaleY) / 2;

            double newCenterX = _circle.Center.X * transformMatrix.Get<double>(0, 0) +
                               _circle.Center.Y * transformMatrix.Get<double>(0, 1) +
                               transformMatrix.Get<double>(0, 2);
            double newCenterY = _circle.Center.X * transformMatrix.Get<double>(1, 0) +
                               _circle.Center.Y * transformMatrix.Get<double>(1, 1) +
                               transformMatrix.Get<double>(1, 2);

            return new CircleRoi(newCenterX, newCenterY, _circle.Radius * avgScale);
        }

        public override string ToString() => $"CircleROI(Center=({CenterX:F1}, {CenterY:F1}), R={Radius:F1})";
    }
}
