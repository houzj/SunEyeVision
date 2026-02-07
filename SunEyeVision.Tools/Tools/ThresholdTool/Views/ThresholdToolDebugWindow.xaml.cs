using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;

using SunEyeVision.PluginSystem;
using System.Windows;
using SunEyeVision.PluginSystem.Base.Base;
using SunEyeVision.PluginSystem.Infrastructure.Base;
using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;


namespace SunEyeVision.Tools.ThresholdTool.Views
{
    public partial class ThresholdToolDebugWindow
    {
        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new ThresholdToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
