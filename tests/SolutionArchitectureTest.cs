using System;
using System.Collections.Generic;
using SunEyeVision.Workflow;
using SunEyeVision.Plugin.SDK.Execution.Parameters;

namespace SunEyeVision.Tests;

/// <summary>
/// 解决方案架构测试
/// </summary>
public class SolutionArchitectureTest
{
    public void TestNewArchitecture()
    {
        // 1. 创建解决方案
        var solution = SolutionFile.CreateNew("测试解决方案");
        solution.Description = "测试与VisionMaster对齐的新架构";

        // 2. 添加工作流
        var workflow1 = solution.AddWorkflow("图像采集");
        var workflow2 = solution.AddWorkflow("缺陷检测");

        // 3. 添加节点
        var node1 = new WorkflowNode(Guid.NewGuid().ToString(), "相机采集", NodeType.AlgorithmNode);
        node1.AlgorithmType = "CameraCapture";
        node1.Parameters = new GenericToolParameters();
        workflow1.Nodes.Add(node1);

        var node2 = new WorkflowNode(Guid.NewGuid().ToString(), "阈值分割", NodeType.AlgorithmNode);
        node2.AlgorithmType = "Threshold";
        node2.Parameters = new GenericToolParameters();
        workflow2.Nodes.Add(node2);

        // 4. 添加连接
        workflow1.Connections.Add(new Connection
        {
            SourceNode = node1.Id,
            TargetNode = node2.Id
        });

        // 5. 添加全局变量
        solution.AddGlobalVariable("ProductType", "TypeA", "String");
        solution.AddGlobalVariable("Threshold", 128, "Int32");

        // 6. 添加设备
        var camera = solution.AddDevice("相机1", DeviceType.Camera);
        camera.IpAddress = "192.168.1.100";
        camera.Port = 8080;

        var light = solution.AddDevice("光源1", DeviceType.LightSource);

        // 7. 添加通讯配置
        var plcComm = solution.AddCommunication("PLC通讯", CommunicationType.ModbusTCP);
        plcComm.Settings["host"] = "192.168.1.200";
        plcComm.Settings["port"] = 502;

        // 8. 配置数据库
        solution.DatabaseConfiguration = new DatabaseConfiguration(DatabaseType.MySQL, "Server=localhost;Database=test;User=root;Password=123456");

        // 9. 配置执行策略
        solution.ExecutionStrategy = new ExecutionStrategy(WorkflowExecutionMode.Sequential);

        // 10. 添加版本历史
        solution.AddVersion("初始版本", new List<string>
        {
            "创建基础架构",
            "添加图像采集工作流",
            "添加缺陷检测工作流"
        });

        // 11. 验证解决方案
        var (isValid, errors) = solution.Validate();
        if (!isValid)
        {
            throw new Exception("解决方案验证失败: " + string.Join(", ", errors));
        }

        Console.WriteLine("解决方案架构测试通过！");
    }

    public void TestRunContext()
    {
        // 1. 创建工作流
        var workflow = new Workflow();
        workflow.Name = "测试工作流";

        // 2. 添加节点
        var node = new WorkflowNode(Guid.NewGuid().ToString(), "测试节点", NodeType.AlgorithmNode);
        node.Parameters = new GenericToolParameters();
        workflow.Nodes.Add(node);

        // 3. 添加全局变量
        var globalVars = new List<GlobalVariable>
        {
            new GlobalVariable("TestVar", "test", "String")
        };

        // 4. 创建执行上下文
        var context = RunContext.Create(workflow, globalVars);

        // 5. 获取节点参数
        var params = context.GetNodeParameters(node.Id);
        if (params == null)
        {
            throw new Exception("节点参数获取失败");
        }

        // 6. 获取全局变量
        var testVar = context.GetGlobalVariable<string>("TestVar");
        if (testVar != "test")
        {
            throw new Exception("全局变量获取失败");
        }

        Console.WriteLine("执行上下文测试通过！");
    }
}
