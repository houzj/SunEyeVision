using System.Windows;
using System.Windows.Controls;
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

        /// <summary>
        /// 分组标签选中事件
        /// </summary>
        private void GroupRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.DataContext is VariableGroupViewModel groupVM)
            {
                var viewModel = DataContext as GlobalVariableManagerViewModel;
                if (viewModel != null && viewModel.SelectedGroup != groupVM)
                {
                    viewModel.SelectedGroup = groupVM;
                }
            }
        }
    }
}
