using System;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;

namespace SunEyeVision.Algorithms
{
    /// <summary>
    /// Gaussian blur algorithm
    /// </summary>
    public class GaussianBlurAlgorithm : BaseAlgorithm
    {
        public GaussianBlurAlgorithm(ILogger logger)
            : base("Gaussian Blur", "Apply high-frequency blur processing to image", logger)
        {
        }

        public override Mat Process(Mat image)
        {
            // Default kernel size is 3
            return Process(image, new AlgorithmParameters());
        }

        public override Mat Process(Mat image, AlgorithmParameters parameters)
        {
            var kernelSize = parameters.HasParameter("KernelSize")
                ? parameters.GetParameter<int>("KernelSize")
                : 3;

            // Ensure kernel size is odd number
            if (kernelSize % 2 == 0) kernelSize++;

            var result = new Mat(image.Width, image.Height, image.Channels);
            var halfKernel = kernelSize / 2;

            // Generate Gaussian kernel
            var kernel = GenerateGaussianKernel(kernelSize);

            // Apply Gaussian blur
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    for (int c = 0; c < image.Channels; c++)
                    {
                        double sum = 0;
                        double weightSum = 0;

                        for (int ky = -halfKernel; ky <= halfKernel; ky++)
                        {
                            for (int kx = -halfKernel; kx <= halfKernel; kx++)
                            {
                                var nx = x + kx;
                                var ny = y + ky;

                                if (nx >= 0 && nx < image.Width && ny >= 0 && ny < image.Height)
                                {
                                    var pixel = image.Data[(ny * image.Width + nx) * image.Channels + c];
                                    var weight = kernel[ky + halfKernel, kx + halfKernel];

                                    sum += pixel * weight;
                                    weightSum += weight;
                                }
                            }
                        }

                        result.Data[(y * result.Width + x) * result.Channels + c] = (byte)(sum / weightSum);
                    }
                }
            }

            return result;
        }

        private double[,] GenerateGaussianKernel(int size)
        {
            var kernel = new double[size, size];
            var sigma = size / 3.0;
            var half = size / 2;
            var sum = 0.0;

            for (int y = -half; y <= half; y++)
            {
                for (int x = -half; x <= half; x++)
                {
                    var value = Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                    kernel[y + half, x + half] = value;
                    sum += value;
                }
            }

            // Normalization
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] /= sum;
                }
            }

            return kernel;
        }
    }
}
