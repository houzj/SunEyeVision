using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// ROI显示信息模型 - 用于信息面板数据绑定
    /// </summary>
    public class ROIDisplayInfo : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private Point _position;
        private Size _size;
        private double _rotation;
        private double _radius;
        private Point _endPoint;
        private double _length;
        private ROIType _type;
        private Guid _id;

        /// <summary>
        /// ROI唯一标识
        /// </summary>
        public Guid ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// ROI名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// ROI类型
        /// </summary>
        public ROIType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 位置（中心点或起点）
        /// </summary>
        public Point Position
        {
            get => _position;
            set
            {
                if (SetProperty(ref _position, value))
                {
                    OnPropertyChanged(nameof(PositionX));
                    OnPropertyChanged(nameof(PositionY));
                    OnPropertyChanged(nameof(StartPointX));
                    OnPropertyChanged(nameof(StartPointY));
                }
            }
        }

        /// <summary>
        /// X坐标
        /// </summary>
        public double PositionX
        {
            get => _position.X;
            set
            {
                if (Math.Abs(_position.X - value) > 0.001)
                {
                    _position.X = value;
                    OnPropertyChanged(nameof(Position));
                    OnPropertyChanged(nameof(PositionX));
                    OnPropertyChanged(nameof(StartPointX));
                }
            }
        }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double PositionY
        {
            get => _position.Y;
            set
            {
                if (Math.Abs(_position.Y - value) > 0.001)
                {
                    _position.Y = value;
                    OnPropertyChanged(nameof(Position));
                    OnPropertyChanged(nameof(PositionY));
                    OnPropertyChanged(nameof(StartPointY));
                }
            }
        }

        /// <summary>
        /// 尺寸
        /// </summary>
        public Size Size
        {
            get => _size;
            set
            {
                if (SetProperty(ref _size, value))
                {
                    OnPropertyChanged(nameof(Width));
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width
        {
            get => _size.Width;
            set
            {
                if (Math.Abs(_size.Width - value) > 0.001)
                {
                    _size.Width = value;
                    OnPropertyChanged(nameof(Size));
                    OnPropertyChanged(nameof(Width));
                }
            }
        }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height
        {
            get => _size.Height;
            set
            {
                if (Math.Abs(_size.Height - value) > 0.001)
                {
                    _size.Height = value;
                    OnPropertyChanged(nameof(Size));
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        /// <summary>
        /// 旋转角度（度数，仅用于旋转矩形）
        /// 范围[-180°, 180°]，逆时针为正
        /// </summary>
        public double Rotation
        {
            get => _rotation;
            set => SetProperty(ref _rotation, value);
        }

        /// <summary>
        /// 半径（用于圆形）
        /// </summary>
        public double Radius
        {
            get => _radius;
            set => SetProperty(ref _radius, value);
        }

        /// <summary>
        /// 终点（用于直线）
        /// </summary>
        public Point EndPoint
        {
            get => _endPoint;
            set
            {
                if (SetProperty(ref _endPoint, value))
                {
                    OnPropertyChanged(nameof(EndPointX));
                    OnPropertyChanged(nameof(EndPointY));
                }
            }
        }

        /// <summary>
        /// 终点X坐标
        /// </summary>
        public double EndPointX
        {
            get => _endPoint.X;
            set
            {
                if (Math.Abs(_endPoint.X - value) > 0.001)
                {
                    _endPoint.X = value;
                    OnPropertyChanged(nameof(EndPoint));
                    OnPropertyChanged(nameof(EndPointX));
                    UpdateLength();
                }
            }
        }

        /// <summary>
        /// 终点Y坐标
        /// </summary>
        public double EndPointY
        {
            get => _endPoint.Y;
            set
            {
                if (Math.Abs(_endPoint.Y - value) > 0.001)
                {
                    _endPoint.Y = value;
                    OnPropertyChanged(nameof(EndPoint));
                    OnPropertyChanged(nameof(EndPointY));
                    UpdateLength();
                }
            }
        }

        /// <summary>
        /// 直线起点X坐标（别名，等同于PositionX）
        /// </summary>
        public double StartPointX
        {
            get => PositionX;
            set => PositionX = value;
        }

        /// <summary>
        /// 直线起点Y坐标（别名，等同于PositionY）
        /// </summary>
        public double StartPointY
        {
            get => PositionY;
            set => PositionY = value;
        }

        /// <summary>
        /// 直线长度
        /// </summary>
        public double Length
        {
            get => _length;
            private set => SetProperty(ref _length, value);
        }

        private double _lineAngle;

        /// <summary>
        /// 直线角度（从起点到终点的方向角，数学角度系统，逆时针为正，范围[-180°, 180°]）
        /// </summary>
        public double LineAngle
        {
            get => _lineAngle;
            private set => SetProperty(ref _lineAngle, value);
        }

        /// <summary>
        /// 更新直线长度和角度
        /// </summary>
        private void UpdateLength()
        {
            if (Type == ROIType.Line)
            {
                var dx = _endPoint.X - _position.X;
                var dy = _endPoint.Y - _position.Y;
                Length = Math.Sqrt(dx * dx + dy * dy);

                // 计算角度（数学角度系统：从X轴正方向逆时针旋转的角度）
                // 屏幕坐标系Y轴向下，Atan2(dy,dx)返回的角度与数学角度符号相反
                // 数学角度：逆时针为正，所以需要取负
                // Atan2 返回 [-π, π]，转换为度数 [-180°, 180°]
                LineAngle = -Math.Atan2(dy, dx) * 180 / Math.PI;
            }
        }

        /// <summary>
        /// 从ROI对象更新显示信息
        /// </summary>
        public void UpdateFromROI(ROI roi)
        {
            if (roi == null) return;

            ID = roi.ID;
            Name = string.IsNullOrEmpty(roi.Name) ? $"{roi.Type}_{roi.ID.ToString().Substring(0, 8)}" : roi.Name;
            Type = roi.Type;
            Position = roi.Position;
            Size = roi.Size;
            Rotation = roi.Rotation;
            Radius = roi.Radius;
            EndPoint = roi.EndPoint;
            UpdateLength();
        }

        /// <summary>
        /// 应用修改到ROI对象
        /// </summary>
        public void ApplyToROI(ROI roi)
        {
            if (roi == null) return;

            roi.Name = Name;
            roi.Position = Position;
            roi.Size = Size;
            roi.Rotation = Rotation;
            roi.Radius = Radius;
            roi.EndPoint = EndPoint;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
