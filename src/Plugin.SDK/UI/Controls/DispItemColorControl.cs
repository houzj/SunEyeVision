using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Models.Visualization;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 显示项颜色配置控件
    /// </summary>
    /// <remarks>
    /// 用于配置显示项的可见性和样式。
    ///
    /// 功能：
    /// - 小眼睛图标：控制显示/隐藏状态
    /// - 画笔图标：点击弹出样式设置对话框
    /// - 样式设置：OK颜色、NG颜色、透明度、粗细
    /// - 双向数据绑定，实时生效
    /// - 支持内容水平对齐设置（Left、Center、Right）
    ///
    /// 使用示例：
    /// <code>
    /// &lt;controls:LabeledControl Label="输出图像"&gt;
    ///   &lt;controls:DispItemColorControl
    ///       Config="{Binding DisplayConfig.OutputImage, Mode=TwoWay}"
    ///       ContentHorizontalAlignment="Left"/&gt;
    /// &lt;/controls:LabeledControl&gt;
    /// </code>
    /// </remarks>
    public class DispItemColorControl : Control
    {
        #region 模板部件

        /// <summary>
        /// 可见性按钮部件名称
        /// </summary>
        public const string PART_VisibilityButton = "PART_VisibilityButton";

        /// <summary>
        /// 样式设置按钮部件名称
        /// </summary>
        public const string PART_StyleSettingsButton = "PART_StyleSettingsButton";

        /// <summary>
        /// 样式设置Popup部件名称
        /// </summary>
        public const string PART_StyleSettingsPopup = "PART_StyleSettingsPopup";

        private Button? _visibilityButton;
        private Button? _styleSettingsButton;
        private Popup? _styleSettingsPopup;

        #endregion

        #region 静态构造函数

        static DispItemColorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DispItemColorControl),
                new FrameworkPropertyMetadata(typeof(DispItemColorControl)));
        }

        #endregion

        #region DependencyProperty

        #region Config - 显示项配置

        /// <summary>
        /// 显示项配置（支持双向绑定）
        /// </summary>
        public static readonly DependencyProperty ConfigProperty =
            DependencyProperty.Register(
                nameof(Config),
                typeof(DisplayItemConfig),
                typeof(DispItemColorControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnConfigChanged));

        public DisplayItemConfig Config
        {
            get => (DisplayItemConfig)GetValue(ConfigProperty);
            set => SetValue(ConfigProperty, value);
        }

        private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DispItemColorControl)d;
            var oldConfig = (DisplayItemConfig?)e.OldValue;
            var newConfig = (DisplayItemConfig?)e.NewValue;

            PluginLogger.Info($"ConfigChanged - Old: {oldConfig?.Name}, New: {newConfig?.Name}", "DispItemColorControl");
        }

        #endregion

        #region ContentHorizontalAlignment - 内容水平对齐

        /// <summary>
        /// 内容水平对齐方式
        /// </summary>
        public static readonly DependencyProperty ContentHorizontalAlignmentProperty =
            DependencyProperty.Register(
                nameof(ContentHorizontalAlignment),
                typeof(HorizontalAlignment),
                typeof(DispItemColorControl),
                new FrameworkPropertyMetadata(
                    HorizontalAlignment.Right,
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public HorizontalAlignment ContentHorizontalAlignment
        {
            get => (HorizontalAlignment)GetValue(ContentHorizontalAlignmentProperty);
            set => SetValue(ContentHorizontalAlignmentProperty, value);
        }

        #endregion

        #endregion

        #region 构造函数

        public DispItemColorControl()
        {
            PluginLogger.Info($"DispItemColorControl 初始化", "DispItemColorControl");
        }

        #endregion

        #region 模板应用

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            PluginLogger.Info($"OnApplyTemplate", "DispItemColorControl");

            // 移除旧的事件处理
            if (_visibilityButton != null)
            {
                _visibilityButton.Click -= OnVisibilityButtonClick;
            }
            if (_styleSettingsButton != null)
            {
                _styleSettingsButton.Click -= OnStyleSettingsButtonClick;
            }

            // 获取模板部件
            _visibilityButton = GetTemplateChild(PART_VisibilityButton) as Button;
            _styleSettingsButton = GetTemplateChild(PART_StyleSettingsButton) as Button;
            _styleSettingsPopup = GetTemplateChild(PART_StyleSettingsPopup) as Popup;

            // 添加新的事件处理
            if (_visibilityButton != null)
            {
                _visibilityButton.Click += OnVisibilityButtonClick;
                PluginLogger.Info($"PART_VisibilityButton 获取成功", "DispItemColorControl");
            }
            else
            {
                PluginLogger.Warning($"PART_VisibilityButton 未找到", "DispItemColorControl");
            }

            if (_styleSettingsButton != null)
            {
                _styleSettingsButton.Click += OnStyleSettingsButtonClick;
                PluginLogger.Info($"PART_StyleSettingsButton 获取成功", "DispItemColorControl");
            }
            else
            {
                PluginLogger.Warning($"PART_StyleSettingsButton 未找到", "DispItemColorControl");
            }

            if (_styleSettingsPopup != null)
            {
                PluginLogger.Info($"PART_StyleSettingsPopup 获取成功", "DispItemColorControl");
            }
            else
            {
                PluginLogger.Warning($"PART_StyleSettingsPopup 未找到", "DispItemColorControl");
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 可见性按钮点击事件
        /// </summary>
        private void OnVisibilityButtonClick(object sender, RoutedEventArgs e)
        {
            if (Config == null)
            {
                PluginLogger.Warning($"Config 为 null，无法切换可见性", "DispItemColorControl");
                return;
            }

            Config.IsVisible = !Config.IsVisible;
            PluginLogger.Info($"切换可见性: {Config.Name} -> {Config.IsVisible}", "DispItemColorControl");
        }

        /// <summary>
        /// 样式设置按钮点击事件
        /// </summary>
        private void OnStyleSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            if (_styleSettingsPopup == null)
            {
                PluginLogger.Warning($"PART_StyleSettingsPopup 为 null，无法打开样式设置", "DispItemColorControl");
                return;
            }

            _styleSettingsPopup.IsOpen = !_styleSettingsPopup.IsOpen;
            PluginLogger.Info($"切换样式设置Popup: {_styleSettingsPopup.IsOpen}", "DispItemColorControl");
        }

        #endregion
    }
}
