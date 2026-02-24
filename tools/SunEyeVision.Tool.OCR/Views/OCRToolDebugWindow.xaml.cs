using SunEyeVision.Plugin.SDK;
using System.Windows;
using SunEyeVision.Plugin.SDK.ViewModels;
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
