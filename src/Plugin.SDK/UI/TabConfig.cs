using System.Collections.Generic;
using System.Windows;

namespace SunEyeVision.Plugin.SDK.UI
{
    /// <summary>
    /// Tab类型枚举 - 对应基类预定义的面板
    /// </summary>
    public enum TabType
    {
        /// <summary>
        /// 基本参数面板
        /// </summary>
        BasicParams,

        /// <summary>
        /// 运行参数面板
        /// </summary>
        RuntimeParams,

        /// <summary>
        /// 结果展示面板
        /// </summary>
        Result,

        /// <summary>
        /// 自定义面板
        /// </summary>
        Custom
    }

    /// <summary>
    /// Tab项配置 - 支持XAML声明式配置
    /// </summary>
    /// <remarks>
    /// 使用示例：
    /// <code>
    /// &lt;ui:TabItemConfig Header="输入配置" Type="BasicParams" IsSelected="True"/&gt;
    /// &lt;ui:TabItemConfig Header="阈值参数" Type="RuntimeParams"/&gt;
    /// &lt;ui:TabItemConfig Header="执行结果" Type="Result"/&gt;
    /// </code>
    /// </remarks>
    public class TabItemConfig : DependencyObject
    {
        /// <summary>
        /// Tab标题依赖属性
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(TabItemConfig),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Tab类型依赖属性
        /// </summary>
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(TabType),
                typeof(TabItemConfig),
                new PropertyMetadata(TabType.BasicParams));

        /// <summary>
        /// 是否选中依赖属性
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(TabItemConfig),
                new PropertyMetadata(false));

        /// <summary>
        /// 是否可见依赖属性
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                nameof(IsVisible),
                typeof(bool),
                typeof(TabItemConfig),
                new PropertyMetadata(true));

        /// <summary>
        /// Tab标题
        /// </summary>
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Tab类型
        /// </summary>
        public TabType Type
        {
            get => (TabType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        /// <summary>
        /// 是否选中（默认第一个Tab会被选中）
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// 是否可见（默认true）
        /// </summary>
        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }
    }

    /// <summary>
    /// Tab配置集合 - 支持XAML声明式配置
    /// </summary>
    /// <remarks>
    /// 提供常用预设配置：
    /// - Default: 三Tab模式（基本参数、运行参数、结果展示）
    /// - Simple: 单Tab模式（参数）
    /// - Parameters: 双Tab模式（基本参数、结果展示）
    /// 
    /// 使用示例：
    /// <code>
    /// &lt;ui:BaseToolDebugWindow.TabsConfig&gt;
    ///     &lt;ui:TabConfigCollection&gt;
    ///         &lt;ui:TabItemConfig Header="输入配置" Type="BasicParams" IsSelected="True"/&gt;
    ///         &lt;ui:TabItemConfig Header="阈值参数" Type="RuntimeParams"/&gt;
    ///         &lt;ui:TabItemConfig Header="执行结果" Type="Result"/&gt;
    ///     &lt;/ui:TabConfigCollection&gt;
    /// &lt;/ui:BaseToolDebugWindow.TabsConfig&gt;
    /// 
    /// &lt;!-- 或使用预设 --&gt;
    /// &lt;ui:BaseToolDebugWindow.TabsConfig&gt;
    ///     &lt;x:Static Member="ui:TabConfigCollection.Simple"/&gt;
    /// &lt;/ui:BaseToolDebugWindow.TabsConfig&gt;
    /// </code>
    /// </remarks>
    public class TabConfigCollection : List<TabItemConfig>
    {
        /// <summary>
        /// 默认配置：三个Tab（基本参数、运行参数、结果展示）
        /// </summary>
        public static TabConfigCollection Default => new TabConfigCollection
        {
            new TabItemConfig { Header = "基本参数", Type = TabType.BasicParams, IsSelected = true },
            new TabItemConfig { Header = "运行参数", Type = TabType.RuntimeParams },
            new TabItemConfig { Header = "结果展示", Type = TabType.Result }
        };

        /// <summary>
        /// 简单配置：仅一个Tab
        /// </summary>
        public static TabConfigCollection Simple => new TabConfigCollection
        {
            new TabItemConfig { Header = "参数", Type = TabType.BasicParams, IsSelected = true }
        };

        /// <summary>
        /// 参数配置：基本参数和结果展示
        /// </summary>
        public static TabConfigCollection Parameters => new TabConfigCollection
        {
            new TabItemConfig { Header = "基本参数", Type = TabType.BasicParams, IsSelected = true },
            new TabItemConfig { Header = "结果展示", Type = TabType.Result }
        };
    }
}
