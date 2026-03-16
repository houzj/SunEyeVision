using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Core.Services.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// JSON序列化扩展方法
    /// </summary>
    /// <remarks>
    /// 提供便捷的序列化和反序列化扩展方法，统一使用JsonSerializationOptions配置。
    /// </remarks>
    public static class JsonSerializationExtensions
    {
        /// <summary>
        /// 将对象序列化为JSON字符串（使用默认配置）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要序列化的对象</param>
        /// <returns>JSON字符串</returns>
        public static string ToJson<T>(this T value)
        {
            if (value == null)
                return "null";

            return JsonSerializer.Serialize(value, JsonSerializationOptions.Default);
        }

        /// <summary>
        /// 将对象序列化为紧凑的JSON字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">要序列化的对象</param>
        /// <returns>紧凑的JSON字符串</returns>
        public static string ToCompactJson<T>(this T value)
        {
            if (value == null)
                return "null";

            return JsonSerializer.Serialize(value, JsonSerializationOptions.Compact);
        }

        /// <summary>
        /// 将JSON字符串反序列化为对象（使用默认配置）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <returns>反序列化的对象</returns>
        public static T? FromJson<T>(this string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, JsonSerializationOptions.Default);
        }

        /// <summary>
        /// 将JSON字符串反序列化为对象（使用兼容配置）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="json">JSON字符串</param>
        /// <returns>反序列化的对象</returns>
        public static T? FromJsonCompatibility<T>(this string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, JsonSerializationOptions.Compatibility);
        }
    }
}
