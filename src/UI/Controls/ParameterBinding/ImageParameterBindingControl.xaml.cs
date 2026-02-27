using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.ParameterBinding
{
    /// <summary>
    /// 图像参数绑定控件
    /// </summary>
    /// <remarks>
    /// 专门用于图像类型参数的绑定控件，提供：
    /// 1. 缩略图预览
    /// 2. 下拉选择器
    /// 3. 数据源分组显示
    /// </remarks>
    public partial class ImageParameterBindingControl : UserControl
    {
        public ImageParameterBindingControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// ViewModel依赖属性
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(ImageParameterBindingViewModel),
                typeof(ImageParameterBindingControl),
                new PropertyMetadata(null, OnViewModelChanged));

        /// <summary>
        /// ViewModel
        /// </summary>
        public ImageParameterBindingViewModel? ViewModel
        {
            get => (ImageParameterBindingViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// 是否显示缩略图预览依赖属性
        /// </summary>
        public static readonly DependencyProperty ShowThumbnailPreviewProperty =
            DependencyProperty.Register(
                nameof(ShowThumbnailPreview),
                typeof(bool),
                typeof(ImageParameterBindingControl),
                new PropertyMetadata(true));

        /// <summary>
        /// 是否显示缩略图预览
        /// </summary>
        public bool ShowThumbnailPreview
        {
            get => (bool)GetValue(ShowThumbnailPreviewProperty);
            set => SetValue(ShowThumbnailPreviewProperty, value);
        }

        /// <summary>
        /// 是否只读依赖属性
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(ImageParameterBindingControl),
                new PropertyMetadata(false));

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion

        #region 私有方法

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageParameterBindingControl control)
            {
                control.DataContext = e.NewValue;
            }
        }

        #endregion
    }
}
