using System;
using System.Linq;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.UI.Extensions;
using SunEyeVision.UI.Models;

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
    /// 
    /// 使用示例:
    /// <code>
    /// var manager = new NodeResultManager(viewModel);
    /// manager.UpdateNodeResult(node, results);
    /// </code>
    /// </remarks>
    public class NodeResultManager
    {
        private readonly ViewModels.MainWindowViewModel _viewModel;

        public NodeResultManager(ViewModels.MainWindowViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
        }

        /// <summary>
        /// 清空结果显示
        /// </summary>
        public void ClearResultDisplay()
        {
            _viewModel.CalculationResults.Clear();
        }

        /// <summary>
        /// 更新节点图像数据
        /// </summary>
        private void UpdateNodeImage(WorkflowNode node, ToolResults result)
        {
            // 尝试从结果中获取输出图像
            var outputImage = GetOutputImage(result);
            if (outputImage != null && node.ImageData != null)
            {
                // 更新节点的图像数据
                // 注意：图像载入节点已有自己的图像管理逻辑
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
