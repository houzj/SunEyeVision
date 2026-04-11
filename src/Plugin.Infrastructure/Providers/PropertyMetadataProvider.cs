using SunEyeVision.Plugin.Infrastructure.Managers.Tool;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Plugin.Infrastructure.Providers;

/// <summary>
/// 属性元数据提供者实现
/// </summary>
public class PropertyMetadataProvider : IPropertyMetadataProvider
{
    /// <summary>
    /// 获取所有属性元数据
    /// </summary>
    public List<ToolPropertyMetadata>? GetAllPropertyMetadata(string toolType)
    {
        var allMetadata = ToolRegistry.GetAllPropertyMetadata(toolType);
        return allMetadata.Count > 0 ? allMetadata : null;
    }
    
    /// <summary>
    /// 按分类获取属性元数据（方案B）
    /// </summary>
    public List<ToolPropertyMetadata> GetPropertyMetadataByCategory(string toolType, OutputTypeCategory category)
    {
        return ToolRegistry.GetMetadataByCategory(toolType, category);
    }
    
    /// <summary>
    /// 按类型获取属性元数据（兼容旧接口）
    /// </summary>
    public List<ToolPropertyMetadata> GetPropertyMetadataByType(string toolType, Type targetType)
    {
        // 确定目标类型所属的分类
        var category = OutputTypeCategoryMapper.GetCategory(targetType);
        
        // 从对应分类获取所有属性
        var categoryProperties = ToolRegistry.GetMetadataByCategory(toolType, category);
        
        // 过滤兼容的属性
        return categoryProperties.Where(metadata => 
            OutputTypeCategoryMapper.IsTypeCompatible(metadata.PropertyType, targetType)
        ).ToList();
    }
}
