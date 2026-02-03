using SunEyeVision.Core.Interfaces.Plugins;
using System.Collections.Generic;

namespace SunEyeVision.Plugins.ImageProcessing
{
    /// <summary>
    /// 图像处理插件
    /// 实现IPlugin和IAlgorithmPlugin接口
    /// 使用Auto模式（零代码UI）
    /// </summary>
    public class ImageProcessingPlugin : IAlgorithmPlugin
    {
        public string PluginId => "ImageProcessing";
        public string PluginName => "Image Processing Plugin";
        public string Version => "1.0.0";
        public string Description => "Provides image processing algorithms";
        public string Author => "Team A";

        public string AlgorithmType => "ImageProcessing";
        public string Icon => "image.png";
        public string Category => "Image Processing";

        private bool _isInitialized = false;
        private bool _isRunning = false;

        public void Initialize()
        {
            _isInitialized = true;
        }

        public void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Cleanup()
        {
            _isInitialized = false;
        }

        public ParameterMetadata[] GetParameters()
        {
            return new[]
            {
                new ParameterMetadata
                {
                    Name = "brightness",
                    DisplayName = "Brightness",
                    Type = "int",
                    DefaultValue = 0,
                    MinValue = -100,
                    MaxValue = 100,
                    Description = "Adjust image brightness (-100 to 100)"
                },
                new ParameterMetadata
                {
                    Name = "contrast",
                    DisplayName = "Contrast",
                    Type = "double",
                    DefaultValue = 1.0,
                    MinValue = 0.1,
                    MaxValue = 3.0,
                    Description = "Adjust image contrast (0.1 to 3.0)"
                },
                new ParameterMetadata
                {
                    Name = "grayscale",
                    DisplayName = "Grayscale",
                    Type = "bool",
                    DefaultValue = false,
                    Description = "Convert to grayscale"
                },
                new ParameterMetadata
                {
                    Name = "filterType",
                    DisplayName = "Filter Type",
                    Type = "string",
                    DefaultValue = "None",
                    Options = new object[] { "None", "Blur", "Sharpen", "EdgeDetection" },
                    Description = "Apply filter to image"
                }
            };
        }

        public object Execute(object inputImage, Dictionary<string, object> parameters)
        {
            if (!_isRunning)
            {
                throw new System.InvalidOperationException("Plugin is not running");
            }


            // 这里应该实现实际的图像处理逻辑
            // 当前版本仅返回输入图像作为示例
            return inputImage;
        }

        public bool ValidateParameters(Dictionary<string, object> parameters)
        {
            // 验证参数
            if (!parameters.ContainsKey("brightness"))
                return false;
            if (!parameters.ContainsKey("contrast"))
                return false;
            if (!parameters.ContainsKey("grayscale"))
                return false;
            if (!parameters.ContainsKey("filterType"))
                return false;

            var brightness = (int)parameters["brightness"];
            if (brightness < -100 || brightness > 100)
                return false;

            var contrast = (double)parameters["contrast"];
            if (contrast < 0.1 || contrast > 3.0)
                return false;

            return true;
        }
    }
}
