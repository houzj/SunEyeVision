using System.Windows;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Views.Windows
{
    /// <summary>
    /// SolutionPathDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SolutionPathDialog : Window
    {
        public string? SelectedPath { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SolutionPathDialog(string currentPath)
        {
            InitializeComponent();

            var viewModel = new SolutionPathDialogViewModel(currentPath);
            DataContext = viewModel;

            viewModel.OnConfirmed += (path) =>
            {
                SelectedPath = path;
                DialogResult = true;
                Close();
            };

            viewModel.OnCancelled += () =>
            {
                SelectedPath = null;
                DialogResult = false;
                Close();
            };
        }
    }
}
