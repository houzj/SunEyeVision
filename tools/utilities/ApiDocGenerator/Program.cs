using System;

using System.IO;

using System.Xml;

using System.Xml.XPath;

using System.Collections.Generic;

using System.Text;

using System.Text.RegularExpressions;

using System.Linq;

namespace ApiDocGenerator

{

    /// <summary>

    /// API 文档生成器 - 从 XML 注释生成 HTML 文档

    /// </summary>

    class Program

    {

        static void Main(string[] args)

        {

            // 配置路径

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;

            string solutionDir = Path.GetFullPath(Path.Combine(currentDir, "../../../"));

            string sourceDir = Path.Combine(solutionDir, "Help/Source/zh-CN/api");

            string outputDir = Path.Combine(solutionDir, "Help/Output");

            // XML 文档路径 - 使用 tools 目录下的副本

            var xmlFiles = new[]

            {

                Path.Combine(solutionDir, "tools/SunEyeVision.Core.xml"),

                Path.Combine(solutionDir, "tools/SunEyeVision.Algorithms.xml"),

                Path.Combine(solutionDir, "tools/SunEyeVision.Workflow.xml"),

                Path.Combine(solutionDir, "tools/SunEyeVision.Plugin.xml"),

                Path.Combine(solutionDir, "tools/SunEyeVision.UI.xml")

            };

            // 生成各个模块的 API 文档

            GenerateApiDocumentation("Core", xmlFiles[0], sourceDir, solutionDir);

            GenerateApiDocumentation("Algorithms", xmlFiles[1], sourceDir, solutionDir);

            GenerateApiDocumentation("Workflow", xmlFiles[2], sourceDir, solutionDir);

            GenerateApiDocumentation("Plugins", xmlFiles[3], sourceDir, solutionDir);

        }

        /// <summary>

        /// 生成指定模块的 API 文档

        /// </summary>

        static void GenerateApiDocumentation(string moduleName, string xmlPath, string outputDir, string solutionDir)

        {

            if (!File.Exists(xmlPath))

            {

                return;

            }

            try

            {

                var doc = new XmlDocument();

                doc.Load(xmlPath);

                var members = doc.SelectNodes("//member");

                if (members == null || members.Count == 0)

                {

                    return;

                }

                // 分类收集成员

                var namespaces = new Dictionary<string, List<XmlNode>>();

                foreach (XmlNode member in members)

                {

                    var name = member.Attributes?["name"]?.Value ?? "";

                    if (string.IsNullOrEmpty(name)) continue;

                    string ns = ExtractNamespace(name);

                    if (!namespaces.ContainsKey(ns))

                    {

                        namespaces[ns] = new List<XmlNode>();

                    }

                    namespaces[ns].Add(member);

                }

                // 生成 HTML 文档

                string outputFile = Path.Combine(outputDir, $"{moduleName.ToLower()}.html");

                GenerateHtml(moduleName, namespaces, outputFile);

            }

            catch (Exception ex)

            {

            }

        }

        /// <summary>

        /// 从成员名称中提取命名空间

        /// </summary>

        static string ExtractNamespace(string memberName)

        {

            if (memberName.StartsWith("N:"))

            {

                // 命名空间

                return memberName.Substring(2);

            }

            else if (memberName.StartsWith("T:"))

            {

                // 类型

                var typeName = memberName.Substring(2);

                var lastDot = typeName.LastIndexOf('.');

                return lastDot > 0 ? typeName.Substring(0, lastDot) : "";

            }

            else if (memberName.StartsWith("M:") || memberName.StartsWith("P:") ||

                     memberName.StartsWith("F:") || memberName.StartsWith("E:"))

            {

                // 方法、属性、字段、事件

                var memberNameOnly = memberName.Substring(2);

                var lastDot = memberNameOnly.LastIndexOf('.');

                if (lastDot > 0)

                {

                    var fullType = memberNameOnly.Substring(0, lastDot);

                    var typeLastDot = fullType.LastIndexOf('.');

                    return typeLastDot > 0 ? fullType.Substring(0, typeLastDot) : "";

                }

            }

            return "";

        }

        /// <summary>

        /// 生成 HTML 文档

        /// </summary>

        static void GenerateHtml(string moduleName, Dictionary<string, List<XmlNode>> namespaces, string outputPath)

