using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 图像源选择器 - 显示所有父节点的图像输出供选择（核心控件，不包含标签和外框）
    /// </summary>
    /// <remarks>
    /// 用于工作流中选择输入图像来源。显示当前节点所有上游节点的图像输出端口。
    /// 与 BindableParameter 保持一致的设计模式：
    /// - 使用 AvailableDataSource 作为统一数据模型
    /// - SelectedDataSource 类型为 AvailableDataSource?，与参数属性类型一致
    /// - 唯一区别是下拉样式：图像源用 ListBox，数值源用 TreeView
    /// 
    /// 设计理念：
    /// - 统一绑定到 AvailableDataSources（包含所有类型的数据源）
    /// - 控件内部根据参数类型自动过滤
    /// - 简化 XAML 绑定：统一使用 AvailableDataSources
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ImageSourceSelector
    ///     AvailableDataSources="{Binding AvailableDataSources, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type controls:ToolDebugControlBase}}}"
    ///     SelectedDataSource="{Binding Parameters.ImageSource, Mode=TwoWay}" /&gt;
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

        public static readonly DependencyProperty SelectedDataSourceProperty =
            DependencyProperty.Register(nameof(SelectedDataSource), typeof(AvailableDataSource), typeof(ImageSourceSelector),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDataSourceChanged));

        public static readonly DependencyProperty AvailableDataSourcesProperty =
            DependencyProperty.Register(nameof(AvailableDataSources), typeof(ObservableCollection<AvailableDataSource>), typeof(ImageSourceSelector),
                new PropertyMetadata(null, OnAvailableDataSourcesChanged));

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(ImageSourceSelector),
                new PropertyMetadata("选择图像源..."));

        public static readonly DependencyProperty SelectorWidthProperty =
            DependencyProperty.Register(nameof(SelectorWidth), typeof(double), typeof(ImageSourceSelector),
                new PropertyMetadata(200.0));

        #endregion

        #region 属性

        /// <summary>
        /// 当前选中的图像源（与 BindableParameter 绑定模式一致）
        /// </summary>
        public AvailableDataSource SelectedDataSource
        {
            get => (AvailableDataSource)GetValue(SelectedDataSourceProperty);
            set => SetValue(SelectedDataSourceProperty, value);
        }

        /// <summary>
        /// 所有类型的数据源集合（统一绑定源，控件内部过滤图像类型）
        /// </summary>
        public ObservableCollection<AvailableDataSource> AvailableDataSources
        {
            get => (ObservableCollection<AvailableDataSource>)GetValue(AvailableDataSourcesProperty);
            set => SetValue(AvailableDataSourcesProperty, value);
        }

        /// <summary>
        /// 占位符文本
        /// </summary>
        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// 选择器宽度（默认200px）
        /// </summary>
        public double SelectorWidth
        {
            get => (double)GetValue(SelectorWidthProperty);
            set => SetValue(SelectorWidthProperty, value);
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
            if (_imageSourceList.SelectedItem is AvailableDataSource selectedSource)
            {
                SelectedDataSource = selectedSource;
                _popup.IsOpen = false;
                RaiseEvent(new RoutedEventArgs(ImageSourceChangedEvent));
            }
        }

        private static void OnSelectedDataSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 显示文本更新由Template绑定处理
        }

        private static void OnAvailableDataSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ImageSourceSelector selector)
            {

                // 如果当前选中的源不在新列表中，清除选择
                if (selector.SelectedDataSource != null && selector.AvailableDataSources != null)
                {
                    bool found = false;
                    foreach (var source in selector.AvailableDataSources)
                    {
                        // 精确匹配数据源（基于节点ID和属性名称）
                        if (source.SourceNodeId == selector.SelectedDataSource.SourceNodeId &&
                            source.PropertyName == selector.SelectedDataSource.PropertyName)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        selector.SelectedDataSource = null;
                    }
                }
            }
        }
    }
}
