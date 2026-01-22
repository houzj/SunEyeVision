using System;
using System.Collections.Generic;

namespace SunEyeVision.PluginSystem
{
    /// <summary>
    /// 工具插件接口 - 扩展自IVisionPlugin，提供工具元数据和参数管理
    /// </summary>
    public interface IToolPlugin : IVisionPlugin
    {
        /// <summary>
        /// 获取工具元数据列表
        /// </summary>
        /// <returns>工具元数据列表</returns>
        List<ToolMetadata> GetToolMetadata();

        /// <summary>
        /// 创建工具实例
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>图像处理器实例</returns>
        SunEyeVision.Interfaces.IImageProcessor CreateToolInstance(string toolId);

        /// <summary>
        /// 获取工具的默认参数
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>默认算法参数</returns>
        SunEyeVision.Models.AlgorithmParameters GetDefaultParameters(string toolId);

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="parameters">算法参数</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateParameters(string toolId, SunEyeVision.Models.AlgorithmParameters parameters);
    }

    /// <summary>
    /// 工具插件特性 - 用于标记工具插件类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolPluginAttribute : Attribute
    {
        /// <summary>
        /// 工具ID
        /// </summary>
        public string ToolId { get; set; } = string.Empty;

        /// <summary>
        /// 工具名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 工具版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 工具分类
        /// </summary>
        public string Category { get; set; } = "未分类";

        public ToolPluginAttribute(string toolId, string name)
        {
            ToolId = toolId;
            Name = name;
        }
    }
}
