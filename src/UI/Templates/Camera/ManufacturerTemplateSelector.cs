using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.UI.Views.Camera
{
    /// <summary>
    /// 厂商模板选择器
    /// </summary>
    public class ManufacturerTemplateSelector : DataTemplateSelector
    {
        public DataTemplate HikvisionTemplate { get; set; }
        public DataTemplate DahuaTemplate { get; set; }
        public DataTemplate BaslerTemplate { get; set; }
        public DataTemplate GenericTemplate { get; set; }
        
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is HikvisionParamsViewModel)
                return HikvisionTemplate;
            if (item is DahuaParamsViewModel)
                return DahuaTemplate;
            if (item is BaslerParamsViewModel)
                return BaslerTemplate;
            
            return GenericTemplate;
        }
    }
}
