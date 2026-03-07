using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.TemplateMatching.Views
{
    /// <summary>
    /// 模板匹配工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class TemplateMatchingToolDebugWindow : BaseToolDebugWindow
    {
        private TemplateMatchingToolViewModel _viewModel = null!;

        public TemplateMatchingToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new TemplateMatchingToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public TemplateMatchingToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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
            
            if (MethodCombo != null)
            {
                MethodCombo.ItemsSource = _viewModel.Methods;
                MethodCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Method)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (ThresholdParam != null)
            {
                ThresholdParam.DoubleValue = _viewModel.Threshold;
                ThresholdParam.AvailableBindings = _viewModel.AvailableBindings;
                ThresholdParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Threshold)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                ThresholdParam.BindingTypeChanged += OnThresholdBindingModeChanged;
            }
            
            if (MaxMatchesParam != null)
            {
                MaxMatchesParam.IntValue = _viewModel.MaxMatches;
                MaxMatchesParam.SetBinding(BindableParameter.IntValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.MaxMatches)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (MultiScaleCheckBox != null)
            {
                MultiScaleCheckBox.SetBinding(CheckBox.IsCheckedProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.MultiScale)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (ImageSourceSelector != null)
                _viewModel.SelectedImageSource = ImageSourceSelector.SelectedImageSource;
        }

        private void OnThresholdBindingModeChanged(object sender, RoutedEventArgs e)
        {
            if (ThresholdParam != null)
            {
                _viewModel.ThresholdBindingMode = ThresholdParam.BindingType;
                _viewModel.ThresholdBindingSource = ThresholdParam.BindingSource;
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
                if (ThresholdParam != null)
                    ThresholdParam.DoubleValue = _viewModel.Threshold;
            }
            base.OnResetRequested();
        }
    }
}
