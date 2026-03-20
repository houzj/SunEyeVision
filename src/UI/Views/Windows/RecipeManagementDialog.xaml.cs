using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

            // 订阅配方创建事件
            viewModel.OnRecipeCreated += OnRecipeCreated;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 配方创建完成事件处理 - 选中新建的配方并进入编辑模式
        /// </summary>
        private void OnRecipeCreated(Workflow.Recipe recipe)
        {
            // 使用Dispatcher确保在UI线程执行
            Dispatcher.Invoke(() =>
            {
                var dataGrid = RecipeDataGrid;
                if (dataGrid == null) return;

                // 选中新建的配方
                dataGrid.SelectedItem = recipe;

                // 滚动到选中的项
                dataGrid.ScrollIntoView(recipe);

                // 延迟一点时间让DataGrid完成选中，然后进入编辑模式
                Dispatcher.Invoke(() =>
                {
                    if (dataGrid.SelectedItem == recipe)
                    {
                        var row = dataGrid.ItemContainerGenerator.ContainerFromItem(recipe) as DataGridRow;
                        if (row != null)
                        {
                            // 聚焦到第一列（名称列）
                            row.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                        }
                    }
                }, System.Windows.Threading.DispatcherPriority.Input);
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
    }
}
