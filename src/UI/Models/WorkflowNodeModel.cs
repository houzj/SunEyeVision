using System;
using System.Windows;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Workflow;
using SunEyeVision.Plugin.SDK.Models;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 工作流节点模型 - UI 层扩展属性
    /// </summary>
    /// <remarks>
    /// 重构说明（2026-03-26）：
    /// - 继承 WorkflowNodeBase，不再重复实现 INotifyPropertyChanged
    /// - WorkflowNodeBase 已包含 Position, IsSelected, IsVisible, Status 等属性
    /// - 本类仅添加 UI 层特有的强类型属性和计算属性
    ///
    /// 数据源统一：
    /// - UI 直接绑定 Solution.Workflow.Nodes（ObservableCollection）
    /// - 不再需要同步代码
    /// </remarks>
    public class WorkflowNode : WorkflowNodeBase
    {
        /// <summary>
        /// 节点输出缓存（UI 层专用，强类型）
        /// </summary>
        public new NodeOutputCache? OutputCache
        {
            get => base.OutputCache as NodeOutputCache;
            set => base.OutputCache = value;
        }

        /// <summary>
        /// 节点输入源（UI 层专用，强类型）
        /// </summary>
        public new ImageInputSource? InputSource
        {
            get => base.InputSource as ImageInputSource;
            set => base.InputSource = value;
        }

        /// <summary>
        /// 最近一次执行结果（UI 层专用，强类型）
        /// </summary>
        public new ToolResults? LastResult
        {
            get => base.LastResult as ToolResults;
            set => base.LastResult = value;
        }

        /// <summary>
        /// 节点样式配置（UI 层专用，强类型）
        /// </summary>
        public NodeStyleConfig StyleConfigTyped
        {
            get => base.StyleConfig as NodeStyleConfig ?? NodeStyles.Standard;
            set => base.StyleConfig = value;
        }

        #region 端口位置计算属性

        /// <summary>
        /// 获取上方连接点位置（动态计算）
        /// </summary>
        public Point TopPortPosition => StyleConfigTyped.GetTopPortPosition(Position);

        /// <summary>
        /// 获取下方连接点位置（动态计算）
        /// </summary>
        public Point BottomPortPosition => StyleConfigTyped.GetBottomPortPosition(Position);

        /// <summary>
        /// 获取左侧连接点位置（动态计算）
        /// </summary>
        public Point LeftPortPosition => StyleConfigTyped.GetLeftPortPosition(Position);

        /// <summary>
        /// 获取右侧连接点位置（动态计算）
        /// </summary>
        public Point RightPortPosition => StyleConfigTyped.GetRightPortPosition(Position);

        /// <summary>
        /// 获取节点边界矩形（用于框选等操作）
        /// </summary>
        public Rect NodeRect => StyleConfigTyped.GetNodeRect(Position);

        /// <summary>
        /// 获取节点中心点（用于距离计算）
        /// </summary>
        public Point NodeCenter => StyleConfigTyped.GetNodeCenter(Position);

        #endregion

        #region 类型判断属性

        /// <summary>
        /// 是否为图像采集节点（基于 ToolType 计算）
        /// </summary>
        public bool IsImageCaptureNode => ToolType != null && (
            ToolType.Contains("ImageCapture", StringComparison.OrdinalIgnoreCase) ||
            ToolType.Contains("Camera", StringComparison.OrdinalIgnoreCase) ||
            ToolType.Contains("视频源", StringComparison.OrdinalIgnoreCase) ||
            ToolType.Contains("VideoSource", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 是否为图像加载节点（基于 ToolType 计算）
        /// </summary>
        public bool IsImageLoadNode => ToolType != null && (
            ToolType.Contains("ImageLoad", StringComparison.OrdinalIgnoreCase) ||
            ToolType.Contains("图片载入", StringComparison.OrdinalIgnoreCase) ||
            ToolType.Contains("ImageSource", StringComparison.OrdinalIgnoreCase));

        #endregion

        /// <summary>
        /// 确保输入源已初始化
        /// </summary>
        public ImageInputSource EnsureInputSource()
        {
            if (InputSource == null)
            {
                InputSource = new ImageInputSource(Id);
                OnPropertyChanged(nameof(InputSource));
            }
            return InputSource;
        }

        #region 构造函数

        public WorkflowNode(string id, string name, string dispName, string toolType)
            : base(id, name, dispName, toolType)
        {
            // 初始化样式配置
            StyleConfigTyped = NodeStyles.Standard;
        }

        /// <summary>
        /// 供 System.Text.Json 反序列化使用的无参构造函数
        /// </summary>
        [System.Text.Json.Serialization.JsonConstructor]
        public WorkflowNode() : base()
        {
            StyleConfigTyped = NodeStyles.Standard;
        }

        #endregion

        #region 类型转换

        /// <summary>
        /// 从 WorkflowNodeBase 创建 UI 层 WorkflowNode（数据层 → UI 层的统一转换入口）
        /// </summary>
        /// <remarks>
        /// 如果 baseNode 本身就是 WorkflowNode，直接返回；
        /// 否则创建新的 WorkflowNode 并完整复制所有属性。
        /// 确保加载解决方案时不丢失位置、参数、尺寸等信息。
        /// </remarks>
        public static WorkflowNode FromBase(WorkflowNodeBase baseNode)
        {
            if (baseNode is WorkflowNode existing)
                return existing;

            var node = new WorkflowNode(
                baseNode.Id,
                baseNode.Name,
                baseNode.DispName ?? baseNode.Name,
                baseNode.ToolType)
            {
                GlobalIndex = baseNode.GlobalIndex,
                LocalIndex = baseNode.LocalIndex,
                PositionX = baseNode.PositionX,
                PositionY = baseNode.PositionY,
                Width = baseNode.Width,
                Height = baseNode.Height,
                IsEnabled = baseNode.IsEnabled,
                Parameters = baseNode.Parameters,
                ParameterBindings = baseNode.ParameterBindings
            };

            return node;
        }

        #endregion
    }

    /// <summary>
    /// 工作流连接线模型（UI 层专用，保留独立类）
    /// </summary>
    /// <remarks>
    /// 连接线包含大量 UI 渲染相关属性（路径数据、箭头位置等），
    /// 与数据层 Connection 类差异较大，因此保持独立。
    /// </remarks>
    public class WorkflowConnection : ObservableObject
    {
        private string _id = string.Empty;
        private string _sourceNodeId = string.Empty;
        private string _targetNodeId = string.Empty;
        private string _sourcePort = "output";
        private string _targetPort = "input";
        private Point _sourcePosition;
        private Point _targetPosition;
        private Point _arrowPosition;
        private double _arrowAngle = 0;
        private ConnectionStatus _status = ConnectionStatus.Idle;
        private bool _showPathPoints = false;
        private string _pathData = string.Empty;
        private bool _isSelected = false;
        private int _pathUpdateCounter = 0;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string SourceNodeId
        {
            get => _sourceNodeId;
            set => SetProperty(ref _sourceNodeId, value);
        }

        public string TargetNodeId
        {
            get => _targetNodeId;
            set => SetProperty(ref _targetNodeId, value);
        }

        public string SourcePort
        {
            get => _sourcePort;
            set => SetProperty(ref _sourcePort, value);
        }

        public string TargetPort
        {
            get => _targetPort;
            set => SetProperty(ref _targetPort, value);
        }

        /// <summary>
        /// 箭头角度 - 用于旋转箭头指向目标方向
        /// </summary>
        public double ArrowAngle
        {
            get => _arrowAngle;
            set => SetProperty(ref _arrowAngle, value);
        }

        /// <summary>
        /// 箭头位置 - 箭头尾部的实际显示位置
        /// </summary>
        public Point ArrowPosition
        {
            get => _arrowPosition;
            set
            {
                if (SetProperty(ref _arrowPosition, value))
                {
                    OnPropertyChanged(nameof(ArrowX));
                    OnPropertyChanged(nameof(ArrowY));
                }
            }
        }

        /// <summary>
        /// 箭头X坐标
        /// </summary>
        public double ArrowX => ArrowPosition.X;

        /// <summary>
        /// 箭头Y坐标
        /// </summary>
        public double ArrowY => ArrowPosition.Y;

        /// <summary>
        /// 箭头大小 - 固定10px
        /// </summary>
        public double ArrowSize => 10;

        /// <summary>
        /// 箭头缩放比例 - 固定1.0，10px基准
        /// </summary>
        public double ArrowScale => 1.0;

        /// <summary>
        /// 连接状态 - 用于执行过程中的视觉反馈
        /// </summary>
        public ConnectionStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 连接状态对应的颜色
        /// </summary>
        public string StatusColor => Status switch
        {
            ConnectionStatus.Idle => "#666666",
            ConnectionStatus.Transmitting => "#FF9500",
            ConnectionStatus.Completed => "#34C759",
            ConnectionStatus.Error => "#FF3B30",
            _ => "#666666"
        };

        /// <summary>
        /// 是否显示路径点（用于调试）
        /// </summary>
        public bool ShowPathPoints
        {
            get => _showPathPoints;
            set => SetProperty(ref _showPathPoints, value);
        }

        /// <summary>
        /// 连线路径数据（SVG路径字符串）
        /// </summary>
        public string PathData
        {
            get => _pathData;
            set => SetProperty(ref _pathData, value);
        }

        /// <summary>
        /// 连线路径点集合（拐点）
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<Point> PathPoints { get; set; } = new();

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _isVisible = true;

        /// <summary>
        /// 连线是否可见（用于虚拟化渲染）
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public Point SourcePosition
        {
            get => _sourcePosition;
            set
            {
                if (SetProperty(ref _sourcePosition, value))
                {
                    OnPropertyChanged(nameof(StartX));
                    OnPropertyChanged(nameof(StartY));
                }
            }
        }

        public Point TargetPosition
        {
            get => _targetPosition;
            set
            {
                if (SetProperty(ref _targetPosition, value))
                {
                    OnPropertyChanged(nameof(EndX));
                    OnPropertyChanged(nameof(EndY));
                }
            }
        }

        /// <summary>
        /// 连接线起点X坐标（用于绑定）
        /// </summary>
        public double StartX => SourcePosition.X;

        /// <summary>
        /// 连接线起点Y坐标（用于绑定）
        /// </summary>
        public double StartY => SourcePosition.Y;

        /// <summary>
        /// 连接线终点X坐标（用于绑定）
        /// </summary>
        public double EndX => TargetPosition.X;

        /// <summary>
        /// 连接线终点Y坐标（用于绑定）
        /// </summary>
        public double EndY => TargetPosition.Y;

        public WorkflowConnection()
        {
            Id = string.Empty;
            SourceNodeId = string.Empty;
            TargetNodeId = string.Empty;
            SourcePosition = new Point(0, 0);
            TargetPosition = new Point(0, 0);
            ArrowPosition = new Point(0, 0);
        }

        public WorkflowConnection(string id, string sourceNodeId, string targetNodeId)
        {
            Id = id;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            SourcePosition = new Point(0, 0);
            TargetPosition = new Point(0, 0);
            ArrowPosition = new Point(0, 0);
        }

        /// <summary>
        /// 路径更新计数器（用于触发绑定更新）
        /// </summary>
        public int PathUpdateCounter
        {
            get => _pathUpdateCounter;
            private set
            {
                if (_pathUpdateCounter != value)
                {
                    _pathUpdateCounter = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 触发路径相关属性的更新
        /// </summary>
        public void InvalidatePath()
        {
            _pathUpdateCounter++;
            OnPropertyChanged(nameof(PathUpdateCounter));
        }
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionStatus
    {
        Idle,
        Transmitting,
        Completed,
        Error
    }
}
