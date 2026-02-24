using SunEyeVision.Plugin.SDK;
using System.Windows;
using SunEyeVision.Plugin.SDK.ViewModels;
namespace SunEyeVision.Tool.ROICrop.Views
{
    public partial class ROICropToolDebugWindow
    {
        public ROICropToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new ROICropToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
