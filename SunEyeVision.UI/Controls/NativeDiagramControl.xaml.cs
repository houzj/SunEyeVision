using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using AIStudio.Wpf.DiagramDesigner.ViewModels;
using AIStudio.Wpf.DiagramDesigner;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// NativeDiagramControl - 使用AIStudio.Wpf.DiagramDesigner原生库
    /// 支持贝塞尔曲线连接、缩放平移、对齐吸附、撤销重做
    /// </summary>
    public partial class NativeDiagramControl : UserControl
    {
        private ObservableCollection<WorkflowNode> _nodes = new ObservableCollection<WorkflowNode>();
        private ObservableCollection<WorkflowConnection> _connections = new ObservableCollection<WorkflowConnection>();
        private bool _isInitialized = false;
        private MainWindowViewModel? _viewModel;

        // 原生图表相关
        private DiagramViewModel? _diagramViewModel;  // DiagramViewModel
        private DiagramControl? _diagramControl;   // DiagramControl
        private DiagramAdapter? _adapter;

        // 拖放去重
        private string? _lastDragDropId = null;
        private DateTime _lastDropTime = DateTime.MinValue;

        public NativeDiagramControl()
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
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 控件已初始化，跳过重复初始化");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ========== 控件初始化开始 ==========");

                // 初始化适配器
                _adapter = new DiagramAdapter();

                // 加载原生图表控件
                LoadNativeDiagram();

                // 配置原生图表功能
                ConfigureDiagramFeatures();

                // 同步数据
                SyncData();

                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ========== 控件初始化完成 ==========");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ 初始化失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 堆栈跟踪: {ex.StackTrace}");

                // 显示错误信息
                MessageBox.Show($"初始化失败: {ex.Message}\n请确保已安装 AIStudio.Wpf.DiagramDesigner 包",
                    "NativeDiagramControl", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载原生图表控件
        /// 创建 DiagramViewModel 和 DiagramControl
        /// </summary>
        private void LoadNativeDiagram()
        {
            try
            {
                // 创建 DiagramViewModel 实例
                _diagramViewModel = new DiagramViewModel();
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ✅ DiagramViewModel 创建成功");

                // 创建 DiagramControl 实例
                _diagramControl = new DiagramControl();


                // === 新增：设置画布尺寸为 10000x10000 ===
                _diagramControl.Width = 10000;
                _diagramControl.Height = 10000;
                
            
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ✅ 画布尺寸设置为 10000x10000");
                // ==========================================

                // 启用拖放
                _diagramControl.AllowDrop = true;
                _diagramControl.DragEnter += DiagramControl_DragEnter;
                _diagramControl.DragOver += DiagramControl_DragOver;
                _diagramControl.Drop += DiagramControl_Drop;

                // 设置 DiagramControl 的 DataContext 为 DiagramViewModel
                _diagramControl.DataContext = _diagramViewModel;

                // 设置到容器
                DiagramContainer.Content = _diagramControl;
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ✅ DiagramControl 加载成功 (画布: 10000x10000)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ 加载原生图表失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 配置原生图表功能
        /// </summary>
        private void ConfigureDiagramFeatures()
        {
            if (_diagramViewModel == null)
                return;

            try
            {
                // 配置网格吸附
                _diagramViewModel.DiagramOption.SnappingOption.EnableSnapping = true;
                _diagramViewModel.DiagramOption.SnappingOption.SnappingRadius = 20.0;
                _diagramViewModel.DiagramOption.SnappingOption.BlockSnappingRadius = 20.0;

                // 配置网格大小
                _diagramViewModel.DiagramOption.LayoutOption.GridCellWidth = 20.0;
                _diagramViewModel.DiagramOption.LayoutOption.GridCellHeight = 20.0;

                // 显示网格
                _diagramViewModel.DiagramOption.LayoutOption.ShowGrid = true;

                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ✅ 原生图表功能配置完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ⚠ 配置原生图表功能失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步数据
        /// </summary>
        private void SyncData()
        {
            if (_diagramViewModel == null || _adapter == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 同步数据 - Nodes: {_nodes.Count}, Connections: {_connections.Count}");

                // 同步节点（传入 DiagramViewModel）
                _adapter.SyncNodes(_nodes, _diagramViewModel);

                // 同步连接（传入 DiagramViewModel）
                _adapter.SyncConnections(_connections, _diagramViewModel);

                // 更新空状态
                UpdateEmptyState();

                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ✅ 数据同步完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ 数据同步失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新空状态显示
        /// </summary>
        private void UpdateEmptyState()
        {
            if (EmptyStateText == null)
                return;

            if (_nodes.Count == 0 && _connections.Count == 0)
            {
                EmptyStateText.Visibility = Visibility.Visible;
            }
            else
            {
                EmptyStateText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Loaded事件处理
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ========== Loaded 事件触发 ==========");

            // 检查DataContext
            var dataContext = this.DataContext;
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] DataContext: {dataContext?.GetType().Name ?? "null"}");

            // 获取 MainWindowViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] MainWindow: {mainWindow?.GetType().Name ?? "null"}");
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (_viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ✓ ViewModel 已获取");

                    if (_viewModel.WorkflowTabViewModel == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ❌ WorkflowTabViewModel 为 null");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] WorkflowTabViewModel.SelectedTab: {_viewModel.WorkflowTabViewModel.SelectedTab?.Name ?? "null"}");

                    // 订阅节点集合变化
                    if (_viewModel.WorkflowTabViewModel.SelectedTab is WorkflowTabViewModel workflowTab)
                    {
                        SubscribeToWorkflowChanges(workflowTab);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ❌ SelectedTab 不是 WorkflowTabViewModel");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] ❌ 无法获取 ViewModel");
                }
            }
        }

        /// <summary>
        /// 订阅工作流变化
        /// </summary>
        private void SubscribeToWorkflowChanges(WorkflowTabViewModel workflowTab)
        {
            _nodes = workflowTab.WorkflowNodes;
            _connections = workflowTab.WorkflowConnections;

            _nodes.CollectionChanged += OnNodesCollectionChanged;
            _connections.CollectionChanged += OnConnectionsCollectionChanged;

            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ✓ 订阅到工作流 - Nodes: {_nodes.Count}, Connections: {_connections.Count}");

            // 初始化并同步数据
            if (!_isInitialized)
            {
                Initialize();
            }
            else
            {
                SyncData();
            }
        }

        /// <summary>
        /// 节点集合变化处理
        /// </summary>
        private void OnNodesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 节点集合变化 - Action: {e.Action}");

            if (_adapter != null && _diagramViewModel != null)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (WorkflowNode node in e.NewItems!)
                        {
                            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] OnNodesCollectionChanged: 添加节点 {node.Id}");

                            // 使用公开方法创建节点（不使用反射）
                            var nativeNode = _adapter.CreateNativeNode(node, _diagramViewModel);
                            _diagramViewModel.Add(nativeNode);
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        // TODO: 实现节点删除
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        SyncData();
                        break;
                }
            }

            UpdateEmptyState();
        }

        /// <summary>
        /// 连接集合变化处理
        /// </summary>
        private void OnConnectionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 连接集合变化 - Action: {e.Action}");

            if (_adapter != null && _diagramViewModel != null)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (WorkflowConnection connection in e.NewItems!)
                        {
                            try
                            {
                                // 重新创建连接（传入 DiagramViewModel）
                                var createConnectionMethod = _adapter.GetType().GetMethod("CreateConnectionInternal",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                if (createConnectionMethod != null)
                                {
                                    var nativeConnection = createConnectionMethod.Invoke(_adapter, new object[] { connection, _diagramViewModel });
                                    _adapter.AddConnection(nativeConnection, _diagramViewModel);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ 创建连接失败: {ex.Message}");
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        // TODO: 实现连接删除
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        SyncData();
                        break;
                }
            }

            UpdateEmptyState();
        }

        /// <summary>
        /// 节点集合变化处理
        /// </summary>
        private void DiagramControl_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] DragEnter 事件触发");
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] GetDataPresent('ToolItem'): {e.Data.GetDataPresent("ToolItem")}");

            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] DragEnter: 设置 Copy 效果");
            }
            else
            {
                e.Effects = DragDropEffects.None;
                System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] DragEnter: 设置 None 效果");
            }
            e.Handled = true;
        }

        /// <summary>
        /// 拖放悬停事件
        /// </summary>
        private void DiagramControl_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            // 不设置 e.Handled，允许 Drop 事件触发
        }

        /// <summary>
        /// NativeDiagramControl 的 DragEnter 事件（作为备选方案）
        /// </summary>
        private void NativeDiagramControl_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] NativeDiagramControl_DragEnter 事件触发（备选方案）");

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
        /// NativeDiagramControl 的 PreviewDrop 事件（隧道事件，最先触发）
        /// </summary>
        private void NativeDiagramControl_PreviewDrop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] PreviewDrop 事件触发（隧道事件）");

            try
            {
                if (e.Data.GetData("ToolItem") is not ToolItem item)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] PreviewDrop: 数据不是 ToolItem 类型");
                    return;
                }

                // 去重检查：防止同一个拖放操作触发多次
                var currentDropId = $"{item.ToolId}_{DateTime.Now.Ticks}";
                var timeSinceLastDrop = (DateTime.Now - _lastDropTime).TotalMilliseconds;

                if (_lastDragDropId != null && timeSinceLastDrop < 100)
                {
                    System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 去重：忽略重复的 Drop 事件（距离上次 {timeSinceLastDrop:F0}ms）");
                    e.Handled = true;
                    return;
                }

                _lastDragDropId = currentDropId;
                _lastDropTime = DateTime.Now;

                // 获取放置位置（相对于 NativeDiagramControl）
                Point dropPosition = e.GetPosition(this);
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] PreviewDrop position: ({dropPosition.X:F0}, {dropPosition.Y:F0})");

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 警告: ToolItem 的 ToolId 为空");
                    return;
                }

                // 获取当前工作流标签页
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab is not WorkflowTabViewModel workflowTab)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 警告: 无法获取 WorkflowTabViewModel");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Creating node: ToolId={item.ToolId}, Name={item.Name}");

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点，自动分配序号
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Node position set to: ({newNode.Position.X:F0}, {newNode.Position.Y:F0})");

                // 添加新节点到工作流（这会触发 OnNodesCollectionChanged，自动创建原生节点）
                workflowTab.WorkflowNodes.Add(newNode);

                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Node added: Index={newNode.Index}, GlobalIndex={newNode.GlobalIndex}");

                // 标记为已处理，防止其他事件处理器再次处理
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ PreviewDrop 失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 堆栈: {ex.StackTrace}");
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// NativeDiagramControl 的 DragOver 事件（作为备选方案）
        /// </summary>
        private void NativeDiagramControl_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            // 不设置 e.Handled，允许 Drop 事件触发
        }

        /// <summary>
        /// NativeDiagramControl 的 Drop 事件（作为备选方案）
        /// </summary>
        private void NativeDiagramControl_Drop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] NativeDiagramControl_Drop 事件触发（备选方案）");

            try
            {
                if (e.Data.GetData("ToolItem") is not ToolItem item)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] NativeDiagramControl_Drop: 数据不是 ToolItem 类型");
                    return;
                }

                // 获取放置位置（相对于 NativeDiagramControl）
                Point dropPosition = e.GetPosition(this);
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] NativeDiagramControl_Drop position: ({dropPosition.X:F0}, {dropPosition.Y:F0})");

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 警告: ToolItem 的 ToolId 为空");
                    return;
                }

                // 获取当前工作流标签页
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab is not WorkflowTabViewModel workflowTab)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 警告: 无法获取 WorkflowTabViewModel");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Creating node: ToolId={item.ToolId}, Name={item.Name}");

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点，自动分配序号
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Node position set to: ({newNode.Position.X:F0}, {newNode.Position.Y:F0})");

                // 添加新节点到工作流
                workflowTab.WorkflowNodes.Add(newNode);

                // 创建原生节点（通过 DiagramAdapter）
                if (_adapter != null && _diagramViewModel != null)
                {
                    // 直接调用公开方法，不使用反射
                    var nativeNode = _adapter.CreateNativeNode(newNode, _diagramViewModel);
                    _diagramViewModel.Add(nativeNode);
                    System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Native node created and added");
                }

                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Node added: Index={newNode.Index}, GlobalIndex={newNode.GlobalIndex}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ NativeDiagramControl_Drop 失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 堆栈: {ex.StackTrace}");
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 拖放放下事件 - 创建新节点
        /// </summary>
        private void DiagramControl_Drop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] Drop 事件触发");

            try
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] GetDataPresent('ToolItem'): {e.Data.GetDataPresent("ToolItem")}");

                var data = e.Data.GetData("ToolItem");
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] GetData 结果: {data?.GetType().Name ?? "null"}");

                if (data is not ToolItem item)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] Drop: 数据不是 ToolItem 类型");
                    return;
                }

                // 获取放置位置（相对于 DiagramControl）
                Point dropPosition = e.GetPosition(_diagramControl);
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Drop position: ({dropPosition.X:F0}, {dropPosition.Y:F0})");

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 警告: ToolItem 的 ToolId 为空");
                    return;
                }

                // 获取当前工作流标签页
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab is not WorkflowTabViewModel workflowTab)
                {
                    System.Diagnostics.Debug.WriteLine("[NativeDiagramControl] 警告: 无法获取 WorkflowTabViewModel");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Creating node: ToolId={item.ToolId}, Name={item.Name}");

                // 清除其他节点的选中状态
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel 的 CreateNode 方法创建节点，自动分配序号
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Node position set to: ({newNode.Position.X:F0}, {newNode.Position.Y:F0})");

                // 添加新节点到工作流
                workflowTab.WorkflowNodes.Add(newNode);

                // 创建原生节点（通过 DiagramAdapter）
                if (_adapter != null && _diagramViewModel != null)
                {
                    // 直接调用公开方法，不使用反射
                    var nativeNode = _adapter.CreateNativeNode(newNode, _diagramViewModel);
                    _diagramViewModel.Add(nativeNode);
                    System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Native node created and added");
                }

                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] Node added: Index={newNode.Index}, GlobalIndex={newNode.GlobalIndex}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] ❌ Drop 失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] 堆栈: {ex.StackTrace}");
                // 不要 throw，避免程序崩溃
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置路径计算器类型（用于兼容性，NativeDiagram使用贝塞尔曲线）
        /// </summary>
        public void SetPathCalculator(string pathCalculatorType)
        {
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] NativeDiagram 使用贝塞尔曲线，忽略路径计算器设置: {pathCalculatorType}");
        }

        /// <summary>
        /// 获取 DiagramViewModel（公开访问，用于缩放控制）
        /// </summary>
        public DiagramViewModel? GetDiagramViewModel()
        {
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] GetDiagramViewModel 被调用，返回: {_diagramViewModel?.GetType().Name ?? "null"}");
            return _diagramViewModel;
        }

        /// <summary>
        /// 获取 DiagramControl（公开访问，用于调试）
        /// </summary>
        public DiagramControl? GetDiagramControl()
        {
            System.Diagnostics.Debug.WriteLine($"[NativeDiagramControl] GetDiagramControl 被调用，返回: {_diagramControl?.GetType().Name ?? "null"}");
            return _diagramControl;
        }
    }
}
