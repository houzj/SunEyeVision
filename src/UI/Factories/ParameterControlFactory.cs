using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.UI.Factories
{
    /// <summary>
    /// 参数控件工厂 - 根据参数元数据自动生成对应的UI控件
    /// </summary>
    public static class ParameterControlFactory
    {
        /// <summary>
        /// 根据运行时参数元数据创建对应的UI控件（推荐）
        /// </summary>
        /// <param name="metadata">运行时参数元数据</param>
        /// <returns>生成的控件</returns>
        public static FrameworkElement CreateControlFromRuntimeMetadata(RuntimeParameterMetadata metadata)
        {
            var type = metadata.Type;
            
            // 数值类型
            if (type == typeof(int) || type == typeof(long) || 
                type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return CreateNumericControlFromRuntime(metadata);
            }

            // 布尔类型
            if (type == typeof(bool))
            {
                return CreateBoolControlFromRuntime(metadata);
            }

            // 枚举类型
            if (type.IsEnum)
            {
                return CreateEnumControlFromRuntime(metadata);
            }

            // 字符串类型
            if (type == typeof(string))
            {
                return CreateStringControlFromRuntime(metadata);
            }

            // 其他类型使用默认控件
            return CreateDefaultControlFromRuntime(metadata);
        }

        #region RuntimeParameterMetadata 控件创建

        private static FrameworkElement CreateNumericControlFromRuntime(RuntimeParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var textBox = new TextBox
            {
                Width = 80,
                Text = metadata.Value?.ToString() ?? "0",
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // 如果有范围限制，添加滑块
            if (metadata.Min.HasValue && metadata.Max.HasValue)
            {
                var slider = new Slider
                {
                    Width = 120,
                    Minimum = metadata.Min.Value,
                    Maximum = metadata.Max.Value,
                    Value = metadata.Value != null ? System.Convert.ToDouble(metadata.Value) : 0,
                    VerticalAlignment = VerticalAlignment.Center
                };

                slider.ValueChanged += (s, e) =>
                {
                    textBox.Text = e.NewValue.ToString("0.##");
                };
                textBox.TextChanged += (s, e) =>
                {
                    if (double.TryParse(textBox.Text, out var value))
                    {
                        slider.Value = value;
                    }
                };

                panel.Children.Add(slider);
            }

            panel.Children.Add(textBox);
            return panel;
        }

        private static Control CreateBoolControlFromRuntime(RuntimeParameterMetadata metadata)
        {
            var checkBox = new CheckBox
            {
                IsChecked = metadata.Value is bool b ? b : false,
                VerticalAlignment = VerticalAlignment.Center
            };
            return checkBox;
        }

        private static FrameworkElement CreateEnumControlFromRuntime(RuntimeParameterMetadata metadata)
        {
            var comboBox = new ComboBox
            {
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };

            var enumType = metadata.Type;
            foreach (var value in System.Enum.GetValues(enumType))
            {
                comboBox.Items.Add(value?.ToString() ?? string.Empty);
            }

            if (metadata.Value != null)
            {
                comboBox.SelectedItem = metadata.Value.ToString();
            }
            else if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }

            return comboBox;
        }

        private static FrameworkElement CreateStringControlFromRuntime(RuntimeParameterMetadata metadata)
        {
            if (metadata.Description?.Contains("\n") == true || metadata.Description?.Length > 50 == true)
            {
                var textBox = new TextBox
                {
                    Width = 200,
                    Height = 60,
                    Text = metadata.Value?.ToString() ?? string.Empty,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalAlignment = VerticalAlignment.Center
                };
                return textBox;
            }

            return new TextBox
            {
                Width = 200,
                Text = metadata.Value?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static FrameworkElement CreateDefaultControlFromRuntime(RuntimeParameterMetadata metadata)
        {
            return new TextBox
            {
                Width = 200,
                Text = metadata.Value?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        #endregion
    }
}
