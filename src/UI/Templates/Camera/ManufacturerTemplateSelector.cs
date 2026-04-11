using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Views.Camera
{
    /// <summary>
    /// 厂商模板选择器
    /// </summary>
    /// <remarks>
    /// 注意：厂商特定的 ViewModel（HikvisionParamsViewModel、DahuaParamsViewModel、BaslerParamsViewModel）
    /// 当前未实现，所有相机类型都使用通用模板。
    /// </remarks>
    public class ManufacturerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GenericTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // TODO: 未来实现厂商特定参数时，取消注释以下代码
            // if (item is HikvisionParamsViewModel)
            //     return HikvisionTemplate;
            // if (item is DahuaParamsViewModel)
            //     return DahuaTemplate;
            // if (item is BaslerParamsViewModel)
            //     return BaslerTemplate;

            return GenericTemplate;
        }
    }
}
