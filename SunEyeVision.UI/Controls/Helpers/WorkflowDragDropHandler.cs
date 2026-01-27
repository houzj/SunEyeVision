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
            if (e.Data.GetDataPresent(typeof(string)))
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
            if (e.Data.GetDataPresent(typeof(string)))
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
            if (!e.Data.GetDataPresent(typeof(string)))
                return;

            var nodeType = e.Data.GetData(typeof(string)) as string;
            if (string.IsNullOrEmpty(nodeType))
                return;

            var selectedTab = _viewModel?.WorkflowTabViewModel.SelectedTab;
            if (selectedTab == null)
                return;

            // 计算放置位置
            var dropPosition = e.GetPosition(_canvasControl.WorkflowCanvas);

            // 创建新节点
            var newNode = new WorkflowNode(
                Guid.NewGuid().ToString(),
                $"Node_{selectedTab.WorkflowNodes.Count + 1}",
                nodeType)
            {
                Position = dropPosition,
                IsSelected = false
            };

            // 添加到节点集合
            selectedTab.WorkflowNodes.Add(newNode);

            _viewModel?.AddLog($"[Canvas] 创建节点: {newNode.Name} ({newNode.AlgorithmType}) at ({dropPosition.X:F0}, {dropPosition.Y:F0})");

            e.Handled = true;
        }
    }
}
