using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.TemplateMatchingTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.TemplateMatchingTool.Views
{
    public partial class TemplateMatchingToolDebugWindow : BaseToolDebugWindow
    {
        public TemplateMatchingToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new TemplateMatchingToolViewModel();
        }
    }
}
