using System;
using System.Windows;
using SunEyeVision.PluginSystem.Infrastructure.Base;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;
using SunEyeVision.PluginSystem.Base.Base;

namespace SunEyeVision.PluginSystem.Infrastructure.UI.Windows
{
    /// <summary>
    /// 增强版工具调试窗口 - 支持完整的MVVM架构
    /// 提供动态参数控件、命令绑定、进度显示等功能
    /// </summary>
    public partial class EnhancedToolDebugWindow : Window
    {
        private AutoToolDebugViewModelBase? _viewModel;

        /// <summary>
        /// 是否有自定义内容
        /// </summary>
        public bool HasCustomContent { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public EnhancedToolDebugWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 使用ViewModel初始化窗口
        /// </summary>
        public void Initialize(AutoToolDebugViewModelBase viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// 设置自定义内容区域
        /// </summary>
        public void SetCustomContent(object content)
        {
            CustomContentArea.Content = content;
            HasCustomContent = content != null;
        }

        /// <summary>
        /// 设置结果内容区域
        /// </summary>
        public void SetResultContent(object content)
        {
            ResultContentArea.Content = content;
        }

        /// <summary>
        /// 窗口加载事件
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 居中显示在主窗口
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                var rect = new System.Windows.Rect(mainWindow.Left, mainWindow.Top, mainWindow.Width, mainWindow.Height);
                this.Left = rect.Left + (rect.Width - this.Width) / 2;
                this.Top = rect.Top + (rect.Height - this.Height) / 2;
            }
        }
    }
}
