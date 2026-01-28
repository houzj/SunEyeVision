using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Shared.Controls.PropertyGrid
{
    /// <summary>
    /// 通用属性网格控件
    /// 用于显示和编辑插件的参数
    /// </summary>
    public partial class GenericPropertyGrid : UserControl
    {
        public GenericPropertyGrid()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 属性项集合
        /// </summary>
        public List<PropertyItem> Properties
        {
            get { return (List<PropertyItem>)GetValue(PropertiesProperty); }
            set { SetValue(PropertiesProperty, value); }
        }

        public static readonly DependencyProperty PropertiesProperty =
            DependencyProperty.Register("Properties", typeof(List<PropertyItem>), typeof(GenericPropertyGrid),
                new PropertyMetadata(null, OnPropertiesChanged));

        private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (GenericPropertyGrid)d;
            control.UpdateProperties();
        }

        private void UpdateProperties()
        {
            PropertiesPanel.Children.Clear();

            if (Properties == null) return;

            foreach (var property in Properties)
            {
                var panel = new PropertyItemPanel(property);
                PropertiesPanel.Children.Add(panel);
            }
        }
    }

    /// <summary>
    /// 属性项
    /// </summary>
    public class PropertyItem
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public object[] Options { get; set; }
        public string Description { get; set; }
    }
}
