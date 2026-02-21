using SunEyeVision.Plugin.Abstractions;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tool.Threshold.Views
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
