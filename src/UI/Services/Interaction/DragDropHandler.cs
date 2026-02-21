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
            System.Diagnostics.Debug.WriteLine("[DragEnter] ▶ 画布接收拖拽进入事件");
            System.Diagnostics.Debug.WriteLine($"[DragEnter]   - Data formats: {string.Join(", ", e.Data.GetFormats())}");

            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
                System.Diagnostics.Debug.WriteLine("[DragEnter] ✓ ToolItem 数据存在，允许复制");
            }
            else
            {
                e.Effects = DragDropEffects.None;
                System.Diagnostics.Debug.WriteLine("[DragEnter] ✗ ToolItem 数据不存在");
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
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine("[Drop] ▶ Canvas_Drop 开始执行");
            System.Diagnostics.Debug.WriteLine($"[Drop]   - Sender 类型: {sender?.GetType().Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[Drop]   - Data formats: {string.Join(", ", e.Data.GetFormats())}");

            try
            {
                // 检查 sender
                if (sender is not System.Windows.Controls.Canvas canvas)
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop] ✗ Sender 不是 Canvas 类型");
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[Drop] ✓ Canvas 获取成功");

                // 检查拖拽数据
                var toolItemData = e.Data.GetData("ToolItem");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - GetData(\"ToolItem\") 类型: {toolItemData?.GetType().Name ?? "null"}");

                if (toolItemData is not ToolItem item)
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop] ✗ 数据不是 ToolItem 类型");
                    return;
                }

                // 获取放置位置
                Point dropPosition = e.GetPosition(canvas);
                System.Diagnostics.Debug.WriteLine($"[Drop]   - 放置位置: ({dropPosition.X:F1}, {dropPosition.Y:F1})");

                // 验证数据
                System.Diagnostics.Debug.WriteLine($"[Drop] ToolItem 数据:");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - Name: {item.Name}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - ToolId: {item.ToolId}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - AlgorithmType: {item.AlgorithmType}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - Category: {item.Category}");

                if (string.IsNullOrEmpty(item.ToolId))
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop] ✗ ToolId 为空");
                    return;
                }

                // 从 MainWindow 动态获取当前选中的工作流
                System.Diagnostics.Debug.WriteLine($"[Drop] ▶ 获取当前工作流 Tab...");
                WorkflowTabViewModel workflowTab = GetCurrentWorkflowTab();
                if (workflowTab == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop] ✗ 无法获取当前工作流Tab");
                    MessageBox.Show("无法获取当前工作流，请确保已打开工作流标签页", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[Drop] ✓ 工作流 Tab 获取成功: Id={workflowTab.Id}, Name={workflowTab.Name}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - 当前节点数: {workflowTab.WorkflowNodes.Count}");

                // 清除其他节点的选中状态
                System.Diagnostics.Debug.WriteLine($"[Drop] ▶ 清除其他节点的选中状态...");
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点
                System.Diagnostics.Debug.WriteLine($"[Drop] ▶ 调用 WorkflowTabViewModel.CreateNode...");
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                System.Diagnostics.Debug.WriteLine($"[Drop] ✓ 节点创建返回成功");

                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                System.Diagnostics.Debug.WriteLine($"[Drop] 节点详情:");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - Id: {newNode.Id}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - Name: {newNode.Name}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - AlgorithmType: {newNode.AlgorithmType}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - Index: {newNode.Index}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - GlobalIndex: {newNode.GlobalIndex}");
                System.Diagnostics.Debug.WriteLine($"[Drop]   - IsImageCapture: {newNode.IsImageCaptureNode}");

                // 添加新节点
                System.Diagnostics.Debug.WriteLine($"[Drop] ▶ 添加节点到集合...");
                workflowTab.WorkflowNodes.Add(newNode);
                System.Diagnostics.Debug.WriteLine($"[Drop] ✓ 节点已添加, 当前节点数: {workflowTab.WorkflowNodes.Count}");

                // 触发图像预览器显示
                System.Diagnostics.Debug.WriteLine($"[Drop] ▶ 设置 SelectedNode...");
                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop]   - MainWindow 获取成功");
                    if (mainWindow.DataContext is MainWindowViewModel viewModel)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Drop]   - MainWindowViewModel 获取成功");
                        viewModel.SelectedNode = newNode;
                        System.Diagnostics.Debug.WriteLine($"[Drop] ✓ SelectedNode 已设置");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Drop] ✗ MainWindow.DataContext 不是 MainWindowViewModel: {mainWindow.DataContext?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop] ✗ 无法获取 MainWindow");
                }

                System.Diagnostics.Debug.WriteLine($"[Drop] ✓✓✓ 节点添加完成 ✓✓✓");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Drop] ✗✗✗ 异常 ✗✗✗");
                System.Diagnostics.Debug.WriteLine($"[Drop] 异常类型: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"[Drop] 异常消息: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Drop] 堆栈跟踪:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Drop] 内部异常: {ex.InnerException.Message}");
                }
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}\n\n类型: {ex.GetType().Name}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取当前选中的工作流 Tab
        /// </summary>
        private WorkflowTabViewModel GetCurrentWorkflowTab()
        {
            System.Diagnostics.Debug.WriteLine("[GetCurrentWorkflowTab] ▶ 开始获取当前工作流 Tab");
            try
            {
                // 从 MainWindow 获取当前选中的工作流
                if (Application.Current?.MainWindow is MainWindow mainWindow)
                {
                    System.Diagnostics.Debug.WriteLine("[GetCurrentWorkflowTab]   ✓ MainWindow 获取成功");

                    if (mainWindow.DataContext is MainWindowViewModel mainWindowViewModel)
                    {
                        System.Diagnostics.Debug.WriteLine("[GetCurrentWorkflowTab]   ✓ MainWindowViewModel 获取成功");
                        System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab]   - WorkflowTabViewModel 类型: {mainWindowViewModel.WorkflowTabViewModel?.GetType().Name ?? "null"}");

                        var selectedTab = mainWindowViewModel.WorkflowTabViewModel?.SelectedTab;
                        System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab]   - SelectedTab: {(selectedTab != null ? selectedTab.Name : "null")}");

                        if (selectedTab != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab] ✓ 返回 SelectedTab: {selectedTab.Name}");
                            return selectedTab;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[GetCurrentWorkflowTab] ✗ SelectedTab 为 null");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab] ✗ MainWindow.DataContext 不是 MainWindowViewModel: {mainWindow.DataContext?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[GetCurrentWorkflowTab] ✗ Application.Current?.MainWindow 不是 MainWindow");
                    System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab]   - Application.Current: {(Application.Current != null ? "存在" : "null")}");
                    System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab]   - MainWindow: {(Application.Current?.MainWindow != null ? Application.Current.MainWindow.GetType().Name : "null")}");
                }

                System.Diagnostics.Debug.WriteLine("[GetCurrentWorkflowTab] ✗ 返回 null");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab] ✗ 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GetCurrentWorkflowTab]   堆栈: {ex.StackTrace}");
                return null;
            }
        }
    }
}