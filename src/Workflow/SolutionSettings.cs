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
/// 职责：用户偏好设置和最近使用记录
///
/// 特性：
/// - 持久化到 JSON 文件
/// - 继承 ObservableObject（支持 UI 绑定）
/// - 线程安全集合（ObservableCollection + Lock）
/// - 自动限制最近使用列表数量（最多20个）
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
    private readonly object _recentSolutionsLock = new();
    private readonly ILogger _logger;

    /// <summary>
    /// 最大最近使用数量
    /// </summary>
    public const int MaxRecentCount = 20;

    /// <summary>
    /// 当前解决方案ID
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string CurrentSolutionId
    {
        get => _currentSolutionId;
        set => SetProperty(ref _currentSolutionId, value, "当前解决方案");
    }

    /// <summary>
    /// 最近使用的解决方案（按访问时间排序，最多20个）
    /// </summary>
    [JsonIgnore]
    public ObservableCollection<SolutionMetadata> RecentSolutions { get; private set; }

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object> UserPreferences { get; set; }

    /// <summary>
    /// 启动时跳过配置界面
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
        RecentSolutions = new ObservableCollection<SolutionMetadata>();
        UserPreferences = new Dictionary<string, object>();
        _logger = VisionLogger.Instance;

        _logger.Log(LogLevel.Info, "解决方案用户设置初始化完成", "SolutionSettings");
    }

    /// <summary>
    /// 添加到最近使用列表
    /// </summary>
    /// <param name="metadata">元数据对象</param>
    public void AddRecentSolution(SolutionMetadata metadata)
    {
        if (metadata == null)
        {
            _logger.Log(LogLevel.Warning, "添加到最近使用失败：元数据为空", "SolutionSettings");
            return;
        }

        lock (_recentSolutionsLock)
        {
            // 移除已存在的记录
            var existing = RecentSolutions.FirstOrDefault(s => s.Id == metadata.Id);
            if (existing != null)
            {
                RecentSolutions.Remove(existing);
            }

            // 更新最后访问时间
            metadata.UpdateLastAccessTime();

            // 添加到列表开头
            RecentSolutions.Insert(0, metadata.Clone());

            // 限制列表长度
            while (RecentSolutions.Count > MaxRecentCount)
            {
                RecentSolutions.RemoveAt(RecentSolutions.Count - 1);
            }

            _logger.Log(LogLevel.Info, $"添加到最近使用: {metadata.Name} (Id={metadata.Id})", "SolutionSettings");
        }
    }

    /// <summary>
    /// 从最近使用列表中移除
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    public void RemoveRecentSolution(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
        {
            _logger.Log(LogLevel.Warning, "从最近使用移除失败：解决方案ID为空", "SolutionSettings");
            return;
        }

        lock (_recentSolutionsLock)
        {
            var existing = RecentSolutions.FirstOrDefault(s => s.Id == solutionId);
            if (existing != null)
            {
                RecentSolutions.Remove(existing);
                _logger.Log(LogLevel.Info, $"从最近使用移除: {existing.Name} (Id={solutionId})", "SolutionSettings");
            }
        }
    }

    /// <summary>
    /// 清空最近使用列表
    /// </summary>
    public void ClearRecentSolutions()
    {
        lock (_recentSolutionsLock)
        {
            int count = RecentSolutions.Count;
            RecentSolutions.Clear();
            _logger.Log(LogLevel.Info, $"清空最近使用列表: 清空了 {count} 条记录", "SolutionSettings");
        }
    }

    /// <summary>
    /// 获取最近使用列表的副本（线程安全）
    /// </summary>
    /// <returns>元数据列表</returns>
    public List<SolutionMetadata> GetRecentSolutionsCopy()
    {
        lock (_recentSolutionsLock)
        {
            return RecentSolutions.Select(m => m.Clone()).ToList();
        }
    }

    /// <summary>
    /// 获取最近使用列表中的指定解决方案
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>元数据对象，不存在返回 null</returns>
    public SolutionMetadata? GetRecentSolution(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
            return null;

        lock (_recentSolutionsLock)
        {
            var metadata = RecentSolutions.FirstOrDefault(s => s.Id == solutionId);
            return metadata?.Clone();
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

                // 加载最近使用列表（逐个添加，保持线程安全）
                if (settings.UserPreferences != null)
                {
                    UserPreferences = new Dictionary<string, object>(settings.UserPreferences);
                }

                // 加载最近使用列表
                if (settings.RecentSolutions != null && settings.RecentSolutions.Count > 0)
                {
                    lock (_recentSolutionsLock)
                    {
                        foreach (var metadata in settings.RecentSolutions)
                        {
                            RecentSolutions.Add(metadata);
                        }
                    }

                    _logger.Log(LogLevel.Success, $"加载最近使用列表成功: {settings.RecentSolutions.Count} 条记录", "SolutionSettings");
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
        return $"设置统计: 当前解决方案={_currentSolutionId}, 最近使用={RecentSolutions.Count}, 偏好设置={UserPreferences.Count}";
    }
}
