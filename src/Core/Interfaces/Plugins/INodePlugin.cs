namespace SunEyeVision.Core.Interfaces.Plugins
{
    /// <summary>
    /// 节点插件接口
    /// 定义工作流节点的标准行为
    /// </summary>
    public interface INodePlugin : IPlugin
    {
        /// <summary>
        /// 节点类型标识
        /// </summary>
        string NodeType { get; }

        /// <summary>
        /// 节点图标
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// 节点分类
        /// </summary>
        string Category { get; }

        /// <summary>
        /// 节点输入端口定义
        /// </summary>
        PortDefinition[] InputPorts { get; }

        /// <summary>
        /// 节点输出端口定义
        /// </summary>
        PortDefinition[] OutputPorts { get; }

        /// <summary>
        /// 节点参数元数�?
        /// </summary>
        ParameterMetadata[] GetParameters();

        /// <summary>
        /// 执行节点逻辑
        /// </summary>
        /// <param name="inputs">输入数据</param>
        /// <returns>输出数据</returns>
        object Execute(object[] inputs);
    }

    /// <summary>
    /// 端口定义
    /// </summary>
    public class PortDefinition
    {
        /// <summary>
        /// 端口ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 端口名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 端口数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// 参数元数�?
    /// </summary>
    public class ParameterMetadata
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 参数显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 默认�?
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// 最小�?
        /// </summary>
        public object MinValue { get; set; }

        /// <summary>
        /// 最大�?
        /// </summary>
        public object MaxValue { get; set; }

        /// <summary>
        /// 可选值列�?
        /// </summary>
        public object[] Options { get; set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; }
    }
}
