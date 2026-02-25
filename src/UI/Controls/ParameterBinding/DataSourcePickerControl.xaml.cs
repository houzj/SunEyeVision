using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.UI.Controls.ParameterBinding
{
    /// <summary>
    /// DataSourcePickerControl.xaml 的交互逻辑
    /// </summary>
    public partial class DataSourcePickerControl : UserControl
    {
        public DataSourcePickerControl()
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
                typeof(ViewModels.DataSourcePickerViewModel),
                typeof(DataSourcePickerControl),
                new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// ViewModel
        /// </summary>
        public ViewModels.DataSourcePickerViewModel? ViewModel
        {
            get => (ViewModels.DataSourcePickerViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        #endregion

        #region 私有方法

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataSourcePickerControl control)
            {
                control.DataContext = e.NewValue;

                // 订阅事件
                if (e.OldValue is ViewModels.DataSourcePickerViewModel oldViewModel)
                {
                    oldViewModel.SelectionConfirmed -= control.OnSelectionConfirmed;
                    oldViewModel.Cancelled -= control.OnCancelled;
                }

                if (e.NewValue is ViewModels.DataSourcePickerViewModel newViewModel)
                {
                    newViewModel.SelectionConfirmed += control.OnSelectionConfirmed;
                    newViewModel.Cancelled += control.OnCancelled;
                }
            }
        }

        private void OnSelectionConfirmed(object? sender, ViewModels.DataSourceSelectedEventArgs e)
        {
            // 可以在这里处理选择确认后的逻辑
        }

        private void OnCancelled(object? sender, System.EventArgs e)
        {
            // 可以在这里处理取消后的逻辑
        }

        /// <summary>
        /// 处理数据源项点击
        /// </summary>
        private void OnDataSourceClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is AvailableDataSource dataSource)
            {
                ViewModel!.SelectedDataSource = dataSource;
            }
        }

        #endregion
    }
}
