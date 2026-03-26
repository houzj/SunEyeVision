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

namespace SunEyeVision.Tool.GaussianBlur.Views
{
    /// <summary>
    /// 高斯模糊工具调试窗口 - 直接绑定参数架构
    /// </summary>
    public partial class GaussianBlurToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        private ImageSourceSelector? _imageSourceSelector;
        private ParamComboBox? _kernelSizeCombo;
        private BindableParameter? _sigmaParam;
        private ParamComboBox? _borderTypeCombo;

        private GaussianBlurParameters _parameters = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 图像源管理

        public ImageSourceInfo? SelectedImageSource { get; set; }
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 构造函数

        public GaussianBlurToolDebugWindow()
        {
            InitializeComponent();
            _parameters = new GaussianBlurParameters();
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public GaussianBlurToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "高斯模糊";

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

            // 核大小
            if (_kernelSizeCombo != null)
            {
                _kernelSizeCombo.ItemsSource = new[] { "3", "5", "7", "9", "11", "13", "15", "17", "19", "21" };
                var binding = new Binding("KernelSize")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay,
                    Converter = new IntToStringConverter()
                };
                _kernelSizeCombo.SetBinding(ParamComboBox.SelectedItemProperty, binding);
            }

            // Sigma - 直接绑定
            if (_sigmaParam != null)
            {
                var sigmaBinding = new Binding("Sigma")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _sigmaParam.SetBinding(BindableParameter.DoubleValueProperty, sigmaBinding);
            }

            // 边界类型
            if (_borderTypeCombo != null)
            {
                _borderTypeCombo.ItemsSource = new[] { "Reflect", "Constant", "Replicate", "Default" };
                _borderTypeCombo.SelectedItem = "Reflect";
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
            if (parameters is GaussianBlurParameters blurParams)
            {
                _parameters = blurParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: KernelSize={blurParams.KernelSize}, Sigma={blurParams.Sigma}", "GaussianBlurTool");
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
                PluginLogger.Warning("请选择输入图像源", "GaussianBlurTool");
                return;
            }

            if (_dataProvider == null || !_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行", "GaussianBlurTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据", "GaussianBlurTool");
                return;
            }

            ExecuteTool(imageMat);
        }

        private void ExecuteTool(Mat inputImage)
        {
            if (Tool is not GaussianBlurTool blurTool)
            {
                PluginLogger.Error("工具实例无效", "GaussianBlurTool");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var runParams = (GaussianBlurParameters)_parameters.Clone();
                GaussianBlurResults result = blurTool.Run(inputImage, runParams);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        PluginLogger.Success($"执行成功 - 耗时: {stopwatch.ElapsedMilliseconds}ms", "GaussianBlurTool");
                    }
                    else
                    {
                        PluginLogger.Error($"执行失败 - {result.ErrorMessage}", "GaussianBlurTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PluginLogger.Error($"执行异常: {ex.Message}", "GaussianBlurTool");
            }
        }

        protected override void OnResetRequested()
        {
            _parameters = new GaussianBlurParameters();
            SetupBindingsAndEvents();
            base.OnResetRequested();
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
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.ToString() ?? "5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string str && int.TryParse(str, out int result))
                return result;
            return 5;
        }
    }
}
