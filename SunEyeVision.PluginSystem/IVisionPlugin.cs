using System;
using System.Collections.Generic;
using System.Reflection;
using SunEyeVision.Models;

namespace SunEyeVision.PluginSystem
{
    /// <summary>
    /// 视觉插件接口
    /// </summary>
    public interface IVisionPlugin
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 插件作�?
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 插件描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 插件ID
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// 插件依赖
        /// </summary>
        List<string> Dependencies { get; }

        /// <summary>
        /// 插件图标
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// 初始化插�?
        /// </summary>
        void Initialize();

        /// <summary>
        /// 卸载插件
        /// </summary>
        void Unload();

        /// <summary>
        /// 获取算法节点列表
        /// </summary>
        /// <returns>算法节点类型列表</returns>
        List<Type> GetAlgorithmNodes();

        /// <summary>
        /// 插件是否已加�?
        /// </summary>
        bool IsLoaded { get; }
    }
}
