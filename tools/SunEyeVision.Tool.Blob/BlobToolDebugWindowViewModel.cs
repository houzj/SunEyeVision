using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Metadata;

namespace SunEyeVision.Tool.Blob.Views
{
    /// <summary>
    /// Blob工具调试窗口ViewModel
    /// </summary>
    public class BlobToolDebugWindowViewModel
    {
        #region 字段

        private BlobParameters _parameters;
        private int _minArea = 10;
        private bool _showBlobs = true;
        private bool _showLabels = true;
        private BlobResults _results;

        #endregion

        #region 属性

        /// <summary>
        /// 工具参数
        /// </summary>
        public BlobParameters Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                if (_parameters != null)
                {
                    MinArea = _parameters.MinArea;
                }
            }
        }

        /// <summary>
        /// Blob结果
        /// </summary>
        public BlobResults Results
        {
            get => _results;
            set
            {
                _results = value;
                OnPropertyChanged(nameof(BlobResults));
                OnPropertyChanged(nameof(BlobCount));
            }
        }

        /// <summary>
        /// 最小面积
        /// </summary>
        public int MinArea
        {
            get => _minArea;
            set
            {
                if (_minArea != value)
                {
                    _minArea = value;
                    OnPropertyChanged(nameof(MinArea));
                    // 更新参数
                    if (_parameters != null)
                    {
                        _parameters.MinArea = value;
                    }
                }
            }
        }

        /// <summary>
        /// 显示Blob
        /// </summary>
        public bool ShowBlobs
        {
            get => _showBlobs;
            set
            {
                if (_showBlobs != value)
                {
                    _showBlobs = value;
                    OnPropertyChanged(nameof(ShowBlobs));
                    // 刷新叠加图像
                    UpdateOverlayImage();
                }
            }
        }

        /// <summary>
        /// 显示标签
        /// </summary>
        public bool ShowLabels
        {
            get => _showLabels;
            set
            {
                if (_showLabels != value)
                {
                    _showLabels = value;
                    OnPropertyChanged(nameof(ShowLabels));
                    // 刷新叠加图像
                    UpdateOverlayImage();
                }
            }
        }

        /// <summary>
        /// Blob总数
        /// </summary>
        public int BlobCount => _results?.Blobs?.Count ?? 0;

        /// <summary>
        /// Blob结果列表（用于绑定到DataGrid）
        /// </summary>
        public ObservableCollection<BlobResultViewModel> BlobResults { get; set; } = new();

        /// <summary>
        /// 缩略图图像源
        /// </summary>
        public object ThumbnailImage => _results?.OverlayImage;

        #endregion

        #region 命令

        /// <summary>
        /// 最小面积增加命令
        /// </summary>
        public ICommand IncrementMinAreaCommand => new RelayCommand(() =>
        {
            MinArea = Math.Min(MinArea + 10, 10000);
        });

        /// <summary>
        /// 最小面积减少命令
        /// </summary>
        public ICommand DecrementMinAreaCommand => new RelayCommand(() =>
        {
            MinArea = Math.Max(MinArea - 10, 1);
        });

        /// <summary>
        /// 打开样式设置命令
        /// </summary>
        public ICommand OpenStyleSettingsCommand => new RelayCommand(() =>
        {
            // TODO: 打开样式设置弹窗
        });

        #endregion

        #region 方法

        /// <summary>
        /// 更新Blob结果列表
        /// </summary>
        public void UpdateBlobResults()
        {
            BlobResults.Clear();
            
            if (_results?.Blobs != null)
            {
                foreach (var blob in _results.Blobs)
                {
                    BlobResults.Add(new BlobResultViewModel(blob));
                }
            }
        }

        /// <summary>
        /// 更新叠加图像
        /// </summary>
        private void UpdateOverlayImage()
        {
            // TODO: 根据ShowBlobs和ShowLabels重新生成叠加图像
            OnPropertyChanged(nameof(ThumbnailImage));
        }

        /// <summary>
        /// 属性变更通知
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region ViewModel类

        /// <summary>
        /// Blob结果ViewModel（用于UI绑定）
        /// </summary>
        public class BlobResultViewModel
        {
            public BlobResultViewModel(BlobResult blob)
            {
                Index = blob.Index;
                Area = (int)blob.Area;
                CenterX = (int)blob.CenterX;
                CenterY = (int)blob.CenterY;
                Width = (int)blob.Width;
                Height = (int)blob.Height;
                Circularity = Math.Round(blob.Circularity, 2);
                Convexity = Math.Round(blob.Convexity, 2);
                InertiaRatio = Math.Round(blob.InertiaRatio, 2);
                IsVisible = blob.IsVisible;
            }

            public int Index { get; set; }
            public int Area { get; set; }
            public int CenterX { get; set; }
            public int CenterY { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public double Circularity { get; set; }
            public double Convexity { get; set; }
            public double InertiaRatio { get; set; }
            public bool IsVisible { get; set; }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 简单命令实现
        /// </summary>
        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
            {
                return _canExecute?.Invoke() ?? true;
            }

            public void Execute(object? parameter)
            {
                _execute.Invoke();
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
        }

        #endregion
    }
}
