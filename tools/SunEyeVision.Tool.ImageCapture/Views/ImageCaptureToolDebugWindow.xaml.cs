using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ImageCapture.Views
{
    /// <summary>
    /// 图像采集工具调试窗口 - 继承非泛型基类
    /// </summary>
    public partial class ImageCaptureToolDebugWindow : BaseToolDebugWindow
    {
        private ImageCaptureToolViewModel _viewModel = null!;

        public ImageCaptureToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ImageCaptureToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public ImageCaptureToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            Title = $"{_viewModel.ToolName} - 调试窗口";
        }

        private void InitializeUI()
        {
            // ===== 基本参数Tab =====
            AddToBasicParams(CreateSectionHeader("设备设置"));
            
            AddToBasicParams(new ParamComboBox
            {
                Label = "设备ID",
                ItemsSource = new[] { "默认设备", "设备 1", "设备 2" },
                SelectedItem = "默认设备"
            });

            AddToBasicParams(CreateSectionHeader("分辨率设置"));

            AddToBasicParams(new ParamSlider
            {
                Label = "宽度",
                Value = _viewModel.Width,
                Minimum = 640,
                Maximum = 3840
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "高度",
                Value = _viewModel.Height,
                Minimum = 480,
                Maximum = 2160
            });

            AddToBasicParams(CreateSectionHeader("相机参数"));

            AddToBasicParams(new ParamSlider
            {
                Label = "曝光时间 (ms)",
                Value = _viewModel.ExposureTime,
                Minimum = 0.1,
                Maximum = 1000,
                SmallChange = 0.1,
                LargeChange = 10
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "增益",
                Value = _viewModel.Gain,
                Minimum = 0,
                Maximum = 16,
                SmallChange = 0.1,
                LargeChange = 1
            });
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

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }
    }
}
