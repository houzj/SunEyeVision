using SunEyeVision.Plugin.SDK.Execution;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Plugin.SDK.Models.Roi;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.ViewModels
{
    /// <summary>
    /// 区域订阅面板的 ViewModel
    /// </summary>
    public class RegionSubscribeViewModel : ObservableObject
    {
        private RegionSubscribeMode _subscribeMode = RegionSubscribeMode.RectRegion; // 默认：按矩形区域

        /// <summary>
        /// 继承方式
        /// </summary>
        public RegionSubscribeMode SubscribeMode
        {
            get => _subscribeMode;
            set
            {
                if (SetProperty(ref _subscribeMode, value))
                {
                    // 通知所有布尔可见性属性变更
                    OnPropertyChanged(nameof(IsRectRegionMode));
                    OnPropertyChanged(nameof(IsRectParameterMode));
                    OnPropertyChanged(nameof(IsCircleRegionMode));
                    OnPropertyChanged(nameof(IsCircleParameterMode));
                }
            }
        }

        #region 模式可见性属性

        /// <summary>
        /// 是否为按矩形区域模式
        /// </summary>
        public bool IsRectRegionMode => SubscribeMode == RegionSubscribeMode.RectRegion;

        /// <summary>
        /// 是否为按矩形参数模式
        /// </summary>
        public bool IsRectParameterMode => SubscribeMode == RegionSubscribeMode.RectParameter;

        /// <summary>
        /// 是否为按圆形区域模式
        /// </summary>
        public bool IsCircleRegionMode => SubscribeMode == RegionSubscribeMode.CircleRegion;

        /// <summary>
        /// 是否为按圆形参数模式
        /// </summary>
        public bool IsCircleParameterMode => SubscribeMode == RegionSubscribeMode.CircleParameter;

        #endregion

        #region 矩形区域参数

        private string? _rectRegionSource;

        /// <summary>
        /// 矩形区域绑定源
        /// </summary>
        public string? RectRegionSource
        {
            get => _rectRegionSource;
            set => SetProperty(ref _rectRegionSource, value);
        }

        #endregion

        #region 矩形参数

        private string? _rectXSource;

        /// <summary>
        /// 矩形X绑定源
        /// </summary>
        public string? RectXSource
        {
            get => _rectXSource;
            set => SetProperty(ref _rectXSource, value);
        }

        private string? _rectYSource;

        /// <summary>
        /// 矩形Y绑定源
        /// </summary>
        public string? RectYSource
        {
            get => _rectYSource;
            set => SetProperty(ref _rectYSource, value);
        }

        private string? _rectWidthSource;

        /// <summary>
        /// 矩形宽度绑定源
        /// </summary>
        public string? RectWidthSource
        {
            get => _rectWidthSource;
            set => SetProperty(ref _rectWidthSource, value);
        }

        private string? _rectHeightSource;

        /// <summary>
        /// 矩形高度绑定源
        /// </summary>
        public string? RectHeightSource
        {
            get => _rectHeightSource;
            set => SetProperty(ref _rectHeightSource, value);
        }

        #endregion

        #region 圆形区域参数

        private string? _circleRegionSource;

        /// <summary>
        /// 圆形区域绑定源
        /// </summary>
        public string? CircleRegionSource
        {
            get => _circleRegionSource;
            set => SetProperty(ref _circleRegionSource, value);
        }

        #endregion

        #region 圆形参数

        private string? _circleCenterXSource;

        /// <summary>
        /// 圆形中心X绑定源
        /// </summary>
        public string? CircleCenterXSource
        {
            get => _circleCenterXSource;
            set => SetProperty(ref _circleCenterXSource, value);
        }

        private string? _circleCenterYSource;

        /// <summary>
        /// 圆形中心Y绑定源
        /// </summary>
        public string? CircleCenterYSource
        {
            get => _circleCenterYSource;
            set => SetProperty(ref _circleCenterYSource, value);
        }

        private string? _circleRadiusSource;

        /// <summary>
        /// 圆形半径绑定源
        /// </summary>
        public string? CircleRadiusSource
        {
            get => _circleRadiusSource;
            set => SetProperty(ref _circleRadiusSource, value);
        }

        #endregion
    }
}
