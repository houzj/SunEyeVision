using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DispatcherAnalyzer;

class Program
{
    static void Main(string[] args)
    {
        var directoryPath = args.Length > 0 ? args[0] : "src/UI";

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"目录不存在: {directoryPath}");
            return;
        }

        Console.WriteLine("Dispatcher调用分析工具");
        Console.WriteLine("========================");
        Console.WriteLine();

        AnalyzeDispatcherCalls(directoryPath);
    }

    /// <summary>
    /// 分析指定目录中的所有C#文件，检查Dispatcher调用是否正确
    /// </summary>
    static void AnalyzeDispatcherCalls(string directoryPath)
    {
        var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        int filesChecked = 0;
        int issuesFound = 0;

        Console.WriteLine($"开始分析 {csFiles.Length} 个C#文件...");
        Console.WriteLine();

        foreach (var file in csFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                var issues = AnalyzeFile(file, content);

                if (issues.Count > 0)
                {
                    filesChecked++;
                    issuesFound += issues.Count;
                    Console.WriteLine($"文件: {file.Substring(file.IndexOf("src"))}");
                    foreach (var issue in issues)
                    {
                        Console.WriteLine($"  问题 (行 {issue.LineNumber}): {issue.Message}");
                        Console.WriteLine($"    代码: {issue.LineContent.Trim()}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分析文件失败: {file}");
                Console.WriteLine($"  错误: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"分析完成。");
        Console.WriteLine($"检查的文件: {csFiles.Length}");
        Console.WriteLine($"有问题的文件: {filesChecked}");
        Console.WriteLine($"发现的问题: {issuesFound}");
    }

    /// <summary>
    /// 分析单个文件
    /// </summary>
    static List<DispatcherIssue> AnalyzeFile(string filePath, string content)
    {
        var issues = new List<DispatcherIssue>();
        var lines = content.Split('\n');

        // 模式: 错误的Dispatcher.BeginInvoke参数顺序
        // 错误: _dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => { ... });
        // 正确: _dispatcher.BeginInvoke(new Action(() => { ... }), DispatcherPriority.ContextIdle);
        var wrongPattern = new Regex(@"\.BeginInvoke\s*\(\s*DispatcherPriority\.[A-Za-z]+\s*,", RegexOptions.Multiline);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            // 跳过注释
            if (line.Trim().StartsWith("//") || line.Trim().StartsWith("*"))
                continue;

            if (wrongPattern.IsMatch(line))
            {
                issues.Add(new DispatcherIssue
                {
                    LineNumber = lineNumber,
                    Message = "错误的BeginInvoke参数顺序：应该是 BeginInvoke(Action, DispatcherPriority)",
                    LineContent = line,
                    FilePath = filePath
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Dispatcher问题信息
    /// </summary>
    class DispatcherIssue
    {
        public int LineNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string LineContent { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
