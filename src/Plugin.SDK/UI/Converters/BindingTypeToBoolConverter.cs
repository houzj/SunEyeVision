using System;
using System.Globalization;
using System.Windows.Data;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Plugin.SDK.UI.Converters
{
    /// <summary>
    /// BindingType 到 Bool 的转换器
    /// </summary>
    /// <remarks>
    /// 用于在 XAML 中根据 BindingType 控制控件可见性或状态。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;Button Visibility="{Binding BindingType, Converter={StaticResource BindingTypeToBoolConverter}, ConverterParameter=Constant}"&gt;
    ///     常量模式
    /// &lt;/Button&gt;
    /// </code>
    /// </remarks>
    public class BindingTypeToBoolConverter : IValueConverter
    {
        /// <summary>
        /// 将 BindingType 转换为 Bool
        /// </summary>
        /// <param name="value">BindingType 值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">期望的 BindingType 值 ("Constant" 或 "Binding")</param>
        /// <param name="culture">文化信息</param>
        /// <returns>如果当前 BindingType 等于期望值，返回 true，否则返回 false</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BindingType bindingType && parameter is string expectedType)
            {
                if (Enum.TryParse<BindingType>(expectedType, out BindingType expected))
                {
                    return bindingType == expected;
                }
            }
            return false;
        }

        /// <summary>
        /// 将 Bool 转换回 BindingType（不实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// BindingType 到 Visibility 的转换器
    /// </summary>
    /// <remarks>
    /// 用于在 XAML 中根据 BindingType 控制控件可见性。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;Grid Visibility="{Binding BindingType, Converter={StaticResource BindingTypeToVisibilityConverter}, ConverterParameter=Constant}"&gt;
    ///     常量模式内容
    /// &lt;/Grid&gt;
    /// </code>
    /// </remarks>
    public class BindingTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BindingType bindingType && parameter is string expectedType)
            {
                if (Enum.TryParse<BindingType>(expectedType, out BindingType expected))
                {
                    return bindingType == expected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 数据类型到可见性的转换器
    /// </summary>
    /// <remarks>
    /// 用于在 XAML 中根据 DataType 控制编辑器可见性。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;Grid Visibility="{Binding DataType, Converter={StaticResource DataTypeToVisibilityConverter}, ConverterParameter=Int}"&gt;
    ///     整数编辑器
    /// &lt;/Grid&gt;
    /// &lt;Grid Visibility="{Binding DataType, Converter={StaticResource DataTypeToVisibilityConverter}, ConverterParameter=Numeric}"&gt;
    ///     数值编辑器（支持 Int、Int64、Double、Single、Decimal）
    /// &lt;/Grid&gt;
    /// </code>
    /// </remarks>
    public class DataTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Type dataType && parameter is string expectedType)
            {
                var typeCode = Type.GetTypeCode(dataType);

                // 支持特殊参数 "Numeric"，同时匹配所有数值类型
                if (expectedType == "Numeric")
                {
                    bool isNumeric = typeCode == TypeCode.Int32 || 
                                    typeCode == TypeCode.Int64 || 
                                    typeCode == TypeCode.Double || 
                                    typeCode == TypeCode.Single || 
                                    typeCode == TypeCode.Decimal;
                    return isNumeric ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }

                // 支持类型名称匹配（如 "Int"、"String" 等）
                if (expectedType == "Int")
                {
                    return typeCode == TypeCode.Int32 || typeCode == TypeCode.Int64
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                }
                if (expectedType == "Double")
                {
                    return typeCode == TypeCode.Double || typeCode == TypeCode.Single
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                }
                if (expectedType == "String")
                {
                    return typeCode == TypeCode.String || typeCode == TypeCode.Char
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                }
                if (expectedType == "Bool")
                {
                    return typeCode == TypeCode.Boolean
                        ? System.Windows.Visibility.Visible
                        : System.Windows.Visibility.Collapsed;
                }
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
