using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.UI.Tools;
using SunEyeVision.PluginSystem.Tools.ROICropTool.ViewModels;

namespace SunEyeVision.PluginSystem.UI.Tools
{
    public partial class ROICropToolDebugWindow : BaseToolDebugWindow
    {
        public ROICropToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new ROICropToolViewModel();
        }
    }
}
