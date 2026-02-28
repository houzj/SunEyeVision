using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.OCR.Views
{
    /// <summary>
    /// OCR识别工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class OCRToolDebugWindow : BaseToolDebugWindow
    {
        private OCRToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;

        public OCRToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new OCRToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public OCRToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            Title = $"{_viewModel.ToolName} - 调试窗口";
        }

        private void InitializeUI()
        {
            // ===== 基本参数Tab =====
            // 图像源选择 - 使用ImageSourceSelector控件
            _imageSourceSelector = new ImageSourceSelector
            {
                Label = "输入图像",
                SelectedImageSource = _viewModel.SelectedImageSource,
                AvailableImageSources = _viewModel.AvailableImageSources,
                ShowThumbnail = true,
                ShowSizeInfo = true,
                PlaceholderText = "选择图像源..."
            };
            _imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;
            AddToBasicParams(_imageSourceSelector);

            AddToBasicParams(CreateSectionHeader("OCR设置"));

            AddToBasicParams(new ParamComboBox
            {
                Label = "识别语言",
                ItemsSource = _viewModel.Languages,
                SelectedItem = _viewModel.Language
            });

            AddToBasicParams(CreateSectionHeader("Tessdata路径"));

            AddToBasicParams(new ParamTextBox
            {
                Label = "数据路径",
                Text = _viewModel.DataPath,
                Height = 32
            });

            AddToBasicParams(CreateSectionHeader("高级设置"));

            AddToBasicParams(new ParamTextBox
            {
                Label = "字符白名单",
                Text = _viewModel.CharWhitelist,
                Height = 60,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            });

            AddToBasicParams(new ParamComboBox
            {
                Label = "页面分割模式 (PSM)",
                ItemsSource = new[] {
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
                },
                SelectedItem = "3: 全自动"
            });
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedImageSource = _imageSourceSelector.SelectedImageSource;
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
            }
            base.OnResetRequested();
        }

        private static TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#333333")),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }
    }
}
