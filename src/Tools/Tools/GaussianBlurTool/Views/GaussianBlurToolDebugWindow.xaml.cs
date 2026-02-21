using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;

using System.Windows;
using System;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;


namespace SunEyeVision.Tools.GaussianBlurTool.Views
{
    public partial class GaussianBlurToolDebugWindow : Window
    {
        public GaussianBlurToolDebugWindow(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            InitializeComponent();

            // 创建并初始化ViewModel
            var viewModel = new GaussianBlurToolViewModel();
            viewModel.Initialize(toolId, toolPlugin, toolMetadata);
            DataContext = viewModel;
        }
    }
}
