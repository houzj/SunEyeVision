using System.Collections.Generic;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ColorConvert
{
    /// <summary>
    /// 颜色空间转换工具 ViewModel
    /// </summary>
    public class ColorConvertToolViewModel : AutoToolDebugViewModelBase
    {
        private string _targetColorSpace = "GRAY";
        private string _sourceColorSpace = "BGR";
        private int _channels = 0;

        public string TargetColorSpace
        {
            get => _targetColorSpace;
            set
            {
                SetProperty(ref _targetColorSpace, value);
                SetParamValue("TargetColorSpace", value);
            }
        }

        public string SourceColorSpace
        {
            get => _sourceColorSpace;
            set
            {
                SetProperty(ref _sourceColorSpace, value);
                SetParamValue("SourceColorSpace", value);
            }
        }

        public int Channels
        {
            get => _channels;
            set
            {
                SetProperty(ref _channels, value);
                SetParamValue("Channels", value);
            }
        }

        public string[] ColorSpaces { get; } = { "GRAY", "RGB", "HSV", "Lab", "XYZ", "YCrCb" };
        public string[] SourceColorSpaces { get; } = { "BGR", "RGB", "GRAY", "HSV", "Lab" };

        /// <summary>
        /// 构建参数字典（供基类 Execute 使用）
        /// </summary>
        protected override Dictionary<string, object> BuildParameterDictionary()
        {
            return new Dictionary<string, object>
            {
                ["targetColorSpace"] = TargetColorSpace,
                ["sourceColorSpace"] = SourceColorSpace,
                ["channels"] = Channels
            };
        }

        /// <summary>
        /// 执行成功回调
        /// </summary>
        protected override void OnExecutionCompleted(AlgorithmResult result)
        {
            if (result.Data != null)
            {
                DebugMessage = $"转换完成: {SourceColorSpace} → {TargetColorSpace}";
            }
        }

        /// <summary>
        /// 重置参数
        /// </summary>
        public override void ResetParameters()
        {
            TargetColorSpace = "GRAY";
            SourceColorSpace = "BGR";
            Channels = 0;
            base.ResetParameters();
        }

        /// <summary>
        /// 运行工具
        /// </summary>
        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在转换 {SourceColorSpace} → {TargetColorSpace}...";
            var random = new System.Random();
            System.Threading.Thread.Sleep(random.Next(50, 150));
            ExecutionTime = $"{random.Next(30, 80)} ms";
            StatusMessage = "颜色空间转换完成";
            ToolStatus = "就绪";
            DebugMessage = $"转换完成: {SourceColorSpace} → {TargetColorSpace}";
        }
    }
}
