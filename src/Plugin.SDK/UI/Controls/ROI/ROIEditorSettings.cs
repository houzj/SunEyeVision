using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// ROI编辑器显示设置
    /// </summary>
    public class ROIEditorSettings : INotifyPropertyChanged
    {
        private double _labelFontSize = 12;
        private Color _labelForeground = Colors.Blue;
        private Color _labelBackground = Color.FromArgb(180, 255, 255, 255);
        private double _defaultStrokeThickness = 2;
        private double _selectedStrokeThickness = 3;
        private string _labelFontFamily = "Microsoft YaHei";
        private bool _showLabelOnPreview = true;
        private bool _showLabelOnEdit = true;
        private double _labelOffset = 8; // 标签与ROI的间距

        /// <summary>
        /// 标签字体大小
        /// </summary>
        public double LabelFontSize
        {
            get => _labelFontSize;
            set => SetProperty(ref _labelFontSize, value);
        }

        /// <summary>
        /// 标签前景色
        /// </summary>
        public Color LabelForeground
        {
            get => _labelForeground;
            set => SetProperty(ref _labelForeground, value);
        }

        /// <summary>
        /// 标签背景色
        /// </summary>
        public Color LabelBackground
        {
            get => _labelBackground;
            set => SetProperty(ref _labelBackground, value);
        }

        /// <summary>
        /// 默认边框线宽
        /// </summary>
        public double DefaultStrokeThickness
        {
            get => _defaultStrokeThickness;
            set => SetProperty(ref _defaultStrokeThickness, value);
        }

        /// <summary>
        /// 选中时边框线宽
        /// </summary>
        public double SelectedStrokeThickness
        {
            get => _selectedStrokeThickness;
            set => SetProperty(ref _selectedStrokeThickness, value);
        }

        /// <summary>
        /// 标签字体族
        /// </summary>
        public string LabelFontFamily
        {
            get => _labelFontFamily;
            set => SetProperty(ref _labelFontFamily, value);
        }

        /// <summary>
        /// 预览时是否显示标签
        /// </summary>
        public bool ShowLabelOnPreview
        {
            get => _showLabelOnPreview;
            set => SetProperty(ref _showLabelOnPreview, value);
        }

        /// <summary>
        /// 编辑时是否显示标签
        /// </summary>
        public bool ShowLabelOnEdit
        {
            get => _showLabelOnEdit;
            set => SetProperty(ref _showLabelOnEdit, value);
        }

        /// <summary>
        /// 标签与ROI的间距（像素）
        /// </summary>
        public double LabelOffset
        {
            get => _labelOffset;
            set => SetProperty(ref _labelOffset, value);
        }

        /// <summary>
        /// 获取默认设置实例
        /// </summary>
        public static ROIEditorSettings Default { get; } = new ROIEditorSettings();

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
