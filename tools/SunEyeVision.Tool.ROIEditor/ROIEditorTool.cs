using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.ROIEditor.Views;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器工具
    /// </summary>
    public class ROIEditorTool : ITool<ROIEditorParameters, ROIEditorResults>
    {
        public string Name => "ROI编辑器";
        public string Description => "创建和编辑感兴趣区域（ROI）";
        public string Version => "1.0.0";
        public string Category => "图像处理";

        // 显式实现 ITool.HasDebugWindow
        bool ITool.HasDebugWindow => true;

        // 显式实现 ITool.CreateDebugWindow
        System.Windows.Window? ITool.CreateDebugWindow()
        {
            return new ROIEditorToolDebugWindow();
        }

        public ROIEditorResults Run(Mat image, ROIEditorParameters parameters)
        {
            var result = new ROIEditorResults();
            var sw = Stopwatch.StartNew();
            
            try
            {
                result.ROIs = parameters?.ROIs ?? new List<ROIData>();
                result.ROICount = result.ROIs.Count;

                if (image != null && !image.Empty())
                {
                    // 绘制ROI到输出图像
                    var outputImage = image.Clone();
                    foreach (var roi in result.ROIs)
                    {
                        DrawROI(outputImage, roi);
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

        public ValidationResult ValidateParameters(ROIEditorParameters parameters) => new ValidationResult();
        
        public ROIEditorParameters GetDefaultParameters() => new ROIEditorParameters();
    }
}
