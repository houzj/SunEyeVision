using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Views;

namespace SunEyeVision.Tool.Threshold.Views
{
    /// <summary>
    /// 阈值化工具调试窗口 - 使用优化后的BindableParameter控件
    /// </summary>
    public partial class ThresholdToolDebugWindow : BaseToolDebugWindow
    {
        private ThresholdToolViewModel _viewModel = null!;
        private BindableParameter _thresholdParam = null!;
        private BindableParameter _maxValueParam = null!;
        private ImageSourceSelector _imageSourceSelector = null!;
        
        // RegionEditor 相关
        private RegionEditorControl _regionEditorControl = null!;
        private RegionEditorIntegration _regionEditorIntegration = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        // 节点引用（用于配置持久化）
        private object? _currentNode;
        private System.Reflection.PropertyInfo? _parametersProperty;

        /// <summary>
        /// 工具执行完成事件（供主窗口订阅，用于更新图像显示）
        /// </summary>
        public event EventHandler<ThresholdResults>? ToolExecutionCompleted;

        public ThresholdToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ThresholdToolViewModel();
            DataContext = _viewModel;
            
            // 初始化 RegionEditor
            InitializeRegionEditor();
            
            InitializeUI();
        }

        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = _viewModel.ToolName;
            
            // 设置数据提供者（由工作流引擎注入）
            if (_dataProvider != null)
            {
                _viewModel.PopulateImageSources(_dataProvider);
                _regionEditorIntegration?.SetCurrentNodeId(toolId);
            }
        }

        private void InitializeRegionEditor()
        {
            // 创建数据提供者
            _dataProvider = new WorkflowDataSourceProvider();
            
            // 创建 RegionEditorViewModel
            var regionEditorViewModel = new RegionEditorViewModel();
            
            // 创建集成帮助类
            _regionEditorIntegration = new RegionEditorIntegration(regionEditorViewModel);
            _regionEditorIntegration.SetCurrentNodeId(_viewModel.ToolId);
            
            // 获取 RegionEditorControl
            _regionEditorControl = new RegionEditorControl
            {
                DataContext = regionEditorViewModel,
                Margin = new Thickness(0, 8, 0, 0),
                Height = 280
            };
        }

        private void InitializeUI()
        {
            // ===== 基本参数Tab =====
            // 图像源选择 - 使用ImageSourceSelector控件
            _imageSourceSelector = new ImageSourceSelector
            {
                Label = "输入图像",
                SelectedImageSource = _viewModel.SelectedImageSource,
                AvailableImageSources = _viewModel.AvailableImageSources,
                ShowThumbnail = true,
                ShowSizeInfo = true,
                PlaceholderText = "选择图像源..."
            };
            _imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;
            AddToBasicParams(_imageSourceSelector);

            // ROI设置 - 使用 RegionEditorControl
            AddToBasicParams(CreateROISection());

            // ===== 运行参数Tab =====
            AddToRuntimeParams(CreateSectionHeader("阈值参数"));

            // 使用BindableParameter控件 - 阈值
            _thresholdParam = new BindableParameter
            {
                Label = "阈值",
                Value = _viewModel.Threshold,
                Minimum = 0,
                Maximum = 255,
                SmallChange = 1,
                LargeChange = 10,
                MinValueLabel = "[0-255]",
                ShowSlider = true,
                DecimalPlaces = 0,
                AvailableBindings = _viewModel.AvailableBindings
            };
            _thresholdParam.BindingModeChanged += OnThresholdBindingModeChanged;
            AddToRuntimeParams(_thresholdParam);

            // 使用BindableParameter控件 - 最大值
            _maxValueParam = new BindableParameter
            {
                Label = "最大值",
                Value = _viewModel.MaxValue,
                Minimum = 0,
                Maximum = 255,
                SmallChange = 1,
                LargeChange = 10,
                MinValueLabel = "[0-255]",
                ShowSlider = true,
                DecimalPlaces = 0,
                AvailableBindings = _viewModel.AvailableBindings
            };
            _maxValueParam.BindingModeChanged += OnMaxValueBindingModeChanged;
            AddToRuntimeParams(_maxValueParam);

            // 阈值类型选择
            AddToRuntimeParams(CreateSectionHeader("阈值类型"));
            AddToRuntimeParams(new ParamComboBox
            {
                Label = "类型",
                ItemsSource = _viewModel.ThresholdTypes,
                SelectedItem = _viewModel.ThresholdType
            });

            // 高级参数
            AddToRuntimeParams(CreateSectionHeader("高级参数"));
            AddToRuntimeParams(new ParamSlider
            {
                Label = "块大小",
                Value = _viewModel.AdaptiveBlockSize,
                Minimum = 3,
                Maximum = 31,
                SmallChange = 2,
                LargeChange = 4,
                ShowTextBox = true
            });

            AddToRuntimeParams(new ParamSlider
            {
                Label = "常数C",
                Value = _viewModel.AdaptiveC,
                Minimum = -50,
                Maximum = 50,
                SmallChange = 1,
                LargeChange = 5,
                ShowTextBox = true
            });

            // ===== 结果展示Tab =====
            AddToResult(CreateResultPanel());
        }

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedImageSource = _imageSourceSelector.SelectedImageSource;
            
            // 如果选择了图像源，更新 RegionEditor 的数据提供者
            if (_viewModel.SelectedImageSource != null && _dataProvider != null)
            {
                // 工作流引擎应该已经调用了 UpdateNodeOutput
                // 这里我们不需要额外操作
            }
        }

        /// <summary>
        /// 设置工作流数据提供者（由工作流引擎调用）
        /// </summary>
        public void SetDataProvider(WorkflowDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
            
            // 填充图像源列表
            _viewModel.PopulateImageSources(dataProvider);
            
            // 更新 RegionEditor 数据源
            _regionEditorIntegration?.SetCurrentNodeId(_viewModel.ToolId);

            // 恢复之前保存的图像源选择
            RestoreImageSourceSelection();
        }

        /// <summary>
        /// 设置当前节点引用（用于配置持久化）
        /// </summary>
        /// <param name="node">当前工作流节点</param>
        public void SetCurrentNode(object node)
        {
            _currentNode = node;
            _parametersProperty = node?.GetType().GetProperty("Parameters");
            
            // 从节点参数加载配置
            LoadConfigFromNode();
        }

        /// <summary>
        /// 从节点参数加载配置
        /// </summary>
        private void LoadConfigFromNode()
        {
            if (_currentNode == null || _parametersProperty == null) return;

            try
            {
                var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
                if (parameters != null)
                {
                    _viewModel.LoadFromNodeParameters(parameters);
                    
                    // 更新UI控件
                    _thresholdParam.Value = _viewModel.Threshold;
                    _maxValueParam.Value = _viewModel.MaxValue;

                    System.Diagnostics.Debug.WriteLine($"[ThresholdTool] 从节点加载配置: Threshold={_viewModel.Threshold}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThresholdTool] 加载配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存配置到节点参数
        /// </summary>
        private void SaveConfigToNode()
        {
            if (_currentNode == null || _parametersProperty == null) return;

            try
            {
                // 获取或创建 Parameters 字典
                var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
                if (parameters == null)
                {
                    // 尝试创建新的字典
                    var dictType = typeof(Dictionary<string, object>);
                    parameters = Activator.CreateInstance(dictType) as IDictionary<string, object>;
                    _parametersProperty.SetValue(_currentNode, parameters);
                }

                if (parameters != null)
                {
                    _viewModel.SaveToNodeParameters(parameters);
                    System.Diagnostics.Debug.WriteLine($"[ThresholdTool] 保存配置到节点: ImageSource={_viewModel.SelectedImageSource?.NodeId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThresholdTool] 保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复图像源选择（在数据提供者就绪后调用）
        /// </summary>
        private void RestoreImageSourceSelection()
        {
            if (_currentNode == null || _parametersProperty == null || _dataProvider == null) return;

            try
            {
                var parameters = _parametersProperty.GetValue(_currentNode) as IDictionary<string, object>;
                var (savedNodeId, savedOutputPort) = ThresholdToolViewModel.GetSavedImageSource(parameters);

                if (!string.IsNullOrEmpty(savedNodeId))
                {
                    // 在可用图像源中查找匹配项
                    var matchedSource = _viewModel.AvailableImageSources.FirstOrDefault(s =>
                        s.NodeId == savedNodeId && 
                        (string.IsNullOrEmpty(savedOutputPort) || s.OutputPortName == savedOutputPort));

                    if (matchedSource != null)
                    {
                        _viewModel.SelectedImageSource = matchedSource;
                        _imageSourceSelector.SelectedImageSource = matchedSource;
                        System.Diagnostics.Debug.WriteLine($"[ThresholdTool] 恢复图像源选择: {savedNodeId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThresholdTool] 恢复图像源选择失败: {ex.Message}");
            }
        }

        private void OnThresholdBindingModeChanged(object sender, RoutedEventArgs e)
        {
            if (_thresholdParam.BindingMode == ParameterBindingMode.Binding)
            {
                _viewModel.ThresholdBindingType = new BindingTypeOption(BindingType.DynamicBinding, "动态绑定", "从其他节点获取");
                if (!string.IsNullOrEmpty(_thresholdParam.BindingSource))
                {
                    var parts = _thresholdParam.BindingSource.Split('.');
                    _viewModel.ThresholdSourceNode = parts.Length > 0 ? parts[0] : null;
                    _viewModel.ThresholdSourceProperty = parts.Length > 1 ? parts[1] : null;
                }
            }
            else
            {
                _viewModel.ThresholdBindingType = new BindingTypeOption(BindingType.Constant, "常量值", "使用固定值");
                _viewModel.ThresholdSourceNode = null;
                _viewModel.ThresholdSourceProperty = null;
            }
        }

        private void OnMaxValueBindingModeChanged(object sender, RoutedEventArgs e)
        {
            if (_maxValueParam.BindingMode == ParameterBindingMode.Binding)
            {
                _viewModel.MaxValueBindingType = new BindingTypeOption(BindingType.DynamicBinding, "动态绑定", "从其他节点获取");
                if (!string.IsNullOrEmpty(_maxValueParam.BindingSource))
                {
                    var parts = _maxValueParam.BindingSource.Split('.');
                    _viewModel.MaxValueSourceNode = parts.Length > 0 ? parts[0] : null;
                    _viewModel.MaxValueSourceProperty = parts.Length > 1 ? parts[1] : null;
                }
            }
            else
            {
                _viewModel.MaxValueBindingType = new BindingTypeOption(BindingType.Constant, "常量值", "使用固定值");
            }
        }

        protected override void OnExecuteRequested()
        {
            // 同步参数值到ViewModel
            _viewModel.Threshold = (int)_thresholdParam.Value;
            _viewModel.MaxValue = (int)_maxValueParam.Value;

            // 检查是否选择了图像源
            if (_viewModel.SelectedImageSource == null)
            {
                _viewModel.StatusMessage = "⚠️ 请选择输入图像源";
                UpdateExecutionStatus(false, "跳过执行：未选择图像源");
                return;
            }

            // 检查数据提供者
            if (_dataProvider == null)
            {
                _viewModel.StatusMessage = "⚠️ 数据提供者未初始化";
                UpdateExecutionStatus(false, "跳过执行：数据提供者未就绪");
                return;
            }

            // 检查前驱节点是否已执行
            if (!_dataProvider.HasNodeOutput(_viewModel.SelectedImageSource.NodeId))
            {
                _viewModel.StatusMessage = "⚠️ 图像源节点尚未执行，请先执行前驱节点";
                UpdateExecutionStatus(false, "跳过执行：图像源未就绪");
                return;
            }

            // 从工作流数据源获取图像
            var imageMat = _dataProvider.GetCurrentBindingValue(
                _viewModel.SelectedImageSource.NodeId,
                "Output",
                _viewModel.SelectedImageSource.OutputPortName
            ) as OpenCvSharp.Mat;

            if (imageMat == null || imageMat.Empty())
            {
                _viewModel.StatusMessage = "⚠️ 无法获取图像数据或图像为空";
                UpdateExecutionStatus(false, "跳过执行：图像数据无效");
                return;
            }

            // 订阅执行完成事件（用于通知主窗口更新显示）
            _viewModel.ExecutionCompleted -= OnViewModelExecutionCompleted;
            _viewModel.ExecutionCompleted += OnViewModelExecutionCompleted;

            // 执行工具（传入图像）
            _viewModel.RunToolWithImage(imageMat);

            // 解析 RegionEditor 中的区域
            var regions = _regionEditorControl.ResolveAllRegions();
            // 可以将 regions 传递给工具进行 ROI 处理

            base.OnExecuteRequested();
        }

        /// <summary>
        /// ViewModel 执行完成回调
        /// </summary>
        private void OnViewModelExecutionCompleted(object? sender, RunResult result)
        {
            if (result.IsSuccess && result.ToolResult is ThresholdResults thresholdResult)
            {
                // 通知主窗口更新显示
                ToolExecutionCompleted?.Invoke(this, thresholdResult);
                
                System.Diagnostics.Debug.WriteLine(
                    $"[ThresholdTool] 执行完成: 阈值={thresholdResult.ThresholdUsed:F0}, " +
                    $"图像尺寸={thresholdResult.InputSize.Width}x{thresholdResult.InputSize.Height}");
            }
        }

        /// <summary>
        /// 更新执行状态显示
        /// </summary>
        private void UpdateExecutionStatus(bool success, string message)
        {
            // 可以在此更新UI状态
            System.Diagnostics.Debug.WriteLine($"[ThresholdTool] {message}");
        }

        protected override void OnResetRequested()
        {
            if (_viewModel != null)
            {
                _viewModel.ResetParameters();
                // 重置BindableParameter控件
                _thresholdParam.Value = _viewModel.Threshold;
                _maxValueParam.Value = _viewModel.MaxValue;
            }
            base.OnResetRequested();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // 保存配置到节点参数
            SaveConfigToNode();

            _regionEditorIntegration?.Dispose();
            base.OnClosed(e);
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                Margin = new Thickness(0, 12, 0, 8)
            };
        }

        private Border CreateROISection()
        {
            var headerPanel = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Margin = new Thickness(0, 0, 0, 0)
            };

            // 标题
            var titleText = new TextBlock
            {
                Text = "处理区域",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(titleText, 0);
            headerPanel.Children.Add(titleText);

            // 模式选择按钮
            var drawingButton = new RadioButton
            {
                Content = "绘制",
                IsChecked = true,
                Margin = new Thickness(0, 0, 8, 0)
            };
            drawingButton.Checked += (s, e) => 
            {
                if (_regionEditorControl?.ViewModel != null)
                    _regionEditorControl.ViewModel.CurrentMode = RegionDefinitionMode.Drawing;
            };
            Grid.SetColumn(drawingButton, 2);
            headerPanel.Children.Add(drawingButton);

            var subscribeButton = new RadioButton
            {
                Content = "订阅"
            };
            subscribeButton.Checked += (s, e) => 
            {
                if (_regionEditorControl?.ViewModel != null)
                    _regionEditorControl.ViewModel.CurrentMode = RegionDefinitionMode.SubscribeByRegion;
            };
            Grid.SetColumn(subscribeButton, 2);
            headerPanel.Children.Add(subscribeButton);

            // RegionEditorControl（折叠面板）
            var expander = new Expander
            {
                Header = headerPanel,
                IsExpanded = false,
                Margin = new Thickness(0, 0, 0, 0)
            };

            // RegionEditorControl 作为内容
            expander.Content = _regionEditorControl;

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9D9D9")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Child = expander
            };
        }

        private Border CreateResultPanel()
        {
            // 状态图标
            var statusIcon = new TextBlock { FontSize = 16, VerticalAlignment = VerticalAlignment.Center };
            statusIcon.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath("StatusIcon"),
                Mode = BindingMode.OneWay
            });

            // 状态文本
            var statusText = new TextBlock { FontSize = 12, FontWeight = FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center };
            statusText.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath("StatusMessage"),
                Mode = BindingMode.OneWay
            });

            // 执行时间
            var timeValue = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")) };
            timeValue.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath("ExecutionTime"),
                Mode = BindingMode.OneWay
            });

            // 结果详情网格
            var detailsGrid = new Grid
            {
                Margin = new Thickness(0, 12, 0, 0)
            };
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            detailsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;

            // 阈值参数
            AddResultRow(detailsGrid, row++, "阈值", "Threshold", "{0:F0}");
            AddResultRow(detailsGrid, row++, "最大值", "MaxValue", "{0}");
            AddResultRow(detailsGrid, row++, "阈值类型", "ThresholdType");
            AddResultRow(detailsGrid, row++, "输入尺寸", "InputSizeDisplay");
            AddResultRow(detailsGrid, row++, "执行时间", "ExecutionTimeMs", "{0:F1} ms");

            // 调试信息（可折叠）
            var debugExpander = new Expander
            {
                Header = new TextBlock { Text = "调试信息", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")) },
                IsExpanded = false,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var debugText = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };
            debugText.SetBinding(TextBlock.TextProperty, new Binding
            {
                Path = new PropertyPath("DebugMessage"),
                Mode = BindingMode.OneWay
            });
            debugExpander.Content = debugText;

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D9D9D9")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new StackPanel
                {
                    Children =
                    {
                        // 标题行
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Children = { statusIcon, new Border { Width = 8 }, statusText }
                        },
                        // 分隔线
                        new Border
                        {
                            Height = 1,
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8E8")),
                            Margin = new Thickness(0, 8, 0, 0)
                        },
                        // 提示信息
                        new TextBlock
                        {
                            Text = "★ 执行后图像显示在主窗口图像区域",
                            FontSize = 11,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0066CC")),
                            Margin = new Thickness(0, 8, 0, 0)
                        },
                        // 结果详情
                        detailsGrid,
                        // 调试信息
                        debugExpander
                    }
                }
            };
        }

        /// <summary>
        /// 添加结果行
        /// </summary>
        private void AddResultRow(Grid grid, int row, string label, string bindingPath, string? format = null)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var labelBlock = new TextBlock
            {
                Text = label + "：",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(labelBlock, row);
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };

            var binding = new Binding
            {
                Path = new PropertyPath(bindingPath),
                Mode = BindingMode.OneWay
            };
            if (!string.IsNullOrEmpty(format))
            {
                binding.StringFormat = format;
            }
            valueBlock.SetBinding(TextBlock.TextProperty, binding);

            Grid.SetRow(valueBlock, row);
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);
        }
    }

    // 辅助扩展方法
    internal static class GridExtensions
    {
        public static T SetGrid<T>(this T element, int row, int column) where T : UIElement
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            return element;
        }
    }
}
