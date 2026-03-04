using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.Threshold
{
    public class ThresholdToolViewModel : ToolViewModelBase
    {
        private string _thresholdType = "Binary";
        private int _threshold = 128;
        private int _maxValue = 255;
        private double _adaptiveBlockSize = 11;
        private double _adaptiveC = 2;

        #region 图像源选择

        private ImageSourceInfo? _selectedImageSource;

        /// <summary>
        /// 当前选中的图像源
        /// </summary>
        public ImageSourceInfo? SelectedImageSource
        {
            get => _selectedImageSource;
            set => SetProperty(ref _selectedImageSource, value);
        }

        /// <summary>
        /// 可用图像源列表（由工作流上下文提供）
        /// </summary>
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; }
            = new ObservableCollection<ImageSourceInfo>();

        /// <summary>
        /// 可用绑定源列表（用于参数绑定）
        /// </summary>
        public List<string> AvailableBindings { get; } = new List<string>();

        #endregion

        #region 参数绑定支持

        private BindingTypeOption _thresholdBindingType;
        private BindingTypeOption _maxValueBindingType;
        private string _availableSourcesSummary = "请在工作流中配置前驱节点";
        private string? _thresholdSourceNode;
        private string? _thresholdSourceProperty;
        private string? _maxValueSourceNode;
        private string? _maxValueSourceProperty;

        /// <summary>
        /// 绑定类型选项列表
        /// </summary>
        public ObservableCollection<BindingTypeOption> BindingTypeOptions { get; }

        /// <summary>
        /// 阈值绑定类型
        /// </summary>
        public BindingTypeOption ThresholdBindingType
        {
            get => _thresholdBindingType;
            set
            {
                if (SetProperty(ref _thresholdBindingType, value))
                {
                    OnPropertyChanged(nameof(ThresholdBindingEditor));
                    OnPropertyChanged(nameof(IsThresholdConstant));
                    OnPropertyChanged(nameof(IsThresholdDynamic));
                }
            }
        }

        /// <summary>
        /// 最大值绑定类型
        /// </summary>
        public BindingTypeOption MaxValueBindingType
        {
            get => _maxValueBindingType;
            set
            {
                if (SetProperty(ref _maxValueBindingType, value))
                {
                    OnPropertyChanged(nameof(MaxValueBindingEditor));
                    OnPropertyChanged(nameof(IsMaxValueConstant));
                    OnPropertyChanged(nameof(IsMaxValueDynamic));
                }
            }
        }

        /// <summary>
        /// 阈值是否为常量模式
        /// </summary>
        public bool IsThresholdConstant => ThresholdBindingType?.Type == BindingType.Constant;

        /// <summary>
        /// 阈值是否为动态绑定模式
        /// </summary>
        public bool IsThresholdDynamic => ThresholdBindingType?.Type == BindingType.DynamicBinding;

        /// <summary>
        /// 最大值是否为常量模式
        /// </summary>
        public bool IsMaxValueConstant => MaxValueBindingType?.Type == BindingType.Constant;

        /// <summary>
        /// 最大值是否为动态绑定模式
        /// </summary>
        public bool IsMaxValueDynamic => MaxValueBindingType?.Type == BindingType.DynamicBinding;

        /// <summary>
        /// 阈值绑定编辑器内容
        /// </summary>
        public string ThresholdBindingEditor
        {
            get
            {
                if (IsThresholdDynamic)
                {
                    return ThresholdSourceDisplay;
                }
                return Threshold.ToString();
            }
        }

        /// <summary>
        /// 最大值绑定编辑器内容
        /// </summary>
        public string MaxValueBindingEditor
        {
            get
            {
                if (IsMaxValueDynamic)
                {
                    return MaxValueSourceDisplay;
                }
                return MaxValue.ToString();
            }
        }

        /// <summary>
        /// 阈值源显示文本
        /// </summary>
        public string ThresholdSourceDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(_thresholdSourceNode) && !string.IsNullOrEmpty(_thresholdSourceProperty))
                    return $"{_thresholdSourceNode}.{_thresholdSourceProperty}";
                return "选择数据源...";
            }
        }

        /// <summary>
        /// 最大值源显示文本
        /// </summary>
        public string MaxValueSourceDisplay
        {
            get
            {
                if (!string.IsNullOrEmpty(_maxValueSourceNode) && !string.IsNullOrEmpty(_maxValueSourceProperty))
                    return $"{_maxValueSourceNode}.{_maxValueSourceProperty}";
                return "选择数据源...";
            }
        }

        /// <summary>
        /// 可用数据源摘要
        /// </summary>
        public string AvailableSourcesSummary
        {
            get => _availableSourcesSummary;
            set => SetProperty(ref _availableSourcesSummary, value);
        }

        /// <summary>
        /// 阈值源节点ID
        /// </summary>
        public string? ThresholdSourceNode
        {
            get => _thresholdSourceNode;
            set
            {
                if (SetProperty(ref _thresholdSourceNode, value))
                    OnPropertyChanged(nameof(ThresholdSourceDisplay));
            }
        }

        /// <summary>
        /// 阈值源属性
        /// </summary>
        public string? ThresholdSourceProperty
        {
            get => _thresholdSourceProperty;
            set
            {
                if (SetProperty(ref _thresholdSourceProperty, value))
                    OnPropertyChanged(nameof(ThresholdSourceDisplay));
            }
        }

        /// <summary>
        /// 最大值源节点ID
        /// </summary>
        public string? MaxValueSourceNode
        {
            get => _maxValueSourceNode;
            set
            {
                if (SetProperty(ref _maxValueSourceNode, value))
                    OnPropertyChanged(nameof(MaxValueSourceDisplay));
            }
        }

        /// <summary>
        /// 最大值源属性
        /// </summary>
        public string? MaxValueSourceProperty
        {
            get => _maxValueSourceProperty;
            set
            {
                if (SetProperty(ref _maxValueSourceProperty, value))
                    OnPropertyChanged(nameof(MaxValueSourceDisplay));
            }
        }

        #endregion

        public string ThresholdType
        {
            get => _thresholdType;
            set
            {
                SetProperty(ref _thresholdType, value);
                SetParamValue("ThresholdType", value);
            }
        }

        public int Threshold
        {
            get => _threshold;
            set
            {
                SetProperty(ref _threshold, value);
                SetParamValue("Threshold", value);
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                SetProperty(ref _maxValue, value);
                SetParamValue("MaxValue", value);
            }
        }

        public double AdaptiveBlockSize
        {
            get => _adaptiveBlockSize;
            set
            {
                if (value % 2 == 0)
                    value++;
                SetProperty(ref _adaptiveBlockSize, value);
                SetParamValue("AdaptiveBlockSize", value);
            }
        }

        public double AdaptiveC
        {
            get => _adaptiveC;
            set
            {
                SetProperty(ref _adaptiveC, value);
                SetParamValue("AdaptiveC", value);
            }
        }

        public string[] ThresholdTypes { get; } = { 
            "Binary", "BinaryInv", 
            "Trunc", "ToZero", "ToZeroInv",
            "AdaptiveMean", "AdaptiveGaussian"
        };

        public ThresholdToolViewModel()
        {
            // 初始化绑定类型选项
            BindingTypeOptions = new ObservableCollection<BindingTypeOption>
            {
                new BindingTypeOption(BindingType.Constant, "常量值", "使用固定值"),
                new BindingTypeOption(BindingType.DynamicBinding, "动态绑定", "从其他节点获取")
            };

            _thresholdBindingType = BindingTypeOptions[0];
            _maxValueBindingType = BindingTypeOptions[0];
        }

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            // 调用基类初始化（初始化 ToolRunner）
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "图像阈值化";
        }

        /// <summary>
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new ThresholdParameters
            {
                Threshold = this.Threshold,
                MaxValue = this.MaxValue,
                Type = ParseThresholdType(this.ThresholdType),
                AdaptiveMethod = ParseAdaptiveMethod(this.ThresholdType),
                BlockSize = (int)this.AdaptiveBlockSize,
                Invert = false
            };
        }

        /// <summary>
        /// 解析阈值类型字符串
        /// </summary>
        private static global::SunEyeVision.Tool.Threshold.ThresholdType ParseThresholdType(string typeStr)
        {
            return typeStr switch
            {
                "Binary" => global::SunEyeVision.Tool.Threshold.ThresholdType.Binary,
                "BinaryInv" => global::SunEyeVision.Tool.Threshold.ThresholdType.BinaryInv,
                "Trunc" => global::SunEyeVision.Tool.Threshold.ThresholdType.Trunc,
                "ToZero" => global::SunEyeVision.Tool.Threshold.ThresholdType.ToZero,
                "ToZeroInv" => global::SunEyeVision.Tool.Threshold.ThresholdType.ToZeroInv,
                "AdaptiveMean" => global::SunEyeVision.Tool.Threshold.ThresholdType.Binary,
                "AdaptiveGaussian" => global::SunEyeVision.Tool.Threshold.ThresholdType.Binary,
                _ => global::SunEyeVision.Tool.Threshold.ThresholdType.Binary
            };
        }

        /// <summary>
        /// 解析自适应方法字符串
        /// </summary>
        private static AdaptiveMethod ParseAdaptiveMethod(string typeStr)
        {
            return typeStr switch
            {
                "AdaptiveMean" => AdaptiveMethod.Mean,
                "AdaptiveGaussian" => AdaptiveMethod.Gaussian,
                _ => AdaptiveMethod.Mean
            };
        }

        /// <summary>
        /// 获取参数绑定容器（用于工作流执行）
        /// </summary>
        public ParameterBindingContainer GetBindingContainer()
        {
            var container = new ParameterBindingContainer();

            // 添加阈值绑定
            var thresholdBinding = new ParameterBinding
            {
                ParameterName = "Threshold",
                BindingType = ThresholdBindingType?.Type ?? BindingType.Constant,
                ConstantValue = Threshold,
                TargetType = typeof(int)
            };

            if (IsThresholdDynamic)
            {
                thresholdBinding.SourceNodeId = ThresholdSourceNode;
                thresholdBinding.SourceProperty = ThresholdSourceProperty;
            }

            container.SetBinding(thresholdBinding);

            // 添加最大值绑定
            var maxValueBinding = new ParameterBinding
            {
                ParameterName = "MaxValue",
                BindingType = MaxValueBindingType?.Type ?? BindingType.Constant,
                ConstantValue = MaxValue,
                TargetType = typeof(int)
            };

            if (IsMaxValueDynamic)
            {
                maxValueBinding.SourceNodeId = MaxValueSourceNode;
                maxValueBinding.SourceProperty = MaxValueSourceProperty;
            }

            container.SetBinding(maxValueBinding);

            return container;
        }

        /// <summary>
        /// 设置可用数据源（由外部工作流调用）
        /// </summary>
        public void SetAvailableDataSources(string nodeId, IDataSourceQueryService dataSourceQuery)
        {
            if (dataSourceQuery == null) return;

            var sources = dataSourceQuery.GetParentNodes(nodeId);
            AvailableSourcesSummary = sources.Count > 0 
                ? $"发现 {sources.Count} 个前驱节点可提供数据"
                : "未找到前驱节点";
        }

        /// <summary>
        /// 从工作流数据源填充可用图像源列表
        /// </summary>
        public void PopulateImageSources(WorkflowDataSourceProvider dataProvider)
        {
            AvailableImageSources.Clear();
            
            if (dataProvider == null)
            {
                AvailableSourcesSummary = "数据提供者未初始化";
                return;
            }

            // 获取所有父节点输出
            var nodeOutputs = dataProvider.GetParentNodeOutputs("Mat");
            
            foreach (var nodeOutput in nodeOutputs)
            {
                // 检查是否包含图像输出
                if (nodeOutput.DataType == "Mat" || 
                    nodeOutput.Children.Any(c => c.DataType == "Mat"))
                {
                    var imageSource = new ImageSourceInfo
                    {
                        NodeId = nodeOutput.NodeId,
                        NodeName = nodeOutput.NodeName,
                        OutputPortName = "Output"
                    };
                    
                    // 如果节点输出是复合对象，查找 Mat 属性
                    if (nodeOutput.DataType != "Mat" && nodeOutput.Children.Any())
                    {
                        var matChild = nodeOutput.Children.FirstOrDefault(c => 
                            c.PropertyPath == "OutputImage" && c.DataType == "Mat");
                        if (matChild != null)
                        {
                            imageSource.OutputPortName = matChild.PropertyPath;
                        }
                    }
                    
                    AvailableImageSources.Add(imageSource);
                }
            }
            
            AvailableSourcesSummary = AvailableImageSources.Count > 0
                ? $"发现 {AvailableImageSources.Count} 个图像源"
                : "未找到图像源";
        }

        /// <summary>
        /// 使用外部图像运行工具
        /// </summary>
        public void RunToolWithImage(OpenCvSharp.Mat inputImage)
        {
            if (inputImage == null || inputImage.Empty())
            {
                StatusMessage = "输入图像为空";
                return;
            }

            try
            {
                // 设置当前图像
                CurrentImage = inputImage;
                
                // 使用基类的执行方法
                RunTool();
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"运行出错: {ex.Message}";
                DebugMessage = $"异常: {ex.Message}\n{ex.StackTrace}";
            }
        }

        #region 结果显示属性

        private double _executionTimeMs;
        private bool _isSuccess;
        private string _inputSizeDisplay = "-";
        private OpenCvSharp.Size _inputSize;

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public double ExecutionTimeMs
        {
            get => _executionTimeMs;
            private set => SetProperty(ref _executionTimeMs, value);
        }

        /// <summary>
        /// 是否执行成功
        /// </summary>
        public bool IsSuccess
        {
            get => _isSuccess;
            private set
            {
                if (SetProperty(ref _isSuccess, value))
                {
                    OnPropertyChanged(nameof(StatusIcon));
                }
            }
        }

        /// <summary>
        /// 输入图像尺寸显示文本
        /// </summary>
        public string InputSizeDisplay
        {
            get => _inputSizeDisplay;
            private set => SetProperty(ref _inputSizeDisplay, value);
        }

        /// <summary>
        /// 状态图标（✅ 或 ❌）
        /// </summary>
        public string StatusIcon => IsSuccess ? "✅" : "❌";

        /// <summary>
        /// 重写执行完成回调，更新结果显示属性
        /// </summary>
        protected override void OnExecutionCompleted(Plugin.SDK.Core.RunResult result)
        {
            base.OnExecutionCompleted(result);

            IsSuccess = result.IsSuccess;
            ExecutionTimeMs = result.ExecutionTimeMs;

            if (result.IsSuccess && result.ToolResult is ThresholdResults thresholdResult)
            {
                _inputSize = thresholdResult.InputSize;
                InputSizeDisplay = $"{thresholdResult.InputSize.Width} × {thresholdResult.InputSize.Height}";
            }
            else
            {
                InputSizeDisplay = "-";
            }
        }

        #endregion

        #region 配置持久化

        /// <summary>
        /// 保存配置到节点参数（用于持久化）
        /// </summary>
        /// <param name="parameters">节点参数字典</param>
        public void SaveToNodeParameters(IDictionary<string, object> parameters)
        {
            if (parameters == null) return;

            // 保存图像源选择
            if (SelectedImageSource != null)
            {
                parameters["ImageSource_NodeId"] = SelectedImageSource.NodeId;
                parameters["ImageSource_OutputPortName"] = SelectedImageSource.OutputPortName ?? "Output";
            }
            else
            {
                parameters.Remove("ImageSource_NodeId");
                parameters.Remove("ImageSource_OutputPortName");
            }

            // 保存阈值参数
            parameters["Threshold"] = Threshold;
            parameters["MaxValue"] = MaxValue;
            parameters["ThresholdType"] = ThresholdType;
            parameters["AdaptiveBlockSize"] = AdaptiveBlockSize;
            parameters["AdaptiveC"] = AdaptiveC;

            // 保存绑定配置
            parameters["ThresholdBindingType"] = ThresholdBindingType?.Type.ToString() ?? "Constant";
            parameters["MaxValueBindingType"] = MaxValueBindingType?.Type.ToString() ?? "Constant";

            if (!string.IsNullOrEmpty(ThresholdSourceNode))
                parameters["ThresholdSourceNode"] = ThresholdSourceNode;
            if (!string.IsNullOrEmpty(ThresholdSourceProperty))
                parameters["ThresholdSourceProperty"] = ThresholdSourceProperty;
            if (!string.IsNullOrEmpty(MaxValueSourceNode))
                parameters["MaxValueSourceNode"] = MaxValueSourceNode;
            if (!string.IsNullOrEmpty(MaxValueSourceProperty))
                parameters["MaxValueSourceProperty"] = MaxValueSourceProperty;
        }

        /// <summary>
        /// 从节点参数加载配置（用于恢复状态）
        /// </summary>
        /// <param name="parameters">节点参数字典</param>
        public void LoadFromNodeParameters(IDictionary<string, object> parameters)
        {
            if (parameters == null) return;

            // 恢复阈值参数
            if (parameters.TryGetValue("Threshold", out var threshold))
                Threshold = Convert.ToInt32(threshold);
            if (parameters.TryGetValue("MaxValue", out var maxValue))
                MaxValue = Convert.ToInt32(maxValue);
            if (parameters.TryGetValue("ThresholdType", out var thresholdType))
                ThresholdType = thresholdType?.ToString() ?? "Binary";
            if (parameters.TryGetValue("AdaptiveBlockSize", out var blockSize))
                AdaptiveBlockSize = Convert.ToDouble(blockSize);
            if (parameters.TryGetValue("AdaptiveC", out var adaptiveC))
                AdaptiveC = Convert.ToDouble(adaptiveC);

            // 恢复绑定配置
            if (parameters.TryGetValue("ThresholdBindingType", out var thresholdBindingType))
            {
                var typeStr = thresholdBindingType?.ToString() ?? "Constant";
                ThresholdBindingType = BindingTypeOptions.FirstOrDefault(o => o.Type.ToString() == typeStr) 
                    ?? BindingTypeOptions[0];
            }
            if (parameters.TryGetValue("MaxValueBindingType", out var maxValueBindingType))
            {
                var typeStr = maxValueBindingType?.ToString() ?? "Constant";
                MaxValueBindingType = BindingTypeOptions.FirstOrDefault(o => o.Type.ToString() == typeStr) 
                    ?? BindingTypeOptions[0];
            }

            if (parameters.TryGetValue("ThresholdSourceNode", out var thresholdSourceNode))
                ThresholdSourceNode = thresholdSourceNode?.ToString();
            if (parameters.TryGetValue("ThresholdSourceProperty", out var thresholdSourceProperty))
                ThresholdSourceProperty = thresholdSourceProperty?.ToString();
            if (parameters.TryGetValue("MaxValueSourceNode", out var maxValueSourceNode))
                MaxValueSourceNode = maxValueSourceNode?.ToString();
            if (parameters.TryGetValue("MaxValueSourceProperty", out var maxValueSourceProperty))
                MaxValueSourceProperty = maxValueSourceProperty?.ToString();
        }

        /// <summary>
        /// 获取保存的图像源信息（用于在数据提供者就绪后恢复选择）
        /// </summary>
        /// <param name="parameters">节点参数字典</param>
        /// <returns>图像源信息元组（NodeId, OutputPortName）</returns>
        public static (string? NodeId, string? OutputPortName) GetSavedImageSource(IDictionary<string, object>? parameters)
        {
            if (parameters == null) return (null, null);

            string? nodeId = null;
            string? outputPortName = null;

            if (parameters.TryGetValue("ImageSource_NodeId", out var nodeIdObj))
                nodeId = nodeIdObj?.ToString();
            if (parameters.TryGetValue("ImageSource_OutputPortName", out var portNameObj))
                outputPortName = portNameObj?.ToString();

            return (nodeId, outputPortName);
        }

        #endregion
    }

    /// <summary>
    /// 绑定类型选项
    /// </summary>
    public class BindingTypeOption
    {
        public BindingType Type { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public BindingTypeOption(BindingType type, string displayName, string description)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;
    }
}
