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
using OpenCvSharp;

namespace SunEyeVision.Tool.Threshold
{
    /// <summary>
    /// 阈值工具 ViewModel
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 持有 Parameters 实例，UI 直接绑定参数属性
    /// 2. 工作流集成：参数绑定、图像源选择、执行控制
    /// 3. 配置持久化：保存/加载工作流状态
    /// 
    /// UI 绑定示例：
    /// - 参数值: {Binding Parameters.Threshold}
    /// - 绑定类型: {Binding ThresholdBindingType}
    /// - 绑定源: {Binding ThresholdSourceDisplay}
    /// </remarks>
    public class ThresholdToolViewModel : ToolViewModelBase
    {
        #region 核心属性

        /// <summary>
        /// 参数实例（UI 直接绑定此对象的属性）
        /// </summary>
        public ThresholdParameters Parameters { get; } = new ThresholdParameters();

        #endregion

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

        // 绑定配置由 BindableParameter 控件直接管理，不再在 ViewModel 中存储
        // 这样避免了数据冗余和同步问题

        #region 阈值类型选项（用于 UI ComboBox）

        /// <summary>
        /// 阈值类型选项列表
        /// </summary>
        public string[] ThresholdTypes { get; } = { 
            "Binary", "BinaryInv", 
            "Trunc", "ToZero", "ToZeroInv",
            "AdaptiveMean", "AdaptiveGaussian"
        };

        /// <summary>
        /// 当前选中的阈值类型字符串（用于 UI 绑定）
        /// </summary>
        public string ThresholdTypeString
        {
            get
            {
                return Parameters.Type switch
                {
                    ThresholdType.Binary => "Binary",
                    ThresholdType.BinaryInv => "BinaryInv",
                    ThresholdType.Trunc => "Trunc",
                    ThresholdType.ToZero => "ToZero",
                    ThresholdType.ToZeroInv => "ToZeroInv",
                    _ => "Binary"
                };
            }
            set
            {
                Parameters.Type = ParseThresholdType(value);
                Parameters.AdaptiveMethod = ParseAdaptiveMethod(value);
                OnPropertyChanged();
            }
        }

        #endregion

        public ThresholdToolViewModel()
        {
        }

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            base.Initialize(toolId, toolPlugin, toolMetadata);
            ToolName = toolMetadata?.DisplayName ?? "图像阈值化";
        }

        /// <summary>
        /// 获取当前运行参数（直接返回 Parameters 实例的克隆）
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return Parameters.Clone();
        }

        /// <summary>
        /// 解析阈值类型字符串
        /// </summary>
        private static ThresholdType ParseThresholdType(string typeStr)
        {
            return typeStr switch
            {
                "Binary" => ThresholdType.Binary,
                "BinaryInv" => ThresholdType.BinaryInv,
                "Trunc" => ThresholdType.Trunc,
                "ToZero" => ThresholdType.ToZero,
                "ToZeroInv" => ThresholdType.ToZeroInv,
                "AdaptiveMean" => ThresholdType.Binary,
                "AdaptiveGaussian" => ThresholdType.Binary,
                _ => ThresholdType.Binary
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
        /// <param name="externalBindings">外部提供的绑定配置（由 BindableParameter 控件收集）</param>
        public ParameterBindingContainer GetBindingContainer(ParameterBindingContainer? externalBindings = null)
        {
            // 如果提供了外部绑定配置，直接使用
            if (externalBindings != null)
            {
                return externalBindings;
            }

            // 否则创建默认的常量绑定
            var container = new ParameterBindingContainer();
            
            container.SetBinding(new ParameterBinding
            {
                ParameterName = "Threshold",
                BindingType = BindingType.Constant,
                ConstantValue = Parameters.Threshold,
                TargetType = typeof(int)
            });

            container.SetBinding(new ParameterBinding
            {
                ParameterName = "MaxValue",
                BindingType = BindingType.Constant,
                ConstantValue = Parameters.MaxValue,
                TargetType = typeof(int)
            });

            return container;
        }

        /// <summary>
        /// 设置可用数据源（由外部工作流调用）
        /// </summary>
        public void SetAvailableDataSources(string nodeId, IDataSourceQueryService dataSourceQuery)
        {
            if (dataSourceQuery == null) return;

            var sources = dataSourceQuery.GetParentNodes(nodeId);
            // 可根据需要更新 AvailableBindings
        }

        /// <summary>
        /// 从工作流数据源填充可用图像源列表
        /// </summary>
        public void PopulateImageSources(WorkflowDataSourceProvider dataProvider)
        {
            AvailableImageSources.Clear();
            
            if (dataProvider == null) return;

            var nodeOutputs = dataProvider.GetParentNodeOutputs("Mat");
            
            foreach (var nodeOutput in nodeOutputs)
            {
                if (nodeOutput.DataType == "Mat" || 
                    nodeOutput.Children.Any(c => c.DataType == "Mat"))
                {
                    var imageSource = new ImageSourceInfo
                    {
                        NodeId = nodeOutput.NodeId,
                        NodeName = nodeOutput.NodeName,
                        OutputPortName = "Output"
                    };
                    
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
                CurrentImage = inputImage;
                RunTool();
            }
            catch (Exception ex)
            {
                StatusMessage = $"运行出错: {ex.Message}";
                DebugMessage = $"异常: {ex.Message}\n{ex.StackTrace}";
            }
        }

        #region 结果显示属性

        private double _executionTimeMs;
        private bool _isSuccess;
        private string _inputSizeDisplay = "-";

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
        protected override void OnExecutionCompleted(RunResult result)
        {
            base.OnExecutionCompleted(result);

            IsSuccess = result.IsSuccess;
            ExecutionTimeMs = result.ExecutionTimeMs;

            if (result.IsSuccess && result.ToolResult is ThresholdResults thresholdResult)
            {
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
            parameters["Threshold"] = Parameters.Threshold;
            parameters["MaxValue"] = Parameters.MaxValue;
            parameters["ThresholdType"] = ThresholdTypeString;
            parameters["AdaptiveBlockSize"] = Parameters.BlockSize;
            parameters["Invert"] = Parameters.Invert;
            
            // 绑定配置由 DebugWindow 直接保存到 parameters["Bindings"]
        }

        /// <summary>
        /// 从节点参数加载配置（用于恢复状态）
        /// </summary>
        public void LoadFromNodeParameters(IDictionary<string, object> parameters)
        {
            if (parameters == null) return;

            // 恢复阈值参数
            if (parameters.TryGetValue("Threshold", out var threshold))
                Parameters.Threshold = Convert.ToInt32(threshold);
            if (parameters.TryGetValue("MaxValue", out var maxValue))
                Parameters.MaxValue = Convert.ToInt32(maxValue);
            if (parameters.TryGetValue("ThresholdType", out var thresholdType))
                ThresholdTypeString = thresholdType?.ToString() ?? "Binary";
            if (parameters.TryGetValue("AdaptiveBlockSize", out var blockSize))
                Parameters.BlockSize = Convert.ToInt32(blockSize);
            if (parameters.TryGetValue("Invert", out var invert))
                Parameters.Invert = Convert.ToBoolean(invert);
            
            // 绑定配置由 DebugWindow 直接从 parameters["Bindings"] 加载到控件
        }

        /// <summary>
        /// 获取保存的图像源信息（用于在数据提供者就绪后恢复选择）
        /// </summary>
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
}
