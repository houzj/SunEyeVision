using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
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
        private string _status = "就绪";
        private Models.WorkflowInfo? _currentWorkflow;

        /// <summary>
        /// 工作流切换事件
        /// </summary>
        public event EventHandler<string>? WorkflowSwitched;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public Models.WorkflowInfo? CurrentWorkflow
        {
            get => _currentWorkflow;
            set
            {
                if (SetProperty(ref _currentWorkflow, value))
                {
                    UpdateStatus();
                    // 触发工作流切换事件
                    if (value != null)
                    {
                        WorkflowSwitched?.Invoke(this, value.Name);
                    }
                }
            }
        }

        public ObservableCollection<Models.WorkflowInfo> Workflows { get; }

        public ObservableCollection<Models.ToolItem> Tools { get; }
        public ToolboxViewModel Toolbox { get; }
        public ObservableCollection<Models.WorkflowNode> WorkflowNodes { get; }
        public ObservableCollection<Models.WorkflowConnection> WorkflowConnections { get; }

        public Models.WorkflowNode? SelectedNode { get; set; }
        public WorkflowViewModel WorkflowViewModel { get; set; }

        public string StatusText
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private void UpdateStatus()
        {
            if (CurrentWorkflow != null)
            {
                StatusText = CurrentWorkflow.IsRunning
                    ? $"工作流 '{CurrentWorkflow.Name}' 运行中 ({(CurrentWorkflow.RunMode == RunMode.Single ? "单次" : "连续")}模式)"
                    : $"就绪 - 工作流: {CurrentWorkflow.Name}";
            }
            else
            {
                StatusText = "就绪";
            }
        }

        public string CameraStatus => "已连接 (2台)";

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
        public ICommand OpenDebugWindowCommand { get; }
        public ICommand ResetLayoutCommand { get; }

        /// <summary>
        /// 重置布局请求事件
        /// </summary>
        public event EventHandler? ResetLayoutRequested;

        public MainWindowViewModel()
        {
            Workflows = new ObservableCollection<Models.WorkflowInfo>();

            Tools = new ObservableCollection<Models.ToolItem>();
            Toolbox = new ToolboxViewModel();
            WorkflowNodes = new ObservableCollection<Models.WorkflowNode>();
            WorkflowConnections = new ObservableCollection<Models.WorkflowConnection>();

            WorkflowViewModel = new WorkflowViewModel();

            InitializeTools();
            InitializeSampleWorkflow();

            NewWorkflowCommand = new RelayCommand(ExecuteNewWorkflow);
            OpenWorkflowCommand = new RelayCommand(ExecuteOpenWorkflow);
            SaveWorkflowCommand = new RelayCommand(ExecuteSaveWorkflow);
            SaveAsWorkflowCommand = new RelayCommand(ExecuteSaveAsWorkflow);
            RunWorkflowCommand = new RelayCommand(ExecuteRunWorkflow);
            StopWorkflowCommand = new RelayCommand(ExecuteStopWorkflow);
            ShowSettingsCommand = new RelayCommand(ExecuteShowSettings);
            ShowAboutCommand = new RelayCommand(ExecuteShowAbout);
            ShowHelpCommand = new RelayCommand(ExecuteShowHelp);
            ShowShortcutsCommand = new RelayCommand(ExecuteShowShortcuts);
            PauseCommand = new RelayCommand(ExecutePause);
            UndoCommand = new RelayCommand(ExecuteUndo);
            RedoCommand = new RelayCommand(ExecuteRedo);
            OpenDebugWindowCommand = new RelayCommand<Models.WorkflowNode>(ExecuteOpenDebugWindow);
            ResetLayoutCommand = new RelayCommand(ExecuteResetLayout);
        }

        private void ExecutePause()
        {
            // TODO: 实现暂停功能
        }

        private void ExecuteUndo()
        {
            // TODO: 实现撤销功能
        }

        private void ExecuteRedo()
        {
            // TODO: 实现重做功能
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

        private void InitializeSampleWorkflow()
        {
            // 创建默认工作流（不包含任何示例节点）
            var defaultWorkflow = new Models.WorkflowInfo
            {
                Name = "默认工作流",
                RunMode = RunMode.Single
            };

            Workflows.Add(defaultWorkflow);
            CurrentWorkflow = defaultWorkflow;

            // 清空画布，确保没有任何节点和连接
            WorkflowNodes.Clear();
            WorkflowConnections.Clear();
        }

        private void ExecuteNewWorkflow()
        {
            var newWorkflow = new Models.WorkflowInfo
            {
                Name = $"工作流{Workflows.Count + 1}",
                RunMode = RunMode.Single
            };
            Workflows.Add(newWorkflow);
            CurrentWorkflow = newWorkflow;

            // 清空画布
            WorkflowNodes.Clear();
            WorkflowConnections.Clear();
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
            if (CurrentWorkflow != null)
            {
                if (CurrentWorkflow.RunMode == RunMode.Single)
                {
                    CurrentWorkflow.IsRunning = true;
                    UpdateStatus();
                    // TODO: 执行单次工作流
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        CurrentWorkflow.IsRunning = false;
                        UpdateStatus();
                    };
                    timer.Start();
                }
                else
                {
                    CurrentWorkflow.IsRunning = true;
                    UpdateStatus();
                    // TODO: 执行连续工作流
                }
            }
        }

        private void ExecuteStopWorkflow()
        {
            if (CurrentWorkflow != null)
            {
                CurrentWorkflow.IsRunning = false;
                UpdateStatus();
                // TODO: 停止工作流执行
            }
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

        private void ExecuteResetLayout()
        {
            ResetLayoutRequested?.Invoke(this, EventArgs.Empty);
            StatusText = "布局已重置";
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
