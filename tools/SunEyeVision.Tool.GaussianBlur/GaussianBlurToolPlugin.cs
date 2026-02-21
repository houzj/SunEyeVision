using System;
using System.Collections.Generic;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Tool.GaussianBlur
{
    /// <summary>
    /// 高斯模糊工具插件
    /// </summary>
    [ToolPlugin("gaussian_blur", "GaussianBlur")]
    public class GaussianBlurToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "高斯模糊";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "应用高斯模糊滤镜";
        public string PluginId => "suneye.gaussian_blur";
        public string Icon => "🌫️";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(GaussianBlurAlgorithm) };

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "gaussian_blur",
                    Name = "GaussianBlur",
                    DisplayName = "高斯模糊",
                    Icon = "🌫️",
                    Category = "图像处理",
                    Description = "应用高斯模糊滤镜",
                    AlgorithmType = typeof(GaussianBlurAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "kernelSize",
                            DisplayName = "核大小",
                            Description = "高斯核大小(必须为奇数)",
                            Type = ParameterType.Int,
                            DefaultValue = 5,
                            MinValue = 3,
                            MaxValue = 99,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "sigma",
                            DisplayName = "标准差",
                            Description = "高斯核的标准差",
                            Type = ParameterType.Double,
                            DefaultValue = 1.5,
                            MinValue = 0.1,
                            MaxValue = 10.0,
                            Required = false,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "borderType",
                            DisplayName = "边界类型",
                            Description = "像素外推方法",
                            Type = ParameterType.Enum,
                            DefaultValue = "Reflect",
                            Options = new object[] { "Reflect", "Constant", "Replicate", "Default" },
                            Required = false,
                            Category = "高级参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "outputImage",
                            DisplayName = "输出图像",
                            Description = "模糊后的图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "processTime",
                            DisplayName = "处理时间(ms)",
                            Description = "算法执行时间",
                            Type = ParameterType.Double
                        }
                    }
                }
            };
        }

        public IImageProcessor CreateToolInstance(string toolId) => new GaussianBlurAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("kernelSize", 5);
            parameters.Set("sigma", 1.5);
            parameters.Set("borderType", "Reflect");
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var kernelSize = parameters.Get<int>("kernelSize");
            if (kernelSize == null || kernelSize < 3 || kernelSize > 99)
            {
                result.AddError("核大小必须在3-99之间");
            }
            else if (kernelSize % 2 == 0)
            {
                result.AddError("核大小必须为奇数");
            }

            var sigma = parameters.Get<double>("sigma");
            if (sigma != null && sigma <= 0)
            {
                result.AddWarning("标准差应大于0");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        #endregion
    }

    /// <summary>
    /// 高斯模糊算法实现（简化版）
    /// </summary>
    public class GaussianBlurAlgorithm : IImageProcessor
    {
        public string Name => "高斯模糊";
        public string Description => "应用高斯模糊滤镜";

        public object? Process(object image)
        {
            // 简化实现：仅返回处理时间
            return 10.5;
        }
    }
}
