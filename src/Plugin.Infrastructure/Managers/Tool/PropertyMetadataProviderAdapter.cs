using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.Infrastructure.Managers.Tool
{
    /// <summary>
    /// 属性元数据提供者适配器
    /// </summary>
    /// <remarks>
    /// 桥接 ToolRegistry 和 IPropertyMetadataProvider 接口。
    /// 实现 SDK 层定义的接口，内部调用 Infrastructure 层的 ToolRegistry。
    /// 
    /// 优化方案（2026-04）：
    /// - 按具体类型查询，无需分类
    /// - 直接精确匹配类型（typeof(Point[]), typeof(Mat) 等）
    /// - 符合视觉软件特点（OpenCvSharp 主要使用数组）
    /// </remarks>
    public class PropertyMetadataProviderAdapter : IPropertyMetadataProvider
    {
        /// <summary>
        /// 获取所有属性元数据
        /// </summary>
        /// <param name="toolType">工具类型名称或ID</param>
        /// <returns>属性元数据列表，未找到返回 null</returns>
        public List<ToolPropertyMetadata>? GetAllPropertyMetadata(string toolType)
        {
            try
            {
                // 通过 ToolRegistry 获取属性元数据
                var metadataList = ToolRegistry.GetAllPropertyMetadata(toolType);
                return metadataList;
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"[PropertyMetadataProvider] 获取属性元数据失败: {toolType}, 错误: {ex.Message}",
                    "PropertyMetadataProvider");
                return null;
            }
        }

        /// <summary>
        /// 按类型获取属性元数据（优化方案）
        /// </summary>
        /// <param name="toolType">工具类型名称或ID</param>
        /// <param name="propertyType">属性类型（如 typeof(Point[]), typeof(Mat) 等）</param>
        /// <returns>属性元数据列表</returns>
        public List<ToolPropertyMetadata> GetPropertyMetadataByType(string toolType, Type propertyType)
        {
            try
            {
                // 直接调用 ToolRegistry 的按类型查询方法
                return ToolRegistry.GetMetadataByType(toolType, propertyType);
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"[PropertyMetadataProvider] 按类型获取属性元数据失败: {toolType}, 类型: {propertyType?.Name}, 错误: {ex.Message}",
                    "PropertyMetadataProvider");
                return new List<ToolPropertyMetadata>();
            }
        }
    }
}
