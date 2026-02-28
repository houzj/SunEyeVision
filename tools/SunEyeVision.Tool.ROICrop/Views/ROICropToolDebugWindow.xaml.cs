using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ROICrop.Views
{
    /// <summary>
    /// ROI裁剪工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class ROICropToolDebugWindow : BaseToolDebugWindow
    {
        private ROICropToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;

        public ROICropToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ROICropToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public ROICropToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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

            AddToBasicParams(CreateSectionHeader("ROI区域设置"));

            AddToBasicParams(new ParamSlider
            {
                Label = "X坐标",
                Value = _viewModel.X,
                Minimum = 0,
                Maximum = 1000
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "Y坐标",
                Value = _viewModel.Y,
                Minimum = 0,
                Maximum = 1000
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "宽度",
                Value = _viewModel.Width,
                Minimum = 1,
                Maximum = 2000
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "高度",
                Value = _viewModel.Height,
                Minimum = 1,
                Maximum = 2000
            });

            AddToBasicParams(new LabeledControl
            {
                Label = "归一化坐标",
                Content = new CheckBox
                {
                    IsChecked = _viewModel.Normalize,
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
