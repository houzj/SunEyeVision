using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.ROI;

// 明确Window类型，避免与OpenCvSharp.Window冲突
using Window = System.Windows.Window;

namespace SunEyeVision.Tool.ROIEditor.Views
{
    /// <summary>
    /// ROI编辑器调试窗口
    /// </summary>
    public partial class ROIEditorToolDebugWindow : Window
    {
        private ROIEditorToolViewModel? _viewModel;

        /// <summary>
        /// 关联的ViewModel
        /// </summary>
        public ROIEditorToolViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                _viewModel = value;
                DataContext = value;
                OnViewModelSet();
            }
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName
        {
            get => txtTitle.Text;
            set => txtTitle.Text = value;
        }

        public ROIEditorToolDebugWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ViewModel = new ROIEditorToolViewModel();
            ViewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = toolMetadata?.DisplayName ?? "ROI编辑器";
        }

        private void OnViewModelSet()
        {
            if (_viewModel == null || RoiEditor == null) return;

            // 同步ROI数据
            _viewModel.ROIs.CollectionChanged += OnROIsCollectionChanged;

            // 如果有图像，加载到编辑器
            if (_viewModel.CurrentImage != null)
            {
                LoadImageToEditor(_viewModel.CurrentImage);
            }

            // 订阅ROI编辑器事件
            RoiEditor.ROIChanged += OnROIChanged;
        }

        private void OnROIsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (RoiEditor == null) return;

            // 同步到编辑器
            RoiEditor.LoadROIs(_viewModel!.ROIs);
            UpdateROICount();
        }

        private void OnROIChanged(object? sender, ROIChangedEventArgs e)
        {
            if (_viewModel == null) return;

            switch (e.ChangeType)
            {
                case ROIChangeType.Added:
                    if (e.ROI != null)
                    {
                        _viewModel.AddROI(e.ROI);
                    }
                    break;

                case ROIChangeType.Removed:
                    if (e.ROI != null)
                    {
                        _viewModel.RemoveROI(e.ROI.ID);
                    }
                    break;

                case ROIChangeType.Cleared:
                    _viewModel.ClearROIs();
                    break;
            }

            UpdateROICount();
        }

        private void UpdateROICount()
        {
            if (_viewModel != null)
            {
                ROICountText.Text = $"ROI数量: {_viewModel.ROIs.Count}";
                ROIListBox.ItemsSource = _viewModel.ROIs.Select((r, i) => $"ROI {i + 1}: {r.Type}").ToList();
            }
        }

        private void LoadImageToEditor(Mat image)
        {
            try
            {
                var bitmapSource = WriteableBitmapConverter.ToWriteableBitmap(image);
                RoiEditor?.LoadImage(bitmapSource);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图像失败: {ex.Message}");
            }
        }

        #region 事件处理

        private void EditModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (RoiEditor != null)
            {
                RoiEditor.CurrentMode = ROIMode.Edit;
            }
            if (_viewModel != null)
            {
                _viewModel.Mode = "Edit";
            }
        }

        private void InheritModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (RoiEditor != null)
            {
                RoiEditor.CurrentMode = ROIMode.Inherit;
            }
            if (_viewModel != null)
            {
                _viewModel.Mode = "Inherit";
            }
        }

        private void ShowGridCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ShowGrid = ShowGridCheck.IsChecked ?? false;
            }
        }

        private void EnableSnapCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EnableSnap = EnableSnapCheck.IsChecked ?? true;
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.RunTool();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ResetParameters();
            RoiEditor?.DeselectAll();
        }

        #endregion

        #region 独立运行 - 图像加载功能

        /// <summary>
        /// 打开图像文件对话框
        /// </summary>
        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|所有文件|*.*",
                Title = "选择图像文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadLocalImage(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// 从本地文件加载图像
        /// </summary>
        /// <param name="filePath">图像文件路径</param>
        public void LoadLocalImage(string filePath)
        {
            try
            {
                using var image = Cv2.ImRead(filePath, ImreadModes.Color);
                if (!image.Empty())
                {
                    // 更新ViewModel
                    if (_viewModel != null)
                    {
                        _viewModel.CurrentImage = image.Clone();
                    }

                    // 加载到编辑器
                    LoadImageToEditor(image);

                    // 显示文件名
                    CurrentImageFileName.Text = Path.GetFileName(filePath);
                }
                else
                {
                    MessageBox.Show($"无法加载图像: {filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
