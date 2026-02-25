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
    /// ͼʾö
    /// </summary>
    public enum ImageDisplayType
    {
        Original,    // ԭʼͼ
        Processed,   // ͼ?
        Result       // ͼ
    }

    /// <summary>
    /// ͼʾ?
    /// </summary>
    public class ImageDisplayTypeItem
    {
        public ImageDisplayType Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    /// <summary>
    /// 规则:
    /// </summary>
    public class ResultItem
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// ͼģ?
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private string _title = "太阳眼视觉";
        private bool _isRunning = false;
        private string _status = "";
        private string _selectedWorkflowName = "默认工作流";
        private string _currentCanvasTypeText = "ԭ Diagram (?";

        // ͼʾ
        private BitmapSource? _displayImage;
        private double _imageScale = 1.0;

        // ͼ
        private ImageDisplayTypeItem? _selectedImageType;
        private bool _showImagePreview = false;
        private BitmapSource? _originalImage;
        private BitmapSource? _processedImage;
        private BitmapSource? _resultImage;

        // ͼԤ
        private bool _autoSwitchEnabled = false;
        private int _currentImageIndex = -1;

        // й״?
        private bool _isAllWorkflowsRunning = false;
        private string _allWorkflowsRunButtonText = "";

        // ִй
        private readonly WorkflowExecutionManager _executionManager;

        // ?
        private ObservableCollection<Models.PropertyGroup> _propertyGroups = new ObservableCollection<Models.PropertyGroup>();
        private string _logText = "[ϵͳ] ȴ...\n";

        // ۵״?
        private bool _isToolboxCollapsed = true;
        private bool _isImageDisplayCollapsed = false;
        private bool _isPropertyPanelCollapsed = false;
        private double _toolboxWidth = 260;
        private double _rightPanelWidth = 500;
        private double _imageDisplayHeight = 500;

        // ָ?
        private double _splitterPosition = 500; // Ĭͼ߶
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
        /// ǰʾı
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

        // ע⣺ɾȫ?WorkflowNodes ?WorkflowConnections ?
        // нڵӶӦͨ WorkflowTabViewModel.SelectedTab 
        // ȷÿ?Tab Ƕ?

        private Models.WorkflowNode? _selectedNode;
        private bool _showPropertyPanel = false;
        private Models.NodeImageData? _activeNodeImageData;
        private string? _currentDisplayNodeId = null;  // ?ٵǰʾĲɼڵIDڱظ?

        /// <summary>
        /// ǰڵͼݣڰ󶨵ͼԤؼ
        /// ÿɼڵάͼ?
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
                return _selectedNode;
            }
            set
            {
                bool changed = SetProperty(ref _selectedNode, value);
                
                if (changed)
                {
                    // ɼ?
                    ShowPropertyPanel = value != null;

                    // »ڵͼݣģлڵʱлͼ񼯺?
                    UpdateActiveNodeImageData(value);

                    // ڵѡ״̬仯ʱͼԤ?
                    UpdateImagePreviewVisibility(value);
                    // ؽڵԵ?
                    LoadNodeProperties(value);
                }
            }
        }

        /// <summary>
        /// »ڵͼ?
        /// ʵֲͬɼڵӵжͼԤ
        /// 规则:Żظͬڵ?
        /// </summary>
        private void UpdateActiveNodeImageData(Models.WorkflowNode? node)
        {
            // 1лͼɼڵ
            if (node?.IsImageCaptureNode == true)
            {
                // ȷڵͼӳٳʼ
                node.ImageData ??= new Models.NodeImageData(node.Id);
                
                // ?ؼŻǷлͬĽڵҵǰ
                bool isSameNode = _currentDisplayNodeId == node.Id;
                bool hasActiveData = ActiveNodeImageData != null;
                
                if (isSameNode && hasActiveData)
                {
                    // ͬڵҵǰ
                    // ?ȻҪ?ActiveNodeImageData ȷ󶨴?
                    // ΪܴӷǲɼڵлActiveNodeImageData Ϊ null?
                    ActiveNodeImageData = node.ImageData;
                    return;
                }
                
                // ?ͬڵ֮ǰգ¸IDͼ
                _currentDisplayNodeId = node.Id;
                int imageCount = node.ImageData.PrepareForDisplay();  // ?
                
                ActiveNodeImageData = node.ImageData;
            }
            // 2лͼɼ?
            // ?κβ UpdateImagePreviewVisibility ͳһ
            // Ա _currentDisplayNodeId ?ActiveNodeImageData ?
            // лͬβɼڵķǲɼڵʱᴥ¼
            // ԭ⣺֮ǰ?_currentDisplayNodeId?UpdateImagePreviewVisibility
            // е isSameNode жʧЧÿл¼?
        }

        /// <summary>
        /// ʾ?
        /// </summary>
        public bool ShowPropertyPanel
        {
            get => _showPropertyPanel;
            set => SetProperty(ref _showPropertyPanel, value);
        }
        public Models.WorkflowConnection? SelectedConnection { get; set; }
        public WorkflowViewModel WorkflowViewModel { get; set; }
        
        // ̹?
        public WorkflowTabControlViewModel WorkflowTabViewModel { get; }

        /// <summary>
        /// ǰѡлڳ/?
        /// ÿжĳ/?
        /// </summary>
        public AppCommands.CommandManager? CurrentCommandManager
        {
            get => WorkflowTabViewModel.SelectedTab?.CommandManager;
        }

        // ڸٵǰĵظ?
        private AppCommands.CommandManager? _subscribedCommandManager;

        public string StatusText
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string CameraStatus => "?(2?";

        // ͼʾ?
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
        /// ͼʾͼ
        /// </summary>
        public ObservableCollection<ImageDisplayTypeItem> ImageDisplayTypes { get; }

        /// <summary>
        /// ǰѡеͼʾ?
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
        /// ʾͼ뼰Ԥģ飨ImageCaptureToolڵʾ?
        /// </summary>
        public bool ShowImagePreview
        {
            get => _showImagePreview;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[ShowImagePreview] Setter? {_showImagePreview} -> {value}");
                if (SetProperty(ref _showImagePreview, value))
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowImagePreview] PropertyChangedѴ? ǰ? {_showImagePreview}");
                    OnPropertyChanged(nameof(ImagePreviewHeight));
                }
            }
        }

        /// <summary>
        /// ͼԤ߶ȣڶ̬ͼԤģĿռ?
        /// </summary>
        public GridLength ImagePreviewHeight
        {
            get => ShowImagePreview ? new GridLength(60) : new GridLength(0);
        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<ResultItem> CalculationResults { get; }

        // ?
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

        // ۵״̬?
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
        /// ͼʾ߶ȣָϷ?
        /// </summary>
        public double SplitterPosition
        {
            get => _splitterPosition;
            private set
            {
                // ȷںΧ
                value = Math.Max(MinImageAreaHeight, Math.Min(MaxImageAreaHeight, value));
                if (Math.Abs(_splitterPosition - value) > 1) // ΢С
                {
                    _splitterPosition = value;
                    OnPropertyChanged(nameof(SplitterPosition));

                    // ʵʸ?
                    double availableHeight = _splitterPosition;
                    double propertyHeight = Math.Max(200, Math.Min(600, 900 - availableHeight));
                    PropertyPanelActualHeight = propertyHeight;
                }
            }
        }

        /// <summary>
        /// ʵʸ?
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
        /// ָλãӴ̨ã
        /// </summary>
        public void SaveSplitterPosition(double position)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveSplitterPosition] λ: {position}");
            SplitterPosition = position;

            // ѡ浽û?
            // Settings.Default.SplitterPosition = position;
            // Settings.Default.Save();
        }

        /// <summary>
        /// йǷ
        /// </summary>
        public bool IsAllWorkflowsRunning
        {
            get => _isAllWorkflowsRunning;
            set => SetProperty(ref _isAllWorkflowsRunning, value);
        }

        /// <summary>
        /// йаťı
        /// </summary>
        public string AllWorkflowsRunButtonText
        {
            get => _allWorkflowsRunButtonText;
            set => SetProperty(ref _allWorkflowsRunButtonText, value);
        }

        /// <summary>
        /// ԭʼͼ
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
        /// ͼ?
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
        /// ͼ
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
        /// ͼ񼯺ϣʹŻϣ
        /// </summary>
        public BatchObservableCollection<ImageInfo> ImageCollection { get; }

        /// <summary>
        /// ǷԶл
        /// </summary>
        public bool AutoSwitchEnabled
        {
            get => _autoSwitchEnabled;
            set => SetProperty(ref _autoSwitchEnabled, value);
        }

        /// <summary>
        /// ǰʾͼ?
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
        /// ־
        /// </summary>
        public void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            // ־׷ӵĩβ
            LogText += $"[{timestamp}] {message}\n";

            // ־Ŀౣ?00?
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

        // й
        public ICommand RunAllWorkflowsCommand { get; }
        public ICommand ToggleContinuousAllCommand { get; }

        // ͼ
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand FitToWindowCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }

        // ͼ
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
            // ɾȫ WorkflowNodes ?WorkflowConnections ĳʼ

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

            // й
            RunAllWorkflowsCommand = new RelayCommand(async () => await ExecuteRunAllWorkflows(), () => !IsAllWorkflowsRunning);
            ToggleContinuousAllCommand = new RelayCommand(ExecuteToggleContinuousAll, () => true);

            // ͼ
            ZoomInCommand = new RelayCommand(ExecuteZoomIn);
            ZoomOutCommand = new RelayCommand(ExecuteZoomOut);
            FitToWindowCommand = new RelayCommand(ExecuteFitToWindow);
            ResetViewCommand = new RelayCommand(ExecuteResetView);
            ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreen);

            // ͼ
            BrowseImageCommand = new RelayCommand(ExecuteBrowseImage);
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClearImageCommand = new RelayCommand(ExecuteClearImage);
        }

        /// <summary>
        /// ѡл仯
        /// </summary>
        private void OnSelectedTabChanged(object? sender, EventArgs e)
        {
            // »?
            SubscribeToCurrentCommandManager();

            // ³/ť״?
            UpdateUndoRedoCommands();

            // µǰʾ
            UpdateCurrentCanvasType();

            //  SmartPathConverter ĽڵӼ
            if (WorkflowTabViewModel?.SelectedTab != null)
            {
                Converters.Path.SmartPathConverter.Nodes = WorkflowTabViewModel.SelectedTab.WorkflowNodes;
                Converters.Path.SmartPathConverter.Connections = WorkflowTabViewModel.SelectedTab.WorkflowConnections;
            }
        }

        /// <summary>
        /// ״̬仯?
        /// </summary>
        private void OnWorkflowStatusChanged(object? sender, EventArgs e)
        {
            // й״?
            IsAllWorkflowsRunning = WorkflowTabViewModel.IsAnyWorkflowRunning;
            AllWorkflowsRunButtonText = IsAllWorkflowsRunning ? "ֹͣ" : "";

            // CanExecute״?
            (RunAllWorkflowsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ToggleContinuousAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// µǰʾ
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
                _ => "δ֪"
            };
            }
            else
            {
                CurrentCanvasTypeText = "无画布";
            }
        }

        /// <summary>
        /// ĵǰ״̬?
        /// </summary>
        private void SubscribeToCurrentCommandManager()
        {
            // ȡľɵ?
            if (_subscribedCommandManager != null)
            {
                _subscribedCommandManager.CommandStateChanged -= OnCurrentCommandManagerStateChanged;
            }

            // µ?
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
        /// ³/CanExecute״?
        /// </summary>
        private void UpdateUndoRedoCommands()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var undoCmd = UndoCommand as RelayCommand;
                var redoCmd = RedoCommand as RelayCommand;
                undoCmd?.RaiseCanExecuteChanged();
                redoCmd?.RaiseCanExecuteChanged();

                // ״̬ʾ
                StatusText = CurrentCommandManager?.LastCommandDescription ?? "";
            });
        }

        /// <summary>
        /// ǰ״̬仯?
        /// </summary>
        private void OnCurrentCommandManagerStateChanged(object? sender, EventArgs e)
        {
            UpdateUndoRedoCommands();
        }

        /// <summary>
        /// жǷԳڵǰѡл?
        /// </summary>
        private bool CanExecuteUndo()
        {
            return CurrentCommandManager?.CanUndo ?? false;
        }

        /// <summary>
        /// жǷڵǰѡл?
        /// </summary>
        private bool CanExecuteRedo()
        {
            return CurrentCommandManager?.CanRedo ?? false;
        }

        private void ExecutePause()
        {
            // TODO: ʵͣ
        }

        private void ExecuteUndo()
        {
            if (CurrentCommandManager == null)
            {
                AddLog("?? ûѡеĻ޷");
                return;
            }

            try
            {
                CurrentCommandManager.Undo();
                AddLog($"?? : {CurrentCommandManager.LastCommandDescription}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ʧ: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRedo()
        {
            if (CurrentCommandManager == null)
            {
                AddLog("?? ûѡеĻ޷");
                return;
            }

            try
            {
                CurrentCommandManager.Redo();
                AddLog($"?? : {CurrentCommandManager.LastCommandDescription}");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"ʧ: {ex.Message}", "错误",
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
                // ӽڵ㵽ǰѡеıǩҳ
                WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(new Models.WorkflowNode("1", "ͼɼ_1", "image_capture")
                {
                    Position = new System.Windows.Point(100, 100),
                    IsSelected = false
                });

                WorkflowTabViewModel.SelectedTab.WorkflowNodes.Add(new Models.WorkflowNode("2", "˹ģ", "gaussian_blur")
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
            // TODO: ¹
        }

        private void ExecuteOpenWorkflow()
        {
            // TODO: 򿪹?
        }

        private void ExecuteSaveWorkflow()
        {
            // TODO: 湤ļ
        }

        private void ExecuteSaveAsWorkflow()
        {
            // TODO: Ϊļ
        }

        private async System.Threading.Tasks.Task ExecuteRunWorkflow()
        {
            AddLog("=== ʼִй ===");

            if (WorkflowTabViewModel == null)
            {
                AddLog("?? WorkflowTabViewModel ?null");
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
                AddLog($"? ִʧ? {ex.Message}");
                AddLog($"? 쳣: {ex.StackTrace}");
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
            // TODO: ֱתݼҳ
        }

        /// <summary>
        /// ؽڵԵ?
        /// </summary>
        public void LoadNodeProperties(Models.WorkflowNode? node)
        {
            if (node == null)
            {
                PropertyGroups.Clear();
                return;
            }

            PropertyGroups.Clear();

            // Ϣ
            var basicGroup = new Models.PropertyGroup
            {
                Name = "?? Ϣ",
                IsExpanded = true,
                Parameters = new ObservableCollection<Models.PropertyItem>
                {
                    new Models.PropertyItem { Label = "错误", Value = node.Name },
                    new Models.PropertyItem { Label = "ID", Value = node.Id },
                    new Models.PropertyItem { Label = "错误", Value = node.AlgorithmType ?? "δ֪" }
                }
            };
            PropertyGroups.Add(basicGroup);

            // 
            var paramGroup = new Models.PropertyGroup
            {
                Name = "?? ",
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

            // ͳ
            var perfGroup = new Models.PropertyGroup
            {
                Name = "?? ͳ",
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
        /// ӵǰɾڵ㣨ͨģʽ?
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
        /// ƶڵ㵽λãͨģʽ?
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
        /// ӵǰͨģʽ?
        /// </summary>
        public void AddConnectionToWorkflow(WorkflowConnection connection)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.AddConnectionCommand(WorkflowTabViewModel.SelectedTab.WorkflowConnections, connection);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// ӵǰɾӣͨģʽ?
        /// </summary>
        public void DeleteConnectionFromWorkflow(WorkflowConnection connection)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return;

            var command = new AppCommands.DeleteConnectionCommand(WorkflowTabViewModel.SelectedTab.WorkflowConnections, connection);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// ɾѡеĽڵ㣨ͨģʽ?
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

            // ѡ״?
            SelectedNode = null;
            ClearNodeSelections();
        }

        /// <summary>
        /// нڵѡ״?
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
        /// жǷɾѡнڵ
        /// </summary>
        private bool CanDeleteSelectedNodes()
        {
            if (WorkflowTabViewModel.SelectedTab == null)
                return false;

            return WorkflowTabViewModel.SelectedTab.WorkflowNodes.Any(n => n.IsSelected);
        }

        /// <summary>
        /// ִɾѡнڵ
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
                $"ȷҪɾѡ?{selectedCount} ڵ?",
                "ȷɾ",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var command = new AppCommands.BatchDeleteNodesCommand(
                    WorkflowTabViewModel.SelectedTab.WorkflowNodes,
                    WorkflowTabViewModel.SelectedTab.WorkflowConnections,
                    selectedNodes);
                WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);

                // ѡ״?
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
                            // ʹùԴ
                            var debugWindow = ToolDebugWindowFactory.CreateDebugWindow(toolId, toolPlugin, toolMetadata);
                            debugWindow.Owner = System.Windows.Application.Current.MainWindow;
                            debugWindow.ShowDialog();
                            AddLog($"?? 򿪵Դ: {node.Name}");
                            break;

                        case NodeInterfaceType.NewWorkflowCanvas:
                            // µĹǩҳӳڵ?
                            CreateSubroutineWorkflowTab(node);
                            break;

                        case NodeInterfaceType.SubroutineEditor:
                            // ӳ༭ý棩
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
        /// <param name="subroutineNode">ӳ?/param>
        private void CreateSubroutineWorkflowTab(Models.WorkflowNode subroutineNode)
        {
            try
            {
                if (WorkflowTabViewModel == null)
                {
                    AddLog("?? WorkflowTabViewModel ?null");
                    return;
                }

                // ʹӳڵΪ
                string workflowName = subroutineNode.Name;
                if (string.IsNullOrWhiteSpace(workflowName))
                {
                    workflowName = "ӳ";
                }

                AddLog($"?? ӳǩ? {workflowName}");

                // µĹǩҳ
                var newWorkflowTab = new WorkflowTabViewModel
                {
                    Name = workflowName,
                    Id = Guid.NewGuid().ToString()
                };

                // ӵǩҳ
                WorkflowTabViewModel.Tabs.Add(newWorkflowTab);

                // ѡ´ıǩ?
                WorkflowTabViewModel.SelectedTab = newWorkflowTab;

                AddLog($"? ӳ '{workflowName}' ɹ");
                AddLog($"?? ʾڿӽڵӳ߼");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"ӳʧ: {ex.Message}",
                    "错误",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                AddLog($"? ӳʧ: {ex.Message}");
            }
        }

        /// <summary>
        /// лӾ?
        /// </summary>
        private void ExecuteToggleBoundingRectangle()
        {
            AddLog("[ToggleBoundingRectangle] ========== лӾ?==========");

            try
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as Views.Windows.MainWindow;
                if (mainWindow == null)
                {
                    AddLog("[ToggleBoundingRectangle] ? MainWindowΪnull");
                    return;
                }

                AddLog("[ToggleBoundingRectangle] 获取 MainWindow");

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
        /// 在指定的 WorkflowCanvasControl 中切换显示
        /// </summary>
        private void ToggleBoundingRectangleOnCanvas(WorkflowCanvasControl workflowCanvas)
        {
            workflowCanvas.ShowBoundingRectangle = !workflowCanvas.ShowBoundingRectangle;

            // ʾʹõһΪʾ?
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

            AddLog($"[ToggleBoundingRectangle] ========== Ӿ: {(workflowCanvas.ShowBoundingRectangle ? "ʾ" : "")} ==========");
        }

        /// <summary>
        /// л·յʾ
        /// </summary>
        private void ExecuteTogglePathPoints()
        {
            AddLog("[TogglePathPoints] лӵ·յʾ");

            if (WorkflowTabViewModel?.SelectedTab?.WorkflowConnections != null)
            {
                var newState = !WorkflowTabViewModel.SelectedTab.WorkflowConnections.Any(c => c.ShowPathPoints);

                foreach (var connection in WorkflowTabViewModel.SelectedTab.WorkflowConnections)
                {
                    connection.ShowPathPoints = newState;
                }

                AddLog($"[TogglePathPoints] ӵ·յ: {(newState ? "ʾ" : "")}");
            }
        }

        /// <summary>
        /// ִей
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteRunAllWorkflows()
        {
            AddLog("?? ʼй...");
            await WorkflowTabViewModel.RunAllWorkflowsAsync();
            AddLog("? й");
        }

        /// <summary>
        /// лй?ֹͣ
        /// </summary>
        private void ExecuteToggleContinuousAll()
        {
            if (IsAllWorkflowsRunning)
            {
                AddLog("?? ֹͣй");
                WorkflowTabViewModel.StopAllWorkflows();
            }
            else
            {
                AddLog("?? ʼй");
                WorkflowTabViewModel.StartAllWorkflows();
            }
        }

        /// <summary>
        /// ִпʼ¼?
        /// </summary>
        private void OnWorkflowExecutionStarted(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"?? ʼִ? {e.WorkflowId}");
        }

        /// <summary>
        /// ִ¼?
        /// </summary>
        private void OnWorkflowExecutionCompleted(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"? ִ? {e.WorkflowId}");
        }

        /// <summary>
        /// ִֹͣ¼?
        /// </summary>
        private void OnWorkflowExecutionStopped(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"?? ִͣ? {e.WorkflowId}");
        }

        /// <summary>
        /// ִд¼?
        /// </summary>
        private void OnWorkflowExecutionError(object? sender, WorkflowExecutionEventArgs e)
        {
            AddLog($"? ִд? {e.WorkflowId} - {e.ErrorMessage}");
        }

        /// <summary>
        /// ִн¼?
        /// </summary>
        private void OnWorkflowExecutionProgress(object? sender, WorkflowExecutionProgressEventArgs e)
        {
            try
            {
                AddLog(e.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindowViewModel] OnWorkflowExecutionProgress쳣: {ex.Message}");
                AddLog($"?? ־쳣: {ex.Message}");
            }
        }

        /// <summary>
        /// ָ͵Ԫ
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

        #region ͼ

        /// <summary>
        /// Ŵͼ
        /// </summary>
        private void ExecuteZoomIn()
        {
            ImageScale = Math.Min(ImageScale * 1.2, 5.0);
            AddLog($"?? ͼŴ: {ImageScale:P0}");
        }

        /// <summary>
        /// Сͼ
        /// </summary>
        private void ExecuteZoomOut()
        {
            ImageScale = Math.Max(ImageScale / 1.2, 0.1);
            AddLog($"?? ͼС: {ImageScale:P0}");
        }

        /// <summary>
        /// Ӧ
        /// </summary>
        private void ExecuteFitToWindow()
        {
            // TODO: ݴڴСʵű
            ImageScale = 1.0;
            AddLog($"?? Ӧ: {ImageScale:P0}");
        }

        /// <summary>
        /// ͼ
        /// </summary>
        private void ExecuteResetView()
        {
            ImageScale = 1.0;
            AddLog($"? ͼ: {ImageScale:P0}");
        }

        /// <summary>
        /// лȫʾ
        /// </summary>
        private void ExecuteToggleFullScreen()
        {
            // TODO: ʵͼȫʾ
            AddLog("? лȫʾ");
        }

        #endregion

        #region ͼ

        /// <summary>
        /// ͼļ
        /// </summary>
        private void ExecuteBrowseImage()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "ͼļ|*.jpg;*.jpeg;*.png;*.bmp;*.tiff|ļ|*.*",
                    Title = "ѡͼļ"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    AddLog($"?? ѡļ: {filePath}");

                    // TODO: ͼOriginalImage
                    // OriginalImage = LoadImageFromFile(filePath);
                }
            }
            catch (Exception ex)
            {
                AddLog($"? ͼʧ: {ex.Message}");
                System.Windows.MessageBox.Show($"ͼʧ: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ͼ
        /// </summary>
        private void ExecuteLoadImage()
        {
            try
            {
                if (OriginalImage == null)
                {
                    AddLog("?? ѡͼļ");
                    return;
                }

                AddLog("? ͼɹ");
                // TODO: ͼ񲢸ProcessedImageResultImage
            }
            catch (Exception ex)
            {
                AddLog($"? ͼʧ: {ex.Message}");
                System.Windows.MessageBox.Show($"ͼʧ: {ex.Message}", "错误",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ͼ
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

        #region ͼԤ

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
            
            // FullImage
            var fullImage = imageInfo.FullImage;
            
            if (fullImage != null)
            {
                OriginalImage = fullImage;
                AddLog($"?? ͼ: {imageInfo.Name}");
                
                // ȷDisplayImage?
                UpdateDisplayImage();
            }
        }

        #endregion

        #region 

        /// <summary>
        /// ʾͼ
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
        /// ¼
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
        /// 图像预览显示状态管理关系
        /// </summary>
        /// <remarks>
        /// 规则:
        /// 1. 选择图像采集节点 -> 显示该节点的图像
        /// 2. 选择其他节点 -> BFS逆向追踪采集节点，找到可显示图像
        ///    (优化: 逆向追踪采集节点与当前显示同源则更新)
        /// 3. 逆向追踪采集节点图像 -> 显示在图像预览中
        /// </remarks>
        public void UpdateImagePreviewVisibility(Models.WorkflowNode? selectedNode)
        {
            // 1ûѡнڵ ?
            if (selectedNode == null)
            {
                ShowImagePreview = false;
                ActiveNodeImageData = null;
                _currentDisplayNodeId = null;  // ?ID
                return;
            }

            // 2ѡеͼɼڵ ?ʼʾͼԤʹʱûͼ?
            if (selectedNode.IsImageCaptureNode)
            {
                UpdateActiveNodeImageData(selectedNode);
                ShowImagePreview = true;
                OnPropertyChanged(nameof(ShowImagePreview));
                return;
            }

            // 3ѡеĲͼɼ??BFS׷βɼڵ
            // ?ټ飺ûӣνڵ㣬ֱ
            var connections = WorkflowTabViewModel?.SelectedTab?.WorkflowConnections;
            if (connections == null || connections.Count == 0)
            {
                ShowImagePreview = false;
                ActiveNodeImageData = null;
                _currentDisplayNodeId = null;
                OnPropertyChanged(nameof(ShowImagePreview));
                return;
            }
            
            var sourceCaptureNode = FindUpstreamImageCaptureNode(selectedNode);

            if (sourceCaptureNode != null)
            {
                bool hasImages = sourceCaptureNode.ImageData != null && sourceCaptureNode.ImageData.ImageCount > 0;
                
                // ?ŻβɼڵǷ뵱ǰʾ?
                bool isSameNode = _currentDisplayNodeId == sourceCaptureNode.Id;
                bool hasActiveData = ActiveNodeImageData != null;
                
                if (hasImages)
                {
                    if (isSameNode && hasActiveData)
                    {
                        // ?ͬڵҵǰݣҪ?ActiveNodeImageData
                        // ⴥҪͼ¼?
                        ShowImagePreview = true;
                    }
                    else
                    {
                        // ͬڵ֮ǰգҪ?
                        _currentDisplayNodeId = sourceCaptureNode.Id;
                        ActiveNodeImageData = sourceCaptureNode.ImageData;
                        ShowImagePreview = true;
                    }
                }
                else
                {
                    // βɼڵͼ??
                    ShowImagePreview = false;
                    ActiveNodeImageData = null;
                    _currentDisplayNodeId = null;  // ?ID
                }
            }
            else
            {
                // βɼ??
                ShowImagePreview = false;
                ActiveNodeImageData = null;
                _currentDisplayNodeId = null;  // ?ID
            }

            OnPropertyChanged(nameof(ShowImagePreview));
        }

        /// <summary>
        /// ǿˢͼԤӴȳ?
        /// ʹǰ SelectedNode δı䣬Ҳ¼ǷʾͼԤ?
        /// </summary>
        public void ForceRefreshImagePreview()
        {
            UpdateImagePreviewVisibility(_selectedNode);
        }

        /// <summary>
        /// ѡнڵͼɼڵ㣨BFS?
        /// </summary>
        /// <remarks>
        /// ڶβɼڵʱصһҵĲɼڵ?
        /// 1. BFS֤·?
        /// 2. ڵID֤ȷѡ
        /// </remarks>
        /// <param name="node">ʼڵ</param>
        /// <returns>һҵͼɼڵ㣬δҵnull</returns>
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

                // ȡνڵIDڼе˳ٰڵID֤ȷԣ
                var upstreamNodeIds = selectedTab.WorkflowConnections
                    .Where(conn => conn.TargetNodeId == currentNode.Id)
                    .Select(conn => conn.SourceNodeId)
                    .Distinct()
                    .OrderBy(id => id) // ڵID򣬱֤ȷ?
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

                    // ҵͼɼڵ㣬أһҵ?
                    if (upstreamNode.IsImageCaptureNode)
                    {
                        return upstreamNode;
                    }

                    // ǲɼڵ㣬׷
                    visited.Add(upstreamNodeId);
                    queue.Enqueue(upstreamNode);
                }
            }

            return null;
        }

        #endregion

        #endregion // 

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

            public Dictionary<string, object> GetDefaultParameters(string toolId)
            {
                return new Dictionary<string, object>();
            }
        }
    }
}
