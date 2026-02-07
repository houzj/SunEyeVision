using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.ThresholdTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.ThresholdTool.Views
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
