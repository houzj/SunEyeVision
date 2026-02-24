using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.Core;

namespace SunEyeVision.Tool.ImageCapture
{
    /// <summary>
    /// 图像采集工具插件
    /// </summary>
    [ToolPlugin("image_capture", "ImageCapture")]
    public class ImageCaptureToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "图像采集";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "从相机采集图像";
        public string PluginId => "suneye.image_capture";
        public string Icon => "📷";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(ImageCaptureAlgorithm) };

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "image_capture",
                    Name = "ImageCapture",
                    DisplayName = "图像采集",
                    Icon = "📷",
                    Category = "采集",
                    Description = "从相机采集图像",
                    AlgorithmType = typeof(ImageCaptureAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "cameraId",
                            DisplayName = "相机ID",
                            Description = "相机的设备ID",
                            Type = ParameterType.Int,
                            DefaultValue = 0,
                            MinValue = 0,
                            MaxValue = 10,
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "timeout",
                            DisplayName = "超时时间(ms)",
                            Description = "采集超时时间(毫秒)",
                            Type = ParameterType.Int,
                            DefaultValue = 5000,
                            MinValue = 100,
                            MaxValue = 60000,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "triggerMode",
                            DisplayName = "触发模式",
                            Description = "相机触发模式",
                            Type = ParameterType.Enum,
                            DefaultValue = "Soft",
                            Options = new object[] { "Soft", "Hard", "Continuous" },
                            Required = true,
                            Category = "基本参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "outputImage",
                            DisplayName = "输出图像",
                            Description = "采集到的图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "timestamp",
                            DisplayName = "时间戳",
                            Description = "采集时间戳",
                            Type = ParameterType.Double
                        }
                    }
                }
            };
        }

        public IImageProcessor CreateToolInstance(string toolId) => new ImageCaptureAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("cameraId", 0);
            parameters.Set("timeout", 5000);
            parameters.Set("triggerMode", "Soft");
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var cameraId = parameters.Get<int>("cameraId");
            if (cameraId == null || cameraId < 0)
            {
                result.AddError("相机ID必须大于等于0");
            }

            var timeout = parameters.Get<int>("timeout");
            if (timeout != null && timeout < 100)
            {
                result.AddWarning("超时时间过短，可能导致采集失败");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        #endregion
    }

    /// <summary>
    /// 图像采集算法实现
    /// </summary>
    public class ImageCaptureAlgorithm : ImageProcessorBase
    {
        public override string Name => "图像采集";
        public override string Description => "从相机采集图像";

        protected override ImageProcessResult ProcessImage(object image, AlgorithmParameters parameters)
        {
            var cameraId = GetParameter(parameters, "cameraId", 0);
            var timeout = GetParameter(parameters, "timeout", 5000);
            var triggerMode = GetParameter(parameters, "triggerMode", "Soft");

            // TODO: 实际相机采集逻辑

            return ImageProcessResult.FromData(new
            {
                CameraId = cameraId,
                Timeout = timeout,
                TriggerMode = triggerMode,
                Timestamp = System.DateTime.Now.Ticks,
                ProcessedAt = System.DateTime.Now
            });
        }

        protected override ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            var result = new ValidationResult();
            var cameraId = GetParameter<int?>(parameters, "cameraId", null);
            var timeout = GetParameter<int?>(parameters, "timeout", null);

            if (cameraId.HasValue && cameraId.Value < 0)
                result.AddError("相机ID必须大于等于0");
            if (timeout.HasValue && timeout.Value < 100)
                result.AddWarning("超时时间过短，可能导致采集失败");

            return result;
        }
    }
}
