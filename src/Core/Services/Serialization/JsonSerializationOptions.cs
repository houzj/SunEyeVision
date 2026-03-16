using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SunEyeVision.Core.Services.Serialization
{
    /// <summary>
    /// JSON序列化选项统一配置
    /// </summary>
    /// <remarks>
    /// 提供统一的JsonSerializerOptions配置，确保全项目序列化行为一致。
    /// 遵循以下原则：
    /// 1. Single Source of Truth: 所有配置集中管理
    /// 2. DRY: 避免重复配置
    /// 3. 一致性: 序列化和反序列化使用相同配置
    /// </remarks>
    public static class JsonSerializationOptions
    {
        /// <summary>
        /// 默认选项：用于大多数场景
        /// </summary>
        /// <remarks>
        /// 配置：
        /// - WriteIndented = true: 格式化输出，便于人工阅读
        /// - PropertyNamingPolicy = CamelCase: 属性名使用驼峰命名
        /// - DefaultIgnoreCondition = WhenWritingNull: 忽略null值
        /// - Encoder = UnsafeRelaxedJsonEscaping: 放宽转义规则，支持中文
        /// </remarks>
        public static JsonSerializerOptions Default => new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 紧凑选项：用于网络传输或存储空间敏感场景
        /// </summary>
        /// <remarks>
        /// 配置：
        /// - WriteIndented = false: 紧凑输出，减少文件大小
        /// - PropertyNamingPolicy = CamelCase: 属性名使用驼峰命名
        /// - DefaultIgnoreCondition = WhenWritingNull: 忽略null值
        /// - Encoder = UnsafeRelaxedJsonEscaping: 放宽转义规则，支持中文
        /// </remarks>
        public static JsonSerializerOptions Compact => new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 兼容选项：用于与旧版本或其他系统交互
        /// </summary>
        /// <remarks>
        /// 配置：
        /// - WriteIndented = true: 格式化输出，便于人工阅读
        /// - PropertyNamingPolicy = CamelCase: 属性名使用驼峰命名
        /// - DefaultIgnoreCondition = WhenWritingNull: 忽略null值
        /// - Encoder = UnsafeRelaxedJsonEscaping: 放宽转义规则，支持中文
        /// - PropertyNameCaseInsensitive = true: 属性名大小写不敏感
        /// </remarks>
        public static JsonSerializerOptions Compatibility => new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };
    }
}