        {

            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");

            html.AppendLine("<html lang=\"zh-CN\">");

            html.AppendLine("<head>");

            html.AppendLine("    <meta charset=\"UTF-8\">");

            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");

            html.AppendLine($"    <title>{moduleName} 模块 - SunEyeVision API 文档</title>");

            html.AppendLine("    <link rel=\"stylesheet\" href=\"../styles.css\">");

            html.AppendLine("</head>");

            html.AppendLine("<body>");

            // 侧边栏

            html.AppendLine("    <div class=\"container\">");

            html.AppendLine("        <nav class=\"sidebar\">");

            html.AppendLine("            <div class=\"sidebar-header\">");

            html.AppendLine("                <h1>SunEyeVision</h1>");

            html.AppendLine("            </div>");

            html.AppendLine("            <div class=\"nav-menu\">");

            html.AppendLine("                <div class=\"nav-item\" data-page=\"index\">?? API 文档</div>");

            html.AppendLine("                <div class=\"nav-item\" data-page=\"api/core\">核心模块</div>");

            html.AppendLine("                <div class=\"nav-item\" data-page=\"api/algorithms\">算法模块</div>");

            html.AppendLine("                <div class=\"nav-item\" data-page=\"api/workflow\">工作流模块</div>");

            html.AppendLine("                <div class=\"nav-item\" data-page=\"api/plugins\">插件系统</div>");

            html.AppendLine("            </div>");

            html.AppendLine("        </nav>");

            // 主内容

            html.AppendLine("        <main class=\"main-content\">");

            html.AppendLine($"            <header class=\"content-header\">");

            html.AppendLine($"                <h1>{moduleName} 模块 API 文档</h1>");

            html.AppendLine($"                <div class=\"subtitle\">SunEyeVision {moduleName} 模块详细说明</div>");

            html.AppendLine($"            </header>");

            foreach (var ns in namespaces)

            {

                html.AppendLine($"            <section class=\"content-section\">");

                html.AppendLine($"                <h2>命名空间: {ns.Key}</h2>");

                var types = ns.Value.Where(m => m.Attributes?["name"]?.Value?.StartsWith("T:") ?? false);

                foreach (XmlNode type in types)

                {

                    var typeName = type.Attributes?["name"]?.Value?.Substring(2) ?? "";

                    var summary = GetElementText(type, "summary");

                    html.AppendLine($"                <h3>{ExtractTypeName(typeName)}</h3>");

                    html.AppendLine($"                <div class=\"api-signature\">");

                    html.AppendLine($"                    <code>{typeName}</code>");

                    html.AppendLine($"                </div>");

                    html.AppendLine($"                <p>{FormatText(summary)}</p>");

                    // 成员

                    var members = ns.Value.Where(m =>

                    {

                        var name = m.Attributes?["name"]?.Value ?? "";

                        return name.Contains(typeName) && !name.StartsWith("T:");

                    });

                    if (members.Any())

                    {

                        html.AppendLine($"                <h4>成员</h4>");

                        html.AppendLine($"                <ul>");

                        foreach (XmlNode member in members)

                        {

                            var memberName = member.Attributes?["name"]?.Value ?? "";

                            var memberSummary = GetElementText(member, "summary");

                            html.AppendLine($"                    <li><strong>{ExtractMemberName(memberName)}</strong> - {FormatText(memberSummary)}</li>");

                        }

                        html.AppendLine($"                </ul>");

                    }

                }

                html.AppendLine($"            </section>");

            }

            html.AppendLine("            <footer class=\"footer\">");

            html.AppendLine($"                <p>&copy; 2026 SunEyeVision. 保留所有权利。</p>");

            html.AppendLine($"            </footer>");

            html.AppendLine("        </main>");

            html.AppendLine("    </div>");

            // 简单的导航脚本

            html.AppendLine("    <script>");

            html.AppendLine("        document.querySelectorAll('.nav-item').forEach(item => {");

            html.AppendLine("            item.addEventListener('click', function() {");

            html.AppendLine("                const page = this.getAttribute('data-page');");

            html.AppendLine("                window.location.href = page + '.html';");

            html.AppendLine("            });");

            html.AppendLine("        });");

            html.AppendLine("    </script>");

            html.AppendLine("</body>");

            html.AppendLine("</html>");

            // 确保输出目录存在

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            File.WriteAllText(outputPath, html.ToString(), Encoding.UTF8);

        }

        /// <summary>

        /// 提取类型名称(不含命名空间)

        /// </summary>

        static string ExtractTypeName(string fullTypeName)

        {

            var lastDot = fullTypeName.LastIndexOf('.');

            return lastDot > 0 ? fullTypeName.Substring(lastDot + 1) : fullTypeName;

        }

        /// <summary>

        /// 提取成员名称

        /// </summary>

        static string ExtractMemberName(string fullMemberName)

        {

            // 移除前缀和参数

            var name = Regex.Replace(fullMemberName, "^[MTFPE]:", "");

            name = Regex.Replace(name, "\\(.*\\)", "");

            var lastDot = name.LastIndexOf('.');

            return lastDot > 0 ? name.Substring(lastDot + 1) : name;

        }

        /// <summary>

        /// 获取 XML 元素的文本内容

        /// </summary>

        static string GetElementText(XmlNode parent, string elementName)

        {

            var element = parent.SelectSingleNode(elementName);

            return element?.InnerText.Trim() ?? "";

        }

        /// <summary>

        /// 格式化文本(移除多余空格和换行)

        /// </summary>

        static string FormatText(string text)

        {

            if (string.IsNullOrWhiteSpace(text))

                return "";

            return Regex.Replace(text, @"\s+", " ");

        }

    }

}

