using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using AppCommands = SunEyeVision.UI.Commands;
using SunEyeVision.UI.Adapters;
using SunEyeVision.UI.Commands;
using SunEyeVision.UI.Services.Canvas;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services;
using SunEyeVision.UI.Views.Controls.Canvas;
using SunEyeVision.UI.Services.Interaction;
using SunEyeVision.UI.Services.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// ǩҳViewModel
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
        /// ڵ序号管理?
        /// </summary>
        private readonly INodeSequenceManager _sequenceManager;

        /// <summary>
        /// ڵʾ?
        /// </summary>
        private readonly INodeDisplayAdapter _displayAdapter;

        /// <summary>
        /// 
        /// </summary>
        private readonly IWorkflowNodeFactory _nodeFactory;

        /// <summary>
        /// ÿJĳ/?
        /// </summary>
        public AppCommands.CommandManager CommandManager { get; }

        public WorkflowTabViewModel() : this(
            new NodeSequenceManager(),
            new DefaultNodeDisplayAdapter())
        {
        }

        /// <summary>
        /// 带依赖注入的构函?
        /// </summary>
        public WorkflowTabViewModel(INodeSequenceManager sequenceManager, INodeDisplayAdapter displayAdapter)
        {
            _sequenceManager = sequenceManager ?? throw new ArgumentNullException(nameof(sequenceManager));
            _displayAdapter = displayAdapter ?? throw new ArgumentNullException(nameof(displayAdapter));

            // 
            _nodeFactory = new WorkflowNodeFactory(_sequenceManager, _displayAdapter);

            Id = Guid.NewGuid().ToString();
            Name = "工作?";
            State = WorkflowState.Stopped;
            RunMode = RunMode.Single;
            WorkflowNodes = new ObservableCollection<Models.WorkflowNode>();
            WorkflowConnections = new ObservableCollection<Models.WorkflowConnection>();
            CurrentScale = 1.0;
            ScaleTransform = new ScaleTransform(1.0, 1.0);
            CanvasType = CanvasType.WorkflowCanvas; // 默使用 WorkflowCanvas，每丷作流狫

            // ÿ?
            CommandManager = new CommandManager(WorkflowNodes, WorkflowConnections);

            // 订阅ڵ和连接集合变化事?
            WorkflowNodes.CollectionChanged += (s, e) => OnWorkflowNodesChanged(s, e);
            WorkflowConnections.CollectionChanged += (s, e) => OnWorkflowConnectionsChanged(s, e);
        }

        /// <summary>
        /// ID
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// ?
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// S
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
        /// ģʽ
        /// </summary>
        public RunMode RunMode
        {
            get => _runMode;
            set => SetProperty(ref _runMode, value);
        }

        /// <summary>
        /// ״?
        /// </summary>
        public WorkflowState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        /// <summary>
        /// ڵ㼯?
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
        }

        /// <summary>
        /// Ӽ?
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
        }

        /// <summary>
        /// 任ÿRJ?
        /// </summary>
        public ScaleTransform ScaleTransform
        {
            get => _scaleTransform;
            set => SetProperty(ref _scaleTransform, value);
        }

        /// <summary>
        /// ǰÿRJĬ?.0?00%?
        /// </summary>
        public double CurrentScale
        {
            get => _currentScale;
            set => SetProperty(ref _currentScale, value);
        }

        /// <summary>
        /// 画布
        /// </summary>
        public CanvasType CanvasType
        {
            get => _canvasType;
            set => SetProperty(ref _canvasType, value);
        }

        /// <summary>
        /// ˰ťı
        /// </summary>
        public string SingleRunButtonText => "?";

        /// <summary>
        /// ˰ťı
        /// </summary>
        public string ContinuousRunButtonText => IsRunning ? " ֹͣ" : " ";

        /// <summary>
        /// S߷
        /// </summary>
        public bool IsCloseable => true;

        /// <summary>
        /// 获取状显示文?
        /// </summary>
        public string StateText
        {
            get
            {
                return State switch
                {
                    WorkflowState.Stopped => "",
                    WorkflowState.Running => "",
                    WorkflowState.Paused => "",
                    WorkflowState.Error => "",
                    _ => ""
                };
            }
        }

        /// <summary>
        /// ȡ״?
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
        /// ½ڵ㲢
        /// </summary>
        /// <param name="algorithmType">算法</param>
        /// <param name="name">ƣѡĬʹ㷨?/param>
        /// <returns>新创建的ڵ</returns>
        public Models.WorkflowNode CreateNode(string algorithmType, string? name = null)
        {
            if (_nodeFactory == null)
            {
                throw new InvalidOperationException("NodeFactory is not initialized");
            }

            // ʹùԶŷ?
            var node = _nodeFactory.CreateNode(algorithmType, name, Id);

            return node;
        }

        /// <summary>
        /// ù?
        /// </summary>
        public void ResetNodeSequences()
        {
            _sequenceManager.ResetWorkflow(Id);
        }

        /// <summary>
        /// ǿˢֶ֪Ը£
        /// </summary>
        public void RefreshProperty(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
        }
    }

    /// <summary>
    /// ״̬ö?
    /// </summary>
    public enum WorkflowState
    {
        Stopped,   // 已停?
        Running,   // 运?
        Paused,    // 已暂?
        Error      // 
    }
}
