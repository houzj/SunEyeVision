using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Shared.Controls.Common
{
    /// <summary>
    /// 状态指示器
    /// 用于显示运行状�?
    /// </summary>
    public partial class StatusIndicator : UserControl
    {
        public StatusIndicator()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 状态文�?
        /// </summary>
        public string Status
        {
            get { return (string)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register("Status", typeof(string), typeof(StatusIndicator),
                new PropertyMetadata("Ready", OnStatusChanged));

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StatusIndicator)d;
            control.StatusText.Text = e.NewValue?.ToString() ?? "Ready";
        }

        /// <summary>
        /// 是否运行�?
        /// </summary>
        public bool IsRunning
        {
            get { return (bool)GetValue(IsRunningProperty); }
            set { SetValue(IsRunningProperty, value); }
        }

        public static readonly DependencyProperty IsRunningProperty =
            DependencyProperty.Register("IsRunning", typeof(bool), typeof(StatusIndicator),
                new PropertyMetadata(false, OnIsRunningChanged));

        private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StatusIndicator)d;
            var isRunning = (bool)e.NewValue;
            control.IndicatorEllipse.Fill = isRunning ? 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0)) :
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(128, 128, 128));
        }
    }
}
