using System;
using System.Windows.Media.Imaging;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 图像加载完成事件参数
    /// </summary>
    public class ImageLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// 加载的图像
        /// </summary>
        public BitmapSource? Image { get; }

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int Width => Image?.PixelWidth ?? 0;

        /// <summary>
        /// 图像高度
        /// </summary>
        public int Height => Image?.PixelHeight ?? 0;

        public ImageLoadedEventArgs(BitmapSource? image)
        {
            Image = image;
        }
    }
}
