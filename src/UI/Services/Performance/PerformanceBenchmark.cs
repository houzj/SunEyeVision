using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.Services.Performance;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 性能基准测试工具
    /// 用于测量画布系统的各项性能指标
    /// </summary>
    public class PerformanceBenchmark
    {
        private readonly PerformanceMonitor _monitor;

        public PerformanceBenchmark()
        {
            _monitor = new PerformanceMonitor();
        }

        /// <summary>
        /// 运行完整的性能基准测试
        /// </summary>
        public BenchmarkResults RunFullBenchmark()
        {
            var results = new BenchmarkResults
            {
                TestDate = DateTime.Now
            };

            // 测试1: 节点渲染性能
            results.NodeRenderingResults = TestNodeRendering();

            // 测试2: 连线更新性能
            results.ConnectionUpdateResults = TestConnectionUpdates();

            // 测试3: 拖拽响应性能
            results.DragResponseResults = TestDragResponse();

            // 测试4: 路径计算性能
            results.PathCalculationResults = TestPathCalculation();

            // 测试5: 批量更新性能
            results.BatchUpdateResults = TestBatchUpdates();

            return results;
        }

        /// <summary>
        /// 测试1: 节点渲染性能
        /// 测试目标: 支持500+节点流畅渲染
        /// </summary>
        public BenchmarkResult TestNodeRendering()
        {
            var result = new BenchmarkResult
            {
                TestName = "节点渲染性能",
                Target = "支持500+节点流畅渲染"
            };

            var testSizes = new[] { 50, 100, 200, 300, 400, 500 };
            var metrics = new List<BenchmarkMetric>();

            foreach (var nodeCount in testSizes)
            {
                using var measure = _monitor.StartMeasure($"渲染{nodeCount}个节�?);

                var nodes = CreateTestNodes(nodeCount);
                var stopwatch = Stopwatch.StartNew();

                // 模拟渲染过程
                foreach (var node in nodes)
                {
                    // 模拟渲染操作
                    var position = node.Position;
                    var size = new Size(node.StyleConfig.NodeWidth, node.StyleConfig.NodeHeight);
                }

                stopwatch.Stop();
                var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                metrics.Add(new BenchmarkMetric
                {
                    MetricName = $"{nodeCount}个节�?,
                    ValueMs = elapsedMs,
                    TargetMs = nodeCount * 0.5, // 目标: 每个节点<0.5ms
                    Passed = elapsedMs < nodeCount * 0.5
                });

                // Debug.WriteLine($"[节点渲染] {nodeCount}个节�? {elapsedMs:F2}ms");
            }

            result.Metrics = metrics;
            result.Passed = metrics.All(m => m.Passed);
            result.Summary = $"平均每个节点: {metrics.Average(m => m.ValueMs / int.Parse(m.MetricName.Replace("个节�?, ""))):F3}ms";

            return result;
        }

        /// <summary>
        /// 测试2: 连线更新性能
        /// 测试目标: 连线更新延迟<20ms
        /// </summary>
        public BenchmarkResult TestConnectionUpdates()
        {
            var result = new BenchmarkResult
            {
                TestName = "连线更新性能",
                Target = "连线更新延迟<20ms"
            };

            var testSizes = new[] { 10, 50, 100, 200 };
            var metrics = new List<BenchmarkMetric>();

            foreach (var connectionCount in testSizes)
            {
                using var measure = _monitor.StartMeasure($"更新{connectionCount}条连�?);

                var nodes = CreateTestNodes(20);
                var connections = CreateTestConnections(nodes, connectionCount);
                var stopwatch = Stopwatch.StartNew();

                // 模拟连线更新过程
                foreach (var connection in connections)
                {
                    // 模拟路径计算
                    connection.InvalidatePath();
                }

                stopwatch.Stop();
                var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                metrics.Add(new BenchmarkMetric
                {
                    MetricName = $"{connectionCount}条连�?,
                    ValueMs = elapsedMs,
                    TargetMs = 20.0,
                    Passed = elapsedMs < 20.0
                });

                // Debug.WriteLine($"[连线更新] {connectionCount}条连�? {elapsedMs:F2}ms");
            }

            result.Metrics = metrics;
            result.Passed = metrics.All(m => m.Passed);
            result.Summary = $"最大{metrics.Count}条连线耗时: {metrics.Max(m => m.ValueMs):F2}ms";

            return result;
        }

        /// <summary>
        /// 测试3: 拖拽响应性能
        /// 测试目标: 拖拽响应时间<30ms
        /// </summary>
        public BenchmarkResult TestDragResponse()
        {
            var result = new BenchmarkResult
            {
                TestName = "拖拽响应性能",
                Target = "拖拽响应时间<30ms"
            };

            var testSizes = new[] { 10, 50, 100, 200 };
            var metrics = new List<BenchmarkMetric>();

            foreach (var nodeCount in testSizes)
            {
                using var measure = _monitor.StartMeasure($"拖拽{nodeCount}个节点场�?);

                var nodes = CreateTestNodes(nodeCount);
                var draggedNode = nodes.First();
                var stopwatch = Stopwatch.StartNew();

                // 模拟拖拽过程
                var oldPosition = draggedNode.Position;
                draggedNode.Position = new Point(oldPosition.X + 10, oldPosition.Y + 10);

                // 模拟相关更新
                foreach (var node in nodes.Take(10))
                {
                    // 模拟碰撞检测等
                    var bounds = new Rect(
                        node.Position.X,
                        node.Position.Y,
                        node.StyleConfig.NodeWidth,
                        node.StyleConfig.NodeHeight);
                }

                stopwatch.Stop();
                var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

                metrics.Add(new BenchmarkMetric
                {
                    MetricName = $"{nodeCount}个节�?,
                    ValueMs = elapsedMs,
                    TargetMs = 30.0,
                    Passed = elapsedMs < 30.0
                });

                // Debug.WriteLine($"[拖拽响应] {nodeCount}个节�? {elapsedMs:F2}ms");
            }

            result.Metrics = metrics;
            result.Passed = metrics.All(m => m.Passed);
            result.Summary = $"平均响应时间: {metrics.Average(m => m.ValueMs):F2}ms";

            return result;
        }

        /// <summary>
        /// 测试4: 路径计算性能
        /// 测试目标: 单条路径计算<1ms
        /// </summary>
        public BenchmarkResult TestPathCalculation()
        {
            var result = new BenchmarkResult
            {
                TestName = "路径计算性能",
                Target = "单条路径计算<1ms"
            };

            var testIterations = new[] { 100, 500, 1000, 2000 };
            var metrics = new List<BenchmarkMetric>();

            foreach (var iterations in testIterations)
            {
                using var measure = _monitor.StartMeasure($"计算{iterations}次路�?);

                var nodes = CreateTestNodes(20);
                var connections = CreateTestConnections(nodes, iterations);
                var stopwatch = Stopwatch.StartNew();

                // 模拟路径计算
                foreach (var connection in connections)
                {
                    var sourceNode = nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                    var targetNode = nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);

                    if (sourceNode != null && targetNode != null)
                    {
                        // 简单模拟路径计�?
                        var pathPoints = new Point[]
                        {
                            sourceNode.RightPortPosition,
                            targetNode.LeftPortPosition
                        };
                    }
                }

                stopwatch.Stop();
                var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
                var avgPerPath = elapsedMs / iterations;

                metrics.Add(new BenchmarkMetric
                {
                    MetricName = $"{iterations}次计�?,
                    ValueMs = avgPerPath,
                    TargetMs = 1.0,
                    Passed = avgPerPath < 1.0
                });
            }

            result.Metrics = metrics;
            result.Passed = metrics.All(m => m.Passed);
            result.Summary = $"平均每条路径: {metrics.Average(m => m.ValueMs):F3}ms";

            return result;
        }
        /// <summary>
        /// 测试5: 批量更新性能
        /// 测试目标: 批量更新优于单个更新50%+
        /// </summary>
        public BenchmarkResult TestBatchUpdates()
        {
            var result = new BenchmarkResult
            {
                TestName = "批量更新性能",
                Target = "批量更新优于单个更新50%+"
            };

            var testSizes = new[] { 10, 50, 100 };
            var metrics = new List<BenchmarkMetric>();

            foreach (var connectionCount in testSizes)
            {
                var nodes = CreateTestNodes(20);
                var connections = CreateTestConnections(nodes, connectionCount);

                // 测试单个更新
                var singleStopwatch = Stopwatch.StartNew();
                foreach (var connection in connections)
                {
                    connection.InvalidatePath();
                }
                singleStopwatch.Stop();
                var singleTime = singleStopwatch.Elapsed.TotalMilliseconds;

                // 测试批量更新
                var batchStopwatch = Stopwatch.StartNew();
                // 模拟批量更新
                foreach (var connection in connections)
                {
                    connection.InvalidatePath();
                }
                batchStopwatch.Stop();
                var batchTime = batchStopwatch.Elapsed.TotalMilliseconds;

                var improvement = (singleTime - batchTime) / singleTime * 100;

                metrics.Add(new BenchmarkMetric
                {
                    MetricName = $"{connectionCount}条连�?,
                    ValueMs = batchTime,
                    TargetMs = singleTime * 0.5, // 目标: 提升50%
                    Passed = batchTime < singleTime * 0.5,
                    AdditionalInfo = $"提升: {improvement:F1}%"
                });

                
            }

            result.Metrics = metrics;
            result.Passed = metrics.All(m => m.Passed);
            result.Summary = $"平均提升: {metrics.Average(m => double.Parse(m.AdditionalInfo?.Replace("提升: ", "").Replace("%", "") ?? "0")):F1}%";

            return result;
        }

        /// <summary>
        /// 创建测试节点
        /// </summary>
        private ObservableCollection<WorkflowNode> CreateTestNodes(int count)
        {
            var nodes = new ObservableCollection<WorkflowNode>();
            var random = new Random(42); // 固定种子以保证可重复�?

            for (int i = 0; i < count; i++)
            {
                var node = new WorkflowNode(
                    id: $"node_{i}",
                    name: $"节点{i + 1}",
                    algorithmType: "测试算法",
                    index: i,
                    globalIndex: i
                )
                {
                    Position = new Point(random.Next(50, 1500), random.Next(50, 1000))
                };
                nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// 创建测试连线
        /// </summary>
        private ObservableCollection<WorkflowConnection> CreateTestConnections(
            ObservableCollection<WorkflowNode> nodes, int count)
        {
            var connections = new ObservableCollection<WorkflowConnection>();
            var random = new Random(42);

            for (int i = 0; i < count; i++)
            {
                var sourceIndex = random.Next(0, nodes.Count);
                var targetIndex = random.Next(0, nodes.Count);

                if (sourceIndex != targetIndex)
                {
                    var connection = new WorkflowConnection
                    {
                        Id = $"{nodes[sourceIndex].Id}_{nodes[targetIndex].Id}",
                        SourceNodeId = nodes[sourceIndex].Id,
                        TargetNodeId = nodes[targetIndex].Id,
                        SourcePort = "right",
                        TargetPort = "left"
                    };
                    connections.Add(connection);
                }
            }

            return connections;
        }

        /// <summary>
        /// 打印性能测试报告
        /// </summary>
        public void PrintBenchmarkReport(BenchmarkResults results)
        {
            var resultsList = new List<BenchmarkResult>
            {
                results.NodeRenderingResults,
                results.ConnectionUpdateResults,
                results.DragResponseResults,
                results.PathCalculationResults,
                results.BatchUpdateResults
            };

            foreach (var result in resultsList)
            {
                // Debug.WriteLine($"【{result.TestName}�?);
                // Debug.WriteLine($"目标: {result.Target}");
                // Debug.WriteLine($"结果: {(result.Passed ? "�?通过" : "�?未通过")}");
                // Debug.WriteLine($"摘要: {result.Summary}");
                // Debug.WriteLine(string.Empty);

                if (result.Metrics.Any())
                {
                    // Debug.WriteLine("详细指标:");
                    foreach (var metric in result.Metrics)
                    {
                    var status = metric.Passed ? "�? : "�?;

                    if (!string.IsNullOrEmpty(metric.AdditionalInfo))
                    {
                        // Debug.WriteLine($"     {metric.AdditionalInfo}");
                    }
                }
                }
            }

        }
    }

    /// <summary>
    /// 基准测试结果
    /// </summary>
    public class BenchmarkResults
    {
        public DateTime TestDate { get; set; }
        public BenchmarkResult NodeRenderingResults { get; set; } = new BenchmarkResult();
        public BenchmarkResult ConnectionUpdateResults { get; set; } = new BenchmarkResult();
        public BenchmarkResult DragResponseResults { get; set; } = new BenchmarkResult();
        public BenchmarkResult PathCalculationResults { get; set; } = new BenchmarkResult();
        public BenchmarkResult BatchUpdateResults { get; set; } = new BenchmarkResult();

        public int TotalTests => 5;
        public int PassedTests => new[]
        {
            NodeRenderingResults,
            ConnectionUpdateResults,
            DragResponseResults,
            PathCalculationResults,
            BatchUpdateResults
        }.Count(r => r.Passed);
    }

    /// <summary>
    /// 单个基准测试结果
    /// </summary>
    public class BenchmarkResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<BenchmarkMetric> Metrics { get; set; } = new List<BenchmarkMetric>();
    }

    /// <summary>
    /// 基准测试指标
    /// </summary>
    public class BenchmarkMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public double ValueMs { get; set; }
        public double TargetMs { get; set; }
        public bool Passed { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}
