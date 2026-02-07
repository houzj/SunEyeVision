using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.EdgeDetectionTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.EdgeDetectionTool.Views
{
    public partial class EdgeDetectionToolDebugWindow : BaseToolDebugWindow
    {
        public EdgeDetectionToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new EdgeDetectionToolViewModel();
        }
    }
}
