using System.Windows;
using System.Windows.Controls;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;

namespace SunEyeVision.Tool.Threshold.Views
{
    /// <summary>
    /// 阈值化工具调试窗口 - 继承BaseToolDebugWindow实现Tab布局
    /// </summary>
    public partial class ThresholdToolDebugWindow : BaseToolDebugWindow
    {
        private ThresholdToolViewModel _viewModel;

        public ThresholdToolDebugWindow()
        {
            InitializeComponent();
            InitializePanels();
        }

        public ThresholdToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
            : this()
        {
            _viewModel = new ThresholdToolViewModel();
            _viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = _viewModel;
        }

        private void InitializePanels()
        {
            // ===== 基本参数Tab =====
            AddToBasicParams(CreateSectionHeader("阈值设置"));
            AddToBasicParams(new ParamComboBox
            {
                Label = "阈值类型",
                ItemsSource = new[] { "Otsu", "Binary", "BinaryInv", "Trunc", "ToZero", "ToZeroInv", "AdaptiveMean", "AdaptiveGaussian" },
                SelectedItem = "Otsu"
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "阈值",
                Value = 127,
                Minimum = 0,
                Maximum = 255
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "最大值",
                Value = 255,
                Minimum = 0,
                Maximum = 255
            });

            AddToBasicParams(CreateSectionHeader("自适应参数"));

            AddToBasicParams(new ParamSlider
            {
                Label = "块大小（奇数）",
                Value = 11,
                Minimum = 3,
                Maximum = 99,
                SmallChange = 2,
                LargeChange = 10
            });

            AddToBasicParams(new ParamSlider
            {
                Label = "常数C",
                Value = 2,
                Minimum = -50,
                Maximum = 50
            });

            // ===== 运行参数Tab =====
            AddToRuntimeParams(CreateSectionHeader("参数绑定配置"));

            AddToRuntimeParams(CreateBindingPanel("阈值 (Threshold)", "从其他节点动态获取值"));
            AddToRuntimeParams(CreateBindingPanel("最大值 (MaxValue)", "从其他节点动态获取值"));

            AddToRuntimeParams(CreateSectionHeader("可用数据源"));
            AddToRuntimeParams(new TextBlock
            {
                Text = "请在工作流中配置前驱节点",
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666666")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 12)
            });

            // ===== 结果展示Tab =====
            AddToResult(CreateSectionHeader("执行结果"));
            AddToResult(new TextBlock
            {
                Text = "执行后显示结果信息",
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888")),
                Margin = new Thickness(0, 4, 0, 12)
            });
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333")),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }

        private Border CreateBindingPanel(string title, string description)
        {
            return new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8F8F8")),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 12,
                            FontWeight = FontWeights.Medium,
                            Margin = new Thickness(0, 0, 0, 6)
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
                                new ComboBox
                                {
                                    Width = 100,
                                    Padding = new Thickness(6),
                                    FontSize = 11,
                                    Margin = new Thickness(0, 0, 8, 0)
                                }.SetGrid(0, 0),
                                new TextBlock
                                {
                                    Text = "选择数据源...",
                                    FontSize = 11,
                                    VerticalAlignment = VerticalAlignment.Center
                                }.SetGrid(0, 1)
                            }
                        }
                    }
                }
            };
        }

        protected override void OnExecuteRequested()
        {
            _viewModel?.RunTool();
            base.OnExecuteRequested();
        }

        protected override void OnResetRequested()
        {
            // 重置参数
            if (_viewModel != null)
            {
                _viewModel.Threshold = 127;
                _viewModel.MaxValue = 255;
                _viewModel.ThresholdType = "Otsu";
            }
            base.OnResetRequested();
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
