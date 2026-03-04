using System;
using System.Collections.Generic;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 区域数据 - 顶层容器
    /// </summary>
    public class RegionData : ICloneable
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 区域名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 区域定义
        /// </summary>
        public RegionDefinition? Definition { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 是否可编辑
        /// </summary>
        public bool IsEditable { get; set; } = true;

        /// <summary>
        /// 标签
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int ZIndex { get; set; }

        /// <summary>
        /// 显示颜色（ARGB）
        /// </summary>
        public uint DisplayColor { get; set; } = 0xFFFF0000; // 红色

        /// <summary>
        /// 显示透明度 (0-1)
        /// </summary>
        public double DisplayOpacity { get; set; } = 0.3;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 扩展属性
        /// </summary>
        public Dictionary<string, object> ExtendedProperties { get; set; } = new();

        /// <summary>
        /// 获取形状类型
        /// </summary>
        public ShapeType? GetShapeType()
        {
            return Definition?.GetShapeType();
        }

        /// <summary>
        /// 获取定义模式
        /// </summary>
        public RegionDefinitionMode GetMode()
        {
            return Definition?.Mode ?? RegionDefinitionMode.Drawing;
        }

        /// <summary>
        /// 创建绘制模式的区域数据
        /// </summary>
        public static RegionData CreateDrawingRegion(string name, ShapeType shapeType)
        {
            return new RegionData
            {
                Name = name,
                Definition = new ShapeDefinition { ShapeType = shapeType }
            };
        }

        /// <summary>
        /// 创建订阅模式的区域数据（按区域类型）
        /// </summary>
        public static RegionData CreateSubscribedRegion(string name, string nodeId, string outputName, int? index = null)
        {
            return new RegionData
            {
                Name = name,
                Definition = new FixedRegion(nodeId, outputName, index)
            };
        }

        /// <summary>
        /// 创建计算模式的区域数据（按参数订阅）
        /// </summary>
        public static RegionData CreateComputedRegion(string name, ShapeType targetShapeType)
        {
            return new RegionData
            {
                Name = name,
                Definition = new ComputedRegion { TargetShapeType = targetShapeType }
            };
        }

        /// <summary>
        /// 标记已修改
        /// </summary>
        public void MarkModified()
        {
            ModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 克隆
        /// </summary>
        public object Clone()
        {
            var clone = new RegionData
            {
                Id = Id,
                Name = Name,
                Definition = Definition?.Clone(),
                IsEnabled = IsEnabled,
                IsVisible = IsVisible,
                IsEditable = IsEditable,
                Tag = Tag,
                ZIndex = ZIndex,
                DisplayColor = DisplayColor,
                DisplayOpacity = DisplayOpacity,
                CreatedTime = CreatedTime,
                ModifiedTime = DateTime.Now
            };
            foreach (var kvp in ExtendedProperties)
            {
                clone.ExtendedProperties[kvp.Key] = kvp.Value;
            }
            return clone;
        }

        public override string ToString()
        {
            var shapeType = GetShapeType()?.ToString() ?? "Unknown";
            return $"{Name} ({GetMode()}: {shapeType})";
        }
    }
}
