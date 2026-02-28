using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.TemplateMatching.Views
{
    /// <summary>
    /// 模板匹配工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class TemplateMatchingToolDebugWindow : BaseToolDebugWindow
    {
        private TemplateMatchingToolViewModel _viewModel = null!;
        private ImageSourceSelector _imageSourceSelector = null!;
        private BindableParameter _thresholdParam = null!;

        public TemplateMatchingToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new TemplateMatchingToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public TemplateMatchingToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
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

            AddToBasicParams(CreateSectionHeader("匹配参数设置"));

            AddToBasicParams(new ParamComboBox
            {
                Label = "匹配方法",
                ItemsSource = _viewModel.Methods,
                SelectedItem = _viewModel.Method
            });

            // ===== 运行参数Tab =====
            // 使用BindableParameter控件 - 匹配阈值
            _thresholdParam = new BindableParameter
            {
                Label = "匹配阈值",
                Value = _viewModel.Threshold,
                Minimum = 0,
                Maximum = 1,
                SmallChange = 0.05,
                LargeChange = 0.1,
                MinValueLabel = "[0-1]",
                ShowSlider = true,
                DecimalPlaces = 2,
                AvailableBindings = _viewModel.AvailableBindings
            };
            _thresholdParam.BindingModeChanged += OnThresholdBindingModeChanged;
            AddToRuntimeParams(_thresholdParam);

            AddToRuntimeParams(new ParamSlider
            {
                Label = "最大匹配数",
                Value = _viewModel.MaxMatches,
                Minimum = 1,
                Maximum = 100
            });

            AddToRuntimeParams(new LabeledControl
            {
                Label = "多尺度匹配",
                Content = new CheckBox
                {
                    IsChecked = _viewModel.MultiScale,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        private void OnThresholdBindingModeChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ThresholdBindingMode = _thresholdParam.BindingMode;
            _viewModel.ThresholdBindingSource = _thresholdParam.BindingSource;
        }

        protected override void OnExecuteRequested()
        {
            // 同步参数值到ViewModel
            _viewModel.Threshold = _thresholdParam.Value;
            _viewModel?.RunTool();
            base.OnExecuteRequested();
        }

        protected override void OnResetRequested()
        {
            if (_viewModel != null)
            {
                _viewModel.ResetParameters();
                // 重置BindableParameter控件
                _thresholdParam.Value = _viewModel.Threshold;
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
