using System;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// ROI形状类型
    /// </summary>
    public enum ROIType
    {
        /// <summary>
        /// 矩形
        /// </summary>
        Rectangle,

        /// <summary>
        /// 圆形
        /// </summary>
        Circle,

        /// <summary>
        /// 旋转矩形
        /// </summary>
        RotatedRectangle,

        /// <summary>
        /// 直线
        /// </summary>
        Line,

        /// <summary>
        /// 多边形
        /// </summary>
        Polygon,

        /// <summary>
        /// 椭圆
        /// </summary>
        Ellipse
    }

    /// <summary>
    /// ROI编辑模式
    /// </summary>
    public enum ROIMode
    {
        /// <summary>
        /// 继承模式 - 只读显示
        /// </summary>
        Inherit,

        /// <summary>
        /// 编辑模式 - 支持创建、编辑、删除
        /// </summary>
        Edit
    }

    /// <summary>
    /// ROI绘制工具
    /// </summary>
    public enum ROITool
    {
        /// <summary>
        /// 选择工具
        /// </summary>
        Select,

        /// <summary>
        /// 多选工具
        /// </summary>
        MultiSelect,

        /// <summary>
        /// 矩形绘制工具
        /// </summary>
        Rectangle,

        /// <summary>
        /// 圆形绘制工具
        /// </summary>
        Circle,

        /// <summary>
        /// 旋转矩形绘制工具
        /// </summary>
        RotatedRectangle,

        /// <summary>
        /// 直线绘制工具
        /// </summary>
        Line
    }
}
