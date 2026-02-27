using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 参数分组控件 - 将相关参数组织在一起
    /// </summary>
    /// <remarks>
    /// 用于参数分组显示，提供可折叠的容器。
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;controls:ParamGroup Header="高级设置" IsExpanded="False"&gt;
    ///     &lt;controls:ParamSlider Label="精度" Value="{Binding Precision}"/&gt;
    ///     &lt;controls:ParamComboBox Label="模式" ItemsSource="{Binding Modes}"/&gt;
    /// &lt;/controls:ParamGroup&gt;
    /// </code>
    /// </remarks>
    public class ParamGroup : Expander
    {
        static ParamGroup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ParamGroup),
                new FrameworkPropertyMetadata(typeof(ParamGroup)));
        }

        public static readonly DependencyProperty GroupStyleProperty =
            DependencyProperty.Register(nameof(GroupStyle), typeof(ParamGroupStyle), typeof(ParamGroup),
                new PropertyMetadata(ParamGroupStyle.Default));

        public ParamGroupStyle GroupStyle
        {
            get => (ParamGroupStyle)GetValue(GroupStyleProperty);
            set => SetValue(GroupStyleProperty, value);
        }
    }

    /// <summary>
    /// 参数分组样式
    /// </summary>
    public enum ParamGroupStyle
    {
        /// <summary>
        /// 默认样式 - 带边框
        /// </summary>
        Default,

        /// <summary>
        /// 卡片样式 - 带阴影
        /// </summary>
        Card,

        /// <summary>
        /// 简洁样式 - 无边框
        /// </summary>
        Simple
    }
}
