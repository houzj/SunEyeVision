using System;
using System.Globalization;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters.Workflow
{
    /// <summary>
    /// 连续运行按钮文本转换器
    /// </summary>
    public class ContinuousRunButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? "⏹ 停止" : "▶ 连续运行";
            }
            return "▶ 连续运行";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
