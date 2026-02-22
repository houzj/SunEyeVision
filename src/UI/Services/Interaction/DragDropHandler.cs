using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Views.Windows;

namespace SunEyeVision.UI.Services.Interaction
{
    /// <summary>
    /// 工作流拖放处理器
    /// 负责从工具箱拖放节点到画布
    /// </summary>
    public class WorkflowDragDropHandler
    {
        private readonly WorkflowCanvasControl _canvasControl;

        // 性能优化：条件编译开关，设为 false 可禁用详细日志
        private const bool ENABLE_VERBOSE_LOG = false;

        public WorkflowDragDropHandler(
            WorkflowCanvasControl canvasControl)
        {
            _canvasControl = canvasControl;
        }

        /// <summary>
        /// 拖放进入事件
        /// </summary>
        public void Canvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖放悬停事件
        /// </summary>
        public void Canvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖放离开事件
        /// </summary>
        public void Canvas_DragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// 拖放放下事件 - 创建新节点
        /// 性能优化：延迟设置 SelectedNode，让节点先渲染完成
        /// </summary>
        public async void Canvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                // 检查 sender
                if (sender is not System.Windows.Controls.Canvas canvas)
                    return;

                // 检查拖拽数据
                var toolItemData = e.Data.GetData("ToolItem");
                if (toolItemData is not ToolItem item)
                    return;

                // 获取放置位置
                Point dropPosition = e.GetPosition(canvas);

                if (string.IsNullOrEmpty(item.ToolId))
                    return;

                // 从 MainWindow 动态获取当前选中的工作流
                WorkflowTabViewModel workflowTab = GetCurrentWorkflowTab();
                if (workflowTab == null)
                {
                    MessageBox.Show("无法获取当前工作流，请确保已打开工作流标签页", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);

                newNode.Position = dropPosition;
                newNode.IsSelected = true;

                // 添加新节点
                workflowTab.WorkflowNodes.Add(newNode);

                // ★ 关键优化：延迟设置 SelectedNode，让节点先渲染完成
                // 使用 Dispatcher.Yield 让 UI 线程先处理渲染，再触发属性面板更新
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

                // 触发图像预览器显示
                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel viewModel)
                    {
                        viewModel.SelectedNode = newNode;
                    }
                }
            }
            catch (Exception ex)
            {
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}\n\n类型: {ex.GetType().Name}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取当前选中的工作流 Tab
        /// </summary>
        private WorkflowTabViewModel GetCurrentWorkflowTab()
        {
            try
            {
                // 从 MainWindow 获取当前选中的工作流
                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel mainWindowViewModel)
                    {
                        var selectedTab = mainWindowViewModel.WorkflowTabViewModel?.SelectedTab;
                        if (selectedTab != null)
                        {
                            return selectedTab;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}