using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// AIStudioDiagramControl - AIStudio风格的画布控件
    /// 使用简化的实现，与WorkflowCanvasControl兼容
    /// </summary>
    public partial class AIStudioDiagramControl : UserControl
    {
        private ObservableCollection<WorkflowNode> _nodes = new ObservableCollection<WorkflowNode>();
        private ObservableCollection<WorkflowConnection> _connections = new ObservableCollection<WorkflowConnection>();
        private bool _isInitialized = false;
        private MainWindowViewModel? _viewModel;

        // 连接线路径缓存
        private Services.ConnectionPathCache? _connectionPathCache;

        // 存储连接线的映射：connectionId -> (Line/Path, original connection)
        private Dictionary<string, FrameworkElement> _connectionElements = new Dictionary<string, FrameworkElement>();

        public AIStudioDiagramControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            // 尝试从主窗口获取 ViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
            }
        }

        /// <summary>
        /// 初始化控件
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] 控件已初始化，跳过重复初始化");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] ========== 控件初始化开始 ==========");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] DiagramCanvas: {DiagramCanvas != null}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] NodesLayer: {NodesLayer != null}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ConnectionsLayer: {ConnectionsLayer != null}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] EmptyStateText: {EmptyStateText != null}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 初始节点数: {_nodes.Count}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 初始连接数: {_connections.Count}");

                // 初始化连接线路径缓存
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] 初始化路径计算器...");
                try
                {
                    var calculator = Services.PathCalculators.PathCalculatorFactory.CreateCalculator();
                    _connectionPathCache = new Services.ConnectionPathCache(_nodes, calculator);
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] ✅ 路径计算器初始化成功");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ❌ 路径计算器初始化失败: {ex.Message}");
                }

                _isInitialized = true;

                // 使用 Dispatcher 确保在 UI 线程上执行渲染
                Dispatcher.Invoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] Dispatcher.Invoke - 开始渲染");
                    // 初始化渲染
                    RenderNodes();
                    RenderConnections();
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] Dispatcher.Invoke - 渲染完成");
                });

                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] ========== 控件初始化完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 初始化失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Loaded事件处理
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_OnLoaded] ========== Loaded 事件触发 ==========");

            // 检查 DiagramCanvas 是否存在
            if (DiagramCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_OnLoaded] ❌ DiagramCanvas 为 null");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_OnLoaded] ✓ DiagramCanvas 存在");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_OnLoaded]   AllowDrop: {DiagramCanvas.AllowDrop}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_OnLoaded]   Width: {DiagramCanvas.Width}, Height: {DiagramCanvas.Height}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_OnLoaded]   ClipToBounds: {DiagramCanvas.ClipToBounds}");
            }

            // 检查 NodesLayer 和 ConnectionsLayer
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_OnLoaded] NodesLayer: {(NodesLayer != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_OnLoaded] ConnectionsLayer: {(ConnectionsLayer != null ? "存在" : "null")}");

            // 获取 MainWindowViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (_viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_OnLoaded] ✓ ViewModel 已获取");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_OnLoaded]   ViewModel type: {_viewModel.GetType().Name}");

                    // 订阅节点集合变化
                    if (_viewModel.WorkflowTabViewModel.SelectedTab is WorkflowTabViewModel workflowTab)
                    {
                        SubscribeToWorkflowChanges(workflowTab);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_OnLoaded] ❌ 获取 ViewModel 失败");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_OnLoaded] ❌ 无法获取 MainWindow");
            }

            Initialize();
        }

        /// <summary>
        /// 订阅工作流集合变化
        /// </summary>
        private void SubscribeToWorkflowChanges(WorkflowTabViewModel workflowTab)
        {
            System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] 订阅工作流集合变化");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] WorkflowNodes: {workflowTab.WorkflowNodes != null}, Count: {workflowTab.WorkflowNodes?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] WorkflowConnections: {workflowTab.WorkflowConnections != null}, Count: {workflowTab.WorkflowConnections?.Count ?? 0}");

            // 订阅节点集合变化
            if (workflowTab.WorkflowNodes is ObservableCollection<WorkflowNode> nodes)
            {
                nodes.CollectionChanged += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"========== [AIStudioDiagramControl] 节点集合变化触发 ==========");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] Action: {args.Action}");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] NewItems: {args.NewItems?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] OldItems: {args.OldItems?.Count ?? 0}");

                    if (args.NewItems != null)
                    {
                        foreach (WorkflowNode item in args.NewItems)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 新增节点: ID={item.Id}, Name={item.Name}, Position=({item.Position.X}, {item.Position.Y})");
                        }
                    }

                    if (args.OldItems != null)
                    {
                        foreach (WorkflowNode item in args.OldItems)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 移除节点: ID={item.Id}, Name={item.Name}");
                        }
                    }

                    // 同步内容而不是替换集合
                    SyncNodesFromViewModel(workflowTab.WorkflowNodes);
                    OnPropertyChanged(nameof(Nodes));

                    // 重新渲染
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 准备调用 RenderNodes()");
                    RenderNodes();
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] RenderNodes 完成");
                    System.Diagnostics.Debug.WriteLine($"========== [AIStudioDiagramControl] 节点集合变化处理完成 ==========");
                };
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ✓ 已订阅节点集合变化");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ❌ WorkflowNodes 不是 ObservableCollection<WorkflowNode>");
            }

            // 订阅连接集合变化
            if (workflowTab.WorkflowConnections is ObservableCollection<WorkflowConnection> connections)
            {
                connections.CollectionChanged += (s, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 连接集合变化: Action={args.Action}, NewItems={args.NewItems?.Count ?? 0}, OldItems={args.OldItems?.Count ?? 0}");

                    // 同步内容而不是替换集合
                    SyncConnectionsFromViewModel(workflowTab.WorkflowConnections);
                    OnPropertyChanged(nameof(Connections));

                    // 重新渲染
                    RenderConnections();
                };
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ✓ 已订阅连接集合变化");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ❌ WorkflowConnections 不是 ObservableCollection<WorkflowConnection>");
            }
        }

        /// <summary>
        /// 从ViewModel同步节点内容到内部集合
        /// </summary>
        private void SyncNodesFromViewModel(ObservableCollection<WorkflowNode> viewModelNodes)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 同步节点内容: ViewModel.Count={viewModelNodes.Count}, _nodes.Count={_nodes.Count}");

            // 清空并重新添加
            _nodes.Clear();
            foreach (var node in viewModelNodes)
            {
                _nodes.Add(node);
            }

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 同步后 _nodes.Count: {_nodes.Count}");
        }

        /// <summary>
        /// 从ViewModel同步连接内容到内部集合
        /// </summary>
        private void SyncConnectionsFromViewModel(ObservableCollection<WorkflowConnection> viewModelConnections)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 同步连接内容: ViewModel.Count={viewModelConnections.Count}, _connections.Count={_connections.Count}");

            // 清空并重新添加
            _connections.Clear();
            foreach (var connection in viewModelConnections)
            {
                _connections.Add(connection);
            }

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 同步后 _connections.Count: {_connections.Count}");
        }

        #region 公共属性

        /// <summary>
        /// 节点集合
        /// </summary>
        public ObservableCollection<WorkflowNode> Nodes
        {
            get => _nodes;
            set
            {
                if (_nodes != value)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Nodes] ========== Nodes 属性被设置 ==========");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Nodes] 旧节点数: {_nodes?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Nodes] 新节点数: {value?.Count ?? 0}");
                    _nodes = value ?? new ObservableCollection<WorkflowNode>();
                    OnPropertyChanged(nameof(Nodes));
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Nodes] 准备调用 RenderNodes()");
                    RenderNodes();
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Nodes] ========== Nodes 属性设置完成 ==========");
                }
            }
        }

        /// <summary>
        /// 连接集合
        /// </summary>
        public ObservableCollection<WorkflowConnection> Connections
        {
            get => _connections;
            set
            {
                if (_connections != value)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Connections] ========== Connections 属性被设置 ==========");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Connections] 旧连接数: {_connections?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Connections] 新连接数: {value?.Count ?? 0}");
                    _connections = value ?? new ObservableCollection<WorkflowConnection>();
                    OnPropertyChanged(nameof(Connections));
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Connections] 准备调用 RenderConnections()");
                    RenderConnections();
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Connections] ========== Connections 属性设置完成 ==========");
                }
            }
        }

        /// <summary>
        /// 当前工作流ID
        /// </summary>
        public string WorkflowId { get; set; } = string.Empty;

        /// <summary>
        /// 当前工作流名称
        /// </summary>
        public string WorkflowName { get; set; } = string.Empty;

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(WorkflowNode node)
        {
            System.Diagnostics.Debug.WriteLine("========== [AIStudioDiagramControl.AddNode] 开始添加节点 ==========");
            if (node == null)
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl.AddNode] ❌ 节点为 null，跳过");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] 节点信息: ID={node.Id}, Name={node.Name}, Position=({node.Position.X}, {node.Position.Y})");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] 添加前 _nodes.Count: {_nodes.Count}");

            _nodes.Add(node);

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] ✓ 节点已添加到 _nodes 集合");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] 添加后 _nodes.Count: {_nodes.Count}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] 准备触发 NodeAdded 事件");

            NodeAdded?.Invoke(this, EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] NodeAdded 事件已触发");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] 准备调用 RenderNodes()");

            RenderNodes();

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] RenderNodes 已完成");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddNode] 当前 NodesLayer.Children.Count: {NodesLayer?.Children.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine("========== [AIStudioDiagramControl.AddNode] 添加节点完成 ==========");
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveNode] ========== 开始移除节点 ==========");
            if (string.IsNullOrEmpty(nodeId))
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveNode] ❌ 节点ID为空，跳过");
                return;
            }

            var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                _nodes.Remove(node);
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveNode] ✓ 节点已移除: {node.Name}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveNode] 当前 _nodes.Count: {_nodes.Count}");
                NodeRemoved?.Invoke(this, EventArgs.Empty);
                RenderNodes();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveNode] ❌ 未找到节点: {nodeId}");
            }
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveNode] ========== 移除节点完成 ==========");
        }

        /// <summary>
        /// 添加连接
        /// </summary>
        public void AddConnection(WorkflowConnection connection)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] ========== 开始添加连接 ==========");
            if (connection == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] ❌ 连接为 null，跳过");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] 连接信息: ID={connection.Id}, SourceNodeId={connection.SourceNodeId}, TargetNodeId={connection.TargetNodeId}");
            _connections.Add(connection);
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] ✓ 连接已添加到 _connections 集合");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] 当前 _connections.Count: {_connections.Count}");
            ConnectionAdded?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] 准备调用 RenderConnections()");
            RenderConnections();
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.AddConnection] ========== 添加连接完成 ==========");
        }

        /// <summary>
        /// 移除连接
        /// </summary>
        public void RemoveConnection(string connectionId)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveConnection] ========== 开始移除连接 ==========");
            if (string.IsNullOrEmpty(connectionId))
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveConnection] ❌ 连接ID为空，跳过");
                return;
            }

            var connection = _connections.FirstOrDefault(c => c.Id == connectionId);
            if (connection != null)
            {
                _connections.Remove(connection);
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveConnection] ✓ 连接已移除: {connection.Id}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveConnection] 当前 _connections.Count: {_connections.Count}");
                ConnectionRemoved?.Invoke(this, EventArgs.Empty);
                RenderConnections();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveConnection] ❌ 未找到连接: {connectionId}");
            }
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RemoveConnection] ========== 移除连接完成 ==========");
        }

        /// <summary>
        /// 清空画布
        /// </summary>
        public void Clear()
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] ========== 开始清空画布 ==========");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] 清空前节点数: {_nodes.Count}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] 清空前连接数: {_connections.Count}");
            _nodes.Clear();
            _connections.Clear();
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] ✓ 节点和连接已清空");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] 清空后节点数: {_nodes.Count}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] 清空后连接数: {_connections.Count}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] 准备调用 RenderNodes()");
            RenderNodes();
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] 准备调用 RenderConnections()");
            RenderConnections();
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.Clear] ========== 清空画布完成 ==========");
        }

        /// <summary>
        /// 加载流程图数据（JSON格式）
        /// </summary>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                // 清空现有数据
                Clear();

                // 反序列化节点和连接
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                if (data.TryGetProperty("nodes", out var nodesElement))
                {
                    foreach (var nodeElement in nodesElement.EnumerateArray())
                    {
                        var a = nodeElement.GetRawText();
                        var node = JsonSerializer.Deserialize<WorkflowNode>(a);
                        if (node != null)
                        {
                            _nodes.Add(node);
                        }
                    }
                }

                if (data.TryGetProperty("connections", out var connectionsElement))
                {
                    foreach (var connElement in connectionsElement.EnumerateArray())
                    {
                        var b = connElement.GetRawText();
                        var connection = JsonSerializer.Deserialize<WorkflowConnection>(b);
                        if (connection != null)
                        {
                            _connections.Add(connection);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 已从JSON加载 {_nodes.Count} 个节点和 {_connections.Count} 个连接");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载流程图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存流程图数据（JSON格式）
        /// </summary>
        public string SaveToJson()
        {
            try
            {
                var data = new
                {
                    workflowId = WorkflowId,
                    workflowName = WorkflowName,
                    nodes = _nodes,
                    connections = _connections
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                return JsonSerializer.Serialize(data, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存流程图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// 加载流程图文件
        /// </summary>
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"文件不存在: {filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                LoadFromJson(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存流程图到文件
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var json = SaveToJson();
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 缩放画布
        /// </summary>
        public void Zoom(double zoomLevel)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 缩放至: {zoomLevel:F2}");
            // 简化实现，暂不实现缩放功能
        }

        /// <summary>
        /// 适应画布大小
        /// </summary>
        public void FitToScreen()
        {
            System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] 适应屏幕大小");
            // 简化实现，暂不实现适应屏幕功能
        }

        /// <summary>
        /// 居中显示
        /// </summary>
        public void CenterView()
        {
            System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl] 居中显示");
            // 简化实现，暂不实现居中显示功能
        }

        /// <summary>
        /// 导出所有节点
        /// </summary>
        public ObservableCollection<WorkflowNode> ExportNodes()
        {
            return new ObservableCollection<WorkflowNode>(_nodes);
        }

        /// <summary>
        /// 导出所有连接
        /// </summary>
        public ObservableCollection<WorkflowConnection> ExportConnections()
        {
            return new ObservableCollection<WorkflowConnection>(_connections);
        }

        #endregion

        #region 公共事件

        /// <summary>
        /// 节点添加事件
        /// </summary>
        public event EventHandler? NodeAdded;

        /// <summary>
        /// 节点移除事件
        /// </summary>
        public event EventHandler? NodeRemoved;

        /// <summary>
        /// 连接添加事件
        /// </summary>
        public event EventHandler? ConnectionAdded;

        /// <summary>
        /// 连接移除事件
        /// </summary>
        public event EventHandler? ConnectionRemoved;

        /// <summary>
        /// 选择改变事件
        /// </summary>
        public event EventHandler? SelectionChanged;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region 渲染方法

        /// <summary>
        /// 渲染节点
        /// </summary>
        private void RenderNodes()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] ========== 开始渲染节点 ==========");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] NodesLayer: {NodesLayer != null}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] 当前节点数: {_nodes.Count}");

                // 清空现有节点
                NodesLayer.Children.Clear();
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] 已清空 NodesLayer，当前 Children 数量: {NodesLayer.Children.Count}");

                foreach (var node in _nodes)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] 渲染节点: ID={node.Id}, Name={node.Name}, Position=({node.Position.X}, {node.Position.Y})");

                    // 创建节点形状
                    var nodeShape = CreateNodeShape(node);
                    if (nodeShape != null)
                    {
                        Canvas.SetLeft(nodeShape, node.Position.X);
                        Canvas.SetTop(nodeShape, node.Position.Y);
                        NodesLayer.Children.Add(nodeShape);
                        System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] ✓ 节点已添加到画布");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] ❌ 节点形状为空");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] 渲染完成，NodesLayer.Children.Count: {NodesLayer.Children.Count}");
                UpdateEmptyState();
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] ========== 渲染节点完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] 渲染节点失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderNodes] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 创建节点形状
        /// </summary>
        private FrameworkElement CreateNodeShape(WorkflowNode node)
        {
            try
            {
                var rectangle = new Rectangle
                {
                    Width = 120,
                    Height = 60,
                    Fill = new SolidColorBrush(Colors.LightBlue),
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2,
                    RadiusX = 8,
                    RadiusY = 8
                };

                // 如果节点被选中，添加边框
                if (node.IsSelected)
                {
                    rectangle.Stroke = new SolidColorBrush(Colors.Red);
                    rectangle.StrokeThickness = 3;
                }

                // 添加节点标签
                var textBlock = new TextBlock
                {
                    Text = node.Name,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Colors.Black),
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 110,
                    MaxHeight = 50
                };

                // 将文本块添加到节点形状的视觉树中
                var grid = new Grid
                {
                    Width = 120,
                    Height = 60
                };
                grid.Children.Add(rectangle);
                grid.Children.Add(textBlock);

                // 返回 Grid 作为节点形状
                return grid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 创建节点形状失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 渲染连接线
        /// </summary>
        private void RenderConnections()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] ========== 开始渲染连接线 ==========");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] ConnectionsLayer: {ConnectionsLayer != null}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 当前连接数: {_connections.Count}");

                // 清空现有连接
                ConnectionsLayer.Children.Clear();
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 已清空 ConnectionsLayer，当前 Children 数量: {ConnectionsLayer.Children.Count}");

                foreach (var connection in _connections)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 渲染连接: ID={connection.Id}, SourceNodeId={connection.SourceNodeId}, TargetNodeId={connection.TargetNodeId}");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 连接位置: Source=({connection.SourcePosition.X}, {connection.SourcePosition.Y}), Target=({connection.TargetPosition.X}, {connection.TargetPosition.Y})");

                    var connectionLine = CreateConnectionLine(connection);
                    if (connectionLine != null)
                    {
                        ConnectionsLayer.Children.Add(connectionLine);
                        _connectionElements[connection.Id] = connectionLine;
                        System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] ✓ 连接线已添加到画布");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] ❌ 连接线为空");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 渲染完成，ConnectionsLayer.Children.Count: {ConnectionsLayer.Children.Count}");
                UpdateEmptyState();
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] ========== 渲染连接线完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 渲染连接线失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.RenderConnections] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        private FrameworkElement CreateConnectionLine(WorkflowConnection connection)
        {
            try
            {
                // 如果有路径计算器，使用它生成路径
                if (_connectionPathCache != null)
                {
                    var pathGeometry = _connectionPathCache.GetPath(connection);
                    if (pathGeometry != null)
                    {
                        var path = new System.Windows.Shapes.Path
                        {
                            Data = pathGeometry,
                            Stroke = new SolidColorBrush(Colors.Black),
                            StrokeThickness = 2,
                            StrokeDashArray = new DoubleCollection { 5, 5 }
                        };

                        // 存储到字典中
                        _connectionElements[connection.Id] = path;
                        return path;
                    }
                }

                // 回退方案：使用简单的直线
                var line = new Line
                {
                    X1 = connection.SourcePosition.X,
                    Y1 = connection.SourcePosition.Y,
                    X2 = connection.TargetPosition.X,
                    Y2 = connection.TargetPosition.Y,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 5 }
                };

                // 存储到字典中
                _connectionElements[connection.Id] = line;
                return line;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 创建连接线失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 设置路径计算器（支持运行时切换）
        /// </summary>
        public void SetPathCalculator(string pathCalculatorType)
        {
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] 切换路径计算器到: {pathCalculatorType}");

            try
            {
                // 解析路径计算器类型
                if (System.Enum.TryParse<Services.PathCalculators.PathCalculatorType>(pathCalculatorType, true, out var type))
                {
                    // 创建新的路径计算器实例
                    var newCalculator = Services.PathCalculators.PathCalculatorFactory.CreateCalculator(type);

                    // 替换ConnectionPathCache
                    _connectionPathCache = new Services.ConnectionPathCache(_nodes, newCalculator);

                    // 重新渲染所有连接线
                    RenderConnections();

                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ✅ 路径计算器已切换到: {type}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ❌ 未知的路径计算器类型: {pathCalculatorType}");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl] ❌ 切换路径计算器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新空状态显示
        /// </summary>
        private void UpdateEmptyState()
        {
            if (EmptyStateText == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.UpdateEmptyState] ❌ EmptyStateText 为 null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.UpdateEmptyState] 节点数: {_nodes.Count}, 连接数: {_connections.Count}");

            if (_nodes.Count == 0 && _connections.Count == 0)
            {
                EmptyStateText.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.UpdateEmptyState] ✓ 设置 EmptyStateText 为 Visible");
            }
            else
            {
                EmptyStateText.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl.UpdateEmptyState] ✓ 设置 EmptyStateText 为 Collapsed");
            }
        }

        #endregion

        #region 拖放事件处理

        /// <summary>
        /// 拖放进入事件
        /// </summary>
        private void DiagramCanvas_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("========== [AIStudioDiagramControl_DragEnter] 拖拽进入 ==========");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] Sender type: {sender?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] 数据格式: {e.Data.GetFormats().FirstOrDefault()}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] ViewModel: {(_viewModel != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] 当前节点数: {_nodes.Count}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] DiagramCanvas.Visibility: {DiagramCanvas?.Visibility}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] DiagramCanvas.IsEnabled: {DiagramCanvas?.IsEnabled}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] DiagramCanvas.AllowDrop: {DiagramCanvas?.AllowDrop}");
            System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_DragEnter] NodesLayer.Children.Count: {NodesLayer?.Children.Count ?? 0}");

            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_DragEnter] ✓ 拖拽进入画布，设置 Copy 效果");
            }
            else
            {
                e.Effects = DragDropEffects.None;
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_DragEnter] ❌ 数据格式不支持，设置 None 效果");
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖放悬停事件
        /// </summary>
        private void DiagramCanvas_DragOver(object sender, DragEventArgs e)
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
        private void DiagramCanvas_DragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// 拖放放下事件 - 创建新节点
        /// </summary>
        private void DiagramCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========== [AIStudioDiagramControl_Drop] 开始拖放处理 ==========");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] Sender type: {sender?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ViewModel: {(_viewModel != null ? "存在" : "null")}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 数据格式: {e.Data.GetFormats().FirstOrDefault()}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] DragDropEffects: {e.Effects}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 当前画布可见性: {this.Visibility}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 当前 _nodes.Count: {_nodes.Count}");

                if (sender is not Canvas canvas)
                {
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_Drop] ❌ Sender 不是 Canvas");
                    return;
                }

                if (!e.Data.GetDataPresent("ToolItem"))
                {
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_Drop] ❌ 数据不包含 ToolItem");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 可用格式: {string.Join(", ", e.Data.GetFormats())}");
                    return;
                }

                var item = e.Data.GetData("ToolItem") as ToolItem;
                if (item == null)
                {
                    System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_Drop] ❌ ToolItem 为 null");
                    return;
                }

                // 获取放置位置
                Point dropPosition = e.GetPosition(canvas);
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 放置位置: ({dropPosition.X:F0}, {dropPosition.Y:F0})");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 拖放工具: ToolId={item.ToolId}, Name={item.Name}");

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ❌ ToolItem 的 ToolId 为空");
                    return;
                }

                // 从 MainWindow 获取 ViewModel
                if (_viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ❌ ViewModel 为 null");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 尝试重新获取 ViewModel...");
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        _viewModel = mainWindow.DataContext as MainWindowViewModel;
                        System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 重新获取 ViewModel 结果: {(_viewModel != null ? "成功" : "失败")}");
                    }
                }

                if (_viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ❌ 仍然无法获取 ViewModel");
                    return;
                }

                if (_viewModel.WorkflowTabViewModel.SelectedTab is not WorkflowTabViewModel workflowTab)
                {
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ❌ 无法获取 WorkflowTabViewModel");
                    System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] SelectedTab type: {_viewModel.WorkflowTabViewModel.SelectedTab?.GetType().Name}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ✓ 获取到 WorkflowTab: {workflowTab.Name}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 当前 WorkflowNodes.Count: {workflowTab.WorkflowNodes.Count}");

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点，自动分配序号
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ✓ 节点创建成功: {newNode.Name}, 位置=({newNode.Position.X:F0}, {newNode.Position.Y:F0})");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop]   Node ID: {newNode.Id}, Index: {newNode.Index}, GlobalIndex: {newNode.GlobalIndex}");

                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 准备添加节点到 WorkflowNodes...");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 添加前 WorkflowNodes.Count: {workflowTab.WorkflowNodes.Count}");

                // 添加新节点到工作流
                workflowTab.WorkflowNodes.Add(newNode);

                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ✓ 节点已添加到工作流");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 添加后 WorkflowNodes.Count: {workflowTab.WorkflowNodes.Count}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 当前 _nodes.Count: {_nodes.Count}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ========== 拖放处理完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] ❌ 拖放异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_Drop] 堆栈跟踪: {ex.StackTrace}");
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 鼠标事件测试（用于诊断）

        /// <summary>
        /// DiagramCanvas 鼠标移动 - 测试 Canvas 是否能接收鼠标事件
        /// </summary>
        private void DiagramCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // 只在第一次鼠标移动时输出日志，避免刷屏
            if (!_mouseMoveLogged)
            {
                System.Diagnostics.Debug.WriteLine("[AIStudioDiagramControl_MouseMove] ✓ Canvas 接收到鼠标移动事件");
                var pos = e.GetPosition(DiagramCanvas);
                System.Diagnostics.Debug.WriteLine($"[AIStudioDiagramControl_MouseMove]   鼠标位置: ({pos.X:F0}, {pos.Y:F0})");
                _mouseMoveLogged = true;
            }
        }

        private bool _mouseMoveLogged = false;

        #endregion
    }
}
