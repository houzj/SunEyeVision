using System.Windows;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// 全局变量管理器对话框
    /// </summary>
    public partial class GlobalVariableManagerDialog : Window
    {
        public GlobalVariableManagerDialog(GlobalVariableManagerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
