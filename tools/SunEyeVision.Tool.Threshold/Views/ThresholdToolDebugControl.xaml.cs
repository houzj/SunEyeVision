using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Views;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Tool.Threshold.Models;

namespace SunEyeVision.Tool.Threshold.Views
{
    /// <summary>
    /// 阈值化工具调试控件 - 纯声明式绑定架构
    /// </summary>
    /// <remarks>
    /// 架构优化：
    /// - 继承 ToolDebugControlBase 获得标准实现
    /// - 支持路由命令（ToolCommands.Execute, ToolCommands.Confirm）
    /// - 支持路由事件（ToolExecutionCompletedEvent）
    /// - 直接持有参数引用，零拷贝实时同步
    /// - 纯声明式XAML绑定，无手动绑定代码
    /// - 使用项目样式系统统一外观
    /// </remarks>
    public partial class ThresholdToolDebugControl 
    {
        #region 字段

        // 数据提供者（直接使用 DataSourceQueryService）
        private DataSourceQueryService? _dataProvider;
        // 当前节点ID
        private string? _currentNodeId;

        #endregion

        #region 属性

        /// <summary>
        /// 关联的工具实例
        /// </summary>
        public IToolPlugin? Tool { get; set; }

        /// <summary>
        /// 参数实例（依赖属性，支持属性变更通知）
        /// </summary>
        public static readonly DependencyProperty ParametersProperty =
            DependencyProperty.Register(
                nameof(Parameters),
                typeof(ThresholdParameters),
                typeof(ThresholdToolDebugControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 参数实例（公共属性，用于XAML绑定）
        /// </summary>
        public ThresholdParameters Parameters
        {
            get => (ThresholdParameters)GetValue(ParametersProperty);
            set => SetValue(ParametersProperty, value);
        }

        #endregion

        #region 数据源管理

        /// <summary>
        /// 当前选中的图像源（旧的自定义类型，用于图像显示）
        /// </summary>
        public ImageSourceInfo? SelectedImageSource { get; set; }

        /// <summary>
        /// 可用图像源列表（旧的自定义类型，用于图像显示）
        /// </summary>
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        /// <summary>
        /// 图像源选择器加载事件
        /// </summary>
        private void ImageSourceSelector_Loaded(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("ImageSourceSelector_Loaded 触发", "ThresholdTool");
            PluginLogger.Info($"AvailableImageSources.Count = {AvailableImageSources.Count}", "ThresholdTool");

            if (AvailableImageSources.Count > 0)
            {
                PluginLogger.Info($"可用图像源列表:", "ThresholdTool");
                foreach (var source in AvailableImageSources)
                {
                    PluginLogger.Info($"  - {source.DisplayName} (NodeId: {source.NodeId})", "ThresholdTool");
                }
            }
            else
            {
                PluginLogger.Warning("AvailableImageSources 为空，可能的原因: 1) SetDataProvider 未被调用 2) 父节点未注册 3) 父节点无 Mat 输出", "ThresholdTool");
            }
        }

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

            // 基类已自动调用 SetupCommandBindings()

            PluginLogger.Info("InitializeComponent 调用成功", "ThresholdTool");

            // 初始化默认参数（设计时使用）
            Parameters = new ThresholdParameters();
            PluginLogger.Info($"默认参数已初始化: TextConfig.OkColor={Parameters.TextConfig.OkColor}, TextConfig.NgColor={Parameters.TextConfig.NgColor}", "ThresholdTool");

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
                // ★ 通过依赖属性设置参数（自动触发属性变更通知）
                Parameters = thresholdParams;

                PluginLogger.Success($"已加载节点参数: Threshold={Parameters.Threshold}", "ThresholdTool");
                PluginLogger.Info($"TextConfig.OkColor={Parameters.TextConfig.OkColor}, TextConfig.NgColor={Parameters.TextConfig.NgColor}", "ThresholdTool");
            }
            else
            {
                PluginLogger.Warning($"参数类型不匹配: 期望 ThresholdParameters，实际 {parameters?.GetType().Name}", "ThresholdTool");
            }
        }

        #endregion

        #region RegionEditor

        private void InitializeRegionEditor()
        {
            // RegionEditor 的初始化由外部通过 Initialize 方法完成
            // 这里不需要做任何操作
        }

        #endregion

        #region 数据提供者设置

