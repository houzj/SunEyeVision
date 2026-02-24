using System;
using System.Globalization;
using System.Windows.Data;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters.Workflow
{
    /// <summary>
    /// 运行模式到按钮文本的转换�?    /// </summary>
    public class RunModeButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RunMode mode)
            {
                return mode switch
                {
                    RunMode.Single => "单次",
                    RunMode.Continuous => "连续",
                    _ => "单次"
                };
            }
            return "单次";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
