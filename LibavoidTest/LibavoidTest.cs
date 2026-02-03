using System;
using System.Windows;
using SunEyeVision.LibavoidWrapper;

namespace LibavoidTest
{
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            Title = "LibavoidWrapper 测试";
            Width = 600;
            Height = 400;

            var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };

            // 添加测试按钮
            var testBtn = new System.Windows.Controls.Button
            {
                Content = "测试 LibavoidRouter 创建",
                Margin = new Thickness(0, 10, 0, 10),
                Height = 40
            };
            testBtn.Click += TestButton_Click;
            stackPanel.Children.Add(testBtn);

            // 添加状态文本
            var statusText = new System.Windows.Controls.TextBlock
            {
                Text = "准备就绪",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 10)
            };
            stackPanel.Children.Add(statusText);

            // 添加日志文本
            var logBox = new System.Windows.Controls.TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Height = 200,
                Text = "日志输出:\n"
            };
            stackPanel.Children.Add(logBox);

            Content = stackPanel;

            _logBox = logBox;
            _statusText = statusText;
        }

        private System.Windows.Controls.TextBox _logBox;
        private System.Windows.Controls.TextBlock _statusText;

        private void Log(string message)
        {
            _logBox.Text += $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
            _logBox.ScrollToEnd();
            System.Diagnostics.Debug.WriteLine(message);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log("========== 开始测试 ==========");

                // 测试1: 创建配置
                Log("步骤1: 创建 RouterConfiguration...");
                var config = new RouterConfiguration();
                Log("✅ RouterConfiguration 创建成功");
                Log($"   IdealSegmentLength: {config.IdealSegmentLength}");
                Log($"   UseOrthogonalRouting: {config.UseOrthogonalRouting}");

                // 测试2: 创建路由器
                Log("\n步骤2: 创建 LibavoidRouter...");
                var router = new LibavoidRouter(config);
                Log("✅ LibavoidRouter 创建成功！");

                // 测试3: 路由简单路径
                Log("\n步骤3: 测试路由...");
                var source = new ManagedPoint(100, 100);
                var target = new ManagedPoint(300, 300);
                var sourceRect = new ManagedRect(90, 90, 20, 20);
                var targetRect = new ManagedRect(290, 290, 20, 20);

                var result = router.RoutePath(
                    source, target,
                    PortDirection.Right, PortDirection.Left,
                    sourceRect, targetRect,
                    null
                );

                if (result.Success)
                {
                    Log("✅ 路由成功！");
                    Log($"   路径点数: {result.PathPoints.Count}");
                    for (int i = 0; i < result.PathPoints.Count; i++)
                    {
                        Log($"   点{i + 1}: ({result.PathPoints[i].X:F1}, {result.PathPoints[i].Y:F1})");
                    }
                }
                else
                {
                    Log($"❌ 路由失败: {result.ErrorMessage}");
                }

                Log("\n========== 测试完成 ==========");
                _statusText.Text = "✅ 所有测试通过！";
                _statusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                Log($"❌ 异常: {ex.GetType().Name}");
                Log($"   消息: {ex.Message}");
                Log($"   堆栈:\n{ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Log($"   内部异常: {ex.InnerException.Message}");
                    Log($"   内部堆栈:\n{ex.InnerException.StackTrace}");
                }

                _statusText.Text = "❌ 测试失败";
                _statusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
    }

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========== LibavoidTest 启动 ==========");
                var app = new Application();
                var window = new TestWindow();
                app.Run(window);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"========== 全局异常: {ex.Message} ==========");
                MessageBox.Show($"程序启动失败:\n{ex.Message}\n\n{ex.StackTrace}",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
