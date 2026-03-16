using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案版本
/// </summary>
/// <remarks>
/// 用于记录解决方案的版本历史，支持版本追踪和回滚。
/// </remarks>
public class SolutionVersion
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 版本描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 变更内容
    /// </summary>
    public List<string> Changes { get; set; } = new();

    /// <summary>
    /// 作者
    /// </summary>
    public string Author { get; set; } = "";

    /// <summary>
    /// 无参构造函数
    /// </summary>
    public SolutionVersion()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SolutionVersion(string version, string description)
    {
        Version = version;
        Description = description;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// 构造函数（带变更列表）
    /// </summary>
    public SolutionVersion(string version, string description, List<string> changes)
    {
        Version = version;
        Description = description;
        Changes = changes ?? new List<string>();
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// 添加变更项
    /// </summary>
    public void AddChange(string change)
    {
        if (!string.IsNullOrWhiteSpace(change))
        {
            Changes.Add(change);
        }
    }

    /// <summary>
    /// 获取变更摘要
    /// </summary>
    public string GetChangeSummary()
    {
        if (Changes.Count == 0)
        {
            return Description;
        }

        return $"{Description} ({Changes.Count} 项变更)";
    }

    /// <summary>
    /// 验证版本信息
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Version))
        {
            errors.Add("版本号不能为空");
        }

        return (errors.Count == 0, errors);
    }
}

/// <summary>
/// 解决方案版本管理器
/// </summary>
public class SolutionVersionManager
{
    private readonly List<SolutionVersion> _versions = new();
    private const int MaxVersions = 50;

    /// <summary>
    /// 获取所有版本
    /// </summary>
    public List<SolutionVersion> GetAll()
    {
        return _versions.ToList();
    }

    /// <summary>
    /// 获取最新版本
    /// </summary>
    public SolutionVersion? GetLatest()
    {
        return _versions.Count > 0 ? _versions[0] : null;
    }

    /// <summary>
    /// 获取指定版本
    /// </summary>
    public SolutionVersion? GetVersion(string version)
    {
        return _versions.FirstOrDefault(v => v.Version == version);
    }

    /// <summary>
    /// 添加新版本
    /// </summary>
    public void AddVersion(SolutionVersion version)
    {
        if (version == null)
            throw new ArgumentNullException(nameof(version));

        // 插入到开头
        _versions.Insert(0, version);

        // 限制版本数量
        while (_versions.Count > MaxVersions)
        {
            _versions.RemoveAt(_versions.Count - 1);
        }
    }

    /// <summary>
    /// 创建新版本
    /// </summary>
    public SolutionVersion CreateVersion(string description, List<string>? changes = null, string author = "")
    {
        var currentVersion = GetLatest();
        var newVersionNumber = IncrementVersion(currentVersion?.Version ?? "1.0");

        var newVersion = new SolutionVersion
        {
            Version = newVersionNumber,
            Description = description,
            Changes = changes ?? new List<string>(),
            Author = author
        };

        AddVersion(newVersion);
        return newVersion;
    }

    /// <summary>
    /// 移除版本
    /// </summary>
    public bool RemoveVersion(string version)
    {
        var versionToRemove = _versions.FirstOrDefault(v => v.Version == version);
        if (versionToRemove == null)
            return false;

        return _versions.Remove(versionToRemove);
    }

    /// <summary>
    /// 清空所有版本
    /// </summary>
    public void Clear()
    {
        _versions.Clear();
    }

    /// <summary>
    /// 版本号递增
    /// </summary>
    private string IncrementVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "1.0";

        var parts = version.Split('.');
        if (parts.Length >= 2)
        {
            if (int.TryParse(parts[1], out var minorVersion))
            {
                return $"{parts[0]}.{minorVersion + 1}";
            }
        }

        return version;
    }
}
