using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SunEyeVision.UI.Views.Controls.Panels;
using SunEyeVision.UI.Services.Thumbnail;

namespace SunEyeVision.UI.Models
{
    /// <summary>
    /// 节点级别的图像数据容器（每个采集节点独立维护）
    /// 用于实现不同采集节点拥有独立的图像预览器
    /// </summary>
    public class NodeImageData : INotifyPropertyChanged
    {
        private int _currentImageIndex = -1;
        private ImageRunMode _imageRunMode = ImageRunMode.运行全部;
        private bool _autoSwitchEnabled = false;

        /// <summary>
        /// 节点ID（与WorkflowNode关联）
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// 该节点专属的图像集合
        /// </summary>
        public BatchObservableCollection<ImageInfo> ImageCollection { get; }

        /// <summary>
        /// 当前显示的图像索引
        /// </summary>
        public int CurrentImageIndex
        {
            get => _currentImageIndex;
            set
            {
                if (SetProperty(ref _currentImageIndex, value))
                {
                    CurrentImageChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 图像运行模式
        /// </summary>
        public ImageRunMode ImageRunMode
        {
            get => _imageRunMode;
            set => SetProperty(ref _imageRunMode, value);
        }

        /// <summary>
        /// 是否启用自动切换
        /// </summary>
        public bool AutoSwitchEnabled
        {
            get => _autoSwitchEnabled;
            set => SetProperty(ref _autoSwitchEnabled, value);
        }

        /// <summary>
        /// 当前图像数量
        /// </summary>
        public int ImageCount => ImageCollection.Count;

        /// <summary>
        /// 获取选中用于运行的图像列表
        /// </summary>
        public IEnumerable<ImageInfo> GetSelectedImages()
        {
            if (ImageRunMode == ImageRunMode.运行全部)
            {
                return ImageCollection;
            }
            return ImageCollection.Where(img => img.IsForRun);
        }

        /// <summary>
        /// 当前图像变化事件
        /// </summary>
        public event EventHandler<int>? CurrentImageChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public NodeImageData(string nodeId)
        {
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            ImageCollection = new BatchObservableCollection<ImageInfo>();
        }

        /// <summary>
        /// 添加图像到集合
        /// </summary>
        public void AddImage(ImageInfo image)
        {
            ImageCollection.Add(image);
            OnPropertyChanged(nameof(ImageCount));
        }

        /// <summary>
        /// 批量添加图像
        /// </summary>
        public void AddImages(IEnumerable<ImageInfo> images)
        {
            ImageCollection.AddRange(images);
            OnPropertyChanged(nameof(ImageCount));
        }

        /// <summary>
        /// 清空图像集合
        /// </summary>
        public void ClearImages()
        {
            ImageCollection.Clear();
            CurrentImageIndex = -1;
            OnPropertyChanged(nameof(ImageCount));
        }

        /// <summary>
        /// 移除指定图像
        /// </summary>
        public void RemoveImage(ImageInfo image)
        {
            ImageCollection.Remove(image);
            if (CurrentImageIndex >= ImageCollection.Count)
            {
                CurrentImageIndex = ImageCollection.Count - 1;
            }
            OnPropertyChanged(nameof(ImageCount));
        }

        /// <summary>
        /// 重置状态（用于节点销毁或切换时）
        /// </summary>
        public void Reset()
        {
            ClearImages();
            ImageRunMode = ImageRunMode.运行全部;
            AutoSwitchEnabled = false;
        }

        /// <summary>
        /// 准备切换到此节点的图像数据进行显示（延迟渲染优化）
        /// 切换节点时，先隐藏所有缩略图显示占位符，然后由 ImagePreviewControl 异步加载可视区域
        /// </summary>
        /// <returns>返回图像数量，用于后续异步加载</returns>
        public int PrepareForDisplay()
        {
            int count = ImageCollection.Count;
            
            // 清除所有缩略图显示（保留文件路径等数据）
            // 这样切换节点时 UI 会立即显示占位符，而不是尝试渲染已有缩略图
            foreach (var image in ImageCollection)
            {
                image.Thumbnail = null;
            }
            
            return count;
        }

        /// <summary>
        /// 获取所有图像的文件路径（用于异步加载）
        /// </summary>
        public string[] GetFilePaths()
        {
            return ImageCollection.Select(img => img.FilePath).ToArray();
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
