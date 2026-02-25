using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Shared.Controls.ParameterPanel
{
    /// <summary>
    /// 通用参数面板控件
    /// 用于展示插件运行时参考?
    /// </summary>
    public partial class GenericParameterPanel : UserControl
    {
        public GenericParameterPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 参数集合
        /// </summary>
        public Dictionary<string, object> Parameters
        {
            get { return (Dictionary<string, object>)GetValue(ParametersProperty); }
            set { SetValue(ParametersProperty, value); }
        }

        public static readonly DependencyProperty ParametersProperty =
            DependencyProperty.Register("Parameters", typeof(Dictionary<string, object>), typeof(GenericParameterPanel),
                new PropertyMetadata(null, OnParametersChanged));

        private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (GenericParameterPanel)d;
            control.UpdateParameters();
        }

        private void UpdateParameters()
        {
            ParametersPanel.Children.Clear();

            if (Parameters == null) return;

            foreach (var kvp in Parameters)
            {
                var parameterItem = new ParameterItem(kvp.Key, kvp.Value);
                ParametersPanel.Children.Add(parameterItem);
            }
        }
    }

    /// <summary>
    /// 参数据?
    /// </summary>
    public class ParameterItem : StackPanel
    {
        public ParameterItem(string name, object value)
        {
            Orientation = Orientation.Horizontal;
            Margin = new Thickness(5, 2, 5, 2);

            var nameLabel = new Label
            {
                Content = name + ":",
                Width = 120,
                FontWeight = FontWeights.SemiBold
            };

            var valueLabel = new Label
            {
                Content = value?.ToString() ?? "null"
            };

            Children.Add(nameLabel);
            Children.Add(valueLabel);
        }
    }
}
