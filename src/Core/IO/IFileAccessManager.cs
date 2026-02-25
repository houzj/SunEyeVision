using System;

namespace SunEyeVision.Core.IO
{
    /// <summary>
    /// 文件访问意图
    /// </summary>
    public enum FileAccessIntent
    {
        /// <summary>读取</summary>
        Read,
        /// <summary>写入</summary>
        Write,
        /// <summary>删除</summary>
        Delete,
        /// <summary>查询状态</summary>
        Query
    }

    /// <summary>
    /// 文件访问结果
    /// </summary>
    public enum FileAccessResult
    {
        /// <summary>获得权限</summary>
        Granted,
        /// <summary>文件已标记删除</summary>
        FileDeleted,
        /// <summary>文件被锁定</summary>
        FileLocked,
        /// <summary>文件未找到</summary>
        FileNotFound
    }

    /// <summary>
    /// 文件类型
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
    /// 使用using语句确保资源释放
    /// </summary>
    public interface IFileAccessScope : IDisposable
    {
        /// <summary>文件路径</summary>
        string FilePath { get; }
        
        /// <summary>访问结果</summary>
        FileAccessResult Result { get; }
        
        /// <summary>访问是否成功</summary>
        bool IsGranted { get; }
        
        /// <summary>错误信息（如果被拒绝）</summary>
        string? ErrorMessage { get; }
    }

    /// <summary>
    /// 文件访问管理器接口
    /// 
    /// 核心原则：
    /// 1. 文件访问应尽可能短暂
    /// 2. 使用计数跟踪正在使用的文件
    /// 3. 删除正在使用的文件时标记为待删除
    /// </summary>
    public interface IFileAccessManager
    {
        /// <summary>
        /// 开始文件访问
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="intent">访问意图</param>
        /// <param name="fileType">文件类型</param>
        /// <returns>访问结果</returns>
        FileAccessResult TryBeginAccess(string filePath, FileAccessIntent intent, FileType fileType = FileType.OriginalImage);

        /// <summary>
        /// 结束文件访问
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
        /// <returns>访问范围，使用using释放</returns>
        IFileAccessScope CreateAccessScope(string filePath, FileAccessIntent intent, FileType fileType = FileType.OriginalImage);

        /// <summary>
        /// 检查文件是否正在使用
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
        /// 如果文件正在使用，标记为删除等待释放后删除
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>删除结果</returns>
        FileAccessResult TrySafeDelete(string filePath);

        /// <summary>
        /// 当前使用的文件数量
        /// </summary>
        int InUseFileCount { get; }

        /// <summary>
        /// 获取当前使用的文件路径列表
        /// </summary>
        /// <returns>文件路径列表</returns>
        System.Collections.Generic.IReadOnlyList<string> GetInUseFiles();

        /// <summary>
        /// 清除已删除文件的记录（谨慎使用）
        /// </summary>
        void ClearDeletedRecords();
    }
}
