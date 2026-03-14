using System.Windows;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// NewRecipeDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NewRecipeDialog : Window
    {
        private readonly NewRecipeDialogViewModel _viewModel;

        /// <summary>
        /// 配方名称
        /// </summary>
        public string RecipeName => _viewModel.RecipeName;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description => _viewModel.Description;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultName">默认配方名称</param>
        public NewRecipeDialog(string defaultName = "新配方")
        {
            InitializeComponent();

            _viewModel = new NewRecipeDialogViewModel
            {
                RecipeName = defaultName
            };
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
                MessageBox.Show("请填写配方名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
