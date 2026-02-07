using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.GaussianBlurTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.GaussianBlurTool.Views
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
