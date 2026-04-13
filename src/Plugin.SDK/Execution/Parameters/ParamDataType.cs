using System;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 参数数据类型枚举
    /// </summary>
    public enum ParamDataType
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
        /// 圆形
        /// </summary>
        Circle,

        /// <summary>
        /// 列表
        /// </summary>
        List,

        /// <summary>
        /// 自定义类型
        /// </summary>
        Custom
    }
}
