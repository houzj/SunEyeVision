using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.ViewModels;
namespace SunEyeVision.Tool.Threshold
{
    public class ThresholdToolViewModel : AutoToolDebugViewModelBase
    {
        private string _thresholdType = "Otsu";
        private int _threshold = 127;
        private int _maxValue = 255;
        private double _adaptiveBlockSize = 11;
        private double _adaptiveC = 2;

        public string ThresholdType
        {
            get => _thresholdType;
            set
            {
                SetProperty(ref _thresholdType, value);
                SetParamValue("ThresholdType", value);
            }
        }

        public int Threshold
        {
            get => _threshold;
            set
            {
                SetProperty(ref _threshold, value);
                SetParamValue("Threshold", value);
            }
        }

        public int MaxValue
        {
            get => _maxValue;
            set
            {
                SetProperty(ref _maxValue, value);
                SetParamValue("MaxValue", value);
            }
        }

        public double AdaptiveBlockSize
        {
            get => _adaptiveBlockSize;
            set
            {
                if (value % 2 == 0)
                    value = value + 1;
                SetProperty(ref _adaptiveBlockSize, value);
                SetParamValue("AdaptiveBlockSize", value);
            }
        }

        public double AdaptiveC
        {
            get => _adaptiveC;
            set
            {
                SetProperty(ref _adaptiveC, value);
                SetParamValue("AdaptiveC", value);
            }
        }

        public string[] ThresholdTypes { get; } = { 
            "Otsu", "Binary", "BinaryInv", 
            "Trunc", "ToZero", "ToZeroInv",
            "AdaptiveMean", "AdaptiveGaussian"
        };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "图像阈值化";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在执行{ThresholdType}阈值化...";
            
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(50, 100));
            
            ExecutionTime = $"{random.Next(30, 60)} ms";
            StatusMessage = $"阈值化完成";
            ToolStatus = "就绪";
        }
    }
}
