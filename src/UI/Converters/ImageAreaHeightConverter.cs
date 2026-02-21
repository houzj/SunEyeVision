using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 图像显示区域高度转换器
    /// 根据图像预览是否显示，返回不同的高度
    /// </summary>
    public class ImageAreaHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool showPreview)
            {
                // 如果显示预览模块，增加图像显示区域的高度
                var height = showPreview ? new GridLength(600, GridUnitType.Pixel) : new GridLength(500, GridUnitType.Pixel);
                Debug.WriteLine($"[ImageAreaHeightConverter] ShowImagePreview={showPreview}, 返回高度={height.Value}");
                return height;
            }

            // 默认高度
            var defaultHeight = new GridLength(500, GridUnitType.Pixel);
            Debug.WriteLine($"[ImageAreaHeightConverter] value不是bool类型，返回默认高度={defaultHeight.Value}");
            return defaultHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
