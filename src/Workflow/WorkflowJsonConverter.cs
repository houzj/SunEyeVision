using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

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
            var description = root.TryGetProperty("Description", out var descElement)
                ? descElement.GetString()
                : string.Empty;

            // 创建工作流实例（Logger 暂时为 null，后续由 LoadWorkflow 设置）
            var workflow = new Workflow(id, name, null!);
            workflow.Description = description;

            // 反序列化节点（使用 FromDictionary 方法恢复强类型参数）
            if (root.TryGetProperty("Nodes", out var nodesElement))
            {
                workflow.Nodes = new List<WorkflowNode>();
                foreach (var nodeElement in nodesElement.EnumerateArray())
                {
                    try
                    {
                        var nodeDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            nodeElement.GetRawText(),
                            options
                        );

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
                        System.Diagnostics.Debug.WriteLine($"Failed to deserialize node: {ex.Message}");
                    }
                }
            }

            // 反序列化连接
            if (root.TryGetProperty("Connections", out var connElement))
            {
                try
                {
                    workflow.Connections = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                        connElement.GetRawText(),
                        options
                    ) ?? new Dictionary<string, List<string>>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize connections: {ex.Message}");
                    workflow.Connections = new Dictionary<string, List<string>>();
                }
            }
            else
            {
                workflow.Connections = new Dictionary<string, List<string>>();
            }

            // 反序列化端口连接
            if (root.TryGetProperty("PortConnections", out var portConnElement))
            {
                try
                {
                    workflow.PortConnections = JsonSerializer.Deserialize<List<PortConnection>>(
                        portConnElement.GetRawText(),
                        options
                    ) ?? new List<PortConnection>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize port connections: {ex.Message}");
                    workflow.PortConnections = new List<PortConnection>();
                }
            }
            else
            {
                workflow.PortConnections = new List<PortConnection>();
            }

            return workflow;
        }

        public override void Write(Utf8JsonWriter writer, Workflow value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // 写入基本属性
            writer.WriteString("Id", value.Id);
            writer.WriteString("Name", value.Name);
            writer.WriteString("Description", value.Description ?? string.Empty);

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

            // 写入端口连接
            writer.WritePropertyName("PortConnections");
            JsonSerializer.Serialize(writer, value.PortConnections, options);

            writer.WriteEndObject();
        }
    }
}
