using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Tool.ROICrop.Views
{
    /// <summary>
    /// ROI裁剪工具调试窗口 - 直接绑定参数架构
    /// </summary>
    public partial class ROICropToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        private ImageSourceSelector? _imageSourceSelector;
        private BindableParameter? _xParam;
        private BindableParameter? _yParam;
        private BindableParameter? _widthParam;
        private BindableParameter? _heightParam;
        private System.Windows.Controls.CheckBox? _normalizeCheckBox;

        private ROICropParameters _parameters = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 图像源管理

        public ImageSourceInfo? SelectedImageSource { get; set; }
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 构造函数

        public ROICropToolDebugWindow()
        {
            InitializeComponent();
            _parameters = new ROICropParameters();
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ROICropToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "ROI裁剪";

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

            // X坐标 - 直接绑定
            if (_xParam != null)
            {
                var binding = new Binding("X")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _xParam.SetBinding(BindableParameter.DoubleValueProperty, binding);
            }

            // Y坐标 - 直接绑定
            if (_yParam != null)
            {
                var binding = new Binding("Y")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _yParam.SetBinding(BindableParameter.DoubleValueProperty, binding);
            }

            // 宽度 - 直接绑定
            if (_widthParam != null)
            {
                var binding = new Binding("Width")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _widthParam.SetBinding(BindableParameter.DoubleValueProperty, binding);
            }

            // 高度 - 直接绑定
            if (_heightParam != null)
            {
                var binding = new Binding("Height")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _heightParam.SetBinding(BindableParameter.DoubleValueProperty, binding);
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
            if (parameters is ROICropParameters cropParams)
            {
                _parameters = cropParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: X={cropParams.X}, Y={cropParams.Y}", "ROICropTool");
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
                PluginLogger.Warning("请选择输入图像源", "ROICropTool");
                return;
            }

            if (_dataProvider == null || !_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行", "ROICropTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据", "ROICropTool");
                return;
            }

            ExecuteTool(imageMat);
        }

        private void ExecuteTool(Mat inputImage)
        {
            if (Tool is not ROICropTool cropTool)
            {
                PluginLogger.Error("工具实例无效", "ROICropTool");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var runParams = (ROICropParameters)_parameters.Clone();
                ROICropResults result = cropTool.Run(inputImage, runParams);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        PluginLogger.Success($"裁剪成功 - 区域: {result.CroppedArea}", "ROICropTool");
                    }
                    else
                    {
                        PluginLogger.Error($"裁剪失败 - {result.ErrorMessage}", "ROICropTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PluginLogger.Error($"执行异常: {ex.Message}", "ROICropTool");
            }
        }

        protected override void OnResetRequested()
        {
            _parameters = new ROICropParameters();
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
}
