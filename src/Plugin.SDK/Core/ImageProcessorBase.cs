using System;
using System.Collections.Generic;
using OpenCvSharp;
using SunEyeVision.Plugin.SDK.Execution.Results;
using SunEyeVision.Plugin.SDK.Validation;

namespace SunEyeVision.Plugin.SDK.Core
{
    /// <summary>
    /// 算法参数容器 - 兼容层
    /// </summary>
    /// <remarks>
    /// 提供基于字典的参数存储，用于简化工具开发。
    /// 推荐使用强类型的 ToolParameters 派生类以获得更好的类型安全。
    /// </remarks>
    public class AlgorithmParameters
    {
        private readonly Dictionary<string, object?> _values = new();

        /// <summary>
        /// 参数字典
        /// </summary>
        public Dictionary<string, object?> Values => _values;

        /// <summary>
        /// 获取参数值
        /// </summary>
        public T? Get<T>(string key)
        {
            if (_values.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                try
                {
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }

        /// <summary>
        /// 设置参数值
        /// </summary>
        public void Set(string key, object? value)
        {
            _values[key] = value;
        }

        /// <summary>
        /// 检查参数是否存在
        /// </summary>
        public bool HasParameter(string key)
        {
            return _values.ContainsKey(key);
        }

        /// <summary>
        /// 转换为字典
        /// </summary>
        public Dictionary<string, object?> ToDictionary()
        {
            return new Dictionary<string, object?>(_values);
        }

        /// <summary>
        /// 从字典创建参数
        /// </summary>
        public static AlgorithmParameters FromDictionary(Dictionary<string, object?> dict)
        {
            var parameters = new AlgorithmParameters();
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    parameters._values[kvp.Key] = kvp.Value;
                }
            }
            return parameters;
        }
    }

    /// <summary>
    /// 图像处理结果 - 兼容层
    /// </summary>
    public class ImageProcessResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 结果数据
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 结果图像
        /// </summary>
        public Mat? ResultImage { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public double ExecutionTimeMs { get; set; }

        /// <summary>
        /// 从数据创建结果
        /// </summary>
        public static ImageProcessResult FromData(object data, Mat? resultImage = null)
        {
            return new ImageProcessResult
            {
                Success = true,
                Data = data,
                ResultImage = resultImage
            };
        }

        /// <summary>
        /// 创建错误结果
        /// </summary>
        public static ImageProcessResult FromError(string errorMessage)
        {
            return new ImageProcessResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// 图像处理器基类 - 兼容层
    /// </summary>
    /// <remarks>
    /// 提供基础的图像处理功能，简化工具开发。
    /// 实现了 IImageProcessor 接口，同时提供参数处理辅助方法。
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
        /// 处理图像
        /// </summary>
        public Mat Process(Mat input)
        {
            var result = ProcessImage(input, new AlgorithmParameters());
            return result.ResultImage ?? input;
        }

        /// <summary>
        /// 处理图像（带矩形ROI）
        /// </summary>
        public Mat Process(Mat input, Rect roi)
        {
            using var roiMat = new Mat(input, roi);
            var result = ProcessImage(roiMat, new AlgorithmParameters());
            return result.ResultImage ?? roiMat.Clone();
        }

        /// <summary>
        /// 处理图像（带圆形ROI）
        /// </summary>
        public Mat Process(Mat input, Point2f center, float radius)
        {
            // 创建圆形掩码
            var mask = new Mat(input.Rows, input.Cols, MatType.CV_8UC1, Scalar.Black);
            Cv2.Circle(mask, (Point)center, (int)radius, Scalar.White, -1);
            
            var masked = new Mat();
            input.CopyTo(masked, mask);
            
            var result = ProcessImage(masked, new AlgorithmParameters());
            mask.Dispose();
            masked.Dispose();
            
            return result.ResultImage ?? input.Clone();
        }

        /// <summary>
        /// 处理图像 - 子类实现
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <returns>处理结果</returns>
        protected abstract ImageProcessResult ProcessImage(object image, AlgorithmParameters parameters);

        /// <summary>
        /// 验证参数 - 子类可重写
        /// </summary>
        protected virtual ValidationResult ValidateParameters(AlgorithmParameters parameters)
        {
            return new ValidationResult();
        }

        /// <summary>
        /// 获取参数值辅助方法
        /// </summary>
        protected T? GetParameter<T>(AlgorithmParameters parameters, string key, T? defaultValue = default)
        {
            if (parameters == null) return defaultValue;
            
            var value = parameters.Get<T>(key);
            if (value == null && defaultValue != null)
                return defaultValue;
            return value ?? defaultValue;
        }
    }
}
