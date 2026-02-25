using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Controls.ParameterBinding
{
    /// <summary>
    /// ParameterBindingControl.xaml 的交互逻辑
    /// </summary>
    public partial class ParameterBindingControl : UserControl
    {
        public ParameterBindingControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// ViewModel依赖属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(ViewModels.ParameterBindingViewModel),
                typeof(ParameterBindingControl),
                new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// ViewModel
        /// </summary>
        public ViewModels.ParameterBindingViewModel? ViewModel
        {
            get => (ViewModels.ParameterBindingViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 是否显示验证消息依赖属性
        /// </summary>
        public static readonly DependencyProperty ShowValidationMessageProperty =
            DependencyProperty.Register(
                nameof(ShowValidationMessage),
                typeof(bool),
                typeof(ParameterBindingControl),
                new PropertyMetadata(true));

        /// <summary>
        /// 是否显示验证消息
        /// </summary>
        public bool ShowValidationMessage
        {
            get => (bool)GetValue(ShowValidationMessageProperty);
            set => SetValue(ShowValidationMessageProperty, value);
        }

        /// <summary>
        /// 是否只读依赖属性
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ParameterBindingControl),
                new PropertyMetadata(false));

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion

        #region 私有方法

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ParameterBindingControl control)
            {
                control.DataContext = e.NewValue;
            }
        }

        #endregion
    }
}
