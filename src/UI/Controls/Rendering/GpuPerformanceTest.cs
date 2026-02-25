using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace SunEyeVision.UI.Controls.Rendering
{
    /// <summary>
    /// CPU vs GPU性能对比测试工具
    /// 测试三种加载方式的性能差异
    /// </summary>
    public class GpuPerformanceTest
    {
        /// <summary>
        /// 性能测试结果
        /// </summary>
        public class TestResult
        {
            public string TestName { get; set; } = "";
            public double TotalTimeMs { get; set; }
            public double AvgTimeMs { get; set; }
            public double Speedup { get; set; }
            public bool IsBest { get; set; }
        }

        /// <summary>
        /// 运行完整的性能对比测试
        /// </summary>
        public static void RunComparisonTest(string testImagePath, int testSize = 80, int iterations = 100)
        {
            if (!File.Exists(testImagePath))
            {
                Debug.WriteLine($"[PerformanceTest] 测试文件不存在: {testImagePath}");
                return;
            }

            Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Debug.WriteLine($"  GPU vs CPU 性能对比测试 (测试{iterations}次)");
            Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Debug.WriteLine($"  测试图像: {Path.GetFileName(testImagePath)}");
            Debug.WriteLine($"  缩略图尺寸: {testSize}px");
            Debug.WriteLine("");

            // 预热
            Debug.WriteLine("=== 预热阶段 (10次) ===");
            TestPureCPU(testImagePath, testSize, 10, silent: true);
            TestWPFDefault(testImagePath, testSize, 10, silent: true);
            
            using var gpuLoader = new DirectXGpuThumbnailLoader();
            gpuLoader.Initialize();
            if (gpuLoader.IsGpuAvailable)
            {
                TestDirectXGPU(testImagePath, testSize, 10, gpuLoader, silent: true);
            }
            Debug.WriteLine("");

            // 正式测试
            Debug.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Debug.WriteLine("  正式测试");
            Debug.WriteLine("╚════════════════════════════════════════════════════════════╝");

            var results = new List<TestResult>();

            // 测试1: 纯CPU（System.Drawing）
            Debug.WriteLine("\n【测试1】纯CPU模式 (System.Drawing)");
            Debug.WriteLine("  - 使用GDI+进行图像解码和缩放");
            Debug.WriteLine("  - 完全在CPU上处理");
            var cpuResult = TestPureCPU(testImagePath, testSize, iterations, silent: false);
            results.Add(cpuResult);

            // 测试2: WPF默认（WPF的GPU加速）
            Debug.WriteLine("\n【测试2】WPF默认模式");
            Debug.WriteLine("  - 使用WPF的BitmapImage");
            Debug.WriteLine("  - 自动使用GPU硬件加速");
            var wpfResult = TestWPFDefault(testImagePath, testSize, iterations, silent: false);
            results.Add(wpfResult);

            // 测试3: DirectX GPU加速
            if (gpuLoader.IsGpuAvailable)
            {
                Debug.WriteLine("\n【测试3】DirectX GPU加速模式");
                Debug.WriteLine("  - 使用优化后的WPF GPU加速");
                Debug.WriteLine("  - DecodePixelWidth硬件解码");
                var gpuResult = TestDirectXGPU(testImagePath, testSize, iterations, gpuLoader, silent: false);
                results.Add(gpuResult);
            }

            // 打印汇总
            PrintSummary(results, iterations);
        }

        /// <summary>
        /// 测试1: 纯CPU模式
        /// </summary>
        private static TestResult TestPureCPU(string filePath, int size, int count, bool silent = false)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                LoadWithPureCPU(filePath, size);
            }
            sw.Stop();

            if (!silent)
            {
                Debug.WriteLine($"  总耗时: {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"  平均: {sw.ElapsedMilliseconds / (double)count:F3}ms/次");
            }

            return new TestResult
            {
                TestName = "纯CPU (System.Drawing)",
                TotalTimeMs = sw.ElapsedMilliseconds,
                AvgTimeMs = sw.ElapsedMilliseconds / (double)count,
                IsBest = false
            };
        }

        /// <summary>
        /// 纯CPU加载方式
        /// </summary>
        private static BitmapImage LoadWithPureCPU(string filePath, int size)
        {
            using var bitmap = new Bitmap(filePath);
            int width = size;
            int height = (int)(bitmap.Height * ((double)size / bitmap.Width));

            // CPU缩放
            using var scaled = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, width, height);
            }

            // 转换为WPF
            var ms = new MemoryStream();
            scaled.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            var wpfBitmap = new BitmapImage();
            wpfBitmap.BeginInit();
            wpfBitmap.CacheOption = BitmapCacheOption.OnLoad;
            wpfBitmap.StreamSource = ms;
            wpfBitmap.EndInit();
            wpfBitmap.Freeze();

            return wpfBitmap;
        }

        /// <summary>
        /// 测试2: WPF默认模式
        /// </summary>
        private static TestResult TestWPFDefault(string filePath, int size, int count, bool silent = false)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                LoadWithWPFDefault(filePath, size);
            }
            sw.Stop();

            if (!silent)
            {
                Debug.WriteLine($"  总耗时: {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"  平均: {sw.ElapsedMilliseconds / (double)count:F3}ms/次");
            }

            return new TestResult
            {
                TestName = "WPF默认",
                TotalTimeMs = sw.ElapsedMilliseconds,
                AvgTimeMs = sw.ElapsedMilliseconds / (double)count,
                IsBest = false
            };
        }

        /// <summary>
        /// WPF默认加载方式
        /// </summary>
        private static BitmapImage LoadWithWPFDefault(string filePath, int size)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath);
            bitmap.DecodePixelWidth = size;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// 测试3: DirectX GPU加速模式
        /// </summary>
        private static TestResult TestDirectXGPU(string filePath, int size, int count, DirectXGpuThumbnailLoader gpuLoader, bool silent = false)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                gpuLoader.LoadThumbnail(filePath, size);
            }
            sw.Stop();

            if (!silent)
            {
                Debug.WriteLine($"  总耗时: {sw.ElapsedMilliseconds}ms");
                Debug.WriteLine($"  平均: {sw.ElapsedMilliseconds / (double)count:F3}ms/次");
            }

            return new TestResult
            {
                TestName = "DirectX GPU",
                TotalTimeMs = sw.ElapsedMilliseconds,
                AvgTimeMs = sw.ElapsedMilliseconds / (double)count,
                IsBest = false
            };
        }

        /// <summary>
        /// 打印测试结果汇总
        /// </summary>
        private static void PrintSummary(List<TestResult> results, int iterations)
        {
            Debug.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Debug.WriteLine("  📊 性能测试汇总");
            Debug.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            // 找出最快的
            TestResult? fastest = null;
            foreach (var result in results)
            {
                if (fastest == null || result.AvgTimeMs < fastest.AvgTimeMs)
                {
                    fastest = result;
                }
            }
            
            if (fastest == null) return;
            
            fastest.IsBest = true;

            // 计算加速比
            foreach (var result in results)
            {
                result.Speedup = result.AvgTimeMs > 0 ? result.AvgTimeMs / fastest.AvgTimeMs : 1;
            }

            // 打印结果表格
            Debug.WriteLine("┌────────────────────────────────┬──────────────┬──────────────┬──────────┐");
            Debug.WriteLine("│ 测试模式                       │   总耗时(ms) │ 平均(ms/次)  │ 加速比   │");
            Debug.WriteLine("├────────────────────────────────┼──────────────┼──────────────┼──────────┤");

            foreach (var result in results)
            {
                string mark = result.IsBest ? "最快" : "";
                string name = result.TestName.PadRight(30);
                string total = result.TotalTimeMs.ToString().PadLeft(12);
                string avg = result.AvgTimeMs.ToString("F3").PadLeft(12);
                string speedup = result.IsBest ? "1.00x" : $"{result.Speedup:F2}x";

                Debug.WriteLine($"│{name}│{total}│{avg}│{speedup,9}{mark}│");
            }

            Debug.WriteLine("└────────────────────────────────┴──────────────┴──────────────┴──────────┘\n");

            // 打印关键结论
            Debug.WriteLine("🎯 关键结论:");
            if (fastest.TestName.Contains("DirectX"))
            {
                var cpuResult = results.Find(r => r.TestName.Contains("纯CPU"));
                if (cpuResult != null)
                {
                    double gpuSpeedup = cpuResult.AvgTimeMs / fastest.AvgTimeMs;
                    Debug.WriteLine($"  DirectX GPU加速比纯CPU快 {gpuSpeedup:F2}x");
                    
                    if (gpuSpeedup >= 5)
                    {
                        Debug.WriteLine($"  🚀 这是真正的GPU加速！你能感受到明显的性能提升！");
                    }
                    else if (gpuSpeedup >= 2)
                    {
                        Debug.WriteLine($"  有明显的性能提升");
                    }
                    else
                    {
                        Debug.WriteLine($"  性能提升不明显，可能是小尺寸缩略图的原因");
                    }
                }
            }
            else if (fastest.TestName.Contains("WPF"))
            {
                Debug.WriteLine($"  WPF默认模式最快，说明当前的DirectX实现还需要优化");
                Debug.WriteLine($"  💡 建议继续使用WPF默认模式（已经使用了GPU硬件加速）");
            }

            Debug.WriteLine($"\n💡 实际应用中，对于100张缩略图的加载：");
            var cpuRes = results.Find(r => r.TestName.Contains("纯CPU"));
            var wpfRes = results.Find(r => r.TestName.Contains("WPF"));
            var dxRes = results.Find(r => r.TestName.Contains("DirectX"));
            
            if (cpuRes != null)
                Debug.WriteLine($"  纯CPU模式: {cpuRes.AvgTimeMs * 100:F0}ms");
            if (wpfRes != null)
                Debug.WriteLine($"  WPF模式: {wpfRes.AvgTimeMs * 100:F0}ms");
            if (dxRes != null)
                Debug.WriteLine($"  DirectX GPU: {dxRes.AvgTimeMs * 100:F0}ms");
            Debug.WriteLine("");
        }

        /// <summary>
        /// 快速测试单张图像的加载性能
        /// </summary>
        public static void QuickTest(string testImagePath, int testSize = 80)
        {
            if (!File.Exists(testImagePath))
            {
                Debug.WriteLine($"[QuickTest] 测试文件不存在: {testImagePath}");
                return;
            }

            Debug.WriteLine($"╔════════════════════════════════════════════════════════════╗");
            Debug.WriteLine($"  快速性能测试 (单张图像)");
            Debug.WriteLine($"╚════════════════════════════════════════════════════════════╝");
            Debug.WriteLine($"  测试图像: {Path.GetFileName(testImagePath)}");
            Debug.WriteLine($"  缩略图尺寸: {testSize}px\n");

            // 纯CPU
            var sw = Stopwatch.StartNew();
            var cpuBitmap = LoadWithPureCPU(testImagePath, testSize);
            sw.Stop();
            Debug.WriteLine($"【纯CPU模式】耗时: {sw.Elapsed.TotalMilliseconds:F2}ms");

            // WPF默认
            sw.Restart();
            var wpfBitmap = LoadWithWPFDefault(testImagePath, testSize);
            sw.Stop();
            Debug.WriteLine($"【WPF默认模式】耗时: {sw.Elapsed.TotalMilliseconds:F2}ms");

            // DirectX GPU
            using var gpuLoader = new DirectXGpuThumbnailLoader();
            gpuLoader.Initialize();
            if (gpuLoader.IsGpuAvailable)
            {
                sw.Restart();
                var gpuBitmap = gpuLoader.LoadThumbnail(testImagePath, testSize);
                sw.Stop();
                Debug.WriteLine($"【DirectX GPU】耗时: {sw.Elapsed.TotalMilliseconds:F2}ms");
            }
            else
            {
                Debug.WriteLine($"【DirectX GPU】GPU不可用");
            }

            Debug.WriteLine("");
        }
    }
}
