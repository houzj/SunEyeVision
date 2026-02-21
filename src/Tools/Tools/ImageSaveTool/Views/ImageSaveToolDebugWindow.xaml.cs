using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;

using SunEyeVision.Plugin.Infrastructure;
using System.Windows;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tools.ImageSaveTool.Views
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
