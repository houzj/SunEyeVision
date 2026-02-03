using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 工作流节点模型
    /// </summary>
    public class WorkflowNode : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _algorithmType = string.Empty;
        private Point _position;
        private bool _isSelected;
        private bool _isEnabled = true;
        private string _status = "待运行";
        private int _index;
        private int _globalIndex;
        private string _nodeTypeIcon = string.Empty;
        private NodeStyleConfig _styleConfig = NodeStyles.Standard; // 默认样式配置

        /// <summary>
        /// 节点样式配置（用于完全解耦样式和逻辑）
        /// </summary>
        public NodeStyleConfig StyleConfig
        {
            get => _styleConfig;
            set
            {
                if (_styleConfig != value)
                {
                    _styleConfig = value ?? NodeStyles.Standard;
                    _styleConfig.Validate();
                    OnPropertyChanged();
                    // 触发端口位置属性变化
                    OnPropertyChanged(nameof(TopPortPosition));
                    OnPropertyChanged(nameof(BottomPortPosition));
                    OnPropertyChanged(nameof(LeftPortPosition));
                    OnPropertyChanged(nameof(RightPortPosition));
                }
            }
        }

        /// <summary>
        /// 属性变更前事件
        /// </summary>
        public event Action<WorkflowNode, string>? PropertyChanging;

        /// <summary>
        /// 属性变更后事件（扩展的标准PropertyChanged）
        /// </summary>
        public event Action<WorkflowNode, string>? PropertyChangedExtended;

        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AlgorithmType
        {
            get => _algorithmType;
            set
            {
                if (_algorithmType != value)
                {
                    _algorithmType = value;
                    OnPropertyChanged();
                }
            }
        }

        public Point Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PositionX));
                    OnPropertyChanged(nameof(PositionY));
                }
            }
        }

        /// <summary>
        /// Node X coordinate for binding
        /// </summary>
        public double PositionX
        {
            get => Position.X;
            set
            {
                if (Position.X != value)
                {
                    Position = new Point(value, Position.Y);
                }
            }
        }

        /// <summary>
        /// 获取上方连接点位置（动态计算，完全解耦）
        /// </summary>
        public Point TopPortPosition => _styleConfig.GetTopPortPosition(Position);

        /// <summary>
        /// 获取下方连接点位置（动态计算，完全解耦）
        /// </summary>
        public Point BottomPortPosition => _styleConfig.GetBottomPortPosition(Position);

        /// <summary>
        /// 获取左侧连接点位置（动态计算，完全解耦）
        /// </summary>
        public Point LeftPortPosition => _styleConfig.GetLeftPortPosition(Position);

        /// <summary>
        /// 获取右侧连接点位置（动态计算，完全解耦）
        /// </summary>
        public Point RightPortPosition => _styleConfig.GetRightPortPosition(Position);

        /// <summary>
        /// 获取节点边界矩形（用于框选等操作）
        /// </summary>
        public Rect NodeRect => _styleConfig.GetNodeRect(Position);

        /// <summary>
        /// 获取节点中心点（用于距离计算）
        /// </summary>
        public Point NodeCenter => _styleConfig.GetNodeCenter(Position);

        /// <summary>
        /// Node Y coordinate for binding
        /// </summary>
        public double PositionY
        {
            get => Position.Y;
            set
            {
                if (Position.Y != value)
                {
                    Position = new Point(Position.X, value);
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 工作流中的本地序号（同一工作流中相同类型节点的序号）
        /// </summary>
        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    OnPropertyChanging(nameof(Index));
                    _index = value;
                    OnPropertyChanged(nameof(Index));
                    OnPropertyChanged(nameof(LocalDisplayName));
                    OnPropertyChangedExtended(nameof(Index));
                }
            }
        }

        /// <summary>
        /// 全局唯一序号（所有工作流中的总序号，不可修改）
        /// </summary>
        public int GlobalIndex { get; private set; }

        /// <summary>
        /// 节点类型图标
        /// </summary>
        public string NodeTypeIcon
        {
            get => _nodeTypeIcon;
            set
            {
                if (_nodeTypeIcon != value)
                {
                    _nodeTypeIcon = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 本地显示名称（节点名称 + 本地序号）
        /// </summary>
        public string LocalDisplayName => $"{_name} {_index}";

        public WorkflowNode(string id, string name, string algorithmType, int index = 0, int globalIndex = 0)
        {
            Id = id;
            Name = name;
            AlgorithmType = algorithmType;
            Index = index;
            GlobalIndex = globalIndex;
            Position = new Point(0, 0);
            // 图标由工厂设置，不再在这里设置
        }

        /// <summary>
        /// 触发属性变更前事件
        /// </summary>
        protected void OnPropertyChanging(string propertyName)
        {
            PropertyChanging?.Invoke(this, propertyName);
        }

        /// <summary>
        /// 触发属性变更后事件（扩展）
        /// </summary>
        protected void OnPropertyChangedExtended(string propertyName)
        {
            PropertyChangedExtended?.Invoke(this, propertyName);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 工作流连接线模型
    /// </summary>
        public class WorkflowConnection : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _sourceNodeId = string.Empty;
        private string _targetNodeId = string.Empty;
        private string _sourcePort = "output";
        private string _targetPort = "input";
        private System.Windows.Point _sourcePosition;
        private System.Windows.Point _targetPosition;
        private System.Windows.Point _arrowPosition;
        private double _arrowAngle = 0;
        private ConnectionStatus _status = ConnectionStatus.Idle;
        private bool _showPathPoints = false;
        private string _pathData = string.Empty;
        private bool _isSelected = false;

        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SourceNodeId
        {
            get => _sourceNodeId;
            set
            {
                if (_sourceNodeId != value)
                {
                    _sourceNodeId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetNodeId
        {
            get => _targetNodeId;
            set
            {
                if (_targetNodeId != value)
                {
                    _targetNodeId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SourcePort
        {
            get => _sourcePort;
            set
            {
                if (_sourcePort != value)
                {
                    _sourcePort = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetPort
        {
            get => _targetPort;
            set
            {
                if (_targetPort != value)
                {
                    _targetPort = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 箭头角度 (度) - 用于旋转箭头指向目标方向
        /// </summary>
        public double ArrowAngle
        {
            get => _arrowAngle;
            set
            {
                if (_arrowAngle != value)
                {
                    _arrowAngle = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 箭头位置 - 箭头尾部的实际显示位置
        /// </summary>
        public System.Windows.Point ArrowPosition
        {
            get => _arrowPosition;
            set
            {
                if (_arrowPosition != value)
                {
                    _arrowPosition = value;
                    OnPropertyChanged();
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
        /// 箭头大小 - 固定为10px
        /// </summary>
        public double ArrowSize => 10;

        /// <summary>
        /// 箭头缩放比例 - 固定为1.0（10px基准）
        /// </summary>
        public double ArrowScale => 1.0;

        /// <summary>
        /// 连接状态 - 用于执行过程中的视觉反馈
        /// </summary>
        public ConnectionStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
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
            set
            {
                if (_showPathPoints != value)
                {
                    _showPathPoints = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 连线路径数据（SVG路径字符串）
        /// </summary>
        public string PathData
        {
            get => _pathData;
            set
            {
                if (_pathData != value)
                {
                    _pathData = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 连线路径点集合（拐点）
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<System.Windows.Point> PathPoints { get; set; } = new System.Collections.ObjectModel.ObservableCollection<System.Windows.Point>();

        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public System.Windows.Point SourcePosition
        {
            get => _sourcePosition;
            set
            {
                if (_sourcePosition != value)
                {
                    // 注意：这是 Model 类，无法直接访问 ViewModel
                    // 日志已移到 WorkflowCanvasControl 中通过 _viewModel?.AddLog 输出
                    _sourcePosition = value;
                    OnPropertyChanged();
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
                if (_targetPosition != value)
                {
                    // 注意：这是 Model 类，无法直接访问 ViewModel
                    // 日志已移到 WorkflowCanvasControl 中通过 _viewModel?.AddLog 输出
                    _targetPosition = value;
                    OnPropertyChanged();
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
