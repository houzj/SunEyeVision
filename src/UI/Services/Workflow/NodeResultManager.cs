using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.UI.Extensions;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Images;
using SunEyeVision.UI.Services.Visualization;
using SunEyeVision.UI.Views.Controls.Panels;

// 别名解决命名冲突
using IOPath = System.IO.Path;

namespace SunEyeVision.UI.Services.Workflow
{
    /// <summary>
    /// 节点结果管理器 - 负责更新节点结果并刷新UI显示
    /// </summary>
    /// <remarks>
    /// 核心功能:
    /// 1. 缓存工具执行结果到节点
    /// 2. 更新图像数据
    /// 3. 刷新结果面板显示
    /// 4. 显示可视化元素（检测框、轮廓等）
    /// 
    /// 使用示例:
    /// <code>
    /// var manager = new NodeResultManager(viewModel);
    /// manager.SetOverlayCanvas(imageControl.OverlayCanvas);
    /// manager.UpdateNodeResult(node, results);
    /// </code>
    /// </remarks>
    public class NodeResultManager
    {
        private readonly ViewModels.MainWindowViewModel _viewModel;
        private readonly VisualElementRenderer _renderer;
        
        // 当前显示可视化元素的 Canvas 引用
        private System.Windows.Controls.Canvas? _overlayCanvas;

        public NodeResultManager(ViewModels.MainWindowViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _renderer = new VisualElementRenderer();
        }
        
        /// <summary>
        /// 设置叠加层 Canvas（由 View 层调用）
        /// </summary>
        /// <param name="canvas">OverlayCanvas 引用</param>
        public void SetOverlayCanvas(System.Windows.Controls.Canvas canvas)
        {
            _overlayCanvas = canvas;
            System.Diagnostics.Debug.WriteLine($"[NodeResultManager] OverlayCanvas 已设置");
        }

        /// <summary>
        /// 更新节点结果并刷新UI
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="result">执行结果</param>
        public void UpdateNodeResult(WorkflowNode node, ToolResults result)
        {
            if (node == null || result == null)
                return;

            // 1. 缓存结果到节点
            node.LastResult = result;

            // 2. 更新图像数据（如果结果包含图像）
            UpdateNodeImage(node, result);

            // 3. 如果当前节点被选中，立即刷新UI
            if (node.IsSelected)
            {
                RefreshResultDisplay(node, result);
            }
        }

        /// <summary>
        /// 刷新结果显示（调用时节点已选中）
        /// </summary>
        public void RefreshResultDisplay(WorkflowNode node, ToolResults result)
        {
            if (result == null)
            {
                ClearResultDisplay();
                return;
            }

            // 更新模块结果
            var resultItems = result.GetResultItems();
            _viewModel.UpdateCalculationResults(resultItems);

            // 更新图像结果（如果结果包含图像）
            UpdateDisplayImage(result);
            
            // 更新可视化元素
            UpdateVisualElements(result);
        }

        /// <summary>
        /// 清空结果显示
        /// </summary>
        public void ClearResultDisplay()
        {
            _viewModel.CalculationResults.Clear();
            ClearVisualElements();
            
            // 清空所有图像显示，避免显示已删除节点的旧图像
            _viewModel.DisplayImage = null;
            _viewModel.OriginalImage = null;
            _viewModel.ProcessedImage = null;
            _viewModel.ResultImage = null;
        }
        
        /// <summary>
        /// 更新可视化元素显示
        /// </summary>
        /// <param name="result">工具执行结果</param>
        private void UpdateVisualElements(ToolResults result)
        {
            if (_overlayCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("[NodeResultManager] OverlayCanvas 未设置，无法显示可视化元素");
                return;
            }
            
            // 清除旧元素
            ClearVisualElements();
            
            // 获取可视化元素
            var visualElements = result.GetVisualElements();
            if (visualElements == null)
            {
                System.Diagnostics.Debug.WriteLine("[NodeResultManager] 结果不包含可视化元素");
                return;
            }
            
            var elementList = visualElements.ToList();
            if (elementList.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[NodeResultManager] 可视化元素列表为空");
                return;
            }
            
            // 渲染并添加到 Canvas
            var uiElements = _renderer.Render(elementList);
            foreach (var uiElement in uiElements)
            {
                _overlayCanvas.Children.Add(uiElement);
            }
            
            System.Diagnostics.Debug.WriteLine($"[NodeResultManager] 显示 {uiElements.Count} 个可视化元素");
        }
        
        /// <summary>
        /// 清除可视化元素
        /// </summary>
        private void ClearVisualElements()
        {
            _overlayCanvas?.Children.Clear();
        }

