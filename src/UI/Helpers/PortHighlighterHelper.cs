using System.Windows;

namespace SunEyeVision.UI.Helpers
{
    /// <summary>
    /// 端口高亮辅助类 - 提供附加属性控制端口高亮状态
    /// </summary>
    public static class PortHighlighterHelper
    {
        /// <summary>
        /// IsHighlighted 附加属性
        /// </summary>
        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.RegisterAttached(
                "IsHighlighted",
                typeof(bool),
                typeof(PortHighlighterHelper),
                new PropertyMetadata(false));

        /// <summary>
        /// 获取 IsHighlighted 属性值
        /// </summary>
        public static bool GetIsHighlighted(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsHighlightedProperty);
        }

        /// <summary>
        /// 设置 IsHighlighted 属性值
        /// </summary>
        public static void SetIsHighlighted(DependencyObject obj, bool value)
        {
            obj.SetValue(IsHighlightedProperty, value);
        }
    }
}
