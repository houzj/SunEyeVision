using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.ColorConvertTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.ColorConvertTool.Views
{
    public partial class ColorConvertToolDebugWindow : BaseToolDebugWindow
    {
        public ColorConvertToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new ColorConvertToolViewModel();
        }
    }
}
