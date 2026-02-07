using System;
using System.Threading.Tasks;
using SunEyeVision.Interfaces;
using SunEyeVision.Workflow.Tests;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 测试运行器
    /// </summary>
    public class TestRunner
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("SunEyeVision 工作流执行引擎测试");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 创建日志器
                var logger = new ConsoleLogger();

                // 创建测试套件
                var tests = new WorkflowExecutionTests(logger);

                // 运行所有测试
                await tests.RunAllTests();

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("测试程序执行完成");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试程序异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// 控制台日志器
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void LogDebug(string message)
        {
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}");
        }

        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO]  {DateTime.Now:HH:mm:ss.fff} {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARN]  {DateTime.Now:HH:mm:ss.fff} {message}");
        }

        public void LogError(string message, Exception? exception = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
            if (exception != null)
            {
                Console.WriteLine($"        Exception: {exception.Message}");
                Console.WriteLine($"        StackTrace: {exception.StackTrace}");
            }
            Console.ResetColor();
        }
    }
}
