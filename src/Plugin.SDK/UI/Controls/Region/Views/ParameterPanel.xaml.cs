using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Views
{
    /// <summary>
    /// ParameterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterPanel : UserControl
    {
        public ParameterPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 绑定按钮点击事件
        /// </summary>
        private void BindButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ParameterBindingItem item)
            {
                // 设置选中的参数项
                if (DataContext is ParameterPanelViewModel viewModel)
                {
                    viewModel.SelectedParameter = item;
                    
                    // 触发绑定选择事件
                    OnParameterBindRequested?.Invoke(this, new ParameterBindRequestedEventArgs(item));
                }
            }
        }

        /// <summary>
        /// 清除按钮点击事件
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ParameterBindingItem item)
            {
                if (DataContext is ParameterPanelViewModel viewModel)
                {
                    viewModel.ClearBindingCommand.Execute(item);
                }
            }
        }

        /// <summary>
        /// 参数绑定请求事件
        /// </summary>
        public event ParameterBindRequestedEventHandler? OnParameterBindRequested;
    }

    /// <summary>
    /// 参数绑定请求事件参数
    /// </summary>
    public class ParameterBindRequestedEventArgs : RoutedEventArgs
    {
        public ParameterBindingItem ParameterItem { get; }

        public ParameterBindRequestedEventArgs(ParameterBindingItem parameterItem)
        {
            ParameterItem = parameterItem;
        }
    }

    /// <summary>
    /// 参数绑定请求事件委托
    /// </summary>
    public delegate void ParameterBindRequestedEventHandler(object sender, ParameterBindRequestedEventArgs e);
}
