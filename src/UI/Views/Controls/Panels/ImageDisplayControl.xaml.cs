using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Controls.Panels
{
    /// <summary>
    /// ImageDisplayControl.xaml 的交互逻辑
    /// </summary>
    public partial class ImageDisplayControl : UserControl
    {
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(BitmapSource), typeof(ImageDisplayControl),
                new PropertyMetadata(null, OnImageSourceChanged));

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(ImageDisplayControl),
                new PropertyMetadata(1.0, OnScaleChanged));

        public BitmapSource ImageSource
        {
            get => (BitmapSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public ICommand SelectOriginalCommand { get; }
        public ICommand SelectProcessedCommand { get; }
        public ICommand SelectResultCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand FitToWindowCommand { get; }
        public ICommand SaveImageCommand { get; }

        public ImageDisplayControl()
        {
            InitializeComponent();

            SelectOriginalCommand = new RelayCommand(SelectOriginal);
            SelectProcessedCommand = new RelayCommand(SelectProcessed);
            SelectResultCommand = new RelayCommand(SelectResult);
            ZoomInCommand = new RelayCommand(ZoomIn);
            ZoomOutCommand = new RelayCommand(ZoomOut);
            FitToWindowCommand = new RelayCommand(FitToWindow);
            SaveImageCommand = new RelayCommand(SaveImage);
        }

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageDisplayControl control)
            {
                control.MainImage.Source = e.NewValue as BitmapSource;
                control.UpdatePlaceholderVisibility();
            }
        }

        private static void OnScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageDisplayControl control)
            {
                control.UpdateTransform();
            }
        }

        private void UpdatePlaceholderVisibility()
        {
            PlaceholderText.Visibility = ImageSource == null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateTransform()
        {
            MainImage.RenderTransform = new ScaleTransform(Scale, Scale);
        }

        private void SelectOriginal()
        {
            // TODO: 切换到原图显示。
        }

        private void SelectProcessed()
        {
            // TODO: 切换到处理后图像显示
        }

        private void SelectResult()
        {
            // TODO: 切换到检测结果显示。
        }

        private void ZoomIn()
        {
            Scale = Math.Min(Scale * 1.2, 5.0);
        }

        private void ZoomOut()
        {
            Scale = Math.Max(Scale / 1.2, 0.2);
        }

        private void FitToWindow()
        {
            Scale = 1.0;
        }

        private void SaveImage()
        {
            if (ImageSource == null)
            {
                MessageBox.Show("没有可保存的图像", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG图片|*.png|JPEG图片|*.jpg|BMP图片|*.bmp|所有文件|*.*",
                    DefaultExt = ".png",
                    FileName = "image.png"
                };

                if (dialog.ShowDialog() == true)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(ImageSource));
                    using (var stream = System.IO.File.Create(dialog.FileName))
                    {
                        encoder.Save(stream);
                    }
                    MessageBox.Show("图像保存成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
