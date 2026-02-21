using System;
using System.Collections.Generic;
using SunEyeVision.Core.Interfaces;
using SunEyeVision.Core.Models;

namespace SunEyeVision.Plugin.Abstractions
{
    /// <summary>
    /// 工具插件接口 - 插件开发的唯一入口
    /// </summary>
    /// <remarks>
    /// 此接口定义了插件与框架交互的完整契约。
    /// 插件开发者只需实现此接口即可创建可热加载的工具插件。
    /// </remarks>
    public interface IToolPlugin
    {
        #region 插件基本信息

        /// <summary>
        /// 插件名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 插件唯一标识符
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// 插件描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 插件图标 (Emoji或图标路径)
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// 插件作者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 插件依赖列表
        /// </summary>
        List<string> Dependencies { get; }

        /// <summary>
        /// 插件是否已加载
        /// </summary>
        bool IsLoaded { get; }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 初始化插件
        /// </summary>
        void Initialize();

        /// <summary>
        /// 卸载插件
        /// </summary>
        void Unload();

        #endregion

        #region 工具管理

        /// <summary>
        /// 获取插件提供的所有工具元数据
        /// </summary>
        /// <returns>工具元数据列表</returns>
        List<ToolMetadata> GetToolMetadata();

        /// <summary>
        /// 获取算法节点类型列表
        /// </summary>
        /// <returns>算法节点类型列表</returns>
        List<Type> GetAlgorithmNodes();

        /// <summary>
        /// 创建工具实例
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>图像处理器实例</returns>
        IImageProcessor CreateToolInstance(string toolId);

        /// <summary>
        /// 获取工具的默认参数
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <returns>默认算法参数</returns>
        AlgorithmParameters GetDefaultParameters(string toolId);

        /// <summary>
        /// 验证参数有效性
        /// </summary>
        /// <param name="toolId">工具ID</param>
        /// <param name="parameters">算法参数</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateParameters(string toolId, AlgorithmParameters parameters);

        #endregion
    }

    /// <summary>
    /// 工具插件特性 - 用于标记工具插件类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ToolPluginAttribute : Attribute
    {
        /// <summary>
        /// 工具ID
        /// </summary>
        public string ToolId { get; }

        /// <summary>
        /// 工具名称
        /// </summary>
        public string Name { get; }

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
            ToolId = toolId ?? throw new ArgumentNullException(nameof(toolId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
