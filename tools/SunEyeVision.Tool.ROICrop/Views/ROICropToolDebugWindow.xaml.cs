using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ROICrop.Views
{
    /// <summary>
    /// ROI裁剪工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class ROICropToolDebugWindow : BaseToolDebugWindow
    {
        private ROICropToolViewModel _viewModel = null!;

        public ROICropToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ROICropToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ROICropToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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
            
            if (XParam != null)
            {
                XParam.DoubleValue = _viewModel.X;
                XParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.X)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (YParam != null)
            {
                YParam.DoubleValue = _viewModel.Y;
                YParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Y)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (WidthParam != null)
            {
                WidthParam.DoubleValue = _viewModel.Width;
                WidthParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Width)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (HeightParam != null)
            {
                HeightParam.DoubleValue = _viewModel.Height;
                HeightParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Height)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (NormalizeCheckBox != null)
            {
                NormalizeCheckBox.SetBinding(CheckBox.IsCheckedProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Normalize)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
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
