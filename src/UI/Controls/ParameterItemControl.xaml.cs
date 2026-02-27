using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// 参数项控件 - 支持数据绑定配置的Popup
    /// </summary>
    public partial class ParameterItemControl : UserControl
    {
        public ParameterItemControl()
        {
            InitializeComponent();
        }

        #region 依赖属性

        /// <summary>
        /// 参数项ViewModel
        /// </summary>
        public static readonly DependencyProperty ParameterItemProperty =
            DependencyProperty.Register(
                nameof(ParameterItem),
                typeof(ParameterItemViewModel),
                typeof(ParameterItemControl),
                new PropertyMetadata(null, OnParameterItemChanged));

        public ParameterItemViewModel? ParameterItem
        {
            get => (ParameterItemViewModel?)GetValue(ParameterItemProperty);
            set => SetValue(ParameterItemProperty, value);
        }

        private static void OnParameterItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ParameterItemControl control)
            {
                control.DataContext = e.NewValue;
            }
        }

        #endregion

        #region 事件处理

        private void OnConfirmBinding(object sender, RoutedEventArgs e)
        {
            if (ParameterItem != null)
            {
                // 验证绑定配置
                var validation = ParameterItem.Binding.Validate();
                if (!validation.IsValid)
                {
                    MessageBox.Show(string.Join("\n", validation.Errors), "绑定配置错误",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ParameterItem.CloseBindingPopup();
            }
        }

        private void OnCancelBinding(object sender, RoutedEventArgs e)
        {
            if (ParameterItem != null)
            {
                // 恢复原始绑定配置
                ParameterItem.CloseBindingPopup();
            }
        }

        #endregion
    }
}
