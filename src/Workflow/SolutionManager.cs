using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案管理器
/// </summary>
public class SolutionManager
{
    private readonly string _solutionDirectory;
    private readonly ILogger _logger;
    private readonly Dictionary<string, Solution> _solutions;
    private SolutionConfigurationTable _configurationTable;

    /// <summary>
    /// 解决方案管理器
    /// </summary>
    public SolutionManager(string solutionDirectory)
    {
        _solutionDirectory = solutionDirectory;
        _logger = VisionLogger.Instance;
        _solutions = new Dictionary<string, Solution>();
        _configurationTable = new SolutionConfigurationTable();

        LoadAllSolutions();
    }

    /// <summary>
    /// 创建新解决方案
    /// </summary>
    public Solution CreateSolution(string name, string description = "")
    {
        var solution = new Solution
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        _solutions[solution.Id] = solution;
        SaveSolution(solution);

        _logger.Log(LogLevel.Info, $"创建解决方案: {name}", "SolutionManager");
        return solution;
    }

    /// <summary>
    /// 保存解决方案
    /// </summary>
    public void SaveSolution(Solution solution)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        solution.ModifiedAt = DateTime.Now;

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var filePath = GetSolutionFilePath(solution.Id);
        var json = JsonSerializer.Serialize(solution, jsonOptions);
        File.WriteAllText(filePath, json);

        _solutions[solution.Id] = solution;
        _logger.Log(LogLevel.Success, $"保存解决方案: {solution.Name}", "SolutionManager");
    }

    /// <summary>
    /// 加载解决方案
    /// </summary>
    public Solution? LoadSolution(string solutionId)
    {
        if (_solutions.TryGetValue(solutionId, out var solution))
            return solution;

        var filePath = GetSolutionFilePath(solutionId);
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        solution = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
        if (solution != null)
        {
            _solutions[solution.Id] = solution;
            _logger.Log(LogLevel.Info, $"加载解决方案: {solution.Name}", "SolutionManager");
        }

        return solution;
    }

    /// <summary>
    /// 删除解决方案
    /// </summary>
    public void DeleteSolution(string solutionId)
    {
        if (!_solutions.ContainsKey(solutionId))
            throw new ArgumentException($"解决方案 {solutionId} 不存在");

        var filePath = GetSolutionFilePath(solutionId);
        if (File.Exists(filePath))
            File.Delete(filePath);

        _solutions.Remove(solutionId);
        _logger.Log(LogLevel.Warning, $"删除解决方案: {solutionId}", "SolutionManager");
    }

    /// <summary>
    /// 加载所有解决方案
    /// </summary>
    public void LoadAllSolutions()
    {
        if (!Directory.Exists(_solutionDirectory))
            Directory.CreateDirectory(_solutionDirectory);

        var files = Directory.GetFiles(_solutionDirectory, "*.json");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var solution = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
                if (solution != null && !string.IsNullOrEmpty(solution.Id))
                {
                    _solutions[solution.Id] = solution;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"加载解决方案失败: {file}, 错误: {ex.Message}", "SolutionManager", ex);
            }
        }

        _logger.Log(LogLevel.Info, $"加载了 {_solutions.Count} 个解决方案", "SolutionManager");
    }

    /// <summary>
    /// 获取所有解决方案
    /// </summary>
    public IReadOnlyList<Solution> GetAllSolutions()
    {
        return _solutions.Values.ToList();
    }

    /// <summary>
    /// 导入解决方案
    /// </summary>
    public Solution ImportSolution(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"文件不存在: {filePath}");

        var json = File.ReadAllText(filePath);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var solution = JsonSerializer.Deserialize<Solution>(json, jsonOptions);
        if (solution == null)
            throw new InvalidOperationException("反序列化失败");

        // 生成新的ID避免冲突
        solution.Id = Guid.NewGuid().ToString();
        solution.CreatedAt = DateTime.Now;
        solution.ModifiedAt = DateTime.Now;

        SaveSolution(solution);
        _logger.Log(LogLevel.Success, $"导入解决方案: {solution.Name}", "SolutionManager");

        return solution;
    }

    /// <summary>
    /// 导出解决方案
    /// </summary>
    public void ExportSolution(string solutionId, string exportPath)
    {
        var solution = LoadSolution(solutionId);
        if (solution == null)
            throw new ArgumentException($"解决方案 {solutionId} 不存在");

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(solution, jsonOptions);
        File.WriteAllText(exportPath, json);

        _logger.Log(LogLevel.Success, $"导出解决方案: {solution.Name} 到 {exportPath}", "SolutionManager");
    }

    /// <summary>
    /// 绑定产品ID到解决方案ID
    /// </summary>
    public void BindProductToSolution(string productId, string solutionId)
    {
        _configurationTable.SetMapping(productId, solutionId);
        _logger.Log(LogLevel.Info, $"绑定产品 {productId} 到解决方案 {solutionId}", "SolutionManager");
    }

    /// <summary>
    /// 根据产品ID获取解决方案ID
    /// </summary>
    public string? GetSolutionIdByProduct(string productId)
    {
        return _configurationTable.GetSolutionId(productId);
    }

    /// <summary>
    /// 根据产品ID加载解决方案
    /// </summary>
    public Solution? LoadSolutionByProduct(string productId)
    {
        var solutionId = _configurationTable.GetSolutionId(productId);
        if (string.IsNullOrEmpty(solutionId))
            return null;

        return LoadSolution(solutionId);
    }

    /// <summary>
    /// 获取解决方案文件路径
    /// </summary>
    private string GetSolutionFilePath(string solutionId)
    {
        return Path.Combine(_solutionDirectory, $"{solutionId}.json");
    }
}
