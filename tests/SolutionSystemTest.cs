using System;
using System.Text.Json;
using SunEyeVision.Workflow;
using SunEyeVision.Plugin.SDK.Logging;

/// <summary>
/// 方案系统集成测试
/// </summary>
public class SolutionSystemTest
{
    private readonly ILogger _logger;

    public SolutionSystemTest()
    {
        _logger = VisionLogger.Instance;
    }

    /// <summary>
    /// 测试方案系统基本功能
    /// </summary>
    public void TestBasicFunctionality()
    {
        _logger.Log(LogLevel.Info, "开始测试方案系统...", "SolutionSystemTest");

        // 1. 创建解决方案
        var solution = new Solution
        {
            Name = "测试方案",
            Description = "用于测试方案系统"
        };

        _logger.Log(LogLevel.Success, $"创建解决方案: {solution.Name}", "SolutionSystemTest");

        // 2. 创建配方
        var recipe = new Recipe
        {
            Name = "测试配方",
            Description = "用于测试配方"
        };

        _logger.Log(LogLevel.Success, $"创建配方: {recipe.Name}", "SolutionSystemTest");

        // 3. 测试全局变量
        var globalVariable = new GlobalVariable
        {
            Name = "TestVar",
            Type = "int",
            Value = "100",
            Description = "测试变量"
        };

        _logger.Log(LogLevel.Success, $"创建全局变量: {globalVariable.Name}", "SolutionSystemTest");

        // 4. 测试配置表
        var configTable = new SolutionConfigurationTable();
        configTable.SetMapping("PROD_001", solution.Id);

        var mappedSolutionId = configTable.GetSolutionId("PROD_001");
        _logger.Log(LogLevel.Success, $"配置表映射: PROD_001 -> {mappedSolutionId}", "SolutionSystemTest");

        // 5. 测试JSON序列化
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(solution, jsonOptions);
        _logger.Log(LogLevel.Success, "解决方案序列化成功", "SolutionSystemTest");

        var deserialized = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
        if (deserialized != null && deserialized.Id == solution.Id)
        {
            _logger.Log(LogLevel.Success, "解决方案反序列化成功", "SolutionSystemTest");
        }

        _logger.Log(LogLevel.Success, "方案系统测试完成！", "SolutionSystemTest");
    }
}
