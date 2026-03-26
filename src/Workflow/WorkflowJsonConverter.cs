using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Core.Models;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流 JSON 转换器 - 支持强类型参数的序列化/反序列化
    /// </summary>
    public class WorkflowJsonConverter : JsonConverter<Workflow>
    {
        public override Workflow Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token");
            }

            var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;

            // 读取基本属性
            var id = root.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString();
            var name = root.GetProperty("Name").GetString() ?? "Unnamed Workflow";

            // 创建工作流实例
            var workflow = new Workflow(id, name);

            // 反序列化节点（使用 FromDictionary 方法恢复强类型参数）
            if (root.TryGetProperty("Nodes", out var nodesElement))
            {
                workflow.Nodes = new List<WorkflowNodeBase>();
                foreach (var nodeElement in nodesElement.EnumerateArray())
                {
                    try
                    {
                        var nodeDict = JsonElementToDictionary(nodeElement, options);

                        if (nodeDict != null)
                        {
                            var node = WorkflowNodeBase.FromDictionary(nodeDict);
                            workflow.Nodes.Add(node);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 跳过无法解析的节点，继续处理其他节点
                        VisionLogger.Instance.Log(LogLevel.Warning, $"Failed to deserialize node: {ex.Message}", "WorkflowJsonConverter");
                    }
                }
            }

            // 反序列化连接
            if (root.TryGetProperty("Connections", out var connElement))
            {
                try
                {
                    workflow.Connections = JsonSerializer.Deserialize<List<Connection>>(
                        connElement.GetRawText(),
                        options
                    ) ?? new List<Connection>();
                }
                catch (Exception ex)
                {
                    VisionLogger.Instance.Log(LogLevel.Warning, $"Failed to deserialize connections: {ex.Message}", "WorkflowJsonConverter");
                    workflow.Connections = new List<Connection>();
                }
            }
            else
            {
                workflow.Connections = new List<Connection>();
            }

            return workflow;
        }

        public override void Write(Utf8JsonWriter writer, Workflow value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // 写入基本属性
            writer.WriteString("Id", value.Id);
            writer.WriteString("Name", value.Name);

            // 写入节点（使用 ToDictionary 方法保留强类型信息）
            writer.WritePropertyName("Nodes");
            writer.WriteStartArray();
            foreach (var node in value.Nodes)
            {
                var nodeDict = node.ToDictionary();
                JsonSerializer.Serialize(writer, nodeDict, options);
            }
            writer.WriteEndArray();

            // 写入连接
            writer.WritePropertyName("Connections");
            JsonSerializer.Serialize(writer, value.Connections, options);

            // PortConnections 已移除
            // writer.WritePropertyName("PortConnections");
            // JsonSerializer.Serialize(writer, value.PortConnections, options);

            writer.WriteEndObject();
        }

        /// <summary>
        /// 将 JsonElement 转换为 Dictionary<string, object>
        /// </summary>
        /// <remarks>
        /// 特殊处理：Parameters 字段直接反序列化为 ToolParameters 对象
        /// </remarks>
        private Dictionary<string, object?> JsonElementToDictionary(JsonElement element, JsonSerializerOptions options)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var property in element.EnumerateObject())
            {
                // 特殊处理 Parameters 字段：直接反序列化为 ToolParameters 对象
                if (property.Name == "Parameters")
                {
                    VisionLogger.Instance.Log(LogLevel.Info,
                        $"📥 [参数反序列化开始] 准备反序列化 Parameters 字段",
                        "WorkflowJsonConverter");
                    
                    // 🔍 关键诊断：输出原始 JSON 内容
                    var rawText = property.Value.GetRawText();
                    VisionLogger.Instance.Log(LogLevel.Info,
                        $"📥 [参数反序列化] 原始 JSON: {rawText.Substring(0, Math.Min(200, rawText.Length))}...",
                        "WorkflowJsonConverter");
                    
                    // 🔍 关键诊断：检查 options 中的 TypeInfoResolver
                    VisionLogger.Instance.Log(LogLevel.Info,
                        $"📥 [参数反序列化] JsonSerializerOptions 配置: " +
                        $"WriteIndented={options.WriteIndented}, " +
                        $"PropertyNamingPolicy={options.PropertyNamingPolicy?.GetType().Name ?? "null"}, " +
                        $"TypeInfoResolver={options.TypeInfoResolver?.GetType().Name ?? "null"}",
                        "WorkflowJsonConverter");
                    
                    try
                    {
                        var parameters = JsonSerializer.Deserialize<ToolParameters>(
                            property.Value.GetRawText(),
                            options
                        );
                        if (parameters != null)
                        {
                            // 🔍 详细日志：参数反序列化成功
                            var paramType = parameters.GetType().Name;
                            var paramValues = parameters.GetParameterSummary();
                            VisionLogger.Instance.Log(LogLevel.Success,
                                $"✅ [参数反序列化成功] 类型: {paramType} | 值: {paramValues}",
                                "WorkflowJsonConverter");
                            dict[property.Name] = parameters;
                        }
                        else
                        {
                            VisionLogger.Instance.Log(LogLevel.Warning,
                                $"⚠️ [参数反序列化失败] 结果为 null，使用 GenericToolParameters",
                                "WorkflowJsonConverter");
                            dict[property.Name] = new GenericToolParameters();
                        }
                    }
                    catch (Exception ex)
                    {
                        VisionLogger.Instance.Log(LogLevel.Error,
                            $"❌ [参数反序列化异常] 错误: {ex.Message}",
                            "WorkflowJsonConverter", ex);
                        VisionLogger.Instance.Log(LogLevel.Warning,
                            $"⚠️ [参数反序列化失败] 使用 GenericToolParameters 作为回退",
                            "WorkflowJsonConverter");
                        dict[property.Name] = new GenericToolParameters();
                    }
                }
                else
                {
                    dict[property.Name] = JsonElementToObject(property.Value, options);
                }
            }

            return dict;
        }

        /// <summary>
        /// 将 JsonElement 转换为相应的 C# 对象
        /// </summary>
        private object? JsonElementToObject(JsonElement element, JsonSerializerOptions options)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intVal))
                        return intVal;
                    if (element.TryGetInt64(out long longVal))
                        return longVal;
                    if (element.TryGetDouble(out double doubleVal))
                        return doubleVal;
                    return element.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    return JsonElementToDictionary(element, options);
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(JsonElementToObject(item, options));
                    }
                    return list;
                default:
                    return element.ToString();
            }
        }
    }
}
