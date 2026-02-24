using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Converters.Workflow
{
    /// <summary>
    /// 工作流状态到颜色转换�?
    /// </summary>
    public class WorkflowStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowState state)
            {
                return state switch
                {
                    WorkflowState.Stopped => new SolidColorBrush(Color.FromRgb(153, 153, 153)), // #999999
                    WorkflowState.Running => new SolidColorBrush(Color.FromRgb(0, 204, 153)),   // #00CC99
                    WorkflowState.Paused => new SolidColorBrush(Color.FromRgb(255, 153, 0)),    // #FF9900
                    WorkflowState.Error => new SolidColorBrush(Color.FromRgb(255, 68, 68)),     // #FF4444
                    _ => new SolidColorBrush(Color.FromRgb(153, 153, 153))
                };
            }
            return new SolidColorBrush(Color.FromRgb(153, 153, 153));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 工作流状态到文本转换�?
    /// </summary>
    public class WorkflowStateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is WorkflowState state)
            {
                return "�?; // 所有状态都用●表示
            }
            return "�?;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 连续运行按钮背景转换�?- 运行状态为红色，非运行状态为蓝色
    /// </summary>
    public class ContinuousRunBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? "#FF6B6B" : "#2196F3";
            }
            return "#2196F3";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 连续运行按钮边框转换�?
    /// </summary>
    public class ContinuousRunBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRunning)
            {
                return isRunning ? "#FF4444" : "#0066CC";
            }
            return "#0066CC";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
