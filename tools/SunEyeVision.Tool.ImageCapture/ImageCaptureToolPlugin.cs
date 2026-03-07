using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Tool.ImageCapture.Views;

namespace SunEyeVision.Tool.ImageCapture
{
    public class ImageCaptureParameters : ToolParameters
    {
        public int CameraId { get; set; }
        public int Timeout { get; set; } = 5000;
        public override ValidationResult Validate()
        {
            var r = new ValidationResult();
            if (CameraId < 0) r.AddError("相机ID必须大于等于0");
            return r;
        }
    }

    public class ImageCaptureResults : ToolResults
    {
        [Param(DisplayName = "输出图像", Description = "采集的图像数据", Category = ParamCategory.Output)]
        public Mat? OutputImage { get; set; }
        public DateTime CaptureTime { get; set; }
    }

    [Tool("image_capture", "图像采集", Description = "从相机采集图像", Icon = "📷", Category = "采集")]
    public class ImageCaptureTool : IToolPlugin<ImageCaptureParameters, ImageCaptureResults>
    {
        public bool HasDebugWindow => true;

        public System.Windows.Window? CreateDebugWindow()
        {
            return new ImageCaptureToolDebugWindow();
        }

        public ImageCaptureResults Run(Mat image, ImageCaptureParameters parameters)
        {
            var result = new ImageCaptureResults();
            var sw = Stopwatch.StartNew();
            try
            {
                // TODO: 实际相机采集实现
                // 这里返回一个占位图像
                result.OutputImage = new Mat(480, 640, MatType.CV_8UC3, Scalar.Black);
                result.CaptureTime = DateTime.Now;
                result.SetSuccess(sw.ElapsedMilliseconds);
            }
            catch (Exception ex) { result.SetError($"采集失败: {ex.Message}"); }
            return result;
        }
    }
}
