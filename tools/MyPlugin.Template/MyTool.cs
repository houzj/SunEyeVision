using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.Core;

namespace MyPlugin
{
    /// <summary>
    /// 阈值化工具插件示例 - 展示完整的插件开发流程
    /// </summary>
    /// <remarks>
    /// 这是 SunEyeVision 插件开发的完整示例，包含：
    /// 1. IToolPlugin 接口的完整实现
    /// 2. IImageProcessor 图像处理器的实现
    /// 3. 参数定义、验证和处理逻辑
    /// 
    /// 开发步骤：
    /// 1. 复制此文件并修改命名空间
    /// 2. 修改 ToolPlugin 特性的 ToolId 和 Name
    /// 3. 实现业务逻辑（ThresholdProcessor.Process 方法）
    /// 4. 根据需要添加更多参数
    /// </remarks>
    [ToolPlugin("myplugin-threshold", "Threshold", Version = "1.0.0", Category = "图像处理")]
    public class ThresholdTool : IToolPlugin
    {
        #region 插件基本信息

        public string Name => "Threshold";
        public string Version => "1.0.0";
        public string PluginId => "myplugin-threshold";
        public string Description => "图像二值化处理 - 将灰度图像转换为二值图像";
        public string Icon => "🔲";
        public string Author => "SunEyeVision Team";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }

        #endregion

        #region 生命周期管理

        public void Initialize()
        {
            // 插件初始化：加载资源、初始化状态等
            IsLoaded = true;
        }

        public void Unload()
        {
            // 插件卸载：释放资源、清理状态等
            IsLoaded = false;
        }

        #endregion

        #region 工具管理

        /// <summary>
        /// 定义工具的输入输出参数
        /// </summary>
        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = PluginId,
                    Name = Name,
                    DisplayName = "阈值化处理",
                    Description = Description,
                    Icon = Icon,
                    Category = "图像处理",
                    Version = Version,
                    Author = Author,
                    
                    // 输入参数定义
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "InputImage",
                            DisplayName = "输入图像",
                            Description = "待处理的灰度图像",
                            Type = ParameterType.Image,
                            Required = true
                        },
                        new ParameterMetadata
                        {
                            Name = "ThresholdValue",
                            DisplayName = "阈值",
                            Description = "二值化的阈值 (0-255)",
                            Type = ParameterType.Int,
                            DefaultValue = 128,
                            MinValue = 0,
                            MaxValue = 255,
                            Required = true
                        },
                        new ParameterMetadata
                        {
                            Name = "MaxValue",
                            DisplayName = "最大值",
                            Description = "超过阈值时设置的值",
                            Type = ParameterType.Int,
                            DefaultValue = 255,
                            MinValue = 0,
                            MaxValue = 255,
                            Required = false
                        }
                    },
                    
                    // 输出参数定义
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "OutputImage",
                            DisplayName = "输出图像",
                            Description = "二值化后的图像",
                            Type = ParameterType.Image,
                            Required = true
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 获取算法节点类型（可选）
        /// </summary>
        public List<Type> GetAlgorithmNodes()
        {
            // 如果有自定义算法节点，返回其类型列表
            return new List<Type>();
        }

        /// <summary>
        /// 创建图像处理器实例
        /// </summary>
        public IImageProcessor CreateToolInstance(string toolId)
        {
            if (toolId != PluginId)
                throw new ArgumentException($"Unknown tool ID: {toolId}");
            
            return new ThresholdProcessor();
        }

        /// <summary>
        /// 获取默认参数值
        /// </summary>
        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            if (toolId != PluginId)
                throw new ArgumentException($"Unknown tool ID: {toolId}");

            var parameters = new AlgorithmParameters();
            parameters.Set("ThresholdValue", 128);
            parameters.Set("MaxValue", 255);
            return parameters;
        }

        /// <summary>
        /// 验证参数有效性
        /// </summary>
        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            if (toolId != PluginId)
                return ValidationResult.Failure($"Unknown tool ID: {toolId}");

            var result = new ValidationResult();

            // 验证阈值范围
            var threshold = parameters.Get<int>("ThresholdValue");
            if (threshold < 0 || threshold > 255)
            {
                result.AddError($"阈值必须在 0-255 范围内，当前值: {threshold}");
            }

            // 验证最大值范围
            var maxValue = parameters.Get<int>("MaxValue");
            if (maxValue < 0 || maxValue > 255)
            {
                result.AddError($"最大值必须在 0-255 范围内，当前值: {maxValue}");
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// 阈值化图像处理器 - 实现实际的图像处理逻辑
    /// </summary>
    public class ThresholdProcessor : IImageProcessor
    {
        /// <summary>
        /// 处理图像 - 实现二值化算法
        /// </summary>
        /// <param name="image">输入图像（具体类型取决于你的图像框架）</param>
        /// <returns>处理后的二值图像</returns>
        public object? Process(object image)
        {
            // TODO: 实现实际的图像处理逻辑
            // 
            // 示例伪代码（根据实际使用的图像库调整）:
            // 
            // var inputImage = image as YourImageType;
            // if (inputImage == null) return null;
            // 
            // int threshold = GetParameter<int>("ThresholdValue");
            // int maxValue = GetParameter<int>("MaxValue");
            // 
            // var outputImage = new YourImageType(inputImage.Width, inputImage.Height);
            // 
            // for (int y = 0; y < inputImage.Height; y++)
            // {
            //     for (int x = 0; x < inputImage.Width; x++)
            //     {
            //         var pixel = inputImage.GetPixel(x, y);
            //         var gray = (pixel.R + pixel.G + pixel.B) / 3;
            //         var newPixel = gray > threshold ? maxValue : 0;
            //         outputImage.SetPixel(x, y, newPixel);
            //     }
            // }
            // 
            // return outputImage;

            // 占位返回 - 替换为实际实现
            return image;
        }
    }
}
