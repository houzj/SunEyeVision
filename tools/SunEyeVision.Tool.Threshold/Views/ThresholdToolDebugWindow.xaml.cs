using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
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
using SunEyeVision.Tool.Threshold.Models;

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
        
        // 结果判断控件
        private ToggleSwitch? _cbEmptyCheck;
        private ToggleSwitch? _cbWhitePixelRatioCheck;
        private ToggleSwitch? _cbMeanCheck;
        private ToggleSwitch? _cbAreaCheck;
        private ToggleSwitch? _cbCentroidXCheck;
        private ToggleSwitch? _cbCentroidYCheck;
        private RangeInputControl? _rangeWhitePixelRatio;
        private RangeInputControl? _rangeMean;
        private RangeInputControl? _rangeArea;
        private RangeInputControl? _rangeCentroidX;
        private RangeInputControl? _rangeCentroidY;
        
        // 转换器
        private readonly Plugin.SDK.UI.Controls.Region.Converters.BoolToVisibilityConverter _boolToVisibilityConverter = new();
        
        // 文本显示控件
        private TextBox? _txtContent;
        private ColorSelector? _okColorSelector;
        private ColorSelector? _ngColorSelector;
        private NumericUpDown? _numFontSize;
        private NumericUpDown? _numTextOpacity;
        private NumericUpDown? _numPositionX;
        private NumericUpDown? _numPositionY;

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

            // ========== 结果判断配置绑定 ==========
            SetupResultConfigBindings();

            // ========== 文本显示配置绑定 ==========
            SetupTextConfigBindings();
        }

        /// <summary>
        /// 设置结果判断配置绑定
        /// </summary>
        private void SetupResultConfigBindings()
        {
            PluginLogger.Info("SetupResultConfigBindings 开始", "ThresholdToolDebugWindow");
            
            // ToggleSwitch 绑定到 Enabled 属性
            if (_cbEmptyCheck != null)
            {
                var binding = new Binding("IsEmptyCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _cbEmptyCheck.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
                PluginLogger.Info($"_cbEmptyCheck 绑定已建立 - IsEmptyCheckEnabled: {_parameters.ResultConfig.IsEmptyCheckEnabled}", "ThresholdToolDebugWindow");
            }
            else
            {
                PluginLogger.Warning("_cbEmptyCheck 为 null", "ThresholdToolDebugWindow");
            }

            if (_cbWhitePixelRatioCheck != null)
            {
                var binding = new Binding("IsWhitePixelRatioCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _cbWhitePixelRatioCheck.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
                PluginLogger.Info($"_cbWhitePixelRatioCheck 绑定已建立 - IsWhitePixelRatioCheckEnabled: {_parameters.ResultConfig.IsWhitePixelRatioCheckEnabled}", "ThresholdToolDebugWindow");
                
                // 添加 CheckedChanged 事件监听
                _cbWhitePixelRatioCheck.CheckedChanged += (s, e) =>
                {
                    PluginLogger.Info($"_cbWhitePixelRatioCheck.CheckedChanged - 新值: {e.NewValue}", "ThresholdToolDebugWindow");
                };
            }
            else
            {
                PluginLogger.Warning("_cbWhitePixelRatioCheck 为 null", "ThresholdToolDebugWindow");
            }

            // RangeInputControl 绑定到白色像素比例范围
            if (_rangeWhitePixelRatio != null)
            {
                // ✅ 添加 Visibility 绑定
                var visibilityBinding = new Binding("IsWhitePixelRatioCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.OneWay,
                    Converter = _boolToVisibilityConverter
                };
                _rangeWhitePixelRatio.SetBinding(RangeInputControl.VisibilityProperty, visibilityBinding);
                PluginLogger.Info("_rangeWhitePixelRatio.Visibility 绑定已建立", "ThresholdToolDebugWindow");
                
                // MinValue 和 MaxValue 绑定
                var minBinding = new Binding("WhitePixelRatioMin")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeWhitePixelRatio.SetBinding(RangeInputControl.MinValueProperty, minBinding);

                var maxBinding = new Binding("WhitePixelRatioMax")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeWhitePixelRatio.SetBinding(RangeInputControl.MaxValueProperty, maxBinding);
            }

            // 输出均值判断
            if (_cbMeanCheck != null)
            {
                var binding = new Binding("IsMeanCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _cbMeanCheck.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }

            // RangeInputControl 绑定到输出均值范围
            if (_rangeMean != null)
            {
                // ✅ 添加 Visibility 绑定
                var visibilityBinding = new Binding("IsMeanCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.OneWay,
                    Converter = _boolToVisibilityConverter
                };
                _rangeMean.SetBinding(RangeInputControl.VisibilityProperty, visibilityBinding);
                PluginLogger.Info("_rangeMean.Visibility 绑定已建立", "ThresholdToolDebugWindow");
                
                // MinValue 和 MaxValue 绑定
                var minBinding = new Binding("MeanMin")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeMean.SetBinding(RangeInputControl.MinValueProperty, minBinding);

                var maxBinding = new Binding("MeanMax")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeMean.SetBinding(RangeInputControl.MaxValueProperty, maxBinding);
            }

            // 输出面积判断
            if (_cbAreaCheck != null)
            {
                var binding = new Binding("IsAreaCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _cbAreaCheck.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }

            // RangeInputControl 绑定到输出面积范围
            if (_rangeArea != null)
            {
                // ✅ 添加 Visibility 绑定
                var visibilityBinding = new Binding("IsAreaCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.OneWay,
                    Converter = _boolToVisibilityConverter
                };
                _rangeArea.SetBinding(RangeInputControl.VisibilityProperty, visibilityBinding);
                PluginLogger.Info("_rangeArea.Visibility 绑定已建立", "ThresholdToolDebugWindow");
                
                // MinValue 和 MaxValue 绑定
                var minBinding = new Binding("AreaMin")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeArea.SetBinding(RangeInputControl.MinValueProperty, minBinding);

                var maxBinding = new Binding("AreaMax")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeArea.SetBinding(RangeInputControl.MaxValueProperty, maxBinding);
            }

            // 质心X判断
            if (_cbCentroidXCheck != null)
            {
                var binding = new Binding("IsCentroidXCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _cbCentroidXCheck.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }

            // RangeInputControl 绑定到质心X范围
            if (_rangeCentroidX != null)
            {
                // ✅ 添加 Visibility 绑定
                var visibilityBinding = new Binding("IsCentroidXCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.OneWay,
                    Converter = _boolToVisibilityConverter
                };
                _rangeCentroidX.SetBinding(RangeInputControl.VisibilityProperty, visibilityBinding);
                PluginLogger.Info("_rangeCentroidX.Visibility 绑定已建立", "ThresholdToolDebugWindow");
                
                // MinValue 和 MaxValue 绑定
                var minBinding = new Binding("CentroidXMin")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeCentroidX.SetBinding(RangeInputControl.MinValueProperty, minBinding);

                var maxBinding = new Binding("CentroidXMax")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeCentroidX.SetBinding(RangeInputControl.MaxValueProperty, maxBinding);
            }

            // 质心Y判断
            if (_cbCentroidYCheck != null)
            {
                var binding = new Binding("IsCentroidYCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _cbCentroidYCheck.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }

            // RangeInputControl 绑定到质心Y范围
            if (_rangeCentroidY != null)
            {
                // ✅ 添加 Visibility 绑定
                var visibilityBinding = new Binding("IsCentroidYCheckEnabled")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.OneWay,
                    Converter = _boolToVisibilityConverter
                };
                _rangeCentroidY.SetBinding(RangeInputControl.VisibilityProperty, visibilityBinding);
                PluginLogger.Info("_rangeCentroidY.Visibility 绑定已建立", "ThresholdToolDebugWindow");
                
                // MinValue 和 MaxValue 绑定
                var minBinding = new Binding("CentroidYMin")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeCentroidY.SetBinding(RangeInputControl.MinValueProperty, minBinding);

                var maxBinding = new Binding("CentroidYMax")
                {
                    Source = _parameters.ResultConfig,
                    Mode = BindingMode.TwoWay
                };
                _rangeCentroidY.SetBinding(RangeInputControl.MaxValueProperty, maxBinding);
            }
        }

        /// <summary>
        /// 设置文本显示配置绑定
        /// </summary>
        private void SetupTextConfigBindings()
        {
            // 文本内容绑定
            if (_txtContent != null)
            {
                var binding = new Binding("Content")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _txtContent.SetBinding(TextBox.TextProperty, binding);
            }

            // OK颜色绑定
            if (_okColorSelector != null)
            {
                var binding = new Binding("OkColor")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _okColorSelector.SetBinding(ColorSelector.SelectedColorProperty, binding);
            }

            // NG颜色绑定
            if (_ngColorSelector != null)
            {
                var binding = new Binding("NgColor")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _ngColorSelector.SetBinding(ColorSelector.SelectedColorProperty, binding);
            }

            // 字号绑定
            if (_numFontSize != null)
            {
                var binding = new Binding("FontSize")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _numFontSize.SetBinding(NumericUpDown.ValueProperty, binding);
            }

            // 透明度绑定
            if (_numTextOpacity != null)
            {
                var binding = new Binding("Opacity")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _numTextOpacity.SetBinding(NumericUpDown.ValueProperty, binding);
            }

            // 位置X绑定
            if (_numPositionX != null)
            {
                var binding = new Binding("PositionX")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _numPositionX.SetBinding(NumericUpDown.ValueProperty, binding);
            }

            // 位置Y绑定
            if (_numPositionY != null)
            {
                var binding = new Binding("PositionY")
                {
                    Source = _parameters.TextConfig,
                    Mode = BindingMode.TwoWay
                };
                _numPositionY.SetBinding(NumericUpDown.ValueProperty, binding);
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

        #region 结果判断事件

        // 注：ToggleSwitch 已通过双向绑定自动同步到 _resultConfig，无需手动处理

        #endregion

        #region 图像显示事件

        private void OnToggleOutputImageVisibility(object sender, RoutedEventArgs e)
        {
            _parameters.DisplayConfig.OutputImage.IsVisible = !_parameters.DisplayConfig.OutputImage.IsVisible;
            PluginLogger.Info($"输出图像显示: {_parameters.DisplayConfig.OutputImage.IsVisible}", "ThresholdTool");
        }

        private void OnOpenOutputImageStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开输出图像样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        private void OnToggleThresholdLineVisibility(object sender, RoutedEventArgs e)
        {
            _parameters.DisplayConfig.ThresholdLine.IsVisible = !_parameters.DisplayConfig.ThresholdLine.IsVisible;
            PluginLogger.Info($"阈值分界线显示: {_parameters.DisplayConfig.ThresholdLine.IsVisible}", "ThresholdTool");
        }

        private void OnOpenThresholdLineStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开阈值分界线样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        private void OnToggleRegionVisibility(object sender, RoutedEventArgs e)
        {
            _parameters.DisplayConfig.Region.IsVisible = !_parameters.DisplayConfig.Region.IsVisible;
            PluginLogger.Info($"ROI区域显示: {_parameters.DisplayConfig.Region.IsVisible}", "ThresholdTool");
        }

        private void OnOpenRegionStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开ROI区域样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        private void OnToggleHistogramVisibility(object sender, RoutedEventArgs e)
        {
            _parameters.DisplayConfig.Histogram.IsVisible = !_parameters.DisplayConfig.Histogram.IsVisible;
            PluginLogger.Info($"直方图显示: {_parameters.DisplayConfig.Histogram.IsVisible}", "ThresholdTool");
        }

        private void OnOpenHistogramStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开直方图样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        #endregion

        #region 底部按钮事件

        private void OnContinuousExecute(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("开始连续执行", "ThresholdTool");
            // TODO: 实现连续执行逻辑
        }

        private void OnExecuteClick(object sender, RoutedEventArgs e)
        {
            OnExecuteRequested();
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            PluginLogger.Success("配置已确认", "ThresholdTool");
            DialogResult = true;
            Close();
        }

        #endregion

        #region 结果判断逻辑

        /// <summary>
        /// 执行结果判断
        /// </summary>
        private bool EvaluateResult(Mat outputImage)
        {
            if (outputImage == null || outputImage.Empty())
            {
                return !_parameters.ResultConfig.IsEmptyCheckEnabled;
            }

            var allChecksPassed = true;

            // 白色像素比例判断
            if (_parameters.ResultConfig.IsWhitePixelRatioCheckEnabled)
            {
                var whitePixelCount = Cv2.CountNonZero(outputImage);
                var totalPixels = outputImage.Rows * outputImage.Cols;
                var ratio = (double)whitePixelCount / totalPixels * 100;

                if (ratio < _parameters.ResultConfig.WhitePixelRatioMin || ratio > _parameters.ResultConfig.WhitePixelRatioMax)
                {
                    allChecksPassed = false;
                    PluginLogger.Warning($"白色像素比例 {ratio:F1}% 不在范围内 [{_parameters.ResultConfig.WhitePixelRatioMin}-{_parameters.ResultConfig.WhitePixelRatioMax}]", "ThresholdTool");
                }
            }

            // 输出均值判断
            if (_parameters.ResultConfig.IsMeanCheckEnabled)
            {
                var mean = Cv2.Mean(outputImage);
                var meanValue = mean.Val0;

                if (meanValue < _parameters.ResultConfig.MeanMin || meanValue > _parameters.ResultConfig.MeanMax)
                {
                    allChecksPassed = false;
                    PluginLogger.Warning($"输出均值 {meanValue:F1} 不在范围内 [{_parameters.ResultConfig.MeanMin}-{_parameters.ResultConfig.MeanMax}]", "ThresholdTool");
                }
            }

            // 输出面积判断
            if (_parameters.ResultConfig.IsAreaCheckEnabled)
            {
                var area = Cv2.CountNonZero(outputImage);

                if (area < _parameters.ResultConfig.AreaMin || area > _parameters.ResultConfig.AreaMax)
                {
                    allChecksPassed = false;
                    PluginLogger.Warning($"输出面积 {area} 不在范围内 [{_parameters.ResultConfig.AreaMin}-{_parameters.ResultConfig.AreaMax}]", "ThresholdTool");
                }
            }

            // 质心判断
            if (_parameters.ResultConfig.IsCentroidXCheckEnabled || _parameters.ResultConfig.IsCentroidYCheckEnabled)
            {
                var moments = Cv2.Moments(outputImage);
                var centroidX = moments.M10 / moments.M00;
                var centroidY = moments.M01 / moments.M00;

                if (_parameters.ResultConfig.IsCentroidXCheckEnabled)
                {
                    if (centroidX < _parameters.ResultConfig.CentroidXMin || centroidX > _parameters.ResultConfig.CentroidXMax)
                    {
                        allChecksPassed = false;
                        PluginLogger.Warning($"质心X {centroidX:F1} 不在范围内 [{_parameters.ResultConfig.CentroidXMin}-{_parameters.ResultConfig.CentroidXMax}]", "ThresholdTool");
                    }
                }

                if (_parameters.ResultConfig.IsCentroidYCheckEnabled)
                {
                    if (centroidY < _parameters.ResultConfig.CentroidYMin || centroidY > _parameters.ResultConfig.CentroidYMax)
                    {
                        allChecksPassed = false;
                        PluginLogger.Warning($"质心Y {centroidY:F1} 不在范围内 [{_parameters.ResultConfig.CentroidYMin}-{_parameters.ResultConfig.CentroidYMax}]", "ThresholdTool");
                    }
                }
            }

            return allChecksPassed;
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
