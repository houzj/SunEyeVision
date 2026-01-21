using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.UI.Models;

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

        public string SelectedWorkflowName
        {
            get => _selectedWorkflowName;
            set => SetProperty(ref _selectedWorkflowName, value);
        }

        public ObservableCollection<string> Workflows { get; }

        public ObservableCollection<Models.ToolItem> Tools { get; }
        public ObservableCollection<Models.WorkflowNode> WorkflowNodes { get; }
        public ObservableCollection<Models.WorkflowConnection> WorkflowConnections { get; }

        public Models.WorkflowNode? SelectedNode { get; set; }
        public WorkflowViewModel WorkflowViewModel { get; set; }

        public string StatusText
        {
            get => _status;
            set => SetProperty(ref _status, value);
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
            WorkflowNodes = new ObservableCollection<Models.WorkflowNode>();
            WorkflowConnections = new ObservableCollection<Models.WorkflowConnection>();

            WorkflowViewModel = new WorkflowViewModel();

            InitializeTools();
            InitializeSampleNodes();

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
            UndoCommand = new RelayCommand(ExecuteUndo);
            RedoCommand = new RelayCommand(ExecuteRedo);
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

        private void InitializeSampleNodes()
        {
            WorkflowNodes.Add(new Models.WorkflowNode("1", "图像采集_1", "ImageAcquisition")
            {
                Position = new System.Windows.Point(100, 100),
                IsSelected = false
            });

            WorkflowNodes.Add(new Models.WorkflowNode("2", "灰度化", "GrayScale")
            {
                Position = new System.Windows.Point(300, 100),
                IsSelected = false
            });

            WorkflowNodes.Add(new Models.WorkflowNode("3", "边缘检测", "EdgeDetection")
            {
                Position = new System.Windows.Point(500, 100),
                IsSelected = false
            });

            WorkflowConnections.Add(new Models.WorkflowConnection("conn_1", "1", "2")
            {
                SourcePosition = new System.Windows.Point(240, 145),
                TargetPosition = new System.Windows.Point(300, 145)
            });

            WorkflowConnections.Add(new Models.WorkflowConnection("conn_2", "2", "3")
            {
                SourcePosition = new System.Windows.Point(440, 145),
                TargetPosition = new System.Windows.Point(500, 145)
            });
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
    }
}
