using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AppCommands = SunEyeVision.UI.Commands;
using SunEyeVision.UI.Models;
using SunEyeVision.PluginSystem;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private string _title = "太阳眼视觉";
        private bool _isRunning = false;
        private string _status = "就绪";
        private string _selectedWorkflowName = "默认工作流";
        private string _currentCanvasTypeText = "原生 Diagram (贝塞尔曲线)";

        // 图像显示相关
        private BitmapSource? _displayImage;
        private double _imageScale = 1.0;

        // 属性面板相关
        private ObservableCollection<Models.PropertyGroup> _propertyGroups = new ObservableCollection<Models.PropertyGroup>();
        private string _logText = "[系统] 等待操作...\n";

        // 面板折叠状态
        private bool _isToolboxCollapsed = false;
        private bool _isImageDisplayCollapsed = false;
        private bool _isPropertyPanelCollapsed = false;
        private double _toolboxWidth = 260;
        private double _rightPanelWidth = 500;
        private double _imageDisplayHeight = 500;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    Status = _isRunning ? "运行中" : "已停止";
                }
            }
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 当前画布类型显示文本
        /// </summary>
        public string CurrentCanvasTypeText
        {
            get => _currentCanvasTypeText;
            set => SetProperty(ref _currentCanvasTypeText, value);
        }

        public string SelectedWorkflowName
        {
            get => _selectedWorkflowName;
            set => SetProperty(ref _selectedWorkflowName, value);
        }

        public ObservableCollection<string> Workflows { get; }

        public ObservableCollection<Models.ToolItem> Tools { get; }
        public ToolboxViewModel Toolbox { get; }

        // 注意：删除了全局的 WorkflowNodes 和 WorkflowConnections 属性
        // 所有节点和连接都应该通过 WorkflowTabViewModel.SelectedTab 访问
        // 这样确保每个工作流 Tab 都是独立的

        public Models.WorkflowNode? SelectedNode { get; set; }
        public Models.WorkflowConnection? SelectedConnection { get; set; }
        public WorkflowViewModel WorkflowViewModel { get; set; }
        
        // 多流程管理
        public WorkflowTabControlViewModel WorkflowTabViewModel { get; }

        /// <summary>
        /// 当前选中画布的命令管理器（用于撤销/重做功能）
        /// 每个画布都有独立的撤销/重做栈
        /// </summary>
        public AppCommands.CommandManager? CurrentCommandManager
        {
            get => WorkflowTabViewModel.SelectedTab?.CommandManager;
        }

        // 用于跟踪当前订阅的命令管理器，避免重复订阅
        private AppCommands.CommandManager? _subscribedCommandManager;

        public string StatusText
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string CameraStatus => "已连接 (2台)";

        // 图像显示属性
        public BitmapSource? DisplayImage
        {
            get => _displayImage;
            set => SetProperty(ref _displayImage, value);
        }

        public double ImageScale
        {
            get => _imageScale;
            set => SetProperty(ref _imageScale, value);
        }

        // 属性面板属性
        public ObservableCollection<Models.PropertyGroup> PropertyGroups
        {
            get => _propertyGroups;
            set => SetProperty(ref _propertyGroups, value);
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        // 面板折叠状态属性
        public bool IsToolboxCollapsed
        {
            get => _isToolboxCollapsed;
            set => SetProperty(ref _isToolboxCollapsed, value);
        }

        public bool IsImageDisplayCollapsed
        {
            get => _isImageDisplayCollapsed;
            set => SetProperty(ref _isImageDisplayCollapsed, value);
        }

        public bool IsPropertyPanelCollapsed
        {
            get => _isPropertyPanelCollapsed;
            set => SetProperty(ref _isPropertyPanelCollapsed, value);
        }

        public double ToolboxWidth
        {
            get => _toolboxWidth;
            set => SetProperty(ref _toolboxWidth, value);
        }

        public double RightPanelWidth
        {
            get => _rightPanelWidth;
            set => SetProperty(ref _rightPanelWidth, value);
        }

        public double ImageDisplayHeight
        {
            get => _imageDisplayHeight;
            set => SetProperty(ref _imageDisplayHeight, value);
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            // 新日志插入到最前面
            LogText = $"[{timestamp}] {message}\n" + LogText;

            // 限制日志条目数量，最多保留100条
            var lines = LogText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 100)
            {
                LogText = string.Join("\n", lines.Take(100)) + "\n";
            }
        }

        public ICommand NewWorkflowCommand { get; }
        public ICommand OpenWorkflowCommand { get; }
        public ICommand SaveWorkflowCommand { get; }
        public ICommand SaveAsWorkflowCommand { get; }
        public ICommand RunWorkflowCommand { get; }
        public ICommand StopWorkflowCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand ShowShortcutsCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand DeleteSelectedNodesCommand { get; }
        public ICommand OpenDebugWindowCommand { get; }
        public ICommand ToggleBoundingRectangleCommand { get; }
        public ICommand TogglePathPointsCommand { get; }

        public MainWindowViewModel()
        {
            Workflows = new ObservableCollection<string>
            {
                "默认工作流",
                "边缘检测",
                "目标检测",
                "质量检测"
            };

            Tools = new ObservableCollection<Models.ToolItem>();
            Toolbox = new ToolboxViewModel();
            // 删除了全局 WorkflowNodes 和 WorkflowConnections 的初始化

            WorkflowViewModel = new WorkflowViewModel();
            WorkflowTabViewModel = new WorkflowTabControlViewModel();

            // 初始化当前画布类型
            UpdateCurrentCanvasType();

            // 订阅选中画布变化事件，更新撤销/重做按钮状态
            WorkflowTabViewModel.SelectionChanged += OnSelectedTabChanged;

            // 订阅初始画布的命令管理器
            SubscribeToCurrentCommandManager();

            InitializeTools();
            // InitializeSampleNodes(); // 已禁用：程序启动时不加载测试节点和连线
            InitializePropertyGroups();

            NewWorkflowCommand = new RelayCommand(ExecuteNewWorkflow);
            OpenWorkflowCommand = new RelayCommand(ExecuteOpenWorkflow);
            SaveWorkflowCommand = new RelayCommand(ExecuteSaveWorkflow);
            SaveAsWorkflowCommand = new RelayCommand(ExecuteSaveAsWorkflow);
            RunWorkflowCommand = new RelayCommand(ExecuteRunWorkflow, () => !IsRunning);
            StopWorkflowCommand = new RelayCommand(ExecuteStopWorkflow, () => IsRunning);
            ShowSettingsCommand = new RelayCommand(ExecuteShowSettings);
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);
            ShowHelpCommand = new RelayCommand(ExecuteShowHelp);
            ShowShortcutsCommand = new RelayCommand(ExecuteShowShortcuts);
            PauseCommand = new RelayCommand(ExecutePause);
            UndoCommand = new RelayCommand(ExecuteUndo, CanExecuteUndo);
            RedoCommand = new RelayCommand(ExecuteRedo, CanExecuteRedo);
            DeleteSelectedNodesCommand = new RelayCommand(ExecuteDeleteSelectedNodes, CanDeleteSelectedNodes);
            OpenDebugWindowCommand = new RelayCommand<Models.WorkflowNode>(ExecuteOpenDebugWindow);
            ToggleBoundingRectangleCommand = new RelayCommand(ExecuteToggleBoundingRectangle);
            TogglePathPointsCommand = new RelayCommand(ExecuteTogglePathPoints);
        }

        /// <summary>
        /// 选中画布变化处理
        /// </summary>
        private void OnSelectedTabChanged(object? sender, EventArgs e)
        {
            // 订阅新画布的命令管理器
            SubscribeToCurrentCommandManager();

            // 更新撤销/重做按钮状态
            UpdateUndoRedoCommands();

            // 更新当前画布类型显示
            UpdateCurrentCanvasType();

            // 更新 SmartPathConverter 的节点和连接集合
            if (WorkflowTabViewModel?.SelectedTab != null)
            {
                Converters.SmartPathConverter.Nodes = WorkflowTabViewModel.SelectedTab.WorkflowNodes;
                Converters.SmartPathConverter.Connections = WorkflowTabViewModel.SelectedTab.WorkflowConnections;
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Tab 切换 - 更新 SmartPathConverter, Nodes count: {WorkflowTabViewModel.SelectedTab.WorkflowNodes?.Count ?? 0}");
            }
        }

        /// <summary>
        /// 更新当前画布类型显示
        /// </summary>
        public void UpdateCurrentCanvasType()
        {
            if (WorkflowTabViewModel?.SelectedTab != null)
            {
                var canvasType = WorkflowTabViewModel.SelectedTab.CanvasType;
            CurrentCanvasTypeText = canvasType switch
            {
                Controls.CanvasType.WorkflowCanvas => "工作流画布",
                Controls.CanvasType.NativeDiagram => "原生 Diagram (贝塞尔曲线)",
                _ => "未知画布"
            };
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] 当前画布类型: {CurrentCanvasTypeText}");
            }
            else
            {
                CurrentCanvasTypeText = "工作流画布";
            }
        }

        /// <summary>
        /// 订阅当前画布的命令管理器状态变化
        /// </summary>
        private void SubscribeToCurrentCommandManager()
        {
            // 取消订阅旧的命令管理器
            if (_subscribedCommandManager != null)
            {
                _subscribedCommandManager.CommandStateChanged -= OnCurrentCommandManagerStateChanged;
            }

            // 订阅新的命令管理器
            if (CurrentCommandManager != null)
            {
                CurrentCommandManager.CommandStateChanged += OnCurrentCommandManagerStateChanged;
                _subscribedCommandManager = CurrentCommandManager;
            }
            else
            {
                _subscribedCommandManager = null;
            }
        }

        /// <summary>
        /// 更新撤销/重做命令的CanExecute状态
        /// </summary>
        private void UpdateUndoRedoCommands()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var undoCmd = UndoCommand as RelayCommand;
                var redoCmd = RedoCommand as RelayCommand;
                undoCmd?.RaiseCanExecuteChanged();
                redoCmd?.RaiseCanExecuteChanged();

                // 更新状态栏显示
                StatusText = CurrentCommandManager?.LastCommandDescription ?? "就绪";
            });
        }

        /// <summary>
        /// 当前画布的命令管理器状态变化处理
        /// </summary>
        private void OnCurrentCommandManagerStateChanged(object? sender, EventArgs e)
        {
            UpdateUndoRedoCommands();
        }

        /// <summary>
        /// 判断是否可以撤销（基于当前选中画布）
        /// </summary>
        private bool CanExecuteUndo()
        {
            return CurrentCommandManager?.CanUndo ?? false;
        }

        /// <summary>
        /// 判断是否可以重做（基于当前选中画布）
        /// </summary>
        private bool CanExecuteRedo()
        {
            return CurrentCommandManager?.CanRedo ?? false;
        }

        private void ExecutePause()
        {
            // TODO: 实现暂停功能
        }

        private void ExecuteUndo()
        {
            if (CurrentCommandManager == null)
            {
                AddLog("⚠️ 没有选中的画布，无法撤销");
                return;
            }

            try
            {
                CurrentCommandManager.Undo();
                AddLog($"↩️ 撤销: {CurrentCommandManager.LastCommandDescription}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"撤销失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRedo()
        {
            if (CurrentCommandManager == null)
            {
                AddLog("⚠️ 没有选中的画布，无法重做");
                return;
            }

            try
            {
                CurrentCommandManager.Redo();
                AddLog($"↪️ 重做: {CurrentCommandManager.LastCommandDescription}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"重做失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void InitializeTools()
        {
            Tools.Add(new Models.ToolItem("图像采集", "ImageAcquisition", "📷", "从相机或图像文件获取图像数据"));
            Tools.Add(new Models.ToolItem("灰度化", "GrayScale", "🌑", "将彩色图像转换为灰度图像"));
            Tools.Add(new Models.ToolItem("高斯模糊", "GaussianBlur", "🔮", "应用高斯模糊滤镜减少噪声"));
            Tools.Add(new Models.ToolItem("二值化", "Threshold", "⬛", "将图像转换为二值图像"));
            Tools.Add(new Models.ToolItem("边缘检测", "EdgeDetection", "🔲", "检测图像中的边缘"));
            Tools.Add(new Models.ToolItem("形态学操作", "Morphology", "🔄", "腐蚀、膨胀等形态学操作"));
        }

        private void InitializePropertyGroups()
        {
            // 初始化日志
            AddLog("✅ [系统] 系统启动成功");
            AddLog("✅ [设备] 相机1 连接成功");
            AddLog("✅ [设备] 相机2 连接成功");
        }

        private void InitializeSampleNodes()
        {
            if (WorkflowTabViewModel.SelectedTab != null)
            {
                // 添加节点到当前选中的标签页
                WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(new Models.WorkflowNode("1", "图像采集_1", "image_capture")
                {
                    Position = new System.Windows.Point(100, 100),
                    IsSelected = false
                });

                WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(new Models.WorkflowNode("2", "高斯模糊", "gaussian_blur")
                {
                    Position = new System.Windows.Point(300, 100),
                    IsSelected = false
                });

                WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(new Models.WorkflowNode("3", "边缘检测", "edge_detection")
                {
                    Position = new System.Windows.Point(500, 100),
                    IsSelected = false
                });

                WorkflowTabViewModel.SelectedTab.WorkflowConnections.Add(new Models.WorkflowConnection("conn_1", "1", "2")
                {
                    SourcePosition = new System.Windows.Point(240, 145),
                    TargetPosition = new System.Windows.Point(300, 145)
                });

                WorkflowTabViewModel.SelectedTab.WorkflowConnections.Add(new Models.WorkflowConnection("conn_2", "2", "3")
                {
                    SourcePosition = new System.Windows.Point(440, 145),
                    TargetPosition = new System.Windows.Point(500, 145)
                });
            }
        }

        private void ExecuteNewWorkflow()
        {
            // TODO: 创建新工作流
        }

        private void ExecuteOpenWorkflow()
        {
            // TODO: 打开工作流文件
        }

        private void ExecuteSaveWorkflow()
        {
            // TODO: 保存工作流到文件
        }

        private void ExecuteSaveAsWorkflow()
        {
            // TODO: 另存为工作流文件
        }

        private void ExecuteRunWorkflow()
        {
            IsRunning = true;
            // TODO: 执行工作流
        }

        private void ExecuteStopWorkflow()
        {
            IsRunning = false;
            // TODO: 停止工作流
        }

        private void ExecuteShowSettings()
        {
            // TODO: 显示设置对话框
        }

        private void ExecuteShowAbout()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void ExecuteShowHelp()
        {
            var helpWindow = new HelpWindow();
            helpWindow.ShowDialog();
        }

        private void ExecuteShowShortcuts()
        {
            var helpWindow = new HelpWindow();
            helpWindow.ShowDialog();
            // TODO: 直接跳转到快捷键页面
        }

        /// <summary>
        /// 加载节点属性到属性面板
        /// </summary>
        public void LoadNodeProperties(Models.WorkflowNode? node)
        {
            if (node == null)
            {
                PropertyGroups.Clear();
                return;
            }

            PropertyGroups.Clear();

            // 基本信息
            var basicGroup = new Models.PropertyGroup
            {
                Name = "📋 基本信息",
                IsExpanded = true,
                Parameters = new ObservableCollection<Models.PropertyItem>
                {
                    new Models.PropertyItem { Label = "名称", Value = node.Name },
                    new Models.PropertyItem { Label = "ID", Value = node.Id },
                    new Models.PropertyItem { Label = "类型", Value = node.AlgorithmType ?? "未知" }
                }
            };
            PropertyGroups.Add(basicGroup);

            // 参数配置
            var paramGroup = new Models.PropertyGroup
            {
                Name = "🔧 参数配置",
                IsExpanded = true,
                Parameters = new ObservableCollection<Models.PropertyItem>()
            };

            if (node.Parameters != null)
            {
                foreach (var param in node.Parameters)
                {
                    paramGroup.Parameters.Add(new Models.PropertyItem
                    {
                        Label = param.Key,
                        Value = param.Value?.ToString() ?? ""
                    });
                }
            }
            PropertyGroups.Add(paramGroup);

            // 性能统计
            var perfGroup = new Models.PropertyGroup
            {
                Name = "📊 性能统计",
                IsExpanded = true,
                Parameters = new ObservableCollection<Models.PropertyItem>
                {
                    new Models.PropertyItem { Label = "执行次数", Value = "0" },
                    new Models.PropertyItem { Label = "平均时间", Value = "0 ms" },
                    new Models.PropertyItem { Label = "成功率", Value = "100%" }
                }
            };
            PropertyGroups.Add(perfGroup);
        }

        /// <summary>
        /// 添加节点到当前工作流（通过命令模式）
        /// </summary>
        public void AddNodeToWorkflow(WorkflowNode node)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.AddNodeCommand(WorkflowTabViewModel.SelectedTab.WorkflowNodes, node);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// 从当前工作流删除节点（通过命令模式）
        /// </summary>
        public void DeleteNodeFromWorkflow(WorkflowNode node)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.DeleteNodeCommand(
                WorkflowTabViewModel.SelectedTab.WorkflowNodes,
                WorkflowTabViewModel.SelectedTab.WorkflowConnections,
                node);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// 移动节点到新位置（通过命令模式）
        /// </summary>
        public void MoveNode(WorkflowNode node, Point newPosition)
        {
            var command = new AppCommands.MoveNodeCommand(node, node.Position, newPosition);
            if (WorkflowTabViewModel.SelectedTab != null)
            {
                WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
            }
        }

        /// <summary>
        /// 添加连接到当前工作流（通过命令模式）
        /// </summary>
        public void AddConnectionToWorkflow(WorkflowConnection connection)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.AddConnectionCommand(WorkflowTabViewModel.SelectedTab.WorkflowConnections, connection);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// 从当前工作流删除连接（通过命令模式）
        /// </summary>
        public void DeleteConnectionFromWorkflow(WorkflowConnection connection)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.DeleteConnectionCommand(WorkflowTabViewModel.SelectedTab.WorkflowConnections, connection);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// 批量删除选中的节点（通过命令模式）
        /// </summary>
        public void DeleteSelectedNodes()
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var selectedNodes = WorkflowTabViewModel.SelectedTab.WorkflowNodes.Where(n => n.IsSelected).ToList();
            var command = new AppCommands.BatchDeleteNodesCommand(
                WorkflowTabViewModel.SelectedTab.WorkflowNodes,
                WorkflowTabViewModel.SelectedTab.WorkflowConnections,
                selectedNodes);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);

            // 清除选中状态
            SelectedNode = null;
            ClearNodeSelections();
        }

        /// <summary>
        /// 清除所有节点的选中状态
        /// </summary>
        private void ClearNodeSelections()
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            foreach (var node in WorkflowTabViewModel.SelectedTab.WorkflowNodes)
            {
                node.IsSelected = false;
            }
        }

        /// <summary>
        /// 判断是否可以删除选中节点
        /// </summary>
        private bool CanDeleteSelectedNodes()
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return false;

            return WorkflowTabViewModel.SelectedTab.WorkflowNodes.Any(n => n.IsSelected);
        }

        /// <summary>
        /// 执行删除选中节点
        /// </summary>
        private void ExecuteDeleteSelectedNodes()
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var selectedNodes = WorkflowTabViewModel.SelectedTab.WorkflowNodes.Where(n => n.IsSelected).ToList();
            var selectedCount = selectedNodes.Count;
            if (selectedCount == 0)
                return;

            var result = System.Windows.MessageBox.Show(
                $"确定要删除选中的 {selectedCount} 个节点吗?",
                "确认删除",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var command = new AppCommands.BatchDeleteNodesCommand(
                    WorkflowTabViewModel.SelectedTab.WorkflowNodes,
                    WorkflowTabViewModel.SelectedTab.WorkflowConnections,
                    selectedNodes);
                WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);

                // 清除选中状态
                SelectedNode = null;
                ClearNodeSelections();

                AddLog($"已删除 {selectedCount} 个节点");
            }
        }

        private void ExecuteOpenDebugWindow(Models.WorkflowNode? node)
        {
            if (node != null)
            {
                try
                {
                    // 从ToolRegistry获取工具信息和插件
                    var toolId = node.AlgorithmType ?? node.Name;
                    var toolMetadata = ToolRegistry.GetToolMetadata(toolId);
                    var toolPlugin = ToolRegistry.GetToolPlugin(toolId);

                    if (toolMetadata == null)
                    {
                        System.Windows.MessageBox.Show(
                            $"未找到工具 '{toolId}' 的元数据信息",
                            "工具未找到",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    // 创建调试窗口
                    var debugWindow = new DebugWindow(toolId, toolPlugin ?? new DefaultToolPlugin(), toolMetadata);
                    debugWindow.Owner = System.Windows.Application.Current.MainWindow;
                    debugWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"打开调试窗口失败: {ex.Message}",
                        "错误",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 切换最大外接矩形显示
        /// </summary>
        private void ExecuteToggleBoundingRectangle()
        {
            AddLog("[ToggleBoundingRectangle] ========== 切换最大外接矩形显示 ==========");

            try
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    AddLog("[ToggleBoundingRectangle] ❌ MainWindow为null");
                    return;
                }

                AddLog("[ToggleBoundingRectangle] ✓ MainWindow已找到");

                // 使用MainWindow中保存的WorkflowCanvasControl引用
                var workflowCanvas = mainWindow.GetCurrentWorkflowCanvas();
                if (workflowCanvas == null)
                {
                    AddLog("[ToggleBoundingRectangle] ❌ 无法获取WorkflowCanvasControl");
                    return;
                }

                AddLog("[ToggleBoundingRectangle] ✓ 获取到WorkflowCanvasControl");

                ToggleBoundingRectangleOnCanvas(workflowCanvas);
            }
            catch (Exception ex)
            {
                AddLog($"[ToggleBoundingRectangle] ❌ 错误: {ex.Message}");
                AddLog($"[ToggleBoundingRectangle] 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 在指定的WorkflowCanvasControl上切换矩形显示
        /// </summary>
        private void ToggleBoundingRectangleOnCanvas(Controls.WorkflowCanvasControl workflowCanvas)
        {
            workflowCanvas.ShowBoundingRectangle = !workflowCanvas.ShowBoundingRectangle;

            // 如果开启矩形显示，使用第一个连接作为示例
            if (workflowCanvas.ShowBoundingRectangle)
            {
                var selectedTab = WorkflowTabViewModel?.SelectedTab;
                if (selectedTab != null && selectedTab.WorkflowConnections != null && selectedTab.WorkflowConnections.Count > 0)
                {
                    var firstConnection = selectedTab.WorkflowConnections.FirstOrDefault();
                    if (firstConnection != null)
                    {
                        workflowCanvas.BoundingSourceNodeId = firstConnection.SourceNodeId;
                        workflowCanvas.BoundingTargetNodeId = firstConnection.TargetNodeId;
                        AddLog($"[ToggleBoundingRectangle] 显示连接 {firstConnection.Id} 的外接矩形");
                        AddLog($"[ToggleBoundingRectangle]   源节点ID: {firstConnection.SourceNodeId}");
                        AddLog($"[ToggleBoundingRectangle]   目标节点ID: {firstConnection.TargetNodeId}");
                    }
                    else
                    {
                        AddLog("[ToggleBoundingRectangle] ⚠️ 未找到连接");
                        workflowCanvas.ShowBoundingRectangle = false;
                    }
                }
                else
                {
                    AddLog("[ToggleBoundingRectangle] ⚠️ 当前Tab没有连接");
                    workflowCanvas.ShowBoundingRectangle = false;
                }
            }

            AddLog($"[ToggleBoundingRectangle] ========== 外接矩形: {(workflowCanvas.ShowBoundingRectangle ? "显示" : "隐藏")} ==========");
        }

        /// <summary>
        /// 切换路径拐点显示
        /// </summary>
        private void ExecuteTogglePathPoints()
        {
            AddLog("[TogglePathPoints] 切换所有连接的路径拐点显示");

            if (WorkflowTabViewModel?.SelectedTab?.WorkflowConnections != null)
            {
                var newState = !WorkflowTabViewModel.SelectedTab.WorkflowConnections.Any(c => c.ShowPathPoints);

                foreach (var connection in WorkflowTabViewModel.SelectedTab.WorkflowConnections)
                {
                    connection.ShowPathPoints = newState;
                }

                AddLog($"[TogglePathPoints] 所有连接的路径拐点: {(newState ? "显示" : "隐藏")}");
            }
        }

        /// <summary>
        /// 查找指定类型的子元素
        /// </summary>
        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null)
                return null;

            if (parent is T child)
                return child;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var found = FindVisualChild<T>(System.Windows.Media.VisualTreeHelper.GetChild(parent, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// 默认工具插件 - 用于兼容性
        /// </summary>
        private class DefaultToolPlugin : SunEyeVision.PluginSystem.IToolPlugin
        {
            public string Name => "Default Tool";
            public string Version => "1.0.0";
            public string Author => "SunEyeVision";
            public string Description => "Default tool plugin";
            public string PluginId => "default.tool";
            public List<string> Dependencies => new List<string>();
            public string Icon => "🔧";

            private bool _isLoaded = true;
            public bool IsLoaded => _isLoaded;

            public void Initialize() { }
            public void Unload() { }

            public List<System.Type> GetAlgorithmNodes() => new List<System.Type>();

            public List<SunEyeVision.PluginSystem.ToolMetadata> GetToolMetadata() => new List<SunEyeVision.PluginSystem.ToolMetadata>();

            public SunEyeVision.Interfaces.IImageProcessor CreateToolInstance(string toolId)
            {
                throw new NotImplementedException();
            }

            public SunEyeVision.Models.AlgorithmParameters GetDefaultParameters(string toolId)
            {
                return new SunEyeVision.Models.AlgorithmParameters();
            }

            public SunEyeVision.PluginSystem.ValidationResult ValidateParameters(string toolId, SunEyeVision.Models.AlgorithmParameters parameters)
            {
                return SunEyeVision.PluginSystem.ValidationResult.Success();
            }
        }
    }
}
