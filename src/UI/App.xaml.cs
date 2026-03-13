using System;
using System.Windows;
using System.IO;
using SunEyeVision.UI.Adapters;
using SunEyeVision.Plugin.Infrastructure.Managers.Plugin;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Data;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.UI.Views.Windows;
using System.Runtime.InteropServices;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // RPC_S_SERVER_UNAVAILABLE = 0x000006BA = 1722
    private const int RPC_S_SERVER_UNAVAILABLE = 1722;
    private const int HRESULT_RPC_UNAVAILABLE = unchecked((int)0x800706BA);
    
    static App()
    {
        // 抑制 AIStudio.Wpf.DiagramDesigner 库内部的绑定警告
        // 这些警告不影响功能，来自库的默认模板
        // 只显示Warning及以上级别，不显示Information级别的绑定信息
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // 设置控制台编码为UTF-8，解决中文乱码问题
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // P0优化：预热线程池，消除首次加载延迟
        PrewarmThreadPool();
        
        // 添加FirstChanceException处理 - 捕获RPC异常避免调试器中断
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

        base.OnStartup(e);

        // 添加全局异常处理
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 初始化服务（包括节点显示适配器）
        ServiceInitializer.InitializeServices();

        // 检查并配置解决方案路径
        var solutionsPath = GetOrConfigureSolutionsPath();
        if (string.IsNullOrEmpty(solutionsPath))
        {
            // 用户取消配置，退出应用
            Shutdown();
            return;
        }

        // 初始化项目管理器
        ServiceInitializer.InitializeProjectManager(solutionsPath);

        // 初始化插件管理器
        var pluginManager = new PluginManager();
        string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        pluginManager.LoadPlugins(pluginsPath);

        // 启动决策
        HandleStartupDecision();
    }

    /// <summary>
    /// 处理启动决策
    /// </summary>
    private void HandleStartupDecision()
    {
        var projectManager = ServiceInitializer.ProjectManager;
        var startupDecisionService = new StartupDecisionService(projectManager);
        var decision = startupDecisionService.GetStartupDecision();

        var mainWindow = new MainWindow();

        switch (decision)
        {
            case StartupDecision.LoadRecentAndStart:
                // 自动加载最近项目并启动
                var projectId = startupDecisionService.GetRecentProjectId();
                var recipeName = startupDecisionService.GetRecentRecipeName();

                if (!string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(recipeName))
                {
                    try
                    {
                        projectManager.SetCurrentProject(projectId, recipeName);
                        mainWindow.Show();
                        // Debug.WriteLine($"[App] 自动加载项目: {projectId}, 配方: {recipeName}");
                    }
                    catch (Exception ex)
                    {
                        // 加载失败，显示配置界面
                        // Debug.WriteLine($"[App] 加载项目失败: {ex.Message}");
                        ShowConfigurationDialog(mainWindow, projectId, recipeName);
                    }
                }
                else
                {
                    // 无有效项目，显示配置界面
                    ShowConfigurationDialog(mainWindow);
                }
                break;

            case StartupDecision.SkipConfiguration:
                // 跳过配置，直接进入主界面
                mainWindow.Show();
                break;

            case StartupDecision.ShowConfigurationWithEmptyState:
            case StartupDecision.ShowConfigurationWithRecentProject:
            case StartupDecision.ShowConfiguration:
            default:
                // 显示配置界面
                var preselectProjectId = decision == StartupDecision.ShowConfigurationWithRecentProject
                    ? startupDecisionService.GetRecentProjectId()
                    : null;
                var preselectRecipeName = decision == StartupDecision.ShowConfigurationWithRecentProject
                    ? startupDecisionService.GetRecentRecipeName()
                    : null;

                ShowConfigurationDialog(mainWindow, preselectProjectId, preselectRecipeName);
                break;
        }
    }

    /// <summary>
    /// 获取或配置解决方案路径
    /// </summary>
    private string? GetOrConfigureSolutionsPath()
    {
        var defaultPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solutions");

        // 检查RuntimeConfig中是否配置了路径
        var configPath = GetConfiguredSolutionsPath();
        if (!string.IsNullOrEmpty(configPath))
        {
            // 有配置的路径，直接使用
            return configPath;
        }

        // 没有配置，检查默认路径是否存在
        if (System.IO.Directory.Exists(defaultPath))
        {
            // 默认路径存在，使用它
            return defaultPath;
        }

        // 默认路径不存在，弹出路径配置对话框
        var pathDialog = new SolutionPathDialog(defaultPath);
        var result = pathDialog.ShowDialog();

        if (result == true && pathDialog.SelectedPath != null)
        {
            // 用户确认路径，保存配置
            SaveSolutionsPathConfig(pathDialog.SelectedPath);
            return pathDialog.SelectedPath;
        }

        // 用户取消，返回null（会导致应用退出）
        return null;
    }

    /// <summary>
    /// 获取配置的解决方案路径
    /// </summary>
    private string? GetConfiguredSolutionsPath()
    {
        try
        {
            var configFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solutions_config.json");
            if (!System.IO.File.Exists(configFilePath))
                return null;

            var json = System.IO.File.ReadAllText(configFilePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<SolutionsPathConfig>(json);
            return config?.SolutionsPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 保存解决方案路径配置
    /// </summary>
    private void SaveSolutionsPathConfig(string newPath)
    {
        try
        {
            // 保存新路径配置
            var config = new SolutionsPathConfig { SolutionsPath = newPath };
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize(config, options);

            var configFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "solutions_config.json");
            System.IO.File.WriteAllText(configFilePath, json);

            LogInfo($"保存解决方案路径配置: {newPath}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存路径配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    private void LogInfo(string message)
    {
        try
        {
            var logger = SunEyeVision.Plugin.SDK.Logging.VisionLogger.Instance;
            logger?.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info, message, "App");
        }
        catch
        {
            // 忽略日志错误
        }
    }

    /// <summary>
    /// 解决方案路径配置
    /// </summary>
    private class SolutionsPathConfig
    {
        public string SolutionsPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 显示配置对话框
    /// </summary>
    private void ShowConfigurationDialog(MainWindow mainWindow, string? preselectProjectId = null, string? preselectRecipeName = null)
    {
        var projectManager = ServiceInitializer.ProjectManager;
        var configDialog = new SolutionConfigurationDialog(projectManager, preselectProjectId, preselectRecipeName);

        var result = configDialog.ShowDialog();

        if (result == true && configDialog.IsLaunched && configDialog.LaunchResult.HasValue)
        {
            // 用户点击启动，加载项目和配方
            var (project, recipe) = configDialog.LaunchResult.Value;

            if (project != null && recipe != null)
            {
                try
                {
                    projectManager.SetCurrentProject(project.Id, recipe.Name);
                    mainWindow.Show();
                    // Debug.WriteLine($"[App] 加载项目: {project.Name}, 配方: {recipe.Name}");
                }
                catch (Exception ex)
                {
                    // Debug.WriteLine($"[App] 加载项目失败: {ex.Message}");
                    MessageBox.Show($"加载项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    mainWindow.Show();
                }
            }
            else
            {
                mainWindow.Show();
            }
        }
        else
        {
            // 用户点击跳过或取消
            mainWindow.Show();
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        // Debug.WriteLine("=================================================");
        // Debug.WriteLine($"[App] 全局未处理异常: {ex?.Message}");
        // Debug.WriteLine($"[App] 异常类型: {ex?.GetType().FullName}");
        // Debug.WriteLine($"[App] 堆栈跟踪:\n{ex?.StackTrace}");
        // Debug.WriteLine($"[App] 是否终止: {e.IsTerminating}");
        // Debug.WriteLine("=================================================");

        if (ex?.InnerException != null)
        {
            // Debug.WriteLine($"[App] 内部异常: {ex.InnerException.Message}");
            // Debug.WriteLine($"[App] 内部异常类型: {ex.InnerException.GetType().FullName}");
            // Debug.WriteLine($"[App] 内部堆栈:\n{ex.InnerException.StackTrace}");

            if (ex.InnerException.InnerException != null)
            {
                // Debug.WriteLine($"[App] 第二层内部异常: {ex.InnerException.InnerException.Message}");
                // Debug.WriteLine($"[App] 第二层内部异常类型: {ex.InnerException.InnerException.GetType().FullName}");
            }
        }

        // 保存到文件夹
        try
        {
            string crashLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
            string logContent = $"时间: {DateTime.Now}\n";
            logContent += $"异常: {ex?.Message}\n";
            logContent += $"类型: {ex?.GetType().FullName}\n";
            logContent += $"堆栈:\n{ex?.StackTrace}\n";
            if (ex?.InnerException != null)
            {
                logContent += $"\n内部异常: {ex.InnerException.Message}\n";
                logContent += $"内部堆栈:\n{ex.InnerException.StackTrace}\n";
            }
            File.WriteAllText(crashLog, logContent);
            // Debug.WriteLine($"[App] 崩溃日志已保存到: {crashLog}");
        }
        catch (Exception writeEx)
        {
            // Debug.WriteLine($"[App] 无法保存崩溃日志: {writeEx.Message}");
        }

        MessageBox.Show($"应用程序发生未处理的异常:\n{ex?.Message}\n\n堆栈跟踪:\n{ex?.StackTrace}\n\n详细日志已保存到 crash_log.txt",
            "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Debug.WriteLine($"[App] Dispatcher 未处理异常: {e.Exception.Message}");
        // Debug.WriteLine($"[App] 堆栈跟踪: {e.Exception.StackTrace}");

        if (e.Exception.InnerException != null)
        {
            // Debug.WriteLine($"[App] 内部异常: {e.Exception.InnerException.Message}");
            // Debug.WriteLine($"[App] 内部堆栈: {e.Exception.InnerException.StackTrace}");
        }

        // 记录到插件日志系统
        try
        {
            var logger = SunEyeVision.Plugin.SDK.Logging.PluginLogger.Logger;
            logger.Error($"[App] Dispatcher 未处理异常: {e.Exception.Message}", "App", e.Exception);
            logger.Error($"[App] 堆栈跟踪: {e.Exception.StackTrace}", "App");
            if (e.Exception.InnerException != null)
            {
                logger.Error($"[App] 内部异常: {e.Exception.InnerException.Message}", "App", e.Exception.InnerException);
                logger.Error($"[App] 内部堆栈: {e.Exception.InnerException.StackTrace}", "App");
            }
        }
        catch { }

        e.Handled = true; // 防止应用程序崩溃
        MessageBox.Show($"UI 线程发生异常:\n{e.Exception.Message}\n\n堆栈跟踪:\n{e.Exception.StackTrace}",
            "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// FirstChanceException处理 - 在异常被抛出但未被捕获时触发
    /// 用于捕获并标记RPC异常，避免调试器中断
    /// </summary>
    private void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        // 捕获RPC服务器不可用异常
        if (e.Exception is COMException comEx)
        {
            if (comEx.ErrorCode == HRESULT_RPC_UNAVAILABLE || (comEx.ErrorCode & 0xFFFF) == RPC_S_SERVER_UNAVAILABLE)
            {
                // 标记为已知的非致命RPC异常
                Debug.WriteLine($"[App] 捕获RPC异常(非致命): {comEx.Message}");
                // 异常会被解码器的降级逻辑处理，此处仅记录
            }
        }
        // 捕获WIC相关的内部异常
        else if (e.Exception is System.Reflection.TargetInvocationException tie && 
                 tie.InnerException is COMException innerComEx)
        {
            if (innerComEx.ErrorCode == HRESULT_RPC_UNAVAILABLE || (innerComEx.ErrorCode & 0xFFFF) == RPC_S_SERVER_UNAVAILABLE)
            {
                Debug.WriteLine($"[App] 捕获RPC内部异常(非致命): {innerComEx.Message}");
            }
        }
    }

    /// <summary>
    /// P0优化：预热线程池 - 消除首次Task.Run的冷启动延迟
    /// 预期效果：首张缩略图加载从1783ms降至~50ms
    /// </summary>
    private void PrewarmThreadPool()
    {
        try
        {
            // 设置最小线程数，确保线程池立即可用
            ThreadPool.GetMinThreads(out int minWorker, out int minIO);
            ThreadPool.SetMinThreads(
                Math.Max(minWorker, 8),  // 至少8个工作线程
                Math.Max(minIO, 4)       // 至少4个IO线程
            );

            // 预热：启动几个空任务，强制线程池创建线程
            var warmupTasks = new Task[4];
            for (int i = 0; i < 4; i++)
            {
                warmupTasks[i] = Task.Run(() => Thread.Sleep(10));
            }
            Task.WaitAll(warmupTasks);

            Debug.WriteLine("[App] 线程池预热完成 - 工作线程:8, IO线程:4");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] 线程池预热失败: {ex.Message}");
        }
    }
}
