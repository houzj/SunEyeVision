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

namespace SunEyeVision.Tool.EdgeDetection
{
    /// <summary>
    /// 边缘检测工具插件 - 支持强类型参数和数据绑定
    /// </summary>
    [ToolPlugin("edge_detection", "EdgeDetection")]
    public class EdgeDetectionToolPlugin : IToolPlugin
    {
        private readonly EdgeDetectionTool _tool;
        private List<ToolMetadata>? _cachedMetadata;

        #region 插件基本信息
        public string Name => "边缘检测";
        public string Version => "2.1.0";
        public string Author => "SunEyeVision";
        public string Description => "检测图像中的边缘";
        public string PluginId => "suneye.edge_detection";
        public string Icon => "📐";
        public List<string> Dependencies => new List<string>();
        public bool IsLoaded { get; private set; }
        #endregion

        public EdgeDetectionToolPlugin()
        {
            _tool = new EdgeDetectionTool();
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
                    Id = "edge_detection",
                    Name = "EdgeDetection",
                    DisplayName = "边缘检测",
                    Icon = "📐",
                    Category = "图像处理",
                    Description = "检测图像中的边缘",
                    AlgorithmType = typeof(EdgeDetectionTool),
                    Version = Version,
                    Author = Author,
                    HasDebugInterface = true,
                    // 使用强类型参数生成的元数据
                    InputParameters = GenerateInputParameterMetadata(),
                    OutputParameters = GenerateOutputParameterMetadata(),
                    // 标记支持数据绑定
                    SupportsDataBinding = true,
                    ParameterType = typeof(EdgeDetectionParameters),
                    ResultType = typeof(EdgeDetectionResults)
                }
            };

            return _cachedMetadata;
        }

        /// <summary>
        /// 创建工具实例
        /// </summary>
        public ITool? CreateToolInstance(string toolId)
        {
            return toolId == "edge_detection" ? new EdgeDetectionTool() : null;
        }

        /// <summary>
        /// 创建强类型工具实例
        /// </summary>
        public ITool<EdgeDetectionParameters, EdgeDetectionResults> CreateTypedToolInstance()
        {
            return new EdgeDetectionTool();
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
        public EdgeDetectionParameters GetDefaultTypedParameters()
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
        /// 将AlgorithmParameters转换为EdgeDetectionParameters
        /// </summary>
        public static EdgeDetectionParameters ConvertToTypedParameters(AlgorithmParameters parameters)
        {
            var result = new EdgeDetectionParameters();

            if (parameters.TryGet<double>("Threshold1", out var threshold1))
                result.Threshold1 = threshold1;

            if (parameters.TryGet<double>("Threshold2", out var threshold2))
                result.Threshold2 = threshold2;

            if (parameters.TryGet<int>("ApertureSize", out var apertureSize))
                result.ApertureSize = apertureSize;

            return result;
        }

        /// <summary>
        /// 将EdgeDetectionParameters转换为AlgorithmParameters
        /// </summary>
        public static AlgorithmParameters ConvertToAlgorithmParameters(EdgeDetectionParameters parameters)
        {
            var result = new AlgorithmParameters();
            result.Set("Threshold1", parameters.Threshold1);
            result.Set("Threshold2", parameters.Threshold2);
            result.Set("ApertureSize", parameters.ApertureSize);
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

            // 低阈值
            metadata.Add(new ParameterMetadata
            {
                Name = "Threshold1",
                DisplayName = "低阈值",
                Description = "Canny边缘检测的第一个滞后阈值",
                Type = ParamDataType.Double,
                DefaultValue = defaultParams.Threshold1,
                MinValue = 0.0,
                MaxValue = 255.0,
                Required = true,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = true
            });

            // 高阈值
            metadata.Add(new ParameterMetadata
            {
                Name = "Threshold2",
                DisplayName = "高阈值",
                Description = "Canny边缘检测的第二个滞后阈值",
                Type = ParamDataType.Double,
                DefaultValue = defaultParams.Threshold2,
                MinValue = 0.0,
                MaxValue = 255.0,
                Required = true,
                Category = "基本参数",
                EditableInDebug = true,
                SupportsBinding = true
            });

            // 孔径大小
            metadata.Add(new ParameterMetadata
            {
                Name = "ApertureSize",
                DisplayName = "孔径大小",
                Description = "Sobel算子的孔径大小(3、5、7)",
                Type = ParamDataType.Int,
                DefaultValue = defaultParams.ApertureSize,
                MinValue = 3,
                MaxValue = 7,
                Required = false,
                Category = "高级参数",
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
                    Description = "边缘检测结果图像",
                    Type = ParamDataType.Image
                },
                new ParameterMetadata
                {
                    Name = "EdgeCount",
                    DisplayName = "边缘数量",
                    Description = "检测到的边缘轮廓数量",
                    Type = ParamDataType.Int
                },
                new ParameterMetadata
                {
                    Name = "Threshold1Used",
                    DisplayName = "实际低阈值",
                    Description = "实际使用的低阈值",
                    Type = ParamDataType.Double
                },
                new ParameterMetadata
                {
                    Name = "Threshold2Used",
                    DisplayName = "实际高阈值",
                    Description = "实际使用的高阈值",
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

    #region 工具实现

    public class EdgeDetectionTool : ITool<EdgeDetectionParameters, EdgeDetectionResults>
    {
        public string Name => "边缘检测";
        public string Description => "检测图像中的边缘";
        public string Version => "2.1.0";
        public string Category => "图像处理";

        public EdgeDetectionResults Run(Mat image, EdgeDetectionParameters parameters)
        {
            var result = new EdgeDetectionResults();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (image == null || image.Empty())
                {
                    result.SetError("输入图像为空");
                    return result;
                }

                // 确保输入是灰度图
                Mat grayImage = image;
                if (image.Channels() > 1)
                {
                    grayImage = new Mat();
                    Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
                }

                var outputImage = new Mat();
                Cv2.Canny(grayImage, outputImage, parameters.Threshold1, parameters.Threshold2, parameters.ApertureSize);

                // 计算边缘数量
                Cv2.FindContours(outputImage, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                result.OutputImage = outputImage;
                result.EdgeCount = contours.Length;
                result.Threshold1Used = parameters.Threshold1;
                result.Threshold2Used = parameters.Threshold2;
                result.ApertureSizeUsed = parameters.ApertureSize;
                result.InputSize = new OpenCvSharp.Size(image.Width, image.Height);
                result.SetSuccess(stopwatch.ElapsedMilliseconds);

                if (grayImage != image)
                    grayImage.Dispose();
            }
            catch (Exception ex)
            {
                result.SetError($"处理失败: {ex.Message}");
            }

            return result;
        }

        public Task<EdgeDetectionResults> RunAsync(Mat image, EdgeDetectionParameters parameters)
        {
            return Task.Run(() => Run(image, parameters));
        }

        public ValidationResult ValidateParameters(EdgeDetectionParameters parameters) => parameters.Validate();
        public EdgeDetectionParameters GetDefaultParameters() => new EdgeDetectionParameters();
    }

    #endregion
}
