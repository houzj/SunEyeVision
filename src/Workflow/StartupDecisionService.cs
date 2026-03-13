using System;
using System.IO;

namespace SunEyeVision.Workflow;

/// <summary>
/// 启动决策结果
/// </summary>
public enum StartupDecision
{
    /// <summary>
    /// 显示配置界面（空状态，无项目）
    /// </summary>
    ShowConfigurationWithEmptyState,

    /// <summary>
    /// 显示配置界面（有最近项目）
    /// </summary>
    ShowConfigurationWithRecentProject,

    /// <summary>
    /// 显示配置界面（默认）
    /// </summary>
    ShowConfiguration,

    /// <summary>
    /// 跳过配置界面，直接进入主界面
    /// </summary>
    SkipConfiguration,

    /// <summary>
    /// 直接进入主界面，自动加载最近项目
    /// </summary>
    LoadRecentAndStart
}

/// <summary>
/// 启动决策服务
/// </summary>
/// <remarks>
/// 职责：
/// 1. 检查 RuntimeConfig
/// 2. 决定启动时是否显示配置界面
/// 3. 返回决策结果
/// 
/// 使用场景：
/// 1. 应用启动时调用
/// 2. 根据用户配置决定启动流程
/// </remarks>
public class StartupDecisionService
{
    private readonly ProjectManager _projectManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="projectManager">项目管理器</param>
    public StartupDecisionService(ProjectManager projectManager)
    {
        _projectManager = projectManager ?? throw new ArgumentNullException(nameof(projectManager));
    }

    /// <summary>
    /// 获取启动决策
    /// </summary>
    /// <returns>启动决策结果</returns>
    public StartupDecision GetStartupDecision()
    {
        // 获取运行时配置
        var config = _projectManager.RuntimeConfig;

        // 检查是否跳过配置界面
        if (config.SkipStartupConfig)
        {
            // 检查是否有最近项目
            if (!string.IsNullOrEmpty(config.CurrentProject) &&
                HasValidRecentProject(config.CurrentProject))
            {
                return StartupDecision.LoadRecentAndStart;
            }

            return StartupDecision.SkipConfiguration;
        }

        // 检查是否有项目
        var projectsDirectory = _projectManager.ProjectsDirectory;
        if (projectsDirectory == null || !Directory.Exists(projectsDirectory))
        {
            // 目录不存在，显示空状态
            return StartupDecision.ShowConfigurationWithEmptyState;
        }

        var projectDirectories = Directory.GetDirectories(projectsDirectory);
        if (projectDirectories.Length == 0)
        {
            // 无项目，显示空状态
            return StartupDecision.ShowConfigurationWithEmptyState;
        }

        // 有项目，检查是否有最近项目
        if (!string.IsNullOrEmpty(config.CurrentProject) &&
            HasValidRecentProject(config.CurrentProject))
        {
            return StartupDecision.ShowConfigurationWithRecentProject;
        }

        // 有项目，但无最近项目，显示配置界面
        return StartupDecision.ShowConfiguration;
    }

    /// <summary>
    /// 检查最近项目是否有效
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <returns>是否有效</returns>
    private bool HasValidRecentProject(string projectId)
    {
        try
        {
            var projectPath = _projectManager.GetProjectPath(projectId);
            return projectPath != null && Directory.Exists(projectPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取最近项目ID
    /// </summary>
    /// <returns>项目ID，如果没有则返回null</returns>
    public string? GetRecentProjectId()
    {
        var config = _projectManager.RuntimeConfig;
        return string.IsNullOrEmpty(config.CurrentProject) ? null : config.CurrentProject;
    }

    /// <summary>
    /// 获取最近配方名称
    /// </summary>
    /// <returns>配方名称，如果没有则返回null</returns>
    public string? GetRecentRecipeName()
    {
        var config = _projectManager.RuntimeConfig;
        return string.IsNullOrEmpty(config.CurrentRecipe) ? null : config.CurrentRecipe;
    }
}
