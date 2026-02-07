using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.UI.Tools;
using SunEyeVision.PluginSystem.Tools.ImageCaptureTool.ViewModels;

namespace SunEyeVision.PluginSystem.UI.Tools
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