        public override void SetDataProvider(object dataProvider)
        {
            PluginLogger.Info($"SetDataProvider 被调用，dataProvider = {(dataProvider != null ? "非空" : "null")}", "ThresholdTool");

            // 直接接受 DataSourceQueryService
            if (dataProvider is DataSourceQueryService queryService)
            {
                _dataProvider = queryService;
                PluginLogger.Info("使用 DataSourceQueryService", "ThresholdTool");

                PopulateImageSources(_dataProvider);
                PopulateParameterSources(_dataProvider);

                // 初始化 RegionEditor
                if (regionEditor != null && regionEditor.ViewModel != null)
                {
                    regionEditor.ViewModel.Initialize(_dataProvider);
                }
            }
            else
            {
                PluginLogger.Warning($"未知的 dataProvider 类型：{dataProvider?.GetType().Name}", "ThresholdTool");
                return;
            }
        }

        public override void SetCurrentNode(object node)
        {
            if (node == null)
                return;

            // 获取并保存节点ID
            var idProperty = node.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                _currentNodeId = idProperty.GetValue(node) as string;
                PluginLogger.Info($"SetCurrentNode: 节点ID已保存 = {_currentNodeId}", "ThresholdTool");
            }


            var parametersProperty = node.GetType().GetProperty("Parameters");
            if (parametersProperty == null)
                return;

