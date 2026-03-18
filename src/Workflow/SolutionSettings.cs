using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Core.Services.Serialization;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案用户设置
/// </summary>
/// <remarks>
/// 职责：用户偏好设置和已知解决方案列表
///
/// 特性：
/// - 持久化到 JSON 文件
/// - 继承 ObservableObject（支持 UI 绑定）
/// - 线程安全集合（Dictionary + Lock）
/// - 已知解决方案列表（持久化存储）
///
/// 设计原则（rule-002）：
/// - 命名符合视觉软件行业标准
/// - 替代 RuntimeConfig（更符合语义）
///
/// 日志规范（rule-003）：
/// - 使用 VisionLogger 记录日志
/// - 使用适当的日志级别（Info/Success/Warning/Error）
///
/// 属性通知（rule-001）：
/// - 继承 ObservableObject
/// - 使用 SetProperty 方法
/// - 为用户可见属性提供 displayName
/// </remarks>
public class SolutionSettings : ObservableObject
{
    private string _currentSolutionId = "";

    // ✅ 使用公共属性进行序列化，避免私有字段序列化问题
    // 内部使用锁保证线程安全，序列化时直接访问字典
    private Dictionary<string, SolutionMetadata> _knownSolutions = new();

    private readonly object _knownSolutionsLock = new();
    private readonly ILogger _logger;

    /// <summary>
    /// 当前解决方案ID
    /// </summary>
    public string CurrentSolutionId
    {
        get => _currentSolutionId;
        set => SetProperty(ref _currentSolutionId, value);
    }

