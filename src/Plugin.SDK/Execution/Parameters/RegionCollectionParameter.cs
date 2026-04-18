using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.Execution.Parameters
{
    /// <summary>
    /// 区域集合参数 - 支持常量和绑定两种模式
    /// </summary>
    /// <remarks>
    /// 设计理念：
    /// 1. 支持常量模式：用户直接绘制区域
    /// 2. 支持绑定模式：订阅其他节点的区域输出
    /// 3. 继承 ParamValueBase，与 ParamValue<T> 保持一致性
    /// 4. 所有工具可复用
    ///
    /// 使用示例：
    /// <code>
    /// public class ThresholdParameters : RegionParameters
    /// {
    ///     public RegionCollectionParameter InspectionRegions { get; }
    ///     public RegionCollectionParameter MaskRegions { get; }
    ///
    ///     public ThresholdParameters()
    ///     {
    ///         InspectionRegions = new RegionCollectionParameter
    ///         {
    ///             Mode = RegionParameterMode.InspectionRegion
    ///         };
    ///
    ///         MaskRegions = new RegionCollectionParameter
    ///         {
    ///             Mode = RegionParameterMode.MaskRegion
    ///         };
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public class RegionCollectionParameter : ParamValueBase
    {
        private readonly ObservableCollection<RegionData> _regions;
        private RegionParameterMode _mode;
        private ParamSetting? _bindingConfig;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RegionCollectionParameter()
        {
            _regions = new ObservableCollection<RegionData>();
            _mode = RegionParameterMode.InspectionRegion;

            VisionLogger.Instance?.Log(LogLevel.Info,
                $"[RegionCollectionParameter] 默认构造函数 | HashCode={GetHashCode():X8} | Mode={_mode} | _regions.HashCode={_regions.GetHashCode():X8}",
                "RegionCollectionParameter");
        }

        /// <summary>
        /// 构造函数（指定模式）
        /// </summary>
        public RegionCollectionParameter(RegionParameterMode mode)
        {
            _regions = new ObservableCollection<RegionData>();
            _mode = mode;

            VisionLogger.Instance?.Log(LogLevel.Info,
                $"[RegionCollectionParameter] 构造函数(mode={mode}) | HashCode={GetHashCode():X8} | _regions.HashCode={_regions.GetHashCode():X8}",
                "RegionCollectionParameter");
        }

        /// <summary>
        /// 区域集合（支持绑定）
        /// </summary>
        /// <remarks>
        /// 支持两种模式：
        /// 1. 常量模式：用户直接绘制的区域集合
        /// 2. 绑定模式：从其他节点订阅的区域集合（通过 BindingConfig）
        /// </remarks>
        public ObservableCollection<RegionData> Regions
        {
            get
            {
                VisionLogger.Instance?.Log(LogLevel.Info,
                    $"[RegionCollectionParameter.Regions.Getter] | HashCode={GetHashCode():X8} | _regions.HashCode={_regions.GetHashCode():X8} | Count={_regions.Count}",
                    "RegionCollectionParameter");

                // 记录每个区域的详细信息
                for (int i = 0; i < _regions.Count; i++)
                {
                    var region = _regions[i];
                    VisionLogger.Instance?.Log(LogLevel.Info,
                        $"[RegionCollectionParameter.Regions.Getter] Region[{i}] | Name={region?.Name ?? "null"} | Type={region?.Parameters?.GetType().Name ?? "null"}",
                        "RegionCollectionParameter");
                }

                return _regions;
            }
        }

        /// <summary>
        /// 区域参数模式
        /// </summary>
        public RegionParameterMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value, "区域模式");
        }

        /// <summary>
        /// 绑定配置（支持订阅其他节点的区域输出）
        /// </summary>
        public override ParamSetting? BindingConfig
        {
            get => _bindingConfig;
            set
            {
                _bindingConfig = value;
                OnPropertyChanged(nameof(BindingConfig));
            }
        }

        /// <summary>
        /// 参数值（实现 ParamValueBase）
        /// </summary>
        [JsonIgnore]
        public override object? ObjectValue
        {
            get => _regions;
            set
            {
                if (value is ObservableCollection<RegionData> collection)
                {
                    _regions.Clear();
                    foreach (var region in collection)
                    {
                        _regions.Add(region);
                    }
                }
            }
        }

        /// <summary>
        /// 值来源标识（实现 ParamValueBase）
        /// </summary>
        public override string ValueSource
        {
            get => _bindingConfig != null ? "Binding" : "Constant";
        }

        /// <summary>
        /// 是否为绑定模式（实现 ParamValueBase）
        /// </summary>
        public override bool IsBinding
        {
            get => _bindingConfig != null;
        }

        /// <summary>
        /// 获取所有启用的区域
        /// </summary>
        /// <returns>启用的区域集合</returns>
        public System.Collections.Generic.IEnumerable<RegionData> GetEnabledRegions()
        {
            return _regions.Where(r => r.IsEnabled);
        }

        /// <summary>
        /// 获取所有可见的区域
        /// </summary>
        /// <returns>可见的区域集合</returns>
        public System.Collections.Generic.IEnumerable<RegionData> GetVisibleRegions()
        {
            return _regions.Where(r => r.IsVisible);
        }

        /// <summary>
        /// 清空所有区域
        /// </summary>
        public void ClearRegions()
        {
            _regions.Clear();
        }

        /// <summary>
        /// 添加区域
        /// </summary>
        /// <param name="region">区域对象</param>
        public void AddRegion(RegionData region)
        {
            _regions.Add(region);
        }

        /// <summary>
        /// 移除区域
        /// </summary>
        /// <param name="region">区域对象</param>
        public void RemoveRegion(RegionData region)
        {
            _regions.Remove(region);
        }

        /// <summary>
        /// 克隆区域集合参数
        /// </summary>
        /// <returns>克隆的对象</returns>
        public RegionCollectionParameter Clone()
        {
            var cloned = new RegionCollectionParameter(_mode)
            {
                BindingConfig = BindingConfig
            };

            foreach (var region in _regions)
            {
                cloned._regions.Add((RegionData)region.Clone());
            }

            return cloned;
        }
    }
}
