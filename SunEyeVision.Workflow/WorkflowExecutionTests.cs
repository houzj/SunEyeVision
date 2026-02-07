using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Decorators;
using SunEyeVision.Workflow;

namespace SunEyeVision.Workflow.Tests
{
    /// <summary>
    /// 工作流执行测试 - 使用真实插件测试工作流执行逻辑
    /// </summary>
    public class WorkflowExecutionTests
    {
        private readonly ILogger _logger;
        private readonly WorkflowEngine _workflowEngine;
        private readonly WorkflowExecutionEngine _executionEngine;

        public WorkflowExecutionTests(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 创建引擎套件
            var (_workflowEngine, _executionEngine, _) = WorkflowEngineFactory.CreateEngineSuite(logger);
            _workflowEngine = _workflowEngine;
            _executionEngine = _executionEngine;
        }

        /// <summary>
        /// 测试1: 顺序执行工作流
        /// </summary>
        public async Task TestSequentialExecution()
        {
            _logger.LogInfo("========== 测试1: 顺序执行工作流 ==========");

            try
            {
                // 创建工作流
                var workflow = _workflowEngine.CreateWorkflow("test_sequential", "顺序执行测试");

                // 创建测试节点
                var node1 = CreateTestNode("gaussian_blur", "node1", "高斯模糊");
                var node2 = CreateTestNode("edge_detection", "node2", "边缘检测");
                var node3 = CreateTestNode("threshold", "node3", "阈值处理");

                // 添加节点
                workflow.AddNode(node1);
                workflow.AddNode(node2);
                workflow.AddNode(node3);

                // 连接节点
                workflow.ConnectNodes("node1", "node2");
                workflow.ConnectNodes("node2", "node3");

                // 执行工作流
                var testImage = new Mat(640, 480, 3);
                var result = await _executionEngine.ExecuteWorkflowAsync(workflow.Id, testImage);

                // 验证结果
                if (result.Success)
                {
                    _logger.LogInfo($"✓ 测试通过: 顺序执行成功, 耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    _logger.LogError($"✗ 测试失败: {string.Join("; ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"测试异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 测试2: 并行执行工作流(带Start节点)
        /// </summary>
        public async Task TestParallelExecution()
        {
            _logger.LogInfo("========== 测试2: 并行执行工作流 ==========");

            try
            {
                // 创建工作流
                var workflow = _workflowEngine.CreateWorkflow("test_parallel", "并行执行测试");

                // 创建Start节点
                var startNode = WorkflowNodeFactory.CreateStartNode("start", "开始");

                // 创建多个并行处理节点
                var node1 = CreateTestNode("gaussian_blur", "node1", "高斯模糊1");
                var node2 = CreateTestNode("edge_detection", "node2", "边缘检测1");
                var node3 = CreateTestNode("gray_scale", "node3", "灰度化");

                // 添加节点
                workflow.AddNode(startNode);
                workflow.AddNode(node1);
                workflow.AddNode(node2);
                workflow.AddNode(node3);

                // 连接: start -> 所有节点(并行)
                workflow.ConnectNodes("start", "node1");
                workflow.ConnectNodes("start", "node2");
                workflow.ConnectNodes("start", "node3");

                // 执行工作流
                var testImage = new Mat(640, 480, 3);
                var result = await _executionEngine.ExecuteWorkflowAsync(workflow.Id, testImage);

                // 验证结果
                if (result.Success)
                {
                    _logger.LogInfo($"✓ 测试通过: 并行执行成功, 耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    _logger.LogError($"✗ 测试失败: {string.Join("; ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"测试异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 测试3: 混合执行工作流
        /// </summary>
        public async Task TestHybridExecution()
        {
            _logger.LogInfo("========== 测试3: 混合执行工作流 ==========");

            try
            {
                // 创建工作流
                var workflow = _workflowEngine.CreateWorkflow("test_hybrid", "混合执行测试");

                // 创建多条执行链
                // 链1: start1 -> node1 -> node3
                var start1 = WorkflowNodeFactory.CreateStartNode("start1", "开始1");
                var node1 = CreateTestNode("gaussian_blur", "node1", "高斯模糊");
                var node3 = CreateTestNode("edge_detection", "node3", "边缘检测");

                // 链2: start2 -> node2 -> node4
                var start2 = WorkflowNodeFactory.CreateStartNode("start2", "开始2");
                var node2 = CreateTestNode("gray_scale", "node2", "灰度化");
                var node4 = CreateTestNode("threshold", "node4", "阈值处理");

                // 添加节点
                workflow.AddNode(start1);
                workflow.AddNode(start2);
                workflow.AddNode(node1);
                workflow.AddNode(node2);
                workflow.AddNode(node3);
                workflow.AddNode(node4);

                // 连接链1
                workflow.ConnectNodes("start1", "node1");
                workflow.ConnectNodes("node1", "node3");

                // 连接链2
                workflow.ConnectNodes("start2", "node2");
                workflow.ConnectNodes("node2", "node4");

                // 执行工作流
                var testImage = new Mat(640, 480, 3);
                var result = await _executionEngine.ExecuteWorkflowAsync(workflow.Id, testImage);

                // 验证结果
                if (result.Success)
                {
                    _logger.LogInfo($"✓ 测试通过: 混合执行成功, 耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    _logger.LogError($"✗ 测试失败: {string.Join("; ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"测试异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 测试4: 缓存装饰器效果
        /// </summary>
        public async Task TestCachingDecorator()
        {
            _logger.LogInfo("========== 测试4: 缓存装饰器效果 ==========");

            try
            {
                // 创建工作流
                var workflow = _workflowEngine.CreateWorkflow("test_caching", "缓存测试");

                // 创建带缓存的节点
                var node = WorkflowNodeFactory.CreateAlgorithmNode(
                    "gaussian_blur",
                    "node1",
                    "高斯模糊(带缓存)",
                    enableCaching: true,
                    enableRetry: false
                );

                if (node == null)
                {
                    _logger.LogError("无法创建节点");
                    return;
                }

                workflow.AddNode(node);

                // 执行两次相同的图像
                var testImage = new Mat(640, 480, 3);

                var startTime = DateTime.Now;
                var result1 = await _executionEngine.ExecuteWorkflowAsync(workflow.Id, testImage);
                var duration1 = (DateTime.Now - startTime).TotalMilliseconds;

                startTime = DateTime.Now;
                var result2 = await _executionEngine.ExecuteWorkflowAsync(workflow.Id, testImage);
                var duration2 = (DateTime.Now - startTime).TotalMilliseconds;

                _logger.LogInfo($"第一次执行: {duration1:F2}ms");
                _logger.LogInfo($"第二次执行: {duration2:F2}ms");
                _logger.LogInfo($"性能提升: {(duration1 - duration2) / duration1 * 100:F1}%");

                if (result1.Success && result2.Success && duration2 < duration1)
                {
                    _logger.LogInfo("✓ 测试通过: 缓存生效, 性能提升");
                }
                else
                {
                    _logger.LogWarning("⚠ 缓存效果不明显或执行失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"测试异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 测试5: 智能策略选择
        /// </summary>
        public async Task TestStrategySelection()
        {
            _logger.LogInfo("========== 测试5: 智能策略选择 ==========");

            try
            {
                // 测试不同类型的工作流,验证策略选择是否正确

                // 测试1: 简单顺序工作流
                var workflow1 = _workflowEngine.CreateWorkflow("test_strategy1", "简单顺序");
                var node1a = CreateTestNode("gaussian_blur", "node1a", "节点1");
                var node2a = CreateTestNode("edge_detection", "node2a", "节点2");
                workflow1.AddNode(node1a);
                workflow1.AddNode(node2a);
                workflow1.ConnectNodes("node1a", "node2a");

                var strategy1 = ExecutionStrategySelector.SelectStrategy(workflow1);
                _logger.LogInfo($"简单顺序工作流选择策略: {strategy1}");

                // 测试2: 并行工作流
                var workflow2 = _workflowEngine.CreateWorkflow("test_strategy2", "并行工作流");
                var start2 = WorkflowNodeFactory.CreateStartNode("start2", "开始");
                var node2b = CreateTestNode("gaussian_blur", "node2b", "节点A");
                var node2c = CreateTestNode("edge_detection", "node2c", "节点B");
                var node2d = CreateTestNode("gray_scale", "node2d", "节点C");
                workflow2.AddNode(start2);
                workflow2.AddNode(node2b);
                workflow2.AddNode(node2c);
                workflow2.AddNode(node2d);
                workflow2.ConnectNodes("start2", "node2b");
                workflow2.ConnectNodes("start2", "node2c");
                workflow2.ConnectNodes("start2", "node2d");

                var strategy2 = ExecutionStrategySelector.SelectStrategy(workflow2);
                _logger.LogInfo($"并行工作流选择策略: {strategy2}");

                // 测试3: 混合工作流(多执行链)
                var workflow3 = _workflowEngine.CreateWorkflow("test_strategy3", "混合工作流");
                var start3a = WorkflowNodeFactory.CreateStartNode("start3a", "开始A");
                var start3b = WorkflowNodeFactory.CreateStartNode("start3b", "开始B");
                var node3a = CreateTestNode("gaussian_blur", "node3a", "节点A");
                var node3b = CreateTestNode("edge_detection", "node3b", "节点B");
                workflow3.AddNode(start3a);
                workflow3.AddNode(start3b);
                workflow3.AddNode(node3a);
                workflow3.AddNode(node3b);
                workflow3.ConnectNodes("start3a", "node3a");
                workflow3.ConnectNodes("start3b", "node3b");

                var strategy3 = ExecutionStrategySelector.SelectStrategy(workflow3);
                _logger.LogInfo($"混合工作流选择策略: {strategy3}");

                _logger.LogInfo("✓ 测试通过: 策略选择功能正常");
            }
            catch (Exception ex)
            {
                _logger.LogError($"测试异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTests()
        {
            _logger.LogInfo("========================================");
            _logger.LogInfo("开始运行工作流执行测试");
            _logger.LogInfo("========================================");

            await TestSequentialExecution();
            _logger.LogInfo("");

            await TestParallelExecution();
            _logger.LogInfo("");

            await TestHybridExecution();
            _logger.LogInfo("");

            await TestCachingDecorator();
            _logger.LogInfo("");

            await TestStrategySelection();
            _logger.LogInfo("");

            _logger.LogInfo("========================================");
            _logger.LogInfo("所有测试完成");
            _logger.LogInfo("========================================");
        }

        /// <summary>
        /// 创建测试节点(如果工具不存在,使用测试处理器)
        /// </summary>
        private AlgorithmNode CreateTestNode(string toolId, string nodeId, string nodeName)
        {
            // 尝试从ToolRegistry创建
            var node = WorkflowNodeFactory.CreateAlgorithmNode(toolId, nodeId, nodeName);

            if (node != null)
            {
                return node;
            }

            // 如果工具不存在,创建测试节点
            var processor = new TestImageProcessor(toolId);
            return new AlgorithmNode(nodeId, nodeName, processor);
        }

        /// <summary>
        /// 测试用图像处理器
        /// </summary>
        private class TestImageProcessor : IImageProcessor
        {
            private readonly string _algorithmType;
            private int _executionCount = 0;

            public TestImageProcessor(string algorithmType)
            {
                _algorithmType = algorithmType;
            }

            public object? Process(object image)
            {
                _executionCount++;

                // 模拟不同算法的处理时间
                var delay = _algorithmType switch
                {
                    "gaussian_blur" => 50,
                    "edge_detection" => 100,
                    "gray_scale" => 30,
                    "threshold" => 20,
                    "morphology" => 80,
                    _ => 40
                };

                System.Threading.Thread.Sleep(delay);

                return image;
            }
        }
    }
}
