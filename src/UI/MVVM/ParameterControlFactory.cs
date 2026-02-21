using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.UI.MVVM
{
    /// <summary>
    /// 参数控件工厂 - 根据ParameterMetadata自动生成对应的UI控件
    /// </summary>
    public static class ParameterControlFactory
    {
        /// <summary>
        /// 根据参数类型生成对应的控件
        /// </summary>
        public static FrameworkElement CreateControl(ParameterMetadata param, object value, Action<object> onValueChanged)
        {
            var stackPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            // 标签
            var label = new TextBlock 
            { 
                Text = param.DisplayName ?? param.Name, 
                FontSize = 11, 
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(102, 102, 102)),
                Margin = new Thickness(0, 0, 0, 4) 
            };
            stackPanel.Children.Add(label);

            FrameworkElement control = param.Type switch
            {
                ParameterType.String => CreateTextBox(value, onValueChanged, param.Required),
                ParameterType.Int => CreateIntControl(value, onValueChanged, Convert.ToDouble(param.MinValue), Convert.ToDouble(param.MaxValue)),
                ParameterType.Double => CreateDoubleControl(value, onValueChanged, Convert.ToDouble(param.MinValue), Convert.ToDouble(param.MaxValue)),
                ParameterType.Bool => CreateCheckBox(value, onValueChanged, param.DisplayName ?? param.Name),
                ParameterType.Enum => CreateComboBox(value, onValueChanged, param.Options),
                _ => CreateTextBox(value, onValueChanged, param.Required)
            };

            stackPanel.Children.Add(control);
            return stackPanel;
        }

        private static TextBox CreateTextBox(object value, Action<object> onChanged, bool required)
        {
            var textBox = new TextBox
            {
                Text = value?.ToString() ?? "",
                Padding = new Thickness(6),
                FontSize = 11,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(221, 221, 221)),
                BorderThickness = new Thickness(1)
            };
            
            if (required)
            {
                textBox.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 255, 240));
            }
            
            textBox.TextChanged += (s, e) => onChanged(textBox.Text);
            return textBox;
        }

        private static StackPanel CreateIntControl(object value, Action<object> onChanged, double min, double max)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var intValue = value is int i ? i : 0;

            var textBox = new TextBox
            {
                Text = intValue.ToString(),
                Width = 80,
                Padding = new Thickness(6),
                FontSize = 11,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(221, 221, 221)),
                BorderThickness = new Thickness(1)
            };
            textBox.TextChanged += (s, e) => 
            {
                if (int.TryParse(textBox.Text, out int val))
                    onChanged(val);
            };

            var slider = new Slider
            {
                Value = intValue,
                Minimum = min == 0 ? 1 : min,
                Maximum = max == 0 ? 100 : max,
                Width = 100,
                Margin = new Thickness(8, 0, 0, 0),
                TickFrequency = 1,
                IsSnapToTickEnabled = true
            };
            slider.ValueChanged += (s, e) => 
            {
                textBox.Text = ((int)slider.Value).ToString();
                onChanged((int)slider.Value);
            };

            var valueText = new TextBlock
            {
                Text = intValue.ToString(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 0, 0, 0),
                Width = 40,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(slider);
            stackPanel.Children.Add(valueText);
            return stackPanel;
        }

        private static ComboBox CreateComboBox(object value, Action<object> onChanged, object[] options)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = options,
                SelectedValue = value,
                Padding = new Thickness(6),
                FontSize = 11,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(221, 221, 221)),
                BorderThickness = new Thickness(1)
            };
            
            if (value != null)
            {
                comboBox.SelectedValue = value;
            }
            
            comboBox.SelectionChanged += (s, e) => onChanged(comboBox.SelectedValue ?? comboBox.SelectedItem);
            return comboBox;
        }

        private static CheckBox CreateCheckBox(object value, Action<object> onChanged, string label)
        {
            var checkBox = new CheckBox
            {
                IsChecked = value is bool b ? b : false,
                Content = label,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.Checked += (s, e) => onChanged(true);
            checkBox.Unchecked += (s, e) => onChanged(false);
            return checkBox;
        }

        private static StackPanel CreateDoubleControl(object value, Action<object> onChanged, double min, double max)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var doubleValue = value is double d ? d : 0.0;

            var textBox = new TextBox
            {
                Text = doubleValue.ToString("F2"),
                Width = 80,
                Padding = new Thickness(6),
                FontSize = 11,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(221, 221, 221)),
                BorderThickness = new Thickness(1)
            };
            textBox.TextChanged += (s, e) => 
            {
                if (double.TryParse(textBox.Text, out double val))
                    onChanged(val);
            };

            var slider = new Slider
            {
                Value = doubleValue,
                Minimum = min == 0 ? 0.1 : min,
                Maximum = max == 0 ? 10.0 : max,
                Width = 100,
                Margin = new Thickness(8, 0, 0, 0),
                TickFrequency = 0.1
            };
            slider.ValueChanged += (s, e) => 
            {
                textBox.Text = slider.Value.ToString("F2");
                onChanged(slider.Value);
            };

            var valueText = new TextBlock
            {
                Text = doubleValue.ToString("F2"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 0, 0, 0),
                Width = 50,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(slider);
            stackPanel.Children.Add(valueText);
            return stackPanel;
        }
    }
}
