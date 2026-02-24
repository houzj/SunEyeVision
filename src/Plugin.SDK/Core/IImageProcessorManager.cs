using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace SunEyeVision.Plugin.SDK.Core
{
    /// <summary>
    /// 图像处理器管理器接口
    /// </summary>
    /// <remarks>
    /// 提供图像处理器的注册、查询和执行功能。
    /// 支持按名称查找和按参数类型查找。
    /// </remarks>
    public interface IImageProcessorManager
    {
        /// <summary>
        /// 注册图像处理器
        /// </summary>
        /// <param name="name">处理器名称</param>
        /// <param name="processor">处理器实例</param>
        void RegisterProcessor(string name, IImageProcessor processor);

        /// <summary>
        /// 注销图像处理器
        /// </summary>
        /// <param name="name">处理器名称</param>
        /// <returns>是否成功注销</returns>
        bool UnregisterProcessor(string name);

        /// <summary>
        /// 获取图像处理器
        /// </summary>
        /// <param name="name">处理器名称</param>
        /// <returns>处理器实例，如果不存在则返回null</returns>
        IImageProcessor? GetProcessor(string name);

        /// <summary>
        /// 检查处理器是否存在
        /// </summary>
        /// <param name="name">处理器名称</param>
        /// <returns>是否存在</returns>
        bool HasProcessor(string name);

        /// <summary>
        /// 获取所有处理器名称
        /// </summary>
        /// <returns>处理器名称集合</returns>
        IEnumerable<string> GetProcessorNames();

        /// <summary>
        /// 按参数类型查找处理器
        /// </summary>
        /// <param name="parameterType">参数类型</param>
        /// <returns>处理器集合</returns>
        IEnumerable<IParametricImageProcessor> FindProcessorsByParameterType(Type parameterType);

        /// <summary>
        /// 处理图像
        /// </summary>
        /// <param name="processorName">处理器名称</param>
        /// <param name="input">输入图像</param>
        /// <param name="parameters">处理参数（可选）</param>
        /// <returns>处理结果</returns>
        Mat Process(string processorName, Mat input, object? parameters = null);

        /// <summary>
        /// 处理图像（带矩形ROI）
        /// </summary>
        /// <param name="processorName">处理器名称</param>
        /// <param name="input">输入图像</param>
        /// <param name="roi">感兴趣区域</param>
        /// <param name="parameters">处理参数（可选）</param>
        /// <returns>处理结果</returns>
        Mat Process(string processorName, Mat input, Rect roi, object? parameters = null);

        /// <summary>
        /// 处理图像（带圆形ROI）
        /// </summary>
        /// <param name="processorName">处理器名称</param>
        /// <param name="input">输入图像</param>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        /// <param name="parameters">处理参数（可选）</param>
        /// <returns>处理结果</returns>
        Mat Process(string processorName, Mat input, Point2f center, float radius, object? parameters = null);

        /// <summary>
        /// 清空所有处理器
        /// </summary>
        void Clear();
    }
}
