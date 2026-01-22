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
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
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
    /// 分类可见性转换器 - 根据工具分类显示或隐藏工具
    /// </summary>
    public class CategoryVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string toolCategory && values[1] is ToolCategory category)
            {
                // 显示属于当前分类的
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
            // 调试输出
            System.Diagnostics.Debug.WriteLine($"CurrentWorkflowMultiConverter called with {values.Length} values");
            for (int i = 0; i < values.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine($"  Value[{i}]: {values[i]?.GetType().Name} = {values[i]}");
            }

            // 支持两种模式：比较 WorkflowInfo 对象或比较 Id 字符串
            if (values.Length >= 2)
            {
                if (values[0] is WorkflowInfo workflow && values[1] is WorkflowInfo currentWorkflow)
                {
                    // 模式1: 比较两个 WorkflowInfo 对象
                    bool result = workflow?.Id == currentWorkflow?.Id;
                    System.Diagnostics.Debug.WriteLine($"  Comparing WorkflowInfo: {workflow?.Id} == {currentWorkflow?.Id} => {result}");
                    return result;
                }
                else if (values[0] is string workflowId && values[1] is string currentWorkflowId)
                {
                    // 模式2: 比较两个 Id 字符串
                    bool result = workflowId == currentWorkflowId;
                    System.Diagnostics.Debug.WriteLine($"  Comparing Id strings: {workflowId} == {currentWorkflowId} => {result}");
                    return result;
                }
            }
            System.Diagnostics.Debug.WriteLine("  Returning false");
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 当前工作流 Id 转换器 - 判断当前工作流 Id 是否匹配
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
