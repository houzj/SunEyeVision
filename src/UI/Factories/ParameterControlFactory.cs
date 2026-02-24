using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.UI.Factories;

namespace SunEyeVision.UI.Factories
{
    /// <summary>
    /// 参数控件工厂 - 根据参数元数据自动生成对应的UI控件
    /// </summary>
    public static class ParameterControlFactory
    {
        /// <summary>
        /// 根据参数元数据创建对应的UI控件
        /// </summary>
        /// <param name="metadata">参数元数据</param>
        /// <returns>生成的控件</returns>
        public static FrameworkElement CreateControl(ParameterMetadata metadata)
        {
            switch (metadata.Type)
            {
                case ParameterType.Int:
                case ParameterType.Double:
                    return CreateNumericControl(metadata);

                case ParameterType.Bool:
                    return CreateBoolControl(metadata);

                case ParameterType.Enum:
                    return CreateEnumControl(metadata);

                case ParameterType.Color:
                    return CreateColorControl(metadata);

                case ParameterType.Image:
                    return CreateImageControl(metadata);

                case ParameterType.FilePath:
                    return CreateFilePathControl(metadata);

                case ParameterType.String:
                    return CreateStringControl(metadata);

                case ParameterType.Point:
                    return CreatePointControl(metadata);

                case ParameterType.Size:
                    return CreateSizeControl(metadata);

                case ParameterType.Rect:
                    return CreateRectControl(metadata);

                case ParameterType.List:
                    return CreateListControl(metadata);

                default:
                    return CreateDefaultControl(metadata);
            }
        }

        #region 数值类型控件

        private static FrameworkElement CreateNumericControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var textBox = new TextBox
            {
                Width = 80,
                Text = metadata.DefaultValue?.ToString() ?? "0",
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // 如果有范围限制，添加滑块
            if (metadata.MinValue != null && metadata.MaxValue != null)
            {
                var slider = new Slider
                {
                    Width = 120,
                    Minimum = Convert.ToDouble(metadata.MinValue),
                    Maximum = Convert.ToDouble(metadata.MaxValue),
                    Value = Convert.ToDouble(metadata.DefaultValue ?? 0),
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

        #endregion

        #region 布尔类型控件

        private static Control CreateBoolControl(ParameterMetadata metadata)
        {
            var checkBox = new CheckBox
            {
                IsChecked = metadata.DefaultValue is bool b ? b : false,
                VerticalAlignment = VerticalAlignment.Center
            };
            return checkBox;
        }

        #endregion

        #region 枚举类型控件

        private static FrameworkElement CreateEnumControl(ParameterMetadata metadata)
        {
            var comboBox = new ComboBox
            {
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (metadata.Options != null)
            {
                foreach (var option in metadata.Options)
                {
                    comboBox.Items.Add(option?.ToString() ?? string.Empty);
                }

                if (metadata.DefaultValue != null)
                {
                    comboBox.SelectedItem = metadata.DefaultValue.ToString();
                }
                else if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }
            }

            return comboBox;
        }

        #endregion

        #region 颜色类型控件

        private static FrameworkElement CreateColorControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var colorPicker = new Button
            {
                Width = 40,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Background = GetColorFromValue(metadata.DefaultValue),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBox = new TextBox
            {
                Width = 120,
                Text = metadata.DefaultValue?.ToString() ?? "#FFFFFF",
                VerticalAlignment = VerticalAlignment.Center
            };

            colorPicker.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                    Title = "选择颜色文件"
                };

                if (dialog.ShowDialog() == true)
                {
                    textBox.Text = dialog.FileName;
                }
            };

            panel.Children.Add(colorPicker);
            panel.Children.Add(textBox);

            return panel;
        }

        private static Brush GetColorFromValue(object? value)
        {
            if (value is string colorStr && colorStr.StartsWith("#"))
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
                }
                catch { }
            }
            return Brushes.White;
        }

        #endregion

        #region 图像类型控件

