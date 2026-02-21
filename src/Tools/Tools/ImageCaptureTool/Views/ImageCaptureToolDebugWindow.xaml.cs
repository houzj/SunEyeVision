using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;

using SunEyeVision.Plugin.Infrastructure;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tools.ImageCaptureTool.Views
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
