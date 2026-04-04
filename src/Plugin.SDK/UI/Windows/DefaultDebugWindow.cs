using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Windows
{
    /// <summary>
    /// 普通窗口壳 - 包装工具调试控件为标准Windows窗口
    /// </summary>
    /// <remarks>
    /// 架构说明：
    /// - 轻量级窗口壳，仅提供窗口容器功能
    /// - 接受任何 UserControl 作为内容
    /// - 适用于大多数工具调试场景
    /// 
    /// 使用场景：
    /// - 大部分标准工具（阈值、滤波、定位等）
    /// - 需要标准窗口样式的工具
    /// 
    /// 不适用场景：
    /// - 需要无边框窗口的工具（使用 CustomDebugWindow）
    /// </remarks>
    public class DefaultDebugWindow : Window
    {
        #region 字段

        private readonly UserControl _control;

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建普通窗口壳
        /// </summary>
        /// <param name="control">工具调试控件实例</param>
        public DefaultDebugWindow(UserControl control)
        {
            _control = control;

            PluginLogger.Info("DefaultDebugWindow 构造函数开始", "DefaultDebugWindow");

            // 设置窗口属性
            Title = "工具调试窗口";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;
            
            // 使用项目样式系统
            Background = (Brush)FindResource("SurfaceBrush");

            // 设置窗口图标（如果有）
            try
            {
                Icon = CreateWindowIcon();
            }
            catch
            {
                // 图标加载失败不影响窗口创建
                PluginLogger.Warning("窗口图标加载失败", "DefaultDebugWindow");
            }

            // 将控件设置为窗口内容
            Content = _control;

            // 尝试订阅确认按钮点击事件（如果控件有）
            SubscribeConfirmEvent(_control);

            // 窗口关闭时清理
            Closed += OnWindowClosed;

            PluginLogger.Success("DefaultDebugWindow 构造函数完成", "DefaultDebugWindow");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 订阅确认按钮事件
        /// </summary>
        private void SubscribeConfirmEvent(UserControl control)
        {
            // 使用反射查找 ConfirmClicked 事件
            var eventType = control.GetType().GetEvent("ConfirmClicked");
            if (eventType != null)
            {
                try
                {
                    var handler = Delegate.CreateDelegate(eventType.EventHandlerType, this, nameof(OnConfirmClicked));
                    eventType.AddEventHandler(control, handler);
                    PluginLogger.Info("已订阅 ConfirmClicked 事件", "DefaultDebugWindow");
                }
                catch (Exception ex)
                {
                    PluginLogger.Warning($"订阅 ConfirmClicked 事件失败: {ex.Message}", "DefaultDebugWindow");
                }
            }
        }

        /// <summary>
        /// 创建窗口图标
        /// </summary>
        private ImageSource? CreateWindowIcon()
        {
            // TODO: 从资源加载窗口图标
            // 暂时返回 null，后续可以添加图标资源
            return null;
        }

        /// <summary>
        /// 确认按钮点击事件处理
        /// </summary>
        private void OnConfirmClicked(object? sender, EventArgs e)
        {
            PluginLogger.Info("确认按钮已点击，关闭窗口", "DefaultDebugWindow");
            Close();
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            // 清理事件绑定
            if (_control != null)
            {
                var eventType = _control.GetType().GetEvent("ConfirmClicked");
                if (eventType != null)
                {
                    try
                    {
                        var handler = Delegate.CreateDelegate(eventType.EventHandlerType, this, nameof(OnConfirmClicked));
                        eventType.RemoveEventHandler(_control, handler);
                    }
                    catch
                    {
                        // 忽略清理失败
                    }
                }
            }

            PluginLogger.Info("窗口已关闭，事件已清理", "DefaultDebugWindow");
        }

        #endregion
    }
}
