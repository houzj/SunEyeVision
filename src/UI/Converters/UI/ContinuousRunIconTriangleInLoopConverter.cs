using System;
using System.Globalization;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 连续运行图标转换�?- 运行状态显示停止图�?红色)，非运行状态显示循环图�?绿色)
    /// </summary>
    public class ContinuousRunIconTriangleInLoopConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                // 运行状态：显示停止图标(方形)
                if (isRunning)
                {
                    return "M6,6 L18,6 L18,18 L6,18 Z";
                }
                // 停止状态：显示简化的循环图标
                else
                {
                    return "M12,2 A10,10 0 1,1 2,12 L5,12 A7,7 0 1,0 12,5 Z";
                }
            }
            return "M12,2 A10,10 0 1,1 2,12 L5,12 A7,7 0 1,0 12,5 Z";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 连续运行图标颜色转换�?- 运行状态为红色，非运行状态为鲜艳的绿�?
    /// </summary>
    public class ContinuousRunIconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                // 运行状态：红色 (#FF5252 - 更醒�?
                // 停止状态：绿色 (#4CAF50 - 更清晰可�?
                return isRunning ? "#FF5252" : "#4CAF50";
            }
            return "#4CAF50";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
