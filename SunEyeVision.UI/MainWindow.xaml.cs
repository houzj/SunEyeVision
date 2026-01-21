using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI
{
    /// <summary>
    /// MainWindow - 太阳眼视觉风格的主界面窗口
    /// 实现完整的机器视觉平台主界面，包含工作流画布、工具箱、属性面板等
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private bool _isDragging;
        private WorkflowNode? _draggedNode;
        private System.Windows.Point _startDragPosition;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            RegisterHotkeys();
        }

        /// <summary>
        /// 注册快捷键
        /// </summary>
        private void RegisterHotkeys()
        {
            // 文件操作快捷键
            InputBindings.Add(new KeyBinding(_viewModel.NewWorkflowCommand, Key.N, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(_viewModel.OpenWorkflowCommand, Key.O, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(_viewModel.SaveWorkflowCommand, Key.S, ModifierKeys.Control));

            // 运行控制快捷键
            InputBindings.Add(new KeyBinding(_viewModel.RunWorkflowCommand, Key.F5, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(_viewModel.StopWorkflowCommand, Key.F5, ModifierKeys.Shift));

            // 帮助快捷键
            InputBindings.Add(new KeyBinding(_viewModel.ShowHelpCommand, Key.F1, ModifierKeys.None));
            InputBindings.Add(new KeyBinding(new PauseCommandWrapper(_viewModel.PauseCommand), Key.Pause, ModifierKeys.None));

            // 编辑快捷键
            InputBindings.Add(new KeyBinding(new UndoCommandWrapper(_viewModel.UndoCommand), Key.Z, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new RedoCommandWrapper(_viewModel.RedoCommand), Key.Y, ModifierKeys.Control));
        }

        #region 窗口事件

        protected override void OnClosed(EventArgs e)
        {
            // TODO: 清理资源
            _viewModel?.StopWorkflowCommand.Execute(null);
            base.OnClosed(e);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO: 加载插件
            // TODO: 加载工作流
        }

        #endregion

        #region 工具箱拖拽

        private void ToolItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Models.ToolItem tool)
            {
                var dragData = new DataObject("ToolItem", tool);
                DragDrop.DoDragDrop(border, dragData, DragDropEffects.Copy);
            }
        }

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
            // 可选:添加离开画布时的视觉效果
        }

        private void WorkflowCanvas_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetData("ToolItem") is Models.ToolItem tool)
                {
                    var position = e.GetPosition(WorkflowCanvas);

                    // 创建新节点
                    var node = new WorkflowNode(
                        Guid.NewGuid().ToString(),
                        tool.Name,
                        tool.AlgorithmType
                    );

                    // 设置拖放位置(居中放置,节点大小140x90)
                    var x = Math.Max(0, position.X - 70);
                    var y = Math.Max(0, position.Y - 45);
                    node.Position = new System.Windows.Point(x, y);

                    // 设置默认参数
                    SetDefaultParametersForNode(node, tool);

                    // 在UI线程中添加节点
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _viewModel.WorkflowNodes.Add(node);
                        _viewModel.StatusText = $"添加节点: {tool.Name}";
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"添加节点时出错: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SetDefaultParametersForNode(WorkflowNode node, Models.ToolItem tool)
        {
            switch (tool.AlgorithmType)
            {
                case "GaussianBlur":
                    node.Parameters.Add("KernelSize", 5);
                    node.Parameters.Add("Sigma", 1.0);
                    break;
                case "Threshold":
                    node.Parameters.Add("Threshold", 128);
                    node.Parameters.Add("Invert", false);
                    break;
                case "GrayScale":
                    node.Parameters.Add("Method", "Average");
                    break;
                case "EdgeDetection":
                    node.Parameters.Add("Method", "Canny");
                    node.Parameters.Add("Threshold1", 50);
                    node.Parameters.Add("Threshold2", 150);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region 节点拖拽

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                _isDragging = true;
                _draggedNode = node;
                _startDragPosition = e.GetPosition(WorkflowCanvas);

                // 更新选中状态
                foreach (var n in _viewModel.WorkflowNodes)
                {
                    n.IsSelected = (n == node);
                }
                _viewModel.SelectedNode = node;

                border.CaptureMouse();
            }
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

                _draggedNode.Position = new System.Windows.Point(
                    _draggedNode.Position.X + offset.X,
                    _draggedNode.Position.Y + offset.Y
                );

                _startDragPosition = currentPosition;
            }
        }

        #endregion

        #region 节点连接

        private void Node_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 双击节点进入连接模式
            if (sender is Border border && border.Tag is WorkflowNode node)
            {
                // 确保选中该节点
                foreach (var n in _viewModel.WorkflowNodes)
                {
                    n.IsSelected = (n == node);
                }
                _viewModel.SelectedNode = node;

                // 启动连接模式
                _viewModel.WorkflowViewModel.SelectedNode = node;
                _viewModel.WorkflowViewModel.ExecuteConnectNodes();
                _viewModel.StatusText = $"已选择源节点: {node.Name}。请点击目标节点以连接。";
            }
        }

        private void Node_ClickForConnection(object sender, RoutedEventArgs e)
        {
            // 连接模式下点击节点作为目标
            if (sender is Border border && border.Tag is WorkflowNode targetNode)
            {
                if (_viewModel.WorkflowViewModel.IsInConnectionMode)
                {
                    var success = _viewModel.WorkflowViewModel.TryConnectNode(targetNode);

                    if (success)
                    {
                        var sourceNode = _viewModel.WorkflowViewModel.ConnectionSourceNode;
                        _viewModel.StatusText = $"成功连接: {sourceNode?.Name} -> {targetNode.Name}";

                        // 同步连接到MainWindowViewModel
                        if (_viewModel.WorkflowViewModel.Connections.LastOrDefault() is WorkflowConnection connection)
                        {
                            _viewModel.WorkflowConnections.Add(connection);
                        }
                    }
                    else
                    {
                        _viewModel.StatusText = $"连接失败：目标节点无效或连接已存在";
                    }
                }
                else
                {
                    // 非连接模式下，只是选中节点
                    foreach (var n in _viewModel.WorkflowNodes)
                    {
                        n.IsSelected = (n == targetNode);
                    }
                    _viewModel.SelectedNode = targetNode;
                    _viewModel.WorkflowViewModel.SelectedNode = targetNode;
                }
            }
        }

        #endregion

        #region 命令包装器类

        private class PauseCommandWrapper : ICommand
        {
            private readonly ICommand? _command;

            public PauseCommandWrapper(ICommand? command)
            {
                _command = command;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _command?.CanExecute(parameter) ?? false;
            }

            public void Execute(object? parameter)
            {
                _command?.Execute(parameter);
            }
        }

        private class UndoCommandWrapper : ICommand
        {
            private readonly ICommand? _command;

            public UndoCommandWrapper(ICommand? command)
            {
                _command = command;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _command?.CanExecute(parameter) ?? false;
            }

            public void Execute(object? parameter)
            {
                _command?.Execute(parameter);
            }
        }

        private class RedoCommandWrapper : ICommand
        {
            private readonly ICommand? _command;

            public RedoCommandWrapper(ICommand? command)
            {
                _command = command;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter)
            {
                return _command?.CanExecute(parameter) ?? false;
            }

            public void Execute(object? parameter)
            {
                _command?.Execute(parameter);
            }
        }

        #endregion
    }
}
