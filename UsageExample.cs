using System;
using VisionMaster.Algorithms;
using VisionMaster.DeviceDriver;
using VisionMaster.Interfaces;
using VisionMaster.Models;
using VisionMaster.PluginSystem;
using VisionMaster.Services;
using VisionMaster.Workflow;

namespace VisionMaster
{
    /// <summary>
    /// VisionMaster 框架使用示例
    /// </summary>
    public class UsageExample
    {
        public static void Main()
        {
            // 1. 初始化日志记录器
            var logger = new FileLogger();
            var consoleLogger = new ConsoleLogger();
            
            logger.Info("VisionMaster 框架启动");
            consoleLogger.Info("VisionMaster 框架启动");

            // 2. 创建配置管理器
            var configManager = new JsonConfigManager();
            configManager.SaveConfig("AppName", "VisionMaster");
            configManager.SaveConfig("Version", "1.0.0");

            // 3. 创建图像处理算法
            var grayScaleAlgorithm = new GrayScaleAlgorithm(logger);
            var thresholdAlgorithm = new ThresholdAlgorithm(logger);
            var gaussianBlurAlgorithm = new GaussianBlurAlgorithm(logger);
            var edgeDetectionAlgorithm = new EdgeDetectionAlgorithm(logger);

            // 4. 创建工作流
            var workflowEngine = new WorkflowEngine(logger);
            var workflow = workflowEngine.CreateWorkflow("workflow1", "图像处理工作流", "演示工作流");

            // 5. 添加算法节点
            var blurNode = new AlgorithmNode("blur", "高斯模糊", gaussianBlurAlgorithm);
            var grayNode = new AlgorithmNode("gray", "灰度转换", grayScaleAlgorithm);
            var thresholdNode = new AlgorithmNode("threshold", "二值化", thresholdAlgorithm);
            var edgeNode = new AlgorithmNode("edge", "边缘检测", edgeDetectionAlgorithm);

            workflow.AddNode(blurNode);
            workflow.AddNode(grayNode);
            workflow.AddNode(thresholdNode);
            workflow.AddNode(edgeNode);

            // 6. 连接节点
            workflow.ConnectNodes("blur", "gray");
            workflow.ConnectNodes("gray", "threshold");
            workflow.ConnectNodes("threshold", "edge");

            // 7. 设置参数
            thresholdNode.Parameters.SetParameter("Threshold", 128);
            blurNode.Parameters.SetParameter("KernelSize", 5);

            // 8. 创建设备驱动
            var deviceManager = new DeviceManager(logger);
            var simulatedCamera = new SimulatedCameraDriver("CAM001", logger);
            deviceManager.RegisterDriver(simulatedCamera);

            // 9. 连接设备
            deviceManager.ConnectDeviceAsync("CAM001").Wait();

            // 10. 采集图像
            var image = deviceManager.CaptureImageAsync("CAM001").Result;

            // 11. 执行工作流
            if (image != null)
            {
                logger.Info($"输入图像尺寸: {image.Width}x{image.Height}");
                
                var results = workflow.Execute(image);
                
                logger.Info($"工作流执行完成，共 {results.Count} 个节点执行");
                
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        logger.Info($"节点执行成功，耗时: {result.ExecutionTime}ms");
                    }
                    else
                    {
                        logger.Error($"节点执行失败: {result.ErrorMessage}");
                    }
                }
            }

            // 12. 保存工作流
            workflowEngine.SaveWorkflowToFile("workflow1", "workflow.json");

            // 13. 加载插件
            var pluginLoader = new PluginLoader(logger);
            pluginLoader.LoadPluginsFromDirectory("plugins");

            var plugins = pluginLoader.GetAllPlugins();
            logger.Info($"已加载 {plugins.Count} 个插件");

            // 14. 清理资源
            deviceManager.Dispose();
            logger.Info("VisionMaster 框架退出");
        }
    }
}
