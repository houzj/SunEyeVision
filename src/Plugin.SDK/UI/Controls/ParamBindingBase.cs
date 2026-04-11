using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Controls
{
    /// <summary>
    /// 参数绑定基类，提供数据源管理、树形结构构建、类型过滤等公共功能
    /// </summary>
    /// <remarks>
    /// V2.0 设计原则：
    /// 1. ParamBinding 继承此基类，专注于参数绑定功能
    /// 2. ConfigSetting 继承此基类，专注于参数值编辑功能
    /// 3. 基类提供公共的树形结构构建和类型过滤方法
    /// </remarks>
    public abstract class ParamBindingBase : Control, INotifyPropertyChanged
    {
        #region 依赖属性

        /// <summary>
        /// 数据类型过滤器
        /// </summary>
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(
                nameof(DataType),
                typeof(ParamDataType),
                typeof(ParamBindingBase),
                new PropertyMetadata(ParamDataType.Double, OnDataTypeChanged));

        #endregion

        #region 公共属性

        /// <summary>
        /// 数据类型过滤器（ParamDataType 枚举）
        /// </summary>
        public ParamDataType DataType
        {
            get => (ParamDataType)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        #endregion

        #region 事件

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region 构造函数

        static ParamBindingBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ParamBindingBase),
                new FrameworkPropertyMetadata(typeof(ParamBindingBase)));
        }

        protected ParamBindingBase()
        {
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取可用的数据源（抽象方法，由子类实现）
        /// </summary>
        /// <returns>数据源集合</returns>
        public abstract List<AvailableDataSource> GetAvailableDataSources();

        /// <summary>
        /// 从数据源列表构建树形结构
        /// </summary>
        /// <remarks>
        /// 按节点名称分组，每个节点作为根节点，该节点的属性作为子节点
        /// 示例：
        /// - 节点A
        ///   - 结果
        ///     - 输出图像
        ///     - 实际使用的阈值
        /// - 节点B
        ///   - 图像
        ///     - 宽度
        ///     - 高度
        /// </remarks>
        /// <param name="dataSources">数据源列表</param>
        /// <returns>树形结构的根节点列表</returns>
        public static List<TreeNodeData> BuildTreeStructure(List<AvailableDataSource> dataSources)
        {
            var rootNodes = new List<TreeNodeData>();
            var nodeGroups = new Dictionary<string, List<AvailableDataSource>>();

            // 按节点名称分组
            foreach (var dataSource in dataSources)
            {
                var groupName = dataSource.SourceNodeName;
                if (!nodeGroups.ContainsKey(groupName))
                {
                    nodeGroups[groupName] = new List<AvailableDataSource>();
                }
                nodeGroups[groupName].Add(dataSource);
            }

            // 为每个节点创建一个根节点
            foreach (var (nodeName, nodeDataSources) in nodeGroups)
            {
                var rootNode = TreeNodeData.CreateGroupNode(nodeName);
                
                // 处理该节点的所有数据源
                var propertyCache = new Dictionary<string, TreeNodeData>();
                
                foreach (var dataSource in nodeDataSources)
                {
                    if (string.IsNullOrEmpty(dataSource.FullTreeName))
                    {
                        // 没有 TreeName：直接添加为叶子节点
                        var leafNode = TreeNodeData.CreateDataSourceNode(dataSource);
                        rootNode.Children.Add(leafNode);
                    }
                    else
                    {
                        // 有 TreeName：解析并创建多级树结构，添加到根节点
                        BuildOrMergeTreeNodeFromFullTreeName(dataSource.FullTreeName!, dataSource, rootNode.Children, propertyCache, rootNode);
                    }
                }
                
                rootNodes.Add(rootNode);
            }

            return rootNodes;
        }

        /// <summary>
        /// 从完整树形名称构建或合并树节点（跳过根节点名称）
        /// </summary>
        /// <param name="fullTreeName">完整树形名称（例如: "阈值工具.结果.实际使用的阈值"）</param>
        /// <param name="dataSource">数据源</param>
        /// <param name="rootNodes">根节点列表（实际上是根节点的 Children）</param>
        /// <param name="nodeCache">节点缓存字典（key: 路径，value: 节点）</param>
        /// <param name="actualRootNode">实际的根节点（来自 BuildTreeStructure）</param>
        private static void BuildOrMergeTreeNodeFromFullTreeName(string fullTreeName, AvailableDataSource dataSource,
            ObservableCollection<TreeNodeData> rootNodes, Dictionary<string, TreeNodeData> nodeCache, TreeNodeData actualRootNode)
        {
            // 将 TreeName 按 `.` 分割
            var parts = fullTreeName.Split('.');

            TreeNodeData? parentNode = null;
            string currentPath = string.Empty;

            // 处理每个层级（跳过根节点名称，从索引 1 开始）
            // 根节点名称（parts[0]）已经在 BuildTreeStructure 中创建
            for (int i = 1; i < parts.Length; i++)
            {
                // 构建当前路径
                if (string.IsNullOrEmpty(currentPath))
                {
                    currentPath = parts[i];
                }
                else
                {
                    currentPath = currentPath + "." + parts[i];
                }

                TreeNodeData? node;

                if (i == parts.Length - 1)
                {
                    // 最后一个部分是叶子节点
                    node = TreeNodeData.CreateDataSourceNode(dataSource);
                    node.Text = parts[i];

                    // 将叶子节点添加到父节点的 Children 中
                    if (parentNode != null)
                    {
                        node.Parent = parentNode;
                        parentNode.Children.Add(node);
                    }
                    else
                    {
                        // 特殊情况：叶子节点直接作为根节点（FullTreeName 只有一层）
                        // 这种情况下，节点应该添加到 actualRootNode
                        node.Parent = actualRootNode;
                        rootNodes.Add(node);
                    }
                }
                else
                {
                    // 中间部分是分组节点
                    if (nodeCache.TryGetValue(currentPath, out node))
                    {
                        // 已存在相同的父节点，直接复用
                        parentNode = node;
                        continue;
                    }

                    // 创建新的分组节点
                    node = TreeNodeData.CreateGroupNode(parts[i]);
                    nodeCache[currentPath] = node;

                    // 添加到父节点或根节点列表
                    if (parentNode != null)
                    {
                        node.Parent = parentNode;
                        parentNode.Children.Add(node);
                    }
                    else
                    {
                        // 第一个分组节点，应该添加到 actualRootNode
                        node.Parent = actualRootNode;
                        rootNodes.Add(node);
                    }
                }

                parentNode = node;
            }
        }

        /// <summary>
        /// 根据参数类型过滤数据源
        /// </summary>
        /// <param name="dataSources">数据源集合</param>
        /// <param name="dataType">目标数据类型</param>
        /// <returns>过滤后的数据源</returns>
        public static List<AvailableDataSource> FilterDataSourcesByType(List<AvailableDataSource> dataSources, ParamDataType dataType)
        {
            // 将 ParamDataType 映射到 OutputTypeCategory
            OutputTypeCategory? expectedCategory = dataType switch
            {
                ParamDataType.Int => OutputTypeCategory.Numeric,
                ParamDataType.Double => OutputTypeCategory.Numeric,
                ParamDataType.String => OutputTypeCategory.Text,
                ParamDataType.Bool => OutputTypeCategory.Numeric,
                _ => null
            };
            
            if (expectedCategory == null)
            {
                // 未指定类型：返回所有数据源
                return dataSources;
            }
            
            // 过滤匹配的数据源
            var filteredDataSources = new List<AvailableDataSource>();
            
            foreach (var dataSource in dataSources)
            {
                if (dataSource.PropertyType != null)
                {
                    // 使用 OutputTypeCategoryMapper 获取数据源的类型分类
                    var sourceCategory = OutputTypeCategoryMapper.GetCategory(dataSource.PropertyType);
                    
                    // 比较分类是否匹配
                    if (sourceCategory == expectedCategory)
                    {
                        filteredDataSources.Add(dataSource);
                    }
                }
            }
            
            return filteredDataSources;
        }

        #endregion

        #region 受保护方法

        /// <summary>
        /// 触发属性更改事件
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region 事件处理程序

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ParamBindingBase control)
            {
                // 子类可以重写此方法进行额外处理
            }
        }

        #endregion
    }
}
