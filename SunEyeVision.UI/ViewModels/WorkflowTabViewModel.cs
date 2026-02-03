using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using AppCommands = SunEyeVision.UI.Commands;
using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Commands;
using SunEyeVision.UI.Interfaces;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services;
using SunEyeVision.UI.Controls;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工作流标签页ViewModel
    /// </summary>
    public class WorkflowTabViewModel : ObservableObject
    {
        private string _id;
        private string _name;
        private bool _isRunning;
        private RunMode _runMode;
        private WorkflowState _state;
        private ObservableCollection<Models.WorkflowNode> _workflowNodes;
        private ObservableCollection<Models.WorkflowConnection> _workflowConnections;
        private ScaleTransform _scaleTransform;
        private double _currentScale;
        private CanvasType _canvasType;

        /// <summary>
        /// 节点序号管理器
        /// </summary>
        private readonly INodeSequenceManager _sequenceManager;

        /// <summary>
        /// 节点显示适配器
        /// </summary>
        private readonly INodeDisplayAdapter _displayAdapter;

        /// <summary>
        /// 节点工厂
        /// </summary>
        private readonly IWorkflowNodeFactory _nodeFactory;

        /// <summary>
        /// 每个画布独立的撤销/重做命令管理器
        /// </summary>
        public AppCommands.CommandManager CommandManager { get; }

        public WorkflowTabViewModel() : this(
            new NodeSequenceManager(),
            new DefaultNodeDisplayAdapter())
        {
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] ✅ 使用默认构造函数创建");
        }

        /// <summary>
        /// 带依赖注入的构造函数
        /// </summary>
        public WorkflowTabViewModel(INodeSequenceManager sequenceManager, INodeDisplayAdapter displayAdapter)
        {
            _sequenceManager = sequenceManager ?? throw new ArgumentNullException(nameof(sequenceManager));
            _displayAdapter = displayAdapter ?? throw new ArgumentNullException(nameof(displayAdapter));

            // 创建节点工厂
            _nodeFactory = new WorkflowNodeFactory(_sequenceManager, _displayAdapter);

            Id = Guid.NewGuid().ToString();
            Name = "工作流1";
            State = WorkflowState.Stopped;
            RunMode = RunMode.Single;
            WorkflowNodes = new ObservableCollection<Models.WorkflowNode>();
            WorkflowConnections = new ObservableCollection<Models.WorkflowConnection>();
            CurrentScale = 1.0;
            ScaleTransform = new ScaleTransform(1.0, 1.0);
            CanvasType = CanvasType.WorkflowCanvas; // 默认使用 WorkflowCanvas，每个工作流独立

            // 每个画布初始化独立的命令管理器
            CommandManager = new CommandManager(WorkflowNodes, WorkflowConnections);

            // 订阅节点和连接集合变化事件
            WorkflowNodes.CollectionChanged += (s, e) => OnWorkflowNodesChanged(s, e);
            WorkflowConnections.CollectionChanged += (s, e) => OnWorkflowConnectionsChanged(s, e);

            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] ════════════════════════════════════");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] ✅ 新建工作流: {Name}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   Id: {Id}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   CanvasType: {CanvasType}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   WorkflowNodes (Hash): {WorkflowNodes.GetHashCode()}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   WorkflowConnections (Hash): {WorkflowConnections.GetHashCode()}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   ScaleTransform (Hash): {ScaleTransform.GetHashCode()}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   CommandManager (Hash): {CommandManager.GetHashCode()}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] ════════════════════════════════════");
        }

        /// <summary>
        /// 工作流ID
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 工作流名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    State = value ? WorkflowState.Running : WorkflowState.Stopped;
                }
            }
        }

        /// <summary>
        /// 运行模式
        /// </summary>
        public RunMode RunMode
        {
            get => _runMode;
            set => SetProperty(ref _runMode, value);
        }

        /// <summary>
        /// 工作流状态
        /// </summary>
        public WorkflowState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        /// <summary>
        /// 工作流节点集合
        /// </summary>
        public ObservableCollection<Models.WorkflowNode> WorkflowNodes
        {
            get => _workflowNodes;
            set
            {
                if (_workflowNodes != null)
                {
                    _workflowNodes.CollectionChanged -= (s, e) => OnWorkflowNodesChanged(s, e);
                }
                SetProperty(ref _workflowNodes, value);
                if (_workflowNodes != null)
                {
                    _workflowNodes.CollectionChanged += (s, e) => OnWorkflowNodesChanged(s, e);
                }
            }
        }

        private void OnWorkflowNodesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] 📝 节点集合变化 (Name: {Name})");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   Action: {e.Action}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   NewItems: {e.NewItems?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   OldItems: {e.OldItems?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   总节点数: {WorkflowNodes?.Count ?? 0}");
        }

        /// <summary>
        /// 工作流连接集合
        /// </summary>
        public ObservableCollection<Models.WorkflowConnection> WorkflowConnections
        {
            get => _workflowConnections;
            set
            {
                if (_workflowConnections != null)
                {
                    _workflowConnections.CollectionChanged -= (s, e) => OnWorkflowConnectionsChanged(s, e);
                }
                SetProperty(ref _workflowConnections, value);
                if (_workflowConnections != null)
                {
                    _workflowConnections.CollectionChanged += (s, e) => OnWorkflowConnectionsChanged(s, e);
                }
            }
        }

        private void OnWorkflowConnectionsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] 🔗 连接集合变化 (Name: {Name})");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   Action: {e.Action}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   NewItems: {e.NewItems?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   OldItems: {e.OldItems?.Count ?? 0}");
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel]   总连接数: {WorkflowConnections?.Count ?? 0}");
        }

        /// <summary>
        /// 缩放变换对象（每个工作流独立）
        /// </summary>
        public ScaleTransform ScaleTransform
        {
            get => _scaleTransform;
            set => SetProperty(ref _scaleTransform, value);
        }

        /// <summary>
        /// 当前缩放比例（每个工作流独立，默认1.0即100%）
        /// </summary>
        public double CurrentScale
        {
            get => _currentScale;
            set => SetProperty(ref _currentScale, value);
        }

        /// <summary>
        /// 画布类型
        /// </summary>
        public CanvasType CanvasType
        {
            get => _canvasType;
            set => SetProperty(ref _canvasType, value);
        }

        /// <summary>
        /// 单次运行按钮文本
        /// </summary>
        public string SingleRunButtonText => "▶";

        /// <summary>
        /// 连续运行按钮文本
        /// </summary>
        public string ContinuousRunButtonText => IsRunning ? "⏹" : "▶▶";

        /// <summary>
        /// 是否可以删除
        /// </summary>
        public bool IsCloseable => true;

        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        public string StateText
        {
            get
            {
                return State switch
                {
                    WorkflowState.Stopped => "●",
                    WorkflowState.Running => "●",
                    WorkflowState.Paused => "●",
                    WorkflowState.Error => "●",
                    _ => "●"
                };
            }
        }

        /// <summary>
        /// 获取状态颜色
        /// </summary>
        public string StateColor
        {
            get
            {
                return State switch
                {
                    WorkflowState.Stopped => "#999999",
                    WorkflowState.Running => "#00CC99",
                    WorkflowState.Paused => "#FF9900",
                    WorkflowState.Error => "#FF4444",
                    _ => "#999999"
                };
            }
        }

        /// <summary>
        /// 创建新节点并自动分配序号
        /// </summary>
        /// <param name="algorithmType">算法类型</param>
        /// <param name="name">节点名称（可选，默认使用算法类型）</param>
        /// <returns>新创建的节点</returns>
        public Models.WorkflowNode CreateNode(string algorithmType, string? name = null)
        {
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] Creating node: Type={algorithmType}, Name={name}, WorkflowId={Id}");
            // 使用工厂创建节点，自动处理序号分配
            var node = _nodeFactory.CreateNode(algorithmType, name, Id);
            // System.Diagnostics.Debug.WriteLine($"[WorkflowTabViewModel] Node created: Id={node.Id}, Index={node.Index}, GlobalIndex={node.GlobalIndex}");
            return node;
        }

        /// <summary>
        /// 重置工作流的所有序号
        /// </summary>
        public void ResetNodeSequences()
        {
            _sequenceManager.ResetWorkflow(Id);
        }

        /// <summary>
        /// 强制刷新属性通知（用于手动触发属性更新）
        /// </summary>
        public void RefreshProperty(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// 工作流状态枚举
    /// </summary>
    public enum WorkflowState
    {
        Stopped,   // 已停止
        Running,   // 运行中
        Paused,    // 已暂停
        Error      // 错误
    }
}
