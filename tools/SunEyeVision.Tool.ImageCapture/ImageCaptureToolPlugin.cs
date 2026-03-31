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
using System.Text.Json.Serialization;

namespace SunEyeVision.Tool.ImageCapture
{
    /// <summary>
    /// 图像采集参数
    /// </summary>
    /// <remarks>
    /// 多态序列化（rule-010: 方案系统实现规范）：
    /// 使用 [JsonDerivedType] 特性标识参数类型，类型标识符为 "ImageCapture"。
    /// </remarks>
    [JsonDerivedType(typeof(ImageCaptureParameters), "ImageCapture")]
    public class ImageCaptureParameters : ToolParameters
    {
        private int _cameraId = 0;
        private int _timeout = 5000;

        /// <summary>
        /// 相机设备ID
        /// </summary>
        [ParameterDisplay(DisplayName = "相机ID", Description = "相机设备ID（0表示第一个相机）", Group = "基本参数", Order = 1)]
        public int CameraId
        {
            get => _cameraId;
            set => SetProperty(ref _cameraId, value, "相机ID");
        }

        /// <summary>
        /// 采集超时时间
        /// </summary>
        [ParameterRange(1000, 30000, Step = 1000)]
        [ParameterDisplay(DisplayName = "超时时间", Description = "采集超时时间（毫秒）", Group = "基本参数", Order = 2)]
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value, "超时时间");
        }

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
        public int CameraIdUsed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// 获取结果项列表
        /// </summary>
        public override IReadOnlyList<ResultItem> GetResultItems()
        {
            var items = new List<ResultItem>();
            items.AddNumeric("CameraIdUsed", CameraIdUsed, "");
            items.AddNumeric("Width", Width, "像素");
            items.AddNumeric("Height", Height, "像素");
            items.AddText("CaptureTime", CaptureTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            items.AddNumeric("ExecutionTimeMs", ExecutionTimeMs, "ms");
            return items;
        }
    }

    [Tool("image_capture", "图像采集", Description = "从相机采集图像", Icon = "📷", Category = "采集", Version = "2.0.0", HasDebugWindow = true)]
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
                // 验证参数
                var validationResult = parameters.Validate();
                if (!validationResult.IsValid)
                {
                    parameters.LogError($"参数验证失败: {string.Join(", ", validationResult.Errors)}");
                    result.SetError($"参数验证失败: {string.Join(", ", validationResult.Errors)}");
                    return result;
                }

                parameters.LogInfo($"开始图像采集: 相机ID={parameters.CameraId}, 超时={parameters.Timeout}ms");

                // TODO: 实际相机采集实现
                // 这里返回一个占位图像
                result.OutputImage = new Mat(480, 640, MatType.CV_8UC3, Scalar.Black);
                result.CaptureTime = DateTime.Now;
                result.CameraIdUsed = parameters.CameraId;
                result.Width = result.OutputImage.Width;
                result.Height = result.OutputImage.Height;

                result.SetSuccess(sw.ElapsedMilliseconds);

                parameters.LogInfo($"图像采集完成: {result.Width}x{result.Height}, 耗时{sw.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                sw.Stop();
                parameters.LogError($"图像采集异常: {ex.Message}", ex);
                result.SetError($"采集失败: {ex.Message}");
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            }

            return result;
        }
    }
}
