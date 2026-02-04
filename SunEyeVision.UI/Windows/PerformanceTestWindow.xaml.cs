using System;
using System.Diagnostics;
using System.Windows;
using SunEyeVision.UI.Services;

namespace SunEyeVision.UI.Windows
{
    /// <summary>
    /// PerformanceTestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PerformanceTestWindow : Window
    {
        private readonly PerformanceBenchmark _benchmark;

        public PerformanceTestWindow()
        {
            InitializeComponent();
            _benchmark = new PerformanceBenchmark();
        }

        /// <summary>
        /// 运行完整测试
        /// </summary>
        private void RunFullTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppendText($"========================================");
                AppendText($"开始性能基准测试...");
                AppendText($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                AppendText($"========================================");
                AppendText();

                RunTest(() =>
                {
                    var results = _benchmark.RunFullBenchmark();
                    PrintResults(results);
                }, "完整基准测试");
            }
            catch (Exception ex)
            {
                AppendText($"❌ 测试异常: {ex.Message}");
                AppendText($"堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 运行节点渲染测试
        /// </summary>
        private void RunNodeTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppendText($"========================================");
                AppendText($"开始节点渲染性能测试...");
                AppendText($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                AppendText($"========================================");
                AppendText();

                RunTest(() =>
                {
                    var result = _benchmark.TestNodeRendering();
                    PrintSingleResult(result);
                }, "节点渲染测试");
            }
            catch (Exception ex)
            {
                AppendText($"❌ 测试异常: {ex.Message}");
                AppendText($"堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 运行连线更新测试
        /// </summary>
        private void RunConnectionTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppendText($"========================================");
                AppendText($"开始连线更新性能测试...");
                AppendText($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                AppendText($"========================================");
                AppendText();

                RunTest(() =>
                {
                    var result = _benchmark.TestConnectionUpdates();
                    PrintSingleResult(result);
                }, "连线更新测试");
            }
            catch (Exception ex)
            {
                AppendText($"❌ 测试异常: {ex.Message}");
                AppendText($"堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 清除结果
        /// </summary>
        private void ClearResults_Click(object sender, RoutedEventArgs e)
        {
            ResultsTextBox.Text = "点击按钮开始性能测试...";
            StatusBarText.Text = "准备就绪";
        }

        /// <summary>
        /// 运行测试（带计时和状态更新）
        /// </summary>
        private void RunTest(Action testAction, string testName)
        {
            var stopwatch = Stopwatch.StartNew();
            StatusBarText.Text = $"正在运行: {testName}...";

            try
            {
                testAction.Invoke();
                stopwatch.Stop();

                var elapsed = stopwatch.Elapsed.TotalSeconds;
                AppendText();
                AppendText($"✅ 测试完成！总耗时: {elapsed:F2}秒");
                StatusBarText.Text = $"测试完成: {testName} (耗时: {elapsed:F2}秒)";
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AppendText();
                AppendText($"❌ 测试失败: {ex.Message}");
                StatusBarText.Text = $"测试失败: {testName}";
                throw;
            }
        }

        /// <summary>
        /// 打印完整测试结果
        /// </summary>
        private void PrintResults(BenchmarkResults results)
        {
            AppendText($"========================================");
            AppendText($"      SunEyeVision 性能基准测试报告");
            AppendText($"      测试时间: {results.TestDate:yyyy-MM-dd HH:mm:ss}");
            AppendText($"      测试通过: {results.PassedTests}/{results.TotalTests}");
            AppendText($"========================================");
            AppendText();

            var resultsList = new[]
            {
                new { Result = results.NodeRenderingResults, Index = 1 },
                new { Result = results.ConnectionUpdateResults, Index = 2 },
                new { Result = results.DragResponseResults, Index = 3 },
                new { Result = results.PathCalculationResults, Index = 4 },
                new { Result = results.BatchUpdateResults, Index = 5 }
            };

            foreach (var item in resultsList)
            {
                var result = item.Result;
                AppendText($"【{item.Index}. {result.TestName}】");
                AppendText($"目标: {result.Target}");
                AppendText($"结果: {(result.Passed ? "✅ 通过" : "❌ 未通过")}");
                AppendText($"摘要: {result.Summary}");
                AppendText();

                if (result.Metrics.Any())
                {
                    AppendText("详细指标:");
                    foreach (var metric in result.Metrics)
                    {
                        var status = metric.Passed ? "✅" : "❌";
                        AppendText($"  {status} {metric.MetricName}: {metric.ValueMs:F2}ms (目标: {metric.TargetMs:F2}ms)");
                        if (!string.IsNullOrEmpty(metric.AdditionalInfo))
                        {
                            AppendText($"     {metric.AdditionalInfo}");
                        }
                    }
                    AppendText();
                }
            }

            AppendText("========================================");
            AppendText($"总体评估: {results.PassedTests}/{results.TotalTests} 项测试通过");
            AppendText($"建议: {GetRecommendation(results)}");
            AppendText("========================================");
        }

        /// <summary>
        /// 打印单个测试结果
        /// </summary>
        private void PrintSingleResult(BenchmarkResult result)
        {
            AppendText($"【{result.TestName}】");
            AppendText($"目标: {result.Target}");
            AppendText($"结果: {(result.Passed ? "✅ 通过" : "❌ 未通过")}");
            AppendText($"摘要: {result.Summary}");
            AppendText();

            if (result.Metrics.Any())
            {
                AppendText("详细指标:");
                foreach (var metric in result.Metrics)
                {
                    var status = metric.Passed ? "✅" : "❌";
                    AppendText($"  {status} {metric.MetricName}: {metric.ValueMs:F2}ms (目标: {metric.TargetMs:F2}ms)");
                    if (!string.IsNullOrEmpty(metric.AdditionalInfo))
                    {
                        AppendText($"     {metric.AdditionalInfo}");
                    }
                }
            }

            AppendText();
            AppendText($"✅ 测试完成！");
        }

        /// <summary>
        /// 获取优化建议
        /// </summary>
        private string GetRecommendation(BenchmarkResults results)
        {
            var failedTests = new[]
            {
                results.NodeRenderingResults,
                results.ConnectionUpdateResults,
                results.DragResponseResults,
                results.PathCalculationResults,
                results.BatchUpdateResults
            }.Where(r => !r.Passed).Select(r => r.TestName).ToList();

            if (failedTests.Count == 0)
            {
                return "所有性能指标均达标，系统性能良好！";
            }

            var recommendations = new List<string>();

            if (failedTests.Contains(results.NodeRenderingResults.TestName))
            {
                recommendations.Add("- 实现虚拟化渲染，只渲染可见区域内的节点");
            }

            if (failedTests.Contains(results.ConnectionUpdateResults.TestName))
            {
                recommendations.Add("- 优化批量更新算法，减少不必要的计算");
            }

            if (failedTests.Contains(results.DragResponseResults.TestName))
            {
                recommendations.Add("- 优化拖拽事件处理，使用延迟更新");
            }

            if (failedTests.Contains(results.PathCalculationResults.TestName))
            {
                recommendations.Add("- 优化路径计算算法，增加缓存策略");
            }

            if (failedTests.Contains(results.BatchUpdateResults.TestName))
            {
                recommendations.Add("- 检查批量更新实现，确保真正批量处理");
            }

            return string.Join("\n", recommendations);
        }

        /// <summary>
        /// 追加文本到结果框
        /// </summary>
        private void AppendText(string? text = null)
        {
            if (!string.IsNullOrEmpty(text))
            {
                ResultsTextBox.AppendText(text + Environment.NewLine);
            }
            ResultsTextBox.ScrollToEnd();
        }
    }
}
