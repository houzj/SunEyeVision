using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.Abstractions.Core
{
    /// <summary>
    /// 算法参数
    /// </summary>
    /// <remarks>
    /// 此类是插件系统的核心契约，用于传递算法参数。
    /// 迁移自 SunEyeVision.Core.Models 以实现依赖反转。
    /// </remarks>
    public class AlgorithmParameters
    {
        /// <summary>
        /// 参数字典
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? Get<T>(string key)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default(T);
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void Set<T>(string key, T value)
        {
            Parameters[key] = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 设置参数值（SetParameter的别名）
        /// </summary>
        public void SetParameter<T>(string key, T value)
        {
            Set(key, value);
        }

        /// <summary>
        /// 获取参数值（Get的别名）
        /// </summary>
        public T? GetParameter<T>(string key)
        {
            return Get<T>(key);
        }

        /// <summary>
        /// 检查参数是否存在
        /// </summary>
        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key);
        }

        /// <summary>
        /// 获取所有参数
        /// </summary>
        public Dictionary<string, object> GetAllParameters()
        {
            return new Dictionary<string, object>(Parameters);
        }
    }
}
