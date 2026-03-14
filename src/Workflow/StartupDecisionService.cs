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
///
/// TODO: 在UI层重构完成后，更新此服务使用 SolutionManager
/// </remarks>
public class StartupDecisionService
{
    // TODO: 暂时禁用此服务，等UI层重构完成后恢复
    // private readonly ProjectManager _projectManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="solutionManager">解决方案管理器</param>
    public StartupDecisionService(SolutionManager solutionManager)
    {
        // TODO: 暂时禁用，等UI层重构完成后恢复
        // _projectManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));
    }

    /// <summary>
    /// 获取启动决策
    /// </summary>
    /// <returns>启动决策结果</returns>
    public StartupDecision GetStartupDecision()
    {
        // TODO: 暂时返回默认值，等UI层重构完成后实现
        return StartupDecision.ShowConfiguration;
    }

    /// <summary>
    /// 获取最近项目ID
    /// </summary>
    /// <returns>项目ID，如果没有则返回null</returns>
    public string? GetRecentProjectId()
    {
        // TODO: 暂时返回null，等UI层重构完成后实现
        return null;
    }

    /// <summary>
    /// 获取最近配方名称
    /// </summary>
    /// <returns>配方名称，如果没有则返回null</returns>
    public string? GetRecentRecipeName()
    {
        // TODO: 暂时返回null，等UI层重构完成后实现
        return null;
    }
}
