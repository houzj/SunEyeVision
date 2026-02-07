using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace SunEyeVision.PluginSystem.Parameters
{
    /// <summary>
    /// 参数项ViewModel，用于UI绑定的参数展示
    /// </summary>
    public class ParameterItem : ObservableObject
    {
        private string _name = "";
        private string _displayName = "";
        private string _description = "";
        private object? _value;
        private object? _defaultValue;
        private Type _dataType = typeof(string);
        private bool _isReadOnly;
        private string? _validationError;
        private bool _hasError;
        private Control? _control;
        private bool _isVisible = true;
        private object? _minValue;
        private object? _maxValue;

        #region 属性

        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 参数值
        /// </summary>
        public object? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnPropertyChanged(nameof(DisplayValue));
                    Validate();
                }
            }
        }

        /// <summary>
        /// 用于UI显示的字符串值
        /// </summary>
        public string DisplayValue
        {
            get => Value?.ToString() ?? "";
            set
            {
                if (TryParseValue(value, out var parsed))
                {
                    Value = parsed;
                }
            }
        }

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue
        {
            get => _defaultValue;
            set => SetProperty(ref _defaultValue, value);
        }

        /// <summary>
        /// 数据类型
        /// </summary>
        public Type DataType
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        /// <summary>
        /// 验证错误消息
        /// </summary>
        public string? ValidationError
        {
            get => _validationError;
            set
            {
                if (SetProperty(ref _validationError, value))
                {
                    HasError = !string.IsNullOrEmpty(value);
                }
            }
        }

        /// <summary>
        /// 是否有验证错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// UI控件
        /// </summary>
        public Control? Control
        {
            get => _control;
            set => SetProperty(ref _control, value);
        }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// 最小值（用于数值类型）
        /// </summary>
        public object? MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        /// <summary>
        /// 最大值（用于数值类型）
        /// </summary>
        public object? MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }

        /// <summary>
        /// 可选值列表（用于枚举、下拉框等）
        /// </summary>
        public ObservableCollection<ParameterOption>? Options { get; set; }

        #endregion

        #region 构造函数

        public ParameterItem()
        {
        }

        public ParameterItem(string name, Type dataType, object? defaultValue = null)
        {
            Name = name;
            DisplayName = name;
            DataType = dataType;
            DefaultValue = defaultValue;
            Value = defaultValue;
        }

        #endregion

        #region 方法

        /// <summary>
        /// 验证参数值
        /// </summary>
        /// <returns>验证是否通过</returns>
        public virtual bool Validate()
        {
            ValidationError = null;

            // 检查必填
            if (Value == null && DefaultValue != null)
            {
                ValidationError = $"参数 '{Name}' 不能为空";
                return false;
            }

            // 类型检查
            if (Value != null && !DataType.IsInstanceOfType(Value))
            {
                ValidationError = $"参数 '{Name}' 的值类型不正确";
                return false;
            }

            // 范围检查（数值类型）
            if (Value is IComparable comparable)
            {
                if (MinValue is IComparable min && comparable.CompareTo(min) < 0)
                {
                    ValidationError = $"参数 '{Name}' 不能小于 {MinValue}";
                    return false;
                }

                if (MaxValue is IComparable max && comparable.CompareTo(max) > 0)
                {
                    ValidationError = $"参数 '{Name}' 不能大于 {MaxValue}";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            Value = DefaultValue;
            ValidationError = null;
        }

        /// <summary>
        /// 尝试解析字符串值
        /// </summary>
        private bool TryParseValue(string input, out object? result)
        {
            result = null;

            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    result = null;
                    return true;
                }

                if (DataType == typeof(string))
                {
                    result = input;
                    return true;
                }

                if (DataType == typeof(int) || DataType == typeof(int?))
                {
                    result = int.Parse(input);
                    return true;
                }

                if (DataType == typeof(double) || DataType == typeof(double?))
                {
                    result = double.Parse(input);
                    return true;
                }

                if (DataType == typeof(float) || DataType == typeof(float?))
                {
                    result = float.Parse(input);
                    return true;
                }

                if (DataType == typeof(bool) || DataType == typeof(bool?))
                {
                    result = bool.Parse(input);
                    return true;
                }

                if (DataType == typeof(decimal) || DataType == typeof(decimal?))
                {
                    result = decimal.Parse(input);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建参数项的克隆
        /// </summary>
        public ParameterItem Clone()
        {
            return new ParameterItem
            {
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                Value = Value,
                DefaultValue = DefaultValue,
                DataType = DataType,
                IsReadOnly = IsReadOnly,
                ValidationError = ValidationError,
                HasError = HasError,
                Control = Control,
                IsVisible = IsVisible,
                MinValue = MinValue,
                MaxValue = MaxValue,
                Options = Options != null ? new ObservableCollection<ParameterOption>(Options) : null
            };
        }

        #endregion
    }

    /// <summary>
    /// 参数选项（用于枚举、下拉框等）
    /// </summary>
    public class ParameterOption
    {
        /// <summary>
        /// 选项值
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 显示文本
        /// </summary>
        public string DisplayText { get; set; } = "";

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        public ParameterOption(object? value, string displayText, string? description = null)
        {
            Value = value;
            DisplayText = displayText;
            Description = description;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
