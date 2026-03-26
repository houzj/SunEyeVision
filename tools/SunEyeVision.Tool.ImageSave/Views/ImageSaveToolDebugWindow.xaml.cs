using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Tool.ImageSave.Views
{
    /// <summary>
    /// 图像保存工具调试窗口 - 直接绑定参数架构
    /// </summary>
    public partial class ImageSaveToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        private ImageSourceSelector? _imageSourceSelector;
        private ParamTextBox? _filePathTextBox;
        private ParamComboBox? _imageFormatCombo;
        private BindableParameter? _qualityParam;
        private System.Windows.Controls.CheckBox? _overwriteCheckBox;

        private ImageSaveParameters _parameters = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 图像源管理

        public ImageSourceInfo? SelectedImageSource { get; set; }
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 构造函数

        public ImageSaveToolDebugWindow()
        {
            InitializeComponent();
            _parameters = new ImageSaveParameters();
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ImageSaveToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "图像保存";

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

            // 文件路径
            if (_filePathTextBox != null)
            {
                var binding = new Binding("OutputPath")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _filePathTextBox.SetBinding(ParamTextBox.TextProperty, binding);
            }

            // 输出格式
            if (_imageFormatCombo != null)
            {
                _imageFormatCombo.ItemsSource = new[] { "PNG", "JPEG", "BMP", "TIFF" };
                var binding = new Binding("OutputFormat")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay,
                    Converter = new FormatConverter()
                };
                _imageFormatCombo.SetBinding(ParamComboBox.SelectedItemProperty, binding);
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
            if (parameters is ImageSaveParameters saveParams)
            {
                _parameters = saveParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: OutputPath={saveParams.OutputPath}", "ImageSaveTool");
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
                PluginLogger.Warning("请选择输入图像源", "ImageSaveTool");
                return;
            }

            if (_dataProvider == null || !_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行", "ImageSaveTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据", "ImageSaveTool");
                return;
            }

            ExecuteTool(imageMat);
        }

        private void ExecuteTool(Mat inputImage)
        {
            if (Tool is not ImageSaveTool saveTool)
            {
                PluginLogger.Error("工具实例无效", "ImageSaveTool");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var runParams = (ImageSaveParameters)_parameters.Clone();
                ImageSaveResults result = saveTool.Run(inputImage, runParams);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        PluginLogger.Success($"保存成功 - {result.SavedPath}", "ImageSaveTool");
                    }
                    else
                    {
                        PluginLogger.Error($"保存失败 - {result.ErrorMessage}", "ImageSaveTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PluginLogger.Error($"执行异常: {ex.Message}", "ImageSaveTool");
            }
        }

        protected override void OnResetRequested()
        {
            _parameters = new ImageSaveParameters();
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
    /// 格式转换器
    /// </summary>
    public class FormatConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var format = value?.ToString()?.ToUpper() ?? "PNG";
            return format;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.ToString()?.ToLower() ?? "png";
        }
    }
}
