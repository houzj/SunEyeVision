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

namespace SunEyeVision.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    static App()
    {
        // 抑制 AIStudio.Wpf.DiagramDesigner 库内部的绑定警告
        // 这些警告不影响功能，来自库的默认模板
        // 只显�?Warning 及以上级别，不显�?Information 级别的绑定信�?
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // 设置控制台编码为UTF-8，解决中文乱码问�?
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // �?P0优化：预热线程池，消除首次加载延�?
        PrewarmThreadPool();

        base.OnStartup(e);

        // 添加全局异常处理
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 初始化服务（包括节点显示适配器）
        // Debug.WriteLine("[App] 正在初始化服�?..");
        ServiceInitializer.InitializeServices();
        // Debug.WriteLine("[App] �?服务初始化完�?);

        // 初始化插件管理器
        // Debug.WriteLine("[App] 正在初始化插件管理器...");
        var pluginManager = new PluginManager(); // 使用 Plugin.Infrastructure.PluginManager
        // 工具插件目录：plugins/（相对于应用程序运行目录�?
        string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        // Debug.WriteLine($"[App] 插件路径: {pluginsPath}");
        pluginManager.LoadPlugins(pluginsPath);
        // Debug.WriteLine("[App] �?插件管理器初始化完成");

        // 显示主窗�?
        // Debug.WriteLine("[App] 正在创建主窗�?..");
        var mainWindow = new MainWindow();
        mainWindow.Show();
        // Debug.WriteLine("[App] �?主窗口已显示");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        // Debug.WriteLine("=================================================");
        // Debug.WriteLine($"[App] �?全局未处理异�? {ex?.Message}");
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
                // Debug.WriteLine($"[App] 第二层内部异�? {ex.InnerException.InnerException.Message}");
                // Debug.WriteLine($"[App] 第二层内部异常类�? {ex.InnerException.InnerException.GetType().FullName}");
            }
        }

        // 保存到文�?
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
        // Debug.WriteLine($"[App] �?Dispatcher 未处理异�? {e.Exception.Message}");
        // Debug.WriteLine($"[App] 堆栈跟踪: {e.Exception.StackTrace}");

        if (e.Exception.InnerException != null)
        {
            // Debug.WriteLine($"[App] 内部异常: {e.Exception.InnerException.Message}");
            // Debug.WriteLine($"[App] 内部堆栈: {e.Exception.InnerException.StackTrace}");
        }

        e.Handled = true; // 防止应用程序崩溃
        MessageBox.Show($"UI 线程发生异常:\n{e.Exception.Message}",
            "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// �?P0优化：预热线程池 - 消除首次Task.Run的冷启动延迟
    /// 预期效果：首张缩略图加载�?1783ms �?~50ms
    /// </summary>
    private void PrewarmThreadPool()
    {
        try
        {
            // 设置最小线程数，确保线程池立即可用
            ThreadPool.GetMinThreads(out int minWorker, out int minIO);
            ThreadPool.SetMinThreads(
                Math.Max(minWorker, 8),  // 至少8个工作线�?
                Math.Max(minIO, 4)       // 至少4个IO线程
            );

            // 预热：启动几个空任务，强制线程池创建线程
            var warmupTasks = new Task[4];
            for (int i = 0; i < 4; i++)
            {
                warmupTasks[i] = Task.Run(() => Thread.Sleep(10));
            }
            Task.WaitAll(warmupTasks);

            Debug.WriteLine("[App] �?线程池预热完�?- 工作线程:8, IO线程:4");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] �?线程池预热失�? {ex.Message}");
        }
    }
}

