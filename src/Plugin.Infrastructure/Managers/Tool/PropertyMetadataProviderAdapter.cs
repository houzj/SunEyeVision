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
        /// 按分类获取属性元数据（方案B）
        /// </summary>
        /// <param name="toolType">工具类型名称或ID</param>
        /// <param name="category">类型分类</param>
        /// <returns>属性元数据列表</returns>
        public List<ToolPropertyMetadata> GetPropertyMetadataByCategory(string toolType, OutputTypeCategory category)
        {
            try
            {
                // 获取所有属性元数据
                var allMetadata = ToolRegistry.GetAllPropertyMetadata(toolType) ?? new List<ToolPropertyMetadata>();

                // 按分类过滤
                var filteredMetadata = allMetadata.Where(metadata =>
                {
                    if (metadata.PropertyType == null)
                        return false;

                    var typeCategory = TypeCategoryMapper.GetCategory(metadata.PropertyType);
                    return typeCategory == category;
                }).ToList();

                return filteredMetadata;
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"[PropertyMetadataProvider] 按分类获取属性元数据失败: {toolType}, 分类: {category}, 错误: {ex.Message}",
                    "PropertyMetadataProvider");
                return new List<ToolPropertyMetadata>();
            }
        }

        /// <summary>
        /// 按类型获取属性元数据（兼容旧接口）
        /// </summary>
        /// <param name="toolType">工具类型名称或ID</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>属性元数据列表</returns>
        public List<ToolPropertyMetadata> GetPropertyMetadataByType(string toolType, Type targetType)
        {
            try
            {
                // 获取所有属性元数据
                var allMetadata = ToolRegistry.GetAllPropertyMetadata(toolType) ?? new List<ToolPropertyMetadata>();

                // 按类型过滤（支持类型匹配和派生类）
                var filteredMetadata = allMetadata.Where(metadata =>
                {
                    if (metadata.PropertyType == null)
                        return false;

                    // 精确匹配或派生类匹配
                    return metadata.PropertyType == targetType ||
                           targetType.IsAssignableFrom(metadata.PropertyType);
                }).ToList();

                return filteredMetadata;
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning,
                    $"[PropertyMetadataProvider] 按类型获取属性元数据失败: {toolType}, 类型: {targetType?.Name}, 错误: {ex.Message}",
                    "PropertyMetadataProvider");
                return new List<ToolPropertyMetadata>();
            }
        }
    }
}
