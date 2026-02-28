using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.GaussianBlur.Views
{
    /// <summary>
    /// 高斯模糊工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class GaussianBlurToolDebugWindow : BaseToolDebugWindow
    {
        private GaussianBlurToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;
        private BindableParameter _sigmaParam = null!;

        public GaussianBlurToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new GaussianBlurToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public GaussianBlurToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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

            // 核大小 - 使用ParamComboBox（奇数值选择）
            AddToBasicParams(new ParamComboBox
            {
                Label = "核大小（奇数）",
                ItemsSource = new[] { "3", "5", "7", "9", "11", "13", "15", "17", "19", "21" },
                SelectedItem = _viewModel.KernelSize.ToString()
            });

            // ===== 运行参数Tab =====
            // 使用BindableParameter控件 - Sigma
            _sigmaParam = new BindableParameter
            {
                Label = "标准差（Sigma）",
                Value = _viewModel.Sigma,
                Minimum = 0.1,
                Maximum = 10.0,
                SmallChange = 0.1,
                LargeChange = 1.0,
                MinValueLabel = "[0.1-10.0]",
                ShowSlider = true,
                DecimalPlaces = 1,
                AvailableBindings = _viewModel.AvailableBindings
            };
            _sigmaParam.BindingModeChanged += OnSigmaBindingModeChanged;
            AddToRuntimeParams(_sigmaParam);

            AddToRuntimeParams(new ParamComboBox
            {
                Label = "边界类型",
                ItemsSource = _viewModel.BorderTypes,
                SelectedItem = _viewModel.BorderType
            });
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        private void OnSigmaBindingModeChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SigmaBindingMode = _sigmaParam.BindingMode;
            _viewModel.SigmaBindingSource = _sigmaParam.BindingSource;
        }

        protected override void OnExecuteRequested()
        {
            // 同步参数值到ViewModel
            _viewModel.Sigma = _sigmaParam.Value;
            _viewModel?.RunTool();
            base.OnExecuteRequested();
        }

        protected override void OnResetRequested()
        {
            if (_viewModel != null)
            {
                _viewModel.ResetParameters();
                // 重置BindableParameter控件
                _sigmaParam.Value = _viewModel.Sigma;
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
