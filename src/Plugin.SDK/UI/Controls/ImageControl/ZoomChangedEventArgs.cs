using System;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 缩放变更事件参数
    /// </summary>
    public class ZoomChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 新的缩放比例
        /// </summary>
        public double Zoom { get; }

        /// <summary>
        /// 缩放百分比
        /// </summary>
        public double ZoomPercent => Zoom * 100;

        public ZoomChangedEventArgs(double zoom)
        {
            Zoom = zoom;
        }
    }
}
