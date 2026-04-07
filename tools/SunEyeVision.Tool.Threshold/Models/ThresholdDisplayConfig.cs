using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.Tool.Threshold.Models
{
    /// <summary>
    /// 显示项配置
    /// </summary>
    public class DisplayItemConfig : ObservableObject
    {
        private string _name = "";
        /// <summary>
        /// 显示项名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, "名称");
        }

        private bool _isVisible = true;
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value, "可见性");
        }

        private Color _okColor = Color.FromRgb(0, 255, 0);
        /// <summary>
        /// OK状态颜色
        /// </summary>
        public Color OkColor
        {
            get => _okColor;
            set => SetProperty(ref _okColor, value, "OK颜色");
        }

        private Color _ngColor = Color.FromRgb(255, 0, 0);
        /// <summary>
        /// NG状态颜色
        /// </summary>
        public Color NgColor
        {
            get => _ngColor;
            set => SetProperty(ref _ngColor, value, "NG颜色");
        }

        private double _opacity = 1.0;
        /// <summary>
        /// 透明度(0-1)
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value, "透明度");
        }

        private double _thickness = 1.0;
        /// <summary>
        /// 线条粗细
        /// </summary>
        public double Thickness
        {
            get => _thickness;
            set => SetProperty(ref _thickness, value, "线条粗细");
        }

        /// <summary>
        /// 深拷贝配置
        /// </summary>
        public DisplayItemConfig Clone()
        {
            return new DisplayItemConfig
            {
                Name = _name,
                IsVisible = _isVisible,
                OkColor = _okColor,
                NgColor = _ngColor,
                Opacity = _opacity,
                Thickness = _thickness
            };
        }
    }

    /// <summary>
    /// 阈值工具图像显示配置
    /// </summary>
    /// <remarks>
    /// 配置输出图像、阈值分界线、ROI区域、直方图等显示项的样式和行为。
    /// </remarks>
    public class ThresholdDisplayConfig : ObservableObject
    {
        private DisplayItemConfig _outputImage = new() { Name = "输出图像" };
        /// <summary>
        /// 输出图像显示配置
        /// </summary>
        public DisplayItemConfig OutputImage
        {
            get => _outputImage;
            set => SetProperty(ref _outputImage, value, "输出图像配置");
        }

        private DisplayItemConfig _thresholdLine = new() { Name = "阈值分界线" };
        /// <summary>
        /// 阈值分界线显示配置
        /// </summary>
        public DisplayItemConfig ThresholdLine
        {
            get => _thresholdLine;
            set => SetProperty(ref _thresholdLine, value, "阈值分界线配置");
        }

        private DisplayItemConfig _region = new() { Name = "ROI区域" };
        /// <summary>
        /// ROI区域显示配置
        /// </summary>
        public DisplayItemConfig Region
        {
            get => _region;
            set => SetProperty(ref _region, value, "ROI区域配置");
        }

        private DisplayItemConfig _histogram = new() { Name = "直方图" };
        /// <summary>
        /// 直方图显示配置
        /// </summary>
        public DisplayItemConfig Histogram
        {
            get => _histogram;
            set => SetProperty(ref _histogram, value, "直方图配置");
        }

        /// <summary>
        /// 深拷贝配置
        /// </summary>
        public ThresholdDisplayConfig Clone()
        {
            return new ThresholdDisplayConfig
            {
                OutputImage = _outputImage.Clone(),
                ThresholdLine = _thresholdLine.Clone(),
                Region = _region.Clone(),
                Histogram = _histogram.Clone()
            };
        }
    }

    /// <summary>
    /// 阈值工具文本显示配置
    /// </summary>
    public class ThresholdTextConfig : ObservableObject
    {
        /// <summary>
        /// 构造函数 - 初始化默认颜色值
        /// </summary>
        public ThresholdTextConfig()
        {
            PluginLogger.Info($"ThresholdTextConfig 初始化: OkColor={_okColor}, NgColor={_ngColor}", "ThresholdTool");
        }

        private string _content = "结果: {Result}";
        /// <summary>
        /// 文本内容（支持变量占位符）
        /// </summary>
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value, "文本内容");
        }

        private Color _okColor = Color.FromRgb(0, 255, 0);
        /// <summary>
        /// OK状态文本颜色
        /// </summary>
        public Color OkColor
        {
            get => _okColor;
            set => SetProperty(ref _okColor, value, "OK颜色");
        }

        private Color _ngColor = Color.FromRgb(255, 0, 0);
        /// <summary>
        /// NG状态文本颜色
        /// </summary>
        public Color NgColor
        {
            get => _ngColor;
            set => SetProperty(ref _ngColor, value, "NG颜色");
        }

        private double _fontSize = 14;
        /// <summary>
        /// 字号
        /// </summary>
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value, "字号");
        }

        private double _opacity = 1.0;
        /// <summary>
        /// 透明度(0-1)
        /// </summary>
        public double Opacity
        {
            get => _opacity;
            set => SetProperty(ref _opacity, value, "透明度");
        }

        private double _positionX = 10;
        /// <summary>
        /// X坐标位置
        /// </summary>
        public double PositionX
        {
            get => _positionX;
            set => SetProperty(ref _positionX, value, "X坐标");
        }

        private double _positionY = 10;
        /// <summary>
        /// Y坐标位置
        /// </summary>
        public double PositionY
        {
            get => _positionY;
            set => SetProperty(ref _positionY, value, "Y坐标");
        }

        /// <summary>
        /// 深拷贝配置
        /// </summary>
        public ThresholdTextConfig Clone()
        {
            return new ThresholdTextConfig
            {
                Content = _content,
                OkColor = _okColor,
                NgColor = _ngColor,
                FontSize = _fontSize,
                Opacity = _opacity,
                PositionX = _positionX,
                PositionY = _positionY
            };
        }
    }
}
