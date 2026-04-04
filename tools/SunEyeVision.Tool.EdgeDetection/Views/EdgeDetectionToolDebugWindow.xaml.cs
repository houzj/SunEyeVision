using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Tool.EdgeDetection.Views
{
    /// <summary>
    /// 边缘检测工具调试窗口 - 直接绑定参数架构
    /// </summary>
    public partial class EdgeDetectionToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        private ImageSourceSelector? _imageSourceSelector;
        private ParamComboBox? _algorithmCombo;
        private BindableParameter? _threshold1Param;
        private BindableParameter? _threshold2Param;
        private ParamComboBox? _apertureSizeCombo;

        private EdgeDetectionParameters _parameters = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 图像源管理

        public ImageSourceInfo? SelectedImageSource { get; set; }
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 构造函数

        public EdgeDetectionToolDebugWindow()
        {
            InitializeComponent();
            _parameters = new EdgeDetectionParameters();
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public EdgeDetectionToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "边缘检测";

            if (_dataProvider != null)
            {
                PopulateImageSources(_dataProvider);
            }
        }

        #endregion

        #region 参数绑定

        private void SetupBindingsAndEvents()
        {
            if (_imageSourceSelector != null)
            {
                _imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;
            }

            // 算法选择
            if (_algorithmCombo != null)
            {
                _algorithmCombo.ItemsSource = new[] { "Canny", "Sobel", "Laplacian", "Scharr", "Prewitt" };
                _algorithmCombo.SelectedItem = "Canny";
            }

            // 阈值1 - 直接绑定
            if (_threshold1Param != null)
            {
                var binding = new Binding("Threshold1")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _threshold1Param.SetBinding(BindableParameter.DoubleValueProperty, binding);
            }

            // 阈值2 - 直接绑定
            if (_threshold2Param != null)
            {
                var binding = new Binding("Threshold2")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _threshold2Param.SetBinding(BindableParameter.DoubleValueProperty, binding);
            }

            // 孔径大小
            if (_apertureSizeCombo != null)
            {
                _apertureSizeCombo.ItemsSource = new[] { "3", "5", "7" };
                var binding = new Binding("ApertureSize")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay,
                    Converter = new IntToStringConverter()
                };
                _apertureSizeCombo.SetBinding(ParamComboBox.SelectedItemProperty, binding);
            }
        }

        #endregion

        #region 数据提供者设置

        public void SetDataProvider(WorkflowDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
            PopulateImageSources(dataProvider);
        }

        public void SetCurrentNode(object node)
        {
            if (node == null) return;

            var parametersProperty = node.GetType().GetProperty("Parameters");
            if (parametersProperty == null) return;

            var parameters = parametersProperty.GetValue(node) as ToolParameters;
            if (parameters is EdgeDetectionParameters edgeParams)
            {
                _parameters = edgeParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: Threshold1={edgeParams.Threshold1}", "EdgeDetectionTool");
            }
        }

        private void PopulateImageSources(WorkflowDataSourceProvider dataProvider)
        {
            AvailableImageSources.Clear();
            if (dataProvider == null) return;

            var nodeOutputs = dataProvider.GetParentNodeOutputs("Mat");
            foreach (var nodeOutput in nodeOutputs)
            {
                if (nodeOutput.DataType == "Mat" ||
                    nodeOutput.Children.Any(c => c.DataType == "Mat"))
                {
                    var imageSource = new ImageSourceInfo
                    {
                        NodeId = nodeOutput.NodeId,
                        NodeName = nodeOutput.NodeName,
                        OutputPortName = "Output"
                    };

                    if (nodeOutput.DataType != "Mat" && nodeOutput.Children.Any())
                    {
                        var matChild = nodeOutput.Children.FirstOrDefault(c =>
                            c.PropertyPath == "OutputImage" && c.DataType == "Mat");
                        if (matChild != null)
                        {
                            imageSource.OutputPortName = matChild.PropertyPath;
                        }
                    }

                    AvailableImageSources.Add(imageSource);
                }
            }
        }

        #endregion

        #region 执行控制

        protected override void OnExecuteRequested()
        {
            if (SelectedImageSource == null)
            {
                PluginLogger.Warning("请选择输入图像源", "EdgeDetectionTool");
                return;
            }

            if (_dataProvider == null || !_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行", "EdgeDetectionTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据", "EdgeDetectionTool");
                return;
            }

            ExecuteTool(imageMat);
        }

        private void ExecuteTool(Mat inputImage)
        {
            if (Tool is not EdgeDetectionTool edgeTool)
            {
                PluginLogger.Error("工具实例无效", "EdgeDetectionTool");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var runParams = (EdgeDetectionParameters)_parameters.Clone();
                EdgeDetectionResults result = edgeTool.Run(inputImage, runParams);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        PluginLogger.Success($"执行成功 - 耗时: {stopwatch.ElapsedMilliseconds}ms", "EdgeDetectionTool");
                    }
                    else
                    {
                        PluginLogger.Error($"执行失败 - {result.ErrorMessage}", "EdgeDetectionTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PluginLogger.Error($"执行异常: {ex.Message}", "EdgeDetectionTool");
            }
        }

        #endregion

        #region 事件处理

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (_imageSourceSelector != null)
                SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        #endregion
    }

    /// <summary>
    /// int <-> string 转换器
    /// </summary>
    public class IntToStringConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.ToString() ?? "3";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string str && int.TryParse(str, out int result))
                return result;
            return 3;
        }
    }
}
