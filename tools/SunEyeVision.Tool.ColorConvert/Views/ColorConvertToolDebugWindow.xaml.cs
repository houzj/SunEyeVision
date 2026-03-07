using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ColorConvert.Views
{
    /// <summary>
    /// 颜色空间转换工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class ColorConvertToolDebugWindow : BaseToolDebugWindow
    {
        private ColorConvertToolViewModel _viewModel = null!;

        public ColorConvertToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ColorConvertToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ColorConvertToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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
            
            if (SourceColorSpaceCombo != null)
            {
                SourceColorSpaceCombo.ItemsSource = _viewModel.SourceColorSpaces;
                SourceColorSpaceCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.SourceColorSpace)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (TargetColorSpaceCombo != null)
            {
                TargetColorSpaceCombo.ItemsSource = _viewModel.ColorSpaces;
                TargetColorSpaceCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.TargetColorSpace)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (ImageSourceSelector != null)
                _viewModel.SelectedImageSource = ImageSourceSelector.SelectedImageSource;
        }

        protected override void OnExecuteRequested()
        {
            _viewModel?.RunTool();
            base.OnExecuteRequested();
        }

        protected override void OnResetRequested()
        {
            _viewModel?.ResetParameters();
            base.OnResetRequested();
        }
    }
}
