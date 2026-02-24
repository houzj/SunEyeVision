using System;
using System.Diagnostics;

namespace SunEyeVision.Plugin.Abstractions.Core
{
    /// <summary>
    /// 图像处理器抽象基类
    /// </summary>
    /// <remarks>
    /// 提供图像处理器的通用实现，包括：
    /// - 执行时间测量
    /// - 错误处理
    /// - 参数验证
    /// - Process() 和 Execute() 方法的统一实现
    /// 
    /// 子类只需实现 ProcessImage() 方法即可获得完整的处理器功能。
    /// </remarks>
    public abstract class ImageProcessorBase : IImageProcessor
    {
        /// <summary>
        /// 处理器名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 处理器描述
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 处理图像（简单模式）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <returns>处理后的图像</returns>
        public virtual object? Process(object image)
        {
            var result = Execute(image, new AlgorithmParameters());
            return result.Success ? result.ResultImage : null;
        }

        /// <summary>
        /// 执行处理（完整模式）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <returns>结构化的算法结果</returns>
        public AlgorithmResult Execute(object image, AlgorithmParameters parameters)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 参数验证
                var validationResult = ValidateParameters(parameters);
                if (!validationResult.IsValid)
                {
                    return AlgorithmResult.CreateError(
                        $"参数验证失败: {string.Join(", ", validationResult.Errors)}",
                        stopwatch.ElapsedMilliseconds);
                }

                // 执行处理
                var result = ProcessImage(image, parameters);
                
                stopwatch.Stop();
                
                // 构建成功结果
                return new AlgorithmResult
                {
                    AlgorithmName = Name,
                    Success = true,
                    ResultImage = result.OutputImage,
                    Data = result.OutputData,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return AlgorithmResult.CreateError(
                    $"处理失败: {ex.Message}",
                    stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// 处理图像（子类实现）
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <returns>处理结果</returns>
        protected abstract ImageProcessResult ProcessImage(object image, AlgorithmParameters parameters);

        /// <summary>
        /// 验证参数（子类可重写）
        /// </summary>
        /// <param name="parameters">待验证的参数</param>
        /// <returns>验证结果</returns>
        protected virtual ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            return ValidationResult.Success();
        }

        /// <summary>
        /// 获取参数值，如果不存在则返回默认值
        /// </summary>
        protected T GetParameter<T>(AlgorithmParameters parameters, string key, T defaultValue)
        {
            var value = parameters.Get<T>(key);
            return value == null ? defaultValue : value;
        }
    }

    /// <summary>
    /// 图像处理结果
    /// </summary>
    public class ImageProcessResult
    {
        /// <summary>
        /// 输出图像
        /// </summary>
        public object? OutputImage { get; set; }

        /// <summary>
        /// 输出数据（附加结果，如测量值、计数等）
        /// </summary>
        public object? OutputData { get; set; }

        /// <summary>
        /// 创建仅包含图像的结果
        /// </summary>
        public static ImageProcessResult FromImage(object? image)
        {
            return new ImageProcessResult { OutputImage = image };
        }

        /// <summary>
        /// 创建包含图像和数据的结果
        /// </summary>
        public static ImageProcessResult FromImageAndData(object? image, object? data)
        {
            return new ImageProcessResult { OutputImage = image, OutputData = data };
        }

        /// <summary>
        /// 创建仅包含数据的结果
        /// </summary>
        public static ImageProcessResult FromData(object? data)
        {
            return new ImageProcessResult { OutputData = data };
        }
    }
}
