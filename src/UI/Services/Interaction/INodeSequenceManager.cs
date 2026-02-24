using SunEyeVision.UI.Views.Controls.Canvas;

namespace SunEyeVision.UI.Services.Interaction
{
    /// <summary>
    /// 节点序号管理器接口，用于管理节点的全局序号和局部序�?
    /// </summary>
    public interface INodeSequenceManager
    {
        /// <summary>
        /// 获取下一个全局序号
        /// </summary>
        /// <returns>全局序号</returns>
        int GetNextGlobalIndex();

        /// <summary>
        /// 获取指定工作流和算法类型的下一个局部序�?
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="algorithmType">算法类型</param>
        /// <returns>局部序�?/returns>
        int GetNextLocalIndex(string workflowId, string algorithmType);

        /// <summary>
        /// 重置所有序�?
        /// </summary>
        void Reset();

        /// <summary>
        /// 重置指定工作流的序号
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        void ResetWorkflow(string workflowId);
    }
}
