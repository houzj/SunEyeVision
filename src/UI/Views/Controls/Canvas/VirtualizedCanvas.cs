using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Views.Controls.Canvas
{
    /// <summary>
    /// 虚拟化画布 - 只渲染可见区域内的节点和连线
    /// 用于提升大规模节点场景下的渲染性能
    /// </summary>
    public class VirtualizedCanvas : System.Windows.Controls.Canvas
    {
        private readonly ObservableCollection<WorkflowNode> _allNodes;
        private readonly ObservableCollection<WorkflowConnection> _allConnections;
        private readonly HashSet<string> _visibleNodes = new HashSet<string>();
        private readonly HashSet<string> _visibleConnections = new HashSet<string>();

        // 可见区域（带缓冲）
        private Rect _viewPort = new Rect(0, 0, 1920, 1080);
        private readonly double _bufferSize = 200.0; // 缓冲区大小（像素）

        // 性能统计
        public int TotalNodes => _allNodes.Count;
        public int VisibleNodes => _visibleNodes.Count;
        public int TotalConnections => _allConnections.Count;
        public int VisibleConnections => _visibleConnections.Count;

        public VirtualizedCanvas(
            ObservableCollection<WorkflowNode> nodes,
            ObservableCollection<WorkflowConnection> connections)
        {
            _allNodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _allConnections = connections ?? throw new ArgumentNullException(nameof(connections));

            // 订阅集合变化事件
            _allNodes.CollectionChanged += (s, e) => UpdateVisibleNodes();
            _allConnections.CollectionChanged += (s, e) => UpdateVisibleConnections();

            // 初始化可见元素
            UpdateVisibleNodes();
            UpdateVisibleConnections();
        }

        /// <summary>
        /// 设置视图区域
        /// </summary>
        public void SetViewPort(double x, double y, double width, double height)
        {
            _viewPort = new Rect(x, y, width, height);
            UpdateVisibleNodes();
            UpdateVisibleConnections();
        }

        /// <summary>
        /// 更新可见节点
        /// </summary>
        public void UpdateVisibleNodes()
        {
            var visibleArea = new Rect(
                _viewPort.X - _bufferSize,
                _viewPort.Y - _bufferSize,
                _viewPort.Width + _bufferSize * 2,
                _viewPort.Height + _bufferSize * 2);

            var newVisibleNodes = new HashSet<string>();

            foreach (var node in _allNodes)
            {
                var nodeRect = new Rect(
                    node.Position.X,
                    node.Position.Y,
                    node.StyleConfig.NodeWidth,
                    node.StyleConfig.NodeHeight);

                if (visibleArea.IntersectsWith(nodeRect))
                {
                    newVisibleNodes.Add(node.Id);
                    node.IsVisible = true;
                }
                else
                {
                    node.IsVisible = false;
                }
            }

            _visibleNodes.Clear();
            foreach (var id in newVisibleNodes)
            {
                _visibleNodes.Add(id);
            }

            InvalidateVisual();
        }

        /// <summary>
        /// 更新可见连线
        /// </summary>
        public void UpdateVisibleConnections()
        {
            var visibleArea = new Rect(
                _viewPort.X - _bufferSize,
                _viewPort.Y - _bufferSize,
                _viewPort.Width + _bufferSize * 2,
                _viewPort.Height + _bufferSize * 2);

            var newVisibleConnections = new HashSet<string>();

            foreach (var connection in _allConnections)
            {
                var sourceNode = _allNodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                var targetNode = _allNodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                if (sourceNode != null && targetNode != null)
                {
                    var sourceRect = new Rect(
                        sourceNode.Position.X,
                        sourceNode.Position.Y,
                        sourceNode.StyleConfig.NodeWidth,
                        sourceNode.StyleConfig.NodeHeight);

                    var targetRect = new Rect(
                        targetNode.Position.X,
                        targetNode.Position.Y,
                        targetNode.StyleConfig.NodeWidth,
                        targetNode.StyleConfig.NodeHeight);

                    // 检查源节点、目标节点或连线本身是否在可见区域内
                    if (visibleArea.IntersectsWith(sourceRect) ||
                        visibleArea.IntersectsWith(targetRect) ||
                        IsConnectionInVisibleArea(connection, visibleArea))
                    {
                        newVisibleConnections.Add(connection.Id);
                        connection.IsVisible = true;
                    }
                    else
                    {
                        connection.IsVisible = false;
                    }
                }
            }

            _visibleConnections.Clear();
            foreach (var id in newVisibleConnections)
            {
                _visibleConnections.Add(id);
            }

            InvalidateVisual();
        }

        /// <summary>
        /// 检查连线是否在可见区域内
        /// </summary>
        private bool IsConnectionInVisibleArea(WorkflowConnection connection, Rect visibleArea)
        {
            // 如果连接线有路径点，检查路径点
            if (connection.PathPoints != null && connection.PathPoints.Count > 0)
            {
                foreach (var point in connection.PathPoints)
                {
                    if (visibleArea.Contains(point))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取可见节点集合（用于绑定）
        /// </summary>
        public ObservableCollection<WorkflowNode> GetVisibleNodes()
        {
            return new ObservableCollection<WorkflowNode>(
                _allNodes.Where(n => _visibleNodes.Contains(n.Id)));
        }

        /// <summary>
        /// 获取可见连线集合（用于绑定）
        /// </summary>
        public ObservableCollection<WorkflowConnection> GetVisibleConnections()
        {
            return new ObservableCollection<WorkflowConnection>(
                _allConnections.Where(c => _visibleConnections.Contains(c.Id)));
        }

        /// <summary>
        /// 获取虚拟化统计信息
        /// </summary>
        public VirtualizationStatistics GetStatistics()
        {
            return new VirtualizationStatistics
            {
                TotalNodes = _allNodes.Count,
                VisibleNodes = _visibleNodes.Count,
                TotalConnections = _allConnections.Count,
                VisibleConnections = _visibleConnections.Count,
                NodeVisibilityRate = _allNodes.Count > 0
                    ? (double)_visibleNodes.Count / _allNodes.Count * 100
                    : 0,
                ConnectionVisibilityRate = _allConnections.Count > 0
                    ? (double)_visibleConnections.Count / _allConnections.Count * 100
                    : 0
            };
        }

        /// <summary>
        /// 打印虚拟化统计信息
        /// </summary>
        public void PrintStatistics()
        {
            var stats = GetStatistics();
            var viewX1 = _viewPort.X;
            var viewY1 = _viewPort.Y;
            var viewX2 = _viewPort.X + _viewPort.Width;
            var viewY2 = _viewPort.Y + _viewPort.Height;
        }
    }

    /// <summary>
    /// 虚拟化统计信息
    /// </summary>
    public class VirtualizationStatistics
    {
        public int TotalNodes { get; set; }
        public int VisibleNodes { get; set; }
        public int TotalConnections { get; set; }
        public int VisibleConnections { get; set; }
        public double NodeVisibilityRate { get; set; }
        public double ConnectionVisibilityRate { get; set; }

        public override string ToString()
        {
            return $"节点: {VisibleNodes}/{TotalNodes} ({NodeVisibilityRate:F1}%), 连线: {VisibleConnections}/{TotalConnections} ({ConnectionVisibilityRate:F1}%)";
        }
    }
}
