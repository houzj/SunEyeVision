using System.Windows.Controls;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Camera
{
    /// <summary>
    /// GenericParamsView.xaml 的交互逻辑
    /// </summary>
    public partial class GenericParamsView : UserControl
    {
        public GenericParamsView()
        {
            InitializeComponent();
            this.DataContext = new GenericParamsViewModel();
        }
    }
}
