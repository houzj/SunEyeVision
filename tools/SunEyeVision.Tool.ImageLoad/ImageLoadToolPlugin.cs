using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Tool.ImageLoad
{
    /// <summary>
    /// 图像载入工具插件 - 简化版
    /// 只负责从文件读取图像，不进行任何处理
    /// </summary>
    [ToolPlugin("image_load", "ImageLoad")]
    public class ImageLoadToolPlugin : IToolPlugin
    {
        private List<ToolMetadata>? _cachedMetadata;

        #region 插件基本信息
        public string Name => "图像载入";
        public string Version => "2.0.0";
        public string Author => "SunEyeVision";
        public string Description => "从文件载入图像，作为工作流的图像源";
        public string PluginId => "suneye.image_load";
        public string Icon => "📁";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        #region 生命周期管理
        public void Initialize() => IsLoaded = true;
        public void Unload() => IsLoaded = false;
        #endregion

        #region 工具管理

        /// <summary>
        /// 获取算法节点类型（已弃用）
        /// </summary>
        [Obsolete("此方法已弃用，请使用 CreateToolInstance 获取工具实例")]
        public List<Type> GetAlgorithmNodes() => new List<Type>();

        /// <summary>
        /// 获取工具元数据
        /// </summary>
        public List<ToolMetadata> GetToolMetadata()
        {
            if (_cachedMetadata != null)
                return _cachedMetadata;

            _cachedMetadata = new List<ToolMetadata>
            {
                new ToolMetadata
                {
                    Id = "image_load",
                    Name = "ImageLoad",
                    DisplayName = "图像载入",
                    Icon = "📁",
                    Category = "采集",
                    Description = "从文件载入图像，作为工作流的图像源",
                    AlgorithmType = typeof(ImageLoadTool),
                    Version = Version,
                    Author = Author,
                    HasDebugInterface = false, // 不需要调试界面
                    // 使用强类型参数生成的元数据
                    InputParameters = GenerateInputParameterMetadata(),
                    OutputParameters = GenerateOutputParameterMetadata(),
                    // 标记支持数据绑定
                    SupportsDataBinding = true,
                    ParameterType = typeof(ImageLoadParameters),
                    ResultType = typeof(ImageLoadResults)
                }
            };

            return _cachedMetadata;
        }

        /// <summary>
        /// 创建工具实例
        /// </summary>
        public ITool? CreateToolInstance(string toolId)
        {
            return toolId == "image_load" ? new ImageLoadTool() : null;
        }

        /// <summary>
        /// 创建强类型工具实例
        /// </summary>
        public static ITool<ImageLoadParameters, ImageLoadResults> CreateTypedToolInstance()
        {
            return new ImageLoadTool();
        }

        /// <summary>
        /// 获取默认参数
        /// </summary>
        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var typedParams = ImageLoadTool.GetDefaultParameters();
            return ConvertToAlgorithmParameters(typedParams);
        }

        /// <summary>
        /// 获取默认强类型参数
        /// </summary>
        public static ImageLoadParameters GetDefaultTypedParameters()
        {
            return ImageLoadTool.GetDefaultParameters();
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public static ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            var typedParams = ConvertToTypedParameters(parameters);
            return ImageLoadTool.ValidateParameters(typedParams);
        }

        #endregion

        #region 参数转换

        /// <summary>
        /// 将AlgorithmParameters转换为ImageLoadParameters
        /// </summary>
        public static ImageLoadParameters ConvertToTypedParameters(AlgorithmParameters parameters)
        {
            var result = new ImageLoadParameters();

            if (parameters.TryGet<string>("filePath", out var filePath))
                result.FilePath = filePath;

            return result;
        }

        /// <summary>
        /// 将ImageLoadParameters转换为AlgorithmParameters
        /// </summary>
        public static AlgorithmParameters ConvertToAlgorithmParameters(ImageLoadParameters parameters)
        {
            var result = new AlgorithmParameters();
            result.Set("filePath", parameters.FilePath);
            return result;
        }

        #endregion

        #region 元数据生成

        /// <summary>
        /// 从强类型参数生成输入参数元数据
        /// </summary>
        private static List<ParameterMetadata> GenerateInputParameterMetadata()
        {
            var defaultParams = ImageLoadTool.GetDefaultParameters();
            var metadata = new List<ParameterMetadata>();

            // 文件路径
            metadata.Add(new ParameterMetadata
            {
                Name = "FilePath",
                DisplayName = "文件路径",
                Description = "要载入的图像文件路径",
                Type = ParamDataType.FilePath,
                DefaultValue = defaultParams.FilePath,
                Required = true,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = false  // 文件路径通常不支持绑定
            });

            return metadata;
        }

        /// <summary>
        /// 生成输出参数元数据
        /// </summary>
        private static List<ParameterMetadata> GenerateOutputParameterMetadata()
        {
            return new List<ParameterMetadata>
            {
                new ParameterMetadata
                {
                    Name = "OutputImage",
                    DisplayName = "输出图像",
                    Description = "载入的图像",
                    Type = ParamDataType.Image
                },
                new ParameterMetadata
                {
                    Name = "Width",
                    DisplayName = "宽度",
                    Description = "图像宽度",
                    Type = ParamDataType.Int
                },
                new ParameterMetadata
                {
                    Name = "Height",
                    DisplayName = "高度",
                    Description = "图像高度",
                    Type = ParamDataType.Int
                },
                new ParameterMetadata
                {
                    Name = "Channels",
                    DisplayName = "通道数",
                    Description = "图像通道数",
                    Type = ParamDataType.Int
                },
                new ParameterMetadata
                {
                    Name = "FileSize",
                    DisplayName = "文件大小",
                    Description = "文件大小(字节)",
                    Type = ParamDataType.Double
                },
                new ParameterMetadata
                {
                    Name = "ExecutionTimeMs",
                    DisplayName = "执行时间",
                    Description = "处理耗时(毫秒)",
                    Type = ParamDataType.Int
                }
            };
        }

        #endregion
    }
}
