using System;
using System.Collections.Generic;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 图像阈值化工具插件 - 支持强类型参数和数据绑定
    /// </summary>
    [ToolPlugin("threshold", "Threshold")]
    public class ThresholdToolPlugin : IToolPlugin
    {
        private readonly ThresholdTool _tool;
        private List<ToolMetadata>? _cachedMetadata;

        #region 插件基本信息
        public string Name => "图像阈值化";
        public string Version => "2.0.0";
        public string Author => "SunEyeVision";
        public string Description => "将灰度图像转换为二值图像";
        public string PluginId => "suneye.threshold";
        public string Icon => "📷";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        public ThresholdToolPlugin()
        {
            _tool = new ThresholdTool();
        }

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
                    Id = "threshold",
                    Name = "Threshold",
                    DisplayName = "图像阈值化",
                    Icon = "📷",
                    Category = "图像处理",
                    Description = "将灰度图像转换为二值图像",
                    AlgorithmType = typeof(ThresholdTool),
                    Version = Version,
                    Author = Author,
                    HasDebugInterface = true,
                    // 使用强类型参数生成的元数据
                    InputParameters = GenerateInputParameterMetadata(),
                    OutputParameters = GenerateOutputParameterMetadata(),
                    // 标记支持数据绑定
                    SupportsDataBinding = true,
                    ParameterType = typeof(ThresholdParameters),
                    ResultType = typeof(ThresholdResults)
                }
            };

            return _cachedMetadata;
        }

        /// <summary>
        /// 创建工具实例
        /// </summary>
        public ITool? CreateToolInstance(string toolId)
        {
            return toolId == "threshold" ? new ThresholdTool() : null;
        }

        /// <summary>
        /// 创建强类型工具实例
        /// </summary>
        public ITool<ThresholdParameters, ThresholdResults> CreateTypedToolInstance()
        {
            return new ThresholdTool();
        }

        /// <summary>
        /// 获取默认参数
        /// </summary>
        public AlgorithmParameters GetDefaultParameters(string toolId)
        {
            var typedParams = _tool.GetDefaultParameters();
            return ConvertToAlgorithmParameters(typedParams);
        }

        /// <summary>
        /// 获取默认强类型参数
        /// </summary>
        public ThresholdParameters GetDefaultTypedParameters()
        {
            return _tool.GetDefaultParameters();
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        public ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters)
        {
            var typedParams = ConvertToTypedParameters(parameters);
            return _tool.ValidateParameters(typedParams);
        }

        #endregion

        #region 参数转换

        /// <summary>
        /// 将AlgorithmParameters转换为ThresholdParameters
        /// </summary>
        public static ThresholdParameters ConvertToTypedParameters(AlgorithmParameters parameters)
        {
            var result = new ThresholdParameters();

            if (parameters.TryGet<int>("threshold", out var threshold))
                result.Threshold = threshold;

            if (parameters.TryGet<int>("maxValue", out var maxValue))
                result.MaxValue = maxValue;

            if (parameters.TryGet<string>("type", out var typeStr))
            {
                if (Enum.TryParse<ThresholdType>(typeStr, out var type))
                    result.Type = type;
            }

            if (parameters.TryGet<string>("adaptiveMethod", out var adaptiveMethodStr))
            {
                if (Enum.TryParse<AdaptiveMethod>(adaptiveMethodStr, out var method))
                    result.AdaptiveMethod = method;
            }

            if (parameters.TryGet<int>("blockSize", out var blockSize))
                result.BlockSize = blockSize;

            if (parameters.TryGet<bool>("invert", out var invert))
                result.Invert = invert;

            return result;
        }

        /// <summary>
        /// 将ThresholdParameters转换为AlgorithmParameters
        /// </summary>
        public static AlgorithmParameters ConvertToAlgorithmParameters(ThresholdParameters parameters)
        {
            var result = new AlgorithmParameters();
            result.Set("threshold", parameters.Threshold);
            result.Set("maxValue", parameters.MaxValue);
            result.Set("type", parameters.Type.ToString());
            result.Set("adaptiveMethod", parameters.AdaptiveMethod.ToString());
            result.Set("blockSize", parameters.BlockSize);
            result.Set("invert", parameters.Invert);
            return result;
        }

        #endregion

        #region 元数据生成

        /// <summary>
        /// 从强类型参数生成输入参数元数据
        /// </summary>
        private List<ParameterMetadata> GenerateInputParameterMetadata()
        {
            var defaultParams = _tool.GetDefaultParameters();
            var metadata = new List<ParameterMetadata>();

            // 阈值
            metadata.Add(new ParameterMetadata
            {
                Name = "Threshold",
                DisplayName = "阈值",
                Description = "二值化的阈值(0-255)",
                Type = ParamDataType.Int,
                DefaultValue = defaultParams.Threshold,
                MinValue = 0,
                MaxValue = 255,
                Required = true,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = true
            });

            // 最大值
            metadata.Add(new ParameterMetadata
            {
                Name = "MaxValue",
                DisplayName = "最大值",
                Description = "超过阈值时使用的最大值(0-255)",
                Type = ParamDataType.Int,
                DefaultValue = defaultParams.MaxValue,
                MinValue = 0,
                MaxValue = 255,
                Required = true,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = true
            });

            // 阈值类型
            metadata.Add(new ParameterMetadata
            {
                Name = "Type",
                DisplayName = "阈值类型",
                Description = "二值化方法",
                Type = ParamDataType.Enum,
                DefaultValue = defaultParams.Type.ToString(),
                Options = ThresholdParameters.GetThresholdTypeOptions() as object[] ?? Array.Empty<object>(),
                Required = true,
                Category = "基本参数",
                SupportsBinding = true
            });

            // 自适应方法
            metadata.Add(new ParameterMetadata
            {
                Name = "AdaptiveMethod",
                DisplayName = "自适应方法",
                Description = "自适应阈值方法",
                Type = ParamDataType.Enum,
                DefaultValue = defaultParams.AdaptiveMethod.ToString(),
                Options = ThresholdParameters.GetAdaptiveMethodOptions() as object[] ?? Array.Empty<object>(),
                Required = false,
                Category = "高级参数",
                SupportsBinding = true
            });

            // 块大小
            metadata.Add(new ParameterMetadata
            {
                Name = "BlockSize",
                DisplayName = "块大小",
                Description = "计算阈值的邻域大小(奇数)",
                Type = ParamDataType.Int,
                DefaultValue = defaultParams.BlockSize,
                MinValue = 3,
                MaxValue = 31,
                Required = false,
                Category = "高级参数",
                SupportsBinding = true
            });

            // 反转结果
            metadata.Add(new ParameterMetadata
            {
                Name = "Invert",
                DisplayName = "反转结果",
                Description = "是否反转二值化结果",
                Type = ParamDataType.Bool,
                DefaultValue = defaultParams.Invert,
                Required = false,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = true
            });

            return metadata;
        }

        /// <summary>
        /// 生成输出参数元数据
        /// </summary>
        private List<ParameterMetadata> GenerateOutputParameterMetadata()
        {
            return new List<ParameterMetadata>
            {
                new ParameterMetadata
                {
                    Name = "OutputImage",
                    DisplayName = "输出图像",
                    Description = "二值化后的图像",
                    Type = ParamDataType.Image
                },
                new ParameterMetadata
                {
                    Name = "ThresholdUsed",
                    DisplayName = "实际阈值",
                    Description = "实际使用的阈值",
                    Type = ParamDataType.Double
                },
                new ParameterMetadata
                {
                    Name = "MaxValueUsed",
                    DisplayName = "实际最大值",
                    Description = "实际使用的最大值",
                    Type = ParamDataType.Int
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
