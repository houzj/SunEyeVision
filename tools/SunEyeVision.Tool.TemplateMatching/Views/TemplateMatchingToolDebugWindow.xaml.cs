using SunEyeVision.Plugin.SDK;
using System.Windows;
using SunEyeVision.Plugin.SDK.ViewModels;
namespace SunEyeVision.Tool.TemplateMatching.Views
{
    public partial class TemplateMatchingToolDebugWindow
    {
        public TemplateMatchingToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new TemplateMatchingToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
