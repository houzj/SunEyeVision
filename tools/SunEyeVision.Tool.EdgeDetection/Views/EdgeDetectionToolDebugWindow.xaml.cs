using SunEyeVision.Plugin.Abstractions;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tool.EdgeDetection.Views
{
    public partial class EdgeDetectionToolDebugWindow
    {
        public EdgeDetectionToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new EdgeDetectionToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
