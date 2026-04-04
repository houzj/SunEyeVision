using System;
using System.Diagnostics;
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

namespace SunEyeVision.Tool.ImageCapture.Views
{
    /// <summary>
    /// 图像采集工具调试窗口 - 直接绑定参数架构
    /// </summary>
    public partial class ImageCaptureToolDebugWindow : BaseToolDebugWindow
    {
        #region 字段

        private ParamComboBox? _deviceIdCombo;
        private BindableParameter? _widthParam;
        private BindableParameter? _heightParam;
        private BindableParameter? _exposureParam;
        private BindableParameter? _gainParam;

        private ImageCaptureParameters _parameters = null!;
        private WorkflowDataSourceProvider _dataProvider = null!;

        #endregion

        #region 构造函数

        public ImageCaptureToolDebugWindow()
        {
            InitializeComponent();
            _parameters = new ImageCaptureParameters();
            ResolveNamedControls();
            SetupBindingsAndEvents();
        }

        public ImageCaptureToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
            : this()
        {
            Tool = toolPlugin;
            NodeName = toolMetadata?.DisplayName ?? "图像采集";
        }

        #endregion

        #region 参数绑定

        private void SetupBindingsAndEvents()
        {
            // 设备ID ComboBox
            if (_deviceIdCombo != null)
            {
                _deviceIdCombo.ItemsSource = new[] { "0", "1", "2", "3" };
                _deviceIdCombo.SelectedItem = "0";
            }

            // 宽度 - 直接绑定
            if (_widthParam != null)
            {
                // ImageCaptureParameters 没有 Width 属性，这里只是显示
            }

            // 高度 - 直接绑定
            if (_heightParam != null)
            {
                // ImageCaptureParameters 没有 Height 属性
            }
        }

        #endregion

        #region 数据提供者设置

        public void SetDataProvider(WorkflowDataSourceProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public void SetCurrentNode(object node)
        {
            if (node == null) return;

            var parametersProperty = node.GetType().GetProperty("Parameters");
            if (parametersProperty == null) return;

            var parameters = parametersProperty.GetValue(node) as ToolParameters;
            if (parameters is ImageCaptureParameters captureParams)
            {
                _parameters = captureParams;
                SetupBindingsAndEvents();
                PluginLogger.Success($"参数引用已设置: CameraId={captureParams.CameraId}", "ImageCaptureTool");
            }
        }

        #endregion

        #region 执行控制

        protected override void OnExecuteRequested()
        {
            ExecuteTool();
        }

        private void ExecuteTool()
        {
            if (Tool is not ImageCaptureTool captureTool)
            {
                PluginLogger.Error("工具实例无效", "ImageCaptureTool");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var runParams = (ImageCaptureParameters)_parameters.Clone();
                ImageCaptureResults result = captureTool.Run(null, runParams);

                stopwatch.Stop();

                Dispatcher.Invoke(() =>
                {
                    if (result.IsSuccess)
                    {
                        PluginLogger.Success($"采集成功 - 耗时: {stopwatch.ElapsedMilliseconds}ms", "ImageCaptureTool");
                    }
                    else
                    {
                        PluginLogger.Error($"采集失败 - {result.ErrorMessage}", "ImageCaptureTool");
                    }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                PluginLogger.Error($"执行异常: {ex.Message}", "ImageCaptureTool");
            }
        }

        #endregion
    }
}
