using System;
using System.Windows;
using System.IO;
using SunEyeVision.UI.Adapters;
using SunEyeVision.Core.Services;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Data;

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
        // 只显示 Warning 及以上级别，不显示 Information 级别的绑定信息
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 添加全局异常处理
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 初始化服务（包括节点显示适配器）
        // Debug.WriteLine("[App] 正在初始化服务...");
        ServiceInitializer.InitializeServices();
        // Debug.WriteLine("[App] ✅ 服务初始化完成");

        // 初始化插件管理器
        // Debug.WriteLine("[App] 正在初始化插件管理器...");
        var pluginManager = new PluginManager();
        string pluginsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        // Debug.WriteLine($"[App] 插件路径: {pluginsPath}");
        pluginManager.LoadPlugins(pluginsPath);
        // Debug.WriteLine("[App] ✅ 插件管理器初始化完成");

        // 显示主窗口
        // Debug.WriteLine("[App] 正在创建主窗口...");
        var mainWindow = new MainWindow();
        mainWindow.Show();
        // Debug.WriteLine("[App] ✅ 主窗口已显示");
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        // Debug.WriteLine("=================================================");
        // Debug.WriteLine($"[App] ❌ 全局未处理异常: {ex?.Message}");
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

        // 保存到文件
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
        // Debug.WriteLine($"[App] ❌ Dispatcher 未处理异常: {e.Exception.Message}");
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
}

