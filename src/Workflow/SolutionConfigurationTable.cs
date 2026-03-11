using System.Collections.Generic;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案配置表（产品ID -> 解决方案ID映射）
/// </summary>
/// <remarks>
/// 用于通信时根据产品ID自动切换解决方案。
/// 
/// 使用场景：
/// 1. 扫码枪扫描产品ID（如"PROD_001"）
/// 2. 通过配置表查找对应的解决方案ID
/// 3. 加载并切换到该解决方案
/// 4. 应用该方案的参数配置执行检测
/// 
/// 配置文件位置：solutions/config_table.json
/// </remarks>
public class SolutionConfigurationTable
{
    /// <summary>
    /// 产品ID到解决方案ID的映射
    /// </summary>
    public Dictionary<string, string> Mappings { get; set; } = new();

    /// <summary>
    /// 获取产品对应的解决方案ID
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <returns>解决方案ID，如果不存在则返回null</returns>
    public string? GetSolutionId(string productId)
    {
        return Mappings.TryGetValue(productId, out var solutionId) ? solutionId : null;
    }

    /// <summary>
    /// 设置产品映射
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <param name="solutionId">解决方案ID</param>
    public void SetMapping(string productId, string solutionId)
    {
        Mappings[productId] = solutionId;
    }

    /// <summary>
    /// 移除产品映射
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveMapping(string productId)
    {
        return Mappings.Remove(productId);
    }

    /// <summary>
    /// 检查产品是否存在映射
    /// </summary>
    /// <param name="productId">产品ID</param>
    /// <returns>是否存在映射</returns>
    public bool HasMapping(string productId)
    {
        return Mappings.ContainsKey(productId);
    }
}
