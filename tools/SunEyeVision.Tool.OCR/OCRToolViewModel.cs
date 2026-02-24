using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.ViewModels;
namespace SunEyeVision.Tool.OCR
{
    public class OCRToolViewModel : AutoToolDebugViewModelBase
    {
        private string _language = "chi_sim+eng";
        private string _dataPath = "";
        private string _charWhitelist = "";
        private int _psm = 3;

        public string Language
        {
            get => _language;
            set
            {
                SetProperty(ref _language, value);
                SetParamValue("Language", value);
            }
        }

        public string DataPath
        {
            get => _dataPath;
            set
            {
                SetProperty(ref _dataPath, value);
                SetParamValue("DataPath", value);
            }
        }

        public string CharWhitelist
        {
            get => _charWhitelist;
            set
            {
                SetProperty(ref _charWhitelist, value);
                SetParamValue("CharWhitelist", value);
            }
        }

        public int Psm
        {
            get => _psm;
            set
            {
                SetProperty(ref _psm, value);
                SetParamValue("Psm", value);
            }
        }

        public string[] Languages { get; } = { 
            "chi_sim", "chi_tra", "eng", "chi_sim+eng", 
            "jpn", "kor", "fra", "deu", "spa" 
        };

        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "OCR识别";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";
            LoadParameters(toolMetadata);
        }

        public override void RunTool()
        {
            ToolStatus = "运行�?;
            StatusMessage = $"正在执行OCR识别（语言: {Language}�?..";
            
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(300, 600));
            
            ExecutionTime = $"{random.Next(200, 400)} ms";
            StatusMessage = $"OCR识别完成";
            ToolStatus = "就绪";
        }
    }
}
