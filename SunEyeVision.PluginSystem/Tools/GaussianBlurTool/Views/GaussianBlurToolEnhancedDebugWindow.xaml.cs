using System;
using System.Windows;
using SunEyeVision.PluginSystem.Base;
using SunEyeVision.PluginSystem.Base.Windows;
using SunEyeVision.PluginSystem.Tools.GaussianBlurTool.ViewModels;

namespace SunEyeVision.PluginSystem.Tools.GaussianBlurTool.Views
{
    /// <summary>
    /// GaussianBlurToolEnhancedDebugWindow.xaml 的交互逻辑
    /// 完整MVVM架构实现的示例窗口
    /// </summary>
    public partial class GaussianBlurToolEnhancedDebugWindow : EnhancedToolDebugWindow
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public GaussianBlurToolEnhancedDebugWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 使用ViewModel初始化窗口
        /// </summary>
        public void Initialize(GaussianBlurToolViewModel viewModel)
        {
            base.Initialize(viewModel);
        }
    }
}
