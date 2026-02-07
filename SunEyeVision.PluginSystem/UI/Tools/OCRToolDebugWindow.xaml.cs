using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.UI.Tools;
using SunEyeVision.PluginSystem.Tools.OCRTool.ViewModels;

namespace SunEyeVision.PluginSystem.UI.Tools
{
    public partial class OCRToolDebugWindow : BaseToolDebugWindow
    {
        public OCRToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new OCRToolViewModel();
        }
    }
}