            var parameters = parametersProperty.GetValue(node) as ToolParameters;
            if (parameters is ThresholdParameters thresholdParams)
            {
                // 通过依赖属性设置参数（自动触发属性变更通知）
                Parameters = thresholdParams;

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
        private void PopulateImageSources(DataSourceQueryService dataProvider)
        {
            AvailableImageSources.Clear();

            if (dataProvider == null)
            {
                PluginLogger.Warning("PopulateImageSources: dataProvider 为 null，无法获取图像源", "ThresholdTool");
                return;
            }

            PluginLogger.Info("PopulateImageSources: 开始获取父节点输出", "ThresholdTool");

            var nodeOutputs = dataProvider.GetAvailableDataSources(_currentNodeId ?? "", typeof(OpenCvSharp.Mat));


            PluginLogger.Info($"PopulateImageSources: 获取到 {nodeOutputs.Count} 个节点输出", "ThresholdTool");

            foreach (var nodeOutput in nodeOutputs)
            {
                PluginLogger.Info($"节点: {nodeOutput.SourceNodeName} (ID: {nodeOutput.SourceNodeId}), 类型: {nodeOutput.PropertyType.Name}", "ThresholdTool");

                if (nodeOutput.PropertyType == typeof(OpenCvSharp.Mat))
                {
                    var imageSource = new ImageSourceInfo
                    {
                        NodeId = nodeOutput.SourceNodeId,
                        NodeName = nodeOutput.SourceNodeName,
                        OutputPortName = nodeOutput.PropertyName
                    };

                    AvailableImageSources.Add(imageSource);
                    PluginLogger.Success($"添加图像源: {nodeOutput.SourceNodeName}", "ThresholdTool");
                }
            }

            PluginLogger.Info($"PopulateImageSources: 完成，共有 {AvailableImageSources.Count} 个可用图像源", "ThresholdTool");
        }

        /// <summary>
        /// 填充可用数据源列表（SDK类型，用于参数绑定）
        /// </summary>
        private void PopulateParameterSources(DataSourceQueryService dataProvider)
        {
            if (dataProvider == null)
            {
                PluginLogger.Warning("PopulateParameterSources: dataProvider 为 null，无法获取参数数据源", "ThresholdTool");
                return;
            }

            PluginLogger.Info("PopulateParameterSources: 开始获取参数数据源", "ThresholdTool");

            var nodeOutputs = dataProvider.GetAvailableDataSources(_currentNodeId ?? "", null);

            PluginLogger.Info($"PopulateParameterSources: 获取到 {nodeOutputs.Count} 个节点输出", "ThresholdTool");

            // 创建新的集合实例以触发 DependencyProperty 回调
            var newDataSources = new ObservableCollection<AvailableDataSource>();

            foreach (var nodeOutput in nodeOutputs)
            {
                PluginLogger.Info($"节点: {nodeOutput.SourceNodeName} (ID: {nodeOutput.SourceNodeId}), 属性: {nodeOutput.PropertyName}, 类型: {nodeOutput.PropertyType.Name}, TreeName: {nodeOutput.FullTreeName}", "ThresholdTool");

                var dataSource = new AvailableDataSource
                {
                    SourceNodeId = nodeOutput.SourceNodeId,
                    SourceNodeName = nodeOutput.SourceNodeName,
                    PropertyName = nodeOutput.PropertyName,
                    PropertyType = nodeOutput.PropertyType,
                    DisplayName = $"{nodeOutput.SourceNodeName}.{nodeOutput.PropertyName}",
                    FullTreeName = nodeOutput.FullTreeName
                };

                newDataSources.Add(dataSource);
                PluginLogger.Success($"添加数据源: {dataSource.DisplayName}", "ThresholdTool");
            }

            // 替换集合引用，触发 DependencyProperty 的回调
            AvailableDataSources = newDataSources;

            PluginLogger.Success($"PopulateParameterSources: 完成，共有 {AvailableDataSources.Count} 个可用数据源", "ThresholdTool");
        }

        /// <summary>
        /// 将 AvailableDataSource 列表转换为 ImageSourceInfo 列表（用于 ImageSourceSelector）
        /// </summary>
        private void PopulateImageSourcesFromParameterSources()
        {
            AvailableImageSources.Clear();

            foreach (var dataSource in AvailableDataSources)
            {
                var imageSource = new ImageSourceInfo
                {
                    NodeId = dataSource.SourceNodeId,
                    NodeName = dataSource.SourceNodeName,
                    OutputPortName = dataSource.PropertyName,
                    DataType = dataSource.PropertyType?.Name ?? "Mat"
                };

                AvailableImageSources.Add(imageSource);
                PluginLogger.Info($"添加图像源（从参数源转换）: {imageSource.DisplayName}", "ThresholdTool");
            }

            PluginLogger.Success($"PopulateImageSourcesFromParameterSources: 完成，共有 {AvailableImageSources.Count} 个可用图像源", "ThresholdTool");
        }

        #endregion

        #region 基类重写

        /// <summary>
        /// 执行工具逻辑 - 基类调用
        /// </summary>
        protected override object ExecuteTool()
        {
            // 检查图像源
            if (SelectedImageSource == null)
            {
                throw new InvalidOperationException("请选择输入图像源");
            }

            // 检查数据提供者
            if (_dataProvider == null)
            {
                throw new InvalidOperationException("数据提供者未初始化");
            }

            // 获取输入图像
            if (!_dataProvider.HasNodeExecuted(SelectedImageSource.NodeId))
            {
                throw new InvalidOperationException("图像源节点尚未执行，请先执行前驱节点");
            }

            var imageMat = _dataProvider.GetPropertyValue(
                SelectedImageSource.NodeId,
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                throw new InvalidOperationException("无法获取图像数据或图像为空");
            }

            // 检查工具实例
            if (Tool is not ThresholdTool thresholdTool)
            {
                throw new InvalidOperationException("工具实例无效");
            }

            // 克隆参数用于执行（线程安全）
            var runParams = (ThresholdParameters)Parameters.Clone();

            // 执行工具
            return thresholdTool.Run(imageMat, runParams);
        }

        /// <summary>
        /// 判断是否可执行 - 基类调用
        /// </summary>
        protected override bool CanExecuteTool()
        {
            return SelectedImageSource != null && _dataProvider != null;
        }

        #endregion

        #region 执行控制

        protected virtual void OnExecuteRequested()
        {
            // 基类已处理执行逻辑，这里保留是为了向后兼容（如果有其他地方调用）
            // 实际执行由基类的 OnExecuteCommand 调用 ExecuteTool() 完成
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
                return !Parameters.ResultConfig.IsEmptyCheckEnabled;
            }

            var allChecksPassed = true;

            // 白色像素比例判断
            if (Parameters.ResultConfig.IsWhitePixelRatioCheckEnabled)
            {
                var whitePixelCount = Cv2.CountNonZero(outputImage);
                var totalPixels = outputImage.Rows * outputImage.Cols;
                var ratio = (double)whitePixelCount / totalPixels * 100;

                if (ratio < Parameters.ResultConfig.WhitePixelRatioMin || ratio > Parameters.ResultConfig.WhitePixelRatioMax)
                {
                    allChecksPassed = false;
                    PluginLogger.Warning($"白色像素比例 {ratio:F1}% 不在范围内 [{Parameters.ResultConfig.WhitePixelRatioMin}-{Parameters.ResultConfig.WhitePixelRatioMax}]", "ThresholdTool");
                }
            }

            // 输出均值判断
            if (Parameters.ResultConfig.IsMeanCheckEnabled)
            {
                var mean = Cv2.Mean(outputImage);
                var meanValue = mean.Val0;

                if (meanValue < Parameters.ResultConfig.MeanMin || meanValue > Parameters.ResultConfig.MeanMax)
                {
                    allChecksPassed = false;
                    PluginLogger.Warning($"输出均值 {meanValue:F1} 不在范围内 [{Parameters.ResultConfig.MeanMin}-{Parameters.ResultConfig.MeanMax}]", "ThresholdTool");
                }
            }

            // 输出面积判断
            if (Parameters.ResultConfig.IsAreaCheckEnabled)
            {
                var area = Cv2.CountNonZero(outputImage);

                if (area < Parameters.ResultConfig.AreaMin || area > Parameters.ResultConfig.AreaMax)
                {
                    allChecksPassed = false;
                    PluginLogger.Warning($"输出面积 {area} 不在范围内 [{Parameters.ResultConfig.AreaMin}-{Parameters.ResultConfig.AreaMax}]", "ThresholdTool");
                }
            }

            // 质心判断
            if (Parameters.ResultConfig.IsCentroidXCheckEnabled || Parameters.ResultConfig.IsCentroidYCheckEnabled)
            {
                var moments = Cv2.Moments(outputImage);
                var centroidX = moments.M10 / moments.M00;
                var centroidY = moments.M01 / moments.M00;

                if (Parameters.ResultConfig.IsCentroidXCheckEnabled)
                {
                    if (centroidX < Parameters.ResultConfig.CentroidXMin || centroidX > Parameters.ResultConfig.CentroidXMax)
                    {
                        allChecksPassed = false;
                        PluginLogger.Warning($"质心X {centroidX:F1} 不在范围内 [{Parameters.ResultConfig.CentroidXMin}-{Parameters.ResultConfig.CentroidXMax}]", "ThresholdTool");
                    }
                }

                if (Parameters.ResultConfig.IsCentroidYCheckEnabled)
                {
                    if (centroidY < Parameters.ResultConfig.CentroidYMin || centroidY > Parameters.ResultConfig.CentroidYMax)
                    {
                        allChecksPassed = false;
                        PluginLogger.Warning($"质心Y {centroidY:F1} 不在范围内 [{Parameters.ResultConfig.CentroidYMin}-{Parameters.ResultConfig.CentroidYMax}]", "ThresholdTool");
                    }
                }
            }

            return allChecksPassed;
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
        }

