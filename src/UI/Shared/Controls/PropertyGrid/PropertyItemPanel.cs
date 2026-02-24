using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Shared.Controls.PropertyGrid
{
    /// <summary>
    /// 单个属性项的面�?
    /// </summary>
    public class PropertyItemPanel : StackPanel
    {
        private readonly PropertyItem _property;

        public PropertyItemPanel(PropertyItem property)
        {
            _property = property;
            Orientation = Orientation.Vertical;
            Margin = new Thickness(5);

            CreateContent();
        }

        private void CreateContent()
        {
            // 创建标签
            var label = new Label
            {
                Content = _property.DisplayName,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            Children.Add(label);

            // 根据类型创建编辑�?
            var editor = CreateEditor();
            Children.Add(editor);

            // 如果有描述，添加描述文本
            if (!string.IsNullOrEmpty(_property.Description))
            {
                var description = new TextBlock
                {
                    Text = _property.Description,
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                Children.Add(description);
            }
        }

        private FrameworkElement CreateEditor()
        {
            switch (_property.Type.ToLower())
            {
                case "int":
                case "integer":
                case "number":
                    if (_property.Options != null && _property.Options.Length > 0)
                    {
                        return CreateComboBox();
                    }
                    return CreateNumericEditor();

                case "double":
                case "float":
                case "decimal":
                    return CreateNumericEditor();

                case "bool":
                case "boolean":
                    return CreateCheckBox();

                case "string":
                case "text":
                    if (_property.Options != null && _property.Options.Length > 0)
                    {
                        return CreateComboBox();
                    }
                    return CreateTextBox();

                default:
                    if (_property.Options != null && _property.Options.Length > 0)
                    {
                        return CreateComboBox();
                    }
                    return CreateTextBox();
            }
        }

        private Control CreateNumericEditor()
        {
            // TODO: NumericUpDown 控件不可用，使用 TextBox 替代
            var editor = new TextBox
            {
                Text = _property.Value?.ToString() ?? "0"
            };
            // 这里需要根据实际的NumericUpDown控件来设置属�?
            return editor;
        }

        private Control CreateCheckBox()
        {
            var checkBox = new CheckBox
            {
                IsChecked = _property.Value as bool?
            };
            checkBox.Checked += (s, e) => _property.Value = true;
            checkBox.Unchecked += (s, e) => _property.Value = false;
            return checkBox;
        }

        private Control CreateTextBox()
        {
            var textBox = new TextBox
            {
                Text = _property.Value?.ToString() ?? string.Empty
            };
            textBox.TextChanged += (s, e) => _property.Value = textBox.Text;
            return textBox;
        }

        private Control CreateComboBox()
        {
            var comboBox = new ComboBox();
            foreach (var option in _property.Options)
            {
                comboBox.Items.Add(option);
            }
            comboBox.SelectedItem = _property.Value;
            comboBox.SelectionChanged += (s, e) => _property.Value = comboBox.SelectedItem;
            return comboBox;
        }
    }
}
