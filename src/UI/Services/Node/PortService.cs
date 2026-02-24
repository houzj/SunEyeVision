using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Services.Node;
using SunEyeVision.UI.Views.Controls.Canvas;

namespace SunEyeVision.UI.Services.Node
{
    /// <summary>
    /// 端口方向枚举
    /// </summary>
    public enum PortDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    /// <summary>
    /// 端口信息
    /// </summary>
    public class PortInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public PortDirection Direction { get; set; }
        public Point Position { get; set; }
        public Ellipse? Element { get; set; }
    }

    /// <summary>
    /// 端口服务接口
    /// </summary>
    public interface IPortService
    {
        /// <summary>
        /// 获取指定节点的端口元�?
        /// </summary>
        Ellipse? GetPortElement(string nodeId, string portName);

        /// <summary>
        /// 从端口元素获取对应的节点
        /// </summary>
        WorkflowNode? GetNodeFromPort(Ellipse port);

        /// <summary>
        /// 在指定位置查找端�?
        /// </summary>
        Ellipse? FindPortAtPosition(Point position);

        /// <summary>
        /// 高亮端口
        /// </summary>
        void HighlightPort(Ellipse port, bool highlight);

        /// <summary>
        /// 清除所有端口高�?
        /// </summary>
        void ClearAllHighlights();

        /// <summary>
        /// 确定最佳端口方�?
        /// </summary>
        PortDirection DetermineBestPort(WorkflowNode source, WorkflowNode target);

        /// <summary>
        /// 获取端口位置
        /// </summary>
        Point GetPortPosition(WorkflowNode node, PortDirection direction);

        /// <summary>
        /// 获取端口信息
        /// </summary>
        PortInfo? GetPortInfo(Ellipse port);

        /// <summary>
        /// 清除端口缓存
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// 端口服务 - 管理端口查找、高亮和位置计算
    /// </summary>
    public class PortService : IPortService
    {
        private readonly System.Windows.Controls.Canvas _canvas;
        private readonly Dictionary<string, Ellipse> _portCache;
        private readonly object _cacheLock;

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public int CacheHits { get; private set; }

        /// <summary>
        /// 缓存未命中次�?
        /// </summary>
        public int CacheMisses { get; private set; }

        public PortService(System.Windows.Controls.Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _portCache = new Dictionary<string, Ellipse>();
            _cacheLock = new object();
        }

        public Ellipse? GetPortElement(string nodeId, string portName)
        {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(portName))
            {
                return null;
            }

            string cacheKey = $"{nodeId}_{portName}";

            lock (_cacheLock)
            {
                if (_portCache.TryGetValue(cacheKey, out var cachedPort))
                {
                    CacheHits++;
                    return cachedPort;
                }

                // 查找端口元素
                var port = FindVisualChild<Ellipse>(_canvas,
                    e => e.Name == portName &&
                         GetNodeIdFromElement(e) == nodeId);

                if (port != null)
                {
                    _portCache[cacheKey] = port;
                }

                CacheMisses++;
                return port;
            }
        }

        public WorkflowNode? GetNodeFromPort(Ellipse port)
        {
            if (port == null)
            {
                return null;
            }

            // 从端口元素获取节点信�?
            var nodeElement = FindVisualParent<Border>(port);
            if (nodeElement?.DataContext is WorkflowNode node)
            {
                return node;
            }
            return null;
        }

        public Ellipse? FindPortAtPosition(Point position)
        {
            // 使用 HitTest 查找端口
            var hitResults = VisualTreeHelper.HitTest(_canvas, position);
            if (hitResults != null)
            {
                var port = FindVisualParent<Ellipse>(hitResults.VisualHit);
                return port;
            }
            return null;
        }

        public void HighlightPort(Ellipse port, bool highlight)
        {
            if (port == null)
            {
                return;
            }

            if (highlight)
            {
                port.Stroke = Brushes.LimeGreen;
                port.StrokeThickness = 3;
                port.Opacity = 1.0;
            }
            else
            {
                port.Stroke = Brushes.Gray;
                port.StrokeThickness = 1;
                port.Opacity = 0.7;
            }
        }

        public void ClearAllHighlights()
        {
            // 查找所有端口并清除高亮
            var allPorts = FindAllVisualChildren<Ellipse>(_canvas);
            foreach (var port in allPorts)
            {
                HighlightPort(port, false);
            }
        }

        public PortDirection DetermineBestPort(WorkflowNode source, WorkflowNode target)
        {
            if (source == null || target == null)
            {
                return PortDirection.Right;
            }

            var deltaX = target.Position.X - source.Position.X;
            var deltaY = target.Position.Y - source.Position.Y;

            // 水平偏移主导
            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                return deltaX > 0 ? PortDirection.Right : PortDirection.Left;
            }
            else
            {
                return deltaY > 0 ? PortDirection.Bottom : PortDirection.Top;
            }
        }

        public Point GetPortPosition(WorkflowNode node, PortDirection direction)
        {
            if (node == null)
            {
                return new Point(0, 0);
            }

            var nodeCenterX = node.Position.X + CanvasConfig.NodeWidth / 2;
            var nodeCenterY = node.Position.Y + CanvasConfig.NodeHeight / 2;

            return direction switch
            {
                PortDirection.Top => new Point(nodeCenterX, node.Position.Y),
                PortDirection.Bottom => new Point(nodeCenterX, node.Position.Y + CanvasConfig.NodeHeight),
                PortDirection.Left => new Point(node.Position.X, nodeCenterY),
                PortDirection.Right => new Point(node.Position.X + CanvasConfig.NodeWidth, nodeCenterY),
                _ => new Point(nodeCenterX, nodeCenterY)
            };
        }

        public PortInfo? GetPortInfo(Ellipse port)
        {
            if (port == null)
            {
                return null;
            }

            var node = GetNodeFromPort(port);
            if (node == null)
            {
                return null;
            }

            var direction = DeterminePortDirectionFromName(port.Name);
            var position = GetPortPosition(node, direction);

            return new PortInfo
            {
                NodeId = node.Id,
                PortName = port.Name,
                Direction = direction,
                Position = position,
                Element = port
            };
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _portCache.Clear();
                CacheHits = 0;
                CacheMisses = 0;
            }
        }

        /// <summary>
        /// 从端口名称确定端口方�?
        /// </summary>
        private PortDirection DeterminePortDirectionFromName(string portName)
        {
            if (string.IsNullOrEmpty(portName))
            {
                return PortDirection.Right;
            }

            return portName.ToUpperInvariant() switch
            {
                "TOPPORT" => PortDirection.Top,
                "BOTTOMPORT" => PortDirection.Bottom,
                "LEFTPORT" => PortDirection.Left,
                "RIGHTPORT" => PortDirection.Right,
                _ => PortDirection.Right
            };
        }

        /// <summary>
        /// 在视觉树中查找子元素
        /// </summary>
        private T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            if (parent == null) return null;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t && (predicate == null || predicate(t)))
                {
                    return t;
                }

                var result = FindVisualChild(child, predicate);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// 查找所有指定类型的子元�?
        /// </summary>
        private List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var result = new List<T>();
            FindAllVisualChildrenRecursive(parent, result);
            return result;
        }

        private void FindAllVisualChildrenRecursive<T>(DependencyObject parent, List<T> result) where T : DependencyObject
        {
            if (parent == null) return;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                {
                    result.Add(t);
                }

                FindAllVisualChildrenRecursive(child, result);
            }
        }

        /// <summary>
        /// 在视觉树中查找父元素
        /// </summary>
        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T t) return t;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// 从元素获取节点ID
        /// </summary>
        private string? GetNodeIdFromElement(DependencyObject element)
        {
            var nodeElement = FindVisualParent<Border>(element);
            if (nodeElement?.DataContext is WorkflowNode node)
            {
                return node.Id;
            }
            return null;
        }
    }
}
