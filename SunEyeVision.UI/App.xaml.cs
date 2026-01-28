using System.Windows;
using SunEyeVision.UI.Adapters;
using SunEyeVision.Core.Services;

namespace SunEyeVision.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化服务（包括节点显示适配器）
        ServiceInitializer.InitializeServices();

        // 初始化插件管理器
        var pluginManager = new PluginManager();
        pluginManager.LoadPlugins(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"));

        // 显示主窗口
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}