    /// <summary>
    /// 已知解决方案列表（持久化存储）
    /// </summary>
    /// <remarks>
    /// 序列化说明：
    /// - 使用 [JsonPropertyName] 指定 JSON 属性名
    /// - Getter 返回线程安全的克隆副本
    /// - Setter 仅用于反序列化，直接替换内部字典
    /// </remarks>
    [JsonPropertyName("knownSolutions")]
    public Dictionary<string, SolutionMetadata> KnownSolutions
    {
        get
        {
            lock (_knownSolutionsLock)
            {
                return _knownSolutions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone());
            }
        }
        set
        {
            // 仅用于反序列化
            if (value != null)
            {
                lock (_knownSolutionsLock)
                {
                    _knownSolutions = value;
                }
            }
        }
    }

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    public Dictionary<string, object> UserPreferences { get; set; }

    /// <summary>
    /// 启动时跳过配置界面
    /// </summary>
    public bool SkipStartupConfig
    {
        get => GetPreference<bool>("SkipStartupConfig", false);
        set => SetPreference("SkipStartupConfig", value);
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SolutionSettings()
    {
        _currentSolutionId = "";
        UserPreferences = new Dictionary<string, object>();
        _logger = VisionLogger.Instance;

        _logger.Log(LogLevel.Info, "解决方案用户设置初始化完成", "SolutionSettings");
    }

    /// <summary>
    /// 添加到已知解决方案列表
    /// </summary>
    /// <param name="metadata">元数据对象</param>
    public void AddKnownSolution(SolutionMetadata metadata)
    {
        if (metadata == null)
        {
            _logger.Log(LogLevel.Warning, "添加到已知解决方案失败：元数据为空", "SolutionSettings");
            return;
        }

        // 防御性检查：Id不能为空
        if (string.IsNullOrEmpty(metadata.Id))
        {
            _logger.Log(LogLevel.Warning, $"添加到已知解决方案失败：元数据Id为空（Name={metadata.Name}）", "SolutionSettings");
            return;
        }

        lock (_knownSolutionsLock)
        {
            // 更新最后访问时间
            metadata.UpdateLastAccessTime();

            // 添加或更新到字典
            _knownSolutions[metadata.Id] = metadata.Clone();

            _logger.Log(LogLevel.Info, $"添加到已知解决方案: {metadata.Name} (Id={metadata.Id})", "SolutionSettings");
        }
    }

    /// <summary>
    /// 从已知解决方案列表中移除
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    public void RemoveKnownSolution(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
        {
            _logger.Log(LogLevel.Warning, "从已知解决方案移除失败：解决方案ID为空", "SolutionSettings");
            return;
        }

        lock (_knownSolutionsLock)
        {
            if (_knownSolutions.TryGetValue(solutionId, out var existing))
            {
                _knownSolutions.Remove(solutionId);
                _logger.Log(LogLevel.Info, $"从已知解决方案移除: {existing.Name} (Id={solutionId})", "SolutionSettings");
            }
        }
    }

    /// <summary>
    /// 获取已知解决方案列表（线程安全）
    /// </summary>
    /// <returns>元数据列表</returns>
    public List<SolutionMetadata> GetKnownSolutions()
    {
        lock (_knownSolutionsLock)
        {
            return _knownSolutions.Values.Select(m => m.Clone()).ToList();
        }
    }

    /// <summary>
    /// 检查是否包含指定解决方案
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>是否包含</returns>
    public bool ContainsKnownSolution(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
            return false;

        lock (_knownSolutionsLock)
        {
            return _knownSolutions.ContainsKey(solutionId);
        }
    }

    /// <summary>
    /// 获取用户偏好设置
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>偏好设置值</returns>
    public T? GetPreference<T>(string key, T? defaultValue = default)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.Log(LogLevel.Warning, "获取偏好设置失败：键为空", "SolutionSettings");
            return defaultValue;
        }

        if (UserPreferences.TryGetValue(key, out var value))
        {
            // 类型匹配，直接返回
            if (value is T typedValue)
                return typedValue;

            // 尝试类型转换
            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, $"类型转换失败: key={key}, targetType={typeof(T)}, valueType={value?.GetType()}, error={ex.Message}", "SolutionSettings");
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 设置用户偏好设置
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    public void SetPreference(string key, object? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            _logger.Log(LogLevel.Warning, "设置偏好设置失败：键为空", "SolutionSettings");
            return;
        }

        UserPreferences[key] = value;
        _logger.Log(LogLevel.Info, $"设置偏好: {key} = {value}", "SolutionSettings");
    }

    /// <summary>
    /// 移除用户偏好设置
    /// </summary>
    /// <param name="key">键</param>
    /// <returns>是否成功移除</returns>
    public bool RemovePreference(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        bool removed = UserPreferences.Remove(key);
        if (removed)
        {
            _logger.Log(LogLevel.Info, $"移除偏好: {key}", "SolutionSettings");
        }
        return removed;
    }

    /// <summary>
    /// 从文件加载设置
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <returns>是否成功</returns>
    public bool Load(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "加载设置失败：文件路径为空", "SolutionSettings");
            return false;
        }

        if (!File.Exists(filePath))
        {
            _logger.Log(LogLevel.Info, $"配置文件不存在，使用默认设置: {filePath}", "SolutionSettings");
            return false;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<SolutionSettings>(json, JsonSerializationOptions.Default);

            if (settings != null)
            {
                // 加载基本属性
                _currentSolutionId = settings.CurrentSolutionId;

                // 加载用户偏好
                if (settings.UserPreferences != null)
                {
                    UserPreferences = new Dictionary<string, object>(settings.UserPreferences);
                }

                // 加载已知解决方案列表（逐个添加，保持线程安全）
                if (settings.KnownSolutions != null && settings.KnownSolutions.Count > 0)
                {
                    lock (_knownSolutionsLock)
                    {
                        foreach (var kvp in settings.KnownSolutions)
                        {
                            _knownSolutions[kvp.Key] = kvp.Value;
                        }
                    }

                    _logger.Log(LogLevel.Success, $"加载已知解决方案列表成功: {settings.KnownSolutions.Count} 条记录", "SolutionSettings");
                }

                _logger.Log(LogLevel.Success, $"加载设置成功: {filePath}", "SolutionSettings");
                return true;
            }
            else
            {
                _logger.Log(LogLevel.Warning, $"加载设置失败：反序列化结果为null: {filePath}", "SolutionSettings");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"加载设置失败: {filePath}, 错误: {ex.Message}", "SolutionSettings", ex);
            return false;
        }
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <returns>是否成功</returns>
    public bool Save(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Log(LogLevel.Warning, "保存设置失败：文件路径为空", "SolutionSettings");
            return false;
        }

        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, JsonSerializationOptions.Default);
            File.WriteAllText(filePath, json);

            var fileInfo = new FileInfo(filePath);
            _logger.Log(LogLevel.Success, $"保存设置成功: {filePath}, 文件大小: {fileInfo.Length} 字节", "SolutionSettings");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"保存设置失败: {filePath}, 错误: {ex.Message}", "SolutionSettings", ex);
            return false;
        }
    }

    /// <summary>
    /// 获取设置统计信息
    /// </summary>
    /// <returns>统计信息字符串</returns>
    public string GetStatistics()
    {
        lock (_knownSolutionsLock)
        {
            return $"设置统计: 当前解决方案={_currentSolutionId}, 已知解决方案={_knownSolutions.Count}, 偏好设置={UserPreferences.Count}";
        }
    }
}
