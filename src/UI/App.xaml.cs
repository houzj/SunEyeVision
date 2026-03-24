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
using SunEyeVision.UI.Services;
using SunEyeVision.UI.Services.Monitoring;

namespace SunEyeVision.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // RPC_S_SERVER_UNAVAILABLE = 0x000006BA = 1722
    private const int RPC_S_SERVER_UNAVAILABLE = 1722;
    private const int HRESULT_RPC_UNAVAILABLE = unchecked((int)0x800706BA);

    // 监控定时器
    private DispatcherTimer? _monitorTimer;
    private int _monitorReportInterval = 30; // 每30秒报告一次

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

        // 注册文件关联（仅当前用户，无需管理员权限）
        RegisterFileAssociation();

        // 初始化服务（包括节点显示适配器）
        ServiceInitializer.InitializeServices();

        // 使用默认路径
        var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // 初始化解决方案管理器
        ServiceInitializer.InitializeSolutionManager(defaultPath);

        // 初始化插件管理器
        var pluginManager = new PluginManager();
        // 插件路径: 相对于主程序目录下的 plugins/ 子目录
        string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        pluginManager.LoadPlugins(pluginsPath);

        // 初始化DispatcherTimer监控
        InitializeMonitoring();

        Debug.WriteLine("[App] 监控系统已初始化");

        // 启动决策
        HandleStartupDecision();
    }

    /// <summary>
    /// 处理启动决策
    /// </summary>
    private void HandleStartupDecision()
    {
        var solutionManager = ServiceInitializer.SolutionManager;
        var startupDecisionService = new StartupDecisionService(solutionManager);
        var decision = startupDecisionService.GetStartupDecision();

        var mainWindow = new MainWindow();

        switch (decision)
        {
            case StartupDecision.SkipConfiguration:
                // 跳过配置，自动打开默认解决方案
                var logger = SunEyeVision.Plugin.SDK.Logging.VisionLogger.Instance;
                logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                    "启动决策: 跳过配置，准备加载默认解决方案", "App");
                
                try
                {
                    var defaultMetadata = solutionManager.GetDefaultSolutionMetadata();
                    if (defaultMetadata != null)
                    {
                        logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                            $"找到默认解决方案元数据: Id={defaultMetadata.Id}, Name={defaultMetadata.Name}, FilePath={defaultMetadata.FilePath}", "App");
                        
                        // 使用 LoadSolutionOnly 加载解决方案
                        logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                            "开始加载解决方案 (LoadSolutionOnly)...", "App");
                        
                        var solution = solutionManager.LoadSolutionOnly(defaultMetadata.FilePath);
                        
                        if (solution != null)
                        {
                            logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Success,
                                $"解决方案加载成功: Id={solution.Id}, Name={solution.Name}, Workflows={solution.Workflows.Count}, GlobalVariables={solution.GlobalVariables?.Count ?? 0}, Devices={solution.Devices?.Count ?? 0}", "App");
                            
                            // 调用 SetCurrentSolution 设置当前解决方案
                            // 这与配置对话框启动的流程保持一致
                            logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                                "开始设置当前解决方案 (SetCurrentSolution)...", "App");
                            
                            solutionManager.SetCurrentSolution(solution);
                            
                            logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Success,
                                $"当前解决方案设置完成: CurrentSolution.Id={solutionManager.CurrentSolution?.Id}, CurrentFilePath={solutionManager.CurrentFilePath}", "App");
                            
                            mainWindow.Show();
                            logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Success,
                                $"主窗口已显示，自动打开默认解决方案完成: {defaultMetadata.Name}", "App");
                        }
                        else
                        {
                            // 默认解决方案加载失败，记录警告并显示主界面
                            logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Error,
                                $"默认解决方案加载失败: {defaultMetadata.Name} (LoadSolutionOnly 返回 null)", "App");
                            mainWindow.Show();
                        }
                    }
                    else
                    {
                        // 没有设置默认解决方案，直接显示主界面
                        logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Warning,
                            "未找到默认解决方案元数据，将显示空白主界面", "App");
                        mainWindow.Show();
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Error,
                        $"自动打开默认解决方案异常: {ex.Message}\n堆栈: {ex.StackTrace}", "App", ex);
                    MessageBox.Show($"自动打开默认解决方案失败: {ex.Message}",
                        "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    mainWindow.Show();
                }
                break;

            case StartupDecision.ShowConfiguration:
            default:
                // 显示配置界面（无预选）
                ShowConfigurationDialog(mainWindow, null);
                break;
        }
    }

    /// <summary>
    /// 显示配置对话框
    /// </summary>
    private void ShowConfigurationDialog(MainWindow mainWindow, string? preselectSolutionId = null)
    {
        var logger = SunEyeVision.Plugin.SDK.Logging.VisionLogger.Instance;
        logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
            "启动决策: 显示配置对话框", "App");
        
        var solutionManager = ServiceInitializer.SolutionManager;
        var configDialog = new SolutionConfigurationDialog(solutionManager, preselectSolutionId);

        var result = configDialog.ShowDialog();

        if (result == true && configDialog.IsLaunched && configDialog.LaunchResult != null)
        {
            // 用户点击启动，加载解决方案
            var solution = configDialog.LaunchResult;

            if (solution != null)
            {
                logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                    $"用户从配置对话框启动解决方案: Id={solution.Id}, Name={solution.Name}, Workflows={solution.Workflows.Count}, GlobalVariables={solution.GlobalVariables?.Count ?? 0}, Devices={solution.Devices?.Count ?? 0}", "App");
                
                try
                {
                    logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                        "开始设置当前解决方案 (SetCurrentSolution)...", "App");
                    
                    solutionManager.SetCurrentSolution(solution);
                    
                    logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Success,
                        $"当前解决方案设置完成: CurrentSolution.Id={solutionManager.CurrentSolution?.Id}, CurrentFilePath={solutionManager.CurrentFilePath}", "App");
                    
                    mainWindow.Show();
                    logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Success,
                        "主窗口已显示，从配置对话框启动完成", "App");
                }
                catch (Exception ex)
                {
                    logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Error,
                        $"设置当前解决方案失败: {ex.Message}\n堆栈: {ex.StackTrace}", "App", ex);
                    MessageBox.Show($"加载解决方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    mainWindow.Show();
                }
            }
            else
            {
                logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Warning,
                    "配置对话框返回的 LaunchResult 为 null", "App");
                mainWindow.Show();
            }
        }
        else
        {
            // 用户点击跳过或取消
            logger.Log(SunEyeVision.Plugin.SDK.Logging.LogLevel.Info,
                "用户跳过或取消配置对话框", "App");
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
        Debug.WriteLine($"[App] Dispatcher 未处理异常: {e.Exception.Message}");
        Debug.WriteLine($"[App] 异常类型: {e.Exception.GetType().FullName}");
        Debug.WriteLine($"[App] 堆栈跟踪: {e.Exception.StackTrace}");

        if (e.Exception.InnerException != null)
        {
            Debug.WriteLine($"[App] 内部异常: {e.Exception.InnerException.Message}");
            Debug.WriteLine($"[App] 内部异常类型: {e.Exception.InnerException.GetType().FullName}");
            Debug.WriteLine($"[App] 内部堆栈: {e.Exception.InnerException.StackTrace}");
        }

        // 特别处理TargetParameterCountException
        if (e.Exception is System.Reflection.TargetParameterCountException)
        {
            Debug.WriteLine($"[App] *** 这是一个参数数量不匹配异常 ***");
            Debug.WriteLine($"[App] 可能的原因:");
            Debug.WriteLine($"[App]   1. Dispatcher.BeginInvoke参数顺序错误");
            Debug.WriteLine($"[App]   2. 事件处理器签名不匹配");
            Debug.WriteLine($"[App]   3. 委托调用时参数数量错误");

            // 捕获完整的调用堆栈
            Debug.WriteLine($"[App] 完整调用堆栈:");
            Debug.WriteLine(Environment.StackTrace);

            // 记录到文件以便详细分析
            try
            {
                string diagLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parameter_mismatch_detailed.log");
                string logContent = $"\n\n=== Parameter Mismatch Detail ===\n";
                logContent += $"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n";
                logContent += $"异常: {e.Exception.Message}\n";
                logContent += $"异常类型: {e.Exception.GetType().FullName}\n";
                logContent += $"\n异常堆栈:\n{e.Exception.StackTrace}\n";
                logContent += $"\n完整调用堆栈:\n{Environment.StackTrace}\n";
                if (e.Exception.InnerException != null)
                {
                    logContent += $"\n内部异常: {e.Exception.InnerException.Message}\n";
                    logContent += $"内部异常类型: {e.Exception.InnerException.GetType().FullName}\n";
                    logContent += $"内部堆栈:\n{e.Exception.InnerException.StackTrace}\n";
                }
                logContent += $"===================================\n\n";
                File.AppendAllText(diagLog, logContent);
                Debug.WriteLine($"[App] 详细日志已保存到: {diagLog}");
            }
            catch (Exception writeEx)
            {
                Debug.WriteLine($"[App] 保存详细日志失败: {writeEx.Message}");
            }
        }

        // 记录到插件日志系统
        try
        {
            var logger = SunEyeVision.Plugin.SDK.Logging.PluginLogger.Logger;
            logger.Error($"[App] Dispatcher 未处理异常: {e.Exception.Message}", "App", e.Exception);
            logger.Error($"[App] 异常类型: {e.Exception.GetType().FullName}", "App");
            logger.Error($"[App] 堆栈跟踪: {e.Exception.StackTrace}", "App");
            if (e.Exception.InnerException != null)
            {
                logger.Error($"[App] 内部异常: {e.Exception.InnerException.Message}", "App", e.Exception.InnerException);
                logger.Error($"[App] 内部异常类型: {e.Exception.InnerException.GetType().FullName}", "App");
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
        // 捕获参数数量不匹配异常
        if (e.Exception is System.Reflection.TargetParameterCountException)
        {
            var ex = e.Exception as System.Reflection.TargetParameterCountException;
            Debug.WriteLine($"[App] 捕获TargetParameterCountException(参数数量不匹配): {ex?.Message}");
            Debug.WriteLine($"[App] 异常堆栈: {ex?.StackTrace}");
            
            // 捕获完整调用堆栈
            Debug.WriteLine($"[App] 完整调用堆栈:");
            Debug.WriteLine(Environment.StackTrace);
            
            // 记录到文件以便详细分析
            try
            {
                string diagLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "parameter_mismatch_detailed.log");
                string logContent = $"\n\n=== FirstChanceException - TargetParameterCountException ===\n";
                logContent += $"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n";
                logContent += $"异常: {ex?.Message}\n";
                logContent += $"异常类型: {ex?.GetType().FullName}\n";
                logContent += $"\n异常堆栈:\n{ex?.StackTrace}\n";
                logContent += $"\n完整调用堆栈:\n{Environment.StackTrace}\n";
                if (ex?.InnerException != null)
                {
                    logContent += $"\n内部异常: {ex.InnerException.Message}\n";
                    logContent += $"内部异常类型: {ex.InnerException.GetType().FullName}\n";
                    logContent += $"内部堆栈:\n{ex.InnerException.StackTrace}\n";
                }
                logContent += $"===================================\n\n";
                File.AppendAllText(diagLog, logContent);
                Debug.WriteLine($"[App] 详细日志已保存到: {diagLog}");
            }
            catch (Exception writeEx)
            {
                Debug.WriteLine($"[App] 保存详细日志失败: {writeEx.Message}");
            }
        }
        // 捕获RPC服务器不可用异常
        else if (e.Exception is COMException comEx)
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

    /// <summary>
    /// 注册文件关联
    /// </summary>
    private void RegisterFileAssociation()
    {
        try
        {
            var fileAssociationService = new FileAssociationService();
            if (!fileAssociationService.IsRegistered())
            {
                fileAssociationService.RegisterFileAssociation();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] 文件关联注册失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化监控
    /// </summary>
    private void InitializeMonitoring()
    {
        try
        {
            Debug.WriteLine("[App] 初始化DispatcherTimer监控...");
            var monitor = DispatcherTimerMonitor.Instance;

            // 创建监控报告定时器
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_monitorReportInterval)
            };
            _monitorTimer.Tick += (sender, e) =>
            {
                Debug.WriteLine("[App] === DispatcherTimer 监控报告 ===");
                monitor.PrintAllTimersInfo();
            };
            _monitorTimer.Start();

            Debug.WriteLine($"[App] DispatcherTimer监控已启动，报告间隔: {_monitorReportInterval}秒");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] 初始化监控失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用程序退出时清理
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        // 保存用户设置
        var solutionManager = ServiceInitializer.SolutionManager;
        solutionManager?.SaveUserSettings();

        // 停止监控定时器
        _monitorTimer?.Stop();

        // 生成最终的监控报告
        try
        {
            Debug.WriteLine("[App] === 应用程序退出 - 最终监控报告 ===");
            DispatcherTimerMonitor.Instance.PrintAllTimersInfo();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] 生成最终监控报告失败: {ex.Message}");
        }

        base.OnExit(e);
    }
}

