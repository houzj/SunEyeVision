using System;
using System.Collections.Generic;
using System.Linq;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Workflow;

/// <summary>
/// 解决方案元数据注册表
/// </summary>
/// <remarks>
/// 职责：解决方案元数据的注册和查询
///
/// 特性：
/// - 内存注册表（快速查找）
/// - 线程安全访问（使用 ReaderWriterLockSlim）
/// - 支持批量注册和查询
///
/// 设计原则（rule-002）：
/// - 命名符合视觉软件行业标准
/// - 方法使用 PascalCase，动词开头
///
/// 日志规范（rule-003）：
/// - 使用 VisionLogger 记录日志
/// - 使用适当的日志级别（Info/Success/Warning/Error）
/// </remarks>
public class SolutionRegistry
{
    private readonly Dictionary<string, SolutionMetadata> _metadataMap;
    private readonly System.Threading.ReaderWriterLockSlim _lock;
    private readonly ILogger _logger;

    /// <summary>
    /// 获取已注册的元数据数量
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _metadataMap.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SolutionRegistry()
    {
        _metadataMap = new Dictionary<string, SolutionMetadata>();
        _lock = new System.Threading.ReaderWriterLockSlim();
        _logger = VisionLogger.Instance;
        _logger.Log(LogLevel.Info, "解决方案元数据注册表初始化完成", "SolutionRegistry");
    }

    /// <summary>
    /// 注册元数据
    /// </summary>
    /// <param name="metadata">元数据对象</param>
    /// <returns>是否成功注册（true=新增，false=更新）</returns>
    public bool Register(SolutionMetadata metadata)
    {
        if (metadata == null)
        {
            _logger.Log(LogLevel.Warning, "注册元数据失败：元数据对象为空", "SolutionRegistry");
            return false;
        }

        if (string.IsNullOrEmpty(metadata.Id))
        {
            _logger.Log(LogLevel.Warning, "注册元数据失败：解决方案ID为空", "SolutionRegistry");
            return false;
        }

        _lock.EnterWriteLock();
        try
        {
            bool isNew = !_metadataMap.ContainsKey(metadata.Id);
            _metadataMap[metadata.Id] = metadata;

            if (isNew)
            {
                _logger.Log(LogLevel.Success, $"注册新解决方案元数据: {metadata.Name} (Id={metadata.Id})", "SolutionRegistry");
            }
            else
            {
                _logger.Log(LogLevel.Info, $"更新解决方案元数据: {metadata.Name} (Id={metadata.Id})", "SolutionRegistry");
            }

            return isNew;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 批量注册元数据
    /// </summary>
    /// <param name="metadataList">元数据列表</param>
    /// <returns>成功注册的数量</returns>
    public int RegisterBatch(IEnumerable<SolutionMetadata> metadataList)
    {
        if (metadataList == null)
        {
            _logger.Log(LogLevel.Warning, "批量注册元数据失败：列表为空", "SolutionRegistry");
            return 0;
        }

        int successCount = 0;
        _lock.EnterWriteLock();
        try
        {
            foreach (var metadata in metadataList)
            {
                if (metadata != null && !string.IsNullOrEmpty(metadata.Id))
                {
                    _metadataMap[metadata.Id] = metadata;
                    successCount++;
                }
            }

            _logger.Log(LogLevel.Success, $"批量注册元数据完成: 成功 {successCount}/{metadataList.Count()}", "SolutionRegistry");
            return successCount;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 注销元数据
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>是否成功注销</returns>
    public bool Unregister(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
        {
            _logger.Log(LogLevel.Warning, "注销元数据失败：解决方案ID为空", "SolutionRegistry");
            return false;
        }

        _lock.EnterWriteLock();
        try
        {
            bool removed = _metadataMap.Remove(solutionId);
            if (removed)
            {
                _logger.Log(LogLevel.Info, $"注销解决方案元数据: {solutionId}", "SolutionRegistry");
            }
            else
            {
                _logger.Log(LogLevel.Warning, $"注销元数据失败：解决方案不存在: {solutionId}", "SolutionRegistry");
            }
            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 获取指定解决方案的元数据
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>元数据对象，不存在返回 null</returns>
    public SolutionMetadata? Get(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
            return null;

        _lock.EnterReadLock();
        try
        {
            if (_metadataMap.TryGetValue(solutionId, out var metadata))
            {
                return metadata.Clone();
            }
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取所有元数据
    /// </summary>
    /// <returns>元数据列表</returns>
    public List<SolutionMetadata> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            return _metadataMap.Values.Select(m => m.Clone()).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 查询元数据（条件过滤）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <returns>匹配的元数据列表</returns>
    public List<SolutionMetadata> Query(Func<SolutionMetadata, bool> predicate)
    {
        if (predicate == null)
        {
            _logger.Log(LogLevel.Warning, "查询元数据失败：查询条件为空", "SolutionRegistry");
            return new List<SolutionMetadata>();
        }

        _lock.EnterReadLock();
        try
        {
            return _metadataMap.Values
                .Where(predicate)
                .Select(m => m.Clone())
                .ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 检查解决方案是否已注册
    /// </summary>
    /// <param name="solutionId">解决方案ID</param>
    /// <returns>是否已注册</returns>
    public bool Contains(string solutionId)
    {
        if (string.IsNullOrEmpty(solutionId))
            return false;

        _lock.EnterReadLock();
        try
        {
            return _metadataMap.ContainsKey(solutionId);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 清空所有元数据
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            int count = _metadataMap.Count;
            _metadataMap.Clear();
            _logger.Log(LogLevel.Info, $"清空解决方案元数据注册表: 清空了 {count} 条记录", "SolutionRegistry");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 按名称搜索元数据
    /// </summary>
    /// <param name="name">解决方案名称（支持模糊匹配）</param>
    /// <returns>匹配的元数据列表</returns>
    public List<SolutionMetadata> SearchByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return GetAll();

        return Query(m => m.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 按文件路径搜索元数据
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>匹配的元数据，不存在返回 null</returns>
    public SolutionMetadata? SearchByFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        return Query(m => m.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    /// <summary>
    /// 获取最近使用的元数据列表
    /// </summary>
    /// <param name="maxCount">最大数量（默认10）</param>
    /// <returns>最近使用的元数据列表</returns>
    public List<SolutionMetadata> GetRecent(int maxCount = 10)
    {
        return Query(m => !string.IsNullOrEmpty(m.FilePath))
            .OrderByDescending(m => m.LastAccessTime)
            .Take(maxCount)
            .ToList();
    }
}
