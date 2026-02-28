using System;
using System.IO;
using System.Windows;
using SunEyeVision.Tool.ROIEditor.Views;

namespace SunEyeVision.Tool.ROIEditor
{
    /// <summary>
    /// ROI编辑器独立运行入口点
    /// </summary>
    public static class ROIEditorRunner
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application();
            var window = new ROIEditorToolDebugWindow();
            window.Initialize("standalone", null, null);
            window.Show();

            // 如果命令行参数提供了图像路径，自动加载
            if (args.Length > 0 && File.Exists(args[0]))
            {
                window.LoadLocalImage(args[0]);
            }

            app.Run(window);
        }
    }
}
