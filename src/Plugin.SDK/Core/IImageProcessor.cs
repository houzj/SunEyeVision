using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SunEyeVision.Plugin.SDK.Core
{
    /// <summary>
    /// 图像处理器接口
    /// </summary>
    /// <remarks>
    /// 提供高性能的图像处理能力，直接使用原生图像类型，避免转换开销。
    /// 接口命名保持中立，不暴露底层技术选型。
    /// </remarks>
    public interface IImageProcessor
    {
        /// <summary>
        /// 处理器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 处理器描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 处理图像
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <returns>处理结果</returns>
        Mat Process(Mat input);

        /// <summary>
        /// 处理图像（带矩形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="roi">感兴趣区域</param>
        /// <returns>处理结果</returns>
        Mat Process(Mat input, Rect roi);

        /// <summary>
        /// 处理图像（带圆形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>处理结果</returns>
        Mat Process(Mat input, Point2f center, float radius);
    }

    /// <summary>
    /// 支持参数的图像处理器
    /// </summary>
    public interface IParametricImageProcessor : IImageProcessor
    {
        /// <summary>
        /// 参数类型
        /// </summary>
        Type ParameterType { get; }

        /// <summary>
        /// 使用参数处理图像
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <returns>处理结果</returns>
        Mat Process(Mat input, object parameters);

        /// <summary>
        /// 使用参数处理图像（带矩形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <param name="roi">感兴趣区域</param>
        /// <returns>处理结果</returns>
        Mat Process(Mat input, object parameters, Rect roi);

        /// <summary>
        /// 使用参数处理图像（带圆形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>处理结果</returns>
        Mat Process(Mat input, object parameters, Point2f center, float radius);
    }

    /// <summary>
    /// 异步图像处理器
    /// </summary>
    public interface IAsyncImageProcessor : IImageProcessor
    {
        /// <summary>
        /// 异步处理图像
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<Mat> ProcessAsync(Mat input, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步处理图像（带矩形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="roi">感兴趣区域</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<Mat> ProcessAsync(Mat input, Rect roi, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步处理图像（带圆形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<Mat> ProcessAsync(Mat input, Point2f center, float radius, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 支持参数的异步图像处理器
    /// </summary>
    public interface IAsyncParametricImageProcessor : IParametricImageProcessor, IAsyncImageProcessor
    {
        /// <summary>
        /// 异步处理图像（带参数）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<Mat> ProcessAsync(Mat input, object parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步处理图像（带参数和矩形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <param name="roi">感兴趣区域</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<Mat> ProcessAsync(Mat input, object parameters, Rect roi, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步处理图像（带参数和圆形ROI）
        /// </summary>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数</param>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理结果</returns>
        Task<Mat> ProcessAsync(Mat input, object parameters, Point2f center, float radius, CancellationToken cancellationToken = default);
    }
}
