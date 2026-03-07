using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.GaussianBlur.Views
{
    /// <summary>
    /// 高斯模糊工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class GaussianBlurToolDebugWindow : BaseToolDebugWindow
    {
        private GaussianBlurToolViewModel _viewModel = null!;

        public GaussianBlurToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new GaussianBlurToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public GaussianBlurToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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
            
            if (KernelSizeCombo != null)
            {
                KernelSizeCombo.ItemsSource = new[] { "3", "5", "7", "9", "11", "13", "15", "17", "19", "21" };
                KernelSizeCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.KernelSize)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (SigmaParam != null)
            {
                SigmaParam.AvailableBindings = _viewModel.AvailableBindings;
                SigmaParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Sigma)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                SigmaParam.BindingTypeChanged += OnSigmaBindingModeChanged;
            }
            
            if (BorderTypeCombo != null)
            {
                BorderTypeCombo.ItemsSource = _viewModel.BorderTypes;
                BorderTypeCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.BorderType)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (ImageSourceSelector != null)
                _viewModel.SelectedImageSource = ImageSourceSelector.SelectedImageSource;
        }

        private void OnSigmaBindingModeChanged(object sender, RoutedEventArgs e)
        {
            if (SigmaParam != null)
            {
                _viewModel.SigmaBindingMode = SigmaParam.BindingType;
                _viewModel.SigmaBindingSource = SigmaParam.BindingSource;
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
                if (SigmaParam != null)
                    SigmaParam.DoubleValue = _viewModel.Sigma;
            }
            base.OnResetRequested();
        }
    }
}
