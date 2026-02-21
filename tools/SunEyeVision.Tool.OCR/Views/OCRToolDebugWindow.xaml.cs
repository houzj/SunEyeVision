using SunEyeVision.Plugin.Abstractions;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tool.OCR.Views
{
    public partial class OCRToolDebugWindow
    {
        public OCRToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new OCRToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
