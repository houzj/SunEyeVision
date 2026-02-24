using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters.Node
{
    /// <summary>
    /// 节点显示文本转换�?- 使用适配器获取显示文�?
    /// </summary>
    public class NodeDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowNode node)
            {
                var adapter = NodeDisplayAdapterFactory.GetAdapter(node.AlgorithmType);
                return adapter.GetDisplayText(node);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 节点背景颜色转换�?- 使用适配器获取背景颜�?
    /// </summary>
    public class NodeBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowNode node)
            {
                var adapter = NodeDisplayAdapterFactory.GetAdapter(node.AlgorithmType);
                return new SolidColorBrush(adapter.GetBackgroundColor(node));
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 节点边框颜色转换�?- 使用适配器获取边框颜�?
    /// </summary>
    public class NodeBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowNode node)
            {
                var adapter = NodeDisplayAdapterFactory.GetAdapter(node.AlgorithmType);
                return new SolidColorBrush(adapter.GetBorderColor(node));
            }
            return new SolidColorBrush(Color.FromRgb(255, 149, 0)); // #ff9500
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 颜色到画刷转换器（通用�?
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            if (value is string colorString && TryParseColor(colorString, out Color parsedColor))
            {
                return new SolidColorBrush(parsedColor);
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private bool TryParseColor(string colorString, out Color color)
        {
            if (ColorConverter.ConvertFromString(colorString) is Color parsedColor)
            {
                color = parsedColor;
                return true;
            }
            color = Colors.White;
            return false;
        }
    }
}
