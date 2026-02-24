using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 运行模式枚举
    /// </summary>
    public enum RunMode
    {
        Single,       // 单次运行
        Continuous    // 连续运行
    }

    /// <summary>
    /// 工作流信息类
    /// </summary>
    public class WorkflowInfo : ObservableObject
    {
        private string _id;
        private string _name;
        private bool _isRunning;
        private RunMode _runMode;

        public WorkflowInfo()
        {
            Id = Guid.NewGuid().ToString();
            Name = "新工作流";
            Nodes = new ObservableCollection<WorkflowNode>();
            Connections = new ObservableCollection<WorkflowConnection>();
            RunMode = RunMode.Single;
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
        /// 工作流名�?
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
            set => SetProperty(ref _isRunning, value);
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
        /// 节点集合
        /// </summary>
        public ObservableCollection<WorkflowNode> Nodes { get; set; }

        /// <summary>
        /// 连接线集�?
        /// </summary>
        public ObservableCollection<WorkflowConnection> Connections { get; set; }
    }
}
