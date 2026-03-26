using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SunEyeVision.BuildTools.RuleChecker;

namespace SunEyeVision.BuildTools.RuleChecker
{
    /// <summary>
    /// 规则检查命令行工具
    /// </summary>
    class RuleCheckerProgram
    {
        static int Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("  SunEyeVision 规则检查器 (Rule Checker)");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            // 获取项目根目录
            string projectRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            projectRoot = Path.GetFullPath(projectRoot);

            Console.WriteLine($"📂 项目根目录: {projectRoot}");
            Console.WriteLine();

            // 执行规则检查
            var checker = new RuleChecker(projectRoot);
            Console.WriteLine("🔍 正在检查规则...");
            Console.WriteLine();

            var violations = checker.CheckAllRules();

            // 按优先级分组显示结果
            var criticalViolations = violations.Where(v => v.Priority == RulePriority.Critical).ToList();
            var highViolations = violations.Where(v => v.Priority == RulePriority.High).ToList();
            var mediumViolations = violations.Where(v => v.Priority == RulePriority.Medium).ToList();
            var lowViolations = violations.Where(v => v.Priority == RulePriority.Low).ToList();

            // 显示 CRITICAL 违规
            if (criticalViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("🔴 CRITICAL 优先级违规（必须立即修复）：");
                Console.ResetColor();
                DisplayViolations(criticalViolations);
                Console.WriteLine();
            }

            // 显示 HIGH 优先级违规
            if (highViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🟠 HIGH 优先级违规（应该尽快修复）：");
                Console.ResetColor();
                DisplayViolations(highViolations);
                Console.WriteLine();
            }

            // 显示 MEDIUM 优先级违规
            if (mediumViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("🟡 MEDIUM 优先级违规（可以延后修复）：");
                Console.ResetColor();
                DisplayViolations(mediumViolations);
                Console.WriteLine();
            }

            // 显示 LOW 优先级违规
            if (lowViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("🟢 LOW 优先级违规（可选修复）：");
                Console.ResetColor();
                DisplayViolations(lowViolations);
                Console.WriteLine();
            }

            // 显示总结
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("  检查总结");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"🔴 Critical: {criticalViolations.Count}");
            Console.WriteLine($"🟠 High:     {highViolations.Count}");
            Console.WriteLine($"🟡 Medium:   {mediumViolations.Count}");
            Console.WriteLine($"🟢 Low:      {lowViolations.Count}");
            Console.WriteLine($"总计:        {violations.Count}");
            Console.WriteLine();

            // 根据违规情况返回退出码
            if (criticalViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ 检查失败！发现 CRITICAL 优先级违规，必须修复后再继续。");
                Console.ResetColor();
                return 1;
            }
            else if (highViolations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  警告！发现 HIGH 优先级违规，建议尽快修复。");
                Console.ResetColor();
                return 2;
            }
            else if (violations.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("ℹ️  提示！发现违规，建议修复。");
                Console.ResetColor();
                return 3;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ 检查通过！没有发现违规。");
                Console.ResetColor();
                return 0;
            }
        }

        /// <summary>
        /// 显示违规详情
        /// </summary>
        static void DisplayViolations(List<RuleViolation> violations)
        {
            // 按文件分组
            var grouped = violations.GroupBy(v => v.FilePath);

            foreach (var group in grouped)
            {
                Console.WriteLine();
                Console.WriteLine($"📄 {group.Key}");

                foreach (var violation in group)
                {
                    Console.WriteLine();
                    Console.WriteLine($"   [{violation.RuleId}] {violation.Message}");
                    Console.WriteLine($"   📍 行 {violation.LineNumber}: {violation.Suggestion}");
                }
            }
        }
    }
}
