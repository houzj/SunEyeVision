using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    /// 阈值化工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class ThresholdToolDebugWindow : BaseToolDebugWindow
    {
        // XAML控件引用（命名约定：字段名 = _ + XAML控件名）
        private ImageSourceSelector? _imageSourceSelector;
        private BindableParameter? _thresholdParam;
        private BindableParameter? _maxValueParam;
        private RegionEditorControl? _regionEditor;
        private Image? _outputImage;
        private TextBlock? _statusText;

        private ThresholdToolViewModel _viewModel = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;
        private RegionEditorIntegration _regionEditorIntegration = null!;

        // 节点引用（用于配置持久化）
        private object? _currentNode;
        private System.Reflection.PropertyInfo? _parametersProperty;

        public event EventHandler<ThresholdResults>? ToolExecutionCompleted;

        public ThresholdToolDebugWindow()
        {
            InitializeComponent();

            // 初始化ViewModel
            _viewModel = new ThresholdToolViewModel();
            DataContext = _viewModel;

            // 解析XAML中命名的控件引用
            ResolveNamedControls();
            PluginLogger.Info($"[ThresholdToolDebugWindow构造函数] ResolveNamedControls完成，_regionEditor={(_regionEditor != null ? "已找到" : "未找到")}", "ThresholdTool");

            // 初始化绑定和事件
            SetupBindingsAndEvents();

            // 初始化RegionEditor
            InitializeRegionEditor();

            // 订阅区域数据变更事件
            PluginLogger.Info($"[ThresholdToolDebugWindow构造函数] 准备订阅事件，_regionEditor={(_regionEditor != null ? "已找到" : "未找到")}", "ThresholdTool");
            if (_regionEditor != null)
            {
                _regionEditor.RegionDataChanged += OnRegionDataChanged;
                PluginLogger.Info("✓ 已订阅区域数据变更事件", "ThresholdTool");
            }
            else
            {
                PluginLogger.Error("❌ 无法订阅区域数据变更事件：_regionEditor 为 null", "ThresholdTool");
            }
        }

        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = _viewModel.ToolName;
            
            if (_dataProvider != null)
            {
                _viewModel.PopulateImageSources(_dataProvider);
                _regionEditorIntegration?.SetCurrentNodeId(toolId);
            }
        }

        /// <summary>
        /// 设置绑定和事件
        /// </summary>
        private void SetupBindingsAndEvents()
        {
            // 图像源选择事件
            if (_imageSourceSelector != null)
                _imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;

            // 参数绑定
            if (_thresholdParam != null)
            {
                var binding = new Binding("Parameters.Threshold")
                {
                    Source = _viewModel,
                    Mode = BindingMode.TwoWay
                };
                _thresholdParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }

            if (_maxValueParam != null)
            {
                var binding = new Binding("Parameters.MaxValue")
                {
                    Source = _viewModel,
                    Mode = BindingMode.TwoWay
                };
                _maxValueParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }
        }

        private void InitializeRegionEditor()
        {
            _dataProvider = new WorkflowDataSourceProvider();

            if (_regionEditor != null)
            {
                var regionEditorViewModel = new RegionEditorViewModel();
                _regionEditor.DataContext = regionEditorViewModel;
                _regionEditorIntegration = new RegionEditorIntegration(regionEditorViewModel);
                _regionEditorIntegration.SetCurrentNodeId(_viewModel.ToolId);
            }
        }

        private void OnRegionDataChanged(object? sender, RegionData? region)
        {
            if (region == null)
            {
                PluginLogger.Warning("⚠️ 区域数据变更事件：区域为null", "ThresholdTool");
                return;
            }

            PluginLogger.Info("═══════════════════════════════════════", "ThresholdTool");
            PluginLogger.Info("🎯 区域数据已变更", "ThresholdTool");
            PluginLogger.Info($"✓ 区域名称: {region.Name}", "ThresholdTool");
            PluginLogger.Info($"✓ 区域ID: {region.Id}", "ThresholdTool");
            PluginLogger.Info($"✓ 形状类型: {region.GetShapeType()}", "ThresholdTool");

            if (region.Definition is ShapeDefinition shapeDef)
            {
                PluginLogger.Info($"✓ 中心点: ({shapeDef.CenterX:F2}, {shapeDef.CenterY:F2})", "ThresholdTool");
                PluginLogger.Info($"✓ 尺寸: {shapeDef.Width:F2} × {shapeDef.Height:F2}", "ThresholdTool");
                PluginLogger.Info($"✓ 角度: {shapeDef.Angle:F2}°", "ThresholdTool");
            }

            SaveRegionInfoToNode(region);
            PluginLogger.Info("═══════════════════════════════════════", "ThresholdTool");
        }

        private void SaveRegionInfoToNode(RegionData region)
        {
            if (_currentNode == null || _parametersProperty == null)
            {
                PluginLogger.Warning("⚠️ 无法保存区域信息：节点或参数属性为空", "ThresholdTool");
                return;
            }

            var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
            if (parameters == null)
            {
                parameters = new Dictionary<string, object>();
                _parametersProperty.SetValue(_currentNode, parameters);
            }

            var regionInfo = new Dictionary<string, object>
            {
                ["RegionId"] = region.Id,
                ["RegionName"] = region.Name,
                ["ShapeType"] = region.GetShapeType()?.ToString() ?? "Unknown",
                ["Mode"] = region.GetMode().ToString()
            };

            if (region.Definition is ShapeDefinition shapeDef)
            {
                regionInfo["CenterX"] = shapeDef.CenterX;
                regionInfo["CenterY"] = shapeDef.CenterY;
                regionInfo["Width"] = shapeDef.Width;
                regionInfo["Height"] = shapeDef.Height;
                regionInfo["Angle"] = shapeDef.Angle;
            }

            parameters["CurrentRegion"] = regionInfo;

            // 验证保存
            var saved = parameters["CurrentRegion"] as IDictionary<string, object>;
            if (saved != null)
            {
                PluginLogger.Success($"✓ 验证成功：节点参数中的区域信息包含 {saved.Count} 项", "ThresholdTool");
            }
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (_imageSourceSelector != null)
                _viewModel.SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        #region 数据提供者设置

        public void SetDataProvider(WorkflowDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
            _viewModel.PopulateImageSources(dataProvider);
            _regionEditorIntegration?.SetCurrentNodeId(_viewModel.ToolId);
            RestoreImageSourceSelection();
        }

        public void SetCurrentNode(object node)
        {
            _currentNode = node;
            _parametersProperty = node?.GetType().GetProperty("Parameters");
            LoadConfigFromNode();
        }

        #endregion

        #region 配置持久化

        private void LoadConfigFromNode()
        {
            if (_currentNode == null || _parametersProperty == null) return;

            try
            {
                var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
                if (parameters == null) return;
                
                _viewModel.LoadFromNodeParameters(parameters);
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"加载配置失败: {ex.Message}", "ThresholdTool");
            }
        }

        private void SaveConfigToNode()
        {
            if (_currentNode == null || _parametersProperty == null) return;

            try
            {
                var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
                if (parameters == null)
                {
                    parameters = new Dictionary<string, object>();
                    _parametersProperty.SetValue(_currentNode, parameters);
                }

                _viewModel.SaveToNodeParameters(parameters);
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"保存配置失败: {ex.Message}", "ThresholdTool");
            }
        }

        private void RestoreImageSourceSelection()
        {
            if (_currentNode == null || _parametersProperty == null || _dataProvider == null) return;

            try
            {
                var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
                var (savedNodeId, savedOutputPort) = ThresholdToolViewModel.GetSavedImageSource(parameters);

                if (!string.IsNullOrEmpty(savedNodeId))
                {
                    var matchedSource = _viewModel.AvailableImageSources.FirstOrDefault(s =>
                        s.NodeId == savedNodeId &&
                        (string.IsNullOrEmpty(savedOutputPort) || s.OutputPortName == savedOutputPort));

                    if (matchedSource != null)
                    {
                        _viewModel.SelectedImageSource = matchedSource;
                        if (_imageSourceSelector != null)
                            _imageSourceSelector.SelectedImageSource = matchedSource;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"恢复图像源选择失败: {ex.Message}", "ThresholdTool");
            }
        }

        #endregion

        #region 主窗口ImageControl绑定

        /// <summary>
        /// 设置主窗口的ImageControl - 绑定到RegionEditor
        /// </summary>
        /// <remarks>
        /// 当主窗口传递ImageControl时，RegionEditorControl会在该ImageControl的OverlayCanvas上绘制区域。
        /// 这样用户在调试窗口点击绘制按钮时，区域会显示在主窗口的图像上。
        /// </remarks>
        public override void SetMainImageControl(ImageControl? imageControl)
        {
            base.SetMainImageControl(imageControl);

            if (_regionEditor != null && imageControl != null)
            {
                _regionEditor.SetMainImageControl(imageControl);
                System.Diagnostics.Debug.WriteLine("[ThresholdToolDebugWindow] ✓ RegionEditor 已绑定到主窗口 ImageControl");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ThresholdToolDebugWindow] ⚠ 绑定失败 - RegionEditor: {_regionEditor != null}, ImageControl: {imageControl != null}");
            }
        }

        #endregion

        #region 执行控制

        protected override void OnExecuteRequested()
        {
            if (_viewModel.SelectedImageSource == null)
            {
                if (_statusText != null) _statusText.Text = "⚠️ 请选择输入图像源";
                return;
            }

            if (_dataProvider == null)
            {
                if (_statusText != null) _statusText.Text = "⚠️ 数据提供者未初始化";
                return;
            }

            if (!_dataProvider.HasNodeOutput(_viewModel.SelectedImageSource.NodeId))
            {
                if (_statusText != null) _statusText.Text = "⚠️ 图像源节点尚未执行，请先执行前驱节点";
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                _viewModel.SelectedImageSource.NodeId,
                "Output",
                _viewModel.SelectedImageSource.OutputPortName
            ) as OpenCvSharp.Mat;

            if (imageMat == null || imageMat.Empty())
            {
                if (_statusText != null) _statusText.Text = "⚠️ 无法获取图像数据或图像为空";
                return;
            }

            _viewModel.ExecutionCompleted -= OnViewModelExecutionCompleted;
            _viewModel.ExecutionCompleted += OnViewModelExecutionCompleted;

            _viewModel.RunToolWithImage(imageMat);

            if (_regionEditor != null)
            {
                var regions = _regionEditor.ResolveAllRegions();
                // 可将regions传递给工具进行ROI处理
            }

            base.OnExecuteRequested();
        }

        private void OnViewModelExecutionCompleted(object? sender, RunResult result)
        {
            Dispatcher.Invoke(() =>
            {
                if (result.IsSuccess && result.ToolResult is ThresholdResults thresholdResult)
                {
                    if (_statusText != null)
                        _statusText.Text = $"✅ 执行成功 - 阈值: {thresholdResult.ThresholdUsed:F0}";

                    if (_outputImage != null && _viewModel.OutputImage != null)
                    {
                        _outputImage.Source = _viewModel.OutputImage;
                    }

                    ToolExecutionCompleted?.Invoke(this, thresholdResult);
                }
                else
                {
                    if (_statusText != null)
                        _statusText.Text = $"❌ 执行失败 - {result.ErrorMessage}";
                }
            });
        }

        protected override void OnResetRequested()
        {
            _viewModel.ResetParameters();
            if (_statusText != null) _statusText.Text = "参数已重置";
            base.OnResetRequested();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (_regionEditor != null)
            {
                _regionEditor.RegionDataChanged -= OnRegionDataChanged;
                PluginLogger.Info("✓ 已取消区域数据变更事件订阅", "ThresholdTool");
            }

            SaveConfigToNode();
            _regionEditorIntegration?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}
