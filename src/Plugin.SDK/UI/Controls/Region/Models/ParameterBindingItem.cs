using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Models
{
    /// <summary>
    /// 参数绑定项 - 用于参数面板显示
    /// </summary>
    public class ParameterBindingItem : INotifyPropertyChanged
    {
        private ParameterSource? _source;
        private string _displayPath = string.Empty;
        private object? _currentValue;

        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// 参数显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 参数数据类型
        /// </summary>
        public string DataType { get; set; } = "double";

        /// <summary>
        /// 参数源（可能为null表示未绑定）
        /// </summary>
        public ParameterSource? Source
        {
            get => _source;
            set
            {
                if (SetProperty(ref _source, value))
                {
                    OnPropertyChanged(nameof(IsBound));
                    OnPropertyChanged(nameof(BindingType));
                }
            }
        }

        /// <summary>
        /// 显示路径（绑定的节点输出路径）
        /// </summary>
        public string DisplayPath
        {
            get => _displayPath;
            set => SetProperty(ref _displayPath, value);
        }

        /// <summary>
        /// 当前值（实时更新）
        /// </summary>
        public object? CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        /// <summary>
        /// 是否已绑定
        /// </summary>
        public bool IsBound => Source != null;

        /// <summary>
        /// 绑定类型
        /// </summary>
        public ParameterBindingType BindingType => Source?.BindingType ?? ParameterBindingType.Constant;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
