using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ColorConvert.Views
{
    /// <summary>
    /// 颜色空间转换工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class ColorConvertToolDebugWindow : BaseToolDebugWindow
    {
        private ColorConvertToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;

        public ColorConvertToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ColorConvertToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public ColorConvertToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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

            AddToBasicParams(CreateSectionHeader("颜色空间设置"));

            AddToBasicParams(new ParamComboBox
            {
                Label = "源颜色空间",
                ItemsSource = _viewModel.SourceColorSpaces,
                SelectedItem = _viewModel.SourceColorSpace
            });

            AddToBasicParams(new ParamComboBox
            {
                Label = "目标颜色空间",
                ItemsSource = _viewModel.ColorSpaces,
                SelectedItem = _viewModel.TargetColorSpace
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
