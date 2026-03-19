using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// 配方管理器对话框
    /// </summary>
    public partial class RecipeManagementDialog : Window
    {
        public RecipeManagementDialog(RecipeManagementDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 双击配方行触发应用
        /// </summary>
        private void OnRecipeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is RecipeManagementDialogViewModel viewModel)
            {
                if (viewModel.ApplyRecipeCommand.CanExecute(null))
                {
                    viewModel.ApplyRecipeCommand.Execute(null);
                }
            }
        }
    }
}
