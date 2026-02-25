using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AppCommands = SunEyeVision.UI.Commands;
using SunEyeVision.UI.Models;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.UI;
using SunEyeVision.Workflow;
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Factories;
using SunEyeVision.UI.Infrastructure;
using SunEyeVision.UI.Services.Workflow;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Views.Controls.Panels;
using SunEyeVision.UI.Views.Windows;
using SunEyeVision.UI.Extensions;
using SunEyeVision.UI.Converters.Path;
using UIWorkflowNode = SunEyeVision.UI.Models.WorkflowNode;
using WorkflowWorkflowNode = SunEyeVision.Workflow.WorkflowNode;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 图像显示类型枚举
    /// </summary>
    public enum ImageDisplayType
    {
        Original,    // 原始图
        Processed,   // 处理图
        Result       // 结果图
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
    /// 结果项
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
        private string _status = "";
        private string _selectedWorkflowName = "默认工作流";
        private string _currentCanvasTypeText = "原 Diagram (测试)";

        // 图像显示
        private BitmapSource? _displayImage;
        private double _imageScale = 1.0;

        // 图像类型
        private ImageDisplayTypeItem? _selectedImageType;
        private bool _showImagePreview = false;
        private BitmapSource? _originalImage;
        private BitmapSource? _processedImage;
        private BitmapSource? _resultImage;

        // 图像预览
        private bool _autoSwitchEnabled = false;
        private int _currentImageIndex = -1;

        // 所有工作流状态
        private bool _isAllWorkflowsRunning = false;
        private string _allWorkflowsRunButtonText = "运行";

        // 执行管理
        private readonly WorkflowExecutionManager _executionManager;

        // 属性
        private ObservableCollection<Models.PropertyGroup> _propertyGroups = new ObservableCollection<Models.PropertyGroup>();
        private string _logText = "[系统] 等待中...\n";

        // 折叠状态
        private bool _isToolboxCollapsed = true;
        private bool _isImageDisplayCollapsed = false;
        private bool _isPropertyPanelCollapsed = false;
        private double _toolboxWidth = 260;
        private double _rightPanelWidth = 500;
        private double _imageDisplayHeight = 500;

        // 分割器
        private double _splitterPosition = 500; // 默认图像高度
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

        // 注意：已删除全局WorkflowNodes和WorkflowConnections集合
        // 所有节点和连接现在通过 WorkflowTabViewModel.SelectedTab 访问
        // 确保每个Tab都是独立的

        private Models.WorkflowNode? _selectedNode;
        private bool _showPropertyPanel = false;
        private Models.NodeImageData? _activeNodeImageData;
        private string? _currentDisplayNodeId = null;  // 记录当前显示的采集节点ID，避免重复切换

        /// <summary>
        /// 当前活动节点图像数据，用于绑定到图像预览控件
        /// 每个采集节点维护自己的图像集
        /// </summary>
        public Models.NodeImageData? ActiveNodeImageData
        {
            get => _activeNodeImageData;
            private set
            {
                // 引用相等时不触发更新，避免不必要的绑定刷新
                if (ReferenceEquals(_activeNodeImageData, value))
                {
                    return;
                }
                SetProperty(ref _activeNodeImageData, value);
            }
        }

        public Models.WorkflowNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                bool changed = SetProperty(ref _selectedNode, value);
                
                if (changed)
                {
                    // 显示属性面板
                    ShowPropertyPanel = value != null;

                    // 节点选中状态变化时更新图像预览（整合了 ActiveNodeImageData 更新逻辑）
                    UpdateImagePreviewVisibility(value);
                    // 加载节点属性
                    LoadNodeProperties(value);
                }
            }
        }

        /// <summary>
        /// 是否显示属性面板
        /// </summary>
        public bool ShowPropertyPanel
        {
            get => _showPropertyPanel;
            set => SetProperty(ref _showPropertyPanel, value);
        }
        public Models.WorkflowConnection? SelectedConnection { get; set; }
        public WorkflowViewModel WorkflowViewModel { get; set; }
        
        // 工作流标签页
        public WorkflowTabControlViewModel WorkflowTabViewModel { get; }

        /// <summary>
        /// 当前选中的工作流标签页的命令管理器
        /// 每个标签页拥有独立的命令管理器
        /// </summary>
        public AppCommands.CommandManager? CurrentCommandManager
        {
            get => WorkflowTabViewModel.SelectedTab?.CommandManager;
        }

        // 用于跟踪当前已订阅的命令管理器
        private AppCommands.CommandManager? _subscribedCommandManager;

        public string StatusText
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string CameraStatus => "相机(2台)";

        // 图像显示相关
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
        /// 是否显示图像及其预览模块（ImageCaptureTool节点专用）
        /// </summary>
        public bool ShowImagePreview
        {
            get => _showImagePreview;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[ShowImagePreview] Setter: {_showImagePreview} -> {value}");
                if (SetProperty(ref _showImagePreview, value))
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowImagePreview] PropertyChanged已触发, 当前值: {_showImagePreview}");
                    OnPropertyChanged(nameof(ImagePreviewHeight));
                }
            }
        }

        /// <summary>
        /// 图像预览高度，用于动态控制图像预览模块的空间
        /// </summary>
        public GridLength ImagePreviewHeight
        {
            get => ShowImagePreview ? new GridLength(60) : new GridLength(0);
        }

        /// <summary>
        /// 计算结果
        /// </summary>
        public ObservableCollection<ResultItem> CalculationResults { get; }

        // 属性
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

        // 折叠状态
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
        /// 图像显示区域高度，分割器上方
        /// </summary>
        public double SplitterPosition
        {
            get => _splitterPosition;
            private set
            {
                // 确保在合理范围
                value = Math.Max(MinImageAreaHeight, Math.Min(MaxImageAreaHeight, value));
                if (Math.Abs(_splitterPosition - value) > 1) // 忽略微小变化
                {
                    _splitterPosition = value;
                    OnPropertyChanged(nameof(SplitterPosition));

                    // 更新实际高度
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
        /// 保存分割器位置（从控件调用）
        /// </summary>
        public void SaveSplitterPosition(double position)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveSplitterPosition] 位置: {position}");
            SplitterPosition = position;

            // TODO: 选保存到用户设置
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
        /// 所有工作流按钮文本
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
        /// 图像集合，使用优化集合
        /// </summary>
        public BatchObservableCollection<ImageInfo> ImageCollection { get; }

        /// <summary>
        /// 是否自动切换
        /// </summary>
        public bool AutoSwitchEnabled
        {
            get => _autoSwitchEnabled;
            set => SetProperty(ref _autoSwitchEnabled, value);
        }

        /// <summary>
        /// 当前显示图像索引
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
            // 将日志追加到末尾
            LogText += $"[{timestamp}] {message}\n";

            // 日志条目最多保存100条
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

        // 所有工作流
        public ICommand RunAllWorkflowsCommand { get; }
        public ICommand ToggleContinuousAllCommand { get; }

        // 图像视图
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand FitToWindowCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }

        // 图像操作
        public ICommand BrowseImageCommand { get; }
        public ICommand LoadImageCommand { get; }
        public ICommand ClearImageCommand { get; }

        public MainWindowViewModel()
        {
            Workflows = new ObservableCollection<string>
            {
                "默认工作流",
                "测试工作流",
                "项目工作流",
                "示例工作流"
            };

            Tools = new ObservableCollection<Models.ToolItem>();
            Toolbox = new ToolboxViewModel();
            // 删除全局 WorkflowNodes 和 WorkflowConnections 的初始化

            WorkflowViewModel = new WorkflowViewModel();
            WorkflowTabViewModel = new WorkflowTabControlViewModel();

            // 初始化图像显示类型
            ImageDisplayTypes = new ObservableCollection<ImageDisplayTypeItem>
            {
                new ImageDisplayTypeItem { Type = ImageDisplayType.Original, DisplayName = "原始图", Icon = "📷" },
                new ImageDisplayTypeItem { Type = ImageDisplayType.Processed, DisplayName = "处理图", Icon = "🔧" },
                new ImageDisplayTypeItem { Type = ImageDisplayType.Result, DisplayName = "结果图", Icon = "✅" }
            };
            SelectedImageType = ImageDisplayTypes.FirstOrDefault();

            // 初始化计算结果
            CalculationResults = new ObservableCollection<ResultItem>();

            // 初始化图像集合（优化版）
            ImageCollection = new BatchObservableCollection<ImageInfo>();

            // 初始化执行管理器
            _executionManager = new Services.Workflow.WorkflowExecutionManager(new Infrastructure.DefaultInputProvider());

            // 订阅执行管理器事件
            _executionManager.WorkflowExecutionStarted += OnWorkflowExecutionStarted;
            _executionManager.WorkflowExecutionCompleted += OnWorkflowExecutionCompleted;
            _executionManager.WorkflowExecutionStopped += OnWorkflowExecutionStopped;
            _executionManager.WorkflowExecutionError += OnWorkflowExecutionError;
            _executionManager.WorkflowExecutionProgress += OnWorkflowExecutionProgress;

            // 初始化当前画布类型
            UpdateCurrentCanvasType();

            // 选中标签页变化时更新运行/停止按钮状态
            WorkflowTabViewModel.SelectionChanged += OnSelectedTabChanged;

            // 订阅工作流状态变化
            WorkflowTabViewModel.WorkflowStatusChanged += OnWorkflowStatusChanged;

            // 命令管理器的初始化
            SubscribeToCurrentCommandManager();

            InitializeTools();
            // InitializeSampleNodes(); // 已禁用，暂时不加载测试节点
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

            // 所有工作流
            RunAllWorkflowsCommand = new RelayCommand(async () => await ExecuteRunAllWorkflows(), () => !IsAllWorkflowsRunning);
            ToggleContinuousAllCommand = new RelayCommand(ExecuteToggleContinuousAll, () => true);

            // 图像视图
            ZoomInCommand = new RelayCommand(ExecuteZoomIn);
            ZoomOutCommand = new RelayCommand(ExecuteZoomOut);
            FitToWindowCommand = new RelayCommand(ExecuteFitToWindow);
            ResetViewCommand = new RelayCommand(ExecuteResetView);
            ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreen);

            // 图像操作
            BrowseImageCommand = new RelayCommand(ExecuteBrowseImage);
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClearImageCommand = new RelayCommand(ExecuteClearImage);
        }

        /// <summary>
        /// 选中的标签页变化
        /// </summary>
        private void OnSelectedTabChanged(object? sender, EventArgs e)
        {
            // 更新命令管理器
            SubscribeToCurrentCommandManager();

            // 更新撤销/重做按钮状态
            UpdateUndoRedoCommands();

            // 更新当前显示
            UpdateCurrentCanvasType();

            // 更新 SmartPathConverter 的节点集合
            if (WorkflowTabViewModel?.SelectedTab != null)
            {
                Converters.Path.SmartPathConverter.Nodes = WorkflowTabViewModel.SelectedTab.WorkflowNodes;
                Converters.Path.SmartPathConverter.Connections = WorkflowTabViewModel.SelectedTab.WorkflowConnections;
            }
        }

        /// <summary>
        /// 工作流状态变化
        /// </summary>
        private void OnWorkflowStatusChanged(object? sender, EventArgs e)
        {
            // 所有工作流状态
            IsAllWorkflowsRunning = WorkflowTabViewModel.IsAnyWorkflowRunning;
            AllWorkflowsRunButtonText = IsAllWorkflowsRunning ? "停止" : "运行";

            // 更新CanExecute状态
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
                Views.Controls.Canvas.CanvasType.WorkflowCanvas => "工作流画布",
                Views.Controls.Canvas.CanvasType.NativeDiagram => "原生 Diagram (测试)",
                _ => "未知类型"
            };
            }
            else
            {
                CurrentCanvasTypeText = "无画布";
            }
        }

        /// <summary>
        /// 订阅当前命令管理器的状态变化
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
        /// 更新撤销/重做CanExecute状态
        /// </summary>
        private void UpdateUndoRedoCommands()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var undoCmd = UndoCommand as RelayCommand;
                var redoCmd = RedoCommand as RelayCommand;
                undoCmd?.RaiseCanExecuteChanged();
                redoCmd?.RaiseCanExecuteChanged();

                // 更新状态显示
                StatusText = CurrentCommandManager?.LastCommandDescription ?? "";
            });
        }

        /// <summary>
        /// 当前命令管理器状态变化
        /// </summary>
        private void OnCurrentCommandManagerStateChanged(object? sender, EventArgs e)
        {
            UpdateUndoRedoCommands();
        }

        /// <summary>
        /// 判断是否可以撤销当前选中的工作流
        /// </summary>
        private bool CanExecuteUndo()
        {
            return CurrentCommandManager?.CanUndo ?? false;
        }

        /// <summary>
        /// 判断是否可以重做当前选中的工作流
        /// </summary>
        private bool CanExecuteRedo()
        {
            return CurrentCommandManager?.CanRedo ?? false;
        }

        private void ExecutePause()
        {
            // TODO: 实现暂停
        }

        private void ExecuteUndo()
        {
            if (CurrentCommandManager == null)
            {
                AddLog("❌ 没有选中的工作流，无法撤销");
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
                AddLog("❌ 没有选中的工作流，无法重做");
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
            Tools.Add(new Models.ToolItem("图像采集", "ImageAcquisition", "📷", "从文件或相机获取图像"));
            Tools.Add(new Models.ToolItem("灰度化", "GrayScale", "🎨", "彩色图转换为灰度图"));
            Tools.Add(new Models.ToolItem("高斯模糊", "GaussianBlur", "🌫️", "应用高斯模糊滤镜"));
            Tools.Add(new Models.ToolItem("阈值化", "Threshold", "🔲", "图像转换为二值图像"));
            Tools.Add(new Models.ToolItem("边缘检测", "EdgeDetection", "🔍", "检测图像中的边缘"));
            Tools.Add(new Models.ToolItem("形态学", "Morphology", "📐", "腐蚀和膨胀等形态学运算"));
        }

        private void InitializePropertyGroups()
        {
            // 初始化日志
            AddLog("✅ [系统] 系统启动成功");
            AddLog("✅ [设备] 相机1连接成功");
            AddLog("✅ [设备] 相机2连接成功");
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
            // TODO: 新建工作流
        }

        private void ExecuteOpenWorkflow()
        {
            // TODO: 打开工作流
        }

        private void ExecuteSaveWorkflow()
        {
            // TODO: 保存工作流文件
        }

        private void ExecuteSaveAsWorkflow()
        {
            // TODO: 另存为文件
        }

        private async System.Threading.Tasks.Task ExecuteRunWorkflow()
        {
            AddLog("=== 开始执行工作流 ===");

            if (WorkflowTabViewModel == null)
            {
                AddLog("❌ WorkflowTabViewModel 为null");
                return;
            }

            if (WorkflowTabViewModel.SelectedTab == null)
            {
                AddLog("❌ 没有选中的工作流标签页");
                AddLog("💡 请确保一个标签页被选中");
                return;
            }

            AddLog($"📋 当前工作流: {WorkflowTabViewModel.SelectedTab.Name}");
            AddLog($"📊 节点数量: {WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count}");
            AddLog($"🔗 连接数量: {WorkflowTabViewModel.SelectedTab.WorkflowConnections.Count}");

            if (WorkflowTabViewModel.SelectedTab.WorkflowNodes.Count == 0)
            {
                AddLog("⚠️ 当前工作流没有节点");
                AddLog("💡 请从工具箱拖拽算法节点到画布");
                AddLog("📝 可选节点：图像采集、灰度化、高斯模糊、阈值化、边缘检测、形态学");
                return;
            }

            IsRunning = true;
            AddLog("🚀 开始执行工作流...");

            try
            {
                await _executionManager.RunSingleAsync(WorkflowTabViewModel.SelectedTab);
                AddLog("✅ 执行完成");
            }
            catch (Exception ex)
            {
                AddLog($"❌ 执行失败: {ex.Message}");
                AddLog($"❌ 异常: {ex.StackTrace}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ExecuteStopWorkflow()
        {
            if (WorkflowTabViewModel.SelectedTab == null)
            {
                AddLog("⚠️ 没有选中的工作流标签页");
                return;
            }

            _executionManager.StopContinuousRun(WorkflowTabViewModel.SelectedTab);
            IsRunning = false;
            AddLog("⏹️ 工作流已停止");
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
        /// 加载节点属性
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

            // 参数
            var paramGroup = new Models.PropertyGroup
            {
                Name = "⚙️ 参数",
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
                    new Models.PropertyItem { Label = "平均时间", Value = "0 ms" },
                }
            };
        }

        /// <summary>
        /// 添加节点到当前工作流
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
        /// 删除选中的节点（通过命令模式）
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
        /// 清除所有节点选中状态
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
                $"确定要删除选中的 {selectedCount} 个节点吗？",
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

                AddLog($"🗑️ 已删除 {selectedCount} 个节点");
            }
        }

        private void ExecuteOpenDebugWindow(Models.WorkflowNode? node)
        {
            if (node != null)
            {
                try
                {
                    // 从 ToolRegistry 获取元数据和插件
                    var toolId = node.AlgorithmType ?? node.Name;
                    var toolMetadata = ToolRegistry.GetToolMetadata(toolId);
                    var toolPlugin = ToolRegistry.GetToolPlugin(toolId);

                    if (toolMetadata == null)
                    {
                        System.Windows.MessageBox.Show(
                            $"未找到工具 '{toolId}' 的元数据信息",
                            "未找到工具",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    // 使用 NodeInterfaceFactory 获取界面类型
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
                            // 创建新的工作流标签页（子程序节点专用）
                            CreateSubroutineWorkflowTab(node);
                            break;

                        case NodeInterfaceType.SubroutineEditor:
                            // 子程序编辑器（配置界面）
                            AddLog($"编辑界面: {node.Name}");
                            // TODO: 实现节点编辑
                            System.Windows.MessageBox.Show(
                                "节点编辑功能待实现",
                                "提示",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                            break;

                        case NodeInterfaceType.None:
                        default:
                            // 不显示任何界面
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"打开节点失败: {ex.Message}",
                        "错误",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    AddLog($"打开节点失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 为子程序节点创建新的标签页
        /// </summary>
        /// <param name="subroutineNode">子程序节点</param>
        private void CreateSubroutineWorkflowTab(Models.WorkflowNode subroutineNode)
        {
            try
            {
                if (WorkflowTabViewModel == null)
                {
                    AddLog("❌ WorkflowTabViewModel 为null");
                    return;
                }

                // 使用子程序节点名称作为工作流名称
                string workflowName = subroutineNode.Name;
                if (string.IsNullOrWhiteSpace(workflowName))
                {
                    workflowName = "子程序";
                }

                AddLog($"📝 创建子程序标签页: {workflowName}");

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

                AddLog($"✅ 创建子程序 '{workflowName}' 成功");
                AddLog($"💡 现在可以在画布中添加子程序内部节点逻辑");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"创建子程序失败: {ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                AddLog($"❌ 创建子程序失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 切换显示包围矩形
        /// </summary>
        private void ExecuteToggleBoundingRectangle()
        {
            AddLog("[ToggleBoundingRectangle] ========== 切换显示包围矩形 ==========");

            try
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as Views.Windows.MainWindow;
                if (mainWindow == null)
                {
                    AddLog("[ToggleBoundingRectangle] ❌ MainWindow为null");
                    return;
                }

                AddLog("[ToggleBoundingRectangle] 获取 MainWindow 成功");

                // 使用 MainWindow 获取当前 WorkflowCanvasControl
                var workflowCanvas = mainWindow.GetCurrentWorkflowCanvas();
                if (workflowCanvas == null)
                {
                    AddLog("[ToggleBoundingRectangle] 无法获取 WorkflowCanvasControl");
                    return;
                }

                AddLog("[ToggleBoundingRectangle] 获取 WorkflowCanvasControl 成功");

                ToggleBoundingRectangleOnCanvas(workflowCanvas);
            }
            catch (Exception ex)
            {
                AddLog($"[ToggleBoundingRectangle] 错误: {ex.Message}");
                AddLog($"[ToggleBoundingRectangle] 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 在指定的 WorkflowCanvasControl 中切换显示包围矩形
        /// </summary>
        private void ToggleBoundingRectangleOnCanvas(WorkflowCanvasControl workflowCanvas)
        {
            workflowCanvas.ShowBoundingRectangle = !workflowCanvas.ShowBoundingRectangle;

            // 如果显示，使用第一个连接作为示例
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
                        AddLog($"[ToggleBoundingRectangle] 显示 {firstConnection.Id} 的包围矩形");
                        AddLog($"[ToggleBoundingRectangle]   源节点ID: {firstConnection.SourceNodeId}");
                        AddLog($"[ToggleBoundingRectangle]   目标节点ID: {firstConnection.TargetNodeId}");
                    }
                    else
                    {
                        AddLog("[ToggleBoundingRectangle] 未找到连接");
                        workflowCanvas.ShowBoundingRectangle = false;
                    }
                }
                else
                {
                    AddLog("[ToggleBoundingRectangle] 当前Tab没有连接");
                    workflowCanvas.ShowBoundingRectangle = false;
                }
            }

            AddLog($"[ToggleBoundingRectangle] ========== 包围矩形: {(workflowCanvas.ShowBoundingRectangle ? "显示" : "隐藏")} ==========");
        }

        /// <summary>
        /// 切换路径起点终点显示
        /// </summary>
        private void ExecuteTogglePathPoints()
        {
            AddLog("[TogglePathPoints] 切换显示路径起点终点");

            if (WorkflowTabViewModel?.SelectedTab?.WorkflowConnections != null)
            {
                var newState = !WorkflowTabViewModel.SelectedTab.WorkflowConnections.Any(c => c.ShowPathPoints);

                foreach (var connection in WorkflowTabViewModel.SelectedTab.WorkflowConnections)
                {
                    connection.ShowPathPoints = newState;
                }

                AddLog($"[TogglePathPoints] 显示路径起点终点: {(newState ? "显示" : "隐藏")}");
            }
        }

        /// <summary>
        /// 执行所有工作流
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteRunAllWorkflows()
        {
            AddLog("🚀 开始执行所有工作流...");
            await WorkflowTabViewModel.RunAllWorkflowsAsync();
            AddLog("✅ 完成所有工作流");
        }

        /// <summary>
        /// 切换所有工作流运行/停止
        /// </summary>
        private void ExecuteToggleContinuousAll()
        {
            if (IsAllWorkflowsRunning)
            {
                AddLog("⏹️ 停止所有工作流");
                WorkflowTabViewModel.StopAllWorkflows();
            }
            else
            {
                AddLog("▶️ 开始所有工作流");
                WorkflowTabViewModel.StartAllWorkflows();
            }
        }

        /// <summary>
        /// 执行开始事件
        /// </summary>
        private void OnWorkflowExecutionStarted(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"▶️ 开始执行: {e.WorkflowId}");
        }

        /// <summary>
        /// 执行完成事件
        /// </summary>
        private void OnWorkflowExecutionCompleted(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"✅ 执行完成: {e.WorkflowId}");
        }

        /// <summary>
        /// 执行停止事件
        /// </summary>
        private void OnWorkflowExecutionStopped(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"⏹️ 执行已停止: {e.WorkflowId}");
        }

        /// <summary>
        /// 执行错误事件
        /// </summary>
        private void OnWorkflowExecutionError(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"❌ 执行错误: {e.WorkflowId} - {e.ErrorMessage}");
        }

        /// <summary>
        /// 执行进度事件
        /// </summary>
        private void OnWorkflowExecutionProgress(object? sender, WorkflowExecutionProgressEventArgs e)
        {
            try
            {
                AddLog(e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindowViewModel] OnWorkflowExecutionProgress异常: {ex.Message}");
                AddLog($"⚠️ 日志记录异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找指定类型的视觉子元素
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

        #region 图像视图

        /// <summary>
        /// 放大图像
        /// </summary>
        private void ExecuteZoomIn()
        {
            ImageScale = Math.Min(ImageScale * 1.2, 5.0);
            AddLog($"🔍 图像放大: {ImageScale:P0}");
        }

        /// <summary>
        /// 缩小图像
        /// </summary>
        private void ExecuteZoomOut()
        {
            ImageScale = Math.Max(ImageScale / 1.2, 0.1);
            AddLog($"🔍 图像缩小: {ImageScale:P0}");
        }

        /// <summary>
        /// 适应窗口
        /// </summary>
        private void ExecuteFitToWindow()
        {
            // TODO: 根据窗口大小计算缩放
            ImageScale = 1.0;
            AddLog($"🔍 适应窗口: {ImageScale:P0}");
        }

        /// <summary>
        /// 重置视图
        /// </summary>
        private void ExecuteResetView()
        {
            ImageScale = 1.0;
            AddLog($"🔄 重置视图: {ImageScale:P0}");
        }

        /// <summary>
        /// 切换全屏显示
        /// </summary>
        private void ExecuteToggleFullScreen()
        {
            // TODO: 实现图像全屏显示
            AddLog("🖥️ 切换全屏显示");
        }

        #endregion

        #region 图像操作

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
                    AddLog($"📁 选择文件: {filePath}");

                    // TODO: 加载图像到OriginalImage
                    // OriginalImage = LoadImageFromFile(filePath);
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 加载图像失败: {ex.Message}");
                System.Windows.MessageBox.Show($"加载图像失败: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载图像
        /// </summary>
        private void ExecuteLoadImage()
        {
            try
            {
                if (OriginalImage == null)
                {
                    AddLog("❌ 请先选择图像文件");
                    return;
                }

                AddLog("✅ 加载图像成功");
                // TODO: 处理图像并更新ProcessedImage和ResultImage
            }
            catch (Exception ex)
            {
                AddLog($"❌ 加载图像失败: {ex.Message}");
                System.Windows.MessageBox.Show($"加载图像失败: {ex.Message}", "错误",
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
                AddLog("已清除图像");
            }
            catch (Exception ex)
            {
                AddLog($"清除图像失败: {ex.Message}");
            }
        }

        #endregion

        #region 图像预览

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
            
            // 获取FullImage
            var fullImage = imageInfo.FullImage;
            
            if (fullImage != null)
            {
                OriginalImage = fullImage;
                AddLog($"📷 显示图像: {imageInfo.Name}");
                
                // 确保DisplayImage更新
                UpdateDisplayImage();
            }
        }

        #endregion

        #region 显示

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

            AddLog($"添加了 {results.Count} 条结果记录");
        }

        #region 图像预览

        /// <summary>
        /// 图像预览显示状态管理
        /// </summary>
        /// <remarks>
        /// 规则:
        /// 1. 选择图像采集节点 -> 显示该节点的图像
        /// 2. 选择其他节点 -> BFS逆向追踪采集节点，找到可显示图像
        ///    (优化: 逆向追踪采集节点与当前显示同源则不更新)
        /// 3. 逆向追踪采集节点图像 -> 显示在图像预览中
        /// 
        /// 性能优化:
        /// - 只在状态真正变化时才更新属性，避免触发不必要的绑定刷新
        /// - 引用相等检测在 ActiveNodeImageData setter 中处理
        /// </remarks>
        public void UpdateImagePreviewVisibility(Models.WorkflowNode? selectedNode)
        {
            // 1.没有选中节点时
            if (selectedNode == null)
            {
                // 只在当前状态不一致时才更新
                if (ShowImagePreview || ActiveNodeImageData != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveNodeImageData = null;
                    _currentDisplayNodeId = null;
                }
                return;
            }

            // 2.选中图像采集节点时，初始化显示图像预览（即使暂时没有图像）
            if (selectedNode.IsImageCaptureNode)
            {
                // 确保节点图像已延迟初始化
                selectedNode.ImageData ??= new Models.NodeImageData(selectedNode.Id);
                
                // 检查是否是同一节点
                bool isSameNode = _currentDisplayNodeId == selectedNode.Id;
                
                if (!isSameNode)
                {
                    // 不同节点才更新
                    _currentDisplayNodeId = selectedNode.Id;
                    selectedNode.ImageData.PrepareForDisplay();
                    ActiveNodeImageData = selectedNode.ImageData;
                }
                
                // 只有真正变化时才设置
                if (!ShowImagePreview)
                {
                    ShowImagePreview = true;
                }
                return;
            }

            // 3.选中非图像采集节点时，BFS逆向追踪采集节点
            // 快速检查：没有连接的孤立节点，直接隐藏
            var connections = WorkflowTabViewModel?.SelectedTab?.WorkflowConnections;
            if (connections == null || connections.Count == 0)
            {
                if (ShowImagePreview || ActiveNodeImageData != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveNodeImageData = null;
                    _currentDisplayNodeId = null;
                }
                return;
            }
            
            var sourceCaptureNode = FindUpstreamImageCaptureNode(selectedNode);

            if (sourceCaptureNode != null && sourceCaptureNode.ImageData != null && sourceCaptureNode.ImageData.ImageCount > 0)
            {
                bool isSameNode = _currentDisplayNodeId == sourceCaptureNode.Id;
                
                if (!isSameNode)
                {
                    _currentDisplayNodeId = sourceCaptureNode.Id;
                    ActiveNodeImageData = sourceCaptureNode.ImageData;
                }
                
                if (!ShowImagePreview)
                {
                    ShowImagePreview = true;
                }
            }
            else
            {
                // 逆向追踪失败或没有图像时隐藏
                if (ShowImagePreview || ActiveNodeImageData != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveNodeImageData = null;
                    _currentDisplayNodeId = null;
                }
            }
        }

        /// <summary>
        /// 强制刷新图像预览（从尺寸变化等触发）
        /// 即使当前 SelectedNode 未改变，也重新检查是否显示图像预览
        /// </summary>
        public void ForceRefreshImagePreview()
        {
            UpdateImagePreviewVisibility(_selectedNode);
        }

        /// <summary>
        /// 逆向追踪选中节点的图像采集节点（BFS）
        /// </summary>
        /// <remarks>
        /// 当存在多个采集节点时返回第一个找到的采集节点:
        /// 1. BFS保证路径最短
        /// 2. 节点ID排序保证选择一致
        /// </remarks>
        /// <param name="node">起始节点</param>
        /// <returns>第一个找到的图像采集节点，未找到返回null</returns>
        private Models.WorkflowNode? FindUpstreamImageCaptureNode(Models.WorkflowNode node)
        {
            var selectedTab = WorkflowTabViewModel?.SelectedTab;
            if (selectedTab == null || selectedTab.WorkflowConnections == null || selectedTab.WorkflowNodes == null)
            {
                return null;
            }

            var visited = new HashSet<string>();
            var queue = new Queue<Models.WorkflowNode>();
            queue.Enqueue(node);
            visited.Add(node.Id);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                // 获取上游节点ID，并在集合中排序后按节点ID验证正确性
                var upstreamNodeIds = selectedTab.WorkflowConnections
                    .Where(conn => conn.TargetNodeId == currentNode.Id)
                    .Select(conn => conn.SourceNodeId)
                    .Distinct()
                    .OrderBy(id => id) // 按节点ID排序，保证确定性
                    .ToList();

                foreach (var upstreamNodeId in upstreamNodeIds)
                {
                    if (visited.Contains(upstreamNodeId))
                    {
                        continue;
                    }

                    var upstreamNode = selectedTab.WorkflowNodes.FirstOrDefault(n => n.Id == upstreamNodeId);
                    if (upstreamNode == null)
                    {
                        continue;
                    }

                    // 找到图像采集节点，返回（第一个找到的）
                    if (upstreamNode.IsImageCaptureNode)
                    {
                        return upstreamNode;
                    }

                    // 不是采集节点，继续追踪
                    visited.Add(upstreamNodeId);
                    queue.Enqueue(upstreamNode);
                }
            }

            return null;
        }

        #endregion

        #endregion // 显示

        /// <summary>
        /// 默认工具插件 - 用于测试
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

            public SunEyeVision.Plugin.SDK.Core.IImageProcessor? CreateToolInstance(string toolId)
            {
                return null;
            }

            public AlgorithmParameters GetDefaultParameters(string toolId)
            {
                return new AlgorithmParameters();
            }
        }
    }
}
