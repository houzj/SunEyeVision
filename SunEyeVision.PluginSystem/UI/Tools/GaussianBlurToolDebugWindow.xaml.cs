using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.UI.Tools;
using SunEyeVision.PluginSystem.Tools.GaussianBlurTool.ViewModels;

namespace SunEyeVision.PluginSystem.UI.Tools
{
    /// <summary>
    /// 高斯模糊工具调试窗口
    /// </summary>
    public partial class GaussianBlurToolDebugWindow : BaseToolDebugWindow
    {
        public GaussianBlurToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : base(toolId, toolPlugin, toolMetadata)
        {
            InitializeComponent();
        }

        protected override ToolDebugViewModelBase CreateViewModel()
        {
            return new GaussianBlurToolViewModel();
        }
    }
}
