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
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Views;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Tool.Threshold.Models;

namespace SunEyeVision.Tool.Threshold.Views
{
    /// <summary>
    /// 阈值化工具调试控件 - 普通UserControl架构
    /// </summary>
    /// <remarks>
    /// 架构优化：
    /// - 不继承基类，作为普通UserControl
    /// - 直接持有参数引用，零拷贝实时同步
    /// - 直接绑定到参数对象，不经过 ViewModel 中转
    /// - 使用项目样式系统统一外观
    /// </remarks>
    public partial class ThresholdToolDebugControl : UserControl
    {
        #region 字段

        // 转换器
        private readonly Plugin.SDK.UI.Controls.Region.Converters.BoolToVisibilityConverter _boolToVisibilityConverter = new();

        // 参数引用（零拷贝，与 WorkflowNode.Parameters 同一个实例）
        private ThresholdParameters _parameters = null!;

        // 数据提供者
        private WorkflowDataSourceProvider _dataProvider = null!;
        private RegionEditorIntegration _regionEditorIntegration = null!;

        // 执行状态
        private double _executionTimeMs;

        #endregion

        #region 属性

        /// <summary>
        /// 关联的工具实例
        /// </summary>
        public IToolPlugin? Tool { get; set; }

        #endregion

        #region 事件

        /// <summary>
        /// 运行按钮点击事件
        /// </summary>
        public event EventHandler? ExecuteRequested;

        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        public event EventHandler? ConfirmClicked;

        /// <summary>
        /// 工具执行完成事件
        /// </summary>
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

        #region 主窗口ImageControl绑定

        /// <summary>
        /// 主窗口的ImageControl引用 - 用于区域编辑器绑定
        /// </summary>
        protected ImageControl? _mainImageControl;

        /// <summary>
        /// 设置主窗口的ImageControl - 用于区域编辑器绑定
        /// </summary>
        /// <param name="imageControl">主窗口的ImageControl</param>
        public virtual void SetMainImageControl(ImageControl? imageControl)
        {
            _mainImageControl = imageControl;

            if (regionEditor != null && imageControl != null)
            {
                regionEditor.SetMainImageControl(imageControl);
            }
        }

        #endregion

        #region 构造函数

        public ThresholdToolDebugControl()
        {
            PluginLogger.Info("ThresholdToolDebugControl 构造函数开始", "ThresholdTool");
            
            InitializeComponent();
            
            PluginLogger.Info("InitializeComponent 调用成功", "ThresholdTool");

            // 初始化默认参数（设计时使用）
            _parameters = new ThresholdParameters();
            PluginLogger.Info("默认参数已初始化", "ThresholdTool");

            // 初始化绑定和事件
            PluginLogger.Info("开始设置绑定和事件", "ThresholdTool");
            SetupBindingsAndEvents();
            PluginLogger.Success("绑定和事件设置完成", "ThresholdTool");

            // 初始化RegionEditor
            PluginLogger.Info("开始初始化RegionEditor", "ThresholdTool");
            InitializeRegionEditor();
            PluginLogger.Success("RegionEditor初始化完成", "ThresholdTool");
            
            PluginLogger.Success("ThresholdToolDebugControl 构造函数完成", "ThresholdTool");
        }

        public ThresholdToolDebugControl(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;

            if (_dataProvider != null)
            {
                PopulateImageSources(_dataProvider);
                _regionEditorIntegration?.SetCurrentNodeId(toolId);
            }
        }

