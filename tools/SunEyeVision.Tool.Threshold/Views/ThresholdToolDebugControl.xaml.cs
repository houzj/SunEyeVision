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
        /// 当前选中的图像源（用于图像显示）
        /// </summary>
        public AvailableDataSource? SelectedImageSource => Parameters?.ImageSource;

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

            // 调用基类方法以填充参数数据源和图像源
            base.SetDataProvider(dataProvider);

            // 保存引用用于执行逻辑
            if (dataProvider is DataSourceQueryService queryService)
            {
                _dataProvider = queryService;
                PluginLogger.Info("使用 DataSourceQueryService", "ThresholdTool");

                // ✅ 新增：为 RegionEditorControl 设置 AvailableDataSources
                if (regionEditor != null)
                {
                    regionEditor.AvailableDataSources = AvailableDataSources;
                    PluginLogger.Info($"已为 RegionEditorControl 设置 AvailableDataSources，数量: {AvailableDataSources?.Count ?? 0}", "ThresholdTool");
                }

                // 初始化 RegionEditor
                if (regionEditor != null && regionEditor.ViewModel != null)
                {
                    regionEditor.ViewModel.Initialize(_dataProvider);
                }
            }
            else
            {
                PluginLogger.Warning($"未知的 dataProvider 类型：{dataProvider?.GetType().Name}", "ThresholdTool");
            }
        }

        public override void SetCurrentNode(object node)
        {
            if (node == null)
                return;

            // 调用基类方法（基类已提取 _currentNodeId 并重新填充数据源，包括图像源）
            base.SetCurrentNode(node);

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




        #endregion

        #region 基类重写

        /// <summary>
        /// 执行工具逻辑 - 基类调用
        /// </summary>
        protected override object ExecuteTool()
        {
            // 检查图像源
            var imageSource = Parameters?.ImageSource;
            if (imageSource == null)
            {
                throw new InvalidOperationException("请选择输入图像源");
            }

            // 检查数据提供者
            if (_dataProvider == null)
            {
                throw new InvalidOperationException("数据提供者未初始化");
            }

            // 获取输入图像
            if (!_dataProvider.HasNodeExecuted(imageSource.SourceNodeId))
            {
                throw new InvalidOperationException("图像源节点尚未执行，请先执行前驱节点");
            }

            var imageMat = _dataProvider.GetPropertyValue(
                imageSource.SourceNodeId,
                imageSource.PropertyName
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
            return Parameters?.ImageSource != null && _dataProvider != null;
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
