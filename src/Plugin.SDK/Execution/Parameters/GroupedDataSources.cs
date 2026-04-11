using System;
using System.Collections.Generic;
using System.Linq;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 分组数据源容器
    /// </summary>
    /// <remarks>
    /// 按照输出类型分类存储数据源。
    /// 用于在 UI 中分组显示可用的数据源。
    /// 
    /// 分组类别：
    /// - ImageSources: 图像数据源
    /// - ShapeSources: 形状数据源
    /// - NumericSources: 数值数据源
    /// - TextSources: 文本数据源
    /// - ListSources: 列表数据源
    /// - OtherSources: 其他数据源
    /// 
    /// 使用示例：
    /// <code>
    /// var groupedSources = new GroupedDataSources();
    /// groupedSources.AddDataSource(dataSource);
    /// 
    /// var imageSources = groupedSources.GetSourcesByCategory(OutputTypeCategory.Image);
    /// </code>
    /// </remarks>
    public class GroupedDataSources
    {
        /// <summary>
        /// 图像数据源
        /// </summary>
        public List<AvailableDataSource> ImageSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 形状数据源
        /// </summary>
        public List<AvailableDataSource> ShapeSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 数值数据源
        /// </summary>
        public List<AvailableDataSource> NumericSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 文本数据源
        /// </summary>
        public List<AvailableDataSource> TextSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 列表数据源
        /// </summary>
        public List<AvailableDataSource> ListSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 其他数据源
        /// </summary>
        public List<AvailableDataSource> OtherSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 数据源总数
        /// </summary>
        public int TotalCount => ImageSources.Count + ShapeSources.Count + NumericSources.Count + 
                                 TextSources.Count + ListSources.Count + OtherSources.Count;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => TotalCount == 0;

        /// <summary>
        /// 添加数据源（自动分类）
        /// </summary>
        /// <param name="dataSource">数据源</param>
        /// <param name="category">输出类型分类</param>
        public void AddDataSource(AvailableDataSource dataSource, OutputTypeCategory category)
        {
            if (dataSource == null)
            {
                return;
            }

            switch (category)
            {
                case OutputTypeCategory.Image:
                    ImageSources.Add(dataSource);
                    break;
                case OutputTypeCategory.Shape:
                    ShapeSources.Add(dataSource);
                    break;
                case OutputTypeCategory.Numeric:
                    NumericSources.Add(dataSource);
                    break;
                case OutputTypeCategory.Text:
                    TextSources.Add(dataSource);
                    break;
                case OutputTypeCategory.List:
                    ListSources.Add(dataSource);
                    break;
                case OutputTypeCategory.Other:
                default:
                    OtherSources.Add(dataSource);
                    break;
            }
        }

        /// <summary>
        /// 根据分类获取数据源列表
        /// </summary>
        /// <param name="category">输出类型分类</param>
        /// <returns>数据源列表</returns>
        public List<AvailableDataSource> GetSourcesByCategory(OutputTypeCategory category)
        {
            return category switch
            {
                OutputTypeCategory.Image => ImageSources,
                OutputTypeCategory.Shape => ShapeSources,
                OutputTypeCategory.Numeric => NumericSources,
                OutputTypeCategory.Text => TextSources,
                OutputTypeCategory.List => ListSources,
                OutputTypeCategory.Other => OtherSources,
                _ => OtherSources
            };
        }

        /// <summary>
        /// 获取所有数据源（扁平化列表）
        /// </summary>
        /// <returns>所有数据源列表</returns>
        public List<AvailableDataSource> GetAllSources()
        {
            var allSources = new List<AvailableDataSource>();
            allSources.AddRange(ImageSources);
            allSources.AddRange(ShapeSources);
            allSources.AddRange(NumericSources);
            allSources.AddRange(TextSources);
            allSources.AddRange(ListSources);
            allSources.AddRange(OtherSources);
            return allSources;
        }

        /// <summary>
        /// 清空所有数据源
        /// </summary>
        public void Clear()
        {
            ImageSources.Clear();
            ShapeSources.Clear();
            NumericSources.Clear();
            TextSources.Clear();
            ListSources.Clear();
            OtherSources.Clear();
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            return $"图像: {ImageSources.Count} | 形状: {ShapeSources.Count} | 数值: {NumericSources.Count} | " +
                   $"文本: {TextSources.Count} | 列表: {ListSources.Count} | 其他: {OtherSources.Count} | " +
                   $"总计: {TotalCount}";
        }

        /// <summary>
        /// 获取分组信息（用于 UI 显示）
        /// </summary>
        /// <returns>分组信息列表</returns>
        public List<DataSourceGroupInfo> GetGroupInfos()
        {
            var groups = new List<DataSourceGroupInfo>();

            if (ImageSources.Count > 0)
            {
                groups.Add(new DataSourceGroupInfo
                {
                    Category = OutputTypeCategory.Image,
                    DisplayName = "图像",
                    Icon = "🖼️",
                    Count = ImageSources.Count,
                    DataSources = ImageSources
                });
            }

            if (ShapeSources.Count > 0)
            {
                groups.Add(new DataSourceGroupInfo
                {
                    Category = OutputTypeCategory.Shape,
                    DisplayName = "形状",
                    Icon = "📐",
                    Count = ShapeSources.Count,
                    DataSources = ShapeSources
                });
            }

            if (NumericSources.Count > 0)
            {
                groups.Add(new DataSourceGroupInfo
                {
                    Category = OutputTypeCategory.Numeric,
                    DisplayName = "数值",
                    Icon = "🔢",
                    Count = NumericSources.Count,
                    DataSources = NumericSources
                });
            }

            if (TextSources.Count > 0)
            {
                groups.Add(new DataSourceGroupInfo
                {
                    Category = OutputTypeCategory.Text,
                    DisplayName = "文本",
                    Icon = "📝",
                    Count = TextSources.Count,
                    DataSources = TextSources
                });
            }

            if (ListSources.Count > 0)
            {
                groups.Add(new DataSourceGroupInfo
                {
                    Category = OutputTypeCategory.List,
                    DisplayName = "列表",
                    Icon = "📋",
                    Count = ListSources.Count,
                    DataSources = ListSources
                });
            }

            if (OtherSources.Count > 0)
            {
                groups.Add(new DataSourceGroupInfo
                {
                    Category = OutputTypeCategory.Other,
                    DisplayName = "其他",
                    Icon = "❓",
                    Count = OtherSources.Count,
                    DataSources = OtherSources
                });
            }

            return groups;
        }
    }

    /// <summary>
    /// 数据源分组信息
    /// </summary>
    public class DataSourceGroupInfo
    {
        /// <summary>
        /// 分类
        /// </summary>
        public OutputTypeCategory Category { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 数据源数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 数据源列表
        /// </summary>
        public List<AvailableDataSource> DataSources { get; set; } = new List<AvailableDataSource>();

        /// <summary>
        /// 获取显示文本
        /// </summary>
        /// <returns>格式化的显示文本</returns>
        public string GetDisplayText()
        {
            return $"{Icon} {DisplayName} ({Count})";
        }
    }
}
