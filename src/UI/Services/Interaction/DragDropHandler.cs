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
        private static DateTime _lastLogTime = DateTime.Now;

        // 直接引用注入（优先使用，O(1) 访问）
        private WorkflowTabControlViewModel? _tabViewModel;

        // 缓存 ViewModel 引用，避免每次通过 Application.Current 获取（回退方案）
        private static MainWindowViewModel? _cachedViewModel;
        private static DateTime _viewModelCacheTime = DateTime.MinValue;
        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 工作流标签页管理器（属性注入，在 Canvas Loaded 后设置）
        /// </summary>
        public WorkflowTabControlViewModel? TabViewModel
        {
            get => _tabViewModel;
            set => _tabViewModel = value;
        }

        /// <summary>
        /// 带时间戳的调试日志
        /// </summary>
        private static void LogTimestamp(string tag, string message)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastLogTime).TotalMilliseconds;
            _lastLogTime = now;
            System.Diagnostics.Debug.WriteLine($"[{now:HH:mm:ss.fff}] [+{elapsed:F0}ms] [{tag}] {message}");
        }

        /// <summary>
        /// 获取缓存的 MainWindowViewModel（带过期时间）
        /// </summary>
        private static MainWindowViewModel? GetCachedViewModel()
        {
            // 检查缓存是否有效
            if (_cachedViewModel != null && (DateTime.Now - _viewModelCacheTime) < _cacheExpiry)
            {
                return _cachedViewModel;
            }

            // 重新获取并缓存
            if (Application.Current?.MainWindow is MainWindow mainWindow)
            {
                _cachedViewModel = mainWindow.DataContext as MainWindowViewModel;
                _viewModelCacheTime = DateTime.Now;
            }

            return _cachedViewModel;
        }

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
            _lastLogTime = DateTime.Now; // 重置计时器
            System.Diagnostics.Debug.WriteLine("════════════════════════════════════════════════════════════");
            LogTimestamp("Drop", "▶ Canvas_Drop 开始执行");
            LogTimestamp("Drop", $"  Sender: {sender?.GetType().Name ?? "null"}");

            try
            {
                // 检查 sender
                if (sender is not System.Windows.Controls.Canvas canvas)
                {
                    LogTimestamp("Drop", "✗ Sender 不是 Canvas 类型");
                    return;
                }
                LogTimestamp("Drop", "✓ Canvas 获取成功");

                // 检查拖拽数据
                var toolItemData = e.Data.GetData("ToolItem");
                LogTimestamp("Drop", $"GetData(\"ToolItem\") 类型: {toolItemData?.GetType().Name ?? "null"}");

                if (toolItemData is not ToolItem item)
                {
                    LogTimestamp("Drop", "✗ 数据不是 ToolItem 类型");
                    return;
                }

                // 获取放置位置
                Point dropPosition = e.GetPosition(canvas);
                LogTimestamp("Drop", $"放置位置: ({dropPosition.X:F1}, {dropPosition.Y:F1})");
                LogTimestamp("Drop", $"ToolItem: Name={item.Name}, ToolId={item.ToolId}");

                if (string.IsNullOrEmpty(item.ToolId))
                {
                    LogTimestamp("Drop", "✗ ToolId 为空");
                    return;
                }

                // 从 MainWindow 动态获取当前选中的工作流
                LogTimestamp("Drop", "▶ 获取当前工作流 Tab...");
                WorkflowTabViewModel workflowTab = GetCurrentWorkflowTab();
                if (workflowTab == null)
                {
                    LogTimestamp("Drop", "✗ 无法获取当前工作流Tab");
                    MessageBox.Show("无法获取当前工作流，请确保已打开工作流标签页", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                LogTimestamp("Drop", $"✓ 工作流 Tab 获取成功: {workflowTab.Name}, 节点数={workflowTab.WorkflowNodes.Count}");

                // 注意：Drop 创建节点不计为选中节点，用户再次点击才算点击节点
                // 因此不自动选中、不设置 IsSelected、不设置 SelectedNode

                // 使用 ViewModel 的 CreateNode 方法创建节点
                LogTimestamp("Drop", "▶ 调用 WorkflowTabViewModel.CreateNode...");
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                LogTimestamp("Drop", $"✓ 节点创建完成: Id={newNode.Id}, Name={newNode.Name}");

                newNode.Position = dropPosition;
                LogTimestamp("Drop", "✓ 节点属性设置完成");

                // 添加新节点
                LogTimestamp("Drop", "▶ 添加节点到集合...");
                workflowTab.WorkflowNodes.Add(newNode);
                LogTimestamp("Drop", $"✓ 节点已添加, 当前节点数: {workflowTab.WorkflowNodes.Count}");

                LogTimestamp("Drop", "═══ 节点添加完成 ═══");
            }
            catch (Exception ex)
            {
                LogTimestamp("Drop", $"✗✗✗ 异常: {ex.Message}");
                MessageBox.Show($"拖放节点失败: {ex.Message}\n\n类型: {ex.GetType().Name}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取当前选中的工作流 Tab（优先使用直接引用）
        /// </summary>
        private WorkflowTabViewModel? GetCurrentWorkflowTab()
        {
            try
            {
                // 优先使用直接引用（O(1) 访问）
                if (_tabViewModel != null)
                {
                    var selectedTab = _tabViewModel.SelectedTab;
                    if (selectedTab != null)
                    {
                        LogTimestamp("GetTab", $"✓ 通过直接引用获取成功: {selectedTab.Name}");
                        return selectedTab;
                    }
                }

                // 回退：使用缓存方式
                var viewModel = GetCachedViewModel();
                if (viewModel == null)
                {
                    LogTimestamp("GetTab", "✗ 缓存的 ViewModel 为 null");
                    return null;
                }

                var tab = viewModel.WorkflowTabViewModel?.SelectedTab;
                if (tab != null)
                {
                    LogTimestamp("GetTab", $"✓ 通过缓存获取成功: {tab.Name}");
                    return tab;
                }

                LogTimestamp("GetTab", "✗ SelectedTab 为 null");
                return null;
            }
            catch (Exception ex)
            {
                LogTimestamp("GetTab", $"✗ 异常: {ex.Message}");
                return null;
            }
        }
    }
}