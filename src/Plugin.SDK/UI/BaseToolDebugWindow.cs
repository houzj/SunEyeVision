using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Plugin.SDK.UI
{
    /// <summary>
    /// Tab配置模式
    /// </summary>
    public enum TabMode
    {
        /// <summary>
        /// 默认模式：三个Tab（基本参数、运行参数、结果展示）
        /// </summary>
        Default,

        /// <summary>
        /// 简单模式：仅一个Tab
        /// </summary>
        Simple,

        /// <summary>
        /// 参数模式：仅基本参数和结果展示
        /// </summary>
        Parameters,

        /// <summary>
        /// 自定义模式：完全自定义Tab
        /// </summary>
        Custom
    }

    /// <summary>
    /// 工具调试窗口基类 - 提供统一的Tab布局框架
    /// </summary>
    /// <remarks>
    /// 所有工具调试窗口都应继承此基类，确保UI风格一致性。
    /// 基类提供三个Tab：基本参数、运行参数、结果展示。
    /// 
    /// 使用示例：
    /// <code>
    /// public partial class ThresholdToolDebugWindow : BaseToolDebugWindow
    /// {
    ///     public ThresholdToolDebugWindow()
    ///     {
    ///         InitializeComponent();
    ///         SetBasicParamsPanel(CreateBasicParamsPanel());
    ///         SetRuntimeParamsPanel(CreateRuntimeParamsPanel());
    ///         SetResultPanel(CreateResultPanel());
    ///     }
    /// }
    /// </code>
    /// 
    /// XAML示例：
    /// <code>
    /// &lt;local:BaseToolDebugWindow x:Class="ThresholdToolDebugWindow"
    ///     xmlns:local="clr-namespace:SunEyeVision.Plugin.SDK.UI;assembly=SunEyeVision.Plugin.SDK"&gt;
    ///     &lt;!-- 无需定义布局，由基类提供 --&gt;
    /// &lt;/local:BaseToolDebugWindow&gt;
    /// </code>
    /// 
    /// Tab配置示例：
    /// <code>
    /// // 简单模式（仅一个Tab）
    /// TabConfig = TabMode.Simple;
    /// 
    /// // 自定义Tab
    /// TabConfig = TabMode.Custom;
    /// ClearAllTabs();
    /// AddCustomTab("参数", CreateParamsPanel());
    /// AddCustomTab("预览", CreatePreviewPanel());
    /// </code>
    /// </remarks>
    public class BaseToolDebugWindow : Window
    {
        /// <summary>
        /// Tab配置模式
        /// </summary>
        protected TabMode TabConfig { get; set; } = TabMode.Default;

        /// <summary>
        /// 所有Tab项集合（用于自定义管理）
        /// </summary>
        protected System.Windows.Controls.ItemCollection AllTabs => MainTabControl.Items;
        protected TabControl MainTabControl { get; private set; } = null!;
        protected TabItem BasicParamsTab { get; private set; } = null!;
        protected TabItem RuntimeParamsTab { get; private set; } = null!;
        protected TabItem ResultTab { get; private set; } = null!;

        protected ScrollViewer BasicParamsScrollViewer { get; private set; } = null!;
        protected ScrollViewer RuntimeParamsScrollViewer { get; private set; } = null!;
        protected ScrollViewer ResultScrollViewer { get; private set; } = null!;

        protected StackPanel BasicParamsPanel { get; private set; } = null!;
        protected StackPanel RuntimeParamsPanel { get; private set; } = null!;
        protected StackPanel ResultPanel { get; private set; } = null!;

        private ITool? _tool;
        private string _nodeName = string.Empty;

        /// <summary>
        /// 关联的工具实例
        /// </summary>
        public ITool? Tool
        {
            get => _tool;
            set
            {
                _tool = value;
                OnToolChanged(value);
            }
        }

        /// <summary>
        /// 节点名称 - 用于区分多个相同工具节点
        /// </summary>
        public string NodeName
        {
            get => _nodeName;
            set
            {
                _nodeName = value;
                UpdateTitleDisplay();
            }
        }

        public BaseToolDebugWindow()
        {
            InitializeBaseLayout();
        }

        /// <summary>
        /// 初始化基础布局
        /// </summary>
        private void InitializeBaseLayout()
        {
            // 窗口基础设置
            Title = "工具调试";
            Height = 500;
            Width = 400;
            MinHeight = 400;
            MinWidth = 350;
            MaxHeight = 600;
            MaxWidth = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.White;
            ResizeMode = ResizeMode.NoResize;

            // 创建主容器
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标题栏
            var titleBar = CreateTitleBar();
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // Tab控制区
            MainTabControl = new TabControl
            {
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0)
            };
            Grid.SetRow(MainTabControl, 1);
            mainGrid.Children.Add(MainTabControl);

            // 创建三个Tab
            CreateTabs();

            // 底部操作栏
            var bottomBar = CreateBottomBar();
            Grid.SetRow(bottomBar, 2);
            mainGrid.Children.Add(bottomBar);

            Content = mainGrid;
        }

        /// <summary>
        /// 创建标题栏（无图标，仅显示节点名称）
        /// </summary>
        private Border CreateTitleBar()
        {
            var titleText = new TextBlock
            {
                Name = "txtTitle",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                VerticalAlignment = VerticalAlignment.Center
            };

            return new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 6, 12, 6),
                Child = titleText
            };
        }

        /// <summary>
        /// 更新标题显示
        /// </summary>
        private void UpdateTitleDisplay()
        {
            var txtTitle = FindChild<TextBlock>(this, "txtTitle");
            if (txtTitle != null)
            {
                txtTitle.Text = string.IsNullOrEmpty(NodeName) ? Title : NodeName;
            }
        }

        /// <summary>
        /// 查找子元素
        /// </summary>
        private static T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    if (string.IsNullOrEmpty(childName) || typedChild.Name == childName)
                        return typedChild;
                }
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// 创建Tab（根据TabConfig配置）
        /// </summary>
        protected virtual void CreateTabs()
        {
            switch (TabConfig)
            {
                case TabMode.Simple:
                    CreateSimpleTabs();
                    break;
                case TabMode.Parameters:
                    CreateParametersTabs();
                    break;
                case TabMode.Custom:
                    // 自定义模式：子类自行添加Tab
                    break;
                default:
                    CreateDefaultTabs();
                    break;
            }
        }

        /// <summary>
        /// 创建默认三个Tab
        /// </summary>
        private void CreateDefaultTabs()
        {
            // 基本参数Tab
            BasicParamsTab = new TabItem
            {
                Header = "基本参数",
                IsSelected = true
            };
            BasicParamsScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };
            BasicParamsPanel = new StackPanel();
            BasicParamsScrollViewer.Content = BasicParamsPanel;
            BasicParamsTab.Content = BasicParamsScrollViewer;
            MainTabControl.Items.Add(BasicParamsTab);

            // 运行参数Tab
            RuntimeParamsTab = new TabItem
            {
                Header = "运行参数"
            };
            RuntimeParamsScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };
            RuntimeParamsPanel = new StackPanel();
            RuntimeParamsScrollViewer.Content = RuntimeParamsPanel;
            RuntimeParamsTab.Content = RuntimeParamsScrollViewer;
            MainTabControl.Items.Add(RuntimeParamsTab);

            // 结果展示Tab
            ResultTab = new TabItem
            {
                Header = "结果展示"
            };
            ResultScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };
            ResultPanel = new StackPanel();
            ResultScrollViewer.Content = ResultPanel;
            ResultTab.Content = ResultScrollViewer;
            MainTabControl.Items.Add(ResultTab);
        }

        /// <summary>
        /// 创建简单模式Tab（仅一个Tab）
        /// </summary>
        private void CreateSimpleTabs()
        {
            BasicParamsTab = new TabItem
            {
                Header = "参数",
                IsSelected = true
            };
            BasicParamsScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };
            BasicParamsPanel = new StackPanel();
            BasicParamsScrollViewer.Content = BasicParamsPanel;
            BasicParamsTab.Content = BasicParamsScrollViewer;
            MainTabControl.Items.Add(BasicParamsTab);

            // 简单模式不需要运行参数和结果Tab，初始化为null
            RuntimeParamsTab = null!;
            RuntimeParamsScrollViewer = null!;
            RuntimeParamsPanel = null!;
            ResultTab = null!;
            ResultScrollViewer = null!;
            ResultPanel = null!;
        }

        /// <summary>
        /// 创建参数模式Tab（基本参数和结果展示）
        /// </summary>
        private void CreateParametersTabs()
        {
            // 基本参数Tab
            BasicParamsTab = new TabItem
            {
                Header = "基本参数",
                IsSelected = true
            };
            BasicParamsScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };
            BasicParamsPanel = new StackPanel();
            BasicParamsScrollViewer.Content = BasicParamsPanel;
            BasicParamsTab.Content = BasicParamsScrollViewer;
            MainTabControl.Items.Add(BasicParamsTab);

            // 结果展示Tab
            ResultTab = new TabItem
            {
                Header = "结果展示"
            };
            ResultScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };
            ResultPanel = new StackPanel();
            ResultScrollViewer.Content = ResultPanel;
            ResultTab.Content = ResultScrollViewer;
            MainTabControl.Items.Add(ResultTab);

            // 参数模式不需要运行参数Tab，初始化为null
            RuntimeParamsTab = null!;
            RuntimeParamsScrollViewer = null!;
            RuntimeParamsPanel = null!;
        }

        #region Tab管理方法

        /// <summary>
        /// 添加自定义Tab
        /// </summary>
        /// <param name="header">Tab标题</param>
        /// <param name="content">Tab内容</param>
        /// <returns>创建的TabItem</returns>
        protected TabItem AddCustomTab(string header, FrameworkElement content)
        {
            var tab = new TabItem
            {
                Header = header,
                Content = content
            };
            MainTabControl.Items.Add(tab);
            return tab;
        }

        /// <summary>
        /// 添加自定义Tab（带ScrollViewer包装）
        /// </summary>
        /// <param name="header">Tab标题</param>
        /// <param name="content">Tab内容</param>
        /// <returns>创建的TabItem</returns>
        protected TabItem AddCustomTabWithScroll(string header, FrameworkElement content)
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12),
                Content = content
            };
            return AddCustomTab(header, scrollViewer);
        }

        /// <summary>
        /// 隐藏指定Tab
        /// </summary>
        /// <param name="tab">要隐藏的Tab</param>
        protected void HideTab(TabItem tab)
        {
            if (tab != null && MainTabControl.Items.Contains(tab))
            {
                MainTabControl.Items.Remove(tab);
            }
        }

        /// <summary>
        /// 设置Tab标题
        /// </summary>
        /// <param name="tab">目标Tab</param>
        /// <param name="header">新标题</param>
        protected void SetTabHeader(TabItem tab, string header)
        {
            if (tab != null)
            {
                tab.Header = header;
            }
        }

        /// <summary>
        /// 清除所有Tab
        /// </summary>
        protected void ClearAllTabs()
        {
            MainTabControl.Items.Clear();
        }

        /// <summary>
        /// 获取当前选中的Tab
        /// </summary>
        protected TabItem? GetSelectedTab()
        {
            return MainTabControl.SelectedItem as TabItem;
        }

        /// <summary>
        /// 选中指定Tab
        /// </summary>
        protected void SelectTab(TabItem tab)
        {
            if (tab != null && MainTabControl.Items.Contains(tab))
            {
                tab.IsSelected = true;
            }
        }

        #endregion

        /// <summary>
        /// 创建底部操作栏
        /// </summary>
        private Border CreateBottomBar()
        {
            var runButton = new Button
            {
                Content = "▶ 运行",
                Padding = new Thickness(24, 8, 24, 8),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1890FF")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            runButton.Click += (s, e) => OnExecuteRequested();

            var resetButton = new Button
            {
                Content = "重置",
                Padding = new Thickness(24, 8, 24, 8),
                FontSize = 13,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666")),
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D9D9D9")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            resetButton.Click += (s, e) => OnResetRequested();

            return new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 8, 12, 8),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { runButton, resetButton }
                }
            };
        }

        /// <summary>
        /// 设置窗口标题
        /// </summary>
        protected void SetWindowTitle(string title)
        {
            Title = title;
        }

        /// <summary>
        /// 设置基本参数面板内容
        /// </summary>
        protected void SetBasicParamsContent(FrameworkElement content)
        {
            BasicParamsPanel.Children.Clear();
            BasicParamsPanel.Children.Add(content);
        }

        /// <summary>
        /// 向基本参数面板添加控件
        /// </summary>
        protected void AddToBasicParams(FrameworkElement control)
        {
            BasicParamsPanel.Children.Add(control);
        }

        /// <summary>
        /// 设置运行参数面板内容
        /// </summary>
        protected void SetRuntimeParamsContent(FrameworkElement content)
        {
            RuntimeParamsPanel.Children.Clear();
            RuntimeParamsPanel.Children.Add(content);
        }

        /// <summary>
        /// 向运行参数面板添加控件
        /// </summary>
        protected void AddToRuntimeParams(FrameworkElement control)
        {
            RuntimeParamsPanel.Children.Add(control);
        }

        /// <summary>
        /// 设置结果展示面板内容
        /// </summary>
        protected void SetResultContent(FrameworkElement content)
        {
            ResultPanel.Children.Clear();
            ResultPanel.Children.Add(content);
        }

        /// <summary>
        /// 向结果展示面板添加控件
        /// </summary>
        protected void AddToResult(FrameworkElement control)
        {
            ResultPanel.Children.Add(control);
        }

        /// <summary>
        /// 工具变更时调用 - 子类重写以更新UI
        /// </summary>
        protected virtual void OnToolChanged(ITool? tool)
        {
        }

        /// <summary>
        /// 运行按钮点击时调用 - 子类重写以实现执行逻辑
        /// </summary>
        protected virtual void OnExecuteRequested()
        {
            ExecuteRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 重置按钮点击时调用 - 子类重写以实现重置逻辑
        /// </summary>
        protected virtual void OnResetRequested()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 执行请求事件
        /// </summary>
        public event EventHandler? ExecuteRequested;

        /// <summary>
        /// 重置请求事件
        /// </summary>
        public event EventHandler? ResetRequested;
    }
}
