using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Plugin.SDK.UI
{
    /// <summary>
    /// 工具调试窗口基类 - 提供Tabs插槽架构
    /// </summary>
    /// <remarks>
    /// SDK层提供框架（标题栏、TabControl、底部按钮栏），工具层通过Tabs属性（XAML或代码）添加自定义TabItem。
    /// 
    /// 使用示例（XAML方式 - 推荐）：
    /// <code>
    /// &lt;ui:BaseToolDebugWindow x:Class="MyTool.Views.MyToolDebugWindow"
    ///         xmlns:ui="clr-namespace:SunEyeVision.Plugin.SDK.UI;assembly=SunEyeVision.Plugin.SDK"
    ///         NodeName="{Binding ToolName}"&gt;
    ///     
    ///     &lt;ui:BaseToolDebugWindow.Tabs&gt;
    ///         &lt;TabItem Header="参数" IsSelected="True"&gt;
    ///             &lt;ScrollViewer Padding="12"&gt;
    ///                 &lt;StackPanel&gt;
    ///                     &lt;sdk:BindableParameter Label="阈值" .../&gt;
    ///                 &lt;/StackPanel&gt;
    ///             &lt;/ScrollViewer&gt;
    ///         &lt;/TabItem&gt;
    ///     &lt;/ui:BaseToolDebugWindow.Tabs&gt;
    /// &lt;/ui:BaseToolDebugWindow&gt;
    /// </code>
    /// 
    /// 使用示例（代码方式）：
    /// <code>
    /// public MyToolDebugWindow()
    /// {
    ///     var panel = new StackPanel();
    ///     panel.Children.Add(new BindableParameter { Label = "阈值" });
    ///     AddTab("参数", new ScrollViewer { Content = panel }, true);
    /// }
    /// </code>
    /// </remarks>
    public class BaseToolDebugWindow : Window
    {
        #region 字段

        protected TextBlock PART_TitleText = null!;
        private TabControl _tabControl = null!;

        #endregion

        #region 依赖属性

        public static readonly DependencyProperty NodeNameProperty =
            DependencyProperty.Register(nameof(NodeName), typeof(string), typeof(BaseToolDebugWindow),
                new PropertyMetadata(string.Empty, OnNodeNameChanged));

        /// <summary>
        /// Tabs依赖属性 - 支持XAML绑定
        /// </summary>
        public static readonly DependencyProperty TabsProperty =
            DependencyProperty.Register(nameof(Tabs), typeof(ObservableCollection<TabItem>), typeof(BaseToolDebugWindow),
                new PropertyMetadata(null));

        #endregion

        #region 属性

        /// <summary>
        /// 节点名称 - 显示在标题栏
        /// </summary>
        public string NodeName
        {
            get => (string)GetValue(NodeNameProperty);
            set => SetValue(NodeNameProperty, value);
        }

        /// <summary>
        /// Tab页集合 - 工具层通过此属性添加自定义TabItem
        /// </summary>
        public ObservableCollection<TabItem> Tabs
        {
            get => (ObservableCollection<TabItem>)GetValue(TabsProperty);
            set => SetValue(TabsProperty, value);
        }

        /// <summary>
        /// 关联的工具实例
        /// </summary>
        public IToolPlugin? Tool { get; set; }

        #endregion

        #region 事件

        /// <summary>
        /// 运行按钮点击事件
        /// </summary>
        public event EventHandler? ExecuteRequested;

        /// <summary>
        /// 重置按钮点击事件
        /// </summary>
        public event EventHandler? ResetRequested;

        #endregion

        #region 主窗口ImageControl绑定

        /// <summary>
        /// 主窗口的ImageControl引用 - 用于区域编辑器绑定
        /// </summary>
        protected ImageControl? _mainImageControl;

        /// <summary>
        /// 设置主窗口的ImageControl - 用于区域编辑器绑定
        /// </summary>
        /// <param name="imageControl">主窗口的ImageControl</param>
        /// <remarks>
        /// 子类可重写此方法，将ImageControl传递给内部的RegionEditorControl或其他需要绑定的控件。
        /// 这样区域编辑器可以在主窗口的ImageControl上绘制区域。
        /// </remarks>
        public virtual void SetMainImageControl(ImageControl? imageControl)
        {
            _mainImageControl = imageControl;
        }

        #endregion

        #region 构造函数

        public BaseToolDebugWindow()
        {
            Tabs = new ObservableCollection<TabItem>();
            
            // 设计时跳过UI创建
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // 设置窗口属性
            Title = "工具调试";
            Height = 500;
            Width = 400;
            MinHeight = 400;
            MinWidth = 350;
            MaxHeight = 600;
            MaxWidth = 450;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.White;

            // 创建UI
            CreateUI();
        }

        #endregion

        #region UI创建

        private void CreateUI()
        {
            // 加载资源字典
            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/SunEyeVision.Plugin.SDK;component/UI/Themes/Generic.xaml")
            };
            Resources.MergedDictionaries.Add(dict);

            // 主Grid
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标题栏
            var titleBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 6, 12, 6)
            };
            Grid.SetRow(titleBorder, 0);

            PART_TitleText = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleBorder.Child = PART_TitleText;
            mainGrid.Children.Add(titleBorder);

            // TabControl
            _tabControl = new TabControl
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            _tabControl.SetBinding(TabControl.ItemsSourceProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Tabs))
            });
            Grid.SetRow(_tabControl, 1);
            mainGrid.Children.Add(_tabControl);

            // 底部按钮栏
            var bottomBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 8, 12, 8)
            };
            Grid.SetRow(bottomBorder, 2);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // 运行按钮
            var runButton = CreateButton("▶ 运行", "#1890FF", Brushes.White, OnRunButtonClick);
            runButton.Margin = new Thickness(0, 0, 8, 0);
            runButton.Padding = new Thickness(24, 8, 24, 8);
            runButton.FontWeight = FontWeights.Bold;
            runButton.Tag = "#40A9FF"; // Hover color
            runButton.MouseEnter += OnButtonMouseEnter;
            runButton.MouseLeave += OnButtonMouseLeave;
            buttonPanel.Children.Add(runButton);

            // 重置按钮
            var resetButton = CreateButton("重置", "#F5F5F5", 
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")), 
                OnResetButtonClick);
            resetButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9D9D9"));
            resetButton.BorderThickness = new Thickness(1);
            resetButton.Padding = new Thickness(24, 8, 24, 8);
            resetButton.Tag = "#E8E8E8"; // Hover color
            resetButton.MouseEnter += OnButtonMouseEnter;
            resetButton.MouseLeave += OnButtonMouseLeave;
            buttonPanel.Children.Add(resetButton);

            bottomBorder.Child = buttonPanel;
            mainGrid.Children.Add(bottomBorder);

            Content = mainGrid;
        }

        private Button CreateButton(string content, string bgColor, Brush fgColor, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = content,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                Foreground = fgColor,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13,
                Template = CreateButtonTemplate()
            };
            button.Click += clickHandler;
            return button;
        }

        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath(Button.BackgroundProperty) });
            borderFactory.SetBinding(Border.BorderBrushProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath(Button.BorderBrushProperty) });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath(Button.BorderThicknessProperty) });
            borderFactory.SetBinding(Border.PaddingProperty, new Binding { RelativeSource = RelativeSource.TemplatedParent, Path = new PropertyPath(Button.PaddingProperty) });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;
            return template;
        }

        private void OnButtonMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button button && button.Tag is string hoverColor)
            {
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hoverColor));
            }
        }

        private void OnButtonMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button button)
            {
                // 恢复原色
                if (button.Content?.ToString() == "▶ 运行")
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1890FF"));
                else
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            }
        }

        #endregion

        #region 控件引用解析

        /// <summary>
        /// 自动解析命名控件引用
        /// </summary>
        /// <remarks>
        /// 命名约定：后台字段名 = _ + XAML控件名
        /// 例如：字段 _thresholdParam 对应 XAML x:Name="thresholdParam"
        /// </remarks>
        protected void ResolveNamedControls()
        {
            var type = GetType();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            foreach (var field in type.GetFields(flags))
            {
                // 只处理以_开头且不以Property结尾的字段
                if (!field.Name.StartsWith("_") || field.Name.EndsWith("Property"))
                    continue;
                    
                // 提取控件名：_controlName -> controlName
                var controlName = field.Name.Substring(1);
                var control = FindName(controlName);
                
                if (control != null && field.FieldType.IsInstanceOfType(control))
                {
                    field.SetValue(this, control);
                }
            }
        }

        #endregion

        #region 属性变更回调

        private static void OnNodeNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseToolDebugWindow window)
            {
                window.PART_TitleText.Text = e.NewValue as string ?? string.Empty;
                window.Title = e.NewValue as string ?? "工具调试";
            }
        }

        #endregion

        #region 公开方法 - Tab管理

        /// <summary>
        /// 添加一个Tab页
        /// </summary>
        /// <param name="header">Tab标题</param>
        /// <param name="content">Tab内容</param>
        /// <param name="isSelected">是否选中</param>
        /// <returns>创建的TabItem</returns>
        public TabItem AddTab(string header, object content, bool isSelected = false)
        {
            var tabItem = new TabItem
            {
                Header = header,
                Content = content,
                IsSelected = isSelected
            };
            Tabs.Add(tabItem);
            return tabItem;
        }

        /// <summary>
        /// 移除指定Tab页
        /// </summary>
        public void RemoveTab(TabItem tabItem)
        {
            Tabs.Remove(tabItem);
        }

        /// <summary>
        /// 清空所有Tab页
        /// </summary>
        public void ClearTabs()
        {
            Tabs.Clear();
        }

        /// <summary>
        /// 获取当前选中的Tab
        /// </summary>
        public TabItem? GetSelectedTab() => Tabs.FirstOrDefault(t => t.IsSelected);

        /// <summary>
        /// 选中指定索引的Tab
        /// </summary>
        public void SelectTab(int index)
        {
            if (index >= 0 && index < Tabs.Count)
                Tabs[index].IsSelected = true;
        }

        /// <summary>
        /// 根据Header选中Tab
        /// </summary>
        public void SelectTabByHeader(string header)
        {
            foreach (var tab in Tabs)
            {
                if (tab.Header?.ToString() == header)
                {
                    tab.IsSelected = true;
                    break;
                }
            }
        }

        #endregion

        #region 按钮事件处理

        private void OnRunButtonClick(object sender, RoutedEventArgs e)
        {
            OnExecuteRequested();
        }

        private void OnResetButtonClick(object sender, RoutedEventArgs e)
        {
            OnResetRequested();
        }

        protected virtual void OnExecuteRequested()
        {
            ExecuteRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnResetRequested()
        {
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 绑定配置辅助方法

        protected ParameterBindingContainer CollectBindings(params Controls.BindableParameter[] controls)
        {
            var container = new ParameterBindingContainer();
            foreach (var control in controls)
                if (control != null)
                    container.SetBinding(control.ToParameterBinding());
            return container;
        }

        protected void ApplyBindings(ParameterBindingContainer container, params (string name, Controls.BindableParameter control)[] mappings)
        {
            if (container == null) return;
            foreach (var (name, control) in mappings)
            {
                if (control == null) continue;
                var binding = container.GetBinding(name);
                if (binding != null)
                    control.FromParameterBinding(binding);
            }
        }

        #endregion
    }
}
