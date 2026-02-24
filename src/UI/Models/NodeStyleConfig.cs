using System;
using System.Windows;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 节点样式配置 - 完全解耦样式与逻辑
    /// </summary>
    public class NodeStyleConfig
    {
        /// <summary>
        /// 节点宽度
        /// </summary>
        public double NodeWidth { get; set; } = 160;

        /// <summary>
        /// 节点高度
        /// </summary>
        public double NodeHeight { get; set; } = 40;

        /// <summary>
        /// 端口大小（直径）
        /// </summary>
        public double PortSize { get; set; } = 10;

        /// <summary>
        /// 端口外边距（端口距离节点的距离）
        /// </summary>
        public double PortMargin { get; set; } = 10;

        /// <summary>
        /// 节点圆角半径
        /// </summary>
        public double CornerRadius { get; set; } = 20;

        /// <summary>
        /// 节点内容边距
        /// </summary>
        public double ContentMargin { get; set; } = 12;

        /// <summary>
        /// 芯片厚度
        /// </summary>
        public double ChipThickness { get; set; } = 2;

        /// <summary>
        /// 验证配置的有效�?
        /// </summary>
        public void Validate()
        {
            if (NodeWidth <= 0)
                throw new InvalidOperationException("节点宽度必须大于0");
            if (NodeHeight <= 0)
                throw new InvalidOperationException("节点高度必须大于0");
            if (PortSize <= 0)
                throw new InvalidOperationException("端口大小必须大于0");
            if (PortMargin < 0)
                throw new InvalidOperationException("端口外边距不能为负数");
        }

        /// <summary>
        /// 计算节点的边界矩�?
        /// </summary>
        public Rect GetNodeRect(Point position)
        {
            return new Rect(position.X, position.Y, NodeWidth, NodeHeight);
        }

        /// <summary>
        /// 计算节点中心�?
        /// </summary>
        public Point GetNodeCenter(Point position)
        {
            return new Point(
                position.X + NodeWidth / 2,
                position.Y + NodeHeight / 2
            );
        }

        /// <summary>
        /// 计算上端口中心位�?
        /// </summary>
        public Point GetTopPortPosition(Point position)
        {
            return new Point(
                position.X + NodeWidth / 2,
                position.Y
            );
        }

        /// <summary>
        /// 计算下端口中心位�?
        /// </summary>
        public Point GetBottomPortPosition(Point position)
        {
            return new Point(
                position.X + NodeWidth / 2,
                position.Y + NodeHeight
            );
        }

        /// <summary>
        /// 计算左端口中心位�?
        /// </summary>
        public Point GetLeftPortPosition(Point position)
        {
            return new Point(
                position.X,
                position.Y + NodeHeight / 2
            );
        }

        /// <summary>
        /// 计算右端口中心位�?
        /// </summary>
        public Point GetRightPortPosition(Point position)
        {
            return new Point(
                position.X + NodeWidth,
                position.Y + NodeHeight / 2
            );
        }
    }

    /// <summary>
    /// 预定义的节点样式
    /// </summary>
    public static class NodeStyles
    {
        /// <summary>
        /// 标准节点样式（当前默认）
        /// </summary>
        public static readonly NodeStyleConfig Standard = new NodeStyleConfig
        {
            NodeWidth = 160,
            NodeHeight = 40,
            PortSize = 10,
            PortMargin = 10,
            CornerRadius = 20,
            ContentMargin = 12,
            ChipThickness = 2
        };

        /// <summary>
        /// 紧凑节点样式（小尺寸�?
        /// </summary>
        public static readonly NodeStyleConfig Compact = new NodeStyleConfig
        {
            NodeWidth = 120,
            NodeHeight = 30,
            PortSize = 8,
            PortMargin = 8,
            CornerRadius = 15,
            ContentMargin = 10,
            ChipThickness = 1.5
        };

        /// <summary>
        /// 大型节点样式（大尺寸�?
        /// </summary>
        public static readonly NodeStyleConfig Large = new NodeStyleConfig
        {
            NodeWidth = 200,
            NodeHeight = 60,
            PortSize = 12,
            PortMargin = 12,
            CornerRadius = 25,
            ContentMargin = 15,
            ChipThickness = 3
        };
    }
}
