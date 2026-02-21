using System;
using System.Collections.Generic;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Infrastructure;

namespace SunEyeVision.Tools.OCRTool
{
    /// <summary>
    /// OCR识别工具插件示例
    /// </summary>
    [ToolPlugin("ocr_recognition", "OCR")]
    public class OCRTool : IToolPlugin
    {
        public string Name => "OCR识别";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "光学字符识别";
        public string PluginId => "suneye.ocr";
        public List<string> Dependencies => new List<string>();
        public string Icon => "📝";

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
                typeof(OCRAlgorithm)
            };
        }

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "ocr_recognition",
                    Name = "OCR",
                    DisplayName = "OCR识别",
                    Icon = "📝",
                    Category = "识别",
                    Description = "光学字符识别",
                    AlgorithmType = typeof(OCRAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "language",
                            DisplayName = "识别语言",
                            Description = "OCR识别的语言",
                            Type = ParameterType.Enum,
                            DefaultValue = "CN",
                            Options = new object[] { "CN", "EN", "JP", "KR", "Mixed" },
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "confThreshold",
                            DisplayName = "置信度阈值",
                            Description = "识别结果的最低置信度(0-100)",
                            Type = ParameterType.Double,
                            DefaultValue = 80.0,
                            MinValue = 0.0,
                            MaxValue = 100.0,
                            Required = true,
                            Category = "基本参数",
                            EditableInDebug = true
                        },
                        new ParameterMetadata
                        {
                            Name = "whitelist",
                            DisplayName = "白名单",
                            Description = "允许的字符集(正则表达式)",
                            Type = ParameterType.String,
                            DefaultValue = "",
                            Required = false,
                            Category = "高级参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "enableDenoise",
                            DisplayName = "启用降噪",
                            Description = "是否对图像进行降噪处理",
                            Type = ParameterType.Bool,
                            DefaultValue = true,
                            Required = false,
                            Category = "图像预处理"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "text",
                            DisplayName = "识别文本",
                            Description = "识别到的文本内容",
                            Type = ParameterType.String
                        },
                        new ParameterMetadata
                        {
                            Name = "confidence",
                            DisplayName = "置信度",
                            Description = "识别结果的整体置信度",
                            Type = ParameterType.Double
                        },
                        new ParameterMetadata
                        {
                            Name = "charCount",
                            DisplayName = "字符数量",
                            Description = "识别到的字符数量",
                            Type = ParameterType.Int
                        }
                    }
                }
            };
        }

        public SunEyeVision.Core.Interfaces.IImageProcessor CreateToolInstance(string toolId)
        {
            return new OCRAlgorithm();
        }

        public SunEyeVision.Core.Models.AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new SunEyeVision.Core.Models.AlgorithmParameters();
            parameters.Set("language", "CN");
            parameters.Set("confThreshold", 80.0);
            parameters.Set("whitelist", "");
            parameters.Set("enableDenoise", true);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, SunEyeVision.Core.Models.AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var confThreshold = parameters.Get<double>("confThreshold");
            if (confThreshold != null && (confThreshold < 0 || confThreshold > 100))
            {
                result.AddError("置信度阈值必须在0-100之间");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }

    /// <summary>
    /// OCR算法实现（简化版，仅用于演示）
    /// </summary>
    public class OCRAlgorithm : SunEyeVision.Core.Interfaces.IImageProcessor
    {
        public string Name => "OCR识别";
        public string Description => "光学字符识别";

        public object? Process(object image)
        {
            // 简化实现：仅返回识别文本
            return "Hello SunEyeVision";
        }
    }
}
