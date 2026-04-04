using SunEyeVision.Workflow;

namespace SunEyeVision.Workflow.Nodes
{
    /// <summary>
    /// 节点打开策略接口
    /// </summary>
    /// <remarks>
    /// 使用策略模式处理不同节点类型的打开行为。
    /// 每种节点类型可以有不同的打开策略（如子程序、条件、循环等特殊节点）。
    /// 
    /// 实现示例：
    /// <code>
    /// public class DefaultNodeOpenStrategy : INodeOpenStrategy
    /// {
    ///     public bool CanHandle(NodeOpenContext context)
    ///     {
    ///         return true; // 默认策略处理所有节点
    ///     }
    ///     
    ///     public void Execute(NodeOpenContext context)
    ///     {
    ///         // 根据窗口类型创建窗口并显示
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface INodeOpenStrategy
    {
        /// <summary>
        /// 判断当前策略是否可以处理指定上下文
        /// </summary>
        /// <param name="context">节点打开上下文</param>
        /// <returns>true 表示可以处理，false 表示不能处理</returns>
        bool CanHandle(NodeOpenContext context);

        /// <summary>
        /// 执行打开节点的操作
        /// </summary>
        /// <param name="context">节点打开上下文</param>
        void Execute(NodeOpenContext context);
    }
}
