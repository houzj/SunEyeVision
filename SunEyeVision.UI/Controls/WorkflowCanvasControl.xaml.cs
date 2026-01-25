using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.Events;
using SunEyeVision.UI.Events;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services;
using SunEyeVision.PluginSystem;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// WorkflowCanvasControl.xaml 的交互逻辑
    /// </summary>
    public partial class WorkflowCanvasControl : UserControl
    {
        private IEventBus _eventBus;
        private UIEventPublisher _eventPublisher;
        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;
        private int _workflowCounter = 1;
        private SortedSet<int> _usedWorkflowNumbers = new SortedSet<int>();

        /// <summary>
        /// 工作流切换事件
        /// </summary>
        public event System.EventHandler<string>? WorkflowSwitched;

        // 依赖属性
        public static readonly DependencyProperty NodesProperty =
            DependencyProperty.Register("Nodes", typeof(ObservableCollection<WorkflowNode>), typeof(WorkflowCanvasControl));

        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.Register("Connections", typeof(ObservableCollection<WorkflowConnection>), typeof(WorkflowCanvasControl));

        public static readonly DependencyProperty WorkflowsProperty =
            DependencyProperty.Register("Workflows", typeof(ObservableCollection<WorkflowInfo>), typeof(WorkflowCanvasControl));

        public static readonly DependencyProperty CurrentWorkflowProperty =
            DependencyProperty.Register("CurrentWorkflow", typeof(WorkflowInfo), typeof(WorkflowCanvasControl),
                new PropertyMetadata(null, OnCurrentWorkflowChanged));

        private static void OnCurrentWorkflowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 属性改变时会自动通知绑定更新
        }

        public ObservableCollection<WorkflowNode> Nodes
        {
            get => (ObservableCollection<WorkflowNode>)GetValue(NodesProperty);
            set => SetValue(NodesProperty, value);
        }

        public ObservableCollection<WorkflowConnection> Connections
        {
            get => (ObservableCollection<WorkflowConnection>)GetValue(ConnectionsProperty);
            set => SetValue(ConnectionsProperty, value);
        }

        public ObservableCollection<WorkflowInfo> Workflows
        {
            get => (ObservableCollection<WorkflowInfo>)GetValue(WorkflowsProperty);
            set => SetValue(WorkflowsProperty, value);
        }

        public WorkflowInfo CurrentWorkflow
        {
            get => (WorkflowInfo)GetValue(CurrentWorkflowProperty);
            set => SetValue(CurrentWorkflowProperty, value);
        }

        public WorkflowCanvasControl()
        {
            InitializeComponent();
            Loaded += WorkflowCanvasControl_Loaded;
            Unloaded += WorkflowCanvasControl_Unloaded;

            // 初始化集合
            Workflows = new ObservableCollection<WorkflowInfo>();
            Nodes = new ObservableCollection<WorkflowNode>();
            Connections = new ObservableCollection<WorkflowConnection>();

            // 监听节点集合变化
            Nodes.CollectionChanged += (s, e) =>
            {
                UpdateConnectionPositions();
            };

            // 创建默认工作流
            CreateDefaultWorkflow();
        }

        private void WorkflowCanvasControl_Loaded(object sender, RoutedEventArgs e)
        {
            _eventPublisher = ServiceLocator.Instance.GetService<UIEventPublisher>();
            if (_eventPublisher != null)
            {
                _eventBus = ServiceLocator.Instance.GetService<IEventBus>();
            }

            // 根据已有工作流初始化计数器
            InitializeWorkflowCounter();
        }

        /// <summary>
        /// 初始化工作流计数器
        /// </summary>
        private void InitializeWorkflowCounter()
        {
            if (Workflows.Count == 0)
            {
                _workflowCounter = 0;
                _usedWorkflowNumbers.Clear();
                return;
            }

            // 清空已使用的编号集合
            _usedWorkflowNumbers.Clear();

            // 收集所有已使用的工作流编号
            int maxNumber = 0;
            foreach (var workflow in Workflows)
            {
                // 如果是默认工作流，重命名为"工作流1"
                if (workflow.Name == "默认工作流")
                {
                    workflow.Name = "工作流1";
                    _usedWorkflowNumbers.Add(1);
                    maxNumber = Math.Max(maxNumber, 1);
                }
                else
                {
                    // 从工作流名称中提取数字
                    var match = System.Text.RegularExpressions.Regex.Match(workflow.Name, @"工作流(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                    {
                        _usedWorkflowNumbers.Add(number);
                        maxNumber = Math.Max(maxNumber, number);
                    }
                }
            }
            _workflowCounter = maxNumber;
        }

        /// <summary>
        /// 获取下一个可用的工作流编号
        /// </summary>
        private int GetNextWorkflowNumber()
        {
            if (_usedWorkflowNumbers.Count == 0)
            {
                return 1;
            }

            // 查找第一个未被使用的编号
            int expectedNumber = 1;
            foreach (var number in _usedWorkflowNumbers)
            {
                if (number != expectedNumber)
                {
                    // 找到了未被使用的编号
                    return expectedNumber;
                }
                expectedNumber++;
            }

            // 如果所有编号都被使用，返回下一个递增的编号
            return expectedNumber;
        }

        private void WorkflowCanvasControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 清理
        }

        #region 工作流管理

        /// <summary>
        /// 创建默认工作流
        /// </summary>
        private void CreateDefaultWorkflow()
        {
            // 如果已经有工作流，不需要创建默认工作流（可能从持久化加载）
            if (Workflows.Count > 0)
                return;

            var defaultWorkflow = new WorkflowInfo
            {
                Name = "工作流1",
                RunMode = RunMode.Single
            };
            Workflows.Add(defaultWorkflow);
            CurrentWorkflow = defaultWorkflow;
            _workflowCounter = 1; // 初始化计数器
            _usedWorkflowNumbers.Add(1); // 标记编号1已使用
        }

        /// <summary>
        /// 添加新工作流
        /// </summary>
        public void AddWorkflow()
        {
            // 获取下一个可用的工作流编号
            int nextNumber = GetNextWorkflowNumber();
            var newWorkflow = new WorkflowInfo
            {
                Name = $"工作流{nextNumber}",
                RunMode = RunMode.Single
            };
            Workflows.Add(newWorkflow);
            _usedWorkflowNumbers.Add(nextNumber);

            // 更新计数器
            _workflowCounter = Math.Max(_workflowCounter, nextNumber);

            SwitchToWorkflow(newWorkflow);
        }

        /// <summary>
        /// 删除工作流
        /// </summary>
        public void DeleteWorkflow(WorkflowInfo workflow)
        {
            if (workflow == null)
                return;

            if (Workflows.Count <= 1)
            {
                MessageBox.Show("至少需要保留一个工作流", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (workflow.IsRunning)
            {
                MessageBox.Show("请先停止该工作流", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除工作流 '{workflow.Name}' 吗?", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var index = Workflows.IndexOf(workflow);
                Workflows.Remove(workflow);

                // 从已使用的编号集合中移除
                var match = System.Text.RegularExpressions.Regex.Match(workflow.Name, @"工作流(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    _usedWorkflowNumbers.Remove(number);
                }

                // 切换到其他工作流
                if (CurrentWorkflow == workflow)
                {
                    if (Workflows.Count > 0)
                    {
                        var newIndex = Math.Min(index, Workflows.Count - 1);
                        SwitchToWorkflow(Workflows[newIndex]);
                    }
                    else
                    {
                        CurrentWorkflow = null;
                        Nodes.Clear();
                        Connections.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// 切换到指定工作流
        /// </summary>
        private void SwitchToWorkflow(WorkflowInfo workflow)
        {
            if (workflow == null)
                return;

            // 保存当前工作流数据（如果需要）
            if (CurrentWorkflow != null && CurrentWorkflow != workflow)
            {
                CurrentWorkflow.Nodes.Clear();
                foreach (var node in Nodes)
                {
                    CurrentWorkflow.Nodes.Add(node);
                }

                CurrentWorkflow.Connections.Clear();
                foreach (var conn in Connections)
                {
                    CurrentWorkflow.Connections.Add(conn);
                }
            }

            // 切换到新工作流
            CurrentWorkflow = workflow;

            // 加载新工作流数据
            Nodes.Clear();
            foreach (var node in workflow.Nodes)
            {
                Nodes.Add(node);
            }

            Connections.Clear();
            foreach (var conn in workflow.Connections)
            {
                Connections.Add(conn);
            }

            // 触发工作流切换事件
            WorkflowSwitched?.Invoke(this, workflow.Name);
        }

        /// <summary>
        /// 清空当前工作流
        /// </summary>
        public void ClearCurrentWorkflow()
        {
            if (CurrentWorkflow == null)
                return;

            if (CurrentWorkflow.IsRunning)
            {
                MessageBox.Show("请先停止该工作流", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要清空工作流 '{CurrentWorkflow.Name}' 的所有节点和连接吗?", "确认清空", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Nodes.Clear();
                Connections.Clear();
                CurrentWorkflow.Nodes.Clear();
                CurrentWorkflow.Connections.Clear();
            }
        }

        #endregion

        #region 工作流运行控制

        /// <summary>
        /// 切换运行模式
        /// </summary>
        public void ToggleRunMode(WorkflowInfo workflow)
        {
            if (workflow == null)
                return;

            if (workflow.IsRunning)
            {
                MessageBox.Show("请先停止该工作流", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            workflow.RunMode = workflow.RunMode == RunMode.Single ? RunMode.Continuous : RunMode.Single;
        }

        /// <summary>
        /// 单次运行工作流
        /// </summary>
        public void RunSingle(WorkflowInfo workflow)
        {
            if (workflow == null)
                return;

            if (workflow.IsRunning)
                return;

            if (workflow.RunMode != RunMode.Single)
            {
                workflow.RunMode = RunMode.Single;
            }

            workflow.IsRunning = true;
            SwitchToWorkflow(workflow);

            // 发布运行事件
            _eventBus?.Publish(new UIEvents.WorkflowExecutionEvent("WorkflowCanvasControl", workflow.Id, workflow.Name, UIEvents.WorkflowExecutionType.Start));

            // 模拟单次运行（实际应该调用工作流执行引擎）
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                workflow.IsRunning = false;
                _eventBus?.Publish(new UIEvents.WorkflowExecutionEvent("WorkflowCanvasControl", workflow.Id, workflow.Name, UIEvents.WorkflowExecutionType.Stop));
            };
            timer.Start();
        }

        /// <summary>
        /// 开始连续运行工作流
        /// </summary>
        public void StartContinuous(WorkflowInfo workflow)
        {
            if (workflow == null)
                return;

            if (workflow.IsRunning)
                return;

            if (workflow.RunMode != RunMode.Continuous)
            {
                workflow.RunMode = RunMode.Continuous;
            }

            workflow.IsRunning = true;
            SwitchToWorkflow(workflow);

            // 发布运行事件
            _eventBus?.Publish(new UIEvents.WorkflowExecutionEvent("WorkflowCanvasControl", workflow.Id, workflow.Name, UIEvents.WorkflowExecutionType.Start));
        }

        /// <summary>
        /// 停止工作流运行
        /// </summary>
        public void StopWorkflow(WorkflowInfo workflow)
        {
            if (workflow == null)
                return;

            workflow.IsRunning = false;

            // 发布停止事件
            _eventBus?.Publish(new UIEvents.WorkflowExecutionEvent("WorkflowCanvasControl", workflow.Id, workflow.Name, UIEvents.WorkflowExecutionType.Stop));
        }

        #endregion

        #region XAML 事件处理

        private void AddWorkflow_Click(object sender, RoutedEventArgs e)
        {
            AddWorkflow();
        }

        private void DeleteWorkflow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowInfo workflow)
            {
                DeleteWorkflow(workflow);
            }
        }

        private void WorkflowTab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowInfo workflow)
            {
                SwitchToWorkflow(workflow);
            }
        }

        private void ToggleRunMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowInfo workflow)
            {
                ToggleRunMode(workflow);
            }
        }

        private void RunSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowInfo workflow)
            {
                RunSingle(workflow);
            }
        }

        private void ToggleContinuous_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WorkflowInfo workflow)
            {
                if (workflow.IsRunning)
                {
                    StopWorkflow(workflow);
                }
                else
                {
                    StartContinuous(workflow);
                }
            }
        }

        private void ClearCurrentWorkflow_Click(object sender, RoutedEventArgs e)
        {
            ClearCurrentWorkflow();
        }

        #endregion

        #region 拖放事件

        private void WorkflowCanvas_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void WorkflowCanvas_DragOver(object sender, DragEventArgs e)
        {
            // 每次移动都会触发,不要频繁弹出MessageBox
            if (e.Data.GetDataPresent("ToolItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void WorkflowCanvas_DragLeave(object sender, DragEventArgs e)
        {
            // 可选：添加离开画布时的视觉效果
        }

        private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent("ToolItem") && e.Data.GetData("ToolItem") is Models.ToolItem tool)
                {
                    var position = e.GetPosition(WorkflowCanvas);

                    // 创建新节点
                    var node = new WorkflowNode(
                        Guid.NewGuid().ToString(),
                        tool.Name,
                        tool.ToolId
                    );

                    var x = Math.Max(0, position.X - 70);
                    var y = Math.Max(0, position.Y - 45);
                    node.Position = new System.Windows.Point(x, y);

                    // 添加到Nodes集合
                    Nodes.Add(node);

                    // 发布节点添加事件
                    RaiseNodeAddedEvent(node, tool);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加节点时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _eventBus?.Publish(new ErrorEvent("WorkflowCanvas", $"添加节点时出错: {ex.Message}", ErrorSeverity.Error)
                {
                    StackTrace = ex.StackTrace
                });
            }
        }

        #endregion

        #region 节点事件

        private void Node_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not WorkflowNode node)
                return;

            // 双击事件
            if (e.ClickCount == 2)
            {
                foreach (var n in Nodes)
                {
                    n.IsSelected = (n == node);
                }

                RaiseNodeDoubleClickedEvent(node);
                e.Handled = true;
                return;
            }

            // 单击事件：拖拽准备
            _isDragging = true;
            _draggedNode = node;
            _startDragPosition = e.GetPosition(WorkflowCanvas);

            foreach (var n in Nodes)
            {
                n.IsSelected = (n == node);
            }

            RaiseNodeSelectedEvent(node);
            border.CaptureMouse();
        }

        private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedNode = null!;
                (sender as Border)?.ReleaseMouseCapture();
            }
        }

        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedNode != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(WorkflowCanvas);
                var offset = currentPosition - _startDragPosition;

                var newPosition = new System.Windows.Point(
                    _draggedNode.Position.X + offset.X,
                    _draggedNode.Position.Y + offset.Y
                );

                _draggedNode.Position = newPosition;
                _startDragPosition = currentPosition;

                // 更新连接线位置
                UpdateConnectionPositions();

                RaiseNodeMovedEvent(_draggedNode, newPosition);
            }
        }

        private void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode targetNode)
            {
                RaiseNodeClickedEvent(targetNode);
            }
        }

        /// <summary>
        /// 更新所有连接线的位置（基于节点位置）
        /// </summary>
        private void UpdateConnectionPositions()
        {
            foreach (var connection in Connections)
            {
                var sourceNode = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                var targetNode = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                if (sourceNode != null && targetNode != null)
                {
                    // 更新连接线位置：起点为源节点右侧连接点，终点为目标节点左侧连接点
                    connection.SourcePosition = sourceNode.RightPortPosition;
                    connection.TargetPosition = targetNode.LeftPortPosition;
                }
            }
        }

        #endregion

        #region 事件定义

        public event System.EventHandler<WorkflowNode> NodeAdded;
        public event System.EventHandler<WorkflowNode> NodeSelected;
        public event System.EventHandler<WorkflowNode> NodeDoubleClicked;
        public event System.EventHandler<NodeMovedEventArgs> NodeMoved;
        public event System.EventHandler<WorkflowNode> NodeClicked;

        private void RaiseNodeAddedEvent(WorkflowNode node, Models.ToolItem tool)
        {
            _eventPublisher?.PublishNodeAdded(node.Id, node.Name, tool.ToolId, node.Position.X, node.Position.Y);
            NodeAdded?.Invoke(this, node);
        }

        private void RaiseNodeSelectedEvent(WorkflowNode node)
        {
            _eventPublisher?.PublishNodeSelected(node.Id, node.Name);
            NodeSelected?.Invoke(this, node);
        }

        private void RaiseNodeDoubleClickedEvent(WorkflowNode node)
        {
            _eventPublisher?.PublishDebugWindowOpened(node.Id, node.Name);
            NodeDoubleClicked?.Invoke(this, node);
        }

        private void RaiseNodeMovedEvent(WorkflowNode node, System.Windows.Point newPosition)
        {
            _eventPublisher?.PublishNodeMoved(node.Id, newPosition.X, newPosition.Y);
            NodeMoved?.Invoke(this, new NodeMovedEventArgs(node, newPosition));
        }

        private void RaiseNodeClickedEvent(WorkflowNode node)
        {
            _eventPublisher?.PublishNodeSelected(node.Id, node.Name);
            NodeClicked?.Invoke(this, node);
        }

        #endregion
    }

    /// <summary>
    /// 节点移动事件参数
    /// </summary>
    public class NodeMovedEventArgs : EventArgs
    {
        public WorkflowNode Node { get; set; }
        public System.Windows.Point NewPosition { get; set; }

        public NodeMovedEventArgs(WorkflowNode node, System.Windows.Point newPosition)
        {
            Node = node;
            NewPosition = newPosition;
        }
    }
}
