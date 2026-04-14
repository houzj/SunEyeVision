using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 区域掩码构建器 - 将区域定义绘制到 OpenCV 掩码
    /// </summary>
    public class RegionMaskBuilder : IDisposable
    {
        private readonly Size _imageSize;
        private readonly Mat _mask;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="imageSize">图像尺寸</param>
        public RegionMaskBuilder(Size imageSize)
        {
            _imageSize = imageSize;
            _mask = new Mat(imageSize, MatType.CV_8UC1, Scalar.Black);
        }

        /// <summary>
        /// 获取构建的掩码
        /// </summary>
        public Mat GetMask()
        {
            return _mask;
        }

        /// <summary>
        /// 添加单个区域到掩码
        /// </summary>
        /// <param name="region">区域数据</param>
        public void AddRegion(RegionData region)
        {
            if (!region.IsEnabled || region.Parameters == null)
                return;

            var shape = region.Parameters;
            var shapeType = shape.GetShapeType();

            if (!shapeType.HasValue)
                return;

            switch (shapeType.Value)
            {
                case Models.ShapeType.Rectangle:
                    DrawRectangle(shape);
                    break;
                case Models.ShapeType.Circle:
                    DrawCircle(shape);
                    break;
                case Models.ShapeType.Polygon:
                    DrawPolygon(shape);
                    break;
                case Models.ShapeType.Line:
                    // 直线不适合做掩码区域，记录警告
                    break;
                case Models.ShapeType.Point:
                    // 点不适合做掩码区域，记录警告
                    break;
                case Models.ShapeType.RotatedRectangle:
                    DrawRotatedRectangle(shape);
                    break;
                case Models.ShapeType.Annulus:
                    DrawAnnulus(shape);
                    break;
                case Models.ShapeType.Arc:
                    // 弧形不适合做掩码区域，记录警告
                    break;
            }
        }

        /// <summary>
        /// 添加多个区域到掩码
        /// </summary>
        public void AddRegions(IEnumerable<RegionData> regions)
        {
            foreach (var region in regions)
            {
                AddRegion(region);
            }
        }

        /// <summary>
        /// 绘制矩形
        /// </summary>
        private void DrawRectangle(RegionDefinition.ShapeParameters shape)
        {
            var x = (int)(shape.CenterX - shape.Width / 2);
            var y = (int)(shape.CenterY - shape.Height / 2);
            var rect = new Rect(x, y, (int)shape.Width, (int)shape.Height);
            
            Cv2.Rectangle(_mask, rect, Scalar.White, -1);
        }

        /// <summary>
        /// 绘制圆形
        /// </summary>
        private void DrawCircle(RegionDefinition.ShapeParameters shape)
        {
            var center = new Point2d(shape.CenterX, shape.CenterY);
            var radius = (int)shape.Radius;
            
            Cv2.Circle(_mask, center, radius, Scalar.White, -1);
        }

        /// <summary>
        /// 绘制旋转矩形
        /// </summary>
        private void DrawRotatedRectangle(RegionDefinition.ShapeParameters shape)
        {
            var center = new Point2f((float)shape.CenterX, (float)shape.CenterY);
            var size = new Size2f((float)shape.Width, (float)shape.Height);
            var angle = (float)(shape.Angle * 180.0 / Math.PI); // 转换为度数
            
            var rotatedRect = new RotatedRect(center, size, angle);
            
            var points = rotatedRect.Points();
            
            // 转换为 Point 数组
            var pointArray = new OpenCvSharp.Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                pointArray[i] = new OpenCvSharp.Point((int)points[i].X, (int)points[i].Y);
            }
            
            Cv2.FillPoly(_mask, new[] { pointArray }, Scalar.White);
        }

        /// <summary>
        /// 绘制多边形
        /// </summary>
        private void DrawPolygon(RegionDefinition.ShapeParameters shape)
        {
            if (shape.Points == null || shape.Points.Count < 3)
                return;

            var pointArray = new OpenCvSharp.Point[shape.Points.Count];
            for (int i = 0; i < shape.Points.Count; i++)
            {
                pointArray[i] = new OpenCvSharp.Point(
                    (int)shape.Points[i].X,
                    (int)shape.Points[i].Y
                );
            }

            Cv2.FillPoly(_mask, new[] { pointArray }, Scalar.White);
        }

        /// <summary>
        /// 绘制圆环
        /// </summary>
        private void DrawAnnulus(RegionDefinition.ShapeParameters shape)
        {
            var center = new Point2d(shape.CenterX, shape.CenterY);
            var innerRadius = (int)shape.Radius;
            var outerRadius = (int)shape.OuterRadius;

            if (outerRadius <= 0)
                outerRadius = innerRadius + 20; // 默认外环

            // 绘制外圆
            Cv2.Circle(_mask, center, outerRadius, Scalar.White, -1);

            // 绘制内圆（黑色，实现圆环效果）
            Cv2.Circle(_mask, center, innerRadius, Scalar.Black, -1);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _mask?.Dispose();
        }
    }
}
