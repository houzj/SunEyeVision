using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 属性元数据提供者接口
    /// </summary>
    /// <remarks>
    /// 用于在设计时和运行时提供工具输出属性的元数据。
    /// 
    /// 依赖倒置原则：
    /// - SDK 层定义此接口（依赖抽象而非具体实现）
    /// - Infrastructure 层实现此接口（通过 ToolRegistry）
    /// - UI 层注入具体实现（构造函数注入）
    /// </remarks>
    public interface IPropertyMetadataProvider
    {
        /// <summary>
        /// 获取所有属性元数据
        /// </summary>
        /// <param name="toolType">工具类型名称或ID</param>
        /// <returns>属性元数据列表，未找到返回 null</returns>
        List<ToolPropertyMetadata>? GetAllPropertyMetadata(string toolType);
    }
}
