using System;
using SunEyeVision.Core.Events;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.UI.Events;
using SunEyeVision.UI.Infrastructure;

namespace SunEyeVision.UI.Infrastructure
{
    /// <summary>
    /// UI事件发布服务 - 负责发布UI相关的事�?
    /// </summary>
    public class UIEventPublisher
    {
        private readonly IEventBus _eventBus;

        public UIEventPublisher(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// 发布节点添加事件
        /// </summary>
        public void PublishNodeAdded(string nodeId, string nodeName, string algorithmType, double positionX, double positionY)
        {
            var evt = new UIEvents.NodeAddedEvent("MainWindow", nodeId, nodeName)
            {
                AlgorithmType = algorithmType,
                PositionX = positionX,
                PositionY = positionY
            };
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布节点选中事件
        /// </summary>
        public void PublishNodeSelected(string nodeId, string nodeName)
        {
            var evt = new UIEvents.NodeSelectedEvent("MainWindow", nodeId, nodeName);
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布节点移动事件
        /// </summary>
        public void PublishNodeMoved(string nodeId, double newPositionX, double newPositionY)
        {
            var evt = new UIEvents.NodeMovedEvent("MainWindow", nodeId)
            {
                NewPositionX = newPositionX,
                NewPositionY = newPositionY
            };
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布节点删除事件
        /// </summary>
        public void PublishNodeDeleted(string nodeId, string nodeName)
        {
            var evt = new UIEvents.NodeDeletedEvent("MainWindow", nodeId, nodeName);
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布节点连接事件
        /// </summary>
        public void PublishNodeConnected(string sourceNodeId, string targetNodeId)
        {
            var evt = new UIEvents.NodeConnectedEvent("MainWindow", sourceNodeId, targetNodeId);
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布节点断开连接事件
        /// </summary>
        public void PublishNodeDisconnected(string sourceNodeId, string targetNodeId)
        {
            var evt = new UIEvents.NodeDisconnectedEvent("MainWindow", sourceNodeId, targetNodeId);
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布工作流保存事�?
        /// </summary>
        public void PublishWorkflowSaved(string workflowPath, int nodeCount)
        {
            var evt = new UIEvents.WorkflowSavedEvent("MainWindow", workflowPath)
            {
                NodeCount = nodeCount
            };
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布工作流加载事�?
        /// </summary>
        public void PublishWorkflowLoaded(string workflowPath, int nodeCount)
        {
            var evt = new UIEvents.WorkflowLoadedEvent("MainWindow", workflowPath)
            {
                NodeCount = nodeCount
            };
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布工作流清除事�?
        /// </summary>
        public void PublishWorkflowCleared()
        {
            var evt = new UIEvents.WorkflowClearedEvent("MainWindow");
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布布局改变事件
        /// </summary>
        public void PublishLayoutChanged(string panelName, double oldWidth, double newWidth, bool isCollapsed)
        {
            var evt = new UIEvents.LayoutChangedEvent("MainWindow", panelName)
            {
                OldWidth = oldWidth,
                NewWidth = newWidth,
                IsCollapsed = isCollapsed
            };
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布调试窗口打开事件
        /// </summary>
        public void PublishDebugWindowOpened(string nodeId, string nodeName)
        {
            var evt = new UIEvents.DebugWindowOpenedEvent("MainWindow", nodeId, nodeName);
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布调试窗口关闭事件
        /// </summary>
        public void PublishDebugWindowClosed(string nodeId, string nodeName)
        {
            var evt = new UIEvents.DebugWindowClosedEvent("MainWindow", nodeId, nodeName);
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布参数改变事件
        /// </summary>
        public void PublishParameterChanged(string nodeId, string parameterName, object oldValue, object newValue)
        {
            var evt = new UIEvents.ParameterChangedEvent("MainWindow", nodeId, parameterName)
            {
                OldValue = oldValue,
                NewValue = newValue
            };
            _eventBus.Publish(evt);
        }

        /// <summary>
        /// 发布状态更新事�?
        /// </summary>
        public void PublishStatusUpdate(string statusText)
        {
            var evt = new UIEvents.StatusUpdateEvent("MainWindow", statusText);
            _eventBus.Publish(evt);
        }
    }
}
