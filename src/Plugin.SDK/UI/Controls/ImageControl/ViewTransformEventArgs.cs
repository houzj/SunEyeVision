using System.Windows;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 视图变换事件参数
    /// </summary>
    public class ViewTransformEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// 当前缩放比例
        /// </summary>
        public double Zoom { get; }

        /// <summary>
        /// X偏移量
        /// </summary>
        public double OffsetX { get; }

        /// <summary>
        /// Y偏移量
        /// </summary>
        public double OffsetY { get; }

        public ViewTransformEventArgs(double zoom, double offsetX, double offsetY)
        {
            Zoom = zoom;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }

        public ViewTransformEventArgs(double zoom, double offsetX, double offsetY, RoutedEvent routedEvent)
            : base(routedEvent)
        {
            Zoom = zoom;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }
}
