using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Views;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Tool.Threshold.Views
{
    /// <summary>
    /// 阈值化工具调试窗口 - 直接绑定参数架构
    /// </summary>
    /// <remarks>
    /// 架构优化（rule-008）：
    /// - 直接持有参数引用，零拷贝实时同步
    /// - 直接绑定到参数对象，不经过 ViewModel 中转
    /// - 调试窗口独立，每个节点一个窗口
    /// </remarks>
    public partial class ThresholdToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        // XAML控件引用（命名约定：字段名 = _ + XAML控件名）
        private ImageSourceSelector? _imageSourceSelector;
        private BindableParameter? _thresholdParam;
        private BindableParameter? _maxValueParam;
        private ComboBox? _thresholdTypeComboBox;
        private RegionEditorControl? _regionEditor;
        private Image? _outputImage;
        private TextBlock? _statusText;

        // 参数引用（零拷贝，与 WorkflowNode.Parameters 同一个实例）
        private ThresholdParameters _parameters = null!;

        // 数据提供者
        private WorkflowDataSourceProvider _dataProvider = null!;
        private RegionEditorIntegration _regionEditorIntegration = null!;

        // 执行状态
        private double _executionTimeMs;

        #endregion

        #region 事件

        public event EventHandler<ThresholdResults>? ToolExecutionCompleted;

        #endregion

        #region 图像源管理

        /// <summary>
        /// 当前选中的图像源
        /// </summary>
        public ImageSourceInfo? SelectedImageSource { get; set; }

        /// <summary>
        /// 可用图像源列表
        /// </summary>
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 构造函数

        public ThresholdToolDebugWindow()
        {
            InitializeComponent();

            // 初始化默认参数
            _parameters = new ThresholdParameters();

            // 解析XAML中命名的控件引用
            ResolveNamedControls();

            // 初始化绑定和事件
            SetupBindingsAndEvents();

            // 初始化RegionEditor
            InitializeRegionEditor();
        }

        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "图像阈值化";

            if (_dataProvider != null)
            {
                PopulateImageSources(_dataProvider);
                _regionEditorIntegration?.SetCurrentNodeId(toolId);
            }
        }

        #endregion

        #region 参数绑定

        /// <summary>
        /// 设置绑定和事件
        /// </summary>
        private void SetupBindingsAndEvents()
        {
            // 图像源选择事件
            if (_imageSourceSelector != null)
                _imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;

            // 直接绑定到参数对象
            if (_thresholdParam != null)
            {
                var binding = new Binding("Threshold")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _thresholdParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }

            if (_maxValueParam != null)
            {
                var binding = new Binding("MaxValue")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _maxValueParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }

            // 阈值类型 ComboBox 事件
            if (_thresholdTypeComboBox != null)
            {
                _thresholdTypeComboBox.SelectionChanged += OnThresholdTypeChanged;
                UpdateThresholdTypeComboBox();
            }

            // 区域编辑器事件
            if (_regionEditor != null)
            {
                _regionEditor.RegionDataChanged += OnRegionDataChanged;
            }
        }

        /// <summary>
        /// 阈值类型变更事件
        /// </summary>
        private void OnThresholdTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_thresholdTypeComboBox == null || _parameters == null) return;

            var selectedItem = _thresholdTypeComboBox.SelectedItem as ComboBoxItem;
            var typeStr = selectedItem?.Content?.ToString() ?? "Binary";

            _parameters.Type = ParseThresholdType(typeStr);
            _parameters.AdaptiveMethod = ParseAdaptiveMethod(typeStr);
        }

        /// <summary>
        /// 更新 ComboBox 显示
        /// </summary>
        private void UpdateThresholdTypeComboBox()
        {
            if (_thresholdTypeComboBox == null || _parameters == null) return;

            var typeStr = _parameters.Type switch
            {
                ThresholdType.Binary => "Binary",
                ThresholdType.BinaryInv => "BinaryInv",
                ThresholdType.Trunc => "Trunc",
                ThresholdType.ToZero => "ToZero",
                ThresholdType.ToZeroInv => "ToZeroInv",
                _ => "Binary"
            };

            foreach (ComboBoxItem item in _thresholdTypeComboBox.Items)
            {
                if (item.Content?.ToString() == typeStr)
                {
                    item.IsSelected = true;
                    break;
                }
            }
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

        #endregion

        #region RegionEditor

        private void InitializeRegionEditor()
        {
            _dataProvider = new WorkflowDataSourceProvider();

            if (_regionEditor != null)
            {
                var regionEditorViewModel = _regionEditor.ViewModel;
                _regionEditorIntegration = new RegionEditorIntegration(regionEditorViewModel);
            }
        }

        private void OnRegionDataChanged(object? sender, RegionData? region)
        {
            if (region == null) return;

            PluginLogger.Info($"区域数据已变更: {region.Name} ({region.GetShapeType()})", "ThresholdTool");
            SaveRegionInfoToNode(region);
        }

        private void SaveRegionInfoToNode(RegionData region)
        {
            PluginLogger.Success($"区域信息已保存: {region.Id}", "ThresholdTool");
        }

        #endregion

        #region 数据提供者设置

        public void SetDataProvider(WorkflowDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
            PopulateImageSources(dataProvider);
        }

        public void SetCurrentNode(object node)
        {
            if (node == null) return;

            var parametersProperty = node.GetType().GetProperty("Parameters");
            if (parametersProperty == null) return;

            var parameters = parametersProperty.GetValue(node) as ToolParameters;
            if (parameters is ThresholdParameters thresholdParams)
            {
                // 直接设置参数引用（零拷贝）
                _parameters = thresholdParams;

                // 重新建立绑定
                SetupBindingsAndEvents();

                PluginLogger.Success($"参数引用已设置: Threshold={thresholdParams.Threshold}", "ThresholdTool");
            }
            else
            {
                PluginLogger.Warning($"参数类型不匹配: 期望 ThresholdParameters，实际 {parameters?.GetType().Name}", "ThresholdTool");
            }
        }

        /// <summary>
        /// 填充可用图像源列表
        /// </summary>
        private void PopulateImageSources(WorkflowDataSourceProvider dataProvider)
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

        #endregion

        #region 主窗口ImageControl绑定

        public override void SetMainImageControl(ImageControl? imageControl)
        {
            base.SetMainImageControl(imageControl);

            if (_regionEditor != null && imageControl != null)
            {
                _regionEditor.SetMainImageControl(imageControl);
            }
        }

        #endregion

        #region 执行控制

        protected override void OnExecuteRequested()
        {
            if (SelectedImageSource == null)
            {
                if (_statusText != null) _statusText.Text = "请选择输入图像源";
                return;
            }

            if (_dataProvider == null)
            {
                if (_statusText != null) _statusText.Text = "数据提供者未初始化";
                return;
            }

            if (!_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                if (_statusText != null) _statusText.Text = "图像源节点尚未执行，请先执行前驱节点";
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                if (_statusText != null) _statusText.Text = "无法获取图像数据或图像为空";
                return;
            }

            ExecuteTool(imageMat);
        }

        /// <summary>
        /// 执行工具
        /// </summary>
        private void ExecuteTool(Mat inputImage)
        {
            // 直接转换为强类型工具
            if (Tool is not ThresholdTool thresholdTool)
            {
                if (_statusText != null) _statusText.Text = "工具实例无效";
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 克隆参数用于执行（线程安全）
                var runParams = (ThresholdParameters)_parameters.Clone();

                // 执行工具（返回强类型 ThresholdResults）
                ThresholdResults result = thresholdTool.Run(inputImage, runParams);

                stopwatch.Stop();
                _executionTimeMs = stopwatch.ElapsedMilliseconds;

                // 更新UI
                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        if (_statusText != null)
                            _statusText.Text = $"执行成功 - 阈值: {result.ThresholdUsed:F0}ms";

                        if (_outputImage != null && result.OutputImage != null)
                        {
                            _outputImage.Source = result.OutputImage.ToBitmapSource();
                        }

                        ToolExecutionCompleted?.Invoke(this, result);
                    }
                    else
                    {
                        if (_statusText != null)
                            _statusText.Text = $"执行失败 - {result.ErrorMessage}";
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _executionTimeMs = stopwatch.ElapsedMilliseconds;

                Dispatcher.Invoke(() =>
                {
                    if (_statusText != null)
                        _statusText.Text = $"执行异常: {ex.Message}";
                });

                PluginLogger.Error($"工具执行异常: {ex.Message}", "ThresholdTool");
            }
        }

        protected override void OnResetRequested()
        {
            _parameters = new ThresholdParameters();
            SetupBindingsAndEvents();

            if (_statusText != null) _statusText.Text = "参数已重置";
            base.OnResetRequested();
        }

        #endregion

        #region 窗口关闭

        protected override void OnClosed(System.EventArgs e)
        {
            if (_parameters != null)
            {
                PluginLogger.Info($"调试窗口关闭 - 最终参数: Threshold={_parameters.Threshold}", "ThresholdTool");
            }

            if (_regionEditor != null)
            {
                _regionEditor.RegionDataChanged -= OnRegionDataChanged;
            }

            _regionEditorIntegration?.Dispose();
            base.OnClosed(e);
        }

        #endregion

        #region 图像源选择事件

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (_imageSourceSelector != null)
                SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        #endregion
    }
}
