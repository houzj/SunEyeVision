using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 标签控件包装器 - 为任意控件添加标签
    /// </summary>
    /// <remarks>
    /// 用于包装自定义控件，统一添加标签样式。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:LabeledControl Label="自定义参数"&gt;
    ///     &lt;CheckBox Content="启用" IsChecked="{Binding IsEnabled}"/&gt;
    /// &lt;/controls:LabeledControl&gt;
    /// </code>
    /// </remarks>
    public class LabeledControl : HeaderedContentControl
    {
        static LabeledControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LabeledControl),
                new FrameworkPropertyMetadata(typeof(LabeledControl)));
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(LabeledControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.Register(nameof(LabelWidth), typeof(double), typeof(LabeledControl),
                new PropertyMetadata(80.0));

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(LabeledControl),
                new PropertyMetadata(Orientation.Horizontal));

        public static readonly DependencyProperty RequiredProperty =
            DependencyProperty.Register(nameof(Required), typeof(bool), typeof(LabeledControl),
                new PropertyMetadata(false));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public double LabelWidth
        {
            get => (double)GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public bool Required
        {
            get => (bool)GetValue(RequiredProperty);
            set => SetValue(RequiredProperty, value);
        }
    }
}
