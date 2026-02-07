using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SunEyeVision.PluginSystem.Parameters
{
    /// <summary>
    /// 参数验证器，提供参数验证规则和验证逻辑
    /// </summary>
    public class ParameterValidator
    {
        private readonly Dictionary<string, List<ValidationRule>> _validationRules = new();

        /// <summary>
        /// 添加验证规则
        /// </summary>
        public void AddRule(string parameterName, ValidationRule rule)
        {
            if (!_validationRules.ContainsKey(parameterName))
            {
                _validationRules[parameterName] = new List<ValidationRule>();
            }
            _validationRules[parameterName].Add(rule);
        }

        /// <summary>
        /// 批量添加验证规则
        /// </summary>
        public void AddRules(string parameterName, params ValidationRule[] rules)
        {
            foreach (var rule in rules)
            {
                AddRule(parameterName, rule);
            }
        }

        /// <summary>
        /// 验证单个参数
        /// </summary>
        public ValidationResult Validate(string parameterName, object? value)
        {
            var result = new ValidationResult { IsValid = true };

            if (_validationRules.TryGetValue(parameterName, out var rules))
            {
                foreach (var rule in rules)
                {
                    var ruleResult = rule.Validate(value);
                    if (!ruleResult.IsValid)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = ruleResult.ErrorMessage;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 验证多个参数
        /// </summary>
        public Dictionary<string, ValidationResult> ValidateAll(Dictionary<string, object?> parameters)
        {
            var results = new Dictionary<string, ValidationResult>();

            foreach (var (name, value) in parameters)
            {
                results[name] = Validate(name, value);
            }

            return results;
        }

        /// <summary>
        /// 验证参数项集合
        /// </summary>
        public Dictionary<string, ValidationResult> ValidateItems(IEnumerable<ParameterItem> items)
        {
            var results = new Dictionary<string, ValidationResult>();

            foreach (var item in items)
            {
                results[item.Name] = Validate(item.Name, item.Value);
            }

            return results;
        }

        /// <summary>
        /// 获取参数的所有验证规则
        /// </summary>
        public List<ValidationRule> GetRules(string parameterName)
        {
            return _validationRules.TryGetValue(parameterName, out var rules) ? rules : new List<ValidationRule>();
        }

        /// <summary>
        /// 清除指定参数的验证规则
        /// </summary>
        public void ClearRules(string parameterName)
        {
            _validationRules.Remove(parameterName);
        }

        /// <summary>
        /// 清除所有验证规则
        /// </summary>
        public void ClearAllRules()
        {
            _validationRules.Clear();
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = "";
    }

    /// <summary>
    /// 验证规则基类
    /// </summary>
    public abstract class ValidationRule
    {
        /// <summary>
        /// 验证方法
        /// </summary>
        public abstract ValidationResult Validate(object? value);
    }

    #region 内置验证规则

    /// <summary>
    /// 必填验证规则
    /// </summary>
    public class RequiredRule : ValidationRule
    {
        public override ValidationResult Validate(object? value)
        {
            var isValid = value != null;
            return new ValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? "" : "参数不能为空"
            };
        }
    }

    /// <summary>
    /// 范围验证规则
    /// </summary>
    public class RangeRule : ValidationRule
    {
        private readonly IComparable? _min;
        private readonly IComparable? _max;
        private readonly bool _minInclusive;
        private readonly bool _maxInclusive;

        public RangeRule(IComparable? min, IComparable? max, bool minInclusive = true, bool maxInclusive = true)
        {
            _min = min;
            _max = max;
            _minInclusive = minInclusive;
            _maxInclusive = maxInclusive;
        }

        public override ValidationResult Validate(object? value)
        {
            if (value == null)
            {
                return new ValidationResult { IsValid = true, ErrorMessage = "" };
            }

            if (value is not IComparable comparable)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "值必须是可比较的类型" };
            }

            var minCompare = _min != null ? comparable.CompareTo(_min) : 1;
            var maxCompare = _max != null ? comparable.CompareTo(_max) : -1;

            if (_min != null)
            {
                if (_minInclusive && minCompare < 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"值不能小于 {_min}"
                    };
                }
                if (!_minInclusive && minCompare <= 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"值必须大于 {_min}"
                    };
                }
            }

            if (_max != null)
            {
                if (_maxInclusive && maxCompare > 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"值不能大于 {_max}"
                    };
                }
                if (!_maxInclusive && maxCompare >= 0)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"值必须小于 {_max}"
                    };
                }
            }

            return new ValidationResult { IsValid = true, ErrorMessage = "" };
        }
    }

    /// <summary>
    /// 正则表达式验证规则
    /// </summary>
    public class RegexRule : ValidationRule
    {
        private readonly Regex _regex;
        private readonly string _errorMessage;

        public RegexRule(string pattern, string errorMessage = "格式不正确")
        {
            _regex = new Regex(pattern);
            _errorMessage = errorMessage;
        }

        public override ValidationResult Validate(object? value)
        {
            if (value == null)
            {
                return new ValidationResult { IsValid = true, ErrorMessage = "" };
            }

            var strValue = value.ToString();
            var isValid = !string.IsNullOrEmpty(strValue) && _regex.IsMatch(strValue);

            return new ValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? "" : _errorMessage
            };
        }
    }

    /// <summary>
    /// 自定义验证规则
    /// </summary>
    public class CustomRule : ValidationRule
    {
        private readonly Func<object?, bool> _validateFunc;
        private readonly string _errorMessage;

        public CustomRule(Func<object?, bool> validateFunc, string errorMessage = "验证失败")
        {
            _validateFunc = validateFunc;
            _errorMessage = errorMessage;
        }

        public override ValidationResult Validate(object? value)
        {
            var isValid = _validateFunc(value);
            return new ValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? "" : _errorMessage
            };
        }
    }

    /// <summary>
    /// 长度验证规则
    /// </summary>
    public class LengthRule : ValidationRule
    {
        private readonly int? _minLength;
        private readonly int? _maxLength;

        public LengthRule(int? minLength = null, int? maxLength = null)
        {
            _minLength = minLength;
            _maxLength = maxLength;
        }

        public override ValidationResult Validate(object? value)
        {
            if (value == null)
            {
                return new ValidationResult { IsValid = true, ErrorMessage = "" };
            }

            var strValue = value.ToString();
            var length = strValue?.Length ?? 0;

            if (_minLength.HasValue && length < _minLength.Value)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"长度不能少于 {_minLength} 个字符"
                };
            }

            if (_maxLength.HasValue && length > _maxLength.Value)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"长度不能超过 {_maxLength} 个字符"
                };
            }

            return new ValidationResult { IsValid = true, ErrorMessage = "" };
        }
    }

    /// <summary>
    /// 枚举值验证规则
    /// </summary>
    public class EnumRule : ValidationRule
    {
        private readonly Array _enumValues;

        public EnumRule(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("类型必须是枚举", nameof(enumType));
            }
            _enumValues = Enum.GetValues(enumType);
        }

        public EnumRule(Array enumValues)
        {
            _enumValues = enumValues;
        }

        public override ValidationResult Validate(object? value)
        {
            if (value == null)
            {
                return new ValidationResult { IsValid = true, ErrorMessage = "" };
            }

            var isValid = _enumValues.Cast<object>().Any(v => v.Equals(value));

            return new ValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? "" : "值不在允许的范围内"
            };
        }
    }

    #endregion
}
