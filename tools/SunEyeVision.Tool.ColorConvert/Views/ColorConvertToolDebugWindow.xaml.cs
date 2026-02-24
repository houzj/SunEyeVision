using System.Windows;
using SunEyeVision.Plugin.SDK;
namespace SunEyeVision.Tool.ColorConvert.Views
{
    /// <summary>
    /// 颜色空间转换工具调试窗口
    /// </summary>
    public partial class ColorConvertToolDebugWindow : Window
    {
        public ColorConvertToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new ColorConvertToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
