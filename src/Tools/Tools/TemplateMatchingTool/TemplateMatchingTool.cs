using System;
using System.Collections.Generic;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Infrastructure;

namespace SunEyeVision.Tools.TemplateMatchingTool
{
    /// <summary>
    /// 模板匹配定位工具插件示例
    /// </summary>
    [ToolPlugin("template_matching", "TemplateMatching")]
    public class TemplateMatchingTool : IToolPlugin
    {
        public string Name => "模板匹配定位";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "使用模板匹配进行定位";
        public string PluginId => "suneye.template_matching";
        public List<string> Dependencies => new List<string>();
        public string Icon => "🎯";

        private bool _isLoaded;

        public bool IsLoaded => _isLoaded;

        public void Initialize()
        {
            _isLoaded = true;
        }

        public void Unload()
        {
            _isLoaded = false;
        }

        public List<Type> GetAlgorithmNodes()
        {
            return new List<Type>
            {
                typeof(TemplateMatchingAlgorithm)
            };
        }

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "template_matching",
                    Name = "TemplateMatching",
                    DisplayName = "模板匹配定位",
                    Icon = "🎯",
                    Category = "定位",
                    Description = "使用模板匹配进行定位",
                    AlgorithmType = typeof(TemplateMatchingAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "templateImage",
                            DisplayName = "模板图像",
                            Description = "用于匹配的模板图像",
                            Type = ParameterType.Image,
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "threshold",
                            DisplayName = "匹配阈值",
                            Description = "匹配分数阈值(0-1)",
                            Type = ParameterType.Double,
                            DefaultValue = 0.8,
                            MinValue = 0.0,
                            MaxValue = 1.0,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "method",
                            DisplayName = "匹配方法",
                            Description = "模板匹配算法方法",
                            Type = ParameterType.Enum,
                            DefaultValue = "SqDiffNormed",
                            Options = new object[] { "SqDiffNormed", "CCorrNormed", "CCoeffNormed" },
                            Required = true,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "minSize",
                            DisplayName = "最小尺寸",
                            Description = "目标最小尺寸",
                            Type = ParameterType.Int,
                            DefaultValue = 10,
                            MinValue = 1,
                            MaxValue = 1000,
                            Required = false,
                            Category = "高级参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "score",
                            DisplayName = "匹配分数",
                            Description = "最佳匹配分数",
                            Type = ParameterType.Double
                        },
                        new ParameterMetadata
                        {
                            Name = "position",
                            DisplayName = "匹配位置",
                            Description = "匹配到的中心点坐标",
                            Type = ParameterType.Point
                        },
                        new ParameterMetadata
                        {
                            Name = "matchCount",
                            DisplayName = "匹配数量",
                            Description = "匹配到的目标数量",
                            Type = ParameterType.Int
                        }
                    }
                }
            };
        }

        public SunEyeVision.Core.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            return new TemplateMatchingAlgorithm();
        }

        public SunEyeVision.Core.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new SunEyeVision.Core.Models.AlgorithmParameters();
            parameters.Set("threshold", 0.8);
            parameters.Set("method", "SqDiffNormed");
            parameters.Set("minSize", 10);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Core.Models.AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var threshold = parameters.Get<double>("threshold");
            if (threshold != null && (threshold < 0 || threshold > 1))
            {
                result.AddError("匹配阈值必须在0-1之间");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// 模板匹配算法实现（简化版，仅用于演示）
    /// </summary>
    public class TemplateMatchingAlgorithm : SunEyeVision.Core.Interfaces.IImageProcessor
    {
        public string Name => "模板匹配定位";
        public string Description => "使用模板匹配进行定位";

        public object? Process(object image)
        {
            // 简化实现：仅返回匹配分数
            return 0.95;
        }
    }
}
