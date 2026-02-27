using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 参数滑块控件 - 带标签和数值显示的滑块
    /// </summary>
    /// <remarks>
    /// 用于数值参数配置，提供滑动和输入两种方式。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ParamSlider Label="阈值" 
    ///     Value="{Binding Threshold}" 
    ///     Minimum="0" Maximum="255" /&gt;
    /// </code>
    /// </remarks>
    public class ParamSlider : Control
    {
        static ParamSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ParamSlider),
                new FrameworkPropertyMetadata(typeof(ParamSlider)));
        }

        #region 依赖属性

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ParamSlider),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ParamSlider),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ParamSlider),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ParamSlider),
                new PropertyMetadata(100.0));

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(ParamSlider),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(ParamSlider),
                new PropertyMetadata(10.0));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register(nameof(TickFrequency), typeof(double), typeof(ParamSlider),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty IsSnapToTickEnabledProperty =
            DependencyProperty.Register(nameof(IsSnapToTickEnabled), typeof(bool), typeof(ParamSlider),
                new PropertyMetadata(false));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(ParamSlider),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowTextBoxProperty =
            DependencyProperty.Register(nameof(ShowTextBox), typeof(bool), typeof(ParamSlider),
                new PropertyMetadata(true));

        #endregion

        #region 属性

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double SmallChange
        {
            get => (double)GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public double LargeChange
        {
            get => (double)GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        public double TickFrequency
        {
            get => (double)GetValue(TickFrequencyProperty);
            set => SetValue(TickFrequencyProperty, value);
        }

        public bool IsSnapToTickEnabled
        {
            get => (bool)GetValue(IsSnapToTickEnabledProperty);
            set => SetValue(IsSnapToTickEnabledProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        public bool ShowTextBox
        {
            get => (bool)GetValue(ShowTextBoxProperty);
            set => SetValue(ShowTextBoxProperty, value);
        }

        #endregion
    }
}
