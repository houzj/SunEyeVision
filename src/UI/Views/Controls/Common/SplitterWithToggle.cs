using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SunEyeVision.UI.Views.Controls.Common
{
    /// <summary>
    /// 带有折叠/展开按钮的GridSplitter
    /// </summary>
    public class SplitterWithToggle : GridSplitter
    {
        private Button? _toggleButton;
        private string? _name;
        private TextBlock? _arrowTextBlock;

        public static readonly DependencyProperty ToggleDirectionProperty =
            DependencyProperty.Register("ToggleDirection", typeof(ToggleDirectionType), typeof(SplitterWithToggle),
                new PropertyMetadata(ToggleDirectionType.Left, OnToggleDirectionChanged));

        public ToggleDirectionType ToggleDirection
        {
            get => (ToggleDirectionType)GetValue(ToggleDirectionProperty);
            set => SetValue(ToggleDirectionProperty, value);
        }

        public event EventHandler? ToggleClicked;

        public SplitterWithToggle()
        {
            DefaultStyleKey = typeof(SplitterWithToggle);
            Loaded += (s, e) => _name = Name ?? "UnNamed";
        }

        private static void OnToggleDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SplitterWithToggle splitter)
            {
                splitter.UpdateArrowText();
            }
        }

        private void UpdateArrowText()
        {
            if (_arrowTextBlock == null)
                return;

            var arrow = ToggleDirection switch
            {
                ToggleDirectionType.Left => "◀",
                ToggleDirectionType.Right => "�?,
                ToggleDirectionType.Up => "�?,
                ToggleDirectionType.Down => "�?,
                _ => ""
            };

            _arrowTextBlock.Text = arrow;
        }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 先移除旧按钮的事�?
        if (_toggleButton != null)
        {
            _toggleButton.Click -= OnToggleButtonClick;
        }

        _toggleButton = GetTemplateChild("PART_ToggleButton") as Button;

        // 绑定新按钮的事件
        if (_toggleButton != null)
        {
            _toggleButton.Click += OnToggleButtonClick;

            // 创建并设置箭头文�?
            _arrowTextBlock = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _toggleButton.Content = _arrowTextBlock;
            UpdateArrowText();
        }
    }

        private void OnToggleButtonClick(object? sender, RoutedEventArgs e)
        {
            ToggleClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum ToggleDirectionType
    {
        Left,
        Right,
        Up,
        Down
    }
}
