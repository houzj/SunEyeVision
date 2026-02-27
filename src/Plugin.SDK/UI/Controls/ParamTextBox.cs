using System;
using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 参数文本框控件 - 带标签的文本输入
    /// </summary>
    /// <remarks>
    /// 用于字符串或可编辑数值参数配置。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ParamTextBox Label="文件路径"
    ///     Text="{Binding FilePath, UpdateSourceTrigger=PropertyChanged}" /&gt;
    /// </code>
    /// </remarks>
    public class ParamTextBox : Control
    {
        static ParamTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ParamTextBox),
                new FrameworkPropertyMetadata(typeof(ParamTextBox)));
        }

        #region 依赖属性

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ParamTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ParamTextBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(ParamTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(ParamTextBox),
                new PropertyMetadata(0));

        public static readonly DependencyProperty AcceptsReturnProperty =
            DependencyProperty.Register(nameof(AcceptsReturn), typeof(bool), typeof(ParamTextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ParamTextBox),
                new PropertyMetadata(TextWrapping.NoWrap));

        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(ParamTextBox),
                new PropertyMetadata(ScrollBarVisibility.Hidden));

        public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
            DependencyProperty.Register(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(ParamTextBox),
                new PropertyMetadata(ScrollBarVisibility.Hidden));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ParamTextBox),
                new PropertyMetadata(false));

        #endregion

        #region 属性

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public bool AcceptsReturn
        {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
            set => SetValue(VerticalScrollBarVisibilityProperty, value);
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
            set => SetValue(HorizontalScrollBarVisibilityProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion
    }
}
