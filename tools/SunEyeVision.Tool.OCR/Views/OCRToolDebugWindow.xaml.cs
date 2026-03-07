using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.OCR.Views
{
    /// <summary>
    /// OCR识别工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class OCRToolDebugWindow : BaseToolDebugWindow
    {
        private OCRToolViewModel _viewModel = null!;

        public OCRToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new OCRToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public OCRToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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
            
            if (LanguageCombo != null)
            {
                LanguageCombo.ItemsSource = _viewModel.Languages;
                LanguageCombo.SetBinding(ParamComboBox.SelectedItemProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Language)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (DataPathTextBox != null)
            {
                DataPathTextBox.SetBinding(ParamTextBox.TextProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.DataPath)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (CharWhitelistTextBox != null)
            {
                CharWhitelistTextBox.SetBinding(ParamTextBox.TextProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.CharWhitelist)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (PsmModeCombo != null)
            {
                PsmModeCombo.ItemsSource = new[] {
                    "3: 全自动",
                    "0: 仅方向检测",
                    "1: 自动页面分割",
                    "2: 自动OCR",
                    "4: 单列",
                    "5: 单块垂直文本",
                    "6: 单块文本",
                    "7: 单行",
                    "8: 单词",
                    "9: 单字符"
                };
                PsmModeCombo.SelectedItem = "3: 全自动";
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
