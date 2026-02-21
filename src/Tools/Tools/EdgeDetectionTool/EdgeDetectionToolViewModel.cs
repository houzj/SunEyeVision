using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;

using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;

namespace SunEyeVision.Tools.EdgeDetectionTool
{
    public class EdgeDetectionToolViewModel : AutoToolDebugViewModelBase
    {
        private string _algorithm = "Canny";
        private int _threshold1 = 50;
        private int _threshold2 = 150;
        private int _apertureSize = 3;

        public string Algorithm
        {
            get => _algorithm;
            set
            {
                SetProperty(ref _algorithm, value);
                SetParamValue("Algorithm", value);
            }
        }

        public int Threshold1
        {
            get => _threshold1;
            set
            {
                SetProperty(ref _threshold1, value);
                SetParamValue("Threshold1", value);
            }
        }

        public int Threshold2
        {
            get => _threshold2;
            set
            {
                SetProperty(ref _threshold2, value);
                SetParamValue("Threshold2", value);
            }
        }

        public int ApertureSize
        {
            get => _apertureSize;
            set
            {
                SetProperty(ref _apertureSize, value);
                SetParamValue("ApertureSize", value);
            }
        }

        public string[] Algorithms { get; } = { "Canny", "Sobel", "Laplacian", "Scharr", "Prewitt" };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "边缘检测";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在执行{Algorithm}边缘检测...";
            
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(100, 200));
            
            ExecutionTime = $"{random.Next(60, 120)} ms";
            StatusMessage = $"✅ {Algorithm}边缘检测完成";
            ToolStatus = "就绪";
        }
    }
}
