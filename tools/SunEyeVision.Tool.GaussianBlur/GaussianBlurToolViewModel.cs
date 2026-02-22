using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions.ViewModels;
using System;

namespace SunEyeVision.Tool.GaussianBlur
{
    /// <summary>
    /// 高斯模糊工具调试ViewModel
    /// </summary>
    public class GaussianBlurToolViewModel : AutoToolDebugViewModelBase
    {
        private int _kernelSize = 5;
        private double _sigma = 1.5;
        private string _borderType = "Reflect";

        /// <summary>
        /// 核大小（必须为奇数）
        /// </summary>
        public int KernelSize
        {
            get => _kernelSize;
            set
            {
                if (value % 2 == 0)
                    value = value + 1; // 确保为奇数
                if (SetProperty(ref _kernelSize, value))
                {
                    SetParamValue("KernelSize", value);
                }
            }
        }

        /// <summary>
        /// 标准差（Sigma）
        /// </summary>
        public double Sigma
        {
            get => _sigma;
            set
            {
                if (value < 0.1)
                    value = 0.1;
                if (SetProperty(ref _sigma, value))
                {
                    SetParamValue("Sigma", value);
                }
            }
        }

        /// <summary>
        /// 边界类型
        /// </summary>
        public string BorderType
        {
            get => _borderType;
            set
            {
                if (SetProperty(ref _borderType, value))
                {
                    SetParamValue("BorderType", value);
                }
            }
        }

        public string[] BorderTypes { get; } = { "Reflect", "Constant", "Replicate", "Default" };

        /// <summary>
        /// 初始化工具
        /// </summary>
        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "高斯模糊";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";

            // 从ToolMetadata加载参数
            LoadParameters(toolMetadata);

            // 确保 KernelSize 是奇数
            if (_kernelSize % 2 == 0)
                KernelSize = _kernelSize + 1;
        }

        /// <summary>
        /// 运行工具（同步方法）
        /// </summary>
        public override void RunTool()
        {
            ToolStatus = "运行中";
            StatusMessage = $"正在执行高斯模糊（核大小={KernelSize}, Sigma={Sigma:F2}）...";

            try
            {
                // 模拟执行时间
                var random = new Random();
                System.Threading.Thread.Sleep(random.Next(100, 300));

                // 模拟处理结果
                var executionTime = random.Next(50, 150);
                ExecutionTime = $"{executionTime} ms";
                StatusMessage = $"高斯模糊处理完成 - 耗时: {ExecutionTime}";
                ToolStatus = "就绪";
            }
            catch (Exception ex)
            {
                StatusMessage = $"执行失败: {ex.Message}";
                ToolStatus = "错误";
                throw;
            }
        }

        /// <summary>
        /// 重置参数
        /// </summary>
        public override void ResetParameters()
        {
            KernelSize = 5;
            Sigma = 1.5;
            BorderType = "Reflect";
            base.ResetParameters();
        }
    }
}
