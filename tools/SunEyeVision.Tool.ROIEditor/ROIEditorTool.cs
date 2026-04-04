using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Tool.ROIEditor.Views;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器工具
    /// </summary>
    [Tool("roi-editor", "ROI编辑器",
        Description = "创建和编辑感兴趣区域（ROI）",
        Icon = "📐",
        Category = "图像处理",
        Version = "1.0.0")]
    public class ROIEditorTool : IToolPlugin<ROIEditorParameters, ROIEditorResults>
    {
        public bool HasDebugWindow => true;

        public FrameworkElement? CreateDebugControl()
        {
            return new ROIEditorToolDebugWindow();
        }

        [Obsolete("使用 CreateDebugControl 替代")]
        public System.Windows.Window? CreateDebugWindow()
        {
            return new ROIEditorToolDebugWindow();
        }

        public ROIEditorResults Run(Mat image, ROIEditorParameters parameters)
        {
            var result = new ROIEditorResults();
            var sw = Stopwatch.StartNew();
            
            try
            {
                parameters.LogInfo($"开始ROI编辑器处理");

                result.ROIs = parameters?.ROIs ?? new List<ROIData>();
                result.ROICount = result.ROIs.Count;

                parameters.LogInfo($"ROI数量: {result.ROICount}");

                if (image != null && !image.Empty())
                {
                    // 绘制ROI到输出图像
                    var outputImage = image.Clone();
                    foreach (var roi in result.ROIs)
                    {
                        DrawROI(outputImage, roi);
                    }
                    result.OutputImage = outputImage;
                    parameters.LogInfo($"已在输出图像上绘制 {result.ROICount} 个ROI");
                }

                sw.Stop();
                result.SetSuccess(sw.ElapsedMilliseconds);
                parameters.LogInfo($"ROI编辑器处理完成，耗时: {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                sw.Stop();
                parameters.LogError($"ROI编辑器处理异常: {ex.Message}", ex);
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        private void DrawROI(Mat image, ROIData roi)
        {
            var color = Scalar.Red;
            var thickness = 2;

            switch (roi.Type)
            {
                case "Rectangle":
                    Cv2.Rectangle(image,
                        new Rect((int)roi.X, (int)roi.Y, (int)roi.Width, (int)roi.Height),
                        color, thickness);
                    break;

                case "Circle":
                    Cv2.Circle(image,
                        new Point((int)roi.X, (int)roi.Y),
                        (int)roi.Radius,
                        color, thickness);
                    break;

                case "RotatedRectangle":
                    var center = new Point2f((float)roi.X, (float)roi.Y);
                    var size = new Size2f((float)roi.Width, (float)roi.Height);
                    var rotatedRect = new RotatedRect(center, size, (float)roi.Rotation);
                    Cv2.Rectangle(image, rotatedRect.BoundingRect(), color, thickness);
                    break;

                case "Line":
                    Cv2.Line(image,
                        new Point((int)roi.X, (int)roi.Y),
                        new Point((int)roi.EndX, (int)roi.EndY),
                        color, thickness);
                    break;
            }
        }

    }
}
