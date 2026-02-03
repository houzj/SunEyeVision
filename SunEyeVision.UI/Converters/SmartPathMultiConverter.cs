using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.Converters;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 多值智能路径转换器 - 用于触发路径重新计算
    /// </summary>
    public class SmartPathMultiConverter : IMultiValueConverter
    {
        private readonly SmartPathConverter _converter = new SmartPathConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = WorkflowConnection 对象
            // values[1] = PathUpdateCounter（用于触发更新）
            if (values.Length >= 2 && values[0] is WorkflowConnection connection)
            {
                int counter = values[1] is int ? (int)values[1] : 0;
                // 🔥 减少日志输出以提高性能
                // System.Diagnostics.Debug.WriteLine($"[SmartPathMultiConverter] Convert called - ConnectionId: {connection.Id}, PathUpdateCounter: {counter}");

                // 使用原有的 SmartPathConverter 进行转换，获取字符串
                string pathString = _converter.Convert(connection, typeof(string), parameter, culture) as string;

                // System.Diagnostics.Debug.WriteLine($"[SmartPathMultiConverter]   Path string length: {pathString?.Length ?? 0}");

                // 将字符串转换为 Geometry
                if (!string.IsNullOrEmpty(pathString))
                {
                    try
                    {
                        return Geometry.Parse(pathString);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SmartPathMultiConverter] 解析路径失败: {ex.Message}");
                    }
                }
            }
            return Geometry.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
