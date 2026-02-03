using System;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI;

namespace SunEyeVision.UI.Controls.Helpers
{
    /// <summary>
    /// 工作流拖放处理器
    /// 负责从工具箱拖放节点到画布
    /// </summary>
    public class WorkflowDragDropHandler
    {
        private readonly WorkflowCanvasControl _canvasControl;

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
        /// </summary>
        public void Canvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not Canvas canvas || e.Data.GetData("ToolItem") is not ToolItem item)
                {
                    return;
                }

                // 获取放置位置
                Point dropPosition = e.GetPosition(canvas);

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {
                    return;
                }

                // 从 MainWindow 动态获取当前选中的工作流（而不是使用 _canvasControl.DataContext）
                WorkflowTabViewModel workflowTab = GetCurrentWorkflowTab();
                if (workflowTab == null)
                {
                    return;
                }

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点，自动分配序号
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;

                // 添加新节点
                workflowTab.WorkflowNodes.Add(newNode);
            }
            catch (Exception ex)
            {
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        var selectedTab = mainWindowViewModel.WorkflowTabViewModel.SelectedTab;
                        if (selectedTab != null)
                        {
                            return selectedTab;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}