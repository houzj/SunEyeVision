using System;

namespace SunEyeVision.Core.IO
{
    /// <summary>
    /// 文件访问意图类型
    /// </summary>
    public enum FileAccessIntent
    {
        /// <summary>读取访问</summary>
        Read,
        /// <summary>写入访问</summary>
        Write,
        /// <summary>删除访问</summary>
        Delete,
        /// <summary>查询访问（检查存在性等）</summary>
        Query
    }

    /// <summary>
    /// 文件访问结果
    /// </summary>
    public enum FileAccessResult
    {
        /// <summary>访问已授权</summary>
        Granted,
        /// <summary>文件已被标记删除</summary>
        FileDeleted,
        /// <summary>文件被锁定</summary>
        FileLocked,
        /// <summary>文件不存在</summary>
        FileNotFound
    }

    /// <summary>
    /// 文件类型分类
    /// </summary>
    public enum FileType
    {
        /// <summary>原始图像文件</summary>
        OriginalImage,
        /// <summary>缓存文件</summary>
        CacheFile,
        /// <summary>临时文件</summary>
        TemporaryFile,
        /// <summary>配置文件</summary>
        ConfigFile
    }

    /// <summary>
    /// 文件访问范围接口 - RAII模式
    /// 使用using语句确保文件访问正确释放
    /// </summary>
    public interface IFileAccessScope : IDisposable
    {
        /// <summary>文件路径</summary>
        string FilePath { get; }
        
        /// <summary>访问结果</summary>
        FileAccessResult Result { get; }
        
        /// <summary>访问是否成功</summary>
        bool IsGranted { get; }
        
        /// <summary>错误消息（如果访问被拒绝）</summary>
        string? ErrorMessage { get; }
    }

    /// <summary>
    /// 文件访问管理器接口
    /// 统一管理文件生命周期，解决并发访问和删除竞争问题
    /// 
    /// 核心原则：
    /// 1. 所有文件访问（读/写/删除）都应通过此接口
    /// 2. 使用引用计数跟踪正在使用的文件
    /// 3. 延迟删除机制：删除正在使用的文件时标记为待删除
    /// </summary>
    public interface IFileAccessManager
    {
        /// <summary>
        /// 尝试开始文件访问
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="intent">访问意图</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>访问结果</returns>
        FileAccessResult TryBeginAccess(string filePath, FileAccessIntent intent, FileType fileType = FileType.OriginalImage);

        /// <summary>
        /// 结束文件访问（释放引用计数）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        void EndAccess(string filePath);

        /// <summary>
        /// 创建文件访问范围（RAII模式）
        /// 推荐使用方式：using (var scope = manager.CreateAccessScope(path, FileAccessIntent.Read)) { ... }
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="intent">访问意图</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>访问范围对象，使用using确保释放</returns>
        IFileAccessScope CreateAccessScope(string filePath, FileAccessIntent intent, FileType fileType = FileType.OriginalImage);

        /// <summary>
        /// 检查文件是否正在使用中
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否正在使用</returns>
        bool IsFileInUse(string filePath);

        /// <summary>
        /// 检查文件是否已标记删除
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否已标记删除</returns>
        bool IsFileMarkedDeleted(string filePath);

        /// <summary>
        /// 尝试安全删除文件
        /// 如果文件正在使用，标记为待删除，等待所有引用释放后删除
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>删除结果</returns>
        FileAccessResult TrySafeDelete(string filePath);

        /// <summary>
        /// 获取当前正在使用的文件数量（诊断用）
        /// </summary>
        int InUseFileCount { get; }

        /// <summary>
        /// 获取当前正在使用的文件列表（诊断用）
        /// </summary>
        /// <returns>文件路径列表</returns>
        System.Collections.Generic.IReadOnlyList<string> GetInUseFiles();

        /// <summary>
        /// 清除已删除文件记录（用于清理过期记录）
        /// </summary>
        void ClearDeletedRecords();
    }
}
