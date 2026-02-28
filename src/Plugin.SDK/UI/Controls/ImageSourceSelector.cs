using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 图像源信息 - 用于表示一个可选的图像输出端口
    /// </summary>
    public class ImageSourceInfo
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName { get; set; } = string.Empty;

        /// <summary>
        /// 输出端口名称
        /// </summary>
        public string OutputPortName { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（格式：节点名称.端口名称）
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(OutputPortName) 
            ? NodeName 
            : $"{NodeName}.{OutputPortName}";

        /// <summary>
        /// 缩略图预览（可选）
        /// </summary>
        public BitmapSource? Thumbnail { get; set; }

        /// <summary>
        /// 图像宽度
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// 图像高度
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// 尺寸描述（格式：宽度x高度）
        /// </summary>
        public string SizeDescription => ImageWidth > 0 && ImageHeight > 0 
            ? $"{ImageWidth}x{ImageHeight}" 
            : string.Empty;

        /// <summary>
        /// 是否为空图像源
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(NodeId);
    }

    /// <summary>
    /// 图像源选择器 - 显示所有父节点的图像输出供选择
    /// </summary>
    /// <remarks>
    /// 用于工作流中选择输入图像来源。显示当前节点所有上游节点的图像输出端口，
    /// 支持缩略图预览和图像尺寸信息显示。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ImageSourceSelector Label="输入图像"
    ///     SelectedImageSource="{Binding InputImageSource}"
    ///     AvailableImageSources="{Binding ParentImageSources}" /&gt;
    /// </code>
    /// </remarks>
    public class ImageSourceSelector : Control
    {
        static ImageSourceSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageSourceSelector),
                new FrameworkPropertyMetadata(typeof(ImageSourceSelector)));
        }

        #region 依赖属性

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ImageSourceSelector),
                new PropertyMetadata("图像源"));

        public static readonly DependencyProperty SelectedImageSourceProperty =
            DependencyProperty.Register(nameof(SelectedImageSource), typeof(ImageSourceInfo), typeof(ImageSourceSelector),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedImageSourceChanged));

        public static readonly DependencyProperty AvailableImageSourcesProperty =
            DependencyProperty.Register(nameof(AvailableImageSources), typeof(ObservableCollection<ImageSourceInfo>), typeof(ImageSourceSelector),
                new PropertyMetadata(null, OnAvailableImageSourcesChanged));

        public static readonly DependencyProperty ShowThumbnailProperty =
            DependencyProperty.Register(nameof(ShowThumbnail), typeof(bool), typeof(ImageSourceSelector),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ShowSizeInfoProperty =
            DependencyProperty.Register(nameof(ShowSizeInfo), typeof(bool), typeof(ImageSourceSelector),
                new PropertyMetadata(true));

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(ImageSourceSelector),
                new PropertyMetadata("选择图像源..."));

        #endregion

        #region 属性

        /// <summary>
        /// 标签文本
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// 当前选中的图像源
        /// </summary>
        public ImageSourceInfo SelectedImageSource
        {
            get => (ImageSourceInfo)GetValue(SelectedImageSourceProperty);
            set => SetValue(SelectedImageSourceProperty, value);
        }

        /// <summary>
        /// 可用图像源集合
        /// </summary>
        public ObservableCollection<ImageSourceInfo> AvailableImageSources
        {
            get => (ObservableCollection<ImageSourceInfo>)GetValue(AvailableImageSourcesProperty);
            set => SetValue(AvailableImageSourcesProperty, value);
        }

        /// <summary>
        /// 是否显示缩略图预览
        /// </summary>
        public bool ShowThumbnail
        {
            get => (bool)GetValue(ShowThumbnailProperty);
            set => SetValue(ShowThumbnailProperty, value);
        }

        /// <summary>
        /// 是否显示尺寸信息
        /// </summary>
        public bool ShowSizeInfo
        {
            get => (bool)GetValue(ShowSizeInfoProperty);
            set => SetValue(ShowSizeInfoProperty, value);
        }

        /// <summary>
        /// 占位符文本
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 图像源选择变更事件
        /// </summary>
        public static readonly RoutedEvent ImageSourceChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ImageSourceChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(ImageSourceSelector));

        public event RoutedEventHandler ImageSourceChanged
        {
            add => AddHandler(ImageSourceChangedEvent, value);
            remove => RemoveHandler(ImageSourceChangedEvent, value);
        }

        #endregion

        #region 控件引用

        private Button _selectorButton = null!;
        private Popup _popup = null!;
        private ListBox _imageSourceList = null!;

        #endregion

        public ImageSourceSelector()
        {
            AvailableImageSources = new ObservableCollection<ImageSourceInfo>();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _selectorButton = GetTemplateChild("PART_SelectorButton") as Button 
                ?? throw new InvalidOperationException("PART_SelectorButton not found");
            _popup = GetTemplateChild("PART_Popup") as Popup 
                ?? throw new InvalidOperationException("PART_Popup not found");
            _imageSourceList = GetTemplateChild("PART_ImageSourceList") as ListBox 
                ?? throw new InvalidOperationException("PART_ImageSourceList not found");

            _selectorButton.Click += OnSelectorButtonClick;
            _imageSourceList.SelectionChanged += OnImageSourceListSelectionChanged;
        }

        private void OnSelectorButtonClick(object sender, RoutedEventArgs e)
        {
            _popup.IsOpen = !_popup.IsOpen;
        }

        private void OnImageSourceListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_imageSourceList.SelectedItem is ImageSourceInfo selectedSource)
            {
                SelectedImageSource = selectedSource;
                _popup.IsOpen = false;
                RaiseEvent(new RoutedEventArgs(ImageSourceChangedEvent));
            }
        }

        private static void OnSelectedImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageSourceSelector selector)
            {
                // 更新显示
                selector.UpdateDisplayText();
            }
        }

        private static void OnAvailableImageSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageSourceSelector selector)
            {
                // 如果当前选中的源不在新列表中，清除选择
                if (selector.SelectedImageSource != null && selector.AvailableImageSources != null)
                {
                    bool found = false;
                    foreach (var source in selector.AvailableImageSources)
                    {
                        if (source.NodeId == selector.SelectedImageSource.NodeId &&
                            source.OutputPortName == selector.SelectedImageSource.OutputPortName)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        selector.SelectedImageSource = null;
                    }
                }
            }
        }

        private void UpdateDisplayText()
        {
            // 显示文本更新由Template绑定处理
        }
    }
}
