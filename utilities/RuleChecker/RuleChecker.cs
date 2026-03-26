using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SunEyeVision.BuildTools.RuleChecker
{
    /// <summary>
    /// 规则检查器 - 自动检查代码是否违反项目规则
    /// </summary>
    public class RuleChecker
    {
        private readonly string _projectRoot;
        private readonly List<RuleViolation> _violations = new List<RuleViolation>();

        public RuleChecker(string projectRoot)
        {
            _projectRoot = projectRoot;
        }

        /// <summary>
        /// 执行所有规则检查
        /// </summary>
        public List<RuleViolation> CheckAllRules()
        {
            _violations.Clear();

            // CRITICAL 优先级规则
            CheckRule001_PropertyNotification();
            CheckRule008_PrototypeCodeClean();
            CheckRule010_SolutionSystemImplementation();

            // HIGH 优先级规则
            CheckRule002_NamingConventions();
            CheckRule003_LoggingSystem();
            CheckRule011_TempFileCleanup();
            CheckRule012_ParameterSystemConstraints();

            return _violations;
        }

        #region Rule-001: 属性更改通知统一规范

        /// <summary>
        /// Rule-001: 属性更改通知统一规范
        /// </summary>
        private void CheckRule001_PropertyNotification()
        {
            var csFiles = Directory.GetFiles(_projectRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.codebuddy\\"))
                .ToList();

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;

                    // 检查：直接实现 INotifyPropertyChanged（而不继承 ObservableObject）
                    if (Regex.IsMatch(line, @":\s*INotifyPropertyChanged"))
                    {
                        // 检查是否有详细注释说明原因
                        bool hasExplanation = false;
                        for (int j = Math.Max(0, i - 5); j < i; j++)
                        {
                            if (lines[j].Contains("注意：") || lines[j].Contains("Note:"))
                            {
                                hasExplanation = true;
                                break;
                            }
                        }

                        if (!hasExplanation)
                        {
                            _violations.Add(new RuleViolation
                            {
                                RuleId = "Rule-001",
                                RuleName = "属性更改通知统一规范",
                                Priority = RulePriority.Critical,
                                FilePath = file,
                                LineNumber = lineNumber,
                                Message = "直接实现 INotifyPropertyChanged，应该继承 ObservableObject 基类",
                                Suggestion = "改为继承 ObservableObject，或者添加详细注释说明原因"
                            });
                        }
                    }

                    // 检查：手动实现属性通知
                    if (Regex.IsMatch(line, @"OnPropertyChanged\s*\("))
                    {
                        // 检查是否在 ObservableObject 中
                        bool isInObservableObject = false;
                        for (int j = 0; j < i; j++)
                        {
                            if (lines[j].Contains("class ObservableObject") || lines[j].Contains("class ViewModelBase"))
                            {
                                isInObservableObject = true;
                                break;
                            }
                        }

                        if (!isInObservableObject)
                        {
                            _violations.Add(new RuleViolation
                            {
                                RuleId = "Rule-001",
                                RuleName = "属性更改通知统一规范",
                                Priority = RulePriority.Critical,
                                FilePath = file,
                                LineNumber = lineNumber,
                                Message = "手动实现属性通知逻辑，应该使用 SetProperty 方法",
                                Suggestion = "使用 SetProperty 方法替代手动通知"
                            });
                        }
                    }

                    // 检查：重复实现 INotifyPropertyChanged
                    if (line.Contains("public event PropertyChangedEventHandler? PropertyChanged;"))
                    {
                        // 检查是否在 ObservableObject 中
                        bool isInObservableObject = false;
                        for (int j = 0; j < i; j++)
                        {
                            if (lines[j].Contains("class ObservableObject") || lines[j].Contains("class ViewModelBase"))
                            {
                                isInObservableObject = true;
                                break;
                            }
                        }

                        if (!isInObservableObject)
                        {
                            _violations.Add(new RuleViolation
                            {
                                RuleId = "Rule-001",
                                RuleName = "属性更改通知统一规范",
                                Priority = RulePriority.Critical,
                                FilePath = file,
                                LineNumber = lineNumber,
                                Message = "重复实现 INotifyPropertyChanged，应该继承 ObservableObject",
                                Suggestion = "删除重复实现，继承 ObservableObject 基类"
                            });
                        }
                    }
                }
            }
        }

        #endregion

        #region Rule-002: 命名规范

        /// <summary>
        /// Rule-002: 命名规范
        /// </summary>
        private void CheckRule002_NamingConventions()
        {
            var csFiles = Directory.GetFiles(_projectRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.codebuddy\\"))
                .ToList();

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;

                    // 检查：类名使用小驼峰（应该使用 PascalCase）
                    if (Regex.IsMatch(line, @"class\s+[a-z][a-zA-Z0-9]*\s*[:\{]"))
                    {
                        var match = Regex.Match(line, @"class\s+([a-z][a-zA-Z0-9]*)");
                        if (match.Success)
                        {
                            var className = match.Groups[1].Value;
                            _violations.Add(new RuleViolation
                            {
                                RuleId = "Rule-002",
                                RuleName = "命名规范",
                                Priority = RulePriority.High,
                                FilePath = file,
                                LineNumber = lineNumber,
                                Message = $"类名 '{className}' 使用小驼峰，应该使用 PascalCase",
                                Suggestion = $"改为 PascalCase: {char.ToUpper(className[0])}{className.Substring(1)}"
                            });
                        }
                    }

                    // 检查：私有字段无下划线前缀
                    if (Regex.IsMatch(line, @"private\s+\w+\s+[a-z][a-zA-Z0-9_]*\s*[=;]"))
                    {
                        var match = Regex.Match(line, @"private\s+\w+\s+([a-z][a-zA-Z0-9_]*)");
                        if (match.Success)
                        {
                            var fieldName = match.Groups[1].Value;
                            if (!fieldName.StartsWith("_"))
                            {
                                _violations.Add(new RuleViolation
                                {
                                    RuleId = "Rule-002",
                                    RuleName = "命名规范",
                                    Priority = RulePriority.High,
                                    FilePath = file,
                                    LineNumber = lineNumber,
                                    Message = $"私有字段 '{fieldName}' 缺少下划线前缀",
                                    Suggestion = $"改为: _{fieldName}"
                                });
                            }
                        }
                    }

                    // 检查：布尔值缺少 Is/Has/Can 前缀
                    if (Regex.IsMatch(line, @"(public|private)\s+bool\s+([A-Z][a-zA-Z0-9]*)\s*[{\{]"))
                    {
                        var match = Regex.Match(line, @"(public|private)\s+bool\s+([A-Z][a-zA-Z0-9]*)");
                        if (match.Success)
                        {
                            var boolName = match.Groups[2].Value;
                            if (!boolName.StartsWith("Is") && !boolName.StartsWith("Has") && !boolName.StartsWith("Can"))
                            {
                                _violations.Add(new RuleViolation
                                {
                                    RuleId = "Rule-002",
                                    RuleName = "命名规范",
                                    Priority = RulePriority.High,
                                    FilePath = file,
                                    LineNumber = lineNumber,
                                    Message = $"布尔属性 '{boolName}' 缺少 Is/Has/Can 前缀",
                                    Suggestion = $"改为: Is{boolName} 或 Has{boolName} 或 Can{boolName}"
                                });
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Rule-003: 日志系统使用规范

        /// <summary>
        /// Rule-003: 日志系统使用规范
        /// </summary>
        private void CheckRule003_LoggingSystem()
        {
            var csFiles = Directory.GetFiles(_projectRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.codebuddy\\"))
                .ToList();

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;

                    // 检查：使用 Debug.WriteLine
                    if (line.Contains("Debug.WriteLine"))
                    {
                        _violations.Add(new RuleViolation
                        {
                            RuleId = "Rule-003",
                            RuleName = "日志系统使用规范",
                            Priority = RulePriority.High,
                            FilePath = file,
                            LineNumber = lineNumber,
                            Message = "使用 System.Diagnostics.Debug.WriteLine()",
                            Suggestion = "使用项目的日志系统：ViewModel 层使用 LogInfo()，Service 层使用 _logger.Log()"
                        });
                    }

                    // 检查：使用 Console.WriteLine
                    if (line.Contains("Console.WriteLine"))
                    {
                        _violations.Add(new RuleViolation
                        {
                            RuleId = "Rule-003",
                            RuleName = "日志系统使用规范",
                            Priority = RulePriority.High,
                            FilePath = file,
                            LineNumber = lineNumber,
                            Message = "使用 Console.WriteLine()",
                            Suggestion = "使用项目的日志系统：ViewModel 层使用 LogInfo()，Service 层使用 _logger.Log()"
                        });
                    }

                    // 检查：使用 Trace.WriteLine
                    if (line.Contains("Trace.WriteLine"))
                    {
                        _violations.Add(new RuleViolation
                        {
                            RuleId = "Rule-003",
                            RuleName = "日志系统使用规范",
                            Priority = RulePriority.High,
                            FilePath = file,
                            LineNumber = lineNumber,
                            Message = "使用 System.Diagnostics.Trace.WriteLine()",
                            Suggestion = "使用项目的日志系统：ViewModel 层使用 LogInfo()，Service 层使用 _logger.Log()"
                        });
                    }
                }
            }
        }

        #endregion

        #region Rule-008: 原型设计期代码纯净原则

        /// <summary>
        /// Rule-008: 原型设计期代码纯净原则
        /// </summary>
        private void CheckRule008_PrototypeCodeClean()
        {
            var csFiles = Directory.GetFiles(_projectRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.codebuddy\\"))
                .ToList();

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;

                    // 检查：使用 [Obsolete] 标记
                    if (line.Contains("[Obsolete"))
                    {
                        _violations.Add(new RuleViolation
                        {
                            RuleId = "Rule-008",
                            RuleName = "原型设计期代码纯净原则",
                            Priority = RulePriority.Critical,
                            FilePath = file,
                            LineNumber = lineNumber,
                            Message = "使用 [Obsolete] 标记，原型阶段应该直接删除旧代码",
                            Suggestion = "删除旧代码，不保留兼容性"
                        });
                    }

                    // 检查：条件编译（#if）
                    if (Regex.IsMatch(line.Trim(), @"^#\s*if\s+"))
                    {
                        _violations.Add(new RuleViolation
                        {
                            RuleId = "Rule-008",
                            RuleName = "原型设计期代码纯净原则",
                            Priority = RulePriority.Critical,
                            FilePath = file,
                            LineNumber = lineNumber,
                            Message = "使用条件编译（#if），原型阶段应该避免使用",
                            Suggestion = "删除条件编译，直接使用新代码"
                        });
                    }

                    // 检查：注释掉的代码（连续多行注释）
                    int commentLineCount = 0;
                    for (int j = i; j < Math.Min(i + 5, lines.Length); j++)
                    {
                        if (lines[j].Trim().StartsWith("//"))
                        {
                            commentLineCount++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (commentLineCount >= 3)
                    {
                        // 检查是否包含代码关键字
                        bool isCode = false;
                        for (int j = i; j < i + commentLineCount; j++)
                        {
                            if (Regex.IsMatch(lines[j], @"\b(class|public|private|void|int|string|bool)\b"))
                            {
                                isCode = true;
                                break;
                            }
                        }

                        if (isCode)
                        {
                            _violations.Add(new RuleViolation
                            {
                                RuleId = "Rule-008",
                                RuleName = "原型设计期代码纯净原则",
                                Priority = RulePriority.Critical,
                                FilePath = file,
                                LineNumber = lineNumber,
                                Message = "发现注释掉的代码，原型阶段应该删除旧代码",
                                Suggestion = "删除注释掉的代码，Git 已提供完整的版本历史"
                            });
                        }
                    }
                }
            }
        }

        #endregion

        #region Rule-010: 方案系统实现规范

        /// <summary>
        /// Rule-010: 方案系统实现规范
        /// </summary>
        private void CheckRule010_SolutionSystemImplementation()
        {
            var csFiles = Directory.GetFiles(_projectRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\.codebuddy\\"))
                .ToList();

            foreach (var file in csFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var lineNumber = i + 1;

                    // 检查：使用 Newtonsoft.Json
                    if (line.Contains("Newtonsoft.Json"))
                    {
                        _violations.Add(new RuleViolation
                        {
                            RuleId = "Rule-010",
                            RuleName = "方案系统实现规范",
                            Priority = RulePriority.Critical,
                            FilePath = file,
                            LineNumber = lineNumber,
                            Message = "使用 Newtonsoft.Json，应该使用 System.Text.Json",
                            Suggestion = "改为使用 System.Text.Json 和 [JsonPolymorphic] 特性"
                        });
                    }

                    // 检查：直接调用 ToSerializableDictionary（在业务逻辑中）
                    if (line.Contains("ToSerializableDictionary()") || line.Contains("ToSerializableDictionary ("))
                    {
                        // 检查是否在 JsonConverter 或 ToolParameters 类中（允许的场景）
                        bool inAllowedContext = false;
                        for (int j = Math.Max(0, i - 10); j < i; j++)
                        {
                            if (lines[j].Contains("class JsonConverter") || 
                                lines[j].Contains("class ToolParameters"))
                            {
                                inAllowedContext = true;
                                break;
                            }
                        }

                        if (!inAllowedContext)
                        {
                            _violations.Add(new RuleViolation
                            {
                                RuleId = "Rule-010",
                                RuleName = "方案系统实现规范",
                                Priority = RulePriority.Critical,
                                FilePath = file,
                                LineNumber = lineNumber,
                                Message = "在业务逻辑中调用 ToSerializableDictionary()，应该直接序列化对象",
                                Suggestion = "直接序列化 ToolParameters 实例，避免 Dictionary 转换层"
                            });
                        }
                    }
                }
            }
        }

        #endregion

        #region Rule-011: 临时文件自动清理规则

        /// <summary>
        /// Rule-011: 临时文件自动清理规则
        /// </summary>
        private void CheckRule011_TempFileCleanup()
        {
            var batFiles = Directory.GetFiles(_projectRoot, "*.bat", SearchOption.TopDirectoryOnly);
            var ps1Files = Directory.GetFiles(_projectRoot, "*.ps1", SearchOption.AllDirectories);

            // 检查批处理脚本
            foreach (var file in batFiles)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                bool hasTempFileCreation = false;
                bool hasTempFileCleanup = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // 检查：在项目目录创建临时文件
                    if (Regex.IsMatch(line, @"set\s+TEMP_\w+=\.\\") ||
                        Regex.IsMatch(line, @"echo.*>.*\\temp") ||
                        Regex.IsMatch(line, @"echo.*>.*\\build"))
                    {
                        hasTempFileCreation = true;
                    }

                    // 检查：清理临时文件
                    if (line.Contains("del") && (line.Contains("temp") || line.Contains("build")))
                    {
                        hasTempFileCleanup = true;
                    }
                }

                if (hasTempFileCreation && !hasTempFileCleanup)
                {
                    _violations.Add(new RuleViolation
                    {
                        RuleId = "Rule-011",
                        RuleName = "临时文件自动清理规则",
                        Priority = RulePriority.High,
                        FilePath = file,
                        LineNumber = 1,
                        Message = "脚本创建了临时文件但没有清理",
                        Suggestion = "在脚本结束前添加清理临时文件的命令"
                    });
                }
            }

            // 检查 PowerShell 脚本
            foreach (var file in ps1Files)
            {
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');

                bool hasTempFileCreation = false;
                bool hasTryFinally = false;
                bool hasCleanup = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // 检查：创建临时文件
                    if (Regex.IsMatch(line, @".*\.(txt|log|tmp)\s*=\s*"))
                    {
                        hasTempFileCreation = true;
                    }

                    // 检查：使用 Try-Finally
                    if (line.Contains("try") || line.Contains("finally"))
                    {
                        hasTryFinally = true;
                    }

                    // 检查：清理文件
                    if (line.Contains("Remove-Item") || line.Contains("del"))
                    {
                        hasCleanup = true;
                    }
                }

                if (hasTempFileCreation && !hasTryFinally)
                {
                    _violations.Add(new RuleViolation
                    {
                        RuleId = "Rule-011",
                        RuleName = "临时文件自动清理规则",
                        Priority = RulePriority.High,
                        FilePath = file,
                        LineNumber = 1,
                        Message = "脚本创建了临时文件但没有使用 Try-Finally 确保清理",
                        Suggestion = "使用 Try-Finally 块确保临时文件被清理"
                    });
                }
            }
        }

        #endregion

        #region Rule-012: 参数系统约束条件

        /// <summary>
        /// Rule-012: 参数系统约束条件
        /// </summary>
        private void CheckRule012_ParameterSystemConstraints()
        {
            // 检查 UI 层 WorkflowNode.cs
            var workflowNodePath = Path.Combine(_projectRoot, "src", "UI", "Models", "WorkflowNode.cs");
            if (File.Exists(workflowNodePath))
            {
                var content = File.ReadAllText(workflowNodePath);

                // 检查：是否使用 Dictionary<string, object> 存储参数
                if (!content.Contains("Dictionary<string, object> Parameters"))
                {
                    _violations.Add(new RuleViolation
                    {
                        RuleId = "Rule-012",
                        RuleName = "参数系统约束条件",
                        Priority = RulePriority.High,
                        FilePath = workflowNodePath,
                        LineNumber = 1,
                        Message = "UI 层没有使用 Dictionary<string, object> 存储参数",
                        Suggestion = "保持 UI 层使用 Dictionary<string, object> 存储参数"
                    });
                }

                // 检查：是否添加了 ParametersTypeName 属性
                if (!content.Contains("ParametersTypeName"))
                {
                    _violations.Add(new RuleViolation
                    {
                        RuleId = "Rule-012",
                        RuleName = "参数系统约束条件",
                        Priority = RulePriority.High,
                        FilePath = workflowNodePath,
                        LineNumber = 1,
                        Message = "UI 层没有添加 ParametersTypeName 属性",
                        Suggestion = "添加 ParametersTypeName 属性用于存储参数类型信息"
                    });
                }
            }

            // 检查工具注册机制
            var toolRegistryPath = Path.Combine(_projectRoot, "src", "Plugin.Infrastructure", "Managers", "Tool", "ToolRegistry.cs");
            if (File.Exists(toolRegistryPath))
            {
                var content = File.ReadAllText(toolRegistryPath);

                // 检查：是否使用字符串匹配提取参数类型
                if (content.Contains("Name.StartsWith(\"IToolPlugin\")"))
                {
                    _violations.Add(new RuleViolation
                    {
                        RuleId = "Rule-012",
                        RuleName = "参数系统约束条件",
                        Priority = RulePriority.High,
                        FilePath = toolRegistryPath,
                        LineNumber = 1,
                        Message = "工具注册使用字符串匹配，应该使用类型比较",
                        Suggestion = "使用类型比较：iface.GetGenericTypeDefinition() == typeof(IToolPlugin<,>)"
                    });
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 规则违规记录
    /// </summary>
    public class RuleViolation
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public RulePriority Priority { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 规则优先级
    /// </summary>
    public enum RulePriority
    {
        Critical = 0,  // 🔴 Critical - 必须立即修复
        High = 1,       // 🟠 High - 应该尽快修复
        Medium = 2,    // 🟡 Medium - 可以延后修复
        Low = 3        // 🟢 Low - 可选修复
    }
}
