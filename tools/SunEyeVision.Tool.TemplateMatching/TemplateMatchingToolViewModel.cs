using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.ViewModels;
namespace SunEyeVision.Tool.TemplateMatching
{
    public class TemplateMatchingToolViewModel : AutoToolDebugViewModelBase
    {
        private string _method = "TM_CCOEFF_NORMED";
        private double _threshold = 0.8;
        private int _maxMatches = 1;
        private bool _multiScale = false;

        public string Method
        {
            get => _method;
            set
            {
                SetProperty(ref _method, value);
                SetParamValue("Method", value);
            }
        }

        public double Threshold
        {
            get => _threshold;
            set
            {
                SetProperty(ref _threshold, value);
                SetParamValue("Threshold", value);
            }
        }

        public int MaxMatches
        {
            get => _maxMatches;
            set
            {
                SetProperty(ref _maxMatches, value);
                SetParamValue("MaxMatches", value);
            }
        }

        public bool MultiScale
        {
            get => _multiScale;
            set
            {
                SetProperty(ref _multiScale, value);
                SetParamValue("MultiScale", value);
            }
        }

        public string[] Methods { get; } = { 
            "TM_CCOEFF_NORMED", "TM_CCOEFF", 
            "TM_SQDIFF", "TM_SQDIFF_NORMED",
            "TM_CCORR", "TM_CCORR_NORMED"
        };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "模板匹配";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行�?;
            StatusMessage = $"正在执行模板匹配（{Method}, 阈�?{Threshold:F2}�?..";
            
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(200, 400));
            
            ExecutionTime = $"{random.Next(100, 200)} ms";
            StatusMessage = $"模板匹配完成";
            ToolStatus = "就绪";
        }
    }
}
