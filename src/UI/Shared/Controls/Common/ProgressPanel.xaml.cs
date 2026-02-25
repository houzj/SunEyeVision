using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Shared.Controls.Common
{
    /// <summary>
    /// 进度面板
    /// 用于显示任务执行进度
    /// </summary>
    public partial class ProgressPanel : UserControl
    {
        public ProgressPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 进度值（0-100）
        /// </summary>
        public double ProgressValue
        {
            get { return (double)GetValue(ProgressValueProperty); }
            set { SetValue(ProgressValueProperty, value); }
        }

        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register("ProgressValue", typeof(double), typeof(ProgressPanel),
                new PropertyMetadata(0d, OnProgressValueChanged));

        private static void OnProgressValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ProgressPanel)d;
            control.ProgressBar.Value = (double)e.NewValue;
        }

        /// <summary>
        /// 进度文本
        /// </summary>
        public string ProgressText
        {
            get { return (string)GetValue(ProgressTextProperty); }
            set { SetValue(ProgressTextProperty, value); }
        }

        public static readonly DependencyProperty ProgressTextProperty =
            DependencyProperty.Register("ProgressText", typeof(string), typeof(ProgressPanel),
                new PropertyMetadata(string.Empty, OnProgressTextChanged));

        private static void OnProgressTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ProgressPanel)d;
            control.ProgressTextBlock.Text = e.NewValue?.ToString() ?? string.Empty;
        }
    }
}
