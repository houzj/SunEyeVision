using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 支持常量/绑定模式切换的多类型参数控件（核心控件，不包含标签和外框）
    /// </summary>
    /// <remarks>
    /// 核心概念：
    /// 1. 绑定类型（BindingType）：回答"值从哪里来" - Constant 或 Binding
    /// 2. 数据类型（DataType）：回答"值是什么类型" - Int, Double, String, Bool 等
    /// 3. 绑定类型与数据类型正交，任何数据类型都可以是常量或绑定
    /// 
    /// 注意：此控件只包含核心功能，标签和布局由工具层UI负责。
    /// </remarks>
    public class BindableParameter : Control
    {
        #region 依赖属性


        // IntValue 属性变化回调
        private static void OnIntValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (BindableParameter)d;
            PluginLogger.Info($"[IntValue] {e.OldValue} → {e.NewValue}", "BindableParameter");
        }

        // ===== 参数名（用于参数绑定） =====
        public static readonly DependencyProperty ParameterNameProperty =
            DependencyProperty.Register(nameof(ParameterName), typeof(string), typeof(BindableParameter),
                new PropertyMetadata(string.Empty));

        // ===== 数据类型 =====
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(nameof(DataType), typeof(ParamDataType), typeof(BindableParameter),
                new PropertyMetadata(ParamDataType.Double, OnDataTypeChanged));

        // ===== 绑定类型 =====
        public static readonly DependencyProperty BindingTypeProperty =
            DependencyProperty.Register(nameof(BindingType), typeof(BindingType), typeof(BindableParameter),
                new FrameworkPropertyMetadata(BindingType.Constant, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== 绑定源 =====
        public static readonly DependencyProperty BindingSourceProperty =
            DependencyProperty.Register(nameof(BindingSource), typeof(string), typeof(BindableParameter),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== Int 值 =====
        public static readonly DependencyProperty IntValueProperty =
            DependencyProperty.Register(nameof(IntValue), typeof(int), typeof(BindableParameter),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIntValuePropertyChanged));

        public static readonly DependencyProperty IntMinimumProperty =
            DependencyProperty.Register(nameof(IntMinimum), typeof(int), typeof(BindableParameter),
                new PropertyMetadata(int.MinValue));

        public static readonly DependencyProperty IntMaximumProperty =
            DependencyProperty.Register(nameof(IntMaximum), typeof(int), typeof(BindableParameter),
                new PropertyMetadata(int.MaxValue));

        // ===== Double 值 =====
        public static readonly DependencyProperty DoubleValueProperty =
            DependencyProperty.Register(nameof(DoubleValue), typeof(double), typeof(BindableParameter),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DoubleMinimumProperty =
            DependencyProperty.Register(nameof(DoubleMinimum), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(double.MinValue));

        public static readonly DependencyProperty DoubleMaximumProperty =
            DependencyProperty.Register(nameof(DoubleMaximum), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(BindableParameter),
                new PropertyMetadata(10.0));

        // ===== String 值 =====
        public static readonly DependencyProperty StringValueProperty =
            DependencyProperty.Register(nameof(StringValue), typeof(string), typeof(BindableParameter),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== Bool 值 =====
        public static readonly DependencyProperty BoolValueProperty =
            DependencyProperty.Register(nameof(BoolValue), typeof(bool), typeof(BindableParameter),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        // ===== 显示选项 =====
        public static readonly DependencyProperty ShowSliderProperty =
            DependencyProperty.Register(nameof(ShowSlider), typeof(bool), typeof(BindableParameter),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(BindableParameter),
                new PropertyMetadata(false));

        public static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(BindableParameter),
                new PropertyMetadata(2));

        // ===== 绑定配置 =====
        public static readonly DependencyProperty AvailableBindingsProperty =
            DependencyProperty.Register(nameof(AvailableBindings), typeof(System.Collections.Generic.List<string>), typeof(BindableParameter),
                new PropertyMetadata(null));

        // ===== 转换表达式 =====
        public static readonly DependencyProperty TransformExpressionProperty =
            DependencyProperty.Register(nameof(TransformExpression), typeof(string), typeof(BindableParameter),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region 属性封装

        /// <summary>
        /// 参数名（用于参数绑定，对应 ToolParameters 中的属性名）
        /// </summary>
        public string ParameterName
        {
            get => (string)GetValue(ParameterNameProperty);
            set => SetValue(ParameterNameProperty, value);
        }

        public ParamDataType DataType
        {
            get => (ParamDataType)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        public BindingType BindingType
        {
            get => (BindingType)GetValue(BindingTypeProperty);
            set => SetValue(BindingTypeProperty, value);
        }

        public string BindingSource
        {
            get => (string)GetValue(BindingSourceProperty);
            set => SetValue(BindingSourceProperty, value);
        }

        // Int 值属性
        public int IntValue
        {
            get => (int)GetValue(IntValueProperty);
            set => SetValue(IntValueProperty, value);
        }

        public int IntMinimum
        {
            get => (int)GetValue(IntMinimumProperty);
            set => SetValue(IntMinimumProperty, value);
        }

        public int IntMaximum
        {
            get => (int)GetValue(IntMaximumProperty);
            set => SetValue(IntMaximumProperty, value);
        }

        // Double 值属性
        public double DoubleValue
        {
            get => (double)GetValue(DoubleValueProperty);
            set => SetValue(DoubleValueProperty, value);
        }

        public double DoubleMinimum
        {
            get => (double)GetValue(DoubleMinimumProperty);
            set => SetValue(DoubleMinimumProperty, value);
        }

        public double DoubleMaximum
        {
            get => (double)GetValue(DoubleMaximumProperty);
            set => SetValue(DoubleMaximumProperty, value);
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

        // String 值属性
        public string StringValue
        {
            get => (string)GetValue(StringValueProperty);
            set => SetValue(StringValueProperty, value);
        }

        // Bool 值属性
        public bool BoolValue
        {
            get => (bool)GetValue(BoolValueProperty);
            set => SetValue(BoolValueProperty, value);
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

        public string TransformExpression
        {
            get => (string)GetValue(TransformExpressionProperty);
            set => SetValue(TransformExpressionProperty, value);
        }

        #endregion

        #region 控件引用

        private NumericUpDown _numericEditor = null!;
        private TextBox _stringTextBox = null!;
        private Button _bindingButton = null!;
        private CheckBox _boolCheckBox = null!;

        #endregion

        #region 事件

        /// <summary>
        /// 值变更事件
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        /// <summary>
        /// 绑定类型变更事件
        /// </summary>
        public static readonly RoutedEvent BindingTypeChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(BindingTypeChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(BindableParameter));

        public event RoutedEventHandler BindingTypeChanged
        {
            add => AddHandler(BindingTypeChangedEvent, value);
            remove => RemoveHandler(BindingTypeChangedEvent, value);
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
        }

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableParameter control)
            {
                control.UpdateVisualState();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 获取模板中的控件
            _numericEditor = GetTemplateChild("PART_NumericEditor") as NumericUpDown;
            _stringTextBox = GetTemplateChild("PART_StringTextBox") as TextBox;
            _bindingButton = GetTemplateChild("PART_BindingButton") as Button ??
                throw new InvalidOperationException("PART_BindingButton not found");
            _boolCheckBox = GetTemplateChild("PART_BoolCheckBox") as CheckBox;

            // 绑定事件
            if (_numericEditor != null)
            {
                _numericEditor.ValueChanged += (s, e) => RaiseValueChanged();
            }

            if (_stringTextBox != null)
            {
                _stringTextBox.TextChanged += (s, e) =>
                {
                    if (BindingType == BindingType.Constant)
                        RaiseValueChanged();
                };
            }

            if (_boolCheckBox != null)
            {
                _boolCheckBox.Checked += (s, e) => RaiseValueChanged();
                _boolCheckBox.Unchecked += (s, e) => RaiseValueChanged();
            }

            _bindingButton.Click += OnBindingButtonClick;

            // 初始化状态
            UpdateVisualState();
        }

        private void RaiseValueChanged()
        {
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void OnBindingButtonClick(object sender, RoutedEventArgs e)
        {
            if (BindingType == BindingType.Constant)
            {
                ShowBindingSelector();
            }
            else
            {
                BindingType = BindingType.Constant;
                BindingSource = string.Empty;
                UpdateVisualState();
            }

            RaiseEvent(new RoutedEventArgs(BindingTypeChangedEvent));
        }

        private void ShowBindingSelector()
        {
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
                        BindingType = BindingType.Binding;
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
            if (_bindingButton == null)
                return;

            if (BindingType == BindingType.Binding)
            {
                // 绑定模式：禁用值编辑控件
                if (_numericEditor != null)
                {
                    _numericEditor.IsEnabled = false;
                }
                if (_stringTextBox != null)
                {
                    _stringTextBox.IsEnabled = false;
                    _stringTextBox.Text = BindingSource;
                }
                if (_boolCheckBox != null) _boolCheckBox.IsEnabled = false;
                IsExpanded = false;

                _bindingButton.Content = "🔗";
                _bindingButton.ToolTip = $"绑定源: {BindingSource}\n点击解除绑定";
            }
            else
            {
                // 常量模式：启用值编辑控件
                if (_numericEditor != null)
                {
                    _numericEditor.IsEnabled = true;
                }
                if (_stringTextBox != null)
                {
                    _stringTextBox.IsEnabled = true;
                }
                if (_boolCheckBox != null) _boolCheckBox.IsEnabled = true;

                _bindingButton.Content = "⚡";
                _bindingButton.ToolTip = "点击绑定到其他节点输出";
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == BindingTypeProperty || e.Property == BindingSourceProperty ||
                e.Property == IsExpandedProperty || e.Property == DataTypeProperty)
            {
                UpdateVisualState();
            }
        }

        /// <summary>
        /// 获取当前值（根据数据类型）
        /// </summary>
        public object? GetValue()
        {
            return DataType switch
            {
                ParamDataType.Int => IntValue,
                ParamDataType.Double => DoubleValue,
                ParamDataType.String => StringValue,
                ParamDataType.Bool => BoolValue,
                _ => null
            };
        }

        /// <summary>
        /// 设置当前值（根据数据类型）
        /// </summary>
        public void SetValue(object? value)
        {
            if (value == null)
                return;

            switch (DataType)
            {
                case ParamDataType.Int:
                    if (value is int intVal)
                        IntValue = intVal;
                    else if (int.TryParse(value.ToString(), out int intResult))
                        IntValue = intResult;
                    break;

                case ParamDataType.Double:
                    if (value is double doubleVal)
                        DoubleValue = doubleVal;
                    else if (double.TryParse(value.ToString(), out double doubleResult))
                        DoubleValue = doubleResult;
                    break;

                case ParamDataType.String:
                    StringValue = value.ToString() ?? string.Empty;
                    break;

                case ParamDataType.Bool:
                    if (value is bool boolVal)
                        BoolValue = boolVal;
                    else if (bool.TryParse(value.ToString(), out bool boolResult))
                        BoolValue = boolResult;
                    break;
            }
        }

        /// <summary>
        /// 转换为 ParameterBinding 对象
        /// </summary>
        public ParameterBinding ToParameterBinding()
        {
            if (BindingType == BindingType.Constant)
            {
                return ParameterBinding.CreateConstant(ParameterName, GetValue());
            }
            else
            {
                var parts = BindingSource.Split('.');
                var nodeId = parts.Length > 0 ? parts[0] : string.Empty;
                var property = parts.Length > 1 ? parts[1] : BindingSource;
                return ParameterBinding.CreateBinding(ParameterName, nodeId, property, TransformExpression);
            }
        }

        /// <summary>
        /// 从 ParameterBinding 对象加载
        /// </summary>
        public void FromParameterBinding(ParameterBinding binding)
        {
            if (binding == null)
                return;

            ParameterName = binding.ParameterName;
            BindingType = binding.BindingType;
            TransformExpression = binding.TransformExpression ?? string.Empty;

            if (binding.BindingType == BindingType.Constant)
            {
                SetValue(binding.ConstantValue);
            }
            else
            {
                BindingSource = string.IsNullOrEmpty(binding.SourceProperty)
                    ? binding.SourceNodeId ?? string.Empty
                    : $"{binding.SourceNodeId}.{binding.SourceProperty}";
            }
        }
    }
}
