using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SunEyeVision.Plugin.SDK.Core;
using SunEyeVision.Plugin.SDK.Metadata;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Logic;
using SunEyeVision.Plugin.SDK.UI.Controls.Region.Models;

// 明确Window类型，避免与OpenCvSharp.Window冲突
using Window = System.Windows.Window;

namespace SunEyeVision.Tool.RegionEditor.Views
{
    /// <summary>
    /// 区域编辑器调试窗口
    /// </summary>
    public partial class RegionEditorToolDebugWindow : Window
    {
        private RegionEditorToolViewModel? _viewModel;

        /// <summary>
        /// 关联的ViewModel
        /// </summary>
        public RegionEditorToolViewModel? ViewModel
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

        public RegionEditorToolDebugWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ViewModel = new RegionEditorToolViewModel();
            ViewModel.Initialize(toolId, toolPlugin, toolMetadata);
            NodeName = toolMetadata?.DisplayName ?? "区域编辑器";
        }

        private void OnViewModelSet()
        {
            if (_viewModel == null || RegionEditor == null) return;

            // 同步区域数据
            _viewModel.Regions.CollectionChanged += OnRegionsCollectionChanged;

            // 如果有图像，加载到编辑器
            if (_viewModel.CurrentImage != null)
            {
                LoadImageToEditor(_viewModel.CurrentImage);
            }
        }

        private void OnRegionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (RegionEditor == null || _viewModel == null) return;

            // 同步到编辑器
            RegionEditor.SetRegions(_viewModel.Regions);
            UpdateRegionCount();
        }

        private void UpdateRegionCount()
        {
            if (_viewModel != null)
            {
                RegionCountText.Text = $"区域数量: {_viewModel.Regions.Count}";
                RegionListBox.ItemsSource = _viewModel.Regions.Select((r, i) => 
                    $"区域 {i + 1}: {r.Name} ({r.GetShapeType()})").ToList();
            }
        }

        private void UpdateResolvedRegions()
        {
            if (_viewModel != null)
            {
                var resolved = _viewModel.ResolveAllRegions();
                ResolvedListBox.ItemsSource = resolved.Select((r, i) =>
                    r.IsValid ? $"区域 {i + 1}: {r.ShapeType} - 有效" : $"区域 {i + 1}: 无效 - {r.ErrorMessage}"
                ).ToList();
            }
        }

        private void LoadImageToEditor(Mat image)
        {
            try
            {
                var bitmapSource = WriteableBitmapConverter.ToWriteableBitmap(image);
                RegionEditor?.LoadImage(bitmapSource);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图像失败: {ex.Message}");
            }
        }

        #region 事件处理

        private void RealtimePreviewCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.EnableRealtimePreview = RealtimePreviewCheck.IsChecked ?? true;
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && ColorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                var colorHex = item.Tag.ToString();
                if (uint.TryParse(colorHex?.Replace("#", ""), System.Globalization.NumberStyles.HexNumber, null, out var color))
                {
                    _viewModel.DefaultDisplayColor = color;
                }
            }
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddDrawingRegion($"矩形_{_viewModel.Regions.Count + 1}", ShapeType.Rectangle, 150, 150, 100, 80);
            UpdateRegionCount();
        }

        private void AddCircle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddDrawingRegion($"圆形_{_viewModel.Regions.Count + 1}", ShapeType.Circle, 200, 200, radius: 50);
            UpdateRegionCount();
        }

        private void AddRotatedRect_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.AddDrawingRegion($"旋转矩形_{_viewModel.Regions.Count + 1}", ShapeType.RotatedRectangle, 250, 200, 120, 60, 30);
            UpdateRegionCount();
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            var region = RegionData.CreateDrawingRegion($"直线_{_viewModel?.Regions.Count + 1}", ShapeType.Line);
            if (region.Definition is ShapeDefinition shapeDef)
            {
                shapeDef.StartX = 100;
                shapeDef.StartY = 100;
                shapeDef.EndX = 300;
                shapeDef.EndY = 200;
            }
            _viewModel?.Regions.Add(region);
            UpdateRegionCount();
        }

        private void AddSubscribed_Click(object sender, RoutedEventArgs e)
        {
            var nodeId = NodeIdTextBox.Text.Trim();
            var outputName = OutputNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(outputName))
            {
                MessageBox.Show("请输入节点ID和输出名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _viewModel?.AddSubscribedRegion($"订阅区域_{_viewModel.Regions.Count + 1}", nodeId, outputName);
            UpdateRegionCount();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.RunTool();
            UpdateResolvedRegions();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ResetParameters();
            _viewModel?.ClearRegions();
            UpdateRegionCount();
            ResolvedListBox.ItemsSource = null;
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
