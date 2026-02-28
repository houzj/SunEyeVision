using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.EdgeDetection.Views
{
    /// <summary>
    /// 边缘检测工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class EdgeDetectionToolDebugWindow : BaseToolDebugWindow
    {
        private EdgeDetectionToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;
        private BindableParameter _threshold1Param = null!;
        private BindableParameter _threshold2Param = null!;

        public EdgeDetectionToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new EdgeDetectionToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public EdgeDetectionToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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

            AddToBasicParams(CreateSectionHeader("边缘检测算法"));

            AddToBasicParams(new ParamComboBox
            {
                Label = "算法类型",
                ItemsSource = _viewModel.Algorithms,
                SelectedItem = _viewModel.Algorithm
            });

            // ===== 运行参数Tab =====
            // 使用BindableParameter控件 - 阈值1
            _threshold1Param = new BindableParameter
            {
                Label = "阈值1 (低阈值)",
                Value = _viewModel.Threshold1,
                Minimum = 0,
                Maximum = 255,
                SmallChange = 1,
                LargeChange = 10,
                MinValueLabel = "[0-255]",
                ShowSlider = true,
                DecimalPlaces = 0,
                AvailableBindings = _viewModel.AvailableBindings
            };
            _threshold1Param.BindingModeChanged += OnThreshold1BindingModeChanged;
            AddToRuntimeParams(_threshold1Param);

            // 使用BindableParameter控件 - 阈值2
            _threshold2Param = new BindableParameter
            {
                Label = "阈值2 (高阈值)",
                Value = _viewModel.Threshold2,
                Minimum = 0,
                Maximum = 255,
                SmallChange = 1,
                LargeChange = 10,
                MinValueLabel = "[0-255]",
                ShowSlider = true,
                DecimalPlaces = 0,
                AvailableBindings = _viewModel.AvailableBindings
            };
            _threshold2Param.BindingModeChanged += OnThreshold2BindingModeChanged;
            AddToRuntimeParams(_threshold2Param);

            AddToRuntimeParams(CreateSectionHeader("孔径设置"));

            AddToRuntimeParams(new ParamComboBox
            {
                Label = "孔径大小",
                ItemsSource = new[] { "3", "5", "7" },
                SelectedItem = _viewModel.ApertureSize.ToString()
            });
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        private void OnThreshold1BindingModeChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Threshold1BindingMode = _threshold1Param.BindingMode;
            _viewModel.Threshold1BindingSource = _threshold1Param.BindingSource;
        }

        private void OnThreshold2BindingModeChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Threshold2BindingMode = _threshold2Param.BindingMode;
            _viewModel.Threshold2BindingSource = _threshold2Param.BindingSource;
        }

        protected override void OnExecuteRequested()
        {
            // 同步参数值到ViewModel
            _viewModel.Threshold1 = (int)_threshold1Param.Value;
            _viewModel.Threshold2 = (int)_threshold2Param.Value;
            _viewModel?.RunTool();
            base.OnExecuteRequested();
        }

        protected override void OnResetRequested()
        {
            if (_viewModel != null)
            {
                _viewModel.ResetParameters();
                // 重置BindableParameter控件
                _threshold1Param.Value = _viewModel.Threshold1;
                _threshold2Param.Value = _viewModel.Threshold2;
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
