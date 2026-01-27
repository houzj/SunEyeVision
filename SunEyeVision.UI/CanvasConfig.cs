using System.Windows;

namespace SunEyeVision.UI
{
    /// <summary>
    /// 画布配置类 - 集中管理画布相关的常量和配置参数
    /// </summary>
    public static class CanvasConfig
    {
        #region 节点配置

        /// <summary>
        /// 节点宽度
        /// </summary>
        public static readonly double NodeWidth = 140;

        /// <summary>
        /// 节点高度
        /// </summary>
        public static readonly double NodeHeight = 90;

        /// <summary>
        /// 节点半径（用于圆角）
        /// </summary>
        public static readonly double NodeCornerRadius = 8;

        /// <summary>
        /// 节点边框粗细
        /// </summary>
        public static readonly double NodeBorderThickness = 2;

        /// <summary>
        /// 节点选中边框粗细
        /// </summary>
        public static readonly double SelectedBorderThickness = 3;

        #endregion

        #region 端口配置

        /// <summary>
        /// 端口大小（直径）
        /// </summary>
        public static readonly double PortSize = 14;

        /// <summary>
        /// 端口悬停大小（直径）
        /// </summary>
        public static readonly double PortHoverSize = 18;

        /// <summary>
        /// 端口到节点边缘的距离
        /// </summary>
        public static readonly double PortMargin = 0;

        /// <summary>
        /// 端口命中测试距离
        /// </summary>
        public const double PortHitTestDistance = 20;

        #endregion

        #region 连接配置

        /// <summary>
        /// 连接线粗细
        /// </summary>
        public static readonly double ConnectionStrokeThickness = 2;

        /// <summary>
        /// 连接线虚线模式
        /// </summary>
        public static readonly double[] ConnectionDashArray = new double[] { 5, 5 };

        /// <summary>
        /// 箭头大小
        /// </summary>
        public static readonly double ArrowSize = 10;

        /// <summary>
        /// 箭头颜色
        /// </summary>
        public static readonly string ArrowColor = "#666666";

        /// <summary>
        /// 连接线默认颜色
        /// </summary>
        public static readonly string DefaultConnectionColor = "#666666";

        /// <summary>
        /// 连接线选中颜色
        /// </summary>
        public static readonly string SelectedConnectionColor = "#FF9500";

        /// <summary>
        /// 连接线悬停颜色
        /// </summary>
        public static readonly string HoverConnectionColor = "#3F51B5";

        #endregion

        #region 拖拽配置

        /// <summary>
        /// 拖拽开始距离阈值（像素）
        /// </summary>
        public static readonly double DragStartThreshold = 5;

        /// <summary>
        /// 拖拽命中测试距离
        /// </summary>
        public static readonly double DragHitTestDistance = 30;

        #endregion

        #region 框选配置

        /// <summary>
        /// 框选框边框颜色
        /// </summary>
        public static readonly string SelectionBoxBorderColor = "#3F51B5";

        /// <summary>
        /// 框选框背景颜色（带透明度）
        /// </summary>
        public static readonly string SelectionBoxBackgroundColor = "#3F51B533";

        /// <summary>
        /// 框选框边框粗细
        /// </summary>
        public static readonly double SelectionBoxBorderThickness = 2;

        #endregion

        #region 网格和对齐配置

        /// <summary>
        /// 网格大小
        /// </summary>
        public const double GridSize = 20;

        /// <summary>
        /// 吸附距离
        /// </summary>
        public const double SnapDistance = 10;

        /// <summary>
        /// 是否启用网格吸附
        /// </summary>
        public static readonly bool EnableGridSnap = true;

        /// <summary>
        /// 是否启用节点对齐
        /// </summary>
        public static readonly bool EnableNodeSnap = true;

        #endregion

        #region 缩放和平移配置

        /// <summary>
        /// 最小缩放比例
        /// </summary>
        public static readonly double MinScale = 0.1;

        /// <summary>
        /// 最大缩放比例
        /// </summary>
        public static readonly double MaxScale = 5.0;

