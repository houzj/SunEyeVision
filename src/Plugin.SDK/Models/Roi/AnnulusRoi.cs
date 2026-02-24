using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Extensions;
using SunEyeVision.Plugin.SDK.Models.Geometry;
using System;

namespace SunEyeVision.Plugin.SDK.Models.Roi
{
    /// <summary>
    /// 圆环ROI
    /// </summary>
    public sealed class AnnulusRoi : RoiBase
    {
        private readonly Annulus2d _annulus;

        /// <summary>
        /// 创建完整圆环ROI
        /// </summary>
        public AnnulusRoi(double centerX, double centerY, double innerRadius, double outerRadius)
        {
            _annulus = new Annulus2d(centerX, centerY, innerRadius, outerRadius);
        }

        /// <summary>
        /// 创建扇形圆环ROI
        /// </summary>
        public AnnulusRoi(double centerX, double centerY, double innerRadius, double outerRadius,
                          double startAngle, double endAngle)
        {
            _annulus = new Annulus2d(centerX, centerY, innerRadius, outerRadius, startAngle, endAngle);
        }

        /// <summary>
        /// 从Annulus2d结构创建
        /// </summary>
        public AnnulusRoi(Annulus2d annulus)
        {
            _annulus = annulus;
        }

        /// <summary>
        /// 从圆心创建
        /// </summary>
        public static AnnulusRoi FromCenter(Point2d center, double innerRadius, double outerRadius)
        {
            return new AnnulusRoi(new Annulus2d(center, innerRadius, outerRadius));
        }

        public override RoiType Type => RoiType.Annulus;

        public override bool IsValid => _annulus.Width > 0;

        public override Rect2d BoundingBox => _annulus.BoundingBox;

        public override Point2d Center => _annulus.Center;

        public override double Area => _annulus.Area;

        /// <summary>
        /// 圆环数据
        /// </summary>
        public Annulus2d Annulus => _annulus;

        /// <summary>
        /// 圆心X坐标
        /// </summary>
        public double CenterX => _annulus.Center.X;

        /// <summary>
        /// 圆心Y坐标
        /// </summary>
        public double CenterY => _annulus.Center.Y;

        /// <summary>
        /// 内半径
        /// </summary>
        public double InnerRadius => _annulus.InnerRadius;

        /// <summary>
        /// 外半径
        /// </summary>
        public double OuterRadius => _annulus.OuterRadius;

        /// <summary>
        /// 起始角度（弧度）
        /// </summary>
        public double StartAngle => _annulus.StartAngle;

        /// <summary>
        /// 结束角度（弧度）
        /// </summary>
        public double EndAngle => _annulus.EndAngle;

        /// <summary>
        /// 是否为完整圆环
        /// </summary>
        public bool IsFullCircle => _annulus.IsFullCircle;

        public override bool Contains(Point2d point)
        {
            return _annulus.Contains(point);
        }

        public override Point2d[] GetBoundaryPoints()
        {
            // 返回内圆和外圆的采样点
            const int pointsPerCircle = 32;
            int totalPoints = IsFullCircle ? pointsPerCircle * 2 : pointsPerCircle * 2 + 4;
            Point2d[] points = new Point2d[totalPoints];
            int index = 0;

            if (IsFullCircle)
            {
                double angleStep = 2 * Math.PI / pointsPerCircle;
                for (int i = 0; i < pointsPerCircle; i++)
                {
                    double angle = i * angleStep;
                    points[index++] = _annulus.InnerCircle.GetPointOnCircle(angle);
                }
                for (int i = 0; i < pointsPerCircle; i++)
                {
                    double angle = i * angleStep;
                    points[index++] = _annulus.OuterCircle.GetPointOnCircle(angle);
                }
            }
            else
            {
                // 扇形：添加起始和结束的径向线
                Point2d[] samplePoints = _annulus.GetSamplePoints(pointsPerCircle);
                foreach (var p in samplePoints)
                {
                    points[index++] = p;
                }
                // 内圆弧
                for (int i = pointsPerCircle - 1; i >= 0; i--)
                {
                    double angle = StartAngle + (EndAngle - StartAngle) * i / pointsPerCircle;
                    points[index++] = new Point2d(
                        CenterX + InnerRadius * Math.Cos(angle),
                        CenterY + InnerRadius * Math.Sin(angle)
                    );
                }
            }

            return points;
        }

        public override Mat CreateMask(Size imageSize)
        {
            Mat mask = new Mat(imageSize, MatType.CV_8UC1, Scalar.Black);

            // 先绘制外圆
            Cv2.Circle(mask, new Point((int)CenterX, (int)CenterY), (int)OuterRadius, Scalar.White, -1);

            // 再用黑色绘制内圆（创建圆环效果）
            Cv2.Circle(mask, new Point((int)CenterX, (int)CenterY), (int)InnerRadius, Scalar.Black, -1);

            // 如果是扇形，需要额外处理
            if (!IsFullCircle)
            {
                // TODO: 实现扇形掩码
            }

            return mask;
        }

        public override IRoi Offset(double dx, double dy)
        {
            return new AnnulusRoi(_annulus.Offset(dx, dy));
        }

        public override IRoi Scale(double factor)
        {
            return new AnnulusRoi(_annulus.Scale(factor));
        }

        public override IRoi Transform(Mat transformMatrix)
        {
            if (transformMatrix == null || transformMatrix.Empty())
                return this;

            double avgScale = (GetMatrixScaleX(transformMatrix) + GetMatrixScaleY(transformMatrix)) / 2;

            double newCenterX = _annulus.Center.X * transformMatrix.Get<double>(0, 0) +
                               _annulus.Center.Y * transformMatrix.Get<double>(0, 1) +
                               transformMatrix.Get<double>(0, 2);
            double newCenterY = _annulus.Center.X * transformMatrix.Get<double>(1, 0) +
                               _annulus.Center.Y * transformMatrix.Get<double>(1, 1) +
                               transformMatrix.Get<double>(1, 2);

            double rotation = Math.Atan2(transformMatrix.Get<double>(1, 0), transformMatrix.Get<double>(0, 0));

            return new AnnulusRoi(
                newCenterX, newCenterY,
                _annulus.InnerRadius * avgScale,
                _annulus.OuterRadius * avgScale,
                _annulus.StartAngle + rotation,
                _annulus.EndAngle + rotation
            );
        }

        private static double GetMatrixScaleX(Mat matrix)
        {
            return Math.Sqrt(
                matrix.Get<double>(0, 0) * matrix.Get<double>(0, 0) +
                matrix.Get<double>(1, 0) * matrix.Get<double>(1, 0)
            );
        }

        private static double GetMatrixScaleY(Mat matrix)
        {
            return Math.Sqrt(
                matrix.Get<double>(0, 1) * matrix.Get<double>(0, 1) +
                matrix.Get<double>(1, 1) * matrix.Get<double>(1, 1)
            );
        }

        public override string ToString() =>
            IsFullCircle
                ? $"AnnulusROI(Center=({CenterX:F1}, {CenterY:F1}), R=[{InnerRadius:F1}, {OuterRadius:F1}])"
                : $"AnnulusROI(Center=({CenterX:F1}, {CenterY:F1}), R=[{InnerRadius:F1}, {OuterRadius:F1}], θ=[{StartAngle:F2}, {EndAngle:F2}])";
    }
}
