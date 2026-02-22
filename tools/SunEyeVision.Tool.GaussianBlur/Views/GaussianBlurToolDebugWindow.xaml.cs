using SunEyeVision.Plugin.Abstractions;
using System.Windows;
using SunEyeVision.Plugin.Abstractions.ViewModels;
namespace SunEyeVision.Tool.GaussianBlur.Views
{
    public partial class GaussianBlurToolDebugWindow : Window
    {
        public GaussianBlurToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new GaussianBlurToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
