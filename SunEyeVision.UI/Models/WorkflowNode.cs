using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

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

        public WorkflowNode(string id, string name, string algorithmType)
        {
            Id = id;
            Name = name;
            AlgorithmType = algorithmType;
            Position = new Point(0, 0);
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

        public System.Windows.Point SourcePosition
        {
            get => _sourcePosition;
            set
            {
                if (_sourcePosition != value)
                {
                    _sourcePosition = value;
                    OnPropertyChanged();
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
                    _targetPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public WorkflowConnection(string id, string sourceNodeId, string targetNodeId)
        {
            Id = id;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            SourcePosition = new Point(0, 0);
            TargetPosition = new Point(0, 0);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
