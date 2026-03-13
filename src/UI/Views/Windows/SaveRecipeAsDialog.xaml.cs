using System.Windows;
using System.Collections.Generic;
using SunEyeVision.Workflow;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// SaveRecipeAsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SaveRecipeAsDialog : Window
    {
        private readonly SaveRecipeAsDialogViewModel _viewModel;

        /// <summary>
        /// 选中的目标项目ID
        /// </summary>
        public string TargetProjectId => _viewModel.SelectedProjectId;

        /// <summary>
        /// 配方名称
        /// </summary>
        public string RecipeName => _viewModel.RecipeName;

        /// <summary>
        /// 配方描述
        /// </summary>
        public string? Description => _viewModel.Description;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SaveRecipeAsDialog(string currentProjectId, string currentRecipeName, 
                              string? currentDescription, List<Project> projects)
        {
            InitializeComponent();

            _viewModel = new SaveRecipeAsDialogViewModel(currentProjectId, currentRecipeName, 
                                                      currentDescription, projects);
            DataContext = _viewModel;

            // 设置焦点
            Loaded += (s, e) => RecipeNameTextBox.Focus();
        }

        /// <summary>
        /// 确定按钮点击
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Validate())
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("请填写配方名称和选择目标项目", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
