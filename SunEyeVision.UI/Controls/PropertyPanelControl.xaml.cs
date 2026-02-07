using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// PropertyPanelControl.xaml 的交互逻辑
    /// </summary>
    public partial class PropertyPanelControl : UserControl
    {
        private bool _isUpdatingLogText = false; // 防止递归更新日志

        public static readonly DependencyProperty PropertyGroupsProperty =
            DependencyProperty.Register("PropertyGroups", typeof(ObservableCollection<PropertyGroup>), typeof(PropertyPanelControl),
                new PropertyMetadata(new ObservableCollection<PropertyGroup>(), OnPropertyGroupsChanged));

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register("SelectedNode", typeof(WorkflowNode), typeof(PropertyPanelControl),
                new PropertyMetadata(null, OnSelectedNodeChanged));

        public static readonly DependencyProperty LogTextProperty =
            DependencyProperty.Register("LogText", typeof(string), typeof(PropertyPanelControl),
                new PropertyMetadata("", OnLogTextChanged));

        public ObservableCollection<PropertyGroup> PropertyGroups
        {
            get => (ObservableCollection<PropertyGroup>)GetValue(PropertyGroupsProperty);
            set => SetValue(PropertyGroupsProperty, value);
        }

        public WorkflowNode SelectedNode
        {
            get => (WorkflowNode)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public string LogText
        {
            get => (string)GetValue(LogTextProperty);
            set => SetValue(LogTextProperty, value);
        }

        public PropertyPanelControl()
        {
            InitializeComponent();
            PropertyGroups = new ObservableCollection<PropertyGroup>();
        }

        private static void OnPropertyGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyPanelControl control)
            {
                control.UpdateNoSelectionTextVisibility();
            }
        }

        private static void OnSelectedNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PropertyPanelControl control)
            {
                control.LoadNodeProperties(e.NewValue as WorkflowNode);
            }
        }

        private static void OnLogTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 日志文本变化时无需特殊处理
        }

        private void UpdateNoSelectionTextVisibility()
        {
            NoSelectionText.Visibility = PropertyGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadNodeProperties(WorkflowNode node)
        {
            PropertyGroups.Clear();

            if (node == null)
            {
                UpdateNoSelectionTextVisibility();
                return;
            }

            // 基本信息
            var basicGroup = new PropertyGroup
            {
                Name = "📋 基本信息",
                IsExpanded = true,
                Parameters = new ObservableCollection<PropertyItem>
                {
                    new PropertyItem { Label = "名称:", Value = node.Name },
                    new PropertyItem { Label = "ID:", Value = node.Id },
                    new PropertyItem { Label = "类型:", Value = node.AlgorithmType }
                }
            };

            PropertyGroups.Add(basicGroup);

            // 参数配置
            if (node.Parameters != null && node.Parameters.Count > 0)
            {
                var paramGroup = new PropertyGroup
                {
                    Name = "🔧 参数配置",
                    IsExpanded = true,
                    Parameters = new ObservableCollection<PropertyItem>()
                };

                foreach (var param in node.Parameters)
                {
                    paramGroup.Parameters.Add(new PropertyItem
                    {
                        Label = $"{param.Key}:",
                        Value = param.Value?.ToString() ?? ""
                    });
                }

                PropertyGroups.Add(paramGroup);
            }

            // 性能统计
            var perfGroup = new PropertyGroup
            {
                Name = "📊 性能统计",
                IsExpanded = true,
                Parameters = new ObservableCollection<PropertyItem>
                {
                    new PropertyItem { Label = "状态:", Value = node.Status }
                }
            };

            PropertyGroups.Add(perfGroup);

            UpdateNoSelectionTextVisibility();
        }

        /// <summary>
        /// 添加日志条目
        /// </summary>
        public void AddLogEntry(string message)
        {
            if (_isUpdatingLogText) return; // 防止递归

            try
            {
                _isUpdatingLogText = true;
                var timestamp = System.DateTime.Now.ToString("[HH:mm:ss]");
                LogText += $"{timestamp} {message}\n";
            }
            finally
            {
                _isUpdatingLogText = false;
            }
        }

        /// <summary>
        /// 日志文本框加载事件
        /// </summary>
        private void LogTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保ScrollViewer滚动到底部（显示最新的日志）
            if (LogScrollViewer != null)
            {
                LogScrollViewer.ScrollToEnd();
            }
        }

        /// <summary>
        /// 日志文本变化事件
        /// </summary>
        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 如果是我们自己修改日志文本，不需要处理
            if (_isUpdatingLogText) return;

            // 日志变化时自动滚动到底部（显示最新日志）
            if (LogScrollViewer != null)
            {
                LogScrollViewer.ScrollToEnd();
            }
        }

        /// <summary>
        /// TabControl 选中项变化事件
        /// </summary>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 空实现，预留扩展
        }
    }
}
