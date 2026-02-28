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
        public string Version => "2.0.0";
        public string Author => "SunEyeVision";
        public string Description => "应用高斯模糊滤波";
        public string PluginId => "suneye.gaussian_blur";
        public string Icon => "🖼️";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type>();

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "gaussian_blur",
                    Name = "GaussianBlur",
                    DisplayName = "高斯模糊",
                    Icon = "🖼️",
                    Category = "图像处理",
                    Description = "应用高斯模糊滤波",
                    Version = Version,
                    Author = Author,
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "KernelSize",
                            DisplayName = "核大小",
                            Description = "高斯核大小，必须为奇数",
                            Type = ParamDataType.Int,
                            DefaultValue = 5,
                            MinValue = 3,
                            MaxValue = 99,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "Sigma",
                            DisplayName = "标准差",
                            Description = "高斯核的标准差",
                            Type = ParamDataType.Double,
                            DefaultValue = 1.5,
                            MinValue = 0.1,
                            MaxValue = 10.0,
                            Required = false,
                            Category = "基本参数",
                            EditableInDebug = true
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "OutputImage",
                            DisplayName = "输出图像",
                            Description = "模糊后的图像",
                            Type = ParamDataType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "ExecutionTimeMs",
                            DisplayName = "处理时间(ms)",
                            Description = "算法执行时间",
                            Type = ParamDataType.Int
                        }
                    }
                }
            };
        }

        public ITool? CreateToolInstance(string toolId) => 
            toolId == "gaussian_blur" ? new GaussianBlurTool() : null;

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("KernelSize", 5);
            parameters.Set("Sigma", 1.5);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var result = new ValidationResult();
            var kernelSize = parameters.Get<int>("KernelSize");

            if (kernelSize < 3 || kernelSize > 99)
                result.AddError("核大小必须在3-99之间");
            else if (kernelSize % 2 == 0)
                result.AddError("核大小必须为奇数");

            return result;
        }
        #endregion
    }

    #region 参数和结果定义

    public class GaussianBlurParameters : ToolParameters
    {
        public int KernelSize { get; set; } = 5;
        public double Sigma { get; set; } = 1.5;

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();
            if (KernelSize < 3 || KernelSize > 99)
                result.AddError("核大小必须在3-99之间");
            else if (KernelSize % 2 == 0)
                result.AddError("核大小必须为奇数");
            if (Sigma <= 0)
                result.AddWarning("标准差应大于0");
            return result;
        }
    }

    public class GaussianBlurResults : ToolResults
    {
        public Mat? OutputImage { get; set; }
        public int KernelSizeUsed { get; set; }
        public double SigmaUsed { get; set; }
    }

    #endregion

    #region 工具实现

    public class GaussianBlurTool : ITool<GaussianBlurParameters, GaussianBlurResults>
    {
        public string Name => "高斯模糊";
        public string Description => "应用高斯模糊滤波";
        public string Version => "2.0.0";
        public string Category => "图像处理";

        public GaussianBlurResults Run(Mat image, GaussianBlurParameters parameters)
        {
            var result = new GaussianBlurResults();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (image == null || image.Empty())
                {
                    result.SetError("输入图像为空");
                    return result;
                }

                var outputImage = new Mat();
                Cv2.GaussianBlur(image, outputImage, new Size(parameters.KernelSize, parameters.KernelSize), parameters.Sigma);

                result.OutputImage = outputImage;
                result.KernelSizeUsed = parameters.KernelSize;
                result.SigmaUsed = parameters.Sigma;
                result.SetSuccess(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        public ValidationResult ValidateParameters(GaussianBlurParameters parameters) => parameters.Validate();
        public GaussianBlurParameters GetDefaultParameters() => new GaussianBlurParameters();
    }

    #endregion
}
