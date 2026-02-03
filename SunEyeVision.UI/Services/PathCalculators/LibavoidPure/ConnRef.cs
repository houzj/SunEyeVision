using System;
using System.Collections.Generic;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// Libavoid连接器引用（连接线）
    /// </summary>
    public class ConnRef
    {
        public uint Id { get; }
        public AvoidRouter Router { get; }
        public AvoidPoint Source { get; set; }
        public AvoidPoint Target { get; set; }

        private List<AvoidPoint> _cachedPath;
        private bool _needsUpdate = true;

        public ConnRef(uint id, AvoidRouter router, AvoidPoint source, AvoidPoint target)
        {
            Id = id;
            Router = router;
            Source = source;
            Target = target;
        }

        /// <summary>
        /// 获取显示路径
        /// </summary>
        public List<AvoidPoint> GetDisplayRoute()
        {
            if (_needsUpdate || _cachedPath == null)
            {
                _cachedPath = Router.RoutePath(Source, Target);
                _needsUpdate = false;
            }

            return new List<AvoidPoint>(_cachedPath);
        }

        /// <summary>
        /// 是否需要重新路由
        /// </summary>
        public bool NeedsReroute()
        {
            return _needsUpdate;
        }

        /// <summary>
        /// 使路径失效
        /// </summary>
        internal void Invalidate()
        {
            _needsUpdate = true;
        }
    }
}
