using System;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Tool.Blob.Views
{
    /// <summary>
    /// Blob工具调试窗口
    /// </summary>
    public partial class BlobToolDebugWindow : System.Windows.Window
    {
        private BlobToolDebugWindowViewModel _viewModel;

        public BlobToolDebugWindow()
        {
            InitializeComponent();
            _viewModel = new BlobToolDebugWindowViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// 初始化调试窗口
        /// </summary>
        public void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata toolMetadata)
        {
            _viewModel.Parameters = toolPlugin?.GetDefaultParameters() as BlobParameters;
            _viewModel.UpdateBlobResults();
            
            this.Title = $"Blob工具 - {toolMetadata.DisplayName}";
        }
    }
}
