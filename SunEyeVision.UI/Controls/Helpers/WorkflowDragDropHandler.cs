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
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] Drop position: ({dropPosition.X:F0}, {dropPosition.Y:F0})");

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {
                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 警告: ToolItem 的 ToolId 为空");
                    return;
                }

                // 从 MainWindow 动态获取当前选中的工作流（而不是使用 _canvasControl.DataContext）
                WorkflowTabViewModel workflowTab = GetCurrentWorkflowTab();
                if (workflowTab == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 错误: 无法获取当前选中的工作流");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] ✅ 使用当前工作流: {workflowTab.Name} (Id: {workflowTab.Id})");

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点，自动分配序号
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] Node position set to: ({newNode.Position.X:F0}, {newNode.Position.Y:F0})");

                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] ════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 📝 准备添加节点到工作流集合");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   工作流: {workflowTab.Name} (Id: {workflowTab.Id})");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   添加前节点数: {workflowTab.WorkflowNodes.Count}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   WorkflowNodes Hash: {workflowTab.WorkflowNodes.GetHashCode()}");

                // 添加新节点
                workflowTab.WorkflowNodes.Add(newNode);

                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   添加后节点数: {workflowTab.WorkflowNodes.Count}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   新节点Id: {newNode.Id}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   新节点名称: {newNode.Name}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   新节点索引: Index={newNode.Index}, GlobalIndex={newNode.GlobalIndex}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   新节点位置: ({newNode.Position.X:F0}, {newNode.Position.Y:F0})");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] ✅ 节点已添加到集合");

                // 验证节点确实在集合中
                bool nodeExists = workflowTab.WorkflowNodes.Contains(newNode);
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop]   验证节点在集合中: {nodeExists}");
                if (!nodeExists)
                {
                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] ❌ 错误: 节点不在集合中!");
                }

                // 🔥 关键修复：添加节点后强制刷新UI绑定
                // 因为所有Tab共享同一个WorkflowCanvasControl实例，需要手动刷新ItemsControl绑定
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 🔥 强制刷新UI绑定...");
                _canvasControl.ForceRefreshItemsControls();
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] ✅ UI绑定刷新完成");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] ════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 堆栈: {ex.StackTrace}");
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
                            System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab] ✅ 获取到当前工作流: {selectedTab.Name} (Id: {selectedTab.Id})");
                            System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab]   节点数: {selectedTab.WorkflowNodes?.Count ?? 0}");
                            return selectedTab;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab] ⚠ 无法从 MainWindow 获取当前工作流");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab] 异常: {ex.Message}");
                return null;
            }
        }
    }
}