using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SunEyeVision.Core.IO
{
    /// <summary>
    /// 文件跟踪信息
    /// </summary>
    internal class FileTrackingInfo
    {
        /// <summary>引用计数</summary>
        public int ReferenceCount { get; set; }
        
        /// <summary>文件类型</summary>
        public FileType FileType { get; set; }
        
        /// <summary>是否标记删除</summary>
        public bool IsMarkedForDeletion { get; set; }
        
        /// <summary>最后访问时�?/summary>
        public DateTime LastAccessTime { get; set; }
        
        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }
    }

    /// <summary>
    /// 文件访问管理�?- 核心实现
    /// 
    /// 核心功能�?
    /// 1. 引用计数跟踪正在使用的文�?
    /// 2. 延迟删除机制（文件使用中时标记为待删除）
    /// 3. 线程安全的并发访问控�?
    /// 
    /// 设计原则�?
    /// - 统一的文件访问入口点
    /// - RAII模式确保正确释放
    /// - 最小化锁持有时�?
    /// </summary>
    public class FileAccessManager : IFileAccessManager
    {
        private readonly ConcurrentDictionary<string, FileTrackingInfo> _trackingInfo;
        private readonly HashSet<string> _deletedFiles;
        private readonly object _deletedFilesLock = new object();
        
        // 延迟删除队列：文件路�?-> 删除请求时间
        private readonly ConcurrentDictionary<string, DateTime> _pendingDeletions;
        
        // 延迟删除检查间�?
        private readonly TimeSpan _deletionCheckInterval = TimeSpan.FromSeconds(30);
        private DateTime _lastDeletionCheck = DateTime.MinValue;

        /// <summary>
        /// 创建文件访问管理�?
        /// </summary>
        public FileAccessManager()
        {
            _trackingInfo = new ConcurrentDictionary<string, FileTrackingInfo>(StringComparer.OrdinalIgnoreCase);
            _deletedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _pendingDeletions = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            
            Debug.WriteLine("[FileAccessManager] �?文件访问管理器已初始�?);
        }

        /// <inheritdoc/>
        public int InUseFileCount => _trackingInfo.Count;

        /// <inheritdoc/>
        public FileAccessResult TryBeginAccess(string filePath, FileAccessIntent intent, FileType fileType = FileType.OriginalImage)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return FileAccessResult.FileNotFound;
            }

            // 检查文件是否已标记删除
            if (IsFileMarkedDeleted(filePath))
            {
                return FileAccessResult.FileDeleted;
            }

            // 对于删除意图，特殊处�?
            if (intent == FileAccessIntent.Delete)
            {
                return TryMarkForDeletion(filePath);
            }

            // 检查文件是否存在（对于读取操作�?
            if (intent == FileAccessIntent.Read && !File.Exists(filePath))
            {
                return FileAccessResult.FileNotFound;
            }

            // 增加引用计数
            var info = _trackingInfo.AddOrUpdate(
                filePath,
                path => new FileTrackingInfo
                {
                    ReferenceCount = 1,
                    FileType = fileType,
                    LastAccessTime = DateTime.UtcNow,
                    CreatedTime = DateTime.UtcNow
                },
                (path, existing) =>
                {
                    existing.ReferenceCount++;
                    existing.LastAccessTime = DateTime.UtcNow;
                    return existing;
                });

            return FileAccessResult.Granted;
        }

        /// <inheritdoc/>
        public void EndAccess(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // 减少引用计数
            if (_trackingInfo.TryGetValue(filePath, out var info))
            {
                lock (info)
                {
                    info.ReferenceCount--;
                    
                    if (info.ReferenceCount <= 0)
                    {
                        // 引用计数归零，移除跟�?
                        _trackingInfo.TryRemove(filePath, out _);
                        
                        // 如果文件被标记删除，现在可以执行删除
                        if (info.IsMarkedForDeletion)
                        {
                            ExecuteDelayedDeletion(filePath);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IFileAccessScope CreateAccessScope(string filePath, FileAccessIntent intent, FileType fileType = FileType.OriginalImage)
        {
            var result = TryBeginAccess(filePath, intent, fileType);
            
            string? errorMessage = result switch
            {
                FileAccessResult.FileDeleted => "文件已被标记删除",
                FileAccessResult.FileLocked => "文件被锁�?,
                FileAccessResult.FileNotFound => "文件不存�?,
                _ => null
            };
            
            return new FileAccessScope(this, filePath, result, errorMessage);
        }

        /// <inheritdoc/>
        public bool IsFileInUse(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            return _trackingInfo.TryGetValue(filePath, out var info) && info.ReferenceCount > 0;
        }

        /// <inheritdoc/>
        public bool IsFileMarkedDeleted(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            lock (_deletedFilesLock)
            {
                return _deletedFiles.Contains(filePath);
            }
        }

        /// <inheritdoc/>
        public FileAccessResult TrySafeDelete(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return FileAccessResult.FileNotFound;
            }

            // 检查是否已删除
            if (IsFileMarkedDeleted(filePath))
            {
                return FileAccessResult.FileDeleted;
            }

            // 如果文件正在使用，标记为待删�?
            if (IsFileInUse(filePath))
            {
                if (_trackingInfo.TryGetValue(filePath, out var info))
                {
                    info.IsMarkedForDeletion = true;
                    _pendingDeletions.TryAdd(filePath, DateTime.UtcNow);
                    Debug.WriteLine($"[FileAccessManager] �?文件正在使用，标记待删除: {Path.GetFileName(filePath)}");
                    return FileAccessResult.FileLocked; // 返回锁定状态，表示延迟删除
                }
            }

            // 直接删除
            return ExecuteDeletion(filePath);
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetInUseFiles()
        {
            return _trackingInfo
                .Where(kvp => kvp.Value.ReferenceCount > 0)
                .Select(kvp => kvp.Key)
                .ToList()
                .AsReadOnly();
        }

        /// <inheritdoc/>
        public void ClearDeletedRecords()
        {
            lock (_deletedFilesLock)
            {
                _deletedFiles.Clear();
            }
            
            Debug.WriteLine("[FileAccessManager] �?已清除删除记�?);
        }

        /// <summary>
        /// 处理延迟删除队列
        /// 定期检查待删除文件是否可以执行删除
        /// </summary>
        public void ProcessPendingDeletions()
        {
            var now = DateTime.UtcNow;
            
            // 限制检查频�?
            if (now - _lastDeletionCheck < _deletionCheckInterval)
            {
                return;
            }
            
            _lastDeletionCheck = now;
            
            // 检查待删除队列
            foreach (var kvp in _pendingDeletions.ToList())
            {
                var filePath = kvp.Key;
                
                // 如果文件不再使用，执行删�?
                if (!IsFileInUse(filePath))
                {
                    ExecuteDelayedDeletion(filePath);
                }
                // 如果超过5分钟还没删除，强制标记为已删除（防止泄漏�?
                else if (now - kvp.Value > TimeSpan.FromMinutes(5))
                {
                    MarkAsDeleted(filePath);
                    _pendingDeletions.TryRemove(filePath, out _);
                    Debug.WriteLine($"[FileAccessManager] �?强制标记删除（超时）: {Path.GetFileName(filePath)}");
                }
            }
        }

        /// <summary>
        /// 尝试标记文件为删除状�?
        /// </summary>
        private FileAccessResult TryMarkForDeletion(string filePath)
        {
            // 检查是否已删除
            if (IsFileMarkedDeleted(filePath))
            {
                return FileAccessResult.FileDeleted;
            }

            // 如果文件正在使用，标记为待删�?
            if (IsFileInUse(filePath))
            {
                if (_trackingInfo.TryGetValue(filePath, out var info))
                {
                    info.IsMarkedForDeletion = true;
                    _pendingDeletions.TryAdd(filePath, DateTime.UtcNow);
                    return FileAccessResult.FileLocked;
                }
            }

            // 直接删除
            return ExecuteDeletion(filePath);
        }

        /// <summary>
        /// 执行文件删除
        /// </summary>
        private FileAccessResult ExecuteDeletion(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MarkAsDeleted(filePath);
                    return FileAccessResult.FileNotFound;
                }

                File.Delete(filePath);
                MarkAsDeleted(filePath);
                
                Debug.WriteLine($"[FileAccessManager] �?文件已删�? {Path.GetFileName(filePath)}");
                return FileAccessResult.Granted;
            }
            catch (FileNotFoundException)
            {
                MarkAsDeleted(filePath);
                return FileAccessResult.FileNotFound;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[FileAccessManager] �?文件被占�? {Path.GetFileName(filePath)} - {ex.Message}");
                return FileAccessResult.FileLocked;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine($"[FileAccessManager] �?无权限删�? {Path.GetFileName(filePath)}");
                return FileAccessResult.FileLocked;
            }
        }

        /// <summary>
        /// 执行延迟删除（引用计数归零时调用�?
        /// </summary>
        private void ExecuteDelayedDeletion(string filePath)
        {
            _pendingDeletions.TryRemove(filePath, out _);
            
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Debug.WriteLine($"[FileAccessManager] �?延迟删除完成: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FileAccessManager] �?延迟删除失败: {Path.GetFileName(filePath)} - {ex.Message}");
                }
            }
            
            MarkAsDeleted(filePath);
        }

        /// <summary>
        /// 标记文件为已删除
        /// </summary>
        private void MarkAsDeleted(string filePath)
        {
            lock (_deletedFilesLock)
            {
                _deletedFiles.Add(filePath);
            }
        }

        /// <summary>
        /// 获取诊断信息
        /// </summary>
        public string GetDiagnosticInfo()
        {
            var inUseFiles = GetInUseFiles();
            int deletedCount;
            lock (_deletedFilesLock)
            {
                deletedCount = _deletedFiles.Count;
            }
            
            return $"正在使用:{inUseFiles.Count}�?已删除记�?{deletedCount}�?待删�?{_pendingDeletions.Count}�?;
        }
    }
}
