using System.Windows;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Plugin.Infrastructure.UI.Windows
{
    /// <summary>
    /// BaseToolDebugWindow.xaml 的交互逻辑 - 工具调试窗口基类
    /// </summary>
    public partial class BaseToolDebugWindow : Window
    {
        /// <summary>
        /// 无参构造函数（供XAML编译器使用）
        /// </summary>
        protected BaseToolDebugWindow()
        {
            InitializeComponent();
        }

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

        /// <summary>
        /// 自定义内容 - 由子类XAML填充
        /// </summary>
        public object CustomContent
        {
            get => GetValue(CustomContentProperty);
            set => SetValue(CustomContentProperty, value);
        }

        public static readonly DependencyProperty CustomContentProperty =
            DependencyProperty.Register(nameof(CustomContent), typeof(object), typeof(BaseToolDebugWindow), new PropertyMetadata(null, OnCustomContentChanged));

        private static void OnCustomContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseToolDebugWindow window)
            {
                window.MainContentArea.Content = e.NewValue;
            }
        }
    }
}
