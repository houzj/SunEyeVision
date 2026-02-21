using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AppCommands = SunEyeVision.UI.Commands;
using SunEyeVision.UI.Models;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.UI;
using SunEyeVision.Workflow;
using SunEyeVision.UI.Controls.Rendering;
using UIWorkflowNode = SunEyeVision.UI.Models.WorkflowNode;
using WorkflowWorkflowNode = SunEyeVision.Workflow.WorkflowNode;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 图像显示类型枚举
    /// </summary>
    public enum ImageDisplayType
    {
        Original,    // 原始图像
        Processed,   // 处理后图像
        Result       // 结果图像
    }

    /// <summary>
    /// 图像显示类型项
    /// </summary>
    public class ImageDisplayTypeItem
    {
        public ImageDisplayType Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    /// <summary>
    /// 计算结果项
    /// </summary>
    public class ResultItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

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

        // 图像类型相关
        private ImageDisplayTypeItem? _selectedImageType;
        private bool _showImagePreview = false;
        private BitmapSource? _originalImage;
        private BitmapSource? _processedImage;
        private BitmapSource? _resultImage;

        // 图像预览相关
        private bool _autoSwitchEnabled = false;
        private int _currentImageIndex = -1;

        // 所有工作流运行状态
        private bool _isAllWorkflowsRunning = false;
        private string _allWorkflowsRunButtonText = "连续运行";

        // 工作流执行管理器
        private readonly Services.WorkflowExecutionManager _executionManager;

        // 属性面板相关
        private ObservableCollection<Models.PropertyGroup> _propertyGroups = new ObservableCollection<Models.PropertyGroup>();
        private string _logText = "[系统] 等待操作...\n";

        // 面板折叠状态
        private bool _isToolboxCollapsed = true;
        private bool _isImageDisplayCollapsed = false;
        private bool _isPropertyPanelCollapsed = false;
        private double _toolboxWidth = 260;
        private double _rightPanelWidth = 500;
        private double _imageDisplayHeight = 500;

        // 分隔条相关
        private double _splitterPosition = 500; // 默认图像区域高度
        private const double DefaultPropertyPanelHeight = 300;
        private const double MinImageAreaHeight = 200;
        private const double MaxImageAreaHeight = 800;

        private double _propertyPanelActualHeight = DefaultPropertyPanelHeight;

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

        private Models.WorkflowNode? _selectedNode;
        private bool _showPropertyPanel = false;
        private Models.NodeImageData? _activeNodeImageData;
        private string? _currentDisplayNodeId = null;  // ★ 跟踪当前显示的采集节点ID，用于避免重复加载

        /// <summary>
        /// 当前活动节点的图像数据（用于绑定到图像预览控件）
        /// 每个采集节点维护独立的图像集合
        /// </summary>
        public Models.NodeImageData? ActiveNodeImageData
        {
            get => _activeNodeImageData;
            private set => SetProperty(ref _activeNodeImageData, value);
        }

        public Models.WorkflowNode? SelectedNode
        {
            get
            {
                // AddLog($"[调试] SelectedNode 读取: {(_selectedNode == null ? "null" : _selectedNode.Name)}");
                return _selectedNode;
            }
            set
            {
                bool changed = SetProperty(ref _selectedNode, value);
                AddLog($"[调试] SelectedNode 设置: value={value?.Name ?? "null"}, changed={changed}");
                
                if (changed)
                {
                    AddLog($"[调试] SelectedNode 变更: {(value == null ? "null" : value.Name)}");

                    // 更新属性面板可见性
                    ShowPropertyPanel = value != null;

                    // 更新活动节点的图像数据（核心：切换节点时切换图像集合）
                    UpdateActiveNodeImageData(value);

                    // 节点选中状态变化时，更新图像预览显示
                    UpdateImagePreviewVisibility(value);
                    // 加载节点属性到属性面板
                    LoadNodeProperties(value);
                }
            }
        }

        /// <summary>
        /// 更新活动节点的图像数据
        /// 实现不同采集节点拥有独立的图像预览器
        /// ★ 优化：避免重复加载相同节点的缩略图
        /// </summary>
        private void UpdateActiveNodeImageData(Models.WorkflowNode? node)
        {
            // 情况1：切换到图像采集节点
            if (node?.IsImageCaptureNode == true)
            {
                // 确保节点有图像数据容器（延迟初始化）
                node.ImageData ??= new Models.NodeImageData(node.Id);
                
                // ★ 关键优化：检查是否切换到相同的节点且当前已有数据
                bool isSameNode = _currentDisplayNodeId == node.Id;
                bool hasActiveData = ActiveNodeImageData != null;
                
                if (isSameNode && hasActiveData)
                {
                    // 相同节点且当前有数据
                    // ★ 仍然需要更新 ActiveNodeImageData 以确保绑定触发更新
                    // （因为可能从非采集节点切换回来，ActiveNodeImageData 曾被设为 null）
                    ActiveNodeImageData = node.ImageData;
                    AddLog($"[调试] 保持节点 {node.Name} 的图像集合（相同节点，跳过清空缩略图）");
                    return;
                }
                
                // ★ 不同节点或之前数据已清空：更新跟踪ID并清空缩略图
                _currentDisplayNodeId = node.Id;
                int imageCount = node.ImageData.PrepareForDisplay();  // 清空缩略图
                
                ActiveNodeImageData = node.ImageData;
                AddLog($"[调试] 切换到节点 {node.Name} 的图像集合，共 {imageCount} 张图像（延迟加载模式）");
            }
            // 情况2：切换到非图像采集节点
            // ★ 不做任何操作，由 UpdateImagePreviewVisibility 统一处理
            // 这样可以保持 _currentDisplayNodeId 和 ActiveNodeImageData 不变，
            // 当切换到相同上游采集节点的非采集节点时，不会触发重新加载
            // 原问题：之前这里会清空 _currentDisplayNodeId，导致 UpdateImagePreviewVisibility
            // 中的 isSameNode 判断失效，每次切换都会重新加载
        }

        /// <summary>
        /// 显示属性面板
        /// </summary>
        public bool ShowPropertyPanel
        {
            get => _showPropertyPanel;
            set => SetProperty(ref _showPropertyPanel, value);
        }
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
            set
            {
                if (SetProperty(ref _imageScale, value))
                {
                    OnPropertyChanged(nameof(DisplayImage));
                }
            }
        }

        /// <summary>
        /// 图像显示类型集合
        /// </summary>
        public ObservableCollection<ImageDisplayTypeItem> ImageDisplayTypes { get; }

        /// <summary>
        /// 当前选中的图像显示类型
        /// </summary>
        public ImageDisplayTypeItem? SelectedImageType
        {
            get => _selectedImageType;
            set
            {
                if (SetProperty(ref _selectedImageType, value))
                {
                    UpdateDisplayImage();
                }
            }
        }

        /// <summary>
        /// 显示图像载入及预览模块（仅对ImageCaptureTool节点显示）
        /// </summary>
        public bool ShowImagePreview
        {
            get => _showImagePreview;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[ShowImagePreview] Setter被调用: {_showImagePreview} -> {value}");
                if (SetProperty(ref _showImagePreview, value))
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowImagePreview] PropertyChanged已触发, 当前值: {_showImagePreview}");
                    OnPropertyChanged(nameof(ImagePreviewHeight));
                }
            }
        }

        /// <summary>
        /// 图像预览区域高度（用于动态控制图像预览模块的空间）
        /// </summary>
        public GridLength ImagePreviewHeight
        {
            get => ShowImagePreview ? new GridLength(60) : new GridLength(0);
        }

        /// <summary>
        /// 计算结果集合
        /// </summary>
        public ObservableCollection<ResultItem> CalculationResults { get; }

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
        /// 图像显示区域高度（分隔条上方区域）
        /// </summary>
        public double SplitterPosition
        {
            get => _splitterPosition;
            private set
            {
                // 确保在合理范围内
                value = Math.Max(MinImageAreaHeight, Math.Min(MaxImageAreaHeight, value));
                if (Math.Abs(_splitterPosition - value) > 1) // 避免微小抖动
                {
                    _splitterPosition = value;
                    OnPropertyChanged(nameof(SplitterPosition));

                    // 更新属性面板实际高度
                    double availableHeight = _splitterPosition;
                    double propertyHeight = Math.Max(200, Math.Min(600, 900 - availableHeight));
                    PropertyPanelActualHeight = propertyHeight;
                }
            }
        }

        /// <summary>
        /// 属性面板实际高度
        /// </summary>
        public double PropertyPanelActualHeight
        {
            get => _propertyPanelActualHeight;
            private set
            {
                if (Math.Abs(_propertyPanelActualHeight - value) > 1)
                {
                    _propertyPanelActualHeight = value;
                    OnPropertyChanged(nameof(PropertyPanelActualHeight));
                }
            }
        }

        /// <summary>
        /// 保存分隔条位置（从代码后台调用）
        /// </summary>
        public void SaveSplitterPosition(double position)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveSplitterPosition] 保存位置: {position}");
            SplitterPosition = position;

            // 可选：保存到用户设置文件
            // Settings.Default.SplitterPosition = position;
            // Settings.Default.Save();
        }

        /// <summary>
        /// 所有工作流是否正在运行
        /// </summary>
        public bool IsAllWorkflowsRunning
        {
            get => _isAllWorkflowsRunning;
            set => SetProperty(ref _isAllWorkflowsRunning, value);
        }

        /// <summary>
        /// 所有工作流运行按钮文本
        /// </summary>
        public string AllWorkflowsRunButtonText
        {
            get => _allWorkflowsRunButtonText;
            set => SetProperty(ref _allWorkflowsRunButtonText, value);
        }

        /// <summary>
        /// 原始图像
        /// </summary>
        public BitmapSource? OriginalImage
        {
            get => _originalImage;
            set
            {
                if (SetProperty(ref _originalImage, value))
                {
                    UpdateDisplayImage();
                }
            }
        }

        /// <summary>
        /// 处理后图像
        /// </summary>
        public BitmapSource? ProcessedImage
        {
            get => _processedImage;
            set
            {
                if (SetProperty(ref _processedImage, value))
                {
                    UpdateDisplayImage();
                }
            }
        }

        /// <summary>
        /// 结果图像
        /// </summary>
        public BitmapSource? ResultImage
        {
            get => _resultImage;
            set
            {
                if (SetProperty(ref _resultImage, value))
                {
                    UpdateDisplayImage();
                }
            }
        }

        /// <summary>
        /// 图像集合（使用批量操作优化集合）
        /// </summary>
        public BatchObservableCollection<Controls.ImageInfo> ImageCollection { get; }

        /// <summary>
        /// 是否启用自动切换
        /// </summary>
        public bool AutoSwitchEnabled
        {
            get => _autoSwitchEnabled;
            set => SetProperty(ref _autoSwitchEnabled, value);
        }

        /// <summary>
        /// 当前显示的图像索引
        /// </summary>
        public int CurrentImageIndex
        {
            get => _currentImageIndex;
            set
            {
                if (SetProperty(ref _currentImageIndex, value))
                {
                    UpdateCurrentImageDisplay();
                }
            }
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            // 新日志追加到末尾
            LogText += $"[{timestamp}] {message}\n";

            // 限制日志条目数量，最多保留100条
            var lines = LogText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 100)
            {
                LogText = string.Join("\n", lines.Skip(lines.Length - 100)) + "\n";
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

        // 所有工作流控制命令
        public ICommand RunAllWorkflowsCommand { get; }
        public ICommand ToggleContinuousAllCommand { get; }

        // 图像控制命令
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand FitToWindowCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }

        // 图像载入命令
        public ICommand BrowseImageCommand { get; }
        public ICommand LoadImageCommand { get; }
        public ICommand ClearImageCommand { get; }

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

            // 初始化图像显示类型
            ImageDisplayTypes = new ObservableCollection<ImageDisplayTypeItem>
            {
                new ImageDisplayTypeItem { Type = ImageDisplayType.Original, DisplayName = "原始图像", Icon = "📷" },
                new ImageDisplayTypeItem { Type = ImageDisplayType.Processed, DisplayName = "处理后图像", Icon = "⚙️" },
                new ImageDisplayTypeItem { Type = ImageDisplayType.Result, DisplayName = "结果图像", Icon = "✓" }
            };
            SelectedImageType = ImageDisplayTypes.FirstOrDefault();

            // 初始化计算结果集合
            CalculationResults = new ObservableCollection<ResultItem>();

            // 初始化图像集合（批量操作优化）
            ImageCollection = new BatchObservableCollection<Controls.ImageInfo>();

            // 初始化工作流执行管理器
            _executionManager = new Services.WorkflowExecutionManager(new Services.DefaultInputProvider());

            // 订阅执行管理器的事件
            _executionManager.WorkflowExecutionStarted += OnWorkflowExecutionStarted;
            _executionManager.WorkflowExecutionCompleted += OnWorkflowExecutionCompleted;
            _executionManager.WorkflowExecutionStopped += OnWorkflowExecutionStopped;
            _executionManager.WorkflowExecutionError += OnWorkflowExecutionError;
            _executionManager.WorkflowExecutionProgress += OnWorkflowExecutionProgress;

            // 初始化当前画布类型
            UpdateCurrentCanvasType();

            // 订阅选中画布变化事件，更新撤销/重做按钮状态
            WorkflowTabViewModel.SelectionChanged += OnSelectedTabChanged;

            // 订阅工作流状态变化事件
            WorkflowTabViewModel.WorkflowStatusChanged += OnWorkflowStatusChanged;

            // 订阅初始画布的命令管理器
            SubscribeToCurrentCommandManager();

            InitializeTools();
            // InitializeSampleNodes(); // 已禁用：程序启动时不加载测试节点和连线
            InitializePropertyGroups();

            NewWorkflowCommand = new RelayCommand(ExecuteNewWorkflow);
            OpenWorkflowCommand = new RelayCommand(ExecuteOpenWorkflow);
            SaveWorkflowCommand = new RelayCommand(ExecuteSaveWorkflow);
            SaveAsWorkflowCommand = new RelayCommand(ExecuteSaveAsWorkflow);
            RunWorkflowCommand = new RelayCommand(async () => await ExecuteRunWorkflow(), () => !IsRunning);
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

            // 所有工作流控制命令
            RunAllWorkflowsCommand = new RelayCommand(async () => await ExecuteRunAllWorkflows(), () => !IsAllWorkflowsRunning);
            ToggleContinuousAllCommand = new RelayCommand(ExecuteToggleContinuousAll, () => true);

            // 图像控制命令
            ZoomInCommand = new RelayCommand(ExecuteZoomIn);
            ZoomOutCommand = new RelayCommand(ExecuteZoomOut);
            FitToWindowCommand = new RelayCommand(ExecuteFitToWindow);
            ResetViewCommand = new RelayCommand(ExecuteResetView);
            ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreen);

            // 图像载入命令
            BrowseImageCommand = new RelayCommand(ExecuteBrowseImage);
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClearImageCommand = new RelayCommand(ExecuteClearImage);
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
            }
        }

        /// <summary>
        /// 工作流状态变化处理
        /// </summary>
        private void OnWorkflowStatusChanged(object? sender, EventArgs e)
        {
            // 更新所有工作流运行状态
            IsAllWorkflowsRunning = WorkflowTabViewModel.IsAnyWorkflowRunning;
            AllWorkflowsRunButtonText = IsAllWorkflowsRunning ? "停止运行" : "连续运行";

            // 更新命令的CanExecute状态
            (RunAllWorkflowsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleContinuousAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        private async System.Threading.Tasks.Task ExecuteRunWorkflow()
        {
            AddLog("=== 开始执行工作流 ===");

            if (WorkflowTabViewModel == null)
            {
                AddLog("⚠️ WorkflowTabViewModel 为 null");
                return;
            }

            if (WorkflowTabViewModel.SelectedTab == null)
            {
                AddLog("⚠️ 没有选中的工作流标签页");
                AddLog("⚠️ 请确保至少有一个工作流标签页被选中");
                return;
            }

            AddLog($"📋 当前工作流: {WorkflowTabViewModel.SelectedTab.Name}");
            AddLog($"📊 节点数量: {WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count}");
            AddLog($"🔗 连接数量: {WorkflowTabViewModel.SelectedTab.WorkflowConnections.Count}");

            if (WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count == 0)
            {
                AddLog("⚠️ 当前工作流没有节点");
                AddLog("💡 提示：请从左侧工具箱拖拽算法节点到画布上");
                AddLog("💡 可选节点：图像采集、灰度化、高斯模糊、二值化、边缘检测、形态学操作");
                return;
            }

            IsRunning = true;
            AddLog("🚀 调用执行引擎...");

            try
            {
                await _executionManager.RunSingleAsync(WorkflowTabViewModel.SelectedTab);
                AddLog("✅ 工作流执行完成");
            }
            catch (Exception ex)
            {
                AddLog($"❌ 工作流执行失败: {ex.Message}");
                AddLog($"❌ 异常详情: {ex.StackTrace}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ExecuteStopWorkflow()
        {
            if (WorkflowTabViewModel?.SelectedTab == null)
            {
                AddLog("⚠️ 没有选中的工作流标签页");
                return;
            }

            _executionManager.StopContinuousRun(WorkflowTabViewModel.SelectedTab);
            IsRunning = false;
            AddLog("⏹️ 停止工作流执行");
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
        public void AddNodeToWorkflow(UIWorkflowNode node)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.AddNodeCommand(WorkflowTabViewModel.SelectedTab.WorkflowNodes, node);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// 从当前工作流删除节点（通过命令模式）
        /// </summary>
        public void DeleteNodeFromWorkflow(UIWorkflowNode node)
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
        public void MoveNode(UIWorkflowNode node, Point newPosition)
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

                    // 使用NodeInterfaceFactory决定打开哪个界面
                    var interfaceType = NodeInterfaceFactory.GetInterfaceType(node.ToWorkflowNode(), toolMetadata);

                    switch (interfaceType)
                    {
                        case NodeInterfaceType.DebugWindow:
                            // 使用工厂创建调试窗口
                            var debugWindow = ToolDebugWindowFactory.CreateDebugWindow(toolId, toolPlugin, toolMetadata);
                            debugWindow.Owner = System.Windows.Application.Current.MainWindow;
                            debugWindow.ShowDialog();
                            AddLog($"🔧 打开调试窗口: {node.Name}");
                            break;

                        case NodeInterfaceType.NewWorkflowCanvas:
                            // 创建新的工作流标签页（子程序节点）
                            CreateSubroutineWorkflowTab(node);
                            break;

                        case NodeInterfaceType.SubroutineEditor:
                            // 子程序编辑器（条件配置界面）
                            AddLog($"📝 打开子程序编辑器: {node.Name}");
                            // TODO: 实现子程序编辑器
                            System.Windows.MessageBox.Show(
                                "子程序编辑器功能待实现",
                                "功能提示",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                            break;

                        case NodeInterfaceType.None:
                        default:
                            // 不打开任何界面
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"打开节点界面失败: {ex.Message}",
                        "错误",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    AddLog($"❌ 打开节点界面失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 为子程序节点创建新的工作流标签页
        /// </summary>
        /// <param name="subroutineNode">子程序节点</param>
        private void CreateSubroutineWorkflowTab(Models.WorkflowNode subroutineNode)
        {
            try
            {
                if (WorkflowTabViewModel == null)
                {
                    AddLog("⚠️ WorkflowTabViewModel 为 null");
                    return;
                }

                // 使用子程序节点名称作为工作流名称
                string workflowName = subroutineNode.Name;
                if (string.IsNullOrWhiteSpace(workflowName))
                {
                    workflowName = "子程序工作流";
                }

                AddLog($"📋 创建子程序工作流标签页: {workflowName}");

                // 创建新的工作流标签页
                var newWorkflowTab = new WorkflowTabViewModel
                {
                    Name = workflowName,
                    Id = Guid.NewGuid().ToString()
                };

                // 添加到标签页集合
                WorkflowTabViewModel.Tabs.Add(newWorkflowTab);

                // 选中新创建的标签页
                WorkflowTabViewModel.SelectedTab = newWorkflowTab;

                AddLog($"✅ 子程序工作流 '{workflowName}' 创建成功");
                AddLog($"💡 提示：您现在可以在这个工作流中添加节点来定义子程序逻辑");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"创建子程序工作流失败: {ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                AddLog($"❌ 创建子程序工作流失败: {ex.Message}");
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
        /// 执行单次运行所有工作流
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteRunAllWorkflows()
        {
            AddLog("🚀 开始单次运行所有工作流...");
            await WorkflowTabViewModel.RunAllWorkflowsAsync();
            AddLog("✅ 所有工作流单次运行完成");
        }

        /// <summary>
        /// 切换所有工作流的连续运行/停止
        /// </summary>
        private void ExecuteToggleContinuousAll()
        {
            if (IsAllWorkflowsRunning)
            {
                AddLog("⏹️ 停止所有工作流连续运行");
                WorkflowTabViewModel.StopAllWorkflows();
            }
            else
            {
                AddLog("🔄 开始所有工作流连续运行");
                WorkflowTabViewModel.StartAllWorkflows();
            }
        }

        /// <summary>
        /// 工作流执行开始事件处理
        /// </summary>
        private void OnWorkflowExecutionStarted(object? sender, Services.WorkflowExecutionEventArgs e)
        {
            AddLog($"🚀 工作流开始执行: {e.WorkflowId}");
        }

        /// <summary>
        /// 工作流执行完成事件处理
        /// </summary>
        private void OnWorkflowExecutionCompleted(object? sender, Services.WorkflowExecutionEventArgs e)
        {
            AddLog($"✅ 工作流执行完成: {e.WorkflowId}");
        }

        /// <summary>
        /// 工作流执行停止事件处理
        /// </summary>
        private void OnWorkflowExecutionStopped(object? sender, Services.WorkflowExecutionEventArgs e)
        {
            AddLog($"⏹️ 工作流执行停止: {e.WorkflowId}");
        }

        /// <summary>
        /// 工作流执行错误事件处理
        /// </summary>
        private void OnWorkflowExecutionError(object? sender, Services.WorkflowExecutionEventArgs e)
        {
            AddLog($"❌ 工作流执行错误: {e.WorkflowId} - {e.ErrorMessage}");
        }

        /// <summary>
        /// 工作流执行进度事件处理
        /// </summary>
        private void OnWorkflowExecutionProgress(object? sender, Services.WorkflowExecutionProgressEventArgs e)
        {
            try
            {
                AddLog(e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindowViewModel] OnWorkflowExecutionProgress异常: {ex.Message}");
                AddLog($"⚠️ 日志处理异常: {ex.Message}");
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

        #region 图像控制命令

        /// <summary>
        /// 放大图像
        /// </summary>
        private void ExecuteZoomIn()
        {
            ImageScale = Math.Min(ImageScale * 1.2, 5.0);
            AddLog($"🔎 图像放大: {ImageScale:P0}");
        }

        /// <summary>
        /// 缩小图像
        /// </summary>
        private void ExecuteZoomOut()
        {
            ImageScale = Math.Max(ImageScale / 1.2, 0.1);
            AddLog($"🔎 图像缩小: {ImageScale:P0}");
        }

        /// <summary>
        /// 适应窗口
        /// </summary>
        private void ExecuteFitToWindow()
        {
            // TODO: 根据窗口大小计算合适的缩放比例
            ImageScale = 1.0;
            AddLog($"📐 适应窗口: {ImageScale:P0}");
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        private void ExecuteResetView()
        {
            ImageScale = 1.0;
            AddLog($"⟲ 重置视图: {ImageScale:P0}");
        }

        /// <summary>
        /// 切换全屏显示
        /// </summary>
        private void ExecuteToggleFullScreen()
        {
            // TODO: 实现图像全屏显示功能
            AddLog("⛶ 切换全屏显示");
        }

        #endregion

        #region 图像载入命令

        /// <summary>
        /// 浏览图像文件
        /// </summary>
        private void ExecuteBrowseImage()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|所有文件|*.*",
                    Title = "选择图像文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    AddLog($"📁 已选择文件: {filePath}");

                    // TODO: 载入图像到OriginalImage
                    // OriginalImage = LoadImageFromFile(filePath);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 浏览图像失败: {ex.Message}");
                System.Windows.MessageBox.Show($"浏览图像失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 载入图像
        /// </summary>
        private void ExecuteLoadImage()
        {
            try
            {
                if (OriginalImage == null)
                {
                    AddLog("⚠️ 请先选择图像文件");
                    return;
                }

                AddLog("✅ 图像载入成功");
                // TODO: 处理图像并更新ProcessedImage和ResultImage
            }
            catch (Exception ex)
            {
                AddLog($"❌ 载入图像失败: {ex.Message}");
                System.Windows.MessageBox.Show($"载入图像失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清除图像
        /// </summary>
        private void ExecuteClearImage()
        {
            try
            {
                OriginalImage = null;
                ProcessedImage = null;
                ResultImage = null;
                ImageScale = 1.0;
                AddLog("🗑️ 已清除图像");
            }
            catch (Exception ex)
            {
                AddLog($"❌ 清除图像失败: {ex.Message}");
            }
        }

        #endregion

        #region 图像预览命令

        /// <summary>
        /// 更新当前图像显示
        /// </summary>
        private void UpdateCurrentImageDisplay()
        {
            if (CurrentImageIndex < 0 || CurrentImageIndex >= ImageCollection.Count)
            {
                OriginalImage = null;
                ProcessedImage = null;
                ResultImage = null;
                return;
            }

            var imageInfo = ImageCollection[CurrentImageIndex];
            
            // 主动触发FullImage加载
            var fullImage = imageInfo.FullImage;
            
            if (fullImage != null)
            {
                OriginalImage = fullImage;
                AddLog($"📷 加载图像: {imageInfo.Name}");
                
                // 确保DisplayImage被更新
                UpdateDisplayImage();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新显示图像
        /// </summary>
        private void UpdateDisplayImage()
        {
            if (SelectedImageType == null)
            {
                return;
            }

            switch (SelectedImageType.Type)
            {
                case ImageDisplayType.Original:
                    DisplayImage = OriginalImage;
                    break;
                case ImageDisplayType.Processed:
                    DisplayImage = ProcessedImage;
                    break;
                case ImageDisplayType.Result:
                    DisplayImage = ResultImage;
                    break;
            }
        }

        /// <summary>
        /// 更新计算结果
        /// </summary>
        public void UpdateCalculationResults(Dictionary<string, object> results)
        {
            CalculationResults.Clear();

            if (results == null || results.Count == 0)
                return;

            foreach (var kvp in results)
            {
                CalculationResults.Add(new ResultItem
                {
                    Name = kvp.Key,
                    Value = kvp.Value?.ToString() ?? "null"
                });
            }

            AddLog($"📊 更新计算结果: {results.Count} 项");
        }

        #region 图像预览器智能显示

        /// <summary>
        /// 更新图像预览显示状态（基于连接关系智能切换）
        /// </summary>
        /// <remarks>
        /// 规则：
        /// 1. 选中图像采集节点 → 显示该节点的图像（如有）
        /// 2. 选中其他节点 → BFS追溯上游采集节点，找到则显示其图像
        ///    ★ 优化：如果上游采集节点与当前显示的相同，不重新加载
        /// 3. 无上游采集节点或无图像 → 隐藏图像预览器
        /// </remarks>
        public void UpdateImagePreviewVisibility(Models.WorkflowNode? selectedNode)
        {
            AddLog($"[UpdateImagePreviewVisibility] ▶ 开始处理, selectedNode={selectedNode?.Name ?? "null"}");
            
            // 情况1：没有选中节点 → 隐藏
            if (selectedNode == null)
            {
                AddLog($"[UpdateImagePreviewVisibility] 情况1: 没有选中节点");
                ShowImagePreview = false;
                ActiveNodeImageData = null;
                _currentDisplayNodeId = null;  // ★ 清除跟踪ID
                AddLog("[调试] 图像预览: 隐藏 (没有选中节点)");
                return;
            }

            // 情况2：选中的是图像采集节点 → 始终显示图像预览器（即使暂时没有图像）
            if (selectedNode.IsImageCaptureNode)
            {
                AddLog($"[UpdateImagePreviewVisibility] 情况2: 选中图像采集节点 {selectedNode.Name}");
                UpdateActiveNodeImageData(selectedNode);
                ShowImagePreview = true;
                AddLog($"[调试] 图像预览: 显示 (选中采集节点: {selectedNode.Name}, 图像数: {selectedNode.ImageData?.ImageCount ?? 0})");
                OnPropertyChanged(nameof(ShowImagePreview));
                return;
            }

            // 情况3：选中的不是图像采集节点 → BFS追溯上游采集节点
            // ★ 快速检查：如果没有连接，不可能有上游节点，直接隐藏
            var connections = WorkflowTabViewModel?.SelectedTab?.WorkflowConnections;
            if (connections == null || connections.Count == 0)
            {
                AddLog($"[UpdateImagePreviewVisibility] 情况3-快速返回: 无连接，无需BFS搜索");
                ShowImagePreview = false;
                ActiveNodeImageData = null;
                _currentDisplayNodeId = null;
                AddLog($"[调试] 图像预览: 隐藏 (当前节点: {selectedNode.Name}, 无连接)");
                OnPropertyChanged(nameof(ShowImagePreview));
                return;
            }
            
            AddLog($"[UpdateImagePreviewVisibility] 情况3: 非图像采集节点 {selectedNode.Name}, 连接数={connections.Count}, 开始BFS查找上游");
            var sourceCaptureNode = FindUpstreamImageCaptureNode(selectedNode);
            AddLog($"[UpdateImagePreviewVisibility] BFS结果: sourceCaptureNode={sourceCaptureNode?.Name ?? "null"}");

            if (sourceCaptureNode != null)
            {
                bool hasImages = sourceCaptureNode.ImageData != null && sourceCaptureNode.ImageData.ImageCount > 0;
                AddLog($"[UpdateImagePreviewVisibility] 上游节点图像状态: hasImages={hasImages}, ImageCount={sourceCaptureNode.ImageData?.ImageCount ?? 0}");
                
                // ★ 优化：检查上游采集节点是否与当前显示的相同
                bool isSameNode = _currentDisplayNodeId == sourceCaptureNode.Id;
                bool hasActiveData = ActiveNodeImageData != null;
                
                if (hasImages)
                {
                    if (isSameNode && hasActiveData)
                    {
                        // ★ 相同节点且当前有数据，不需要更新 ActiveNodeImageData
                        // 避免触发不必要的缩略图重新加载
                        ShowImagePreview = true;
                        AddLog($"[调试] 图像预览: 保持显示 (上游采集节点: {sourceCaptureNode.Name}, 当前节点: {selectedNode.Name}, 相同节点跳过更新)");
                    }
                    else
                    {
                        // 不同节点或之前数据已清空，需要更新显示
                        _currentDisplayNodeId = sourceCaptureNode.Id;
                        ActiveNodeImageData = sourceCaptureNode.ImageData;
                        ShowImagePreview = true;
                        AddLog($"[调试] 图像预览: 显示 (上游采集节点: {sourceCaptureNode.Name}, 当前节点: {selectedNode.Name})");
                    }
                }
                else
                {
                    // 上游采集节点无图像 → 隐藏
                    AddLog($"[UpdateImagePreviewVisibility] 上游节点无图像，准备隐藏");
                    ShowImagePreview = false;
                    ActiveNodeImageData = null;
                    _currentDisplayNodeId = null;  // ★ 清除跟踪ID
                    AddLog($"[调试] 图像预览: 隐藏 (上游采集节点 {sourceCaptureNode.Name} 无图像)");
                }
            }
            else
            {
                // 无上游采集节点 → 隐藏
                AddLog($"[UpdateImagePreviewVisibility] 无上游采集节点，准备隐藏并清空ActiveNodeImageData");
                ShowImagePreview = false;
                ActiveNodeImageData = null;
                _currentDisplayNodeId = null;  // ★ 清除跟踪ID
                AddLog($"[调试] 图像预览: 隐藏 (当前节点: {selectedNode.Name}, 无上游采集节点)");
            }

            OnPropertyChanged(nameof(ShowImagePreview));
            AddLog($"[UpdateImagePreviewVisibility] ✓ 处理完成, ShowImagePreview={ShowImagePreview}");
        }

        /// <summary>
        /// 强制刷新图像预览器（用于连接创建后等场景）
        /// 即使当前 SelectedNode 未改变，也会重新计算是否显示图像预览器
        /// </summary>
        public void ForceRefreshImagePreview()
        {
            AddLog($"[ForceRefreshImagePreview] 强制刷新图像预览器, SelectedNode={_selectedNode?.Name ?? "null"}");
            UpdateImagePreviewVisibility(_selectedNode);
        }

        /// <summary>
        /// 查找选中节点的上游图像采集节点（BFS广度优先搜索）
        /// </summary>
        /// <remarks>
        /// 当存在多个上游采集节点时，返回第一个找到的采集节点：
        /// 1. BFS保证连接路径最短
        /// 2. 按节点ID排序保证确定性选择
        /// </remarks>
        /// <param name="node">起始节点</param>
        /// <returns>第一个找到的上游图像采集节点，未找到返回null</returns>
        private Models.WorkflowNode? FindUpstreamImageCaptureNode(Models.WorkflowNode node)
        {
            System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] ▶ 开始BFS搜索, 起始节点={node.Name} (Id={node.Id})");
            
            var selectedTab = WorkflowTabViewModel?.SelectedTab;
            if (selectedTab == null || selectedTab.WorkflowConnections == null || selectedTab.WorkflowNodes == null)
            {
                System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] ✗ 提前返回: selectedTab={selectedTab != null}, Connections={selectedTab?.WorkflowConnections?.Count ?? 0}, Nodes={selectedTab?.WorkflowNodes?.Count ?? 0}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] 当前工作流: {selectedTab.WorkflowNodes.Count} 个节点, {selectedTab.WorkflowConnections.Count} 条连接");

            var visited = new HashSet<string>();
            var queue = new Queue<Models.WorkflowNode>();
            queue.Enqueue(node);
            visited.Add(node.Id);
            int iterationCount = 0;

            while (queue.Count > 0)
            {
                iterationCount++;
                var currentNode = queue.Dequeue();
                System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] [迭代{iterationCount}] 处理节点: {currentNode.Name} (Id={currentNode.Id})");

                // 获取上游节点ID（按连接在集合中的顺序，再按节点ID排序保证确定性）
                var upstreamNodeIds = selectedTab.WorkflowConnections
                    .Where(conn => conn.TargetNodeId == currentNode.Id)
                    .Select(conn => conn.SourceNodeId)
                    .Distinct()
                    .OrderBy(id => id) // 按节点ID排序，保证确定性
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] [迭代{iterationCount}] 找到 {upstreamNodeIds.Count} 个上游节点ID: [{string.Join(", ", upstreamNodeIds)}]");

                foreach (var upstreamNodeId in upstreamNodeIds)
                {
                    if (visited.Contains(upstreamNodeId))
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] [迭代{iterationCount}]   跳过已访问节点: {upstreamNodeId}");
                        continue;
                    }

                    var upstreamNode = selectedTab.WorkflowNodes.FirstOrDefault(n => n.Id == upstreamNodeId);
                    if (upstreamNode == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] [迭代{iterationCount}]   ⚠ 节点未找到: {upstreamNodeId}");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] [迭代{iterationCount}]   检查上游节点: {upstreamNode.Name}, IsImageCaptureNode={upstreamNode.IsImageCaptureNode}");

                    // 找到图像采集节点，立即返回（第一个找到的）
                    if (upstreamNode.IsImageCaptureNode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] ✓✓✓ 找到图像采集节点: {upstreamNode.Name} (Id={upstreamNode.Id})");
                        return upstreamNode;
                    }

                    // 非采集节点，继续向上追溯
                    visited.Add(upstreamNodeId);
                    queue.Enqueue(upstreamNode);
                    System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] [迭代{iterationCount}]   加入队列继续追溯: {upstreamNode.Name}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[FindUpstreamImageCaptureNode] ✗ BFS遍历完成，未找到图像采集节点，共 {iterationCount} 次迭代");
            return null;
        }

        #endregion

        #endregion // 辅助方法

        /// <summary>
        /// 默认工具插件 - 用于兼容性
        /// </summary>
        private class DefaultToolPlugin : IToolPlugin
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

            public List<ToolMetadata> GetToolMetadata() => new List<ToolMetadata>();

            public SunEyeVision.Core.Interfaces.IImageProcessor CreateToolInstance(string toolId)
            {
                throw new NotImplementedException();
            }

            public SunEyeVision.Core.Models.AlgorithmParameters GetDefaultParameters(string toolId)
            {
                return new SunEyeVision.Core.Models.AlgorithmParameters();
            }

            public ValidationResult ValidateParameters(string toolId, SunEyeVision.Core.Models.AlgorithmParameters parameters)
            {
                return ValidationResult.Success();
            }
        }
    }
}
