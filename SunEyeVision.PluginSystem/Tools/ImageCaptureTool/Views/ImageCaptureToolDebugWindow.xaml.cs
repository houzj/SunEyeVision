using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.ImageCaptureTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.ImageCaptureTool.Views
{
    public partial class ImageCaptureToolDebugWindow : BaseToolDebugWindow
    {
        public ImageCaptureToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new ImageCaptureToolViewModel();
        }
    }
}
