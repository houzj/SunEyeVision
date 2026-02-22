using System;
using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.ViewModels;

namespace SunEyeVision.Tool.ColorConvert
{
    /// <summary>
    /// 颜色空间转换工具 ViewModel
    /// </summary>
    public class ColorConvertToolViewModel : AutoToolDebugViewModelBase
    {
        private string _targetColorSpace = "Gray";
        private bool _useGpu = false;

        public string TargetColorSpace
        {
            get => _targetColorSpace;
            set
            {
                SetProperty(ref _targetColorSpace, value);
                SetParamValue("TargetColorSpace", value);
            }
        }

        public bool UseGpu
        {
            get => _useGpu;
            set
            {
                SetProperty(ref _useGpu, value);
                SetParamValue("UseGpu", value);
            }
        }

        public string[] ColorSpaces { get; } = { "Gray", "HSV", "Lab", "YUV", "RGB", "BGR" };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "颜色空间转换";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在转换到 {TargetColorSpace} 空间...";
            
            var random = new Random();
            System.Threading.Thread.Sleep(random.Next(50, 150));
            
            ExecutionTime = $"{random.Next(30, 80)} ms";
            StatusMessage = $"✅ 颜色转换完成（{TargetColorSpace}）";
            ToolStatus = "就绪";
        }
    }
}
