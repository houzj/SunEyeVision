using System.Collections.ObjectModel;
using SunEyeVision.Plugin.SDK;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls;
using SunEyeVision.Plugin.SDK.ViewModels;

namespace SunEyeVision.Tool.ColorConvert
{
    /// <summary>
    /// 颜色空间转换工具 ViewModel
    /// </summary>
    public class ColorConvertToolViewModel : ToolViewModelBase
    {
        private string _targetColorSpace = "GRAY";
        private string _sourceColorSpace = "BGR";
        private int _channels = 0;

        #region 图像源选择

        private ImageSourceInfo? _selectedImageSource;

        /// <summary>
        /// 当前选中的图像源
        /// </summary>
        public ImageSourceInfo? SelectedImageSource
        {
            get => _selectedImageSource;
            set => SetProperty(ref _selectedImageSource, value);
        }

        /// <summary>
        /// 可用图像源列表（由工作流上下文提供）
        /// </summary>
        public ObservableCollection<ImageSourceInfo> AvailableImageSources { get; }
            = new ObservableCollection<ImageSourceInfo>();

        #endregion

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
        /// 获取当前运行参数
        /// </summary>
        protected override ToolParameters GetRunParameters()
        {
            return new ColorConvertParameters
            {
                TargetColorSpace = this.TargetColorSpace
            };
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
