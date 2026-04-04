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
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

namespace SunEyeVision.Tool.ColorConvert.Views
{
    /// <summary>
    /// 颜色空间转换工具调试控件
    /// </summary>
    public partial class ColorConvertToolDebugControl : UserControl
    {
        #region 字段

        // XAML控件引用
        private ImageSourceSelector? _imageSourceSelector;
        private ParamComboBox? _sourceColorSpaceCombo;
        private ParamComboBox? _targetColorSpaceCombo;

        // 参数引用（零拷贝）
        private ColorConvertParameters _parameters = null!;

        // 数据提供者
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 图像源管理

        public ImageSourceInfo? SelectedImageSource { get; set; }
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; } = new();

        #endregion

        #region 事件

        public event EventHandler? ExecuteRequested;
        public event EventHandler? ConfirmClicked;

        #endregion

        #region 构造函数

        public ColorConvertToolDebugControl()
        {
            InitializeComponent();
            _parameters = new ColorConvertParameters();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        private void ResolveNamedControls()
        {
            _imageSourceSelector = this.FindName("ImageSourceSelector") as ImageSourceSelector;
            _sourceColorSpaceCombo = this.FindName("SourceColorSpaceCombo") as ParamComboBox;
            _targetColorSpaceCombo = this.FindName("TargetColorSpaceCombo") as ParamComboBox;
        }

        #endregion

        #region 参数绑定

        private void SetupBindingsAndEvents()
        {
            // 图像源选择
            if (_imageSourceSelector != null)
            {
                _imageSourceSelector.ImageSourceChanged += OnImageSourceChanged;
            }

            // 源颜色空间
            if (_sourceColorSpaceCombo != null)
            {
                _sourceColorSpaceCombo.ItemsSource = new[] { "BGR", "RGB", "GRAY", "HSV", "Lab" };
                _sourceColorSpaceCombo.SelectedItem = "BGR";
            }

            // 目标颜色空间 - 直接绑定到参数
            if (_targetColorSpaceCombo != null)
            {
                _targetColorSpaceCombo.ItemsSource = new[] { "GRAY", "RGB", "HSV", "Lab", "XYZ", "YCrCb" };
                var binding = new Binding("TargetColorSpace")
                {
                    Source = _parameters,
                    Mode = BindingMode.TwoWay
                };
                _targetColorSpaceCombo.SetBinding(ParamComboBox.SelectedItemProperty, binding);
            }
        }

        #endregion

        #region 数据提供者设置

        public void SetDataProvider(WorkflowDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
            PopulateImageSources(dataProvider);
        }

        public void SetParameters(ToolParameters parameters)
        {
            if (parameters is ColorConvertParameters colorParams)
            {
                _parameters = colorParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: TargetColorSpace={colorParams.TargetColorSpace}", "ColorConvertTool");
            }
        }

        public void SetMainImageControl(object imageControl)
        {
            // 颜色转换工具不需要主图像控件
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

        protected virtual void OnExecuteRequested()
        {
            if (SelectedImageSource == null)
            {
                PluginLogger.Warning("请选择输入图像源", "ColorConvertTool");
                return;
            }

            if (_dataProvider == null)
            {
                PluginLogger.Warning("数据提供者未初始化", "ColorConvertTool");
                return;
            }

            if (!_dataProvider.HasNodeOutput(SelectedImageSource.NodeId))
            {
                PluginLogger.Warning("图像源节点尚未执行，请先执行前驱节点", "ColorConvertTool");
                return;
            }

            var imageMat = _dataProvider.GetCurrentBindingValue(
                SelectedImageSource.NodeId,
                "Output",
                SelectedImageSource.OutputPortName
            ) as Mat;

            if (imageMat == null || imageMat.Empty())
            {
                PluginLogger.Warning("无法获取图像数据或图像为空", "ColorConvertTool");
                return;
            }

            ExecuteTool(imageMat);
        }

        private void ExecuteTool(Mat inputImage)
        {
            // 执行工具逻辑需要工具实例
            ExecuteRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnResetRequested()
        {
            _parameters = new ColorConvertParameters();
            SetupBindingsAndEvents();
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnConfirmClicked()
        {
            ConfirmClicked?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 图像源选择事件

        private void OnImageSourceChanged(object sender, RoutedEventArgs e)
        {
            if (_imageSourceSelector != null)
                SelectedImageSource = _imageSourceSelector.SelectedImageSource;
        }

        #endregion
    }
}
