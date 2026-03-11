using System;
using System.Text.Json;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // 测试1: JSON 对象反序列化为 Dictionary<string, object>
        string json1 = @"{
    ""Id"": ""workflow-001"",
    ""Name"": ""测试工作流"",
    ""Count"": 10,
    ""Enabled"": true
        }";

        Console.WriteLine("=== 测试1: 反序列化为 Dictionary<string, object> ===");
        var dict1 = JsonSerializer.Deserialize<Dictionary<string, object>>(json1);
        Console.WriteLine($"类型: {dict1.GetType().FullName}");
        Console.WriteLine($"Id: {dict1["Id"]} (类型: {dict1["Id"].GetType().Name})");
        Console.WriteLine($"Count: {dict1["Count"]} (类型: {dict1["Count"].GetType().Name})");
        Console.WriteLine($"Enabled: {dict1["Enabled"]} (类型: {dict1["Enabled"].GetType().Name})");
        Console.WriteLine();

        // 测试2: 嵌套字典结构
        string json2 = @"{
    ""Workflow"": {
        ""Id"": ""workflow-001"",
        ""Nodes"": [
            {
                ""Id"": ""node-001"",
                ""Name"": ""阈值处理"",
                ""Parameters"": {
                    ""Threshold"": 128,
                    ""MaxValue"": 255
                }
            }
        ]
    }
        }";

        Console.WriteLine("=== 测试2: 嵌套字典结构 ===");
        var dict2 = JsonSerializer.Deserialize<Dictionary<string, object>>(json2);
        var workflow = (JsonElement)dict2["Workflow"];
        Console.WriteLine($"Workflow 类型: {workflow.GetType().FullName}");
        Console.WriteLine($"Workflow.Id: {workflow.GetProperty("Id")}");
        Console.WriteLine($"Workflow.Nodes 类型: {workflow.GetProperty("Nodes").ValueKind}");
        Console.WriteLine();

        // 测试3: 序列化字典
        var dict3 = new Dictionary<string, object>
        {
            ["Id"] = "test-001",
            ["Name"] = "测试",
            ["Data"] = new Dictionary<string, object>
            {
                ["Key1"] = "Value1",
                ["Key2"] = 123
            }
        };

        Console.WriteLine("=== 测试3: 序列化字典 ===");
        var json3 = JsonSerializer.Serialize(dict3, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json3);
        Console.WriteLine();

        // 测试4: 反序列化后又序列化
        Console.WriteLine("=== 测试4: 往返测试 ===");
        var originalDict = new Dictionary<string, object>
        {
            ["String"] = "Hello",
            ["Int"] = 42,
            ["Bool"] = true,
            ["Double"] = 3.14
        };

        var json4 = JsonSerializer.Serialize(originalDict);
        var restoredDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json4);

        foreach (var kvp in restoredDict)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value} (类型: {kvp.Value.GetType().Name})");
        }
    }
}
