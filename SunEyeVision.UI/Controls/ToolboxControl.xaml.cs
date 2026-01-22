using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// ToolboxControl.xaml 的交互逻辑
    /// </summary>
    public partial class ToolboxControl : UserControl
    {
        private ToolboxViewModel _viewModel;

        public ToolboxViewModel Toolbox => _viewModel;

        public ToolboxControl()
        {
            InitializeComponent();
            _viewModel = new ToolboxViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// 开始拖拽工具项
        /// </summary>
        private void ToolItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ToolItem tool)
            {
                var data = new DataObject("ToolItem", tool);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// 展开/折叠分类
        /// </summary>
        private void CategoryHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ToolCategory category)
            {
                category.IsExpanded = !category.IsExpanded;
            }
        }
    }
}
