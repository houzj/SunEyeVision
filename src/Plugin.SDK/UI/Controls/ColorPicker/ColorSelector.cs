using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 颜色选择器控件（核心控件，不包含标签和外框）
    /// </summary>
    /// <remarks>
    /// 提供紧凑的颜色选择功能，点击弹出 ColorPicker。
    /// 
    /// 功能特性：
    /// - 紧凑的颜色预览 + RGB 文本显示
    /// - 点击切换 ColorPicker 显示/隐藏
    /// - 失焦自动关闭 ColorPicker
    /// - 双向数据绑定
    /// 
    /// 注意：此控件只包含核心功能，标签和布局由工具层UI负责。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ColorSelector SelectedColor="{Binding DisplayColor, Mode=TwoWay}" /&gt;
    /// </code>
    /// </remarks>
    [TemplatePart(Name = PART_SelectorBorder, Type = typeof(Border))]
    [TemplatePart(Name = PART_ColorPreview, Type = typeof(Border))]
    [TemplatePart(Name = PART_RgbText, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_ColorPickerPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_ColorPicker, Type = typeof(ColorPicker))]
    public class ColorSelector : Control
    {
        private const string PART_SelectorBorder = "PART_SelectorBorder";
        private const string PART_ColorPreview = "PART_ColorPreview";
        private const string PART_RgbText = "PART_RgbText";
        private const string PART_ColorPickerPopup = "PART_ColorPickerPopup";
        private const string PART_ColorPicker = "PART_ColorPicker";

        private Border _selectorBorder;
        private Border _colorPreview;
        private TextBlock _rgbText;
        private Popup _colorPickerPopup;
        private ColorPicker _colorPicker;

        static ColorSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorSelector),
                new FrameworkPropertyMetadata(typeof(ColorSelector)));
        }

        #region 依赖属性

        /// <summary>
        /// 选中的颜色（uint ARGB 格式）
        /// </summary>
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(uint), typeof(ColorSelector),
                new FrameworkPropertyMetadata(0xFFFF0000, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedColorChanged));

        /// <summary>
        /// 选中的颜色（uint ARGB 格式）
        /// </summary>
        public uint SelectedColor
        {
            get => (uint)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        #endregion

        #region 路由事件

        /// <summary>
        /// 颜色变更事件
        /// </summary>
        public static readonly RoutedEvent SelectedColorChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SelectedColorChanged), RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<uint>), typeof(ColorSelector));

        /// <summary>
        /// 颜色变更事件
        /// </summary>
        public event RoutedPropertyChangedEventHandler<uint> SelectedColorChanged
        {
            add => AddHandler(SelectedColorChangedEvent, value);
            remove => RemoveHandler(SelectedColorChangedEvent, value);
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 移除旧事件
            if (_selectorBorder != null)
            {
                _selectorBorder.MouseLeftButtonUp -= OnSelectorBorderMouseLeftButtonUp;
                _selectorBorder.LostFocus -= OnSelectorBorderLostFocus;
            }

            if (_colorPicker != null)
            {
                _colorPicker.SelectedColorChanged -= OnColorPickerSelectedColorChanged;
            }

            if (_colorPickerPopup != null)
            {
                _colorPickerPopup.Closed -= OnColorPickerPopupClosed;
            }

            // 获取模板部件
            _selectorBorder = GetTemplateChild(PART_SelectorBorder) as Border;
            _colorPreview = GetTemplateChild(PART_ColorPreview) as Border;
            _rgbText = GetTemplateChild(PART_RgbText) as TextBlock;
            _colorPickerPopup = GetTemplateChild(PART_ColorPickerPopup) as Popup;
            _colorPicker = GetTemplateChild(PART_ColorPicker) as ColorPicker;

            // 绑定新事件
            if (_selectorBorder != null)
            {
                _selectorBorder.MouseLeftButtonUp += OnSelectorBorderMouseLeftButtonUp;
                _selectorBorder.LostFocus += OnSelectorBorderLostFocus;
            }

            if (_colorPicker != null)
            {
                _colorPicker.SelectedColorChanged += OnColorPickerSelectedColorChanged;
            }

            if (_colorPickerPopup != null)
            {
                _colorPickerPopup.Closed += OnColorPickerPopupClosed;
            }

            UpdateVisualState();
        }

        /// <summary>
        /// 点击触发器 Border：切换 Popup 显示/隐藏
        /// </summary>
        private void OnSelectorBorderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_colorPickerPopup != null)
            {
                // 切换显示状态
                _colorPickerPopup.IsOpen = !_colorPickerPopup.IsOpen;

                // 如果打开，确保 Border 有焦点
                if (_colorPickerPopup.IsOpen && _selectorBorder != null)
                {
                    _selectorBorder.Focus();
                }
            }
        }

        /// <summary>
        /// 触发器 Border 失焦：关闭 Popup
        /// </summary>
        private void OnSelectorBorderLostFocus(object sender, RoutedEventArgs e)
        {
            if (_colorPickerPopup != null && _colorPickerPopup.IsOpen)
            {
                _colorPickerPopup.IsOpen = false;
            }
        }

        private void OnColorPickerSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<uint> e)
        {
            SelectedColor = e.NewValue;
        }

        private void OnColorPickerPopupClosed(object sender, EventArgs e)
        {
            // Popup 关闭时不需要额外处理
        }

        private void OnSelectedColorChanged(uint oldValue, uint newValue)
        {
            UpdateVisualState();

            var args = new RoutedPropertyChangedEventArgs<uint>(oldValue, newValue, SelectedColorChangedEvent);
            RaiseEvent(args);
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorSelector)d;
            control.OnSelectedColorChanged((uint)e.OldValue, (uint)e.NewValue);
        }

        private void UpdateVisualState()
        {
            var color = UIntToColor(SelectedColor);

            if (_colorPreview != null)
            {
                _colorPreview.Background = new SolidColorBrush(color);
            }

            if (_rgbText != null)
            {
                _rgbText.Text = $"{color.R}, {color.G}, {color.B}";
            }
        }

        #region 辅助方法

        private static Color UIntToColor(uint argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }

        #endregion
    }
}
