using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Workflow;

/// <summary>
/// Recipe 的自定义 JSON 转换器
/// </summary>
/// <remarks>
/// 处理 Dictionary<string, ToolParameters> 的多态序列化。
/// 
/// 序列化策略：
/// 1. 遍历 ParameterMappings 中的每个 ToolParameters
/// 2. 为每个参数添加 $type 字段（类型程序集限定名）
/// 3. 直接序列化参数的所有属性（无需添加特性）
/// 
/// 反序列化策略：
/// 1. 读取 $type 字段获取类型信息
/// 2. 使用反射创建对应类型的实例
/// 3. 填充参数属性值
/// 
/// 设计优势：
/// - ToolParameters 零修改（不添加任何 JSON 特性）
/// - JSON 格式清晰易读（参数直接展开）
/// - 支持所有 ToolParameters 派生类
/// </remarks>
public class RecipeJsonConverter : JsonConverter<Recipe>
{
    public override Recipe? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var recipe = new Recipe();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "id":
                    recipe.Id = reader.GetString() ?? Guid.NewGuid().ToString();
                    break;
                case "name":
                    recipe.Name = reader.GetString() ?? "新建配方";
                    break;
                case "description":
                    recipe.Description = reader.GetString() ?? string.Empty;
                    break;
                case "parameterMappings":
                    recipe.ParameterMappings = ReadParameterMappings(ref reader, options);
                    break;
            }
        }

        return recipe;
    }

    public override void Write(Utf8JsonWriter writer, Recipe value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("description", value.Description);

        writer.WritePropertyName("parameterMappings");
        WriteParameterMappings(writer, value.ParameterMappings, options);

        writer.WriteEndObject();
    }

    /// <summary>
    /// 读取参数映射（处理 ToolParameters 多态）
    /// </summary>
    private Dictionary<string, ToolParameters> ReadParameterMappings(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var mappings = new Dictionary<string, ToolParameters>();

        if (reader.TokenType != JsonTokenType.StartObject)
            return mappings;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var nodeId = reader.GetString();
            reader.Read();

            var parameters = ReadToolParameters(ref reader, options);
            if (parameters != null)
            {
                mappings[nodeId ?? string.Empty] = parameters;
            }
        }

        return mappings;
    }

    /// <summary>
    /// 写入参数映射（处理 ToolParameters 多态）
    /// </summary>
    private void WriteParameterMappings(Utf8JsonWriter writer, Dictionary<string, ToolParameters> mappings, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in mappings)
        {
            writer.WritePropertyName(kvp.Key);
            WriteToolParameters(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// 读取 ToolParameters（多态反序列化）
    /// </summary>
    private ToolParameters? ReadToolParameters(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        // 读取整个 JSON 对象到 JsonDocument
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        // 读取 $type 字段
        if (!root.TryGetProperty("$type", out var typeProp))
            return null;

        var typeName = typeProp.GetString();
        if (string.IsNullOrEmpty(typeName))
            return null;

        // 使用反射创建实例
        var type = System.Type.GetType(typeName);
        if (type == null || !typeof(ToolParameters).IsAssignableFrom(type))
            return null;

        var parameters = (ToolParameters?)System.Activator.CreateInstance(type);
        if (parameters == null)
            return null;

        // 填充属性值
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name == "$type" || prop.Name == "version" || prop.Name == "context")
                continue;

            var propertyInfo = type.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                try
                {
                    var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), propertyInfo.PropertyType, options);
                    propertyInfo.SetValue(parameters, value);
                }
                catch
                {
                    // 忽略反序列化失败的属性
                }
            }
        }

        return parameters;
    }

    /// <summary>
    /// 写入 ToolParameters（多态序列化）
    /// </summary>
    private void WriteToolParameters(Utf8JsonWriter writer, ToolParameters parameters, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // 写入类型信息
        writer.WriteString("$type", parameters.GetType().AssemblyQualifiedName ?? string.Empty);

        // 写入版本
        writer.WriteNumber("version", parameters.Version);

        // 写入所有属性
        var type = parameters.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.Name == "Version" || prop.Name == "Context")
                continue;

            if (!prop.CanRead)
                continue;

            var value = prop.GetValue(parameters);
            if (value != null)
            {
                writer.WritePropertyName(JsonNamingPolicy.CamelCase.ConvertName(prop.Name));
                JsonSerializer.Serialize(writer, value, options);
            }
        }

        writer.WriteEndObject();
    }
}
