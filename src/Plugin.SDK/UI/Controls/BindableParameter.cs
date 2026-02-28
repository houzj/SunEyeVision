using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 参数绑定模式
    /// </summary>
    public enum ParameterBindingMode
    {
        /// <summary>
        /// 常量模式 - 使用固定值
        /// </summary>
        Constant,

        /// <summary>
        /// 绑定模式 - 绑定到其他节点输出
        /// </summary>
        Binding
    }

    /// <summary>
    /// 支持常量/绑定模式切换的参数控件
    /// </summary>
    /// <remarks>
    /// 布局：
    /// 第一行：Label | TextBox | ▲▼ | BindingButton | MinValueLabel
    /// 第二行（展开时）：Slider
    /// 支持数值型参数的常量值输入和节点绑定切换
    /// </remarks>
    public class BindableParameter : Control
    {
        #region 依赖属性

        // ===== 标签属性 =====
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(BindableParameter),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MinValueLabelProperty =
            DependencyProperty.Register(nameof(MinValueLabel), typeof(string), typeof(BindableParameter),
                new PropertyMetadata(string.Empty));

        // ===== 值属性 =====
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(BindableParameter),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(100.0));

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(10.0));

        // ===== 绑定模式属性 =====
        public static readonly DependencyProperty BindingModeProperty =
            DependencyProperty.Register(nameof(BindingMode), typeof(ParameterBindingMode), typeof(BindableParameter),
                new FrameworkPropertyMetadata(ParameterBindingMode.Constant, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty BindingSourceProperty =
            DependencyProperty.Register(nameof(BindingSource), typeof(string), typeof(BindableParameter),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== 显示选项 =====
        public static readonly DependencyProperty ShowSliderProperty =
            DependencyProperty.Register(nameof(ShowSlider), typeof(bool), typeof(BindableParameter),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(BindableParameter),
                new PropertyMetadata(false));

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(BindableParameter),
                new PropertyMetadata(0));

        // ===== 绑定配置 =====
        public static readonly DependencyProperty AvailableBindingsProperty =
            DependencyProperty.Register(nameof(AvailableBindings), typeof(System.Collections.Generic.List<string>), typeof(BindableParameter),
                new PropertyMetadata(null));

        #endregion

        #region 属性封装

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string MinValueLabel
        {
            get => (string)GetValue(MinValueLabelProperty);
            set => SetValue(MinValueLabelProperty, value);
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

        public ParameterBindingMode BindingMode
        {
            get => (ParameterBindingMode)GetValue(BindingModeProperty);
            set => SetValue(BindingModeProperty, value);
        }

        public string BindingSource
        {
            get => (string)GetValue(BindingSourceProperty);
            set => SetValue(BindingSourceProperty, value);
        }

        public bool ShowSlider
        {
            get => (bool)GetValue(ShowSliderProperty);
            set => SetValue(ShowSliderProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }

        public System.Collections.Generic.List<string> AvailableBindings
        {
            get => (System.Collections.Generic.List<string>)GetValue(AvailableBindingsProperty);
            set => SetValue(AvailableBindingsProperty, value);
        }

        #endregion

        #region 控件引用

        private TextBox _valueTextBox = null!;
        private Slider _slider = null!;
        private RepeatButton _decreaseButton = null!;  // ▼ 按钮
        private RepeatButton _increaseButton = null!;  // ▲ 按钮
        private Button _bindingButton = null!;
        private Border _expandPanel = null!;
        private DispatcherTimer _focusCheckTimer = null!;

        #endregion

        #region 事件

        /// <summary>
        /// 绑定模式变更事件
        /// </summary>
        public static readonly RoutedEvent BindingModeChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(BindingModeChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler BindingModeChanged
        {
            add => AddHandler(BindingModeChangedEvent, value);
            remove => RemoveHandler(BindingModeChangedEvent, value);
        }

        /// <summary>
        /// 绑定源选择事件
        /// </summary>
        public static readonly RoutedEvent BindingSourceSelectedEvent =
            EventManager.RegisterRoutedEvent(nameof(BindingSourceSelected), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler BindingSourceSelected
        {
            add => AddHandler(BindingSourceSelectedEvent, value);
            remove => RemoveHandler(BindingSourceSelectedEvent, value);
        }

        #endregion

        static BindableParameter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BindableParameter),
                new FrameworkPropertyMetadata(typeof(BindableParameter)));
        }

        public BindableParameter()
        {
            AvailableBindings = new System.Collections.Generic.List<string>();

            // 初始化焦点检查定时器（用于延迟一帧检查焦点状态）
            _focusCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _focusCheckTimer.Tick += OnFocusCheckTimerTick;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 获取模板中的控件
            _valueTextBox = GetTemplateChild("PART_ValueTextBox") as TextBox ?? throw new InvalidOperationException("PART_ValueTextBox not found");
            _slider = GetTemplateChild("PART_Slider") as Slider ?? throw new InvalidOperationException("PART_Slider not found");
            _decreaseButton = GetTemplateChild("PART_DecreaseButton") as RepeatButton ?? throw new InvalidOperationException("PART_DecreaseButton not found");
            _increaseButton = GetTemplateChild("PART_IncreaseButton") as RepeatButton ?? throw new InvalidOperationException("PART_IncreaseButton not found");
            _bindingButton = GetTemplateChild("PART_BindingButton") as Button ?? throw new InvalidOperationException("PART_BindingButton not found");
            _expandPanel = GetTemplateChild("PART_ExpandPanel") as Border ?? throw new InvalidOperationException("PART_ExpandPanel not found");

            // 让slider和expandPanel可获取焦点，以便正确判断焦点离开
            _slider.Focusable = true;
            _expandPanel.Focusable = true;

            // 绑定事件
            _valueTextBox.TextChanged += OnValueTextBoxTextChanged;
            _valueTextBox.LostFocus += OnValueTextBoxLostFocus;
            _valueTextBox.PreviewKeyDown += OnValueTextBoxPreviewKeyDown;
            _valueTextBox.GotFocus += OnValueTextBoxGotFocus;
            _decreaseButton.Click += OnDecreaseButtonClick;
            _increaseButton.Click += OnIncreaseButtonClick;
            _bindingButton.Click += OnBindingButtonClick;

            // 订阅控件焦点离开事件
            _valueTextBox.LostFocus += OnControlLostFocus;
            _slider.LostFocus += OnControlLostFocus;
            _expandPanel.LostFocus += OnControlLostFocus;
            _increaseButton.LostFocus += OnControlLostFocus;
            _decreaseButton.LostFocus += OnControlLostFocus;

            // 初始化状态
            UpdateVisualState();
        }

        private void OnValueTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // 点击文本框时展开滑块
            if (ShowSlider && BindingMode == ParameterBindingMode.Constant)
            {
                IsExpanded = true;
            }
        }

        /// <summary>
        /// 控件失去焦点时检查是否需要收起滑块
        /// </summary>
        private void OnControlLostFocus(object sender, RoutedEventArgs e)
        {
            _focusCheckTimer.Start();
        }

        /// <summary>
        /// 定时器检查焦点状态
        /// </summary>
        private void OnFocusCheckTimerTick(object? sender, EventArgs e)
        {
            _focusCheckTimer.Stop();

            // 如果焦点已离开整个控件，且当前展开滑块，则收起
            if (!IsKeyboardFocusWithin && IsExpanded && ShowSlider && BindingMode == ParameterBindingMode.Constant)
            {
                IsExpanded = false;
            }
        }

        private void OnValueTextBoxPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (BindingMode != ParameterBindingMode.Constant)
                return;

            if (e.Key == System.Windows.Input.Key.Up)
            {
                Value = Math.Min(Maximum, Value + SmallChange);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                Value = Math.Max(Minimum, Value - SmallChange);
                e.Handled = true;
            }
        }

        private void OnValueTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(_valueTextBox.Text, out double result))
            {
                if (result >= Minimum && result <= Maximum)
                {
                    Value = result;
                }
            }
        }

        private void OnValueTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // 格式化显示值
            string format = DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "F0";
            _valueTextBox.Text = Value.ToString(format);
        }

        private void OnDecreaseButtonClick(object sender, RoutedEventArgs e)
        {
            Value = Math.Max(Minimum, Value - SmallChange);
        }

        private void OnIncreaseButtonClick(object sender, RoutedEventArgs e)
        {
            Value = Math.Min(Maximum, Value + SmallChange);
        }

        private void OnBindingButtonClick(object sender, RoutedEventArgs e)
        {
            if (BindingMode == ParameterBindingMode.Constant)
            {
                // 切换到绑定模式，显示绑定选择器
                ShowBindingSelector();
            }
            else
            {
                // 切换回常量模式
                BindingMode = ParameterBindingMode.Constant;
                BindingSource = string.Empty;
                UpdateVisualState();
            }

            RaiseEvent(new RoutedEventArgs(BindingModeChangedEvent));
        }

        private void ShowBindingSelector()
        {
            // 创建弹出菜单
            var contextMenu = new ContextMenu
            {
                PlacementTarget = _bindingButton,
                Placement = PlacementMode.Bottom,
                StaysOpen = false
            };

            if (AvailableBindings != null && AvailableBindings.Count > 0)
            {
                foreach (var binding in AvailableBindings)
                {
                    var item = new MenuItem { Header = binding };
                    item.Click += (s, e) =>
                    {
                        BindingSource = binding;
                        BindingMode = ParameterBindingMode.Binding;
                        UpdateVisualState();
                        RaiseEvent(new RoutedEventArgs(BindingSourceSelectedEvent));
                    };
                    contextMenu.Items.Add(item);
                }
            }
            else
            {
                var emptyItem = new MenuItem
                {
                    Header = "(无可用绑定源)",
                    IsEnabled = false
                };
                contextMenu.Items.Add(emptyItem);
            }

            contextMenu.IsOpen = true;
        }

        private void UpdateVisualState()
        {
            if (_valueTextBox == null || _slider == null ||
                _decreaseButton == null || _increaseButton == null || _bindingButton == null || _expandPanel == null)
                return;

            if (BindingMode == ParameterBindingMode.Binding)
            {
                // 绑定模式：禁用值编辑控件
                _valueTextBox.IsEnabled = false;
                _slider.IsEnabled = false;
                _decreaseButton.IsEnabled = false;
                _increaseButton.IsEnabled = false;
                _valueTextBox.Text = BindingSource;
                IsExpanded = false;

                // 更新按钮样式表示绑定状态
                _bindingButton.Content = "🔗";
                _bindingButton.ToolTip = $"绑定源: {BindingSource}\n点击解除绑定";
            }
            else
            {
                // 常量模式：启用值编辑控件
                _valueTextBox.IsEnabled = true;
                _slider.IsEnabled = ShowSlider;
                _decreaseButton.IsEnabled = true;
                _increaseButton.IsEnabled = true;

                string format = DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "F0";
                _valueTextBox.Text = Value.ToString(format);

                // 更新按钮样式表示常量模式
                _bindingButton.Content = "⚡";
                _bindingButton.ToolTip = "点击绑定到其他节点输出";
            }

            // 更新滑块展开状态
            _expandPanel.Visibility = (IsExpanded && ShowSlider && BindingMode == ParameterBindingMode.Constant)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == BindingModeProperty || e.Property == BindingSourceProperty || e.Property == IsExpandedProperty)
            {
                UpdateVisualState();
            }
            else if (e.Property == ValueProperty && BindingMode == ParameterBindingMode.Constant)
            {
                if (_valueTextBox != null)
                {
                    string format = DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "F0";
                    _valueTextBox.Text = Value.ToString(format);
                }
            }
        }
    }
}
