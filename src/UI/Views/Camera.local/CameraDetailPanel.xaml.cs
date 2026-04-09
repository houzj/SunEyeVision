using System.Windows.Controls;
using System.Windows;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Camera
{
    /// <summary>
    /// CameraDetailPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CameraDetailPanel : UserControl
    {
        public CameraDetailPanel()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// 制造商模板选择器
    /// </summary>
    public class ManufacturerTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// 海康威模板
        /// </summary>
        public DataTemplate HikvisionTemplate { get; set; }

        /// <summary>
        /// 大华模板
        /// </summary>
        public DataTemplate DahuaTemplate { get; set; }

        /// <summary>
        /// 通用模板
        /// </summary>
        public DataTemplate GenericTemplate { get; set; }

        /// <summary>
        /// 根据制造商类型选择模板
        /// </summary>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // item 实际上是 GenericParamsViewModel 或其他厂商参数 ViewModel
            // 我们需要通过 DataContext 获取 CameraDetailViewModel 来判断制造商类型
            
            // 获取父级的 DataContext (CameraDetailViewModel)
            var parent = container as FrameworkElement;
            while (parent != null && parent.DataContext is not CameraDetailViewModel)
            {
                parent = parent.Parent as FrameworkElement;
            }

            if (parent?.DataContext is CameraDetailViewModel cameraDetailVm)
            {
                var viewModel = cameraDetailVm.SelectedCamera;
                if (viewModel != null)
                {
                    return viewModel.CameraType switch
                    {
                        "Hikvision" => HikvisionTemplate,
                        "Dahua" => DahuaTemplate,
                        _ => GenericTemplate
                    };
                }
            }

            return GenericTemplate;
        }
    }
}
