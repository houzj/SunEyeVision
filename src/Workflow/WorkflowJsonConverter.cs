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
                workflow.Nodes = new List<WorkflowNode>();
                foreach (var nodeElement in nodesElement.EnumerateArray())
                {
                    try
                    {
                        var nodeDict = JsonElementToDictionary(nodeElement);

                        if (nodeDict != null)
                        {
                            var nodeType = nodeDict.TryGetValue("Type", out var typeVal)
                                ? (NodeType)Convert.ToInt32(typeVal)
                                : NodeType.Algorithm;

                            var node = WorkflowNode.FromDictionary(nodeDict, nodeType);
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
        private Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = JsonElementToObject(property.Value);
            }

            return dict;
        }

        /// <summary>
        /// 将 JsonElement 转换为相应的 C# 对象
        /// </summary>
        private object? JsonElementToObject(JsonElement element)
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
                    return JsonElementToDictionary(element);
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(JsonElementToObject(item));
                    }
                    return list;
                default:
                    return element.ToString();
            }
        }
    }
}
