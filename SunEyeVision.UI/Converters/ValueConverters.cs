using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Controls;

namespace SunEyeVision.UI.Converters
{
    /// <summary>
    /// 布尔值反转转换器
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine($"[BoolToVisibilityConverter] Convert������, value={value}");
            if (value is bool boolValue)
            {
                var result = boolValue ? Visibility.Visible : Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"[BoolToVisibilityConverter] ����: {result} (boolValue={boolValue})");
                return result;
            }
            System.Diagnostics.Debug.WriteLine($"[BoolToVisibilityConverter] value����bool����, ����Collapsed");
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// 分类可见性转换器 - 根据工具分类显示或隐藏工�?
    /// </summary>
    public class CategoryVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string toolCategory && values[1] is ToolCategory category)
            {
                // 显示属于当前分类�?
                return toolCategory == category.Name ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 当前工作流转换器 - 判断工作流是否为当前选中
    /// </summary>
    public class CurrentWorkflowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value是工作流名称，需要通过与父级ItemsControl的CurrentWorkflow比较
            // 这个转换器需要在DataTrigger中通过RelativeSource使用
            if (value is string workflowName)
            {
                return true; // 将在绑定时通过多值绑定或其他方式实现
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 当前工作流多值转换器 - 判断工作流是否为当前选中
    /// </summary>
    public class CurrentWorkflowMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 支持两种模式：比�?WorkflowInfo 对象或比�?Id 字符�?
            if (values.Length >= 2)
            {
                if (values[0] is WorkflowInfo workflow && values[1] is WorkflowInfo currentWorkflow)
                {
                    // 模式1: 比较两个 WorkflowInfo 对象
                    return workflow?.Id == currentWorkflow?.Id;
                }
                else if (values[0] is string workflowId && values[1] is string currentWorkflowId)
                {
                    // 模式2: 比较两个 Id 字符�?
                    return workflowId == currentWorkflowId;
                }
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 当前工作�?Id 转换�?- 判断当前工作�?Id 是否匹配
    /// </summary>
    public class CurrentWorkflowIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is WorkflowCanvasControl control && value is string workflowId)
            {
                var currentWorkflow = control.CurrentWorkflow;
                return currentWorkflow?.Id == workflowId;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
