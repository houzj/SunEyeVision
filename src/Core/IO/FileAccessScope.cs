using System;

namespace SunEyeVision.Core.IO
{
    /// <summary>
    /// 文件访问范围 - RAII模式实现
    /// 确保 EndAccess 在作用域结束时自动调用
    /// 
    /// 使用示例：
    /// <code>
    /// using (var scope = fileAccessManager.CreateAccessScope(filePath, FileAccessIntent.Read))
    /// {
    ///     if (scope.IsGranted)
    ///     {
    ///         // 安全访问文件
    ///     }
    /// }
    /// // scope.Dispose() 自动调用 EndAccess
    /// </code>
    /// </summary>
    public sealed class FileAccessScope : IFileAccessScope
    {
        private readonly IFileAccessManager _manager;
        private bool _disposed;

        /// <inheritdoc/>
        public string FilePath { get; }

        /// <inheritdoc/>
        public FileAccessResult Result { get; }

        /// <inheritdoc/>
        public bool IsGranted => Result == FileAccessResult.Granted;

        /// <inheritdoc/>
        public string? ErrorMessage { get; }

        /// <summary>
        /// 创建文件访问范围
        /// </summary>
        /// <param name="manager">文件访问管理器</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="result">访问结果</param>
        /// <param name="errorMessage">错误消息</param>
        internal FileAccessScope(
            IFileAccessManager manager,
            string filePath,
            FileAccessResult result,
            string? errorMessage = null)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Result = result;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 释放资源 - 自动调用 EndAccess
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // 只有访问成功时才需要释放?
                if (IsGranted)
                {
                    _manager.EndAccess(FilePath);
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数 - 确保资源释放
        /// </summary>
        ~FileAccessScope()
        {
            Dispose();
        }
    }
}
