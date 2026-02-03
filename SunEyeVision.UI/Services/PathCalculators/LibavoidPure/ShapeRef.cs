using System;

namespace SunEyeVision.UI.Services.PathCalculators.LibavoidPure
{
    /// <summary>
    /// Libavoid形状引用（障碍物）
    /// </summary>
    public class ShapeRef
    {
        public uint Id { get; }
        public AvoidRouter Router { get; }
        public AvoidPolygon Polygon { get; set; }

        public ShapeRef(uint id, AvoidRouter router, AvoidPolygon polygon)
        {
            Id = id;
            Router = router;
            Polygon = polygon;
        }

        public AvoidRectangle Bounds => Polygon.Bounds;
    }
}
