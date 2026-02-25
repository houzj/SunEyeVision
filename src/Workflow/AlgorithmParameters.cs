using System;
using System.Collections.Generic;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 算法参数容器
    /// </summary>
    public class AlgorithmParameters
    {
        /// <summary>
        /// 参数字典
        /// </summary>
        public Dictionary<string, object> Values { get; set; }

        public AlgorithmParameters()
        {
            Values = new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            if (Values.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void SetValue(string key, object value)
        {
            Values[key] = value;
        }

        /// <summary>
        /// 检查参数是否存在
        /// </summary>
        public bool HasParameter(string key)
        {
            return Values.ContainsKey(key);
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>(Values);
        }

        /// <summary>
        /// 从字典创建参数
        /// </summary>
        public static AlgorithmParameters FromDictionary(Dictionary<string, object> dict)
        {
            var parameters = new AlgorithmParameters();
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    parameters.Values[kvp.Key] = kvp.Value;
                }
            }
            return parameters;
        }
    }
}
