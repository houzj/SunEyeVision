using System.Collections.Generic;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.ViewModels;
using SunEyeVision.Plugin.SDK.Core;

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
        /// 构建参数字典（供基类 Execute 使用�?
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
                DebugMessage = $"转换完成: {SourceColorSpace} �?{TargetColorSpace}";
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
    }
}
