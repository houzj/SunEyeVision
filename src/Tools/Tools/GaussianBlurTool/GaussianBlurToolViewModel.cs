using SunEyeVision.Plugin.Abstractions;
using SunEyeVision.Plugin.Abstractions;

using SunEyeVision.Plugin.Infrastructure;
using SunEyeVision.Plugin.Infrastructure;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Controls;
using System;
using SunEyeVision.Plugin.Infrastructure.Base;
using SunEyeVision.Plugin.Infrastructure.UI.Windows;
using SunEyeVision.Tools.GaussianBlurTool.DTOs;



namespace SunEyeVision.Tools.GaussianBlurTool
{
    /// <summary>
    /// 高斯模糊工具调试ViewModel - 完整MVVM架构实现
    /// 使用参数项管理、命令驱动、异步执行
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
                    UpdateParameterItem("KernelSize", value);
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
                    UpdateParameterItem("Sigma", value);
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
                    UpdateParameterItem("BorderType", value);
                    SetParamValue("BorderType", value);
                }
            }
        }

        /// <summary>
        /// 初始化工具
        /// </summary>
        public override void Initialize(string toolId, IToolPlugin? toolPlugin, ToolMetadata? toolMetadata)
        {
            ToolId = toolId;
            ToolName = toolMetadata?.DisplayName ?? "高斯模糊";
            ToolStatus = "就绪";
            StatusMessage = "准备就绪";

            // 初始化参数项
            InitializeParameterItems();

            // 从ToolMetadata加载参数
            LoadParameters(toolMetadata);

            // 确保 KernelSize 是奇数
            if (_kernelSize % 2 == 0)
                KernelSize = _kernelSize + 1;

            // 设置验证规则
            SetupValidationRules();
        }

        /// <summary>
        /// 初始化参数项（使用ParameterControlFactory动态生成控件）
        /// </summary>
        private void InitializeParameterItems()
        {
            ParameterItems.Clear();

            // 核大小参数
            var kernelSizeItem = new ParameterItem("KernelSize", typeof(int), 5)
            {
                DisplayName = "核大小",
                Description = "高斯核的大小，必须是奇数，范围3-99",
                MinValue = 3,
                MaxValue = 99
            };
            AddParameterItem(kernelSizeItem);

            // Sigma参数
            var sigmaItem = new ParameterItem("Sigma", typeof(double), 1.5)
            {
                DisplayName = "标准差",
                Description = "高斯函数的标准差，控制模糊程度",
                MinValue = 0.1,
                MaxValue = 10.0
            };
            AddParameterItem(sigmaItem);

            // 边界类型参数
            var borderTypeOptions = new[]
            {
                new ParameterOption("Reflect", "Reflect", "边缘像素镜像"),
                new ParameterOption("Constant", "Constant", "使用常数值填充"),
                new ParameterOption("Replicate", "Replicate", "边缘像素复制"),
                new ParameterOption("Default", "Default", "默认模式")
            };

            var borderTypeItem = new ParameterItem("BorderType", typeof(string), "Reflect")
            {
                DisplayName = "边界处理",
                Description = "图像边缘的处理方式",
                Options = new System.Collections.ObjectModel.ObservableCollection<ParameterOption>(borderTypeOptions)
            };
            AddParameterItem(borderTypeItem);
        }

        /// <summary>
        /// 设置验证规则
        /// </summary>
        private void SetupValidationRules()
        {
            // TODO: ParameterValidator目前是占位实现，暂不支持验证规则
            // 核大小验证
            // Validator.AddRules("KernelSize",
            //     new RequiredRule(),
            //     new RangeRule(3, 99),
            //     new CustomRule(v =>
            //     {
            //         if (v is int intValue)
            //             return intValue % 2 != 0;
            //         return false;
            //     }, "核大小必须是奇数"));

            // Sigma验证
            // Validator.AddRules("Sigma",
            //     new RequiredRule(),
            //     new RangeRule(0.1, 10.0));

            // 边界类型验证
            // Validator.AddRule("BorderType",
            //     new RequiredRule());
        }

        /// <summary>
        /// 更新参数项的值
        /// </summary>
        private void UpdateParameterItem(string name, object? value)
        {
            var item = GetParameterItem(name);
            if (item != null)
            {
                item.Value = value;
            }
        }

        /// <summary>
        /// 从参数项构建参数字典
        /// </summary>
        protected override System.Collections.Generic.Dictionary<string, object?> BuildParameterDictionary()
        {
            return new System.Collections.Generic.Dictionary<string, object?>
            {
                { "KernelSize", KernelSize },
                { "Sigma", Sigma },
                { "BorderType", BorderType }
            };
        }

        /// <summary>
        /// 核心执行逻辑（异步）
        /// </summary>
        protected override async Task ExecuteToolCoreAsync(CancellationToken cancellationToken)
        {
            var random = new Random();

            // 模拟处理步骤
            ReportProgress(10, "初始化图像处理...");
            await Task.Delay(50, cancellationToken);

            ReportProgress(30, "应用高斯模糊...");
            await Task.Delay(100, cancellationToken);

            ReportProgress(60, "计算卷积...");
            await Task.Delay(100, cancellationToken);

            ReportProgress(80, "处理边界...");
            await Task.Delay(50, cancellationToken);

            ReportProgress(100, "处理完成");

            // 模拟FPS计算
            FPS = $"{random.Next(20, 60)}.{random.Next(0, 9)}";
        }

        /// <summary>
        /// 运行工具（同步方法，保留向后兼容）
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
                StatusMessage = $"✅ 高斯模糊处理完成 - 耗时: {ExecutionTime}";
                ToolStatus = "就绪";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ 执行失败: {ex.Message}";
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

        /// <summary>
        /// 转换为DTO
        /// </summary>
        public GaussianBlurToolDTO ToDTO()
        {
            return new GaussianBlurToolDTO
            {
                KernelSize = _kernelSize,
                Sigma = _sigma,
                BorderType = _borderType
            };
        }

        /// <summary>
        /// 从DTO加载
        /// </summary>
        public void FromDTO(GaussianBlurToolDTO dto)
        {
            KernelSize = dto.KernelSize;
            Sigma = dto.Sigma;
            BorderType = dto.BorderType;
        }
    }
}
