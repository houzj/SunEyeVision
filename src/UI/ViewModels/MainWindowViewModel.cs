using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using AppCommands = SunEyeVision.UI.Commands;
using SunEyeVision.UI.Models;
using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Validation;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
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
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.UI.Converters.Path;
using SunEyeVision.UI.Services.Performance;
using SunEyeVision.UI.Services.Logging;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Core.Models;
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

        // 工作流文件管理
        private string? _currentWorkflowFilePath;
        private bool _isWorkflowModified = false;

        // 工作流引擎
        private readonly WorkflowEngine _workflowEngine;
        private readonly ILogger _logger;

        // 执行管理
        private readonly WorkflowExecutionManager _executionManager;

        // 运行时参数存储
        private readonly Dictionary<string, object> _runtimeParameters = new();

        // 节点结果管理
        private readonly Services.Workflow.NodeResultManager _nodeResultManager;

        // 工作流执行协调器（图像切换优化）
        private WorkflowExecutionOrchestrator? _executionOrchestrator;

        // 已打开的调试窗口（全局单例）
        private Window? _openDebugWindow;

        // 数据提供者缓存（用于在节点执行完成后更新前置节点输出）
        private readonly Dictionary<string, Plugin.SDK.UI.Controls.Region.Models.WorkflowDataSourceProvider> _nodeDataProviders = new();

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
        private Models.ImageInputSource? _activeInputSource; // 新增：活动输入源
        private string? _currentDisplayNodeId = null;  // 记录当前显示的采集节点ID，避免重复切换

        /// <summary>
        /// 当前活动节点图像数据，用于绑定到图像预览控件
        /// 每个采集节点维护自己的图像集
        /// </summary>
        [Obsolete("请使用 ActiveInputSource 替代。此属性仅为向后兼容保留。")]
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

        /// <summary>
        /// 当前活动输入源，用于绑定到图像预览控件
        /// 输入源存储用户选择的图像文件，执行流程不会修改
        /// </summary>
        public Models.ImageInputSource? ActiveInputSource
        {
            get => _activeInputSource;
            private set
            {
                if (ReferenceEquals(_activeInputSource, value))
                {
                    return;
                }
                SetProperty(ref _activeInputSource, value);
            }
        }

        /// <summary>
        /// 图像显示控件的数据源（当前节点 + 所有父节点）
        /// </summary>
        public ObservableCollection<ImageSourceInfo> DisplayImageSources { get; }

        /// <summary>
        /// 当前选中的图像显示源索引
        /// </summary>
        private int _selectedDisplayImageSourceIndex = -1;

        /// <summary>
        /// 当前选中的图像显示源索引
        /// </summary>
        public int SelectedDisplayImageSourceIndex
        {
            get => _selectedDisplayImageSourceIndex;
            set => SetProperty(ref _selectedDisplayImageSourceIndex, value);
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

                    // 刷新结果显示（如果节点有缓存结果）
                    if (value?.LastResult != null)
                    {
                        _nodeResultManager.RefreshResultDisplay(value, value.LastResult);
                    }
                    else
                    {
                        // 清空结果显示
                        _nodeResultManager.ClearResultDisplay();
                    }

                    // 更新图像显示数据源（当前节点 + 父节点）
                    UpdateDisplayImageSources(value);

                    // 节点选中状态变化时更新图像预览（整合了 ActiveNodeImageData 更新逻辑）
                    UpdateImagePreviewVisibility(value);
                    // 加载节点属性
                    LoadNodeProperties(value);
                }
            }
        }

        /// <summary>
        /// 强制选中节点（即使引用相同也强制更新显示）
        /// 用于双击节点时确保图像显示正确更新
        /// </summary>
        /// <remarks>
        /// ★ 防闪烁优化：移除不必要的清空操作，直接刷新显示
        /// </remarks>
        /// <param name="node">要选中的节点</param>
        public void ForceSelectNode(Models.WorkflowNode node)
        {
            // 更新选中状态
            if (_selectedNode != node)
            {
                SelectedNode = node;
            }
            else
            {
                // ★ 优化：即使引用相同，也只刷新结果，不清空（防止闪烁）
                if (node?.LastResult != null)
                {
                    _nodeResultManager.RefreshResultDisplay(node, node.LastResult);
                }
                UpdateDisplayImageSources(node);
                UpdateImagePreviewVisibility(node);
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

        // 当前解决方案信息
        private string _currentSolutionName = string.Empty;
        private string _currentSolutionPath = string.Empty;

        /// <summary>
        /// 当前解决方案名称
        /// </summary>
        public string CurrentSolutionName
        {
            get => _currentSolutionName;
            set => SetProperty(ref _currentSolutionName, value);
        }

        /// <summary>
        /// 当前解决方案路径
        /// </summary>
        public string CurrentSolutionPath
        {
            get => _currentSolutionPath;
            set => SetProperty(ref _currentSolutionPath, value);
        }

        /// <summary>
        /// 是否有当前解决方案
        /// </summary>
        public bool HasCurrentSolution => !string.IsNullOrEmpty(CurrentSolutionName);

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
        /// <remarks>
        /// ★ 防闪烁优化：setter 不再自动更新 DisplayImage，由调用方显式控制
        /// </remarks>
        public BitmapSource? OriginalImage
        {
            get => _originalImage;
            set => SetProperty(ref _originalImage, value);
        }

        /// <summary>
        /// 处理后图像
        /// </summary>
        /// <remarks>
        /// ★ 防闪烁优化：setter 不再自动更新 DisplayImage，由调用方显式控制
        /// </remarks>
        public BitmapSource? ProcessedImage
        {
            get => _processedImage;
            set => SetProperty(ref _processedImage, value);
        }

        /// <summary>
        /// 结果图像
        /// </summary>
        /// <remarks>
        /// ★ 防闪烁优化：setter 不再自动更新 DisplayImage，由调用方显式控制
        /// </remarks>
        public BitmapSource? ResultImage
        {
            get => _resultImage;
            set => SetProperty(ref _resultImage, value);
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
        /// 添加日志（智能迁移版本 - 自动解析Emoji和来源）
        /// </summary>
        public void AddLog(string message)
        {
            var (level, cleanMessage, source) = ParseLegacyLogMessage(message);
            VisionLogger.Instance.Log(level, cleanMessage, source);
        }

        /// <summary>
        /// 添加带级别的日志
        /// </summary>
        public void AddLog(LogLevel level, string message, string? source = null)
        {
            VisionLogger.Instance.Log(level, message, source);
        }

        /// <summary>
        /// 解析遗留日志消息，提取级别、来源和纯净消息
        /// </summary>
        private static (LogLevel level, string message, string source) ParseLegacyLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return (LogLevel.Info, message ?? string.Empty, LogSource.UI("默认"));

            var level = LogLevel.Info;
            var source = LogSource.UI("默认");
            var cleanMessage = message;

            // 解析Emoji前缀确定日志级别
            if (message.StartsWith("✅") || message.StartsWith("✓") || message.StartsWith("✔"))
            {
                level = LogLevel.Success;
                cleanMessage = message.Substring(1).TrimStart();
            }
            else if (message.StartsWith("❌") || message.StartsWith("✖") || message.StartsWith("✗"))
            {
                level = LogLevel.Error;
                cleanMessage = message.Substring(1).TrimStart();
            }
            else if (message.StartsWith("⚠️") || message.StartsWith("⚠"))
            {
                level = LogLevel.Warning;
                cleanMessage = message.Substring(message.StartsWith("⚠️") ? 2 : 1).TrimStart();
            }
            else if (message.StartsWith("🗑️") || message.StartsWith("🔧") || message.StartsWith("📝"))
            {
                level = LogLevel.Info;
                cleanMessage = message.Substring(message.StartsWith("🗑️") ? 2 : 1).TrimStart();
            }

            // 解析[来源]标记
            var sourceMatch = System.Text.RegularExpressions.Regex.Match(cleanMessage, @"^\[([^\]]+)\]\s*");
            if (sourceMatch.Success)
            {
                var sourceTag = sourceMatch.Groups[1].Value;
                cleanMessage = cleanMessage.Substring(sourceMatch.Length);

                // 映射来源标签到LogSource
                source = sourceTag switch
                {
                    "系统" => LogSource.SystemCore,
                    "设备" => LogSource.DeviceCamera,
                    "运行" => LogSource.UIOperation,
                    "Connection" => LogSource.UIConnection,
                    _ => LogSource.UI(sourceTag)
                };
            }

            // 根据消息内容推断来源
            if (cleanMessage.Contains("节点"))
                source = LogSource.UINode;
            else if (cleanMessage.Contains("连接"))
                source = LogSource.UIConnection;
            else if (cleanMessage.Contains("工作流"))
                source = LogSource.UIOperation;
            else if (cleanMessage.Contains("撤销") || cleanMessage.Contains("重做"))
                source = LogSource.UIEdit;
            else if (cleanMessage.Contains("调试窗口"))
                source = LogSource.UIDebug;
            else if (cleanMessage.Contains("运行时参数"))
                source = LogSource.UIConfig;
            else if (cleanMessage.Contains("缓存"))
                source = LogSource.UIOperation;

            return (level, cleanMessage, source);
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public void ClearLog()
        {
            // 使用日志管理器清空
            // 注意：VisionLogger 不支持直接清空，这里仅作标记
            // TODO: 实现日志清空功能
        }

        /// <summary>
        /// 获取工作流执行协调器
        /// </summary>
        public WorkflowExecutionOrchestrator? ExecutionOrchestrator => _executionOrchestrator;

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int Count, long MemoryBytes, double MemoryMB) GetCacheStatistics()
        {
            return _executionOrchestrator?.GetCacheStatistics() ?? (0, 0, 0);
        }

        /// <summary>
        /// 清空执行缓存
        /// </summary>
        public void ClearExecutionCache()
        {
            _executionOrchestrator?.ClearCache();
            AddLog("🗑️ 已清空执行缓存");
        }

        public ICommand NewWorkflowCommand { get; }
        public ICommand OpenWorkflowCommand { get; }
        public ICommand SaveWorkflowCommand { get; }
        public ICommand SaveAsWorkflowCommand { get; }

        // 解决方案保存命令
        public ICommand SaveCurrentSolutionCommand { get; }

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

        public ICommand ClearLogCommand { get; }

        // 解决方案管理
        public ICommand ShowSolutionConfigurationCommand { get; }
        public ICommand SwitchProjectCommand { get; }

        // 管理器
        public ICommand ShowGlobalVariableManagerCommand { get; }
        public ICommand ShowCameraManagerCommand { get; }
        public ICommand ShowCommunicationManagerCommand { get; }

        // 主窗口 ImageControl 获取委托 - 用于区域编辑器绑定
        /// <summary>
        /// 获取主窗口 ImageControl 的委托 - 用于区域编辑器绑定
        /// </summary>
        /// <remarks>
        /// 由 MainWindow 在初始化时设置，用于在调试窗口创建时传递 ImageControl 引用
        /// </remarks>
        public Func<ImageControl?>? GetMainImageControl { get; set; }

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

            // 初始化图像显示数据源
            DisplayImageSources = new ObservableCollection<ImageSourceInfo>();

            // 初始化工作流引擎
            _logger = VisionLogger.Instance;
            _workflowEngine = new WorkflowEngine(_logger);

            // 初始化执行管理器
            _executionManager = new Services.Workflow.WorkflowExecutionManager(new Infrastructure.DefaultInputProvider());

            // 初始化节点结果管理器
            _nodeResultManager = new Services.Workflow.NodeResultManager(this);

            // 初始化工作流执行协调器（图像切换优化）
            _executionOrchestrator = new WorkflowExecutionOrchestrator(_nodeResultManager, maxCacheSize: 50, maxMemoryMB: 100);

            // 订阅执行管理器事件
            _executionManager.WorkflowExecutionStarted += OnWorkflowExecutionStarted;
            _executionManager.WorkflowExecutionCompleted += OnWorkflowExecutionCompleted;
            _executionManager.WorkflowExecutionStopped += OnWorkflowExecutionStopped;
            _executionManager.WorkflowExecutionError += OnWorkflowExecutionError;
            _executionManager.WorkflowExecutionProgress += OnWorkflowExecutionProgress;
            _executionManager.NodeExecutionCompleted += OnNodeExecutionCompleted;

            // 初始化当前画布类型
            UpdateCurrentCanvasType();

            // 选中标签页变化时更新运行/停止按钮状态
            WorkflowTabViewModel.SelectionChanged += OnSelectedTabChanged;

            // 订阅工作流状态变化
            WorkflowTabViewModel.WorkflowStatusChanged += OnWorkflowStatusChanged;

            // 命令管理器的初始化
            SubscribeToCurrentCommandManager();

            // 监听解决方案变更事件
            var solutionManager = Adapters.ServiceInitializer.SolutionManager;
            solutionManager.CurrentSolutionChanged += OnCurrentSolutionChanged;

            // 初始化当前解决方案信息
            UpdateCurrentSolutionInfo();

            InitializeTools();
            // InitializeSampleNodes(); // 已禁用，暂时不加载测试节点
            InitializePropertyGroups();

            NewWorkflowCommand = new RelayCommand(ExecuteNewWorkflow);
            OpenWorkflowCommand = new RelayCommand(ExecuteOpenWorkflow);
            SaveWorkflowCommand = new RelayCommand(ExecuteSaveWorkflow);
            SaveAsWorkflowCommand = new RelayCommand(ExecuteSaveAsWorkflow);
            
            // 解决方案保存命令
            SaveCurrentSolutionCommand = new RelayCommand(ExecuteSaveCurrentSolution);
            
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

            // 日志操作
            ClearLogCommand = new RelayCommand(ClearLog);

            // 解决方案管理
            ShowSolutionConfigurationCommand = new RelayCommand(ExecuteShowSolutionConfiguration);
            SwitchProjectCommand = new RelayCommand(ExecuteSwitchProject);

            // 管理器
            ShowGlobalVariableManagerCommand = new RelayCommand(ExecuteShowGlobalVariableManager);
            ShowCameraManagerCommand = new RelayCommand(ExecuteShowCameraManager);
            ShowCommunicationManagerCommand = new RelayCommand(ExecuteShowCommunicationManager);
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
                // 准备要添加的节点和连接
                var nodesToAdd = new List<Models.WorkflowNode>
                {
                    new Models.WorkflowNode("1", "图像采集_1", "image_capture")
                    {
                        Position = new System.Windows.Point(100, 100),
                        IsSelected = false
                    },
                    new Models.WorkflowNode("2", "高斯模糊", "gaussian_blur")
                    {
                        Position = new System.Windows.Point(300, 100),
                        IsSelected = false
                    },
                    new Models.WorkflowNode("3", "边缘检测", "edge_detection")
                    {
                        Position = new System.Windows.Point(500, 100),
                        IsSelected = false
                    }
                };

                var connectionsToAdd = new List<Models.WorkflowConnection>
                {
                    new Models.WorkflowConnection("conn_1", "1", "2")
                    {
                        SourcePosition = new System.Windows.Point(240, 145),
                        TargetPosition = new System.Windows.Point(300, 145)
                    },
                    new Models.WorkflowConnection("conn_2", "2", "3")
                    {
                        SourcePosition = new System.Windows.Point(440, 145),
                        TargetPosition = new System.Windows.Point(500, 145)
                    }
                };

                // 批量添加，减少 UI 更新次数
                AddRangeToCollection(WorkflowTabViewModel.SelectedTab.WorkflowNodes, nodesToAdd);
                AddRangeToCollection(WorkflowTabViewModel.SelectedTab.WorkflowConnections, connectionsToAdd);
            }
        }

        private void ExecuteNewWorkflow()
        {
            // TODO: 新建工作流
        }

        private void ExecuteOpenWorkflow()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "工作流文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "打开工作流",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = openFileDialog.FileName;
                    AddLog($"📂 正在加载工作流: {filePath}");

                    // 加载工作流
                    var success = _workflowEngine.LoadWorkflow(filePath);

                    if (!success)
                    {
                        AddLog("❌ 工作流加载失败，请查看日志了解详情");
                        return;
                    }

                    // 获取加载的工作流
                    var workflow = _workflowEngine.CurrentWorkflow;
                    if (workflow == null)
                    {
                        AddLog("❌ 加载的工作流为 null");
                        return;
                    }

                    AddLog($"✅ 工作流加载成功: {workflow.Name}");
                    AddLog($"   - 节点数量: {workflow.Nodes.Count}");
                    AddLog($"   - 连接数量: {workflow.Connections.Count}");

                    // 创建新的 UI 标签页
                    CreateWorkflowTab(workflow, filePath);

                    _currentWorkflowFilePath = filePath;
                    _isWorkflowModified = false;

                    AddLog($"✅ 工作流已在画布中打开");
                }
                catch (Exception ex)
                {
                    AddLog($"❌ 打开工作流时发生异常: {ex.Message}");
                    _logger?.LogError("打开工作流失败", "MainWindowViewModel", ex);
                }
            }
        }

        private void ExecuteSaveWorkflow()
        {
            if (WorkflowTabViewModel?.SelectedTab == null)
            {
                AddLog("❌ 没有选中的工作流标签页");
                return;
            }

            var selectedTab = WorkflowTabViewModel.SelectedTab;
            var workflowId = selectedTab.Id;

            // 如果没有文件路径，执行"另存为"
            if (string.IsNullOrEmpty(_currentWorkflowFilePath))
            {
                ExecuteSaveAsWorkflow();
                return;
            }

            try
            {
                AddLog($"💾 正在保存工作流到: {_currentWorkflowFilePath}");

                // 获取底层工作流对象
                var workflow = _workflowEngine.GetWorkflowById(workflowId);
                if (workflow == null)
                {
                    AddLog($"❌ 工作流不存在: {workflowId}");
                    return;
                }

                // 更新工作流节点（从 UI 层同步到底层）
                UpdateWorkflowFromUI(workflow, selectedTab);

                // 保存工作流
                var success = _workflowEngine.SaveWorkflow(workflowId, _currentWorkflowFilePath);

                if (success)
                {
                    _isWorkflowModified = false;
                    AddLog($"✅ 工作流已成功保存: {_currentWorkflowFilePath}");
                }
                else
                {
                    AddLog("❌ 工作流保存失败，请查看日志了解详情");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ 保存工作流时发生异常: {ex.Message}");
                _logger?.LogError("保存工作流失败", "MainWindowViewModel", ex);
            }
        }

        private void ExecuteSaveAsWorkflow()
        {
            if (WorkflowTabViewModel?.SelectedTab == null)
            {
                AddLog("❌ 没有选中的工作流标签页");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "工作流文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = "json",
                Title = "保存工作流",
                FileName = WorkflowTabViewModel.SelectedTab.Name + ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = saveFileDialog.FileName;
                    var selectedTab = WorkflowTabViewModel.SelectedTab;
                    var workflowId = selectedTab.Id;

                    AddLog($"💾 正在保存工作流到: {filePath}");

                    // 获取底层工作流对象
                    var workflow = _workflowEngine.GetWorkflowById(workflowId);
                    if (workflow == null)
                    {
                        AddLog($"❌ 工作流不存在: {workflowId}");
                        return;
                    }

                    // 更新工作流（从 UI 层同步）
                    UpdateWorkflowFromUI(workflow, selectedTab);

                    // 保存工作流
                    var success = _workflowEngine.SaveWorkflow(workflowId, filePath);

                    if (success)
                    {
                        _currentWorkflowFilePath = filePath;
                        _isWorkflowModified = false;
                        AddLog($"✅ 工作流已成功保存: {filePath}");
                    }
                    else
                    {
                        AddLog("❌ 工作流保存失败，请查看日志了解详情");
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"❌ 另存工作流时发生异常: {ex.Message}");
                    _logger?.LogError("另存工作流失败", "MainWindowViewModel", ex);
                }
            }
        }

        /// <summary>
        /// 保存当前解决方案
        /// </summary>
        private void ExecuteSaveCurrentSolution()
        {
            try
            {
                LogInfo("保存当前解决方案");

                var solutionManager = Adapters.ServiceInitializer.SolutionManager;
                if (solutionManager.CurrentSolution == null)
                {
                    LogWarning("没有当前解决方案");
                    return;
                }

                // 保存当前解决方案
                solutionManager.SaveSolution();

                var metadata = solutionManager.GetMetadata(solutionManager.CurrentSolution.Id);
                LogSuccess($"已保存当前解决方案: {metadata?.Name ?? "未命名"}");
            }
            catch (Exception ex)
            {
                LogError($"保存当前解决方案失败: {ex.Message}", null, ex);
            }
        }

        /// <summary>
        /// 更新当前解决方案信息
        /// </summary>
        private void UpdateCurrentSolutionInfo()
        {
            try
            {
                var solutionManager = Adapters.ServiceInitializer.SolutionManager;
                var currentSolution = solutionManager.CurrentSolution;

                if (currentSolution != null)
                {
                    var metadata = solutionManager.GetMetadata(currentSolution.Id);
                    CurrentSolutionName = metadata?.Name ?? "未命名解决方案";
                    CurrentSolutionPath = metadata?.FilePath ?? "";
                    LogInfo($"当前解决方案: {CurrentSolutionName}");
                }
                else
                {
                    CurrentSolutionName = string.Empty;
                    CurrentSolutionPath = string.Empty;
                }

                // 通知属性变化
                OnPropertyChanged(nameof(HasCurrentSolution));
            }
            catch (Exception ex)
            {
                LogError($"更新解决方案信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 当前解决方案变更事件处理
        /// </summary>
        private void OnCurrentSolutionChanged(object? sender, SolutionMetadataEventArgs e)
        {
            UpdateCurrentSolutionInfo();
        }

        /// <summary>
        /// 同步UI层的工作流到底层工作流（公开接口）
        /// </summary>
        public void SyncWorkflowFromUI(SunEyeVision.Workflow.Workflow workflow, WorkflowTabViewModel tabInfo)
        {
            UpdateWorkflowFromUI(workflow, tabInfo);
        }

        /// <summary>
        /// 从 UI 层同步到底层工作流
        /// </summary>
        private void UpdateWorkflowFromUI(SunEyeVision.Workflow.Workflow workflow, WorkflowTabViewModel tabInfo)
        {
            // 清空现有节点
            workflow.Nodes.Clear();

            // 转换 UI 节点到底层节点
            foreach (var uiNode in tabInfo.WorkflowNodes)
            {
                // 根据 AlgorithmType 确定 NodeType
                var nodeType = DetermineNodeTypeFromAlgorithmType(uiNode.AlgorithmType);

                var workflowNode = new WorkflowWorkflowNode(
                    uiNode.Id,
                    uiNode.Name,
                    nodeType
                )
                {
                    AlgorithmType = uiNode.AlgorithmType,
                    Parameters = ConvertDictionaryToToolParameters(uiNode.Parameters),
                    IsEnabled = uiNode.IsEnabled
                };

                // 设置参数类型名称（用于强类型恢复）
                if (uiNode.Parameters?.GetType() != typeof(Dictionary<string, object>))
                {
                    workflowNode.ParametersTypeName = uiNode.Parameters?.GetType().AssemblyQualifiedName;
                }

                // 转换参数绑定
                if (uiNode.ParameterBindings != null && uiNode.ParameterBindings.Count > 0)
                {
                    workflowNode.ParameterBindings = uiNode.ParameterBindings;
                }

                workflow.Nodes.Add(workflowNode);
            }

            // 清空现有连接
            workflow.Connections.Clear();

            // 转换 UI 连接到底层连接
            foreach (var uiConn in tabInfo.WorkflowConnections)
            {
                // 添加连接
                var connection = new Connection
                {
                    SourceNode = uiConn.SourceNodeId,
                    SourcePort = uiConn.SourcePort ?? "output",
                    TargetNode = uiConn.TargetNodeId,
                    TargetPort = uiConn.TargetPort ?? "input"
                };
                workflow.Connections.Add(connection);
            }

            // 更新工作流元数据
            workflow.Name = tabInfo.Name;
        }

        /// <summary>
        /// 根据算法类型确定节点类型
        /// </summary>
        private SunEyeVision.Core.Models.NodeType DetermineNodeTypeFromAlgorithmType(string algorithmType)
        {
            if (string.IsNullOrEmpty(algorithmType))
                return SunEyeVision.Core.Models.NodeType.Algorithm;

            // 图像采集类工具作为起始节点
            if (algorithmType.Contains("ImageCapture") ||
                algorithmType.Contains("ImageAcquisition") ||
                algorithmType.Contains("Camera") ||
                algorithmType.Contains("image_capture"))
            {
                return SunEyeVision.Core.Models.NodeType.Start;
            }

            // 图像载入类工具作为起始节点
            if (algorithmType.Contains("ImageLoad") ||
                algorithmType.Contains("image_load"))
            {
                return SunEyeVision.Core.Models.NodeType.Start;
            }

            // 控制类节点
            if (algorithmType.Contains("Subroutine") ||
                algorithmType.Contains("subroutine"))
            {
                return SunEyeVision.Core.Models.NodeType.Subroutine;
            }

            if (algorithmType.Contains("Condition") ||
                algorithmType.Contains("condition"))
            {
                return SunEyeVision.Core.Models.NodeType.Condition;
            }

            // 默认为算法节点
            return SunEyeVision.Core.Models.NodeType.Algorithm;
        }

        /// <summary>
        /// 将 Dictionary 参数转换为 ToolParameters
        /// </summary>
        private Plugin.SDK.Execution.Parameters.ToolParameters ConvertDictionaryToToolParameters(Dictionary<string, object>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return new Plugin.SDK.Execution.Parameters.GenericToolParameters();

            var toolParams = new Plugin.SDK.Execution.Parameters.GenericToolParameters();

            // 将 Dictionary 中的键值对复制到 ToolParameters
            foreach (var kvp in parameters)
            {
                toolParams.SetValue(kvp.Key, kvp.Value);
            }

            return toolParams;
        }

        /// <summary>
        /// 将 ToolParameters 转换为 Dictionary
        /// </summary>
        private Dictionary<string, object> ConvertToolParametersToDictionary(Plugin.SDK.Execution.Parameters.ToolParameters? toolParams)
        {
            var dict = new Dictionary<string, object>();

            if (toolParams is Plugin.SDK.Execution.Parameters.GenericToolParameters genericParams)
            {
                // GenericToolParameters 提供了 GetParameterNames 方法
                foreach (var paramName in genericParams.GetParameterNames())
                {
                    // 尝试获取参数值
                    var value = genericParams.GetValue<object>(paramName);
                    if (value != null)
                    {
                        dict[paramName] = value;
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// 为加载的工作流创建新的标签页
        /// </summary>
        private void CreateWorkflowTab(SunEyeVision.Workflow.Workflow workflow, string? filePath)
        {
            var tabInfo = new Models.WorkflowTabInfo
            {
                WorkflowId = workflow.Id,
                Name = workflow.Name,
                FilePath = filePath,
                IsModified = false,
                Workflow = workflow
            };

            // 转换底层节点到 UI 节点（先收集，再批量添加）
            var nodesToLoad = new List<UIWorkflowNode>();
            foreach (var workflowNode in workflow.Nodes)
            {
                var uiNode = new UIWorkflowNode(
                    workflowNode.Id,
                    workflowNode.Name,
                    workflowNode.AlgorithmType
                )
                {
                    Parameters = ConvertToolParametersToDictionary(workflowNode.Parameters),
                    IsEnabled = workflowNode.IsEnabled,
                    ParameterBindings = workflowNode.ParameterBindings
                };

                nodesToLoad.Add(uiNode);
            }

            // 批量添加节点到 tabInfo
            AddRangeToCollection(tabInfo.WorkflowNodes, nodesToLoad);

            // 转换底层连接到 UI 连接（先收集，再批量添加）
            var connectionsToLoad = new List<WorkflowConnection>();
            foreach (var conn in workflow.Connections)
            {
                var connection = new WorkflowConnection
                {
                    SourceNodeId = conn.SourceNode,
                    TargetNodeId = conn.TargetNode,
                    SourcePort = conn.SourcePort,
                    TargetPort = conn.TargetPort
                };

                connectionsToLoad.Add(connection);
            }

            // 批量添加连接到 tabInfo
            AddRangeToCollection(tabInfo.WorkflowConnections, connectionsToLoad);

            // 创建 WorkflowTabViewModel
            var tabViewModel = new ViewModels.WorkflowTabViewModel();

            // 设置属性
            tabViewModel.Name = tabInfo.Name;

            // 将节点和连接复制到新的 ViewModel（批量添加）
            AddRangeToCollection(tabViewModel.WorkflowNodes, tabInfo.WorkflowNodes);
            AddRangeToCollection(tabViewModel.WorkflowConnections, tabInfo.WorkflowConnections);

            // 添加到标签页视图模型
            WorkflowTabViewModel?.Tabs.Add(tabViewModel);
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
                // ★ 传递运行时参数到执行管理器
                _executionManager.SetRuntimeParameters(GetAllRuntimeParameters());

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
        }

        #region 运行时参数注入

        /// <summary>
        /// 设置运行时参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键名</param>
        /// <param name="value">参数值</param>
        public void SetRuntimeParameter<T>(string key, T value)
        {
            _runtimeParameters[key] = value!;
            AddLog($"📝 运行时参数已设置: {key} = {value}");
        }

        /// <summary>
        /// 获取运行时参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键名</param>
        /// <returns>参数值</returns>
        public T? GetRuntimeParameter<T>(string key)
        {
            if (_runtimeParameters.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return default;
        }

        /// <summary>
        /// 获取所有运行时参数
        /// </summary>
        /// <returns>运行时参数字典</returns>
        public Dictionary<string, object> GetAllRuntimeParameters()
        {
            return new Dictionary<string, object>(_runtimeParameters);
        }

        /// <summary>
        /// 清除运行时参数
        /// </summary>
        public void ClearRuntimeParameters()
        {
            _runtimeParameters.Clear();
        }

        #endregion

        /// <summary>
        /// 加载节点属性（完整实现）
        /// </summary>
        public void LoadNodePropertiesFull(Models.WorkflowNode? node)
        {
            PropertyGroups.Clear();

            if (node == null)
            {
                return;
            }

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

            AddLog($"➕ 添加节点: {node.Name} (ID={node.Id}, 类型={node.AlgorithmType})");

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

            AddLog($"➖ 删除节点: {node.Name} (ID={node.Id})");

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

            // 获取节点名称用于日志
            var sourceNode = WorkflowTabViewModel.SelectedTab.WorkflowNodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = WorkflowTabViewModel.SelectedTab.WorkflowNodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
            AddLog($"🔗 创建连接: {sourceNode?.Name ?? connection.SourceNodeId} → {targetNode?.Name ?? connection.TargetNodeId}");

            var command = new AppCommands.AddConnectionCommand(WorkflowTabViewModel.SelectedTab.WorkflowConnections, connection);
            WorkflowTabViewModel.SelectedTab.CommandManager.Execute(command);
        }

        /// <summary>
        /// 从当前工作流删除连接（通过命令模式）
        /// </summary>
        public void DeleteConnectionFromWorkflow(WorkflowConnection connection)
        {
            if (WorkflowTabViewModel.SelectedTab == null)
            {
                return;
            }

            // 获取节点名称用于日志
            var sourceNode = WorkflowTabViewModel.SelectedTab.WorkflowNodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = WorkflowTabViewModel.SelectedTab.WorkflowNodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
            AddLog($"⛓️ 删除连接: {sourceNode?.Name ?? connection.SourceNodeId} → {targetNode?.Name ?? connection.TargetNodeId}");

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
                    // 从 ToolRegistry 获取元数据
                    var toolId = node.AlgorithmType ?? node.Name;
                    var toolMetadata = ToolRegistry.GetToolMetadata(toolId);

                    AddLog($"🔧 尝试打开调试窗口：工具ID={toolId}，节点名称={node.Name}");

                    if (toolMetadata == null)
                    {
                        AddLog($"❌ 未找到工具 '{toolId}' 的元数据信息");
                        System.Windows.MessageBox.Show(
                            $"未找到工具 '{toolId}' 的元数据信息",
                            "未找到工具",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    AddLog($"✅ 找到工具元数据：{toolMetadata.DisplayName}，HasDebugInterface={toolMetadata.HasDebugInterface}");

                    // 获取工具实例用于运行时检查
                    var tool = ToolRegistry.CreateToolInstance(toolId);
                    if (tool == null)
                    {
                        AddLog($"❌ 无法创建工具实例");
                        System.Windows.MessageBox.Show(
                            $"无法创建工具 '{toolId}' 的实例",
                            "无法创建工具",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }

                    AddLog($"✅ 创建工具实例成功：{tool.GetType().Name}，HasDebugWindow={tool.HasDebugWindow}");

                    // 使用 NodeInterfaceFactory 获取界面类型（运行时检查版本）
                    var interfaceType = NodeInterfaceFactory.GetInterfaceType(node.ToWorkflowNode(), toolMetadata, tool);
                    AddLog($"🔍 界面类型：{interfaceType}");

                    switch (interfaceType)
                    {
                        case NodeInterfaceType.DebugWindow:
                            // 全局单例模式：有且只能有一个调试窗口被打开
                            if (_openDebugWindow != null && _openDebugWindow.IsLoaded)
                            {
                                // 已有窗口，询问用户是否切换
                                var result = System.Windows.MessageBox.Show(
                                    $"调试窗口已打开（{_openDebugWindow.Title}）\n\n是否关闭当前窗口并打开新的调试窗口？",
                                    "调试窗口已存在",
                                    System.Windows.MessageBoxButton.YesNo,
                                    System.Windows.MessageBoxImage.Question);

                                if (result != System.Windows.MessageBoxResult.Yes)
                                {
                                    // 用户选择不切换，激活现有窗口
                                    _openDebugWindow.Activate();
                                    _openDebugWindow.Focus();
                                    break;
                                }

                                // 关闭现有窗口
                                _openDebugWindow.Close();
                                _openDebugWindow = null;
                            }

                            // 创建新的调试窗口
                            var mainImageControl = GetMainImageControl?.Invoke();
                            var debugWindow = ToolDebugWindowFactory.CreateDebugWindow(toolId, tool, toolMetadata, mainImageControl);
                            
                            AddLog($"🔨 创建调试窗口：{debugWindow?.GetType().Name ?? "null"}");
                            
                            if (debugWindow != null)
                            {
                                debugWindow.Owner = System.Windows.Application.Current.MainWindow;
                                debugWindow.Title = $"{node.Name} - 调试窗口";

                                // 注入前驱节点数据
                                InjectParentNodesToDebugWindow(debugWindow, node);

                                // 注册关闭事件，窗口关闭时清理引用
                                debugWindow.Closed += (s, e) =>
                                {
                                    _openDebugWindow = null;
                                    AddLog($"🔧 关闭调试窗口: {node.Name}");
                                };

                                _openDebugWindow = debugWindow;
                                debugWindow.Show();
                                AddLog($"✅ 调试窗口已打开: {node.Name}");
                            }
                            else
                            {
                                // 窗口创建失败或工具不支持调试窗口
                                AddLog($"⚠️ 工具 '{node.Name}' 无调试窗口或创建失败");
                            }
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
        /// 使用 BFS 递归查找所有上游节点
        /// </summary>
        /// <param name="tab">当前工作流标签页</param>
        /// <param name="nodeId">起始节点ID</param>
        /// <returns>上游节点列表，按距离排序（最近的在前）</returns>
        private List<(Models.WorkflowNode Node, int Distance)> FindAllUpstreamNodes(
            ViewModels.WorkflowTabViewModel tab, string nodeId)
        {
            var result = new List<(Models.WorkflowNode Node, int Distance)>();
            var visited = new HashSet<string>();
            var queue = new Queue<(string NodeId, int Distance)>();
            
            // 从当前节点开始，查找所有前驱
            queue.Enqueue((nodeId, 0));
            visited.Add(nodeId);
            
            while (queue.Count > 0)
            {
                var (currentNodeId, distance) = queue.Dequeue();
                
                // 查找当前节点的所有直接前驱连接
                var parentConnections = tab.WorkflowConnections
                    .Where(conn => conn.TargetNodeId == currentNodeId)
                    .ToList();
                
                foreach (var connection in parentConnections)
                {
                    if (!visited.Contains(connection.SourceNodeId))
                    {
                        visited.Add(connection.SourceNodeId);
                        
                        var parentNode = tab.WorkflowNodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                        if (parentNode != null)
                        {
                            // 添加到结果（距离 +1）
                            result.Add((parentNode, distance + 1));
                            
                            // 继续向上游查找
                            queue.Enqueue((connection.SourceNodeId, distance + 1));
                        }
                    }
                }
            }
            
            // 按距离排序（最近的在前）
            return result.OrderBy(x => x.Distance).ToList();
        }

        /// <summary>
        /// 更新图像显示控件的数据源（当前节点 + 所有父节点）
        /// </summary>
        /// <param name="selectedNode">当前选中的节点</param>
        private void UpdateDisplayImageSources(Models.WorkflowNode? selectedNode)
        {
            DisplayImageSources.Clear();

            if (selectedNode == null)
            {
                SelectedDisplayImageSourceIndex = -1;
                return;
            }

            // 获取当前工作流标签页
            var selectedTab = WorkflowTabViewModel?.SelectedTab;
            if (selectedTab == null)
            {
                SelectedDisplayImageSourceIndex = -1;
                return;
            }

            // 1. 首先添加当前节点（距离=0）
            if (selectedNode.OutputCache != null && selectedNode.OutputCache.HasOutput)
            {
                // 节点有输出，添加到列表
                var outputCache = selectedNode.OutputCache;
                foreach (var imageName in outputCache.GetImageNames())
                {
                    DisplayImageSources.Add(new ImageSourceInfo
                    {
                        NodeId = selectedNode.Id,
                        NodeName = selectedNode.Name,
                        OutputPortName = imageName,
                        DataType = "Mat",
                        Distance = 0,
                        HasExecuted = true
                    });
                }
            }
            else if (selectedNode.LastResult != null && selectedNode.LastResult.IsSuccess)
            {
                // 从 LastResult 获取输出
                var outputValues = selectedNode.LastResult.GetOutputValues();
                if (outputValues != null)
                {
                    foreach (var kvp in outputValues.Where(kv => kv.Value is OpenCvSharp.Mat))
                    {
                        DisplayImageSources.Add(new ImageSourceInfo
                        {
                            NodeId = selectedNode.Id,
                            NodeName = selectedNode.Name,
                            OutputPortName = kvp.Key,
                            DataType = "Mat",
                            Distance = 0,
                            HasExecuted = true
                        });
                    }
                }
            }
            else
            {
                // 节点未执行，清空图像显示并添加占位项
                DisplayImage = null;
                OriginalImage = null;
                ProcessedImage = null;
                ResultImage = null;
                
                DisplayImageSources.Add(new ImageSourceInfo
                {
                    NodeId = selectedNode.Id,
                    NodeName = $"{selectedNode.Name} (未执行)",
                    OutputPortName = "Output",
                    DataType = InferNodeOutputType(selectedNode),
                    Distance = 0,
                    HasExecuted = false
                });
            }

            // 2. 使用 BFS 查找所有父节点
            var allUpstreamNodes = FindAllUpstreamNodes(selectedTab, selectedNode.Id);

            // 3. 添加所有父节点
            foreach (var (parentNode, distance) in allUpstreamNodes)
            {
                bool hasOutput = false;

                // 检查节点是否有输出
                if (parentNode.OutputCache != null && parentNode.OutputCache.HasOutput)
                {
                    var outputCache = parentNode.OutputCache;
                    foreach (var imageName in outputCache.GetImageNames())
                    {
                        DisplayImageSources.Add(new ImageSourceInfo
                        {
                            NodeId = parentNode.Id,
                            NodeName = parentNode.Name,
                            OutputPortName = imageName,
                            DataType = "Mat",
                            Distance = distance,
                            HasExecuted = true
                        });
                        hasOutput = true;
                    }
                }
                else if (parentNode.LastResult != null && parentNode.LastResult.IsSuccess)
                {
                    var outputValues = parentNode.LastResult.GetOutputValues();
                    if (outputValues != null)
                    {
                        foreach (var kvp in outputValues.Where(kv => kv.Value is OpenCvSharp.Mat))
                        {
                            DisplayImageSources.Add(new ImageSourceInfo
                            {
                                NodeId = parentNode.Id,
                                NodeName = parentNode.Name,
                                OutputPortName = kvp.Key,
                                DataType = "Mat",
                                Distance = distance,
                                HasExecuted = true
                            });
                            hasOutput = true;
                        }
                    }
                }

                // 如果没有输出，添加占位项
                if (!hasOutput)
                {
                    DisplayImageSources.Add(new ImageSourceInfo
                    {
                        NodeId = parentNode.Id,
                        NodeName = $"{parentNode.Name} (未执行)",
                        OutputPortName = "Output",
                        DataType = InferNodeOutputType(parentNode),
                        Distance = distance,
                        HasExecuted = false
                    });
                }
            }

            // 4. 默认选中第一项（当前节点的第一个输出）
            if (DisplayImageSources.Count > 0)
            {
                SelectedDisplayImageSourceIndex = 0;
                AddLog($"📷 已更新图像显示数据源，共 {DisplayImageSources.Count} 项");
            }
            else
            {
                SelectedDisplayImageSourceIndex = -1;
            }
        }

        /// <summary>
        /// 注入前驱节点数据到调试窗口
        /// </summary>
        /// <param name="debugWindow">调试窗口实例</param>
        /// <param name="currentNode">当前节点</param>
        private void InjectParentNodesToDebugWindow(System.Windows.Window debugWindow, Models.WorkflowNode currentNode)
        {
            try
            {
                // 检查窗口是否有 SetDataProvider 方法
                var setDataProviderMethod = debugWindow.GetType().GetMethod("SetDataProvider");
                if (setDataProviderMethod == null)
                {
                    AddLog($"⚠️ 调试窗口 '{currentNode.Name}' 不支持数据提供者注入");
                    return;
                }

                // 获取当前工作流标签页
                var selectedTab = WorkflowTabViewModel?.SelectedTab;
                if (selectedTab == null)
                {
                    AddLog("⚠️ 没有选中的工作流标签页，无法注入前驱节点");
                    return;
                }

                // 创建数据提供者
                var dataProvider = new SunEyeVision.Plugin.SDK.UI.Controls.Region.Models.WorkflowDataSourceProvider
                {
                    CurrentNodeId = currentNode.Id
                };

                // ★ 使用 BFS 递归查找所有上游节点（而不仅仅是直接父节点）
                // 结果按距离排序：最近的父节点排在前面
                var allUpstreamNodes = FindAllUpstreamNodes(selectedTab, currentNode.Id);
                
                AddLog($"📋 查找到 {allUpstreamNodes.Count} 个上游节点");

                // 注册所有上游节点（按距离排序，最近的在前）
                foreach (var (parentNode, distance) in allUpstreamNodes)
                {
                    // 根据节点类型推断输出类型
                    string outputType = InferNodeOutputType(parentNode);

                    // 注册前驱节点信息
                    dataProvider.RegisterParentNode(parentNode.Id, parentNode.Name, outputType);
                    AddLog($"  📌 注册上游节点: {parentNode.Name} (距离: {distance}, 类型: {outputType})");

                    // 如果节点有执行结果，注入输出数据
                    if (parentNode.LastResult != null && parentNode.LastResult.IsSuccess)
                    {
                        var outputValues = parentNode.LastResult.GetOutputValues();
                        if (outputValues != null && outputValues.Count > 0)
                        {
                            // 尝试找到图像类型的输出（通常是第一个或名为 OutputImage 的属性）
                            var imageOutput = outputValues.FirstOrDefault(kv => 
                                kv.Value is OpenCvSharp.Mat || 
                                kv.Key.Contains("Image", StringComparison.OrdinalIgnoreCase) ||
                                kv.Key.Contains("Output", StringComparison.OrdinalIgnoreCase));

                            if (imageOutput.Value != null)
                            {
                                dataProvider.UpdateNodeOutput(parentNode.Id, imageOutput.Value);
                                AddLog($"  ✅ 注入节点输出: {parentNode.Name}.{imageOutput.Key}");
                            }
                            else
                            {
                                // 使用第一个非空输出
                                var firstOutput = outputValues.FirstOrDefault(kv => kv.Value != null);
                                if (firstOutput.Value != null)
                                {
                                    dataProvider.UpdateNodeOutput(parentNode.Id, firstOutput.Value);
                                    AddLog($"  ✅ 注入节点输出: {parentNode.Name}.{firstOutput.Key}");
                                }
                            }
                        }
                    }
                }

                // ★ 设置当前节点引用（用于配置持久化）- 必须在 SetDataProvider 之前调用！
                // 因为 SetDataProvider 内部会调用 RestoreImageSourceSelection，需要 _currentNode 已设置
                var setCurrentNodeMethod = debugWindow.GetType().GetMethod("SetCurrentNode");
                setCurrentNodeMethod?.Invoke(debugWindow, new object[] { currentNode });

                // 调用 SetDataProvider 方法（此时会恢复配置，_currentNode 已就绪）
                setDataProviderMethod.Invoke(debugWindow, new object[] { dataProvider });
                AddLog($"✅ 已注入 {allUpstreamNodes.Count} 个上游节点到调试窗口");

                // ★ 缓存数据提供者（用于在节点执行完成后更新前置节点输出）
                _nodeDataProviders[currentNode.Id] = dataProvider;

                // ★ 订阅工具执行完成事件（用于更新图像显示）
                SubscribeToolExecutionCompleted(debugWindow, currentNode, dataProvider);

                // 窗口关闭时清理缓存
                debugWindow.Closed += (s, e) =>
                {
                    _nodeDataProviders.Remove(currentNode.Id);
                    AddLog($"🗑️ 已清理节点 {currentNode.Name} 的数据提供者缓存");
                };
            }
            catch (Exception ex)
            {
                AddLog($"❌ 注入前驱节点失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 订阅调试窗口的执行完成事件
        /// </summary>
        private void SubscribeToolExecutionCompleted(System.Windows.Window debugWindow, Models.WorkflowNode node, 
            SunEyeVision.Plugin.SDK.UI.Controls.Region.Models.WorkflowDataSourceProvider dataProvider)
        {
            var eventInfo = debugWindow.GetType().GetEvent("ToolExecutionCompleted");
            if (eventInfo != null)
            {
                // 创建事件处理器（使用 Delegate.CreateDelegate 动态创建）
                var eventHandlerType = eventInfo.EventHandlerType;
                
                // 创建委托方法
                var invokeMethod = eventHandlerType.GetMethod("Invoke");
                var parameters = invokeMethod?.GetParameters();
                
                if (parameters != null && parameters.Length == 2)
                {
                    // 创建通用的处理方法
                    Action<object?, object?> handlerAction = (sender, result) =>
                    {
                        try
                        {
                            if (result is SunEyeVision.Plugin.SDK.Execution.Results.ToolResults toolResult)
                            {
                                // 更新节点结果
                                node.LastResult = toolResult;

                                // 获取输出图像（通过反射调用 GetOutputValue 方法）
                                var getOutputMethod = toolResult.GetType().GetMethod("GetOutputValue");
                                var outputImage = getOutputMethod?.MakeGenericMethod(typeof(OpenCvSharp.Mat))
                                    .Invoke(toolResult, new object[] { "OutputImage" }) as OpenCvSharp.Mat;

                                // 更新数据提供者中的节点输出
                                if (outputImage != null)
                                {
                                    dataProvider.UpdateNodeOutput(node.Id, outputImage);
                                }

                                // 更新图像显示
                                _nodeResultManager?.UpdateNodeResult(node, toolResult);

                                AddLog($"✅ 调试窗口执行完成: {node.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddLog($"⚠️ 处理执行结果失败: {ex.Message}");
                        }
                    };

                    // 创建委托
                    var handler = Delegate.CreateDelegate(eventHandlerType, handlerAction.Target, handlerAction.Method);
                    
                    // 订阅事件
                    eventInfo.AddEventHandler(debugWindow, handler);

                    // 窗口关闭时取消订阅（防止内存泄漏）
                    debugWindow.Closed += (s, e) =>
                    {
                        eventInfo.RemoveEventHandler(debugWindow, handler);
                    };
                }
            }
        }

        /// <summary>
        /// 推断节点输出类型
        /// </summary>
        private string InferNodeOutputType(Models.WorkflowNode node)
        {
            // 根据算法类型推断
            var algorithmType = node.AlgorithmType?.ToLower() ?? node.Name.ToLower();

            if (algorithmType.Contains("image") || algorithmType.Contains("capture") || 
                algorithmType.Contains("load") || algorithmType.Contains("采集") || algorithmType.Contains("载入"))
            {
                return "Mat";
            }
            else if (algorithmType.Contains("threshold") || algorithmType.Contains("阈值"))
            {
                return "Mat";
            }
            else if (algorithmType.Contains("gray") || algorithmType.Contains("灰度"))
            {
                return "Mat";
            }
            else if (algorithmType.Contains("blur") || algorithmType.Contains("模糊"))
            {
                return "Mat";
            }
            else if (algorithmType.Contains("edge") || algorithmType.Contains("边缘"))
            {
                return "Mat";
            }
            else if (algorithmType.Contains("morphology") || algorithmType.Contains("形态"))
            {
                return "Mat";
            }
            else if (algorithmType.Contains("region") || algorithmType.Contains("区域"))
            {
                return "RegionData";
            }

            // 默认返回 Mat 类型
            return "Mat";
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
        /// 节点执行完成事件 - 更新节点结果并刷新UI
        /// </summary>
        private void OnNodeExecutionCompleted(object? sender, NodeExecutionResultEventArgs e)
        {
            try
            {
                // ★ 关键日志：节点执行完成
                var outputValues = e.Result?.GetOutputValues();
                bool hasImageOutput = outputValues?.Any(kv => kv.Value is OpenCvSharp.Mat) ?? false;
                AddLog($"✅ 节点执行完成: {e.Node.Name}, 成功={e.Result?.IsSuccess}, 有图像输出={hasImageOutput}");

                // 通过 NodeResultManager 更新节点结果（更新 OutputCache，不修改 InputSource）
                _nodeResultManager.UpdateNodeResult(e.Node, e.Result);

                // ★ 更新所有依赖此节点的数据提供者（修复前置节点未执行问题）
                UpdateDataProvidersForNode(e.Node, e.Result);

                // 如果节点是当前显示节点，刷新输出缓存绑定（不刷新输入源）
                // 新架构：OutputCache 存储执行结果，InputSource 保持不变
                if (_currentDisplayNodeId == e.Node.Id)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 新架构：刷新 OutputCache（如果有输出）
                        var outputCache = e.Node.OutputCache;
                        if (outputCache != null && outputCache.HasOutput)
                        {
                            AddLog($"📷 节点 {e.Node.Name} 输出已更新, 输出图像数={outputCache.Count}");
                        }
                        
                        // 注意：不再刷新 ActiveInputSource，因为输入源不应该被执行结果修改
                        // 向后兼容：刷新 ImageData（已过时）
#pragma warning disable CS0618
                        if (e.Node.ImageData != null)
                        {
                            var temp = e.Node.ImageData;
                            ActiveNodeImageData = null;
                            ActiveNodeImageData = temp;
                            AddLog($"📷 [兼容] 已刷新节点 {e.Node.Name} 的图像预览, 图像数={temp.ImageCount}");
                        }
#pragma warning restore CS0618
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindowViewModel] OnNodeExecutionCompleted异常: {ex.Message}");
                AddLog($"⚠️ 节点结果更新异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新所有依赖此节点的数据提供者
        /// </summary>
        private void UpdateDataProvidersForNode(Models.WorkflowNode node, Plugin.SDK.Execution.Results.ToolResults? result)
        {
            if (result == null || !result.IsSuccess) return;

            try
            {
                // 获取节点的输出图像
                var outputValues = result.GetOutputValues();
                if (outputValues == null || outputValues.Count == 0)
                {
                    AddLog($"⚠️ 节点 {node.Name} 无输出值");
                    return;
                }

                // 尝试找到图像类型的输出
                var imageOutput = outputValues.FirstOrDefault(kv =>
                    kv.Value is OpenCvSharp.Mat ||
                    kv.Key.Contains("Image", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains("Output", StringComparison.OrdinalIgnoreCase));

                object? outputValue = imageOutput.Value ?? outputValues.FirstOrDefault(kv => kv.Value != null).Value;
                if (outputValue == null)
                {
                    AddLog($"⚠️ 节点 {node.Name} 输出值为空");
                    return;
                }

                // ★ 关键日志：输出值类型
                AddLog($"📤 节点 {node.Name} 输出类型: {outputValue.GetType().Name}");

                // 更新所有缓存的数据提供者
                int updatedCount = 0;
                foreach (var kvp in _nodeDataProviders.ToList())
                {
                    try
                    {
                        kvp.Value.UpdateNodeOutput(node.Id, outputValue);
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] 更新数据提供者失败: {ex.Message}");
                    }
                }
                if (updatedCount > 0)
                {
                    AddLog($"🔄 已更新 {updatedCount} 个数据提供者");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] UpdateDataProvidersForNode异常: {ex.Message}");
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
        /// 更新计算结果（支持字典形式）
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

        /// <summary>
        /// 更新计算结果（支持 ResultItem 列表）
        /// </summary>
        public void UpdateCalculationResults(IReadOnlyList<SunEyeVision.Plugin.SDK.Execution.Results.ResultItem> resultItems)
        {
            CalculationResults.Clear();

            if (resultItems == null || resultItems.Count == 0)
                return;

            foreach (var item in resultItems)
            {
                string displayValue = FormatResultValue(item);
                CalculationResults.Add(new ResultItem
                {
                    Name = item.DisplayName ?? item.Name,
                    Value = displayValue
                });
            }
        }

        /// <summary>
        /// 清空计算结果面板
        /// </summary>
        public void ClearCalculationResults()
        {
            CalculationResults.Clear();
            AddLog("已清空信息面板");
        }

        /// <summary>
        /// 格式化结果项值
        /// </summary>
        private string FormatResultValue(SunEyeVision.Plugin.SDK.Execution.Results.ResultItem item)
        {
            if (item.Value == null) return "null";

            return item.Type switch
            {
                SunEyeVision.Plugin.SDK.Execution.Results.ResultItemType.Numeric => item.Unit != null
                    ? $"{item.Value} {item.Unit}"
                    : item.Value?.ToString() ?? "",
                SunEyeVision.Plugin.SDK.Execution.Results.ResultItemType.Boolean => (bool)item.Value ? "✓ 是" : "✗ 否",
                SunEyeVision.Plugin.SDK.Execution.Results.ResultItemType.Text => item.Value?.ToString() ?? "",
                _ => item.Value?.ToString() ?? ""
            };
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
        /// 重构说明:
        /// - 新架构使用 InputSource（输入源）替代 ImageData
        /// - 输入源存储用户选择的图像，执行流程不会修改
        /// - 向后兼容：同时更新 ImageData
        /// 
        /// 性能优化:
        /// - 只在状态真正变化时才更新属性，避免触发不必要的绑定刷新
        /// - 引用相等检测在 ActiveInputSource setter 中处理
        /// </remarks>
        public void UpdateImagePreviewVisibility(Models.WorkflowNode? selectedNode)
        {
            // 1.没有选中节点时
            if (selectedNode == null)
            {
                // 只在当前状态不一致时才更新
                if (ShowImagePreview || ActiveInputSource != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveInputSource = null;
#pragma warning disable CS0618 // 向后兼容
                    ActiveNodeImageData = null;
#pragma warning restore CS0618
                    _currentDisplayNodeId = null;
                }
                return;
            }

            // 2.选中图像载入节点时，初始化显示图像预览（因为需要选择本地文件）
            if (selectedNode.IsImageLoadNode)
            {
                // 新架构：确保 InputSource 已初始化
                var inputSource = selectedNode.EnsureInputSource();
                
#pragma warning disable CS0618 // 向后兼容
                // 向后兼容：确保 ImageData 已初始化
                selectedNode.ImageData ??= new Models.NodeImageData(selectedNode.Id);
#pragma warning restore CS0618

                // 检查是否是同一节点
                bool isSameNode = _currentDisplayNodeId == selectedNode.Id;

                if (!isSameNode)
                {
                    // 不同节点才更新
                    _currentDisplayNodeId = selectedNode.Id;
                    inputSource.PrepareForDisplay();
                    ActiveInputSource = inputSource;
#pragma warning disable CS0618 // 向后兼容
                    selectedNode.ImageData.PrepareForDisplay();
                    ActiveNodeImageData = selectedNode.ImageData;
#pragma warning restore CS0618
                }

                // 只有真正变化时才设置
                if (!ShowImagePreview)
                {
                    ShowImagePreview = true;
                }
                return;
            }

            // 2.5 选中图像采集节点时，隐藏图像预览（相机实时采集不需要预览器）
            if (selectedNode.IsImageCaptureNode)
            {
                if (ShowImagePreview || ActiveInputSource != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveInputSource = null;
#pragma warning disable CS0618 // 向后兼容
                    ActiveNodeImageData = null;
#pragma warning restore CS0618
                    _currentDisplayNodeId = null;
                }
                return;
            }

            // 3.选中处理节点时，检查连接和上游图像载入节点
            // 核心原则：预览器必须绑定到图像载入节点的InputSource
            
            var connections = WorkflowTabViewModel?.SelectedTab?.WorkflowConnections;
            
            // 3.1 检查当前节点是否有输入连接
            bool hasInputConnection = connections?.Any(c => c.TargetNodeId == selectedNode.Id) ?? false;
            
            if (!hasInputConnection)
            {
                // 没有输入连接，隐藏预览器
                if (ShowImagePreview || ActiveInputSource != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveInputSource = null;
#pragma warning disable CS0618 // 向后兼容
                    ActiveNodeImageData = null;
#pragma warning restore CS0618
                    _currentDisplayNodeId = null;
                }
                return;
            }
            
            // 3.2 有输入连接，尝试逆向查找图像载入节点
            var sourceLoadNode = FindUpstreamImageLoadNode(selectedNode);

            if (sourceLoadNode != null)
            {
                // 找到上游图像载入节点，显示预览器（绑定到该节点）
                var sourceInputSource = sourceLoadNode.InputSource ?? sourceLoadNode.EnsureInputSource();
                
                bool isSameNode = _currentDisplayNodeId == sourceLoadNode.Id;
                
                if (!isSameNode)
                {
                    _currentDisplayNodeId = sourceLoadNode.Id;
                    sourceInputSource.PrepareForDisplay();
                    ActiveInputSource = sourceInputSource;
#pragma warning disable CS0618 // 向后兼容
                    var sourceImageData = sourceLoadNode.ImageData;
                    sourceImageData?.PrepareForDisplay();
                    ActiveNodeImageData = sourceImageData;
#pragma warning restore CS0618
                }
                
                if (!ShowImagePreview)
                {
                    ShowImagePreview = true;
                }
            }
            else
            {
                // 找不到上游图像载入节点，隐藏预览器（没有数据源可绑定）
                if (ShowImagePreview || ActiveInputSource != null || _currentDisplayNodeId != null)
                {
                    ShowImagePreview = false;
                    ActiveInputSource = null;
#pragma warning disable CS0618 // 向后兼容
                    ActiveNodeImageData = null;
#pragma warning restore CS0618
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
        /// 逆向追踪选中节点的图像载入节点（BFS）
        /// </summary>
        /// <remarks>
        /// 当存在多个载入节点时返回第一个找到的载入节点:
        /// 1. BFS保证路径最短
        /// 2. 节点ID排序保证选择一致
        /// </remarks>
        /// <param name="node">起始节点</param>
        /// <returns>第一个找到的图像载入节点，未找到返回null</returns>
        private Models.WorkflowNode? FindUpstreamImageLoadNode(Models.WorkflowNode node)
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

                    // 找到图像载入节点，返回（第一个找到的）
                    if (upstreamNode.IsImageLoadNode)
                    {
                        return upstreamNode;
                    }

                    // 不是载入节点，继续追踪
                    visited.Add(upstreamNodeId);
                    queue.Enqueue(upstreamNode);
                }
            }

            return null;
        }


        #endregion

        #endregion // 显示

        #region 解决方案管理

        /// <summary>
        /// 显示解决方案配置界面
        /// </summary>
        private void ExecuteShowSolutionConfiguration()
        {
            try
            {
                LogInfo("打开解决方案配置界面");

                var solutionManager = Adapters.ServiceInitializer.SolutionManager;
                var configDialog = new Views.Windows.SolutionConfigurationDialog(solutionManager);

                var result = configDialog.ShowDialog();

                if (result == true && configDialog.IsLaunched && configDialog.LaunchResult != null)
                {
                    // 用户点击启动，加载解决方案
                    var solution = configDialog.LaunchResult;

                    try
                    {
                        // TODO: 加载解决方案到工作流编辑器
                        var metadata = solutionManager.GetMetadata(solution.Id);
                        LogSuccess($"已加载解决方案: {metadata?.Name ?? "未命名"}");

                        // 更新标题
                        Title = $"太阳眼视觉 - {metadata?.Name ?? "未命名"}";
                    }
                    catch (Exception ex)
                    {
                        LogError($"加载解决方案失败: {ex.Message}", null, ex);
                        MessageBox.Show(
                            $"加载解决方案失败: {ex.Message}",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("打开解决方案配置界面失败", null, ex);
            }
        }

        /// <summary>
        /// 切换项目
        /// </summary>
        private void ExecuteSwitchProject()
        {
            try
            {
                LogInfo("切换解决方案");

                var solutionManager = Adapters.ServiceInitializer.SolutionManager;

                var configDialog = new Views.Windows.SolutionConfigurationDialog(solutionManager, null);

                var result = configDialog.ShowDialog();

                if (result == true && configDialog.IsLaunched && configDialog.LaunchResult != null)
                {
                    var solution = configDialog.LaunchResult;

                    try
                    {
                        solutionManager.SetCurrentSolution(solution);
                        var metadata = solutionManager.GetMetadata(solution.Id);
                        LogSuccess($"已切换到解决方案: {metadata?.Name ?? "未命名"}");

                        // 更新标题
                        Title = $"太阳眼视觉 - {metadata?.Name ?? "未命名"}";
                    }
                    catch (Exception ex)
                    {
                        LogError($"切换解决方案失败: {ex.Message}", null, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("切换项目失败", null, ex);
            }
        }

        /// <summary>
        /// 显示全局变量管理器
        /// </summary>
        private void ExecuteShowGlobalVariableManager()
        {
            try
            {
                LogInfo("打开全局变量管理器");

                var solutionManager = Adapters.ServiceInitializer.SolutionManager;
                var currentSolution = solutionManager.CurrentSolution;

                if (currentSolution == null)
                {
                    MessageBox.Show("请先打开一个解决方案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var viewModel = new ViewModels.GlobalVariableManagerViewModel(solutionManager);
                var dialog = new Views.Windows.GlobalVariableManagerDialog(viewModel);

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError($"打开全局变量管理器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示相机管理器
        /// </summary>
        private void ExecuteShowCameraManager()
        {
            try
            {
                LogInfo("打开相机管理器");

                var solutionManager = Adapters.ServiceInitializer.SolutionManager;
                var currentSolution = solutionManager.CurrentSolution;

                if (currentSolution == null)
                {
                    MessageBox.Show("请先打开一个解决方案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var viewModel = new ViewModels.CameraManagerViewModel(solutionManager);
                var dialog = new Views.Windows.CameraManagerDialog(viewModel);

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError($"打开相机管理器失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示通讯管理器
        /// </summary>
        private void ExecuteShowCommunicationManager()
        {
            try
            {
                LogInfo("打开通讯管理器");

                var solutionManager = Adapters.ServiceInitializer.SolutionManager;
                var currentSolution = solutionManager.CurrentSolution;

                if (currentSolution == null)
                {
                    MessageBox.Show("请先打开一个解决方案", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var viewModel = new ViewModels.CommunicationManagerViewModel(solutionManager);
                var dialog = new Views.Windows.CommunicationManagerDialog(viewModel);

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError($"打开通讯管理器失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 默认工具插件 - 用于测试
        /// </summary>
        private class DefaultToolPlugin : IToolPlugin
        {
            public Type ParamsType => typeof(GenericToolParameters);
            public Type ResultType => typeof(GenericToolResults);

            public ToolResults Run(OpenCvSharp.Mat image, ToolParameters parameters)
            {
                return new GenericToolResults();
            }

            public bool HasDebugWindow => false;
            public System.Windows.Window? CreateDebugWindow() => null;
            public ToolParameters GetDefaultParameters() => new GenericToolParameters();
        }
    }
}
