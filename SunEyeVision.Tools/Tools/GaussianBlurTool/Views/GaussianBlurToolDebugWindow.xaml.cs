using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Base.Models;

using System.Windows;
using System;
using SunEyeVision.PluginSystem.Base.Base;
using SunEyeVision.PluginSystem.Infrastructure.Base;
using SunEyeVision.PluginSystem.Infrastructure.UI.Windows;


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
