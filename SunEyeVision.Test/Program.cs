using System;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.DeviceDriver;
using SunEyeVision.Algorithms;
using SunEyeVision.Workflow;
using SunEyeVision.Services;

namespace SunEyeVision.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== VisionMaster 测试程序 ===\n");

            // 创建日志记录器
            ILogger logger = new ConsoleLogger();

            try
            {
                // 测试设备管理
                await TestDeviceManagement(logger);

                // 测试算法
                await TestAlgorithms(logger);

                // 测试工作流
                await TestWorkflows(logger);

                Console.WriteLine("\n=== 所有测试完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                logger.LogError("测试程序执行失败", ex);
            }
        }

        static async Task TestDeviceManagement(ILogger logger)
        {
            Console.WriteLine("1. 测试设备管理...");
            
            var deviceManager = new DeviceManager(logger);
            
            // 注册模拟相机
            var simulatedCamera = new SimulatedCameraDriver("cam1", logger);
            deviceManager.RegisterDriver(simulatedCamera);

            // 检测设备
            var devices = await deviceManager.DetectDevicesAsync();
            Console.WriteLine($"检测到 {devices.Count} 个设备");

            // 连接设备
            var connected = await deviceManager.ConnectDeviceAsync("cam1");
            Console.WriteLine($"设备连接状态: {connected}");

            // 获取图像
            var image = await deviceManager.CaptureImageAsync("cam1");
            Console.WriteLine($"获取图像: {image?.Width}x{image?.Height}");

            Console.WriteLine("设备管理测试完成\n");
        }

        static async Task TestAlgorithms(ILogger logger)
        {
            Console.WriteLine("2. 测试算法...");
            
            // 创建测试图像
            var testImage = CreateTestImage();
            Console.WriteLine($"创建测试图像: {testImage.Width}x{testImage.Height}");

            // 测试二值化算法
            var thresholdAlgo = new ThresholdAlgorithm(logger);
            var parameters = new AlgorithmParameters();
            parameters.SetParameter("Threshold", 150);
            
            var result = thresholdAlgo.Execute(testImage, parameters);
            Console.WriteLine($"二值化算法结果: {result.Success}, 耗时: {result.ExecutionTimeMs}ms");

            Console.WriteLine("算法测试完成\n");
        }

        static async Task TestWorkflows(ILogger logger)
        {
            Console.WriteLine("3. 测试工作流...");
            
            var workflowEngine = new WorkflowEngine(logger);
            
            // 创建工作流
            var workflow = workflowEngine.CreateWorkflow("test1", "测试工作流", "简单的测试工作流");
            
            // 创建算法节点
            var thresholdAlgo = new ThresholdAlgorithm(logger);
            var thresholdNode = new AlgorithmNode("node1", "二值化处理", thresholdAlgo);
            
            // 添加节点到工作流
            workflow.AddNode(thresholdNode);
            
            // 创建测试图像
            var testImage = CreateTestImage();
            
            // 执行工作流
            var results = workflow.Execute(testImage);
            Console.WriteLine($"工作流执行结果: {results.Count} 个节点结果");

            Console.WriteLine("工作流测试完成\n");
        }

        static Mat CreateTestImage()
        {
            // 创建简单的测试图像（100x100，灰度）
            var width = 100;
            var height = 100;
            var data = new byte[width * height];
            
            var random = new Random();
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)random.Next(0, 255);
            }
            
            return new Mat(data, width, height, 1);
        }
    }
}