using System.Windows;

namespace SunEyeVision.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 显示主窗口
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}

