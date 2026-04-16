using System.Text.Json.Serialization;

namespace SunEyeVision.Plugin.SDK.Execution
{
    /// <summary>
    /// 区域订阅模式
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegionSubscribeMode
    {
        /// <summary>
        /// 按矩形区域 - 订阅整个矩形区域对象
        /// </summary>
        RectRegion = 0,

        /// <summary>
        /// 按矩形参数 - 分别订阅矩形的 X, Y, Width, Height 参数
        /// </summary>
        RectParameter = 1,

        /// <summary>
        /// 按圆形区域 - 订阅整个圆形区域对象
        /// </summary>
        CircleRegion = 2,

        /// <summary>
        /// 按圆形参数 - 分别订阅圆形的 CenterX, CenterY, Radius 参数
        /// </summary>
        CircleParameter = 3
    }
}
