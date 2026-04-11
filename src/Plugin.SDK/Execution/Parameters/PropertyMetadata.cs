using System;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 工具属性元数据（在工具注册时提取，设计时和运行时共享）
    /// </summary>
    /// <remarks>
    /// 核心特性：
    /// - 编译时提取：在工具注册时通过反射一次性提取
    /// - 不依赖实例：通过临时空实例调用 GetPropertyTreeName
    /// - 变量池存储：存储在嵌套字典中，避免重复计算
    /// - 直接读取：使用时直接从变量池读取，性能最优
    /// 
    /// 注意：此类命名为 ToolPropertyMetadata 以避免与 System.Windows.PropertyMetadata 冲突。
    /// </remarks>
    public class ToolPropertyMetadata
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 树形名称（使用 `.` 分隔多级）
        /// </summary>
        public string? TreeName { get; set; }

        /// <summary>
        /// 属性类型
        /// </summary>
        public Type? PropertyType { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }
    }
}