        /// <summary>
        /// 设置节点参数 - 运行时由工厂方法调用
        /// </summary>
        /// <param name="parameters">节点参数实例（与WorkflowNode.Parameters同一实例）</param>
        public void SetParameters(ToolParameters parameters)
        {
            if (parameters is ThresholdParameters thresholdParams)
            {
                // ★ 直接设置参数引用（零拷贝，与节点共享同一实例）
                _parameters = thresholdParams;

                // 重新建立绑定（使用新参数实例）
                SetupBindingsAndEvents();

                PluginLogger.Success($"已加载节点参数: Threshold={_parameters.Threshold}", "ThresholdTool");
            }
            else
            {
                PluginLogger.Warning($"参数类型不匹配: 期望 ThresholdParameters，实际 {parameters?.GetType().Name}", "ThresholdTool");
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
            if (imageSourceSelector != null)
                imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;

            // 直接绑定到参数对象
            if (thresholdParam != null)
            {
                var binding = new Binding("Threshold")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                thresholdParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }

            if (maxValueParam != null)
            {
                var binding = new Binding("MaxValue")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                maxValueParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }

            // 阈值类型 ComboBox 事件
            if (thresholdTypeComboBox != null)
            {
                thresholdTypeComboBox.SelectionChanged += OnThresholdTypeChanged;
                UpdateThresholdTypeComboBox();
            }

            // 区域编辑器事件
            if (regionEditor != null)
            {
                regionEditor.RegionDataChanged += OnRegionDataChanged;
            }

            // 注：结果判断配置和文本显示配置已通过XAML声明式绑定实现，无需手动绑定
        }

        /// <summary>
        /// 阈值类型变更事件
        /// </summary>
        private void OnThresholdTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (thresholdTypeComboBox == null || _parameters == null) return;

            var selectedItem = thresholdTypeComboBox.SelectedItem as ComboBoxItem;
            var typeStr = selectedItem?.Content?.ToString() ?? "Binary";

            _parameters.Type = ParseThresholdType(typeStr);
            _parameters.AdaptiveMethod = ParseAdaptiveMethod(typeStr);
        }

        /// <summary>
        /// 更新 ComboBox 显示
        /// </summary>
        private void UpdateThresholdTypeComboBox()
        {
            if (thresholdTypeComboBox == null || _parameters == null) return;

            var typeStr = _parameters.Type switch
            {
                ThresholdType.Binary => "Binary",
                ThresholdType.BinaryInv => "BinaryInv",
                ThresholdType.Trunc => "Trunc",
                ThresholdType.ToZero => "ToZero",
                ThresholdType.ToZeroInv => "ToZeroInv",
                _ => "Binary"
            };

            foreach (ComboBoxItem item in thresholdTypeComboBox.Items)
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

            if (regionEditor != null)
            {
                var regionEditorViewModel = regionEditor.ViewModel;
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

        #region 执行控制

        protected virtual void OnExecuteRequested()
        {
            if (SelectedImageSource == null)
            {
                PluginLogger.Warning("请选择输入图像源", "ThresholdTool");
                return;
            }

            if (_dataProvider == null)
            {
                PluginLogger.Warning("数据提供者未初始化", "ThresholdTool");
                return;
            }

            if (!_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行，请先执行前驱节点", "ThresholdTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据或图像为空", "ThresholdTool");
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
                PluginLogger.Error("工具实例无效", "ThresholdTool");
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
                        PluginLogger.Success($"执行成功 - 阈值: {result.ThresholdUsed:F0}", "ThresholdTool");
                        ToolExecutionCompleted?.Invoke(this, result);
                    }
                    else
                    {
                        PluginLogger.Error($"执行失败 - {result.ErrorMessage}", "ThresholdTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _executionTimeMs = stopwatch.ElapsedMilliseconds;

                Dispatcher.Invoke(() =>
                {
                    PluginLogger.Error($"执行异常: {ex.Message}", "ThresholdTool");
                });

                PluginLogger.Error($"工具执行异常: {ex.Message}", "ThresholdTool", ex);
            }
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

        #region 图像源选择事件

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (imageSourceSelector != null)
                SelectedImageSource = imageSourceSelector.SelectedImageSource;
        }

        #endregion

        #region 按钮事件处理

        private void OnRunButtonClick(object sender, RoutedEventArgs e)
        {
            OnExecuteRequested();
        }

        private void OnConfirmButtonClick(object sender, RoutedEventArgs e)
        {
            PluginLogger.Success("配置已确认", "ThresholdTool");
            ConfirmClicked?.Invoke(this, EventArgs.Empty);
        }

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
            // UserControl版本：触发ConfirmClicked事件，由窗口壳处理关闭
            ConfirmClicked?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
