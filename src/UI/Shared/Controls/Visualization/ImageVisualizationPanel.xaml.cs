using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Shared.Controls.Visualization
{
    /// <summary>
    /// 图像可视化面�?
    /// 用于显示图像处理结果
    /// </summary>
    public partial class ImageVisualizationPanel : UserControl
    {
        public ImageVisualizationPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 图像�?
        /// </summary>
        public BitmapSource ImageSource
        {
            get { return (BitmapSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(BitmapSource), typeof(ImageVisualizationPanel),
                new PropertyMetadata(null, OnImageSourceChanged));

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageVisualizationPanel)d;
            control.DisplayImage.Source = e.NewValue as BitmapSource;
        }

        /// <summary>
        /// 图像信息
        /// </summary>
        public string ImageInfo
        {
            get { return (string)GetValue(ImageInfoProperty); }
            set { SetValue(ImageInfoProperty, value); }
        }

        public static readonly DependencyProperty ImageInfoProperty =
            DependencyProperty.Register("ImageInfo", typeof(string), typeof(ImageVisualizationPanel),
                new PropertyMetadata(string.Empty, OnImageInfoChanged));

        private static void OnImageInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageVisualizationPanel)d;
            control.InfoTextBlock.Text = e.NewValue?.ToString() ?? string.Empty;
        }
    }
}
