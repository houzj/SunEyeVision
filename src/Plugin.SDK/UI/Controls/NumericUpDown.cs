using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 数值增减控件 - 类似WinForms NumericUpDown
    /// </summary>
    /// <remarks>
    /// 支持鼠标滚轮连续增减、键盘导航、按钮点击增减。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;!-- 绑定 double 类型 --&gt;
    /// &lt;controls:NumericUpDown Value="{Binding Price, Mode=TwoWay}"
    ///     DecimalPlaces="2" Minimum="0" Maximum="100" /&gt;
    /// 
    /// &lt;!-- 绑定 int 类型 --&gt;
    /// &lt;controls:NumericUpDown IntValue="{Binding Count, Mode=TwoWay}"
    ///     Minimum="0" Maximum="100" /&gt;
    /// </code>
    /// 
    /// 注意：只绑定 Value 或 IntValue 其中一个属性，不要同时绑定两个。
    /// </remarks>
    [TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_IncreaseButton, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PART_DecreaseButton, Type = typeof(RepeatButton))]
    public class NumericUpDown : Control
    {
        private const string PART_TextBox = "PART_TextBox";
        private const string PART_IncreaseButton = "PART_IncreaseButton";
        private const string PART_DecreaseButton = "PART_DecreaseButton";

        private TextBox _textBox;
        private RepeatButton _increaseButton;
        private RepeatButton _decreaseButton;
        private bool _isUpdatingText;
        private bool _isSyncingValues; // 防止 Value 和 IntValue 循环更新

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown),
                new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        #region 依赖属性

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged, CoerceValue));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericUpDown),
                new PropertyMetadata(0.0, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericUpDown),
                new PropertyMetadata(100.0, OnRangeChanged));

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NumericUpDown),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(NumericUpDown),
                new PropertyMetadata(10.0));

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(0, OnDecimalPlacesChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NumericUpDown),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ShowUpDownButtonsProperty =
            DependencyProperty.Register(nameof(ShowUpDownButtons), typeof(bool), typeof(NumericUpDown),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IntValueProperty =
            DependencyProperty.Register(nameof(IntValue), typeof(int), typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIntValueChanged, CoerceIntValue));

        #endregion

        #region 路由事件

        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>), typeof(NumericUpDown));

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        #endregion

        #region 属性

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

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool ShowUpDownButtons
        {
            get => (bool)GetValue(ShowUpDownButtonsProperty);
            set => SetValue(ShowUpDownButtonsProperty, value);
        }

        /// <summary>
        /// 整数值属性，方便绑定 int 类型。
        /// 注意：只绑定 Value 或 IntValue 其中一个，不要同时绑定两个。
        /// </summary>
        public int IntValue
        {
            get => (int)GetValue(IntValueProperty);
            set => SetValue(IntValueProperty, value);
        }

        #endregion

        #region 重写方法

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 取消旧事件订阅
            if (_textBox != null)
            {
                _textBox.PreviewKeyDown -= OnTextBoxKeyDown;
                _textBox.PreviewMouseWheel -= OnTextBoxMouseWheel;
                _textBox.LostFocus -= OnTextBoxLostFocus;
            }
            if (_increaseButton != null)
                _increaseButton.Click -= OnIncreaseButtonClick;
            if (_decreaseButton != null)
                _decreaseButton.Click -= OnDecreaseButtonClick;

            // 获取模板部件 (may be null before template is applied)
            _textBox = GetTemplateChild(PART_TextBox) as TextBox;
            _increaseButton = GetTemplateChild(PART_IncreaseButton) as RepeatButton;
            _decreaseButton = GetTemplateChild(PART_DecreaseButton) as RepeatButton;

            // 订阅新事件
            if (_textBox != null)
            {
                _textBox.PreviewKeyDown += OnTextBoxKeyDown;
                _textBox.PreviewMouseWheel += OnTextBoxMouseWheel;
                _textBox.LostFocus += OnTextBoxLostFocus;
                UpdateText();
            }
            if (_increaseButton != null)
                _increaseButton.Click += OnIncreaseButtonClick;
            if (_decreaseButton != null)
                _decreaseButton.Click += OnDecreaseButtonClick;
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (!IsReadOnly && IsFocused)
            {
                ChangeValue(e.Delta > 0 ? SmallChange : -SmallChange);
                e.Handled = true;
            }
            base.OnPreviewMouseWheel(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!IsReadOnly)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        ChangeValue(SmallChange);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        ChangeValue(-SmallChange);
                        e.Handled = true;
                        break;
                    case Key.PageUp:
                        ChangeValue(LargeChange);
                        e.Handled = true;
                        break;
                    case Key.PageDown:
                        ChangeValue(-LargeChange);
                        e.Handled = true;
                        break;
                    case Key.Home:
                        Value = Minimum;
                        e.Handled = true;
                        break;
                    case Key.End:
                        Value = Maximum;
                        e.Handled = true;
                        break;
                }
            }
            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region 私有方法

        private void ChangeValue(double delta)
        {
            var newValue = Math.Round(Value + delta, DecimalPlaces);
            Value = Clamp(newValue);
        }

        private double Clamp(double value)
        {
            return Math.Max(Minimum, Math.Min(Maximum, value));
        }

        private void UpdateText()
        {
            if (_textBox != null && !_isUpdatingText)
            {
                _isUpdatingText = true;
                try
                {
                    string format = DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "F0";
                    _textBox.Text = Value.ToString(format);
                }
                finally
                {
                    _isUpdatingText = false;
                }
            }
        }

        private void ParseAndApplyText()
        {
            if (_textBox == null || _isUpdatingText) return;

            if (double.TryParse(_textBox.Text, out var newValue))
            {
                newValue = Math.Round(newValue, DecimalPlaces);
                Value = Clamp(newValue);
            }
            UpdateText();
        }

        #endregion

        #region 回调方法

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.UpdateText();
            control.RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, ValueChangedEvent));

            // 同步 IntValue
            if (!control._isSyncingValues)
            {
                control._isSyncingValues = true;
                try
                {
                    control.IntValue = (int)Math.Round((double)e.NewValue);
                }
                finally
                {
                    control._isSyncingValues = false;
                }
            }
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.CoerceValue(ValueProperty);
        }

        private static void OnDecimalPlacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;
            control.UpdateText();
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var control = (NumericUpDown)d;
            var value = (double)baseValue;
            return control.Clamp(value);
        }

        private static void OnIntValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericUpDown)d;

            // 同步 Value
            if (!control._isSyncingValues)
            {
                control._isSyncingValues = true;
                try
                {
                    control.Value = (int)e.NewValue;
                }
                finally
                {
                    control._isSyncingValues = false;
                }
            }
        }

        private static object CoerceIntValue(DependencyObject d, object baseValue)
        {
            var control = (NumericUpDown)d;
            var value = (int)baseValue;
            var clamped = control.Clamp(value);
            return (int)Math.Round(clamped);
        }

        #endregion

        #region 事件处理

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ParseAndApplyText();
                e.Handled = true;
            }
        }

        private void OnTextBoxMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsReadOnly)
            {
                ChangeValue(e.Delta > 0 ? SmallChange : -SmallChange);
                e.Handled = true;
            }
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            ParseAndApplyText();
        }

        private void OnIncreaseButtonClick(object sender, RoutedEventArgs e)
        {
            if (!IsReadOnly)
            {
                ChangeValue(SmallChange);
            }
        }

        private void OnDecreaseButtonClick(object sender, RoutedEventArgs e)
        {
            if (!IsReadOnly)
            {
                ChangeValue(-SmallChange);
            }
        }

        #endregion
    }
}