        /// <summary>
        /// 缩放步进值
        /// </summary>
        public static readonly double ScaleStep = 0.1;

        /// <summary>
        /// 默认缩放比例
        /// </summary>
        public static readonly double DefaultScale = 1.0;

        #endregion

        #region 性能配置

        /// <summary>
        /// HitTest调用间隔（毫秒）
        /// </summary>
        public static readonly int HitTestIntervalMs = 50;

        /// <summary>
        /// 节点四叉树查询范围（像素）
        /// </summary>
        public static readonly double QuadTreeQueryRange = 100;

        /// <summary>
        /// 最大节点数量（超过此数量启用虚拟化）
        /// </summary>
        public static readonly int MaxNodesBeforeVirtualization = 100;

        /// <summary>
        /// 是否启用虚拟化
        /// </summary>
        public static readonly bool EnableVirtualization = true;

        /// <summary>
        /// 空间索引类型（Grid/QuadTree）
        /// </summary>
        public static readonly string SpatialIndexType = "Grid";

        /// <summary>
        /// 网格索引单元格大小（像素）
        /// </summary>
        public static readonly double GridCellSize = 200;

        /// <summary>
        /// 四叉树节点容量
        /// </summary>
        public static readonly int QuadTreeCapacity = 10;

        /// <summary>
        /// 四叉树最大深度
        /// </summary>
        public static readonly int QuadTreeMaxDepth = 8;

        /// <summary>
        /// 日志采样率（每N次事件输出一次日志）
        /// </summary>
        public static readonly int LogSampleRate = 100;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public static readonly bool EnableVerboseLogging = false;

        #endregion

        #region 颜色配置

        /// <summary>
        /// 节点默认背景色
        /// </summary>
        public static readonly string DefaultNodeBackground = "#FFFFFF";

        /// <summary>
        /// 节点选中背景色
        /// </summary>
        public static readonly string SelectedNodeBackground = "#E3F2FD";

        /// <summary>
        /// 节点悬停背景色
        /// </summary>
        public static readonly string HoverNodeBackground = "#F5F5F5";

        /// <summary>
        /// 节点禁用背景色
        /// </summary>
        public static readonly string DisabledNodeBackground = "#F5F5F5";

        /// <summary>
        /// 节点默认边框色
        /// </summary>
        public static readonly string DefaultNodeBorder = "#CCCCCC";

        /// <summary>
        /// 节点选中边框色
        /// </summary>
        public static readonly string SelectedNodeBorder = "#2196F3";

        /// <summary>
        /// 端口默认颜色
        /// </summary>
        public static readonly string DefaultPortColor = "#666666";

        /// <summary>
        /// 端口悬停颜色
        /// </summary>
        public static readonly string HoverPortColor = "#2196F3";

        /// <summary>
        /// 端口激活颜色
        /// </summary>
        public static readonly string ActivePortColor = "#FF9500";

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取节点中心点
        /// </summary>
        public static Point GetNodeCenter(Point nodePosition)
        {
            return new Point(
                nodePosition.X + NodeWidth / 2,
                nodePosition.Y + NodeHeight / 2
            );
        }

        /// <summary>
        /// 获取节点左上角位置
        /// </summary>
        public static Point GetNodeTopLeft(Point centerPosition)
        {
            return new Point(
                centerPosition.X - NodeWidth / 2,
                centerPosition.Y - NodeHeight / 2
            );
        }

        /// <summary>
        /// 检查点是否在节点范围内
        /// </summary>
        public static bool IsPointInNode(Point point, Point nodePosition)
        {
            return point.X >= nodePosition.X &&
                   point.X <= nodePosition.X + NodeWidth &&
                   point.Y >= nodePosition.Y &&
                   point.Y <= nodePosition.Y + NodeHeight;
        }

        /// <summary>
        /// 将点吸附到网格
        /// </summary>
        public static Point SnapToGrid(Point point)
        {
            double x = Math.Round(point.X / GridSize) * GridSize;
            double y = Math.Round(point.Y / GridSize) * GridSize;
            return new Point(x, y);
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        public static double GetDistance(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion
    }
}
