using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// 虚拟化画布 - 只渲染视口内的节点和连接线
    /// </summary>
    public class VirtualizedCanvas : Canvas
    {
        private readonly ObservableCollection<WorkflowNode> _nodes;
        private readonly ObservableCollection<WorkflowConnection> _connections;
        private readonly Dictionary<string, FrameworkElement> _visibleNodes;
        private readonly Dictionary<string, FrameworkElement> _visibleConnections;
        private readonly ScaleTransform _scaleTransform;
        private readonly TranslateTransform _translateTransform;
        private readonly Services.ISpatialIndex _spatialIndex;

        private Rect _viewport;
        private double _scale = 1.0;
        private Point _offset = new Point(0, 0);

        /// <summary>
        /// 视口边界
        /// </summary>
        public Rect Viewport => _viewport;

        /// <summary>
        /// 缩放比例
        /// </summary>
        public double Scale
        {
            get => _scale;
            set
            {
                if (Math.Abs(_scale - value) > 0.001)
                {
                    _scale = value;
                    _scaleTransform.ScaleX = _scale;
                    _scaleTransform.ScaleY = _scale;
                    UpdateViewport();
                }
            }
        }

        /// <summary>
        /// 平移偏移
        /// </summary>
        public Point Offset
        {
            get => _offset;
            set
            {
                if (Math.Abs(_offset.X - value.X) > 0.001 || Math.Abs(_offset.Y - value.Y) > 0.001)
                {
                    _offset = value;
                    _translateTransform.X = _offset.X;
                    _translateTransform.Y = _offset.Y;
                    UpdateViewport();
                }
            }
        }

        /// <summary>
        /// 可见节点数量
        /// </summary>
        public int VisibleNodeCount => _visibleNodes.Count;

        /// <summary>
        /// 可见连接数量
        /// </summary>
        public int VisibleConnectionCount => _visibleConnections.Count;

        public VirtualizedCanvas(
            ObservableCollection<WorkflowNode> nodes,
            ObservableCollection<WorkflowConnection> connections)
        {
            _nodes = nodes;
            _connections = connections;
            _visibleNodes = new Dictionary<string, FrameworkElement>();
            _visibleConnections = new Dictionary<string, FrameworkElement>();
            _scaleTransform = new ScaleTransform(1.0, 1.0);
            _translateTransform = new TranslateTransform(0, 0);

            RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    _scaleTransform,
                    _translateTransform
                }
            };

            _spatialIndex = new Services.GridSpatialIndex(CanvasConfig.GridCellSize);

            ClipToBounds = true;

            SubscribeToCollections();
            UpdateViewport();
        }

        /// <summary>
        /// 更新视口
        /// </summary>
        public void UpdateViewport()
        {
            var actualWidth = ActualWidth > 0 ? ActualWidth : 1000;
            var actualHeight = ActualHeight > 0 ? ActualHeight : 800;

            _viewport = new Rect(
                -_offset.X / _scale,
                -_offset.Y / _scale,
                actualWidth / _scale,
                actualHeight / _scale
            );

            UpdateVisibleElements();
        }

        /// <summary>
        /// 强制刷新可见元素
        /// </summary>
        public void Refresh()
        {
            UpdateVisibleElements();
        }

        /// <summary>
        /// 获取可见节点
        /// </summary>
        public List<WorkflowNode> GetVisibleNodes()
        {
            return _spatialIndex.Query(_viewport);
        }

        /// <summary>
        /// 获取可见连接
        /// </summary>
        public List<WorkflowConnection> GetVisibleConnections()
        {
            return _connections.Where(c => IsConnectionVisible(c)).ToList();
        }

        /// <summary>
        /// 检查连接是否可见
        /// </summary>
        private bool IsConnectionVisible(WorkflowConnection connection)
        {
            var sourceNode = _nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = _nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

            if (sourceNode == null || targetNode == null)
                return false;

            var sourceBounds = GetNodeBounds(sourceNode);
            var targetBounds = GetNodeBounds(targetNode);

            return _viewport.IntersectsWith(sourceBounds) || _viewport.IntersectsWith(targetBounds);
        }

        private void UpdateVisibleElements()
        {
            var visibleNodeIds = new HashSet<string>();
            var visibleConnectionIds = new HashSet<string>();

            var visibleNodes = _spatialIndex.Query(_viewport);
            foreach (var node in visibleNodes)
            {
                visibleNodeIds.Add(node.Id);
                if (!_visibleNodes.ContainsKey(node.Id))
                {
                    var element = CreateNodeElement(node);
                    _visibleNodes[node.Id] = element;
                    Children.Add(element);
                }
            }

            var visibleConnections = _connections.Where(c => IsConnectionVisible(c));
            foreach (var connection in visibleConnections)
            {
                visibleConnectionIds.Add(connection.Id);
                if (!_visibleConnections.ContainsKey(connection.Id))
                {
                    var element = CreateConnectionElement(connection);
                    _visibleConnections[connection.Id] = element;
                    Children.Add(element);
                }
            }

            var nodesToRemove = _visibleNodes.Keys.Where(id => !visibleNodeIds.Contains(id)).ToList();
            foreach (var id in nodesToRemove)
            {
                Children.Remove(_visibleNodes[id]);
                _visibleNodes.Remove(id);
            }

            var connectionsToRemove = _visibleConnections.Keys.Where(id => !visibleConnectionIds.Contains(id)).ToList();
            foreach (var id in connectionsToRemove)
            {
                Children.Remove(_visibleConnections[id]);
                _visibleConnections.Remove(id);
            }

            UpdateZIndex();
        }

        private FrameworkElement CreateNodeElement(WorkflowNode node)
        {
            var border = new Border
            {
                Width = CanvasConfig.NodeWidth,
                Height = CanvasConfig.NodeHeight,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CanvasConfig.DefaultNodeBackground)),
                CornerRadius = new CornerRadius(CanvasConfig.NodeCornerRadius),
                BorderThickness = new Thickness(CanvasConfig.NodeBorderThickness),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CanvasConfig.DefaultNodeBorder)),
                Tag = node
            };

            Canvas.SetLeft(border, node.Position.X);
            Canvas.SetTop(border, node.Position.Y);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0066CC")),
                Padding = new Thickness(6),
                CornerRadius = new CornerRadius(3, 3, 0, 0)
            };
            Grid.SetRow(header, 0);

            var headerContent = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = new TextBlock { Text = "🔧", FontSize = 14 };
            var name = new TextBlock
            {
                Text = node.Name,
                Margin = new Thickness(6, 0, 0, 0),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.White
            };
            headerContent.Children.Add(icon);
            headerContent.Children.Add(name);
            header.Child = headerContent;

            var algorithm = new TextBlock
            {
                Text = node.AlgorithmType,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0066CC"))
            };
            Grid.SetRow(algorithm, 1);

            var footer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F8FF")),
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(0, 0, 3, 3)
            };
            Grid.SetRow(footer, 2);

            var footerContent = new TextBlock
            {
                Text = "⏱️ 0ms",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
            };
            footer.Child = footerContent;

            grid.Children.Add(header);
            grid.Children.Add(algorithm);
            grid.Children.Add(footer);

            border.Child = grid;

            return border;
        }

        private FrameworkElement CreateConnectionElement(WorkflowConnection connection)
        {
            var path = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(CanvasConfig.DefaultConnectionColor)),
                StrokeThickness = CanvasConfig.ConnectionStrokeThickness,
                StrokeLineJoin = PenLineJoin.Round,
                Tag = connection
            };

            UpdateConnectionPath(path, connection);

            return path;
        }

        private void UpdateConnectionPath(System.Windows.Shapes.Path path, WorkflowConnection connection)
        {
            var sourceNode = _nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = _nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

            if (sourceNode == null || targetNode == null)
                return;

            var sourcePos = new Point(
                sourceNode.Position.X + CanvasConfig.NodeWidth / 2,
                sourceNode.Position.Y + CanvasConfig.NodeHeight / 2
            );
            var targetPos = new Point(
                targetNode.Position.X + CanvasConfig.NodeWidth / 2,
                targetNode.Position.Y + CanvasConfig.NodeHeight / 2
            );

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = sourcePos,
                IsClosed = false
            };

            var midPoint1 = new Point(sourcePos.X, sourcePos.Y + (targetPos.Y - sourcePos.Y) / 2);
            var midPoint2 = new Point(targetPos.X, sourcePos.Y + (targetPos.Y - sourcePos.Y) / 2);

            pathFigure.Segments.Add(new BezierSegment(midPoint1, midPoint2, targetPos, true));
            pathGeometry.Figures.Add(pathFigure);

            path.Data = pathGeometry;
        }

        private void UpdateZIndex()
        {
            foreach (var kvp in _visibleConnections)
            {
                Panel.SetZIndex(kvp.Value, 1000);
            }

            foreach (var kvp in _visibleNodes)
            {
                Panel.SetZIndex(kvp.Value, 2000);
            }
        }

        private Rect GetNodeBounds(WorkflowNode node)
        {
            return new Rect(
                node.Position.X,
                node.Position.Y,
                CanvasConfig.NodeWidth,
                CanvasConfig.NodeHeight
            );
        }

        private void SubscribeToCollections()
        {
            _nodes.CollectionChanged += (s, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (WorkflowNode node in e.OldItems)
                    {
                        _spatialIndex.Remove(node);
                        if (_visibleNodes.TryGetValue(node.Id, out var element))
                        {
                            Children.Remove(element);
                            _visibleNodes.Remove(node.Id);
                        }
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (WorkflowNode node in e.NewItems)
                    {
                        _spatialIndex.Insert(node);
                    }
                }

                UpdateVisibleElements();
            };

            _connections.CollectionChanged += (s, e) =>
            {
                UpdateVisibleElements();
            };
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateViewport();
        }
    }
}
