using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow
{
    /// <summary>
    /// 工作流 JSON 转换器 - 直接序列化 WorkflowNodeBase
    /// </summary>
    /// <remarks>
    /// 优化说明（rule-010: 方案系统实现规范）：
    /// - 删除 Dictionary 转换层，直接序列化 WorkflowNodeBase 对象
    /// - System.Text.Json 自动处理多态序列化（如有子类需要 [JsonPolymorphic]）
    /// - Parameters 通过 ToolParameters 自身的 [JsonPolymorphic] 处理多态
    /// </remarks>
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

            // 直接反序列化节点（无需 Dictionary 转换层）
            if (root.TryGetProperty("Nodes", out var nodesElement))
            {
                workflow.Nodes = new ObservableCollection<WorkflowNodeBase>();
                foreach (var nodeElement in nodesElement.EnumerateArray())
                {
                    try
                    {
                        var node = JsonSerializer.Deserialize<WorkflowNodeBase>(
                            nodeElement.GetRawText(), options);
                        if (node != null)
                        {
                            workflow.Nodes.Add(node);
                        }
                    }
                    catch (Exception ex)
                    {
                        VisionLogger.Instance.Log(LogLevel.Warning,
                            $"节点标准反序列化失败，尝试旧格式兼容: {ex.Message}", "WorkflowJsonConverter");

                        // 最终降级：兼容旧格式（Dictionary 方式的 JSON）
                        try
                        {
                            var node = ReadNodeLegacyFormat(nodeElement, options);
                            if (node != null)
                            {
                                workflow.Nodes.Add(node);
                                VisionLogger.Instance.Log(LogLevel.Warning,
                                    $"节点使用旧格式兼容加载: {node.Name}", "WorkflowJsonConverter");
                            }
                        }
                        catch (Exception legacyEx)
                        {
                            VisionLogger.Instance.Log(LogLevel.Error,
                                $"Failed to deserialize node: {ex.Message}, legacy fallback also failed: {legacyEx.Message}",
                                "WorkflowJsonConverter");
                        }
                    }
                }
            }

            // 反序列化连接
            if (root.TryGetProperty("Connections", out var connElement))
            {
                try
                {
                    var connList = JsonSerializer.Deserialize<List<WorkflowConnection>>(
                        connElement.GetRawText(),
                        options
                    );
                    workflow.Connections = connList != null
                        ? new ObservableCollection<WorkflowConnection>(connList)
                        : new ObservableCollection<WorkflowConnection>();
                }
                catch (Exception ex)
                {
                    VisionLogger.Instance.Log(LogLevel.Warning, $"Failed to deserialize connections: {ex.Message}", "WorkflowJsonConverter");
                    workflow.Connections = new ObservableCollection<WorkflowConnection>();
                }
            }
            else
            {
                workflow.Connections = new ObservableCollection<WorkflowConnection>();
            }

            return workflow;
        }

        public override void Write(Utf8JsonWriter writer, Workflow value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // 写入基本属性
            writer.WriteString("Id", value.Id);
            writer.WriteString("Name", value.Name);

            // 直接序列化节点（无需 Dictionary 转换层）
            writer.WritePropertyName("Nodes");
            writer.WriteStartArray();
            foreach (var node in value.Nodes)
            {
                JsonSerializer.Serialize(writer, node, options);
            }
            writer.WriteEndArray();

            // 写入连接
            writer.WritePropertyName("Connections");
            JsonSerializer.Serialize(writer, value.Connections, options);

            writer.WriteEndObject();
        }

        /// <summary>
        /// 兼容旧格式（Dictionary 方式的 JSON）的节点加载
        /// </summary>
        private WorkflowNodeBase? ReadNodeLegacyFormat(JsonElement nodeElement, JsonSerializerOptions options)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var property in nodeElement.EnumerateObject())
            {
                if (property.Name == "Parameters")
                {
                    try
                    {
                        var parameters = JsonSerializer.Deserialize<ToolParameters>(
                            property.Value.GetRawText(), options);
                        dict[property.Name] = parameters ?? new GenericToolParameters();
                    }
                    catch
                    {
                        dict[property.Name] = new GenericToolParameters();
                    }
                }
                else
                {
                    dict[property.Name] = JsonElementToSimpleObject(property.Value);
                }
            }

            return WorkflowNodeBase.FromDictionary(dict!);
        }

        /// <summary>
        /// 将 JsonElement 转换为简单对象（仅用于旧格式兼容）
        /// </summary>
        private static object? JsonElementToSimpleObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }
    }
}
