using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.UI.Views.Windows;
using AIStudio.Wpf.DiagramDesigner.ViewModels;
using AIStudio.Wpf.DiagramDesigner;

namespace SunEyeVision.UI.Views.Controls.Canvas
{
    /// <summary>
    /// NativeDiagramControl - 使用AIStudio.Wpf.DiagramDesigner原生�?
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
        /// 初始化控�?
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {

                return;
            }

            try
            {


                // 初始化适配�?
                _adapter = new DiagramAdapter();

                // 加载原生图表控件
                LoadNativeDiagram();

                // 配置原生图表功能
                ConfigureDiagramFeatures();

                // 同步数据
                SyncData();

                _isInitialized = true;


            }
            catch (Exception ex)
            {



                // 显示错误信息
                MessageBox.Show($"初始化失�? {ex.Message}\n请确保已安装 AIStudio.Wpf.DiagramDesigner �?,
                    "NativeDiagramControl", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载原生图表控件
        /// 创建 DiagramViewModel �?DiagramControl
        /// </summary>
        private void LoadNativeDiagram()
        {
            try
            {
                // 创建 DiagramViewModel 实例
                _diagramViewModel = new DiagramViewModel();


                // 创建 DiagramControl 实例
                _diagramControl = new DiagramControl();


                // === 新增：设置画布尺寸为 10000x10000 ===
                _diagramControl.Width = 10000;
                _diagramControl.Height = 10000;
                
            

                // ==========================================

                // 启用拖放
                _diagramControl.AllowDrop = true;
                _diagramControl.DragEnter += DiagramControl_DragEnter;
                _diagramControl.DragOver += DiagramControl_DragOver;
                _diagramControl.Drop += DiagramControl_Drop;

                // 设置 DiagramControl �?DataContext �?DiagramViewModel
                _diagramControl.DataContext = _diagramViewModel;

                // 设置到容�?
                DiagramContainer.Content = _diagramControl;

            }
            catch (Exception ex)
            {

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


            }
            catch (Exception ex)
            {

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


                // 同步节点（传�?DiagramViewModel�?
                _adapter.SyncNodes(_nodes, _diagramViewModel);

                // 同步连接（传�?DiagramViewModel�?
                _adapter.SyncConnections(_connections, _diagramViewModel);

                // 更新空状�?
                UpdateEmptyState();


            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 更新空状态显�?
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
            // 获取 MainWindowViewModel
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                _viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (_viewModel != null)
                {
                    if (_viewModel.WorkflowTabViewModel == null)
                    {
                        return;
                    }

                    // 订阅节点集合变化
                    if (_viewModel.WorkflowTabViewModel.SelectedTab is WorkflowTabViewModel workflowTab)
                    {
                        SubscribeToWorkflowChanges(workflowTab);
                    }
                }
            }
        }

        /// <summary>
        /// 订阅工作流变�?
        /// </summary>
        private void SubscribeToWorkflowChanges(WorkflowTabViewModel workflowTab)
        {
            _nodes = workflowTab.WorkflowNodes;
            _connections = workflowTab.WorkflowConnections;

            _nodes.CollectionChanged += OnNodesCollectionChanged;
            _connections.CollectionChanged += OnConnectionsCollectionChanged;

            

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
            

            if (_adapter != null && _diagramViewModel != null)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (WorkflowNode node in e.NewItems!)
                        {


                            // 使用公开方法创建节点（不使用反射�?
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
            

            if (_adapter != null && _diagramViewModel != null)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (WorkflowConnection connection in e.NewItems!)
                        {
                            try
                            {
                                // 重新创建连接（传�?DiagramViewModel�?
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
            // 不设�?e.Handled，允�?Drop 事件触发
        }

        /// <summary>
        /// NativeDiagramControl �?DragEnter 事件（作为备选方案）
        /// </summary>
        private void NativeDiagramControl_DragEnter(object sender, DragEventArgs e)
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
        /// NativeDiagramControl �?PreviewDrop 事件（隧道事件，最先触发）
        /// </summary>
        private void NativeDiagramControl_PreviewDrop(object sender, DragEventArgs e)
        {
            

            try
            {
                if (e.Data.GetData("ToolItem") is not ToolItem item)
                {

                    return;
                }

                // 去重检查：防止同一个拖放操作触发多�?
                var currentDropId = $"{item.ToolId}_{DateTime.Now.Ticks}";
                var timeSinceLastDrop = (DateTime.Now - _lastDropTime).TotalMilliseconds;

                if (_lastDragDropId != null && timeSinceLastDrop < 100)
                {

                    e.Handled = true;
                    return;
                }

                _lastDragDropId = currentDropId;
                _lastDropTime = DateTime.Now;

                // 获取放置位置（相对于 NativeDiagramControl�?
                Point dropPosition = e.GetPosition(this);
                

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {

                    return;
                }

                // 获取当前工作流标签页
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab is not WorkflowTabViewModel workflowTab)
                {

                    return;
                }

                

                // 清除其他节点的选中状�?
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel �?CreateNode 方法创建节点，自动分配序�?
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                

                // 添加新节点到工作流（这会触发 OnNodesCollectionChanged，自动创建原生节点）
                workflowTab.WorkflowNodes.Add(newNode);

                

                // 标记为已处理，防止其他事件处理器再次处理
                e.Handled = true;
            }
            catch (Exception ex)
            {
                
                
                // 不要 throw，避免程序崩�?
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// NativeDiagramControl �?DragOver 事件（作为备选方案）
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
            // 不设�?e.Handled，允�?Drop 事件触发
        }

        /// <summary>
        /// NativeDiagramControl �?Drop 事件（作为备选方案）
        /// </summary>
        private void NativeDiagramControl_Drop(object sender, DragEventArgs e)
        {
            

            try
            {
                if (e.Data.GetData("ToolItem") is not ToolItem item)
                {

                    return;
                }

                // 获取放置位置（相对于 NativeDiagramControl�?
                Point dropPosition = e.GetPosition(this);
                

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {

                    return;
                }

                // 获取当前工作流标签页
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab is not WorkflowTabViewModel workflowTab)
                {

                    return;
                }

                

                // 清除其他节点的选中状�?
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel �?CreateNode 方法创建节点，自动分配序�?
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                

                // 添加新节点到工作�?
                workflowTab.WorkflowNodes.Add(newNode);

                // 创建原生节点（通过 DiagramAdapter�?
                if (_adapter != null && _diagramViewModel != null)
                {
                    // 直接调用公开方法，不使用反射
                    var nativeNode = _adapter.CreateNativeNode(newNode, _diagramViewModel);
                    _diagramViewModel.Add(nativeNode);
                    
                }

                
            }
            catch (Exception ex)
            {
                
                
                // 不要 throw，避免程序崩�?
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 拖放放下事件 - 创建新节�?
        /// </summary>
        private void DiagramControl_Drop(object sender, DragEventArgs e)
        {
            

            try
            {


                var data = e.Data.GetData("ToolItem");


                if (data is not ToolItem item)
                {

                    return;
                }

                // 获取放置位置（相对于 DiagramControl�?
                Point dropPosition = e.GetPosition(_diagramControl);
                

                // 验证数据
                if (string.IsNullOrEmpty(item.ToolId))
                {

                    return;
                }

                // 获取当前工作流标签页
                if (_viewModel?.WorkflowTabViewModel?.SelectedTab is not WorkflowTabViewModel workflowTab)
                {

                    return;
                }

                

                // 清除其他节点的选中状�?
                foreach (var node in workflowTab.WorkflowNodes)
                {
                    node.IsSelected = false;
                }

                // 使用 ViewModel �?CreateNode 方法创建节点，自动分配序�?
                var newNode = workflowTab.CreateNode(item.ToolId, item.Name);
                newNode.Position = dropPosition;
                newNode.IsSelected = true;
                

                // 添加新节点到工作�?
                workflowTab.WorkflowNodes.Add(newNode);

                // 创建原生节点（通过 DiagramAdapter�?
                if (_adapter != null && _diagramViewModel != null)
                {
                    // 直接调用公开方法，不使用反射
                    var nativeNode = _adapter.CreateNativeNode(newNode, _diagramViewModel);
                    _diagramViewModel.Add(nativeNode);
                    
                }

                
            }
            catch (Exception ex)
            {
                
                
                // 不要 throw，避免程序崩�?
                MessageBox.Show($"拖放节点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 设置路径计算器类型（用于兼容性，NativeDiagram使用贝塞尔曲线）
        /// </summary>
        public void SetPathCalculator(string pathCalculatorType)
        {

        }

        /// <summary>
        /// 获取 DiagramViewModel（公开访问，用于缩放控制）
        /// </summary>
        public DiagramViewModel? GetDiagramViewModel()
        {

            return _diagramViewModel;
        }

        /// <summary>
        /// 获取 DiagramControl（公开访问，用于调试）
        /// </summary>
        public DiagramControl? GetDiagramControl()
        {

            return _diagramControl;
        }
    }
}
