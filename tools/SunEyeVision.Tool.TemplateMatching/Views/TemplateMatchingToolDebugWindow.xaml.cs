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

namespace SunEyeVision.Tool.TemplateMatching.Views
{
    /// <summary>
    /// 模板匹配工具调试窗口 - 直接绑定参数架构
    /// </summary>
    public partial class TemplateMatchingToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        private ImageSourceSelector? _imageSourceSelector;
        private ParamComboBox? _methodCombo;
        private BindableParameter? _thresholdParam;
        private BindableParameter? _maxMatchesParam;
        private System.Windows.Controls.CheckBox? _multiScaleCheckBox;

        private TemplateMatchingParameters _parameters = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 图像源管理

        public ImageSourceInfo? SelectedImageSource { get; set; }
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 构造函数

        public TemplateMatchingToolDebugWindow()
        {
            InitializeComponent();
            _parameters = new TemplateMatchingParameters();
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public TemplateMatchingToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "模板匹配定位";

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

            // 匹配方法
            if (_methodCombo != null)
            {
                _methodCombo.ItemsSource = new[] { "TM_CCOEFF_NORMED", "TM_CCOEFF", "TM_SQDIFF", "TM_SQDIFF_NORMED", "TM_CCORR", "TM_CCORR_NORMED" };
                _methodCombo.SelectedItem = "TM_CCOEFF_NORMED";
            }

            // 匹配阈值 - 直接绑定
            if (_thresholdParam != null)
            {
                var binding = new Binding("Threshold")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _thresholdParam.SetBinding(BindableParameter.DoubleValueProperty, binding);
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
            if (parameters is TemplateMatchingParameters matchParams)
            {
                _parameters = matchParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: Threshold={matchParams.Threshold}", "TemplateMatchingTool");
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
                PluginLogger.Warning("请选择输入图像源", "TemplateMatchingTool");
                return;
            }

            if (_dataProvider == null || !_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行", "TemplateMatchingTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据", "TemplateMatchingTool");
                return;
            }

            ExecuteTool(imageMat);
        }

        private void ExecuteTool(Mat inputImage)
        {
            if (Tool is not TemplateMatchingTool matchTool)
            {
                PluginLogger.Error("工具实例无效", "TemplateMatchingTool");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var runParams = (TemplateMatchingParameters)_parameters.Clone();
                TemplateMatchingResults result = matchTool.Run(inputImage, runParams);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        PluginLogger.Success($"匹配成功 - 分数: {result.Score:F2}", "TemplateMatchingTool");
                    }
                    else
                    {
                        PluginLogger.Error($"匹配失败 - {result.ErrorMessage}", "TemplateMatchingTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PluginLogger.Error($"执行异常: {ex.Message}", "TemplateMatchingTool");
            }
        }

        protected override void OnResetRequested()
        {
            _parameters = new TemplateMatchingParameters();
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
