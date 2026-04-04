using System;
using System.Windows;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow.Nodes.Strategies
{
    /// <summary>
    /// 默认节点打开策略 - 根据窗口类型创建对应窗口
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 处理所有未被特殊策略处理的节点
    /// 2. 根据窗口类型创建对应的窗口
    /// 3. 将调试控件放入窗口中并显示
    /// 
    /// 窗口类型：
    /// - Default: 标准窗口（有标题栏和边框）
    /// - Custom: 自定义窗口（无边框圆角窗口）
    /// </remarks>
    public class DefaultNodeOpenStrategy : INodeOpenStrategy
    {
        public bool CanHandle(NodeOpenContext context)
        {
            // 默认策略可以处理所有上下文
            return true;
        }

        public void Execute(NodeOpenContext context)
        {
            // 检查调试控件
            if (context.DebugControl == null)
            {
                VisionLogger.Instance.Log(LogLevel.Warning, 
                    "调试控件为空，无法创建窗口", 
                    "DefaultNodeOpenStrategy");
                return;
            }

            // 检查控件是否已经是 Window 类型
            if (context.DebugControl is Window existingWindow)
            {
                // 如果控件已经是窗口，直接使用
                VisionLogger.Instance.Log(LogLevel.Info, 
                    "调试控件已经是 Window 类型，直接使用", 
                    "DefaultNodeOpenStrategy");
                ShowWindow(existingWindow, context);
                return;
            }

            // 根据窗口类型创建窗口
            Window? window = null;
            switch (context.WindowStyle)
            {
                case DebugWindowStyle.Default:
                    window = CreateDefaultWindow(context);
                    break;

                case DebugWindowStyle.Custom:
                    window = CreateCustomWindow(context);
                    break;

                default:
                    VisionLogger.Instance.Log(LogLevel.Warning, 
                        $"未知的窗口类型: {context.WindowStyle}，使用默认窗口", 
                        "DefaultNodeOpenStrategy");
                    window = CreateDefaultWindow(context);
                    break;
            }

            // 显示窗口
            if (window != null)
            {
                ShowWindow(window, context);
            }
        }

        /// <summary>
        /// 创建标准窗口
        /// </summary>
        private Window CreateDefaultWindow(NodeOpenContext context)
        {
            var window = new Window
            {
                Title = context.Node.DispName,
                Content = context.DebugControl,
                Width = 400,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = context.MainWindow,
                ResizeMode = ResizeMode.CanResize,
                WindowState = WindowState.Normal
            };

            VisionLogger.Instance.Log(LogLevel.Success, 
                $"创建标准窗口成功: {window.Title}", 
                "DefaultNodeOpenStrategy");

            return window;
        }

        /// <summary>
        /// 创建自定义窗口（无边框圆角窗口）
        /// </summary>
        private Window CreateCustomWindow(NodeOpenContext context)
        {
            // TODO: 实现自定义窗口样式
            // 暂时使用标准窗口，后续可以实现无边框圆角窗口
            var window = new Window
            {
                Title = context.Node.DispName,
                Content = context.DebugControl,
                Width = 400,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = context.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = null
            };

            VisionLogger.Instance.Log(LogLevel.Success, 
                $"创建自定义窗口成功: {window.Title}", 
                "DefaultNodeOpenStrategy");

            return window;
        }

        /// <summary>
        /// 显示窗口
        /// </summary>
        private void ShowWindow(Window window, NodeOpenContext context)
        {
            try
            {
                // 设置窗口所有者
                if (context.MainWindow != null && window.Owner == null)
                {
                    window.Owner = context.MainWindow;
                }

                // 设置创建的窗口引用，返回给调用者
                context.CreatedWindow = window;

                // 显示窗口
                window.Show();

                VisionLogger.Instance.Log(LogLevel.Success, 
                    $"窗口已显示: {window.Title}", 
                    "DefaultNodeOpenStrategy");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error, 
                    $"窗口显示失败: {ex.Message}", 
                    "DefaultNodeOpenStrategy", ex);
            }
        }
    }
}
