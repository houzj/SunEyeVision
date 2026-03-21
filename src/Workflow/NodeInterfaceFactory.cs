using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 节点界面工厂 - 根据节点类型决定打开哪个界面
    /// </summary>
    public static class NodeInterfaceFactory
    {
        /// <summary>
        /// 获取节点的界面类型
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <returns>界面类型</returns>
        public static NodeInterfaceType GetInterfaceType(WorkflowNodeBase node, ToolMetadata? toolMetadata)
        {
            if (node == null)
            {
                return NodeInterfaceType.None;
            }

            // 根据节点类型决定界面类型
            switch (node.NodeType)
            {
                case NodeType.Subroutine:
                    // 子程序节点：创建新的工作流画布
                    return NodeInterfaceType.NewWorkflowCanvas;

                case NodeType.Condition:
                    // 条件节点：使用子程序编辑器（条件配置界面）
                    return NodeInterfaceType.SubroutineEditor;

                case NodeType.Algorithm:
                default:
                    // 算法节点：使用调试窗口（暂时保留此逻辑）
                    // TODO: 后续需要从工具实例动态判断是否支持调试窗口
                    return NodeInterfaceType.DebugWindow;
            }
        }

        /// <summary>
        /// 获取节点的界面类型（运行时检查版本）
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <param name="tool">工具实例（用于运行时检查）</param>
        /// <returns>界面类型</returns>
        public static NodeInterfaceType GetInterfaceType(WorkflowNodeBase node, ToolMetadata? toolMetadata, IToolPlugin? tool)
        {
            if (node == null)
            {
                return NodeInterfaceType.None;
            }

            // 根据节点类型决定界面类型
            switch (node.NodeType)
            {
                case NodeType.Subroutine:
                    return NodeInterfaceType.NewWorkflowCanvas;

                case NodeType.Condition:
                    return NodeInterfaceType.SubroutineEditor;

                case NodeType.Algorithm:
                default:
                    // 运行时检查：检查工具实例的 HasDebugWindow 属性
                    if (tool != null && tool.HasDebugWindow)
                    {
                        return NodeInterfaceType.DebugWindow;
                    }
                    return NodeInterfaceType.None;
            }
        }

        /// <summary>
        /// 检查节点是否可以打开界面
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <returns>是否可以打开界面</returns>
        public static bool CanOpenInterface(WorkflowNodeBase node, ToolMetadata? toolMetadata)
        {
            return GetInterfaceType(node, toolMetadata) != NodeInterfaceType.None;
        }

        /// <summary>
        /// 检查节点是否可以打开界面（运行时检查版本）
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="toolMetadata">工具元数据</param>
        /// <param name="tool">工具实例</param>
        /// <returns>是否可以打开界面</returns>
        public static bool CanOpenInterface(WorkflowNodeBase node, ToolMetadata? toolMetadata, IToolPlugin? tool)
        {
            return GetInterfaceType(node, toolMetadata, tool) != NodeInterfaceType.None;
        }
    }

    /// <summary>
    /// 节点界面类型枚举
    /// </summary>
    public enum NodeInterfaceType
    {
        /// <summary>
        /// 无界面
        /// </summary>
        None,

        /// <summary>
        /// 调试窗口
        /// </summary>
        DebugWindow,

        /// <summary>
        /// 子程序编辑器
        /// </summary>
        SubroutineEditor,

        /// <summary>
        /// 新工作流画布
        /// </summary>
        NewWorkflowCanvas
    }
}
