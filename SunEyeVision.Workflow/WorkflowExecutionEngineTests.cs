using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Models;
using SunEyeVision.PluginSystem;
using SunEyeVision.PluginSystem.Base.Interfaces;
using SunEyeVision.PluginSystem.Infrastructure;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流执行引擎集成测试
    /// </summary>
    public class WorkflowExecutionEngineTests
    {
        private readonly WorkflowEngine _workflowEngine;
        private readonly WorkflowExecutionEngine _executionEngine;
        private readonly IPluginManager _pluginManager;
        private readonly ILogger _logger;

        public WorkflowExecutionEngineTests()
        {
            _logger = new TestLogger();
            var pluginManager = new PluginManager(_logger);
            _workflowEngine = new WorkflowEngine(_logger);
            _executionEngine = new WorkflowExecutionEngine(_workflowEngine, pluginManager, _logger);
            _pluginManager = pluginManager;
            
            // 手动注册工作流控制插件
            var controlPlugin = new SubroutinePlugin(_workflowEngine);
            _pluginManager.RegisterPlugin(controlPlugin);
        }

        /// <summary>
        /// 测试1: 基础工作流执行
        /// </summary>
        public async Task Test_BasicWorkflowExecution()
        {
            Console.WriteLine("=== 测试1: 基础工作流执行 ===");

            // 创建工作流
            var workflow = _workflowEngine.CreateWorkflow("test-basic", "基础测试");

            // 创建测试节点
            var node1 = CreateTestAlgorithmNode("node-1", "灰度转换");
            var node2 = CreateTestAlgorithmNode("node-2", "高斯模糊");
            var node3 = CreateTestAlgorithmNode("node-3", "边缘检测");

            workflow.AddNode(node1);
            workflow.AddNode(node2);
            workflow.AddNode(node3);

            // 连接节点
            workflow.ConnectNodes("node-1", "node-2");
            workflow.ConnectNodes("node-2", "node-3");

        // 执行工作流
        var result = await _executionEngine.ExecuteWorkflow("test-basic", TestHelper.CreateTestMat());

            // 验证结果
            if (result.Success)
            {
                Console.WriteLine($"✓ 测试通过: 执行成功，耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                Console.WriteLine($"  成功节点: {result.NodeResults?.Count}");
            }
            else
            {
                Console.WriteLine($"✗ 测试失败: {result.Errors?.FirstOrDefault()}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 测试2: 带子程序的工作流
        /// </summary>
        public async Task Test_WorkflowWithSubroutine()
        {
            Console.WriteLine("=== 测试2: 带子程序的工作流 ===");

            try
            {
                // 创建子程序工作流
                var subWorkflow = _workflowEngine.CreateWorkflow("test-sub-001", "子程序测试");
                var subNode1 = CreateTestAlgorithmNode("sub-node-1", "去噪");
                var subNode2 = CreateTestAlgorithmNode("sub-node-2", "锐化");
                subWorkflow.AddNode(subNode1);
                subWorkflow.AddNode(subNode2);
                subWorkflow.ConnectNodes("sub-node-1", "sub-node-2");

                // 创建主工作流
                var mainWorkflow = _workflowEngine.CreateWorkflow("test-main-001", "主工作流");
                var mainNode1 = CreateTestAlgorithmNode("main-node-1", "加载");
                
                var subroutineNode = new SubroutineNode
                {
                    Id = "main-node-2",
                    Name = "子程序调用",
                    SubroutineId = "test-sub-001",
                    SubroutineName = "预处理",
                    IsLoop = false,
                    MaxIterations = 1
                };

                var mainNode3 = CreateTestAlgorithmNode("main-node-3", "保存");

                mainWorkflow.AddNode(mainNode1);
                mainWorkflow.AddNode(subroutineNode);
                mainWorkflow.AddNode(mainNode3);
                mainWorkflow.ConnectNodes("main-node-1", "main-node-2");
                mainWorkflow.ConnectNodes("main-node-2", "main-node-3");

                // 订阅事件
                _executionEngine.NodeStatusChanged += (s, e) =>
                {
                    Console.WriteLine($"  节点 {e.NodeId}: {e.Status}");
                };

                // 执行工作流
                var result = await _executionEngine.ExecuteWorkflowAsync("test-main-001", TestHelper.CreateTestMat());

                // 验证结果
                if (result.Success)
                {
                    Console.WriteLine($"✓ 测试通过: 子程序执行成功，耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    Console.WriteLine($"✗ 测试失败: {result.Errors?.FirstOrDefault()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试异常: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 测试3: 带条件分支的工作流
        /// </summary>
        public async Task Test_WorkflowWithCondition()
        {
            Console.WriteLine("=== 测试3: 带条件分支的工作流 ===");

            try
            {
                // 创建工作流
                var workflow = _workflowEngine.CreateWorkflow("test-cond-001", "条件测试");

                var node1 = CreateTestAlgorithmNode("node-1", "质量评估");

                var conditionNode = new ConditionNode
                {
                    Id = "node-2",
                    Name = "质量判断",
                    ConditionType = ConditionType.Expression,
                    ConditionExpression = "QualityScore >= 0.85",
                    TrueValue = true,
                    FalseValue = false
                };

                var node3 = CreateTestAlgorithmNode("node-3", "高质量处理");
                var node4 = CreateTestAlgorithmNode("node-4", "低质量处理");
                var node5 = CreateTestAlgorithmNode("node-5", "后处理");

                workflow.AddNode(node1);
                workflow.AddNode(conditionNode);
                workflow.AddNode(node3);
                workflow.AddNode(node4);
                workflow.AddNode(node5);

                workflow.ConnectNodes("node-1", "node-2");
                workflow.ConnectNodes("node-2", "node-3");
                workflow.ConnectNodes("node-2", "node-4");
                workflow.ConnectNodes("node-3", "node-5");
                workflow.ConnectNodes("node-4", "node-5");

                // 执行工作流
                var result = await _executionEngine.ExecuteWorkflow("test-cond-001", TestHelper.CreateTestMat());

                // 验证结果
                if (result.Success)
                {
                    Console.WriteLine($"✓ 测试通过: 条件分支执行成功，耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    Console.WriteLine($"✗ 测试失败: {result.Errors?.FirstOrDefault()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试异常: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 测试4: 循环子程序
        /// </summary>
        public async Task Test_LoopSubroutine()
        {
            Console.WriteLine("=== 测试4: 循环子程序 ===");

            try
            {
                // 创建子程序工作流
                var subWorkflow = _workflowEngine.CreateWorkflow("test-loop-sub", "循环子程序");
                var subNode = CreateTestAlgorithmNode("sub-loop-node", "迭代处理");
                subWorkflow.AddNode(subNode);

                // 创建主工作流
                var mainWorkflow = _workflowEngine.CreateWorkflow("test-loop-main", "循环主程序");
                var mainNode1 = CreateTestAlgorithmNode("main-node-1", "初始化");
                
                var subroutineNode = new SubroutineNode
                {
                    Id = "main-node-2",
                    Name = "循环子程序",
                    SubroutineId = "test-loop-sub",
                    SubroutineName = "迭代处理",
                    IsLoop = true,
                    LoopType = LoopType.FixedCount,
                    MaxIterations = 3
                };

                var mainNode3 = CreateTestAlgorithmNode("main-node-3", "完成");

                mainWorkflow.AddNode(mainNode1);
                mainWorkflow.AddNode(subroutineNode);
                mainWorkflow.AddNode(mainNode3);
                mainWorkflow.ConnectNodes("main-node-1", "main-node-2");
                mainWorkflow.ConnectNodes("main-node-2", "main-node-3");

                // 订阅进度事件
                _executionEngine.ProgressChanged += (s, e) =>
                {
                    Console.WriteLine($"  进度: {e.Progress:P0} - {e.Message}");
                };

                // 执行工作流
                var result = await _executionEngine.ExecuteWorkflowAsync("test-loop-main", TestHelper.CreateTestMat());

                // 验证结果
                if (result.Success)
                {
                    Console.WriteLine($"✓ 测试通过: 循环子程序执行成功，耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                    
                    var stats = _executionEngine.GetExecutionStatistics();
                    Console.WriteLine($"  总节点: {stats.TotalNodes}, 成功: {stats.CompletedNodes}");
                }
                else
                {
                    Console.WriteLine($"✗ 测试失败: {result.Errors?.FirstOrDefault()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试异常: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 测试5: 循环依赖检测
        /// </summary>
        public async Task Test_CycleDetection()
        {
            Console.WriteLine("=== 测试5: 循环依赖检测 ===");

            try
            {
                // 创建工作流
                var workflow = _workflowEngine.CreateWorkflow("test-cycle", "循环依赖测试");

                var node1 = CreateTestAlgorithmNode("node-1", "节点1");
                var node2 = CreateTestAlgorithmNode("node-2", "节点2");
                var node3 = CreateTestAlgorithmNode("node-3", "节点3");

                workflow.AddNode(node1);
                workflow.AddNode(node2);
                workflow.AddNode(node3);

                // 创建循环
                workflow.ConnectNodes("node-1", "node-2");
                workflow.ConnectNodes("node-2", "node-3");
                workflow.ConnectNodes("node-3", "node-1");

                // 尝试执行工作流
                var result = await _executionEngine.ExecuteWorkflow("test-cycle", TestHelper.CreateTestMat());

                // 验证结果（应该失败）
                if (!result.Success && result.Errors?.Any(e => e.Contains("循环")) == true)
                {
                    Console.WriteLine($"✓ 测试通过: 成功检测到循环依赖");
                    Console.WriteLine($"  错误信息: {result.Errors?.FirstOrDefault()}");
                }
                else
                {
                    Console.WriteLine($"✗ 测试失败: 应该检测到循环依赖");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试异常: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 测试6: 执行暂停和恢复
        /// </summary>
        public async Task Test_PauseAndResume()
        {
            Console.WriteLine("=== 测试6: 执行暂停和恢复 ===");

            try
            {
                // 创建工作流（多个节点）
                var workflow = _workflowEngine.CreateWorkflow("test-pause", "暂停测试");
                for (int i = 1; i <= 10; i++)
                {
                    var node = CreateTestAlgorithmNode($"node-{i}", $"节点{i}");
                    workflow.AddNode(node);
                    if (i > 1)
                    {
                        workflow.ConnectNodes($"node-{i - 1}", $"node-{i}");
                    }
                }

                // 异步执行
                var executionTask = _executionEngine.ExecuteWorkflowAsync("test-pause", TestHelper.CreateTestMat());

                // 等待一会儿然后暂停
                await Task.Delay(100);
                _executionEngine.PauseExecution();
                Console.WriteLine($"  执行已暂停，状态: {_executionEngine.CurrentState}");

                // 等待一会儿然后恢复
                await Task.Delay(200);
                _executionEngine.ResumeExecution();
                Console.WriteLine($"  执行已恢复，状态: {_executionEngine.CurrentState}");

                // 等待完成
                var result = await executionTask;

                if (result.Success)
                {
                    Console.WriteLine($"✓ 测试通过: 暂停和恢复功能正常，耗时 {result.ExecutionTime.TotalMilliseconds:F2}ms");
                }
                else
                {
                    Console.WriteLine($"✗ 测试失败: {result.Errors?.FirstOrDefault()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试异常: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  工作流执行引擎集成测试");
            Console.WriteLine("========================================\n");

            Test_BasicWorkflowExecution();
            await Test_WorkflowWithSubroutine();
            Test_WorkflowWithCondition();
            await Test_LoopSubroutine();
            Test_CycleDetection();
            await Test_PauseAndResume();

            Console.WriteLine("========================================");
            Console.WriteLine("  所有测试完成");
            Console.WriteLine("========================================");
        }

        /// <summary>
        /// 创建测试算法节点
        /// </summary>
        private AlgorithmNode CreateTestAlgorithmNode(string id, string name)
        {
            return new AlgorithmNode(id, name, new TestImageProcessor());
        }
    }

    #region 测试辅助类

    /// <summary>
    /// 测试日志记录器
    /// </summary>
    public class TestLogger : ILogger
    {
        public void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
        public void LogInfo(string message) => Console.WriteLine($"[INFO] {message}");
        public void LogWarning(string message) => Console.WriteLine($"[WARN] {message}");
        public void LogError(string message, Exception? exception = null)
        {
            Console.WriteLine($"[ERROR] {message}");
            if (exception != null)
            {
                Console.WriteLine($"  异常: {exception.Message}");
            }
        }
        public void LogFatal(string message, Exception? exception = null)
        {
            Console.WriteLine($"[FATAL] {message}");
            if (exception != null)
            {
                Console.WriteLine($"  异常: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// 测试图像处理器
    /// </summary>
    public class TestImageProcessor : IImageProcessor
    {
        public string Name => "TestProcessor";
        public string Description => "测试处理器";
        public string Category => "Test";

        public object Process(object input)
        {
            // 模拟处理延迟
            Task.Delay(10).Wait();
            return input;
        }
    }

    /// <summary>
    /// 测试辅助方法
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// 创建测试用的空Mat
        /// </summary>
        public static Mat CreateTestMat()
        {
            return new Mat(640, 480, 3);
        }
    }

    #endregion
}
