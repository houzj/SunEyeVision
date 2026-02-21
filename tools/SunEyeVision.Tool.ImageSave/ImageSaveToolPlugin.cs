using System;
using System.Collections.Generic;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Tool.ImageSave
{
    /// <summary>
    /// 图像保存工具插件
    /// </summary>
    [ToolPlugin("image_save", "ImageSave")]
    public class ImageSaveToolPlugin : IToolPlugin
    {
        #region 插件基本信息
        public string Name => "图像保存";
        public string Version => "1.0.0";
        public string Author => "SunEyeVision";
        public string Description => "保存图像到文件";
        public string PluginId => "suneye.image_save";
        public string Icon => "💾";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理
        public List<Type> GetAlgorithmNodes() => new List<Type> { typeof(ImageSaveAlgorithm) };

        public List<ToolMetadata> GetToolMetadata()
        {
            return new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "image_save",
                    Name = "ImageSave",
                    DisplayName = "图像保存",
                    Icon = "💾",
                    Category = "输出",
                    Description = "保存图像到文件",
                    AlgorithmType = typeof(ImageSaveAlgorithm),
                    Version = "1.0.0",
                    Author = "SunEyeVision",
                    HasDebugInterface = true,
                    InputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "outputPath",
                            DisplayName = "输出路径",
                            Description = "图像保存路径",
                            Type = ParameterType.String,
                            DefaultValue = "output/image",
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "outputFormat",
                            DisplayName = "输出格式",
                            Description = "图像保存格式",
                            Type = ParameterType.Enum,
                            DefaultValue = "png",
                            Options = new object[] { "jpg", "jpeg", "png", "bmp", "tiff" },
                            Required = true,
                            Category = "基本参数"
                        },
                        new ParameterMetadata
                        {
                            Name = "overwrite",
                            DisplayName = "覆盖已存在文件",
                            Description = "如果文件已存在是否覆盖",
                            Type = ParameterType.Bool,
                            DefaultValue = true,
                            Required = false,
                            Category = "高级参数"
                        }
                    },
                    OutputParameters = new List<ParameterMetadata>
                    {
                        new ParameterMetadata
                        {
                            Name = "savedPath",
                            DisplayName = "保存路径",
                            Description = "实际保存的文件路径",
                            Type = ParameterType.String
                        }
                    },
                    HasSideEffects = true,  // 有副作用（写入文件）
                    SupportCaching = false,  // 不支持缓存（每次都应该执行）
                    MaxRetryCount = 2,  // 保存失败最多重试2次
                    RetryDelayMs = 500  // 重试延迟500ms
                }
            };
        }

        public IImageProcessor CreateToolInstance(string toolId) => new ImageSaveAlgorithm();

        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var parameters = new AlgorithmParameters();
            parameters.Set("outputPath", "output/image");
            parameters.Set("outputFormat", "png");
            parameters.Set("overwrite", true);
            return parameters;
        }

        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var result = new ValidationResult();

            var outputPath = parameters.Get<string>("outputPath");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                result.AddError("输出路径不能为空");
            }

            var outputFormat = parameters.Get<string>("outputFormat");
            if (string.IsNullOrWhiteSpace(outputFormat))
            {
                result.AddError("输出格式不能为空");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        #endregion
    }

    /// <summary>
    /// 图像保存算法实现（简化版）
    /// </summary>
    public class ImageSaveAlgorithm : IImageProcessor
    {
        public string Name => "图像保存";
        public string Description => "保存图像到文件";

        public object? Process(object image)
        {
            // 简化实现：仅返回保存状态
            return new { Saved = true, Path = "output/image.png" };
        }
    }
}
