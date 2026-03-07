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
            
            // 初始化绑定和事件
            SetupBindingsAndEvents();
            
            // 初始化RegionEditor
            InitializeRegionEditor();
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
            if (ImageSourceSelector != null)
                ImageSourceSelector.ImageSourceChanged += OnImageSourceChanged;
            
            // 参数绑定
            if (ThresholdParam != null)
            {
                var binding = new Binding("Parameters.Threshold")
                {
                    Source = _viewModel,
                    Mode = BindingMode.TwoWay
                };
                ThresholdParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }
            
            if (MaxValueParam != null)
            {
                var binding = new Binding("Parameters.MaxValue")
                {
                    Source = _viewModel,
                    Mode = BindingMode.TwoWay
                };
                MaxValueParam.SetBinding(BindableParameter.IntValueProperty, binding);
            }
        }

        private void InitializeRegionEditor()
        {
            _dataProvider = new WorkflowDataSourceProvider();
            
            if (RegionEditor != null)
            {
                var regionEditorViewModel = new RegionEditorViewModel();
                RegionEditor.DataContext = regionEditorViewModel;
                _regionEditorIntegration = new RegionEditorIntegration(regionEditorViewModel);
                _regionEditorIntegration.SetCurrentNodeId(_viewModel.ToolId);
            }
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (ImageSourceSelector != null)
                _viewModel.SelectedImageSource = ImageSourceSelector.SelectedImageSource;
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
                        if (ImageSourceSelector != null)
                            ImageSourceSelector.SelectedImageSource = matchedSource;
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
            
            if (RegionEditor != null && imageControl != null)
            {
                RegionEditor.SetMainImageControl(imageControl);
                System.Diagnostics.Debug.WriteLine("[ThresholdToolDebugWindow] ✓ RegionEditor 已绑定到主窗口 ImageControl");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ThresholdToolDebugWindow] ⚠ 绑定失败 - RegionEditor: {RegionEditor != null}, ImageControl: {imageControl != null}");
            }
        }

        #endregion

        #region 执行控制

        protected override void OnExecuteRequested()
        {
            if (_viewModel.SelectedImageSource == null)
            {
                if (StatusText != null) StatusText.Text = "⚠️ 请选择输入图像源";
                return;
            }

            if (_dataProvider == null)
            {
                if (StatusText != null) StatusText.Text = "⚠️ 数据提供者未初始化";
                return;
            }

            if (!_dataProvider.HasNodeOutput(_viewModel.SelectedImageSource.NodeId))
            {
                if (StatusText != null) StatusText.Text = "⚠️ 图像源节点尚未执行，请先执行前驱节点";
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                _viewModel.SelectedImageSource.NodeId,
                "Output",
                _viewModel.SelectedImageSource.OutputPortName
            ) as OpenCvSharp.Mat;

            if (imageMat == null || imageMat.Empty())
            {
                if (StatusText != null) StatusText.Text = "⚠️ 无法获取图像数据或图像为空";
                return;
            }

            _viewModel.ExecutionCompleted -= OnViewModelExecutionCompleted;
            _viewModel.ExecutionCompleted += OnViewModelExecutionCompleted;

            _viewModel.RunToolWithImage(imageMat);

            if (RegionEditor != null)
            {
                var regions = RegionEditor.ResolveAllRegions();
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
                    if (StatusText != null)
                        StatusText.Text = $"✅ 执行成功 - 阈值: {thresholdResult.ThresholdUsed:F0}";
                    
                    if (OutputImage != null && _viewModel.OutputImage != null)
                    {
                        OutputImage.Source = _viewModel.OutputImage;
                    }
                    
                    ToolExecutionCompleted?.Invoke(this, thresholdResult);
                }
                else
                {
                    if (StatusText != null)
                        StatusText.Text = $"❌ 执行失败 - {result.ErrorMessage}";
                }
            });
        }

        protected override void OnResetRequested()
        {
            _viewModel.ResetParameters();
            if (StatusText != null) StatusText.Text = "参数已重置";
            base.OnResetRequested();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            SaveConfigToNode();
            _regionEditorIntegration?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}
