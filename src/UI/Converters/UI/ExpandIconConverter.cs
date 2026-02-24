using System;
using System.Globalization;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// Â±ïÂºÄÂõæÊ†áËΩ¨Êç¢Âô?
    /// true -> ‚ñ?(Â±ïÂºÄÁä∂ÊÄ?, false -> ‚ñ?(ÊäòÂè†Áä∂ÊÄ?
    /// </summary>
    public class ExpandIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "‚ñ? : "‚ñ?;
            }
            return "‚ñ?;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
