using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Validation;

namespace MyPlugin
{
    /// <summary>
    /// 阈值化工具插件示例 - 展示完整的插件开发流程
    /// </summary>
    /// <remarks>
    /// 这是 SunEyeVision 插件开发的完整示例，包含：
    /// 1. IToolPlugin 接口的完整实现
    /// 2. ITool&lt;TParams, TResult&gt; 强类型工具的实现
    /// 3. 参数定义、验证和处理逻辑
    /// 
    /// 开发步骤：
    /// 1. 复制此文件并修改命名空间
    /// 2. 修改 ToolPlugin 特性的 ToolId 和 Name
    /// 3. 定义参数类（ThresholdToolParameters）和结果类（ThresholdToolResults）
    /// 4. 实现业务逻辑（Execute 方法）
    /// </remarks>
    [ToolPlugin("myplugin-threshold", "Threshold", Version = "1.0.0", Category = "图像处理")]
    public class ThresholdToolPlugin : IToolPlugin
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
                            Name = "ThresholdValue",
                            DisplayName = "阈值",
                            Description = "二值化的阈值(0-255)",
                            Type = ParamDataType.Int,
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
                            Type = ParamDataType.Int,
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
                            Type = ParamDataType.Image,
                            Required = true
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 创建工具实例
        /// </summary>
        public ITool? CreateToolInstance(string toolId)
        {
            if (toolId != PluginId)
                return null;
            
            return new ThresholdTool();
        }

        /// <summary>
        /// 获取默认参数值
        /// </summary>
        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            if (toolId != PluginId)
                return new AlgorithmParameters();

            var parameters = new AlgorithmParameters();
            parameters.Set("ThresholdValue", 128);
            parameters.Set("MaxValue", 255);
            return parameters;
        }

        #endregion
    }

    #region 参数和结果定义

    /// <summary>
    /// 阈值化工具参数
    /// </summary>
    public class ThresholdToolParameters : ToolParameters
    {
        /// <summary>
        /// 阈值 (0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1)]
        [ParameterDisplay(DisplayName = "阈值", Description = "二值化的阈值(0-255)", Group = "基本参数", Order = 1)]
        public int ThresholdValue { get; set; } = 128;

        /// <summary>
        /// 最大值 (0-255)
        /// </summary>
        [ParameterRange(0, 255, Step = 1)]
        [ParameterDisplay(DisplayName = "最大值", Description = "超过阈值时设置的值", Group = "基本参数", Order = 2)]
        public int MaxValue { get; set; } = 255;

        /// <summary>
        /// 验证参数
        /// </summary>
        public override ValidationResult Validate()
        {
            var result = base.Validate();

            if (ThresholdValue < 0 || ThresholdValue > 255)
                result.AddError($"阈值必须在 0-255 范围内，当前值: {ThresholdValue}");

            if (MaxValue < 0 || MaxValue > 255)
                result.AddError($"最大值必须在 0-255 范围内，当前值: {MaxValue}");

            return result;
        }
    }

    /// <summary>
    /// 阈值化工具结果
    /// </summary>
    public class ThresholdToolResults : ToolResults
    {
        /// <summary>
        /// 输出图像
        /// </summary>
        public Mat? OutputImage { get; set; }

        /// <summary>
        /// 处理的像素数
        /// </summary>
        public int ProcessedPixels { get; set; }
    }

    #endregion

    #region 工具实现

    /// <summary>
    /// 阈值化工具 - 实现实际的图像处理逻辑
    /// </summary>
    public class ThresholdTool : ITool<ThresholdToolParameters, ThresholdToolResults>
    {
        public string Name => "阈值化处理";
        public string Description => "将灰度图像转换为二值图像";
        public string Version => "1.0.0";
        public string Category => "图像处理";

        /// <summary>
        /// 执行工具
        /// </summary>
        public ThresholdToolResults Run(Mat image, ThresholdToolParameters parameters)
        {
            var result = new ThresholdToolResults();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 执行二值化处理
                var outputImage = new Mat();
                Cv2.Threshold(image, outputImage, parameters.ThresholdValue, parameters.MaxValue, ThresholdTypes.Binary);

                result.OutputImage = outputImage;
                result.ProcessedPixels = image.Rows * image.Cols;
                result.SetSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 异步执行工具
        /// </summary>
        public Task<ThresholdToolResults> RunAsync(Mat image, ThresholdToolParameters parameters)
        {
            return Task.Run(() => Run(image, parameters));
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public ValidationResult ValidateParameters(ThresholdToolParameters parameters)
        {
            if (parameters == null)
                return ValidationResult.Failure("参数不能为空");
            return parameters.Validate();
        }

        /// <summary>
        /// 获取默认参数
        /// </summary>
        public ThresholdToolParameters GetDefaultParameters()
        {
            return new ThresholdToolParameters();
        }
    }

    #endregion
}
