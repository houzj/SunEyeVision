using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Workflow;

/// <summary>
/// 运行时配置（持久化）
/// </summary>
/// <remarks>
/// 记录用户的运行时偏好和最近使用记录，持久化存储。
/// 与 RunContext（执行上下文）不同，RuntimeConfig 是持久的。
/// 
/// 使用场景：
/// 1. 应用启动时恢复上次工作状态
/// 2. 快速切换最近使用的项目和配方
/// 3. 记录用户偏好设置
/// </remarks>
public class RuntimeConfig : ObservableObject
{
    private string _currentProject = string.Empty;
    private string _currentRecipe = "standard";
    private DateTime _lastAccessTime = DateTime.Now;
    private bool _skipStartupConfig = false;

    /// <summary>
    /// 当前项目ID
    /// </summary>
    public string CurrentProject
    {
        get => _currentProject;
        set => SetProperty(ref _currentProject, value, "当前项目");
    }

    /// <summary>
    /// 当前配方名称
    /// </summary>
    public string CurrentRecipe
    {
        get => _currentRecipe;
        set => SetProperty(ref _currentRecipe, value, "当前配方");
    }

    /// <summary>
    /// 最近使用的项目列表
    /// </summary>
    public List<string> RecentProjects { get; set; } = new();

    /// <summary>
    /// 最近使用的配方列表
    /// </summary>
    public List<RecipeUsageRecord> RecentRecipes { get; set; } = new();

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime LastAccessTime
    {
        get => _lastAccessTime;
        set => SetProperty(ref _lastAccessTime, value);
    }

    /// <summary>
    /// 启动时跳过配置界面
    /// </summary>
    public bool SkipStartupConfig
    {
        get => _skipStartupConfig;
        set => SetProperty(ref _skipStartupConfig, value, "启动时跳过配置界面");
    }

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    public Dictionary<string, object> Preferences { get; set; } = new();

    /// <summary>
    /// 更新当前项目
    /// </summary>
    public void SetCurrentProject(string projectId, string recipeName = "standard")
    {
        CurrentProject = projectId;
        CurrentRecipe = recipeName;
        LastAccessTime = DateTime.Now;

        // 更新最近使用列表
        AddRecentProject(projectId);
    }

    /// <summary>
    /// 更新当前配方
    /// </summary>
    public void SetCurrentRecipe(string recipeName)
    {
        CurrentRecipe = recipeName;
        LastAccessTime = DateTime.Now;

        // 更新最近使用列表
        if (!string.IsNullOrEmpty(CurrentProject))
        {
            AddRecentRecipe(CurrentProject, recipeName);
        }
    }

    /// <summary>
    /// 添加最近使用的项目
    /// </summary>
    public void AddRecentProject(string projectId)
    {
        // 移除已存在的记录
        RecentProjects.Remove(projectId);

        // 添加到列表开头
        RecentProjects.Insert(0, projectId);

        // 限制列表长度
        if (RecentProjects.Count > 10)
        {
            RecentProjects = RecentProjects.GetRange(0, 10);
        }
    }

    /// <summary>
    /// 添加最近使用的配方
    /// </summary>
    public void AddRecentRecipe(string projectId, string recipeName)
    {
        // 移除已存在的记录
        RecentRecipes.RemoveAll(r => r.ProjectId == projectId && r.RecipeName == recipeName);

        // 添加新记录
        RecentRecipes.Insert(0, new RecipeUsageRecord
        {
            ProjectId = projectId,
            RecipeName = recipeName,
            LastUsedTime = DateTime.Now
        });

        // 限制列表长度
        if (RecentRecipes.Count > 20)
        {
            RecentRecipes = RecentRecipes.GetRange(0, 20);
        }
    }

    /// <summary>
    /// 获取指定项目的最近使用配方
    /// </summary>
    public string? GetRecentRecipe(string projectId)
    {
        foreach (var record in RecentRecipes)
        {
            if (record.ProjectId == projectId)
                return record.RecipeName;
        }
        return null;
    }

    /// <summary>
    /// 设置用户偏好
    /// </summary>
    public void SetPreference(string key, object value)
    {
        Preferences[key] = value;
    }

    /// <summary>
    /// 获取用户偏好
    /// </summary>
    public T? GetPreference<T>(string key, T? defaultValue = default)
    {
        if (Preferences.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
                return typedValue;

            try
            {
                return (T?)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 清除最近使用记录
    /// </summary>
    public void ClearRecentHistory()
    {
        RecentProjects.Clear();
        RecentRecipes.Clear();
    }
}

/// <summary>
/// 配方使用记录
/// </summary>
public class RecipeUsageRecord
{
    /// <summary>
    /// 项目ID
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// 配方名称
    /// </summary>
    public string RecipeName { get; set; } = string.Empty;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime LastUsedTime { get; set; }
}
