using System;
using System.Windows.Threading;

namespace SunEyeVision.UI.Services.Toolbox
{
    /// <summary>
    /// 交互定时器管理器 - 处理延迟打开和关闭Popup的定时器
    /// </summary>
    public class ToolboxInteractionTimer : IDisposable
    {
        private readonly DispatcherTimer _openTimer;
        private readonly DispatcherTimer _closeTimer;

        public event EventHandler? OpenRequest;
        public event EventHandler? CloseRequest;

        /// <summary>
        /// 打开延迟（毫秒）- 默认200ms
        /// </summary>
        public double OpenDelay { get; set; } = 200;

        /// <summary>
        /// 关闭延迟（毫秒）- 默认300ms
        /// </summary>
        public double CloseDelay { get; set; } = 300;

        public ToolboxInteractionTimer()
        {
            _openTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(OpenDelay)
            };
            _openTimer.Tick += (s, e) =>
            {
                _openTimer.Stop();
                OpenRequest?.Invoke(this, EventArgs.Empty);
            };

            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(CloseDelay)
            };
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer.Stop();
                CloseRequest?.Invoke(this, EventArgs.Empty);
            };
        }

        /// <summary>
        /// 调度打开Popup
        /// </summary>
        public void ScheduleOpen()
        {
            // 取消关闭定时器，防止冲突
            _closeTimer.Stop();

            // 重置并启动打开定时�?
            _openTimer.Interval = TimeSpan.FromMilliseconds(OpenDelay);
            _openTimer.Stop();
            _openTimer.Start();
        }

        /// <summary>
        /// 调度关闭Popup
        /// </summary>
        public void ScheduleClose()
        {
            // 取消打开定时器，防止冲突
            _openTimer.Stop();

            // 重置并启动关闭定时器
            _closeTimer.Interval = TimeSpan.FromMilliseconds(CloseDelay);
            _closeTimer.Stop();
            _closeTimer.Start();
        }

        /// <summary>
        /// 取消所有定时器
        /// </summary>
        public void CancelAll()
        {
            _openTimer.Stop();
            _closeTimer.Stop();
        }

        /// <summary>
        /// 取消打开定时�?
        /// </summary>
        public void CancelOpen()
        {
            _openTimer.Stop();
        }

        /// <summary>
        /// 取消关闭定时�?
        /// </summary>
        public void CancelClose()
        {
            _closeTimer.Stop();
        }

        /// <summary>
        /// 立即触发打开（不经过定时器）
        /// </summary>
        public void TriggerOpenImmediately()
        {
            CancelAll();
            OpenRequest?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 立即触发关闭（不经过定时器）
        /// </summary>
        public void TriggerCloseImmediately()
        {
            CancelAll();
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _openTimer?.Stop();
            _closeTimer?.Stop();
            _openTimer.Tick -= (s, e) => { };
            _closeTimer.Tick -= (s, e) => { };
        }
    }
}
