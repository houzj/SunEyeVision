using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Plugin.SDK.Validation;
using ValidationResult = SunEyeVision.Plugin.SDK.Validation.ValidationResult;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数范围特性
    /// </summary>
    /// <remarks>
    /// 用于标注参数的有效范围，支持自动验证和UI生成。
    /// 
    /// 使用示例：
    /// <code>
    /// public class CircleFindParams : ToolParamsBase
    /// {
    ///     [ParameterRange(0.1, 1000.0)]
    ///     public double MinRadius { get; set; } = 5.0;
    ///     
    ///     [ParameterRange(0.1, 1000.0)]
    ///     public double MaxRadius { get; set; } = 50.0;
    ///     
    ///     [ParameterRange(0, 255)]
    ///     public int Threshold { get; set; } = 128;
    /// }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterRangeAttribute : Attribute
    {
        /// <summary>
        /// 最小值
        /// </summary>
        public double Min { get; }

        /// <summary>
        /// 最大值
        /// </summary>
        public double Max { get; }

        /// <summary>
        /// 步进值（用于UI滑块）
        /// </summary>
        public double Step { get; set; } = 1.0;

        /// <summary>
        /// 单位（用于UI显示）
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 显示格式（用于UI显示）
        /// </summary>
        public string? DisplayFormat { get; set; }

        /// <summary>
        /// 创建参数范围特性
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        public ParameterRangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// 参数显示特性 - UI渲染的唯一元数据来源
    /// </summary>
    /// <remarks>
    /// 此特性与 ParameterRangeAttribute 配合使用，构成参数定义的唯一真实来源。
    /// UI层通过反射读取这些特性生成界面，无需手动维护ParameterMetadata。
    /// 
    /// 使用示例：
    /// <code>
    /// [ParameterRange(0, 255, Step = 1)]
    /// [ParameterDisplay(DisplayName = "阈值", Description = "二值化阈值", 
    ///                   Group = "基本参数", Order = 1, SupportsBinding = true)]
    /// public int Threshold { get; set; } = 128;
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ParameterDisplayAttribute : Attribute
    {
        /// <summary>
        /// 显示名称（UI标签）
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 描述信息（工具提示）
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 分组名称（用于参数分组显示）
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 显示顺序（越小越靠前）
        /// </summary>
        public int Order { get; set; } = int.MaxValue;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 是否高级参数（默认折叠）
        /// </summary>
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// 是否支持调试模式下实时修改
        /// </summary>
        public bool EditableInDebug { get; set; } = true;

        /// <summary>
        /// 是否支持数据绑定（从上游节点获取值）
        /// </summary>
        public bool SupportsBinding { get; set; } = true;
    }

    /// <summary>
    /// 工具参数基类
    /// </summary>
    /// <remarks>
    /// 所有工具参数类的基类，提供参数验证、克隆、元数据功能和属性变更通知。
    /// 
    /// 设计理念：
    /// 1. 强类型参数，编译时检查
    /// 2. 自动验证支持，通过特性标注约束
    /// 3. 序列化友好，支持JSON/Binary
    /// 4. UI绑定友好，继承ObservableObject支持属性变更通知
    /// 5. 参数只定义一次，UI直接绑定Parameters属性
    /// 
    /// 多态序列化：
    /// 使用 [JsonPolymorphic] 特性支持派生类的多态序列化。
    /// 序列化时自动添加 "$type" 字段标识具体类型。
    /// 
    /// 使用示例：
    /// <code>
    /// [JsonDerivedType(typeof(ThresholdParameters), "Threshold")]
    /// public class ThresholdParameters : ToolParameters
    /// {
    ///     private int _threshold = 128;
    ///     
    ///     [ParameterRange(0, 255)]
    ///     [ParameterDisplay(DisplayName = "阈值")]
    ///     public int Threshold
    ///     {
    ///         get => _threshold;
    ///         set => SetProperty(ref _threshold, value, "阈值");
    ///     }
    /// }
    /// </code>
    /// </remarks>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(GenericToolParameters), "Generic")]
    public abstract class ToolParameters : ObservableObject
    {
        /// <summary>
        /// 参数版本（用于序列化兼容性）
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 执行上下文（由执行器注入）
        /// </summary>
        /// <remarks>
        /// 工具执行期间可用的运行时服务，如日志记录器。
        /// 此属性由 ToolExecutor 在执行前自动注入，工具开发者无需手动设置。
        /// 
        /// 使用扩展方法访问：
        /// <code>
        /// parameters.LogInfo("执行信息");
        /// parameters.LogWarning("警告信息");
        /// parameters.LogError("错误信息", exception);
        /// </code>
        /// </remarks>
        [IgnoreSave]
        [IgnoreDisplay]
        public ExecutionContext? Context { get; internal set; }

        /// <summary>
        /// 验证所有参数
        /// </summary>
        /// <returns>验证结果</returns>
        public virtual ValidationResult Validate()
        {
            var result = new ValidationResult();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                // 跳过Version和Context属性
                if (prop.Name == nameof(Version) || prop.Name == nameof(Context)) continue;

                var value = prop.GetValue(this);
                var rangeAttr = prop.GetCustomAttribute<ParameterRangeAttribute>();

                if (rangeAttr != null && value != null)
                {
                    double numValue = Convert.ToDouble(value);
                    if (numValue < rangeAttr.Min || numValue > rangeAttr.Max)
                    {
                        var displayAttr = prop.GetCustomAttribute<ParameterDisplayAttribute>();
                        var displayName = displayAttr?.DisplayName ?? prop.Name;
                        result.AddError($"{displayName} 值 {numValue} 超出范围 [{rangeAttr.Min}, {rangeAttr.Max}]");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 深拷贝参数
        /// </summary>
        public ToolParameters Clone()
        {
            var cloned = (ToolParameters)MemberwiseClone();
            OnClone(cloned);
            return cloned;
        }

        /// <summary>
        /// 派生类可重写此方法执行深拷贝
        /// </summary>
        protected virtual void OnClone(ToolParameters cloned)
        {
            // 派生类可重写此方法执行深拷贝
        }

        /// <summary>
        /// 从另一个参数对象复制值
        /// </summary>
        public virtual void CopyFrom(ToolParameters other)
        {
            if (other == null || other.GetType() != GetType())
                return;

            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    prop.SetValue(this, prop.GetValue(other));
                }
            }
        }

        /// <summary>
        /// 获取运行时参数元数据
        /// </summary>
        public IReadOnlyList<RuntimeParameterMetadata> GetRuntimeParameterMetadata()
        {
            var metadata = new List<RuntimeParameterMetadata>();
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (prop.Name == nameof(Version) || prop.Name == nameof(Context)) continue;

                var rangeAttr = prop.GetCustomAttribute<ParameterRangeAttribute>();
                var displayAttr = prop.GetCustomAttribute<ParameterDisplayAttribute>();

                metadata.Add(new RuntimeParameterMetadata
                {
                    Name = prop.Name,
                    Type = prop.PropertyType,
                    Value = prop.GetValue(this),
                    Min = rangeAttr?.Min,
                    Max = rangeAttr?.Max,
                    Step = rangeAttr?.Step ?? 1.0,
                    Unit = rangeAttr?.Unit,
                    DisplayName = displayAttr?.DisplayName ?? prop.Name,
                    Description = displayAttr?.Description,
                    Group = displayAttr?.Group,
                    Order = displayAttr?.Order ?? int.MaxValue,
                    IsReadOnly = displayAttr?.IsReadOnly ?? false,
                    IsAdvanced = displayAttr?.IsAdvanced ?? false,
                    EditableInDebug = displayAttr?.EditableInDebug ?? true,
                    SupportsBinding = displayAttr?.SupportsBinding ?? true
                });
            }

            return metadata;
        }

        #region 参数分类查询方法

        /// <summary>
        /// 获取所有输入参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetInputParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Input);
        }

        /// <summary>
        /// 获取所有输出参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetOutputParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Output);
        }

        /// <summary>
        /// 获取所有配置参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetConfigParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Config);
        }

        /// <summary>
        /// 获取所有运行时参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetRuntimeParameterProperties()
        {
            return GetPropertiesByCategory(ParamCategory.Runtime);
        }

        /// <summary>
        /// 获取可绑定的输入参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetBindableInputProperties()
        {
            return GetInputParameterProperties()
                .Where(p => !p.IsDefined(typeof(IgnoreBindAttribute)));
        }

        /// <summary>
        /// 获取可保存的参数属性（排除标记IgnoreSave的）
        /// </summary>
        public IEnumerable<PropertyInfo> GetSaveableProperties()
        {
            return GetAllParameterProperties()
                .Where(p => !p.IsDefined(typeof(IgnoreSaveAttribute)));
        }

        /// <summary>
        /// 获取需要在UI中显示的参数属性
        /// </summary>
        public IEnumerable<PropertyInfo> GetDisplayableProperties()
        {
            return GetAllParameterProperties()
                .Where(p => !p.IsDefined(typeof(IgnoreDisplayAttribute)))
                .OrderBy(p => p.GetCustomAttribute<ParamAttribute>()?.Order ?? int.MaxValue);
        }

        /// <summary>
        /// 获取所有参数属性（排除Version）
        /// </summary>
        public IEnumerable<PropertyInfo> GetAllParameterProperties()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != nameof(Version));
        }

        /// <summary>
        /// 根据分类获取参数属性
        /// </summary>
        private IEnumerable<PropertyInfo> GetPropertiesByCategory(ParamCategory category)
        {
            return GetAllParameterProperties()
                .Where(p =>
                {
                    var paramAttr = p.GetCustomAttribute<ParamAttribute>();
                    return paramAttr != null && paramAttr.Category == category;
                });
        }

        #endregion

        #region 参数特性查询方法

        /// <summary>
        /// 判断参数是否需要保存到项目文件
        /// </summary>
        public bool ShouldSave(string propertyName)
        {
            var prop = GetType().GetProperty(propertyName);
            if (prop == null) return false;
            return !prop.IsDefined(typeof(IgnoreSaveAttribute));
        }

        /// <summary>
        /// 判断参数是否支持数据绑定
        /// </summary>
        public bool CanBind(string propertyName)
        {
            var prop = GetType().GetProperty(propertyName);
            if (prop == null) return false;

            var paramAttr = prop.GetCustomAttribute<ParamAttribute>();
            if (paramAttr == null) return false;

            // 只有输入参数且没有IgnoreBind标记的才支持绑定
            return paramAttr.Category == ParamCategory.Input && 
                   !prop.IsDefined(typeof(IgnoreBindAttribute));
        }

        /// <summary>
        /// 判断参数是否需要在UI显示
        /// </summary>
        public bool ShouldDisplay(string propertyName)
        {
            var prop = GetType().GetProperty(propertyName);
            if (prop == null) return false;
            return !prop.IsDefined(typeof(IgnoreDisplayAttribute));
        }

        #endregion

        #region 类型解析辅助方法

        /// <summary>
        /// 从 AssemblyQualifiedName 中提取程序集名称（忽略版本号）
        /// </summary>
        /// <param name="assemblyQualifiedName">完整的类型标识符</param>
        /// <returns>程序集名称（不含版本号），失败返回 null</returns>
        /// <remarks>
        /// 示例：
        /// 输入: "SunEyeVision.Tool.Threshold.ThresholdParameters, SunEyeVision.Tool.Threshold, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        /// 输出: "SunEyeVision.Tool.Threshold"
        /// </remarks>
        private static string? ExtractAssemblyNameWithoutVersion(string assemblyQualifiedName)
        {
            try
            {
                // AssemblyQualifiedName 格式: "TypeName, AssemblyName, Version=..., Culture=..., PublicKeyToken=..."
                // 我们只需要 AssemblyName 部分（TypeName 和 AssemblyName 之间的逗号后，Version 之前）
                var parts = assemblyQualifiedName.Split(',');
                if (parts.Length >= 2)
                {
                    return parts[1].Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从 AssemblyQualifiedName 中提取类型名称
        /// </summary>
        /// <param name="assemblyQualifiedName">完整的类型标识符</param>
        /// <returns>类型名称（含命名空间），失败返回 null</returns>
        /// <remarks>
        /// 示例：
        /// 输入: "SunEyeVision.Tool.Threshold.ThresholdParameters, SunEyeVision.Tool.Threshold, Version=1.0.0.0"
        /// 输出: "SunEyeVision.Tool.Threshold.ThresholdParameters"
        /// </remarks>
        private static string? ExtractTypeName(string assemblyQualifiedName)
        {
            try
            {
                // AssemblyQualifiedName 格式: "TypeName, AssemblyName, ..."
                var commaIndex = assemblyQualifiedName.IndexOf(',');
                if (commaIndex > 0)
                {
                    return assemblyQualifiedName.Substring(0, commaIndex).Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region 序列化支持

        /// <summary>
        /// 转换为可序列化字典（用于项目文件保存）
        /// </summary>
        /// <remarks>
        /// 支持 Enum、Point、Rect 等特殊类型的序列化。
        /// 仅保存标记为可保存的属性（未标记 IgnoreSave）。
        /// </remarks>
        public virtual Dictionary<string, object?> ToSerializableDictionary()
        {
            var dict = new Dictionary<string, object?>
            {
                ["$type"] = GetType().AssemblyQualifiedName,
                ["Version"] = Version
            };

            foreach (var prop in GetSaveableProperties())
            {
                if (!prop.CanRead) continue;

                var value = prop.GetValue(this);
                dict[prop.Name] = SerializeValue(value);
            }

            return dict;
        }

        /// <summary>
        /// 从字典加载参数值
        /// </summary>
        public void LoadFromDictionary(Dictionary<string, object?> dict)
        {
            if (dict == null) return;

            // 恢复版本
            if (dict.TryGetValue("Version", out var version) && version is int v)
            {
                Version = v;
            }

            foreach (var prop in GetSaveableProperties())
            {
                if (!prop.CanWrite) continue;
                if (!dict.TryGetValue(prop.Name, out var value) || value == null) continue;

                try
                {
                    var deserializedValue = DeserializeValue(value, prop.PropertyType);
                    prop.SetValue(this, deserializedValue);
                }
                catch
                {
                    // 反序列化失败时保留默认值
                }
            }
        }

        /// <summary>
        /// 从字典创建强类型参数实例（静态工厂方法）
        /// </summary>
        public static T? FromDictionary<T>(Dictionary<string, object?> dict) where T : ToolParameters
        {
            if (dict == null) return null;

            // 尝试从字典中的类型信息创建实例
            if (dict.TryGetValue("$type", out var typeName) && typeName is string typeStr)
            {
                var type = Type.GetType(typeStr);
                if (type != null && typeof(T).IsAssignableFrom(type))
                {
                    var instance = Activator.CreateInstance(type) as T;
                    instance?.LoadFromDictionary(dict);
                    return instance;
                }
            }

            // 回退到直接创建泛型类型
            var result = Activator.CreateInstance<T>();
            result.LoadFromDictionary(dict);
            return result;
        }

        /// <summary>
        /// 从字典创建参数实例（非泛型版本）
        /// </summary>
        /// <remarks>
        /// 增强的类型解析逻辑，支持版本兼容性：
        /// 1. 优先使用忽略版本号的程序集查找（兼容不同版本）
        /// 2. 回退到 Type.GetType()（标准方法）
        /// 3. 回退到短标识符映射（兼容旧格式）
        ///
        /// 设计原则（rule-004）：
        /// - 提供多层回退机制，提高成功率
        /// - 忽略版本号，提高版本兼容性
        /// - 详细日志记录，便于调试
        /// </remarks>
        public static ToolParameters? CreateFromDictionary(Dictionary<string, object?> dict)
        {
            if (dict == null) return null;

            if (!dict.TryGetValue("$type", out var typeName) || typeName is not string typeStr)
            {
                return null;
            }

            var logger = VisionLogger.Instance;
            logger.Log(LogLevel.Info, $"尝试解析类型标识符: {typeStr}", "ToolParameters");

            Type? type = null;

            // 方法1：通过程序集名称查找（忽略版本号）✅ 优先使用
            var assemblyName = ExtractAssemblyNameWithoutVersion(typeStr);
            var typeNameOnly = ExtractTypeName(typeStr);

            if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(typeNameOnly))
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var assembly = loadedAssemblies.FirstOrDefault(a =>
                    a.GetName().Name == assemblyName);

                if (assembly != null)
                {
                    type = assembly.GetType(typeNameOnly);
                    if (type != null)
                    {
                        logger.Log(LogLevel.Success, $"方法1成功: 通过程序集名称找到类型 (Assembly={assemblyName}, Type={typeNameOnly})", "ToolParameters");
                    }
                }
                else
                {
                    logger.Log(LogLevel.Warning, $"方法1失败: 未找到程序集 {assemblyName}", "ToolParameters");
                }
            }

            // 方法2：Type.GetType()（标准方法，会检查版本号）⚠️ 可能因版本不匹配失败
            if (type == null)
            {
                type = Type.GetType(typeStr);
                if (type != null)
                {
                    logger.Log(LogLevel.Success, $"方法2成功: Type.GetType() 找到类型", "ToolParameters");
                }
                else
                {
                    logger.Log(LogLevel.Warning, "方法2失败: Type.GetType() 未找到类型（可能是版本号不匹配）", "ToolParameters");
                }
            }

            // 方法3：短标识符映射（兼容旧格式）
            if (type == null)
            {
                type = typeStr switch
                {
                    "Generic" => typeof(GenericToolParameters),
                    _ => null
                };
                if (type != null)
                {
                    logger.Log(LogLevel.Success, $"方法3成功: 短标识符映射找到类型 (标识符={typeStr})", "ToolParameters");
                }
                else
                {
                    logger.Log(LogLevel.Warning, "方法3失败: 短标识符映射未找到类型", "ToolParameters");
                }
            }

            // 验证类型
            if (type == null)
            {
                logger.Log(LogLevel.Error, $"所有方法均失败: 无法解析类型 {typeStr}", "ToolParameters");
                return null;
            }

            if (!typeof(ToolParameters).IsAssignableFrom(type))
            {
                logger.Log(LogLevel.Error, $"类型验证失败: {type.FullName} 不是 ToolParameters 的派生类", "ToolParameters");
                return null;
            }

            // 创建实例
            try
            {
                var instance = Activator.CreateInstance(type) as ToolParameters;
                if (instance == null)
                {
                    logger.Log(LogLevel.Error, $"实例创建失败: {type.FullName}", "ToolParameters");
                    return null;
                }

                instance?.LoadFromDictionary(dict);
                logger.Log(LogLevel.Success, $"参数实例创建成功: {type.FullName}", "ToolParameters");
                return instance;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"实例创建异常: {type.FullName}, 错误: {ex.Message}", "ToolParameters", ex);
                return null;
            }
        }

        /// <summary>
        /// 序列化单个值
        /// </summary>
        private static object? SerializeValue(object? value)
        {
            if (value == null) return null;

            var type = value.GetType();

            // 枚举转字符串
            if (type.IsEnum)
                return value.ToString();

            // OpenCvSharp Point
            if (value is OpenCvSharp.Point point)
                return $"{point.X},{point.Y}";

            // OpenCvSharp Point2d
            if (value is OpenCvSharp.Point2d point2d)
                return $"{point2d.X},{point2d.Y}";

            // OpenCvSharp Rect
            if (value is OpenCvSharp.Rect rect)
                return $"{rect.X},{rect.Y},{rect.Width},{rect.Height}";

            // OpenCvSharp Rect2d
            if (value is OpenCvSharp.Rect2d rect2d)
                return $"{rect2d.X},{rect2d.Y},{rect2d.Width},{rect2d.Height}";

            // OpenCvSharp Size
            if (value is OpenCvSharp.Size size)
                return $"{size.Width},{size.Height}";

            // 其他类型直接返回
            return value;
        }

        /// <summary>
        /// 反序列化单个值
        /// </summary>
        private static object? DeserializeValue(object? value, Type targetType)
        {
            if (value == null) return null;

            // 如果已经是目标类型
            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            var strValue = value as string;

            // 枚举
            if (targetType.IsEnum && strValue != null)
                return Enum.Parse(targetType, strValue);

            // OpenCvSharp Point
            if (targetType == typeof(OpenCvSharp.Point) && strValue != null)
            {
                var parts = strValue.Split(',');
                if (parts.Length == 2)
                    return new OpenCvSharp.Point(int.Parse(parts[0]), int.Parse(parts[1]));
            }

            // OpenCvSharp Point2d
            if (targetType == typeof(OpenCvSharp.Point2d) && strValue != null)
            {
                var parts = strValue.Split(',');
                if (parts.Length == 2)
                    return new OpenCvSharp.Point2d(double.Parse(parts[0]), double.Parse(parts[1]));
            }

            // OpenCvSharp Rect
            if (targetType == typeof(OpenCvSharp.Rect) && strValue != null)
            {
                var parts = strValue.Split(',');
                if (parts.Length == 4)
                    return new OpenCvSharp.Rect(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
            }

            // OpenCvSharp Rect2d
            if (targetType == typeof(OpenCvSharp.Rect2d) && strValue != null)
            {
                var parts = strValue.Split(',');
                if (parts.Length == 4)
                    return new OpenCvSharp.Rect2d(double.Parse(parts[0]), double.Parse(parts[1]), double.Parse(parts[2]), double.Parse(parts[3]));
            }

            // OpenCvSharp Size
            if (targetType == typeof(OpenCvSharp.Size) && strValue != null)
            {
                var parts = strValue.Split(',');
                if (parts.Length == 2)
                    return new OpenCvSharp.Size(int.Parse(parts[0]), int.Parse(parts[1]));
            }

            // 尝试直接转换
            return Convert.ChangeType(value, targetType);
        }

        #endregion
    }

    /// <summary>
    /// 运行时参数元数据 - 用于运行时参数值跟踪和内省
    /// </summary>
    /// <remarks>
    /// UI层直接使用此结构进行渲染，无需额外的ParameterMetadata层。
    /// 数据来源于ToolParameters属性上的特性标注。
    /// </remarks>
    public sealed class RuntimeParameterMetadata
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Type { get; set; } = typeof(object);

        /// <summary>
        /// 当前值
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// 最小值（如果适用）
        /// </summary>
        public double? Min { get; set; }

        /// <summary>
        /// 最大值（如果适用）
        /// </summary>
        public double? Max { get; set; }

        /// <summary>
        /// 步进值
        /// </summary>
        public double Step { get; set; } = 1.0;

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 分组
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 是否高级参数
        /// </summary>
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// 是否支持调试模式下实时修改
        /// </summary>
        public bool EditableInDebug { get; set; } = true;

        /// <summary>
        /// 是否支持数据绑定
        /// </summary>
        public bool SupportsBinding { get; set; } = true;
    }

    /// <summary>
    /// 通用工具参数 - 用于兼容层的具体实现
    /// </summary>
    public class GenericToolParameters : ToolParameters
    {
        private readonly Dictionary<string, object?> _values = new();

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void SetValue(string name, object? value)
        {
            _values[name] = value;
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? GetValue<T>(string name)
        {
            if (_values.TryGetValue(name, out var value))
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
        /// 获取所有参数名称
        /// </summary>
        public IEnumerable<string> GetParameterNames() => _values.Keys;

        /// <summary>
        /// 转换为可序列化字典
        /// </summary>
        /// <remarks>
        /// 修复：必须序列化内部字典 _values 的所有参数值
        /// </remarks>
        public override Dictionary<string, object?> ToSerializableDictionary()
        {
            var dict = new Dictionary<string, object?>
            {
                ["$type"] = GetType().AssemblyQualifiedName,
                ["Version"] = Version
            };

            // 序列化所有参数值
            foreach (var kvp in _values)
            {
                dict[kvp.Key] = kvp.Value;
            }

            return dict;
        }
    }
}
