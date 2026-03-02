using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SunEyeVision.Plugin.SDK.UI.Controls.ROI
{
    /// <summary>
    /// ROI信息面板视图模型
    /// </summary>
    public class ROIInfoViewModel : INotifyPropertyChanged
    {
        private readonly ROIImageEditor _editor;
        private ROIDisplayInfo? _currentInfo;
        private ROIType? _filterType;
        private bool _isMultiSelect;
        private bool _canEditSize;
        private int _selectedCount;
        private List<ROIType> _availableFilterTypes = new List<ROIType>();

        /// <summary>
        /// 当前显示的ROI信息
        /// </summary>
        public ROIDisplayInfo? CurrentInfo
        {
            get => _currentInfo;
            private set => SetProperty(ref _currentInfo, value);
        }

        /// <summary>
        /// 多选时的类型筛选
        /// </summary>
        public ROIType? FilterType
        {
            get => _filterType;
            set
            {
                if (SetProperty(ref _filterType, value))
                {
                    RefreshDisplay();
                }
            }
        }

        /// <summary>
        /// 是否处于多选模式
        /// </summary>
        public bool IsMultiSelect
        {
            get => _isMultiSelect;
            private set
            {
                if (SetProperty(ref _isMultiSelect, value))
                {
                    OnPropertyChanged(nameof(IsSingleSelect));
                    OnPropertyChanged(nameof(ShowFilterTypeSelector));
                }
            }
        }

        /// <summary>
        /// 是否处于单选模式
        /// </summary>
        public bool IsSingleSelect => !_isMultiSelect;

        /// <summary>
        /// 是否显示类型筛选器（多选时显示）
        /// </summary>
        public bool ShowFilterTypeSelector => _isMultiSelect;

        /// <summary>
        /// 是否可以编辑尺寸
        /// </summary>
        public bool CanEditSize
        {
            get => _canEditSize;
            private set => SetProperty(ref _canEditSize, value);
        }

        /// <summary>
        /// 选中的ROI数量
        /// </summary>
        public int SelectedCount
        {
            get => _selectedCount;
            private set => SetProperty(ref _selectedCount, value);
        }

        /// <summary>
        /// 可用的筛选类型列表
        /// </summary>
        public List<ROIType> AvailableFilterTypes
        {
            get => _availableFilterTypes;
            private set => SetProperty(ref _availableFilterTypes, value);
        }

        /// <summary>
        /// 是否有选中项
        /// </summary>
        public bool HasSelection => _selectedCount > 0;

        /// <summary>
        /// 应用到所有同类型ROI命令
        /// </summary>
        public ICommand ApplyToAllCommand { get; }

        /// <summary>
        /// 选中ROI变更事件
        /// </summary>
        public event EventHandler? SelectionChanged;

        /// <summary>
        /// ROI属性变更事件（用于通知编辑器更新）
        /// </summary>
        public event EventHandler<ROIPropertyChangedEventArgs>? ROIPropertyChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ROIInfoViewModel(ROIImageEditor editor)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            ApplyToAllCommand = new RelayCommand(ApplyToAll, CanApplyToAll);
            
            // 订阅编辑器的选择变更事件
            _editor.SelectionChanged += OnEditorSelectionChanged;
            _editor.ROIChanged += OnEditorROIChanged;
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void RefreshDisplay()
        {
            // 取消旧的事件订阅
            if (_currentInfo != null)
            {
                _currentInfo.PropertyChanged -= OnCurrentInfoPropertyChanged;
            }

            var selectedROIs = _editor.GetSelectedROIs().ToList();
            SelectedCount = selectedROIs.Count;
            OnPropertyChanged(nameof(HasSelection));

            if (selectedROIs.Count == 0)
            {
                CurrentInfo = null;
                IsMultiSelect = false;
                CanEditSize = false;
                AvailableFilterTypes = new List<ROIType>();
                return;
            }

            // 单选模式
            if (selectedROIs.Count == 1)
            {
                IsMultiSelect = false;
                CanEditSize = true;
                CurrentInfo = new ROIDisplayInfo();
                CurrentInfo.UpdateFromROI(selectedROIs[0]);
                CurrentInfo.PropertyChanged += OnCurrentInfoPropertyChanged;
                AvailableFilterTypes = new List<ROIType>();
                return;
            }

            // 多选模式
            IsMultiSelect = true;

            // 获取选中ROI的所有类型
            var types = selectedROIs.Select(r => r.Type).Distinct().ToList();
            AvailableFilterTypes = types;

            // 确定筛选类型
            ROIType effectiveFilterType;
            if (_filterType.HasValue && types.Contains(_filterType.Value))
            {
                effectiveFilterType = _filterType.Value;
            }
            else
            {
                // 默认选择旋转矩形，如果没有则选择第一个类型
                effectiveFilterType = types.Contains(ROIType.RotatedRectangle) 
                    ? ROIType.RotatedRectangle 
                    : types.First();
                _filterType = effectiveFilterType;
                OnPropertyChanged(nameof(FilterType));
            }

            // 获取该类型的所有ROI
            var filteredROIs = selectedROIs.Where(r => r.Type == effectiveFilterType).ToList();

            if (filteredROIs.Count == 0)
            {
                CurrentInfo = null;
                CanEditSize = false;
                return;
            }

            // 显示第一个ROI的信息
            CanEditSize = true;
            CurrentInfo = new ROIDisplayInfo();
            CurrentInfo.UpdateFromROI(filteredROIs[0]);
            CurrentInfo.PropertyChanged += OnCurrentInfoPropertyChanged;
        }

        /// <summary>
        /// 当前信息属性变更处理
        /// </summary>
        private void OnCurrentInfoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (CurrentInfo == null) return;

            var selectedROIs = _editor.GetSelectedROIs().ToList();
            if (selectedROIs.Count == 0) return;

            ROI? targetROI = null;

            if (selectedROIs.Count == 1)
            {
                // 单选：直接更新该ROI
                targetROI = selectedROIs[0];
            }
            else if (_filterType.HasValue)
            {
                // 多选：更新第一个匹配类型的ROI（实时预览）
                targetROI = selectedROIs.FirstOrDefault(r => r.Type == _filterType.Value);
            }

            if (targetROI != null)
            {
                CurrentInfo.ApplyToROI(targetROI);
                targetROI.ModifiedTime = DateTime.Now;
                
                // 刷新编辑器显示（名称变更时也需要刷新）
                _editor.InvalidateVisual();
                
                // 通知编辑器刷新
                ROIPropertyChanged?.Invoke(this, new ROIPropertyChangedEventArgs(targetROI, e.PropertyName ?? ""));
            }
        }

        /// <summary>
        /// 应用到所有同类型ROI
        /// </summary>
        private void ApplyToAll()
        {
            if (CurrentInfo == null || !_filterType.HasValue) return;

            var selectedROIs = _editor.GetSelectedROIs()
                .Where(r => r.Type == _filterType.Value)
                .ToList();

            if (selectedROIs.Count <= 1) return;

            foreach (var roi in selectedROIs.Skip(1)) // 跳过第一个（已经更新）
            {
                // 只复制尺寸相关属性，不复制位置
                roi.Size = CurrentInfo.Size;
                roi.Rotation = CurrentInfo.Rotation;
                roi.Radius = CurrentInfo.Radius;
                
                // 对于直线，复制相对偏移
                if (roi.Type == ROIType.Line && selectedROIs.Count > 0)
                {
                    var firstROI = selectedROIs[0];
                    var dx = firstROI.EndPoint.X - firstROI.Position.X;
                    var dy = firstROI.EndPoint.Y - firstROI.Position.Y;
                    roi.EndPoint = new System.Windows.Point(roi.Position.X + dx, roi.Position.Y + dy);
                }

                roi.ModifiedTime = DateTime.Now;
            }

            // 刷新编辑器显示
            _editor.InvalidateVisual();
        }

        /// <summary>
        /// 检查是否可以应用到所有
        /// </summary>
        private bool CanApplyToAll()
        {
            if (CurrentInfo == null || !_filterType.HasValue || !_isMultiSelect)
                return false;

            var selectedROIs = _editor.GetSelectedROIs()
                .Where(r => r.Type == _filterType.Value)
                .ToList();

            return selectedROIs.Count > 1;
        }

        /// <summary>
        /// 编辑器选择变更处理
        /// </summary>
        private void OnEditorSelectionChanged(object? sender, EventArgs e)
        {
            RefreshDisplay();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 编辑器ROI变更处理
        /// </summary>
        private void OnEditorROIChanged(object? sender, ROIChangedEventArgs e)
        {
            // 如果修改的是当前选中的ROI，刷新显示
            if (CurrentInfo != null && e.ROI != null && e.ROI.ID == CurrentInfo.ID)
            {
                CurrentInfo.PropertyChanged -= OnCurrentInfoPropertyChanged;
                CurrentInfo.UpdateFromROI(e.ROI);
                CurrentInfo.PropertyChanged += OnCurrentInfoPropertyChanged;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _editor.SelectionChanged -= OnEditorSelectionChanged;
            _editor.ROIChanged -= OnEditorROIChanged;

            if (CurrentInfo != null)
            {
                CurrentInfo.PropertyChanged -= OnCurrentInfoPropertyChanged;
            }
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

    /// <summary>
    /// ROI属性变更事件参数
    /// </summary>
    public class ROIPropertyChangedEventArgs : EventArgs
    {
        public ROI ROI { get; }
        public string PropertyName { get; }

        public ROIPropertyChangedEventArgs(ROI roi, string propertyName)
        {
            ROI = roi;
            PropertyName = propertyName;
        }
    }

    /// <summary>
    /// 简单命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}
