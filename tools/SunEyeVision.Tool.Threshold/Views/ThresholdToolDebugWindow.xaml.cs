using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

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

        public ThresholdToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new ThresholdToolViewModel();
            DataContext = _viewModel;
            InitializeUI();
        }

        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = _viewModel.ToolName; // 使用节点名称
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

            // ROI设置
            AddToBasicParams(CreateROIPanel());

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
            _viewModel?.RunTool();
            base.OnExecuteRequested();
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

        private Border CreateROIPanel()
        {
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
                        new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = GridLength.Auto },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = GridLength.Auto }
                            },
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "ROI区域",
                                    FontSize = 13,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Margin = new Thickness(0, 0, 12, 0)
                                }.SetGrid(0, 0),
                                new TextBlock
                                {
                                    Text = "X: 0  Y: 0  W: 640  H: 480",
                                    FontSize = 12,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"))
                                }.SetGrid(0, 1),
                                new Button
                                {
                                    Content = "设置",
                                    Width = 50,
                                    Height = 24,
                                    FontSize = 12,
                                    Padding = new Thickness(0)
                                }.SetGrid(0, 2)
                            }
                        }
                    }
                }
            };
        }

        private Border CreateResultPanel()
        {
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
                        new TextBlock
                        {
                            Text = "输出图像",
                            FontSize = 13,
                            FontWeight = FontWeights.Medium,
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        new Border
                        {
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                            Height = 200,
                            CornerRadius = new CornerRadius(4),
                            Child = new TextBlock
                            {
                                Text = "预览区域",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"))
                            }
                        },
                        new TextBlock
                        {
                            Text = "执行结果",
                            FontSize = 13,
                            FontWeight = FontWeights.Medium,
                            Margin = new Thickness(0, 16, 0, 8)
                        },
                        new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition { Width = GridLength.Auto },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                            },
                            Children =
                            {
                                new TextBlock { Text = "阈值: ", FontSize = 12 }.SetGrid(0, 0),
                                new TextBlock { Text = "128", FontSize = 12, FontWeight = FontWeights.Medium }.SetGrid(0, 1),
                                new TextBlock { Text = "最大值: ", FontSize = 12 }.SetGrid(1, 0),
                                new TextBlock { Text = "255", FontSize = 12, FontWeight = FontWeights.Medium }.SetGrid(1, 1),
                                new TextBlock { Text = "执行时间: ", FontSize = 12 }.SetGrid(2, 0),
                                new TextBlock { Text = "12.5ms", FontSize = 12, FontWeight = FontWeights.Medium }.SetGrid(2, 1)
                            }
                        }
                    }
                }
            };
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