        #endregion

        #region 图像显示事件

        private void OnToggleOutputImageVisibility(object sender, RoutedEventArgs e)
        {
            Parameters.DisplayConfig.OutputImage.IsVisible = !Parameters.DisplayConfig.OutputImage.IsVisible;
            PluginLogger.Info($"输出图像显示: {Parameters.DisplayConfig.OutputImage.IsVisible}", "ThresholdTool");
        }

        private void OnOpenOutputImageStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开输出图像样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        private void OnToggleThresholdLineVisibility(object sender, RoutedEventArgs e)
        {
            Parameters.DisplayConfig.ThresholdLine.IsVisible = !Parameters.DisplayConfig.ThresholdLine.IsVisible;
            PluginLogger.Info($"阈值分界线显示: {Parameters.DisplayConfig.ThresholdLine.IsVisible}", "ThresholdTool");
        }

        private void OnOpenThresholdLineStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开阈值分界线样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        private void OnToggleRegionVisibility(object sender, RoutedEventArgs e)
        {
            Parameters.DisplayConfig.Region.IsVisible = !Parameters.DisplayConfig.Region.IsVisible;
            PluginLogger.Info($"ROI区域显示: {Parameters.DisplayConfig.Region.IsVisible}", "ThresholdTool");
        }

        private void OnOpenRegionStyle(object sender, RoutedEventArgs e)
        {
            PluginLogger.Info("打开ROI区域样式设置", "ThresholdTool");
            // TODO: 打开样式设置弹窗
        }

        private void OnToggleHistogramVisibility(object sender, RoutedEventArgs e)
        {
            Parameters.DisplayConfig.Histogram.IsVisible = !Parameters.DisplayConfig.Histogram.IsVisible;
            PluginLogger.Info($"直方图显示: {Parameters.DisplayConfig.Histogram.IsVisible}", "ThresholdTool");
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
        }

        #endregion
    }
}
