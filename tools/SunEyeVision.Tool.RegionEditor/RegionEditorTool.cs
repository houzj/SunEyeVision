using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.RegionEditor.Views;

namespace SunEyeVision.Tool.RegionEditor
{
    /// <summary>
    /// 区域编辑器工具
    /// </summary>
    public class RegionEditorTool : ITool<RegionEditorParameters, RegionEditorResults>
    {
        public string Name => "区域编辑器";
        public string Description => "创建和编辑检测区域";
        public string Version => "1.0.0";
        public string Category => "图像处理";

        // 显式实现 ITool.HasDebugWindow
        bool ITool.HasDebugWindow => true;

        // 显式实现 ITool.CreateDebugWindow
        System.Windows.Window? ITool.CreateDebugWindow()
        {
            return new RegionEditorToolDebugWindow();
        }

        public RegionEditorResults Run(Mat image, RegionEditorParameters parameters)
        {
            var result = new RegionEditorResults();
            var sw = Stopwatch.StartNew();

            try
            {
                result.Regions = parameters?.Regions ?? new List<RegionData>();
                result.RegionCount = result.Regions.Count;

                // 解析所有区域
                var resolver = new RegionResolver();
                result.ResolvedRegions = resolver.ResolveAll(result.Regions);

                if (image != null && !image.Empty())
                {
                    // 绘制区域到输出图像
                    var outputImage = image.Clone();
                    foreach (var resolved in result.ResolvedRegions)
                    {
                        if (resolved.IsValid)
                        {
                            DrawRegion(outputImage, resolved);
                        }
                    }
                    result.OutputImage = outputImage;
                }

                result.SetSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 绘制区域
        /// </summary>
        private void DrawRegion(Mat image, ResolvedRegion region)
        {
            var color = GetScalarColor(region.SourceData?.DisplayColor ?? 0xFFFF0000);
            var thickness = 2;

            switch (region.ShapeType)
            {
                case ShapeType.Point:
                    Cv2.Circle(image, 
                        new Point((int)region.CenterX, (int)region.CenterY),
                        3, color, -1);
                    break;

                case ShapeType.Line:
                    Cv2.Line(image,
                        new Point((int)region.StartX, (int)region.StartY),
                        new Point((int)region.EndX, (int)region.EndY),
                        color, thickness);
                    break;

                case ShapeType.Circle:
                    Cv2.Circle(image,
                        new Point((int)region.CenterX, (int)region.CenterY),
                        (int)region.Radius,
                        color, thickness);
                    break;

                case ShapeType.Rectangle:
                    var rect = new Rect(
                        (int)(region.CenterX - region.Width / 2),
                        (int)(region.CenterY - region.Height / 2),
                        (int)region.Width,
                        (int)region.Height);
                    Cv2.Rectangle(image, rect, color, thickness);
                    break;

                case ShapeType.RotatedRectangle:
                    DrawRotatedRectangle(image, region, color, thickness);
                    break;

                case ShapeType.Polygon:
                    if (region.Points.Count >= 3)
                    {
                        var points = new Point[region.Points.Count];
                        for (int i = 0; i < region.Points.Count; i++)
                        {
                            points[i] = new Point((int)region.Points[i].X, (int)region.Points[i].Y);
                        }
                        Cv2.Polylines(image, new[] { points }, true, color, thickness);
                    }
                    break;

                case ShapeType.Annulus:
                    Cv2.Circle(image,
                        new Point((int)region.CenterX, (int)region.CenterY),
                        (int)region.OuterRadius, color, thickness);
                    Cv2.Circle(image,
                        new Point((int)region.CenterX, (int)region.CenterY),
                        (int)region.Radius, color, thickness);
                    break;

                case ShapeType.Arc:
                    // 绘制弧形（简化实现）
                    Cv2.Ellipse(image,
                        new Point((int)region.CenterX, (int)region.CenterY),
                        new Size((int)region.Radius, (int)region.Radius),
                        region.StartAngle,
                        0,
                        region.EndAngle - region.StartAngle,
                        color, thickness);
                    break;
            }
        }

        /// <summary>
        /// 绘制旋转矩形
        /// </summary>
        private void DrawRotatedRectangle(Mat image, ResolvedRegion region, Scalar color, int thickness)
        {
            var center = new Point2f((float)region.CenterX, (float)region.CenterY);
            var size = new Size2f((float)region.Width, (float)region.Height);
            var rotatedRect = new RotatedRect(center, size, (float)region.Angle);

            // 获取旋转矩形的四个顶点
            var points = rotatedRect.Points();

            // 绘制轮廓
            for (int i = 0; i < 4; i++)
            {
                Cv2.Line(image,
                    new Point((int)points[i].X, (int)points[i].Y),
                    new Point((int)points[(i + 1) % 4].X, (int)points[(i + 1) % 4].Y),
                    color, thickness);
            }
        }

        /// <summary>
        /// 将ARGB颜色转换为Scalar
        /// </summary>
        private Scalar GetScalarColor(uint argb)
        {
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);

            // OpenCV使用BGR格式
            return new Scalar(b, g, r, a);
        }

        public ValidationResult ValidateParameters(RegionEditorParameters parameters)
        {
            return parameters?.Validate() ?? new ValidationResult();
        }

        public RegionEditorParameters GetDefaultParameters()
        {
            return new RegionEditorParameters();
        }
    }
}
