using System;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Algorithms
{
    /// <summary>
    /// Threshold algorithm
    /// </summary>
    public class ThresholdAlgorithm : BaseAlgorithm
    {
        public ThresholdAlgorithm(ILogger logger)
            : base("Threshold", "Convert grayscale image to binary image", logger)
        {
        }

        public override Mat Process(Mat image)
        {
            // Default threshold 128
            return Process(image, new AlgorithmParameters());
        }

        public override Mat Process(Mat image, AlgorithmParameters parameters)
        {
            if (image.Channels != 1)
            {
                // If not grayscale image, convert to grayscale first
                var grayAlgo = new GrayScaleAlgorithm(Logger);
                image = grayAlgo.Process(image);
            }

            var threshold = parameters.HasParameter("Threshold")
                ? parameters.GetParameter<int>("Threshold")
                : 128;

            var binaryData = new byte[image.Width * image.Height];

            for (int i = 0; i < image.Data.Length; i++)
            {
                binaryData[i] = image.Data[i] >= threshold ? (byte)255 : (byte)0;
            }

            return new Mat(binaryData, image.Width, image.Height, 1);
        }
    }
}
