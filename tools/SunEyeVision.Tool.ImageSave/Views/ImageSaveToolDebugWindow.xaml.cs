using SunEyeVision.Plugin.SDK;
using System.Windows;
using SunEyeVision.Plugin.SDK.ViewModels;
namespace SunEyeVision.Tool.ImageSave.Views
{
    public partial class ImageSaveToolDebugWindow
    {
        public ImageSaveToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new ImageSaveToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
