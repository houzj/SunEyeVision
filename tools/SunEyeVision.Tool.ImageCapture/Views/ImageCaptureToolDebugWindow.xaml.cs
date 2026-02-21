using SunEyeVision.Plugin.Abstractions;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tool.ImageCapture.Views
{
    public partial class ImageCaptureToolDebugWindow
    {
        public ImageCaptureToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new ImageCaptureToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
