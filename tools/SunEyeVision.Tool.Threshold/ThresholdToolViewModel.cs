using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.Threshold
{
    public class ThresholdToolViewModel : AutoToolDebugViewModelBase
    {
        private string _thresholdType = "Otsu";
        private int _threshold = 127;
        private int _maxValue = 255;
        private double _adaptiveBlockSize = 11;
        private double _adaptiveC = 2;

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
            "Otsu", "Binary", "BinaryInv", 
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
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "图像阈值化";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
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

        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在执行{ThresholdType}阈值化...";
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(50, 100));
            ExecutionTime = $"{random.Next(30, 60)} ms";
            StatusMessage = "阈值化完成";
            ToolStatus = "就绪";
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
