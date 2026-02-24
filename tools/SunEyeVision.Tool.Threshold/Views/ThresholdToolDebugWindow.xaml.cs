using SunEyeVision.Plugin.SDK;
using System.Windows;
using SunEyeVision.Plugin.SDK.ViewModels;
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
