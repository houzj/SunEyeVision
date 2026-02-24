using SunEyeVision.Core.Events;

namespace SunEyeVision.UI.Events
{
    /// <summary>
    /// UI 相关事件定义
    /// </summary>
    public static class UIEvents
    {
        /// <summary>
        /// 节点添加事件
        /// </summary>
        public class NodeAddedEvent : EventBase
        {
            public string NodeId { get; set; }
            public string NodeName { get; set; }
            public string AlgorithmType { get; set; }
            public double PositionX { get; set; }
            public double PositionY { get; set; }

            public NodeAddedEvent(string source, string nodeId, string nodeName)
                : base(source)
            {
                NodeId = nodeId;
                NodeName = nodeName;
            }
        }

        /// <summary>
        /// 节点选中事件
        /// </summary>
        public class NodeSelectedEvent : EventBase
        {
            public string NodeId { get; set; }
            public string NodeName { get; set; }

            public NodeSelectedEvent(string source, string nodeId, string nodeName)
                : base(source)
            {
                NodeId = nodeId;
                NodeName = nodeName;
            }
        }

        /// <summary>
        /// 节点移动事件
        /// </summary>
        public class NodeMovedEvent : EventBase
        {
            public string NodeId { get; set; }
            public double NewPositionX { get; set; }
            public double NewPositionY { get; set; }

            public NodeMovedEvent(string source, string nodeId)
                : base(source)
            {
                NodeId = nodeId;
            }
        }

        /// <summary>
        /// 节点删除事件
        /// </summary>
        public class NodeDeletedEvent : EventBase
        {
            public string NodeId { get; set; }
            public string NodeName { get; set; }

            public NodeDeletedEvent(string source, string nodeId, string nodeName)
                : base(source)
            {
                NodeId = nodeId;
                NodeName = nodeName;
            }
        }

        /// <summary>
        /// 节点连接事件
        /// </summary>
        public class NodeConnectedEvent : EventBase
        {
            public string SourceNodeId { get; set; }
            public string TargetNodeId { get; set; }

            public NodeConnectedEvent(string source, string sourceNodeId, string targetNodeId)
                : base(source)
            {
                SourceNodeId = sourceNodeId;
                TargetNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 节点连接断开事件
        /// </summary>
        public class NodeDisconnectedEvent : EventBase
        {
            public string SourceNodeId { get; set; }
            public string TargetNodeId { get; set; }

            public NodeDisconnectedEvent(string source, string sourceNodeId, string targetNodeId)
                : base(source)
            {
                SourceNodeId = sourceNodeId;
                TargetNodeId = targetNodeId;
            }
        }

        /// <summary>
        /// 工作流保存事�?
        /// </summary>
        public class WorkflowSavedEvent : EventBase
        {
            public string WorkflowPath { get; set; }
            public int NodeCount { get; set; }

            public WorkflowSavedEvent(string source, string workflowPath)
                : base(source)
            {
                WorkflowPath = workflowPath;
            }
        }

        /// <summary>
        /// 工作流加载事�?
        /// </summary>
        public class WorkflowLoadedEvent : EventBase
        {
            public string WorkflowPath { get; set; }
            public int NodeCount { get; set; }

            public WorkflowLoadedEvent(string source, string workflowPath)
                : base(source)
            {
                WorkflowPath = workflowPath;
            }
        }

        /// <summary>
        /// 工作流清除事�?
        /// </summary>
        public class WorkflowClearedEvent : EventBase
        {
            public WorkflowClearedEvent(string source)
                : base(source)
            {
            }
        }

        /// <summary>
        /// 布局改变事件
        /// </summary>
        public class LayoutChangedEvent : EventBase
        {
            public string PanelName { get; set; }
            public double OldWidth { get; set; }
            public double NewWidth { get; set; }
            public bool IsCollapsed { get; set; }

            public LayoutChangedEvent(string source, string panelName)
                : base(source)
            {
                PanelName = panelName;
            }
        }

        /// <summary>
        /// 调试窗口打开事件
        /// </summary>
        public class DebugWindowOpenedEvent : EventBase
        {
            public string NodeId { get; set; }
            public string NodeName { get; set; }

            public DebugWindowOpenedEvent(string source, string nodeId, string nodeName)
                : base(source)
            {
                NodeId = nodeId;
                NodeName = nodeName;
            }
        }

        /// <summary>
        /// 调试窗口关闭事件
        /// </summary>
        public class DebugWindowClosedEvent : EventBase
        {
            public string NodeId { get; set; }
            public string NodeName { get; set; }

            public DebugWindowClosedEvent(string source, string nodeId, string nodeName)
                : base(source)
            {
                NodeId = nodeId;
                NodeName = nodeName;
            }
        }

        /// <summary>
        /// 参数改变事件
        /// </summary>
        public class ParameterChangedEvent : EventBase
        {
            public string NodeId { get; set; }
            public string ParameterName { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }

            public ParameterChangedEvent(string source, string nodeId, string parameterName)
                : base(source)
            {
                NodeId = nodeId;
                ParameterName = parameterName;
            }
        }

        /// <summary>
        /// 状态更新事�?
        /// </summary>
        public class StatusUpdateEvent : EventBase
        {
            public string StatusText { get; set; }

            public StatusUpdateEvent(string source, string statusText)
                : base(source)
            {
                StatusText = statusText;
            }
        }

        /// <summary>
        /// 工作流执行事件类�?
        /// </summary>
        public enum WorkflowExecutionType
        {
            Start,
            Stop,
            Pause,
            Resume,
            Error
        }

        /// <summary>
        /// 工作流执行事�?
        /// </summary>
        public class WorkflowExecutionEvent : EventBase
        {
            public string WorkflowId { get; set; }
            public string WorkflowName { get; set; }
            public WorkflowExecutionType ExecutionType { get; set; }

            public WorkflowExecutionEvent(string source, string workflowId, string workflowName, WorkflowExecutionType executionType)
                : base(source)
            {
                WorkflowId = workflowId;
                WorkflowName = workflowName;
                ExecutionType = executionType;
            }
        }
    }
}
