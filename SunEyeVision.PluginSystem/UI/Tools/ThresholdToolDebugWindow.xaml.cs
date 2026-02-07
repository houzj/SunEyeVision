using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.UI.Tools;
using SunEyeVision.PluginSystem.Tools.ThresholdTool.ViewModels;

namespace SunEyeVision.PluginSystem.UI.Tools
{
    public partial class ThresholdToolDebugWindow : BaseToolDebugWindow
    {
        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new ThresholdToolViewModel();
        }
    }
}
