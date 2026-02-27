using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Plugin.SDK.UI
{
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
    /// </remarks>
    public class BaseToolDebugWindow : Window
    {
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
            Height = 600;
            Width = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.White;

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
                Margin = new Thickness(8, 0, 8, 0),
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
        /// 创建标题栏
        /// </summary>
        private Border CreateTitleBar()
        {
            return new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 12, 16, 12),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "🔧",
                            FontSize = 18,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 8, 0)
                        },
                        new TextBlock
                        {
                            Name = "txtTitle",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new System.Windows.Media.SolidColorBrush(
                                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 创建三个Tab
        /// </summary>
        private void CreateTabs()
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
                Padding = new Thickness(8)
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
                Padding = new Thickness(8)
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
                Padding = new Thickness(8)
            };
            ResultPanel = new StackPanel();
            ResultScrollViewer.Content = ResultPanel;
            ResultTab.Content = ResultScrollViewer;
            MainTabControl.Items.Add(ResultTab);
        }

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
                Padding = new Thickness(16, 12, 16, 12),
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
