using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ImageSave.Views
{
    /// <summary>
    /// 图像保存工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class ImageSaveToolDebugWindow : BaseToolDebugWindow
    {
        private ImageSaveToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;

        public ImageSaveToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ImageSaveToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public ImageSaveToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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

            AddToBasicParams(CreateSectionHeader("文件设置"));

            AddToBasicParams(new ParamTextBox
            {
                Label = "文件路径",
                Text = _viewModel.FilePath,
                Height = 32
            });

            AddToBasicParams(new ParamComboBox
            {
                Label = "图像格式",
                ItemsSource = _viewModel.ImageFormats,
                SelectedItem = _viewModel.ImageFormat
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "图像质量",
                Value = _viewModel.ImageQuality,
                Minimum = 1,
                Maximum = 100
            });

            AddToBasicParams(new LabeledControl
            {
                Label = "覆盖已有文件",
                Content = new CheckBox
                {
                    IsChecked = _viewModel.OverwriteExisting,
                    VerticalAlignment = VerticalAlignment.Center
                }
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