        /// <summary>
        /// 更新节点图像数据
        /// </summary>
        /// <remarks>
        /// 重构说明：
        /// - 新架构：更新 OutputCache（输出缓存），不修改 InputSource（输入源）
        /// - ImageSource集成：创建MemoryImageSource并注册到ImageSourceManager
        /// - 向后兼容：同时更新 ImageData（已过时）
        /// - 设计原则：输入和输出分离，执行结果不应修改用户选择的输入数据
        /// </remarks>
        public bool UpdateNodeImage(WorkflowNode node, ToolResults result)
        {
            // 尝试从结果中获取输出图像
            var outputImage = GetOutputImage(result);
            if (outputImage == null || outputImage.Empty())
            {
                // ★ 关键日志：无图像输出
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] 节点 {node.Name} 无图像输出");
                return false;
            }

            // 获取文件路径（如果有）
            var filePathProp = result.GetType().GetProperty("FilePath");
            var filePath = filePathProp?.GetValue(result) as string ?? "";

            try
            {
                // 转换为 BitmapSource
                var bitmapSource = outputImage.ToBitmapSource();

                // ========== 新架构：使用ImageSourceManager ==========
                // 创建唯一ID
                var imageSourceId = $"node_{node.Id}_output_{Guid.NewGuid():N}";
                
                // 通过ImageSourceManager创建内存图像源
                var imageSource = ImageSourceManager.Instance.CreateFromMemory(
                    outputImage.Clone(), // 克隆Mat，避免所有权问题
                    imageSourceId
                );
                
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] ★ ImageSource已创建: {imageSourceId}");

                // ========== 新架构：更新 OutputCache ==========
                var outputCache = node.EnsureOutputCache();
                
                // 创建输出图像信息
                var outputImageInfo = new OutputImageInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = !string.IsNullOrEmpty(filePath) ? IOPath.GetFileName(filePath) : $"Output_{DateTime.Now:HHmmss}",
                    SourceFilePath = filePath,
                    Image = bitmapSource,
                    ImageSourceId = imageSourceId, // ★ 关联ImageSource
                    Timestamp = DateTime.Now
                };
                
                outputCache.AddOutputImage(outputImageInfo);

                // ★ 关键日志：新架构更新成功
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] ★ 新架构: 节点 {node.Name} OutputCache 已更新, 图像数={outputCache.Count}, ImageSourceId={imageSourceId}");

                // ========== 向后兼容：更新 ImageData（已过时）==========
                // 注意：这里不再清空 ImageData，而是追加图像
                // 这保持了与其他节点类型一致的行为
#pragma warning disable CS0618 // 禁用过时警告
                node.ImageData ??= new NodeImageData(node.Id);

                var imageInfo = new ImageInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = !string.IsNullOrEmpty(filePath) ? IOPath.GetFileName(filePath) : $"Image_{DateTime.Now:HHmmss}",
                    FilePath = filePath,
                    Thumbnail = bitmapSource,
                    FullImage = bitmapSource,
                    AddedTime = DateTime.Now
                };

                // 向后兼容：追加图像而不是清空
                // 这样不会影响用户选择的输入源
                node.ImageData.AddImage(imageInfo);
                node.ImageData.CurrentImageIndex = node.ImageData.ImageCount - 1;
#pragma warning restore CS0618

                // ★ 关键日志：图像更新成功
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] ★ 节点 {node.Name} 图像已更新, 图像数={node.ImageData.ImageCount}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] 更新节点图像失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新显示图像
        /// </summary>
        private void UpdateDisplayImage(ToolResults result)
        {
            var outputImage = GetOutputImage(result);
            if (outputImage != null)
            {
                try
                {
                    _viewModel.ProcessedImage = outputImage.ToBitmapSource();
                    // 自动切换到处理图
                    var processedType = _viewModel.ImageDisplayTypes
                        .FirstOrDefault(t => t.Type == ViewModels.ImageDisplayType.Processed);
                    if (processedType != null)
                    {
                        _viewModel.SelectedImageType = processedType;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[NodeResultManager] 图像转换失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 从结果中获取输出图像
        /// </summary>
        private Mat? GetOutputImage(ToolResults result)
        {
            // 检查结果是否包含图像数据
            if (result == null)
                return null;

            // 尝试通过反射获取 OutputImage 属性
            var outputImageProp = result.GetType().GetProperty("OutputImage");
            if (outputImageProp != null)
            {
                var value = outputImageProp.GetValue(result);
                if (value is Mat mat)
                {
                    return mat;
                }
            }

            return null;
        }
    }
}
