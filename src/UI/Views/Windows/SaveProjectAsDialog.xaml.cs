using System.Windows;
using SunEyeVision.UI.Controls.Helpers;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// SaveProjectAsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SaveProjectAsDialog : Window
    {
        private readonly SaveProjectAsDialogViewModel _viewModel;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName => _viewModel.ProjectName;

        /// <summary>
        /// 项目路径
        /// </summary>
        public string ProjectPath => _viewModel.ProjectPath;

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description => _viewModel.Description;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SaveProjectAsDialog(string defaultName, string defaultPath, string? defaultDescription)
        {
            InitializeComponent();

            _viewModel = new SaveProjectAsDialogViewModel(defaultName, defaultPath, defaultDescription);
            DataContext = _viewModel;

            // 设置焦点
            Loaded += (s, e) => ProjectNameTextBox.Focus();
        }

        /// <summary>
        /// 浏览按钮点击
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果路径为空，从桌面开始；否则从用户输入的路径开始
            var initialPath = string.IsNullOrEmpty(_viewModel.ProjectPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : _viewModel.ProjectPath;

            var selectedPath = FolderBrowserHelper.BrowseForFolder(
                "选择项目保存位置",
                initialPath,
                showNewFolderButton: true);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.ProjectPath = selectedPath;
            }
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
                MessageBox.Show("请填写项目名称和项目路径", "提示",
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
