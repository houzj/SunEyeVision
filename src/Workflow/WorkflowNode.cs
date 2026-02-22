using System;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流节点
    /// </summary>
    public class WorkflowNode
    {
        /// <summary>
        /// 节点ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 节点类型
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// 算法类型名称
        /// </summary>
        public string AlgorithmType { get; set; }

        /// <summary>
        /// 节点参数
        /// </summary>
        public AlgorithmParameters Parameters { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 执行前事件
        /// </summary>
        public event Action<WorkflowNode> BeforeExecute;

        /// <summary>
        /// 执行后事件
        /// </summary>
        public event Action<WorkflowNode, AlgorithmResult> AfterExecute;

        public WorkflowNode(string id, string name, NodeType type)
        {
            Id = id;
            Name = name;
            Type = type;
            AlgorithmType = string.Empty;  // 初始化为非null值
            Parameters = new AlgorithmParameters();
        }

        /// <summary>
        /// 触发执行前事件
        /// </summary>
        protected virtual void OnBeforeExecute()
        {
            BeforeExecute?.Invoke(this);
        }

        /// <summary>
        /// 触发执行后事件
        /// </summary>
        protected virtual void OnAfterExecute(AlgorithmResult result)
        {
            AfterExecute?.Invoke(this, result);
        }

        /// <summary>
        /// 创建算法实例（的初始化处理方法）
        /// </summary>
        public virtual IImageProcessor CreateInstance()
        {
            // 抛出异常，由子类重写来实现具体的加载逻辑
            throw new NotImplementedException($"Algorithm type '{AlgorithmType}' is not implemented.");
        }
    }
}
