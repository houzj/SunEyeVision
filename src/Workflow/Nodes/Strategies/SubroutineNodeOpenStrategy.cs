using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow.Nodes.Strategies
{
    /// <summary>
    /// 子程序节点打开策略 - 处理子程序类型的节点
    /// </summary>
    /// <remarks>
    /// 职责：
    /// 1. 检测节点是否为子程序类型
    /// 2. 执行子程序特定的打开行为
    /// 
    /// 子程序节点的特殊行为：
    /// - 打开子程序对应的子工作流
    /// - 不创建调试窗口
    /// </remarks>
    public class SubroutineNodeOpenStrategy : INodeOpenStrategy
    {
        public bool CanHandle(NodeOpenContext context)
        {
            // 检查节点类型是否为子程序
            return context.Node.NodeType == NodeType.Subroutine;
        }

        public void Execute(NodeOpenContext context)
        {
            VisionLogger.Instance.Log(LogLevel.Info, 
                $"处理子程序节点: {context.Node.Name}", 
                "SubroutineNodeOpenStrategy");

            // TODO: 实现子程序打开逻辑
            // 1. 获取子程序引用的工作流
            // 2. 切换到子工作流视图
            // 3. 或者打开子工作流的编辑窗口

            VisionLogger.Instance.Log(LogLevel.Warning, 
                "子程序节点打开功能尚未实现", 
                "SubroutineNodeOpenStrategy");
        }
    }
}
