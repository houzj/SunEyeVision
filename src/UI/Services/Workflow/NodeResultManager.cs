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
using SunEyeVision.UI.Services.Performance;
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

        // 图像转换器池（用于优化Mat到BitmapSource的转换）
        private readonly ImageConverterPool _imageConverterPool;

        public NodeResultManager(ViewModels.MainWindowViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _renderer = new VisualElementRenderer();
            _imageConverterPool = new ImageConverterPool(maxCacheSize: 10);
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

            // ★ 诊断日志：节点选中状态
            _viewModel.AddLog($"[UpdateNodeResult] 节点: {node.Name}, IsSelected={node.IsSelected}");

            // 1. 缓存结果到节点
            node.LastResult = result;

            // 2. 更新图像数据（如果结果包含图像）
            UpdateNodeImage(node, result);

            // 3. 如果当前节点被选中，立即刷新UI
            if (node.IsSelected)
            {
                _viewModel.AddLog($"[UpdateNodeResult] ✓ 节点已选中，准备刷新显示");
                RefreshResultDisplay(node, result);
            }
            else
            {
                _viewModel.AddLog($"[UpdateNodeResult] ⚠️ 节点未选中，跳过刷新");
            }
        }

        /// <summary>
        /// 刷新结果显示（调用时节点已选中）
        /// </summary>
        /// <remarks>
        /// ★ 防闪烁优化：先准备新数据，再更新UI，避免空白间隙
        /// </remarks>
        public void RefreshResultDisplay(WorkflowNode node, ToolResults result)
        {
            if (result == null)
            {
                ClearResultDisplay();
                return;
            }

            _viewModel.AddLog($"[RefreshResultDisplay] 开始刷新显示: {node.Name}");

            // ★ Step 1: 先准备所有新数据（不触发UI更新）
            var resultItems = result.GetResultItems();
            
            // 获取输出图像并转换为 BitmapSource
            var outputImage = GetOutputImage(result);
            BitmapSource? newProcessedImage = null;
            bool hasNewImage = false;
            
            if (outputImage != null)
            {
                try
                {
                    newProcessedImage = outputImage.ToBitmapSource();
                    hasNewImage = true;
                    _viewModel.AddLog($"[RefreshResultDisplay] ✓ 图像转换成功，尺寸: {newProcessedImage.PixelWidth}x{newProcessedImage.PixelHeight}");
                }
                catch (Exception ex)
                {
                    _viewModel.AddLog($"[RefreshResultDisplay] ❌ 图像转换失败: {ex.Message}");
                }
            }
            else
            {
                _viewModel.AddLog($"[RefreshResultDisplay] ⚠️ 结果中无图像输出");
            }

            // ★ Step 2: 批量更新UI（减少刷新次数）
            _viewModel.UpdateCalculationResults(resultItems);
            
            // ★ Step 3: 设置新图像并显式更新 DisplayImage
            if (hasNewImage && newProcessedImage != null)
            {
                // 设置处理后图像
                _viewModel.ProcessedImage = newProcessedImage;
                
                // ★ 关键修复：显式设置 DisplayImage，不依赖 SelectedImageType setter
                _viewModel.DisplayImage = newProcessedImage;
                _viewModel.AddLog($"[RefreshResultDisplay] ✓ 已设置 ProcessedImage 和 DisplayImage");
                
                // 确保显示类型为 Processed（如果当前不是）
                var processedType = _viewModel.ImageDisplayTypes
                    .FirstOrDefault(t => t.Type == ViewModels.ImageDisplayType.Processed);
                if (processedType != null && _viewModel.SelectedImageType?.Type != ViewModels.ImageDisplayType.Processed)
                {
                    _viewModel.SelectedImageType = processedType;
                    _viewModel.AddLog($"[RefreshResultDisplay] ✓ 已切换到 Processed 类型");
                }
            }
            
            // ★ Step 4: 更新可视化元素
            UpdateVisualElements(result);
            
            _viewModel.AddLog($"[RefreshResultDisplay] ✓ 刷新完成");
        }

        /// <summary>
        /// 清空结果显示
        /// </summary>
        /// <remarks>
        /// ★ 防闪烁优化：不再主动清空图像，由调用方控制
        /// </remarks>
        public void ClearResultDisplay()
        {
            _viewModel.CalculationResults.Clear();
            ClearVisualElements();
            
            // ★ 优化：不再清空图像，保留上一个图像直到新图像到来
            // 这样可以避免节点切换时的闪烁
            // 图像清空由 UpdateDisplayImageSources 或特定逻辑控制
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

        /// <summary>
        /// 从缓存更新节点图像（优化版本）
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <param name="cachedResult">缓存的执行结果</param>
        public void UpdateNodeImageFromCache(WorkflowNode node, CachedExecutionResult cachedResult)
        {
            if (node == null || cachedResult == null || cachedResult.ProcessedImage == null)
                return;

            try
            {
                // 直接使用缓存的BitmapSource，无需重新转换
                var bitmapSource = cachedResult.ProcessedImage;

                // 更新 OutputCache
                var outputCache = node.EnsureOutputCache();
                var outputImageInfo = new OutputImageInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Cached_{DateTime.Now:HHmmss}",
                    SourceFilePath = "",
                    Image = bitmapSource,
                    ImageSourceId = cachedResult.ImageSourceId,
                    Timestamp = DateTime.Now
                };

                outputCache.AddOutputImage(outputImageInfo);

                System.Diagnostics.Debug.WriteLine(
                    $"[NodeResultManager] ★ 缓存更新: 节点 {node.Name}, " +
                    $"执行时间: {cachedResult.ExecutionTimeMs}ms, " +
                    $"缓存命中");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] 缓存更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取节点的输出Mat（用于缓存）
        /// </summary>
        /// <param name="node">工作流节点</param>
        /// <returns>输出Mat，如果无输出返回null</returns>
        public Mat? GetNodeOutputMat(WorkflowNode node)
        {
            if (node?.LastResult == null)
                return null;

            return GetOutputImage(node.LastResult);
        }

        /// <summary>
        /// 快速转换Mat到BitmapSource（使用图像转换池优化）
        /// </summary>
        /// <param name="mat">OpenCV Mat</param>
        /// <returns>BitmapSource</returns>
        public BitmapSource? ConvertMatToBitmapSource(Mat? mat)
        {
            if (mat == null || mat.Empty())
                return null;

            try
            {
                return _imageConverterPool.ConvertToBitmapSource(mat);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NodeResultManager] 图像转换失败: {ex.Message}");
                return null;
            }
        }
    }
}
