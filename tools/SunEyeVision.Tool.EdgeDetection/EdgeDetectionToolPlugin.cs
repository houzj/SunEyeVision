using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;

namespace SunEyeVision.Tool.EdgeDetection
{
    /// <summary>
    /// 边缘检测工具插件
    /// </summary>
    [ToolPlugin("edge_detection", "EdgeDetection")]
    public class EdgeDetectionToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "边缘检测";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "检测图像中的边缘";
        public string PluginId => "suneye.edge_detection";
        public string Icon => "📐";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(EdgeDetectionAlgorithm) };

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "edge_detection",
                    Name = "EdgeDetection",
                    DisplayName = "边缘检测",
                    Icon = "📐",
                    Category = "图像处理",
                    Description = "检测图像中的边缘",
                    AlgorithmType = typeof(EdgeDetectionAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "method",
                            DisplayName = "检测方法",
                            Description = "边缘检测算法",
                            Type = ParameterType.Enum,
                            DefaultValue = "Canny",
                            Options = new object[] { "Canny", "Sobel", "Laplacian", "Scharr" },
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "threshold1",
                            DisplayName = "低阈值",
                            Description = "第一个滞后阈值",
                            Type = ParameterType.Double,
                            DefaultValue = 50.0,
                            MinValue = 0.0,
                            MaxValue = 255.0,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "threshold2",
                            DisplayName = "高阈值",
                            Description = "第二个滞后阈值",
                            Type = ParameterType.Double,
                            DefaultValue = 150.0,
                            MinValue = 0.0,
                            MaxValue = 255.0,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "apertureSize",
                            DisplayName = "孔径大小",
                            Description = "Sobel算子的孔径大小",
                            Type = ParameterType.Int,
                            DefaultValue = 3,
                            MinValue = 1,
                            MaxValue = 7,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "L2gradient",
                            DisplayName = "L2梯度",
                            Description = "是否使用更精确的L2范数计算梯度幅值",
                            Type = ParameterType.Bool,
                            DefaultValue = true,
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "kernelSize",
                            DisplayName = "卷积核大小",
                            Description = "Laplacian算子的孔径大小",
                            Type = ParameterType.Int,
                            DefaultValue = 3,
                            MinValue = 1,
                            MaxValue = 5,
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
                            Description = "边缘检测结果图像",
                            Type = ParameterType.Image
                        },
                        new ParameterMetadata
                        {
                            Name = "edgeCount",
                            DisplayName = "边缘数量",
                            Description = "检测到的边缘轮廓数量",
                            Type = ParameterType.Int
                        }
                    }
                }
            };
        }

        public IImageProcessor CreateToolInstance(string toolId) => new EdgeDetectionAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("method", "Canny");
            parameters.Set("threshold1", 50.0);
            parameters.Set("threshold2", 150.0);
            parameters.Set("apertureSize", 3);
            parameters.Set("L2gradient", true);
            parameters.Set("kernelSize", 3);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var result = new ValidationResult();
            var threshold1 = parameters.Get<double>("threshold1");
            var threshold2 = parameters.Get<double>("threshold2");
            if (threshold1 == null || threshold1 < 0 || threshold1 > 255)
                result.AddError("低阈值必须在0-255之间");
            if (threshold2 == null || threshold2 < 0 || threshold2 > 255)
                result.AddError("高阈值必须在0-255之间");
            if (threshold1 != null && threshold2 != null && threshold1 >= threshold2)
                result.AddWarning("通常情况下低阈值应小于高阈值");
            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        #endregion
    }

    /// <summary>
    /// 边缘检测算法实现
    /// </summary>
    public class EdgeDetectionAlgorithm : ImageProcessorBase
    {
        public override string Name => "边缘检测";
        public override string Description => "检测图像中的边缘";

        protected override ImageProcessResult ProcessImage(object image, AlgorithmParameters parameters)
        {
            var method = GetParameter(parameters, "method", "Canny");
            var threshold1 = GetParameter(parameters, "threshold1", 50.0);
            var threshold2 = GetParameter(parameters, "threshold2", 150.0);
            var apertureSize = GetParameter(parameters, "apertureSize", 3);
            // TODO: 实际图像处理逻辑
            return ImageProcessResult.FromData(new
            {
                Method = method,
                Threshold1 = threshold1,
                Threshold2 = threshold2,
                ApertureSize = apertureSize,
                EdgeCount = 0,
                ProcessedAt = DateTime.Now
            });
        }

        protected override ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            var result = new ValidationResult();
            var threshold1 = GetParameter<double?>(parameters, "threshold1", null);
            var threshold2 = GetParameter<double?>(parameters, "threshold2", null);
            if (threshold1.HasValue && (threshold1.Value < 0 || threshold1.Value > 255))
                result.AddError("低阈值必须在0-255之间");
            if (threshold2.HasValue && (threshold2.Value < 0 || threshold2.Value > 255))
                result.AddError("高阈值必须在0-255之间");
            if (threshold1.HasValue && threshold2.HasValue && threshold1.Value >= threshold2.Value)
                result.AddWarning("通常情况下低阈值应小于高阈值");
            return result;
        }
    }
}
