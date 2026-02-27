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

namespace SunEyeVision.Tool.ImageCapture
{
    [ToolPlugin("image_capture", "ImageCapture")]
    public class ImageCaptureToolPlugin : IToolPlugin
    {
        public string Name => "图像采集";
        public string Version => "2.0.0";
        public string Author => "SunEyeVision";
        public string Description => "从相机采集图像";
        public string PluginId => "suneye.image_capture";
        public string Icon => "📷";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        public List<Type> GetAlgorithmNodes() => new List<Type>();

        public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>
        {
            new ToolMetadata
            {
                Id = "image_capture",
                Name = "ImageCapture",
                DisplayName = "图像采集",
                Icon = "📷",
                Category = "采集",
                Description = "从相机采集图像",
                Version = Version,
                Author = Author,
                InputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata { Name = "CameraId", DisplayName = "相机ID", Type = ParamDataType.Int, DefaultValue = 0 },
                    new ParameterMetadata { Name = "Timeout", DisplayName = "超时时间(ms)", Type = ParamDataType.Int, DefaultValue = 5000 }
                },
                OutputParameters = new List<ParameterMetadata>
                {
                    new ParameterMetadata { Name = "OutputImage", DisplayName = "输出图像", Type = ParamDataType.Image }
                }
            }
        };

        public ITool? CreateToolInstance(string toolId) => toolId == "image_capture" ? new ImageCaptureTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var p = new AlgorithmParameters();
            p.Set("CameraId", 0);
            p.Set("Timeout", 5000);
            return p;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters) => new ValidationResult();
    }

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
        public Mat? OutputImage { get; set; }
        public DateTime CaptureTime { get; set; }
    }

    public class ImageCaptureTool : ITool<ImageCaptureParameters, ImageCaptureResults>
    {
        public string Name => "图像采集";
        public string Description => "从相机采集图像";
        public string Version => "2.0.0";
        public string Category => "采集";

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

        public Task<ImageCaptureResults> RunAsync(Mat image, ImageCaptureParameters parameters)
        {
            return Task.Run(() => Run(image, parameters));
        }

        public ValidationResult ValidateParameters(ImageCaptureParameters parameters) => parameters.Validate();
        public ImageCaptureParameters GetDefaultParameters() => new ImageCaptureParameters();
    }
}
