using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// 帮助窗口 - 支持显示内置内容和外�?HTML 帮助文档
    /// </summary>
    public partial class HelpWindow : Window
    {
        private readonly string _helpDirectory;

        public HelpWindow()
        {
            InitializeComponent();

            // 获取帮助文档目录 - 优先使用构建输出目录
            _helpDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help");
        }

        /// <summary>
        /// 窗口加载时显示内置帮助内�?
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口保持打开，显示内置的帮助内容
            // 用户可以通过按钮打开外部浏览器查看完整文�?
        }

        /// <summary>
        /// 打开完整文档按钮点击事件
        /// </summary>
        private void OpenFullDocsButton_Click(object sender, RoutedEventArgs e)
        {
            // 首先尝试相对于可执行文件的路�?
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string indexPath = Path.Combine(basePath, "Help", "index.html");

            // 如果文件不存在，尝试相对于解决方案的路径
            if (!File.Exists(indexPath))
            {
                // 从代码目录查�?
                string solutionDir = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ".."));
                indexPath = Path.Combine(solutionDir, "Help", "Output", "index.html");
            }

            // 调试信息
            string message = $"基础目录: {basePath}\n尝试路径: {indexPath}\n文件存在: {File.Exists(indexPath)}";

            if (File.Exists(indexPath))
            {
                try
                {
                    // 使用默认浏览器打开 HTML 文档
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = indexPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开帮助文档:\n{ex.Message}\n\n{message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"帮助文档未找�?\n{indexPath}\n\n请确�?Help 文件夹存在且包含帮助文档。\n\n{message}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 加载帮助内容
        /// </summary>
        private void LoadHelpContent()
        {
            // 尝试加载 CHM 文件
            string chmPath = Path.Combine(_helpDirectory, "SunEyeVision.chm");

            if (File.Exists(chmPath))
            {
                // 使用 CHM 文件
                ShowChmHelp(chmPath);
            }
            else
            {
                // 使用 HTML 文件
                ShowHtmlHelp();
            }
        }

        /// <summary>
        /// 显示 CHM 帮助文档
        /// </summary>
        private void ShowChmHelp(string chmPath)
        {
            try
            {
                // 使用 Windows Help API
                var psi = new ProcessStartInfo
                {
                    FileName = "hh.exe",
                    Arguments = chmPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
                this.Close(); // 关闭窗口,CHM 会独立打开
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开 CHM 帮助文档: {ex.Message}\n将使�?HTML 文档代替�?, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShowHtmlHelp();
            }
        }

        /// <summary>
        /// 显示 HTML 帮助文档
        /// </summary>
        private void ShowHtmlHelp()
        {
            string indexPath = Path.Combine(_helpDirectory, "index.html");

            if (File.Exists(indexPath))
            {
                try
                {
                    // 使用默认浏览器打开 HTML 文档
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = indexPath,
                        UseShellExecute = true
                    });

                    this.Close(); // 关闭窗口,HTML 会在浏览器中打开
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开 HTML 帮助文档: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowFallbackHelp();
                }
            }
            else
            {
                ShowFallbackHelp();
            }
        }

        /// <summary>
        /// 显示备用帮助信息(当文档文件不存在�?
        /// </summary>
        private void ShowFallbackHelp()
        {
            // 显示内置的帮助内�?
            HelpTitle.Text = "SunEyeVision 帮助";

            string fallbackContent = GetFallbackContent();
            HelpContent.Inlines.Clear();
            HelpContent.Inlines.Add(new System.Windows.Documents.Run(fallbackContent));
        }

        /// <summary>
        /// 获取备用帮助内容
        /// </summary>
        private string GetFallbackContent()
        {
            return "SunEyeVision 帮助文档\n\n" +
                   "欢迎使用 SunEyeVision 机器视觉算法平台!\n\n" +
                   "帮助文档功能:\n" +
                   "- �?F1 键快速打开帮助\n" +
                   "- 支持 CHM �?HTML 格式\n" +
                   "- 文档位于 Help/Output 目录\n\n" +
                   "帮助内容:\n" +
                   "- 用户手册 - 软件使用指南\n" +
                   "- 软件架构 - 系统设计和结构说明\n" +
                   "- 开发计�?- 未来发展方向\n" +
                   "- 开发进�?- 当前开发状态\n" +
                   "- API 文档 - 开发者参考\n\n" +
                   "注意: 当前帮助文档文件未找到。\n" +
                   "请运�?tools\\GenerateHelpDocumentation.ps1 脚本生成帮助文档。\n\n" +
                   "技术支�? support@suneyevision.com";
        }

        /// <summary>
        /// 显示指定的帮助主�?
        /// </summary>
        public void ShowTopic(string topic)
        {
            string chmPath = Path.Combine(_helpDirectory, "SunEyeVision.chm");

            if (File.Exists(chmPath))
            {
                try
                {
                    // 使用 Windows Help API 显示特定主题
                    var psi = new ProcessStartInfo
                    {
                        FileName = "hh.exe",
                        Arguments = $"\"{chmPath}::{topic}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch
                {
                    ShowHtmlHelp();
                }
            }
            else
            {
                ShowHtmlHelp();
            }
        }

        /// <summary>
        /// 按关键字搜索帮助
        /// </summary>
        public void SearchHelp(string keyword)
        {
            string chmPath = Path.Combine(_helpDirectory, "SunEyeVision.chm");

            if (File.Exists(chmPath))
            {
                try
                {
                    // 使用 Windows Help API 搜索
                    var psi = new ProcessStartInfo
                    {
                        FileName = "hh.exe",
                        Arguments = $"\"{chmPath}\"::/Keyword,{keyword}",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch
                {
                    ShowHtmlHelp();
                }
            }
            else
            {
                ShowHtmlHelp();
            }
        }
    }
}
