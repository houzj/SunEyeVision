using System;
using System.Collections.ObjectModel;
using System.Windows;
using SunEyeVision.UI.Models;

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

        public WorkflowTabViewModel()
        {
            Id = Guid.NewGuid().ToString();
            Name = "工作流1";
            State = WorkflowState.Stopped;
            RunMode = RunMode.Single;
            WorkflowNodes = new ObservableCollection<Models.WorkflowNode>();
            WorkflowConnections = new ObservableCollection<Models.WorkflowConnection>();
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
            set => SetProperty(ref _workflowNodes, value);
        }

        /// <summary>
        /// 工作流连接集合
        /// </summary>
        public ObservableCollection<Models.WorkflowConnection> WorkflowConnections
        {
            get => _workflowConnections;
            set => SetProperty(ref _workflowConnections, value);
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
