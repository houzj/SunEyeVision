using System.Windows;
using SunEyeVision.PluginSystem;

namespace SunEyeVision.PluginSystem.UI
{
    /// <summary>
    /// BaseToolDebugWindow.xaml 的交互逻辑 - 工具调试窗口基类
    /// </summary>
    public partial class BaseToolDebugWindow : Window
    {
        public BaseToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            InitializeComponent();

            // 绑定ViewModel到DataContext
            var viewModel = CreateViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }

        /// <summary>
        /// 创建ViewModel实例 - 由子类实现
        /// </summary>
        /// <returns>ViewModel实例</returns>
        protected virtual ToolDebugViewModelBase CreateViewModel()
        {
            throw new System.NotImplementedException("子类必须实现CreateViewModel方法");
        }
    }
}
