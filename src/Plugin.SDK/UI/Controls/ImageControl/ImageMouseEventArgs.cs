using System;
using System.Windows.Input;
using System.Windows;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 图像鼠标事件参数
    /// </summary>
    public class ImageMouseEventArgs : EventArgs
    {
        /// <summary>
        /// 图像坐标位置
        /// </summary>
        public Point ImagePosition { get; }

        /// <summary>
        /// 屏幕坐标位置
        /// </summary>
        public Point ScreenPosition { get; }

        /// <summary>
        /// 原始鼠标事件参数
        /// </summary>
        public MouseEventArgs OriginalEventArgs { get; }

        /// <summary>
        /// 是否已处理
        /// </summary>
        public bool Handled
        {
            get => OriginalEventArgs.Handled;
            set => OriginalEventArgs.Handled = value;
        }

        public ImageMouseEventArgs(Point imagePosition, Point screenPosition, MouseEventArgs originalEventArgs)
        {
            ImagePosition = imagePosition;
            ScreenPosition = screenPosition;
            OriginalEventArgs = originalEventArgs;
        }
    }
}
