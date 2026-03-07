using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.EdgeDetection.Views
{
    /// <summary>
    /// 边缘检测工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class EdgeDetectionToolDebugWindow : BaseToolDebugWindow
    {
        private EdgeDetectionToolViewModel _viewModel = null!;

        public EdgeDetectionToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new EdgeDetectionToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public EdgeDetectionToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = _viewModel.ToolName;
        }

        private void SetupBindingsAndEvents()
        {
            if (ImageSourceSelector != null)
            {
                ImageSourceSelector.SetBinding(ImageSourceSelector.SelectedImageSourceProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.SelectedImageSource)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                ImageSourceSelector.SetBinding(ImageSourceSelector.AvailableImageSourcesProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.AvailableImageSources)) { Source = _viewModel });
                ImageSourceSelector.ImageSourceChanged += OnImageSourceChanged;
            }
            
            if (AlgorithmCombo != null)
            {
                AlgorithmCombo.ItemsSource = _viewModel.Algorithms;
                AlgorithmCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Algorithm)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (Threshold1Param != null)
            {
                Threshold1Param.IntValue = _viewModel.Threshold1;
                Threshold1Param.AvailableBindings = _viewModel.AvailableBindings;
                Threshold1Param.SetBinding(BindableParameter.IntValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Threshold1)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                Threshold1Param.BindingTypeChanged += OnThreshold1BindingModeChanged;
            }
            
            if (Threshold2Param != null)
            {
                Threshold2Param.IntValue = _viewModel.Threshold2;
                Threshold2Param.AvailableBindings = _viewModel.AvailableBindings;
                Threshold2Param.SetBinding(BindableParameter.IntValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Threshold2)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                Threshold2Param.BindingTypeChanged += OnThreshold2BindingModeChanged;
            }
            
            if (ApertureSizeCombo != null)
            {
                ApertureSizeCombo.ItemsSource = new[] { "3", "5", "7" };
                ApertureSizeCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.ApertureSize)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (ImageSourceSelector != null)
                _viewModel.SelectedImageSource = ImageSourceSelector.SelectedImageSource;
        }

        private void OnThreshold1BindingModeChanged(object sender, RoutedEventArgs e)
        {
            if (Threshold1Param != null)
            {
                _viewModel.Threshold1BindingMode = Threshold1Param.BindingType;
                _viewModel.Threshold1BindingSource = Threshold1Param.BindingSource;
            }
        }

        private void OnThreshold2BindingModeChanged(object sender, RoutedEventArgs e)
        {
            if (Threshold2Param != null)
            {
                _viewModel.Threshold2BindingMode = Threshold2Param.BindingType;
                _viewModel.Threshold2BindingSource = Threshold2Param.BindingSource;
            }
        }

        protected override void OnExecuteRequested()
        {
            _viewModel?.RunTool();
            base.OnExecuteRequested();
        }

        protected override void OnResetRequested()
        {
            if (_viewModel != null)
            {
                _viewModel.ResetParameters();
                if (Threshold1Param != null)
                    Threshold1Param.IntValue = _viewModel.Threshold1;
                if (Threshold2Param != null)
                    Threshold2Param.IntValue = _viewModel.Threshold2;
            }
            base.OnResetRequested();
        }
    }
}