        private static FrameworkElement CreateImageControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var browseButton = new Button
            {
                Content = "浏览...",
                Width = 60,
                Height = 24,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBox = new TextBox
            {
                Width = 140,
                Text = metadata.DefaultValue?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = true
            };

            browseButton.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff|All Files|*.*",
                    Title = "选择图像文件"
                };

                if (dialog.ShowDialog() == true)
                {
                    textBox.Text = dialog.FileName;
                }
            };

            panel.Children.Add(browseButton);
            panel.Children.Add(textBox);

            return panel;
        }

        #endregion

        #region 文件路径控件

        private static FrameworkElement CreateFilePathControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var browseButton = new Button
            {
                Content = "...",
                Width = 30,
                Height = 24,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBox = new TextBox
            {
                Width = 170,
                Text = metadata.DefaultValue?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };

            browseButton.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "选择文件"
                };

                if (dialog.ShowDialog() == true)
                {
                    textBox.Text = dialog.FileName;
                }
            };

            panel.Children.Add(browseButton);
            panel.Children.Add(textBox);

            return panel;
        }

        #endregion

        #region 字符串控件

        private static FrameworkElement CreateStringControl(ParameterMetadata metadata)
        {
            if (metadata.Description?.Contains("\n") == true || metadata.Description?.Length > 50 == true)
            {
                var textBox = new TextBox
                {
                    Width = 200,
                    Height = 60,
                    Text = metadata.DefaultValue?.ToString() ?? string.Empty,
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
                Text = metadata.DefaultValue?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        #endregion

        #region 点坐标控件

        private static FrameworkElement CreatePointControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var xTextBox = new TextBox
            {
                Width = 50,
                Text = "0",
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var yTextBox = new TextBox
            {
                Width = 50,
                Text = "0",
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "X:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
            panel.Children.Add(xTextBox);
            panel.Children.Add(new TextBlock { Text = "Y:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 2, 0) });
            panel.Children.Add(yTextBox);

            return panel;
        }

        #endregion

        #region 尺寸控件

        private static FrameworkElement CreateSizeControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var widthTextBox = new TextBox
            {
                Width = 50,
                Text = "0",
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var heightTextBox = new TextBox
            {
                Width = 50,
                Text = "0",
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "宽:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
            panel.Children.Add(widthTextBox);
            panel.Children.Add(new TextBlock { Text = "高:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 2, 0) });
            panel.Children.Add(heightTextBox);

            return panel;
        }

        #endregion

        #region 矩形控件

        private static FrameworkElement CreateRectControl(ParameterMetadata metadata)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var xTextBox = new TextBox
            {
                Width = 40,
                Text = "0",
                Margin = new Thickness(0, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var yTextBox = new TextBox
            {
                Width = 40,
                Text = "0",
                Margin = new Thickness(0, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var widthTextBox = new TextBox
            {
                Width = 40,
                Text = "0",
                Margin = new Thickness(0, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var heightTextBox = new TextBox
            {
                Width = 40,
                Text = "0",
                Margin = new Thickness(0, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "X:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
            panel.Children.Add(xTextBox);
            panel.Children.Add(new TextBlock { Text = "Y:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 2, 0) });
            panel.Children.Add(yTextBox);
            panel.Children.Add(new TextBlock { Text = "W:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 2, 0) });
            panel.Children.Add(widthTextBox);
            panel.Children.Add(new TextBlock { Text = "H:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 2, 0) });
            panel.Children.Add(heightTextBox);

            return panel;
        }

        #endregion

        #region 列表控件

        private static FrameworkElement CreateListControl(ParameterMetadata metadata)
        {
            var listBox = new ListBox
            {
                Width = 200,
                Height = 80,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (metadata.Options != null)
            {
                foreach (var option in metadata.Options)
                {
                    listBox.Items.Add(option?.ToString() ?? string.Empty);
                }
            }

            return listBox;
        }

        #endregion

        #region 默认控件

        private static FrameworkElement CreateDefaultControl(ParameterMetadata metadata)
        {
            return new TextBox
            {
                Width = 200,
                Text = metadata.DefaultValue?.ToString() ?? string.Empty,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        #endregion
    }
}
