using System;
using System.Globalization;
using System.Windows;
using SunEyeVision.UI.Controls;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// CanvasType到WorkflowCanvas可见性的转换器
    /// </summary>
    public class CanvasTypeToWorkflowVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine($"[CanvasTypeToWorkflowVisibilityConverter] ===== Convert called =====");
            System.Diagnostics.Debug.WriteLine($"[CanvasTypeToWorkflowVisibilityConverter] value: {value}");
            System.Diagnostics.Debug.WriteLine($"[CanvasTypeToWorkflowVisibilityConverter] value type: {value?.GetType().Name ?? "null"}");

            if (value is CanvasType canvasType)
            {
                var visibility = canvasType == CanvasType.WorkflowCanvas ? Visibility.Visible : Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"[CanvasTypeToWorkflowVisibilityConverter] CanvasType: {canvasType}, Returning Visibility: {visibility}");
                return visibility;
            }

            System.Diagnostics.Debug.WriteLine($"[CanvasTypeToWorkflowVisibilityConverter] value is not CanvasType, returning Collapsed");
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
