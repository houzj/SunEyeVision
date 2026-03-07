using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ImageSave.Views
{
    /// <summary>
    /// 图像保存工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class ImageSaveToolDebugWindow : BaseToolDebugWindow
    {
        private ImageSaveToolViewModel _viewModel = null!;

        public ImageSaveToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ImageSaveToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ImageSaveToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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
            
            if (FilePathTextBox != null)
            {
                FilePathTextBox.SetBinding(ParamTextBox.TextProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.FilePath)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (ImageFormatCombo != null)
            {
                ImageFormatCombo.ItemsSource = _viewModel.ImageFormats;
                ImageFormatCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.ImageFormat)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (QualityParam != null)
            {
                QualityParam.IntValue = _viewModel.ImageQuality;
                QualityParam.SetBinding(BindableParameter.IntValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.ImageQuality)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (OverwriteCheckBox != null)
            {
                OverwriteCheckBox.SetBinding(CheckBox.IsCheckedProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.OverwriteExisting)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
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
