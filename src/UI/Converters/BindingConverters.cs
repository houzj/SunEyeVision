using System;
using System.Globalization;
using System.Windows.Data;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值转可见性转换器
    /// </summary>
    public class BoolToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[BoolToVisibleConverter] Convert 被调用: value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                if (value is bool boolValue)
                {
                    var result = boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    PluginLogger.Info($"[BoolToVisibleConverter] 返回: {result}", "UI");
                    return result;
                }

                PluginLogger.Warning($"[BoolToVisibleConverter] value 不是 bool 类型，返回默认值 Collapsed", "UI");
                return System.Windows.Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[BoolToVisibleConverter] Convert 发生异常: {ex.Message}\n{ex.StackTrace}", "UI");
                return System.Windows.Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PluginLogger.Info($"[BoolToVisibleConverter] ConvertBack 被调用: value={value?.ToString() ?? "null"}, value.GetType()={value?.GetType().Name ?? "null"}, targetType={targetType?.Name ?? "null"}", "UI");

                if (value is System.Windows.Visibility visibility)
                {
                    var result = visibility == System.Windows.Visibility.Visible;
                    PluginLogger.Info($"[BoolToVisibleConverter] ConvertBack 返回: {result}", "UI");
                    return result;
                }

                PluginLogger.Warning($"[BoolToVisibleConverter] value 不是 Visibility 类型，返回默认值 false", "UI");
                return false;
            }
            catch (Exception ex)
            {
                PluginLogger.Error($"[BoolToVisibleConverter] ConvertBack 发生异常: {ex.Message}\n{ex.StackTrace}", "UI");
                return false;
            }
        }
    }

    /// <summary>
    /// 布尔值转隐藏转换器（反向）
    /// </summary>
    public class BoolToHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Visibility visibility)
            {
                return visibility != System.Windows.Visibility.Visible;
            }
            return true;
        }
    }

    /// <summary>
    /// 空值转折叠转换器
    /// </summary>
    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 空值转可见转换器（反向）
    /// </summary>
    public class NullToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
