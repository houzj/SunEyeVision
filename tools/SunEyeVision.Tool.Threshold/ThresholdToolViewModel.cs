using System.Collections.Generic;
using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
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
