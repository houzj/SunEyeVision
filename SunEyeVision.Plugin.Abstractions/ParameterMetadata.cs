namespace SunEyeVision.Plugin.Abstractions
{
    /// <summary>
    /// 参数类型枚举
    /// </summary>
    public enum ParameterType
    {
        /// <summary>
        /// 整数
        /// </summary>
        Int,

        /// <summary>
        /// 浮点数
        /// </summary>
        Double,

        /// <summary>
        /// 字符串
        /// </summary>
        String,

        /// <summary>
        /// 布尔值
        /// </summary>
        Bool,

        /// <summary>
        /// 枚举
        /// </summary>
        Enum,

        /// <summary>
        /// 颜色
        /// </summary>
        Color,

        /// <summary>
        /// 点坐标
        /// </summary>
        Point,

        /// <summary>
        /// 尺寸
        /// </summary>
        Size,

        /// <summary>
        /// 矩形
        /// </summary>
        Rect,

        /// <summary>
        /// 图像
        /// </summary>
        Image,

        /// <summary>
        /// 文件路径
        /// </summary>
        FilePath,

        /// <summary>
        /// 列表
        /// </summary>
        List,

        /// <summary>
        /// 自定义类型
        /// </summary>
        Custom
    }

    /// <summary>
    /// 参数元数据 - 描述工具参数的完整信息
    /// </summary>
    public class ParameterMetadata
    {
        /// <summary>
        /// 参数名 (代码标识符)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称 (UI显示)
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型
        /// </summary>
        public ParameterType Type { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 最小值 (用于数值类型)
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// 最大值 (用于数值类型)
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// 枚举选项 (用于枚举类型)
        /// </summary>
        public object[]? Options { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool ReadOnly { get; set; } = false;

        /// <summary>
        /// 参数分类 (用于分组显示)
        /// </summary>
        public string Category { get; set; } = "基本参数";

        /// <summary>
        /// 是否支持调试模式下实时修改
        /// </summary>
        public bool EditableInDebug { get; set; } = true;
    }
}
