using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;

using SunEyeVision.Plugin.Infrastructure;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tools.TemplateMatchingTool.Views
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
