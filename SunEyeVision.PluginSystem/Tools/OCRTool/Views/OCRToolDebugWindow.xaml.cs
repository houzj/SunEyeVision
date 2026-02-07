using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.OCRTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.OCRTool.Views
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
