using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls.Helpers
{
    /// <summary>
    /// 工作流拖放处理器
    /// 负责从工具箱拖放节点到画布
    /// </summary>
    public class WorkflowDragDropHandler
    {
        private readonly WorkflowCanvasControl _canvasControl;
        private readonly MainWindowViewModel? _viewModel;

        public WorkflowDragDropHandler(
            WorkflowCanvasControl canvasControl,
            MainWindowViewModel? viewModel)
        {
            _canvasControl = canvasControl;
            _viewModel = viewModel;
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
                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 警告: ToolItem 的 ToolId 为空");
                    return;
                }

                // 创建新节点，使用ToolId作为AlgorithmType
                var newNode = new WorkflowNode(
                    Guid.NewGuid().ToString(),
                    item.Name,
                    item.ToolId
                );
                newNode.Position = dropPosition;
                newNode.IsSelected = true;

                // 添加到当前标签页
                if (_viewModel?.WorkflowTabViewModel.SelectedTab != null)
                {
                    // 清除其他节点的选中状态
                    foreach (var node in _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes)
                    {
                        node.IsSelected = false;
                    }

                    // 添加新节点
                    _viewModel.WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(newNode);
                    _viewModel.SelectedNode = newNode;

                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 成功创建节点: {item.Name} (ID: {newNode.Id})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 堆栈: {ex.StackTrace}");
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}