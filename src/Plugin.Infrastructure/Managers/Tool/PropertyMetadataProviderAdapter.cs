using System;
using System.Collections.Generic;
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
                
                VisionLogger.Instance.Log(LogLevel.Success,
                    $"[PropertyMetadataProvider] 获取属性元数据成功: {toolType}, 共 {metadataList?.Count ?? 0} 个属性",
                    "PropertyMetadataProvider");
                
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
    }
}
