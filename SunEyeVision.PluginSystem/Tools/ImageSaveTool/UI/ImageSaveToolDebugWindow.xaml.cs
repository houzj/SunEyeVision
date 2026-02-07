using System.Windows;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Tools.ImageSaveTool.ViewModels;
using SunEyeVision.PluginSystem.UI;

namespace SunEyeVision.PluginSystem.Tools.ImageSaveTool.UI
{
    /// <summary>
    /// ImageSaveToolDebugWindow.xaml 的交互逻辑 - 图像保存工具的调试窗口
    /// </summary>
    public partial class ImageSaveToolDebugWindow : Window
    {
        private readonly ImageSaveToolViewModel _viewModel;

        public ImageSaveToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            _viewModel = new ImageSaveToolViewModel();
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = _viewModel;
        }
    }
}
