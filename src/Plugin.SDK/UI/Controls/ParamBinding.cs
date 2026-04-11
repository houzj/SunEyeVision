using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 参数绑定控件 - 支持常量输入和参数绑定切换
    /// </summary>
    public class ParamBinding : Control
    {
        #region 依赖属性

        public static readonly DependencyProperty ParameterNameProperty =
            DependencyProperty.Register(
                nameof(ParameterName),
                typeof(string),
                typeof(ParamBinding),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsBoundProperty =
            DependencyProperty.Register(
                nameof(IsBound),
                typeof(bool),
                typeof(ParamBinding),
                new FrameworkPropertyMetadata(false, OnIsBoundChanged));

        public static readonly DependencyProperty BindingSourceDisplayProperty =
            DependencyProperty.Register(
                nameof(BindingSourceDisplay),
                typeof(string),
                typeof(ParamBinding),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(ParamBinding),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty AvailableBindingsProperty =
            DependencyProperty.Register(
                nameof(AvailableBindings),
                typeof(List<string>),
                typeof(ParamBinding),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IconSymbolProperty =
            DependencyProperty.Register(
                nameof(IconSymbol),
                typeof(string),
                typeof(ParamBinding),
                new PropertyMetadata("🔗"));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ParamBinding),
                new PropertyMetadata(new CornerRadius(4)));

        #endregion

        #region 属性封装

        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName
        {
            get => (string)GetValue(ParameterNameProperty);
            set => SetValue(ParameterNameProperty, value);
        }

        /// <summary>
        /// 是否已绑定
        /// </summary>
        public bool IsBound
        {
            get => (bool)GetValue(IsBoundProperty);
            set => SetValue(IsBoundProperty, value);
        }

        /// <summary>
        /// 绑定源显示文本
        /// </summary>
        public string BindingSourceDisplay
        {
            get => (string)GetValue(BindingSourceDisplayProperty);
            set => SetValue(BindingSourceDisplayProperty, value);
        }

        /// <summary>
        /// 参数值（常量模式）
        /// </summary>
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// 可用的绑定源列表
        /// </summary>
        public List<string>? AvailableBindings
        {
            get => (List<string>?)GetValue(AvailableBindingsProperty);
            set => SetValue(AvailableBindingsProperty, value);
        }

        /// <summary>
        /// 图标符号
        /// </summary>
        public string IconSymbol
        {
            get => (string)GetValue(IconSymbolProperty);
            set => SetValue(IconSymbolProperty, value);
        }

        /// <summary>
        /// 圆角半径
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region 事件

        public static readonly RoutedEvent BindingStateChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(BindingStateChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ParamBinding));

        /// <summary>
        /// 绑定状态变更事件
        /// </summary>
        public event RoutedEventHandler BindingStateChanged
        {
            add => AddHandler(BindingStateChangedEvent, value);
            remove => RemoveHandler(BindingStateChangedEvent, value);
        }

        #endregion

        #region 控件引用

        private TextBox? _textBox;
        private Button? _bindingButton;
        private Popup? _bindingPopup;
        private TreeView? _bindingTreeView;

        #endregion

        #region 构造函数

        static ParamBinding()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ParamBinding),
                new FrameworkPropertyMetadata(typeof(ParamBinding)));
        }

        #endregion

        #region 回调

        private static void OnIsBoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ParamBinding control)
            {
                // 根据绑定状态更新图标
                control.IconSymbol = (bool)e.NewValue ? "🔗" : "⚡";
            }
        }

        #endregion

        #region 模板应用

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _textBox = GetTemplateChild("PART_TextBox") as TextBox;
            _bindingButton = GetTemplateChild("PART_BindingButton") as Button;
            _bindingPopup = GetTemplateChild("PART_BindingPopup") as Popup;
            _bindingTreeView = GetTemplateChild("PART_BindingTreeView") as TreeView;

            // 绑定事件
            if (_bindingButton != null)
            {
                _bindingButton.Click += OnBindingButtonClick;
            }

            if (_bindingTreeView != null)
            {
                _bindingTreeView.SelectedItemChanged += OnBindingTreeViewSelectedItemChanged;
            }
        }

        #endregion

        #region 事件处理

        private void OnBindingButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsBound)
            {
                // 解除绑定
                IsBound = false;
                BindingSourceDisplay = string.Empty;
            }
            else
            {
                // 显示绑定选择器
                if (_bindingPopup != null)
                {
                    _bindingPopup.IsOpen = true;
                }
            }

            RaiseEvent(new RoutedEventArgs(BindingStateChangedEvent));
        }

        private void OnBindingTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is string selectedItem && !string.IsNullOrEmpty(selectedItem))
            {
                // 设置绑定
                BindingSourceDisplay = selectedItem;
                IsBound = true;

                // 关闭 Popup
                if (_bindingPopup != null)
                {
                    _bindingPopup.IsOpen = false;
                }

                RaiseEvent(new RoutedEventArgs(BindingStateChangedEvent));
            }
        }

        #endregion
    }
}
