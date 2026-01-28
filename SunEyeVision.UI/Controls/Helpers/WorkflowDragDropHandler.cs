using System;
using System.Windows;
using System.Windows.Controls;
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
                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] 警告: ToolItem 的 ToolId 为空");
                    return;
                }

                // 直接从 WorkflowCanvasControl 的 DataContext 获取当前标签页
                if (_canvasControl.DataContext is WorkflowTabViewModel workflowTab)
                {
                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] Creating node: ToolId={item.ToolId}, Name={item.Name}");

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

                    System.Diagnostics.Debug.WriteLine($"[Canvas_Drop] Node added: Index={newNode.Index}, GlobalIndex={newNode.GlobalIndex}");
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