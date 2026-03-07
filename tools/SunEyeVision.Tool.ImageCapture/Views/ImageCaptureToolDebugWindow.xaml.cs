using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.ImageCapture.Views
{
    /// <summary>
    /// 图像采集工具调试窗口 - 使用XAML Tabs架构
    /// </summary>
    public partial class ImageCaptureToolDebugWindow : BaseToolDebugWindow
    {
        private ImageCaptureToolViewModel _viewModel = null!;

        public ImageCaptureToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ImageCaptureToolViewModel();
            DataContext = _viewModel;
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ImageCaptureToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = _viewModel.ToolName;
        }

        private void SetupBindingsAndEvents()
        {
            if (DeviceIdCombo != null)
            {
                DeviceIdCombo.ItemsSource = new[] { "默认设备", "设备 1", "设备 2" };
                DeviceIdCombo.SelectedItem = "默认设备";
            }
            
            if (WidthParam != null)
            {
                WidthParam.IntValue = _viewModel.Width;
                WidthParam.SetBinding(BindableParameter.IntValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Width)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (HeightParam != null)
            {
                HeightParam.IntValue = _viewModel.Height;
                HeightParam.SetBinding(BindableParameter.IntValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Height)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (ExposureParam != null)
            {
                ExposureParam.DoubleValue = _viewModel.ExposureTime;
                ExposureParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.ExposureTime)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            if (GainParam != null)
            {
                GainParam.DoubleValue = _viewModel.Gain;
                GainParam.SetBinding(BindableParameter.DoubleValueProperty,
                    new System.Windows.Data.Binding(nameof(_viewModel.Gain)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
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
