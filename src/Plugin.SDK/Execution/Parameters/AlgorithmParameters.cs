using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.Core
{
    /// <summary>
    /// 算法参数容器 - 兼容层
    /// </summary>
    /// <remarks>
    /// 提供基于字典的参数存储，用于简化工具开发。
    /// 推荐使用强类型的 ToolParameters 派生类以获得更好的类型安全。
    /// </remarks>
    public class AlgorithmParameters
    {
        private readonly Dictionary<string, object?> _values = new();

        /// <summary>
        /// 参数字典
        /// </summary>
        public Dictionary<string, object?> Values => _values;

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? Get<T>(string key)
        {
            if (_values.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                try
                {
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void Set(string key, object? value)
        {
            _values[key] = value;
        }

        /// <summary>
        /// 尝试获取参数值
        /// </summary>
        public bool TryGet<T>(string key, out T? value)
        {
            if (_values.TryGetValue(key, out var objValue))
            {
                if (objValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
                try
                {
                    value = (T?)Convert.ChangeType(objValue, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 检查参数是否存在
        /// </summary>
        public bool HasParameter(string key)
        {
            return _values.ContainsKey(key);
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        public Dictionary<string, object?> ToDictionary()
        {
            return new Dictionary<string, object?>(_values);
        }

        /// <summary>
        /// 从字典创建参数
        /// </summary>
        public static AlgorithmParameters FromDictionary(Dictionary<string, object?> dict)
        {
            var parameters = new AlgorithmParameters();
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    parameters._values[kvp.Key] = kvp.Value;
                }
            }
            return parameters;
        }
    }
}
