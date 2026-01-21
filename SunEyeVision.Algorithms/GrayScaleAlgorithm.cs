using System;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Algorithms
{
    /// <summary>
    /// 灰度转换算法
    /// </summary>
    public class GrayScaleAlgorithm : BaseAlgorithm
    {
        public GrayScaleAlgorithm(ILogger logger) 
            : base("灰度转换", "将彩色图像转换为灰度图像", logger)
        {
        }

        public override Mat Process(Mat image)
        {
            if (image.Channels == 1)
            {
                // 已经是灰度图像，直接返回副本
                return image.Clone();
            }

            // 将彩色图像转换为灰度图像
            var grayData = new byte[image.Width * image.Height];
            
            for (int i = 0; i < image.Width * image.Height; i++)
            {
                var r = image.Data[i * 3];
                var g = image.Data[i * 3 + 1];
                var b = image.Data[i * 3 + 2];
                
                // 使用加权平均法计算灰度�?
                grayData[i] = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
            }

            return new Mat(grayData, image.Width, image.Height, 1);
        }
    }
}
