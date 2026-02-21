using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.Abstractions;

namespace SunEyeVision.Plugin.Infrastructure.Base
{
    /// <summary>
    /// 工具UI辅助类 - 提供通用方法减少重复代码
    /// </summary>
    public static class ToolUIHelpers
    {
        /// <summary>
        /// 从ToolMetadata提取参数到ViewModel
        /// </summary>
        public static T GetParameterValue<T>(ToolMetadata? metadata, string paramName, T defaultValue)
        {
            if (metadata?.InputParameters == null)
                return defaultValue;

            var param = metadata.InputParameters.FirstOrDefault(p => 
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            
            if (param?.DefaultValue == null)
                return defaultValue;
            
            if (typeof(T) == typeof(string))
                return (T)(object)param.DefaultValue.ToString();
            if (typeof(T) == typeof(int) && int.TryParse(param.DefaultValue.ToString(), out int intVal))
                return (T)(object)intVal;
            if (typeof(T) == typeof(double) && double.TryParse(param.DefaultValue.ToString(), out double doubleVal))
                return (T)(object)doubleVal;
            if (typeof(T) == typeof(bool) && bool.TryParse(param.DefaultValue.ToString(), out bool boolVal))
                return (T)(object)boolVal;
            if (typeof(T) == typeof(float) && float.TryParse(param.DefaultValue.ToString(), out float floatVal))
                return (T)(object)floatVal;
            
            return defaultValue;
        }

        /// <summary>
        /// 保存参数到字典
        /// </summary>
        public static Dictionary<string, object> CreateParameterDictionary(params (string name, object value)[] parameters)
        {
            var dict = new Dictionary<string, object>();
            foreach (var (name, value) in parameters)
            {
                dict[name] = value;
            }
            return dict;
        }

        /// <summary>
        /// 批量更新参数到字典
        /// </summary>
        public static void UpdateParameters(Dictionary<string, object> dict, params (string name, object value)[] parameters)
        {
            foreach (var (name, value) in parameters)
            {
                dict[name] = value;
            }
        }

        /// <summary>
        /// 获取参数值，支持类型转换
        /// </summary>
        public static T GetValue<T>(Dictionary<string, object> dict, string key, T defaultValue = default)
        {
            if (dict.TryGetValue(key, out var value))
            {
                try
                {
                    if (value == null)
                        return defaultValue;
                    
                    if (typeof(T).IsEnum)
                    {
                        if (value is string str)
                            return (T)Enum.Parse(typeof(T), str);
                        return (T)value;
                    }
                    
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
