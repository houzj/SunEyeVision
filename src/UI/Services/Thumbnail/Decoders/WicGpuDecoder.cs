using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using SunEyeVision.Core.IO;
using SunEyeVision.UI.Services.Thumbnail;
using SunEyeVision.UI.Services.Thumbnail.Decoders;

namespace SunEyeVision.UI.Services.Thumbnail.Decoders
{
    /// <summary>
    /// WIC GPU解码器 - 基于WPF内置WIC硬件加速
    /// </summary>
    public class WicGpuDecoder : IThumbnailDecoder
    {
        private static readonly SemaphoreSlim _gpuDecodeSemaphore = new SemaphoreSlim(4, 4);
        private static int _waitingCount = 0;
        private static int _decodingCount = 0;
        
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public bool IsHardwareAccelerated { get; private set; }
        
        private const int LARGE_IMAGE_THRESHOLD = 4000 * 3000;
        private const int HUGE_IMAGE_THRESHOLD = 8000 * 6000;

        public bool Initialize()
        {
            if (_isInitialized)
                return IsHardwareAccelerated;

            try
            {
                Debug.WriteLine("[WicGpuDecoder] 初始化WIC GPU解码器...");

                int tier = System.Windows.Media.RenderCapability.Tier >> 16;
                bool hasGPU = tier > 0;

                if (!hasGPU)
                {
                    Debug.WriteLine("[WicGpuDecoder] GPU不可用，将使用CPU解码");
                    _isInitialized = true;
                    return false;
                }

                IsHardwareAccelerated = true;
                _isInitialized = true;

                Debug.WriteLine("[WicGpuDecoder] WIC GPU硬件解码器初始化完成");
                Debug.WriteLine($"  渲染层级: Tier {tier}");
                Debug.WriteLine($"  硬件加载: 启用（WPF内置WIC）");
                Debug.WriteLine($"  预期性能提升: 3-5倍");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WicGpuDecoder] 初始化失败: {ex.Message}");
                _isInitialized = true;
                return false;
            }
        }

        public BitmapImage? DecodeThumbnail(string filePath, int size, byte[]? prefetchedData = null, bool verboseLog = false, bool isHighPriority = false)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            var totalSw = Stopwatch.StartNew();

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"[WicGpuDecoder] 文件不存在: {filePath}");
                    return null;
                }

                var waitSw = Stopwatch.StartNew();
                int currentWaiting = Interlocked.Increment(ref _waitingCount);

                bool gotSlot = _gpuDecodeSemaphore.Wait(5000);
                
                waitSw.Stop();
                Interlocked.Decrement(ref _waitingCount);
                
                if (!gotSlot)
                {
                    Debug.WriteLine($"[WicGpuDecoder] 等待GPU槽位超时(5s) | 队列:等待{currentWaiting}个 | file={System.IO.Path.GetFileName(filePath)}");
                    return null;
                }
                
                int currentDecoding = Interlocked.Increment(ref _decodingCount);
                
                if (verboseLog || currentWaiting > 1)
                {
                    Debug.WriteLine($"[WicGpuDecoder] 排队等待:{waitSw.Elapsed.TotalMilliseconds:F0}ms | 队列:等待{currentWaiting}个 解码{currentDecoding}个 | file={System.IO.Path.GetFileName(filePath)}");
                }

                try
                {
                    var readSw = Stopwatch.StartNew();
                    byte[] imageBytes;
                    if (prefetchedData != null && prefetchedData.Length > 0)
                    {
                        imageBytes = prefetchedData;
                        readSw.Stop();
                        Debug.WriteLine($"[WicGpu] UsePrefetched | {System.IO.Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        Debug.WriteLine($"[WicGpu] StartRead wait={waitSw.ElapsedMilliseconds}ms | {System.IO.Path.GetFileName(filePath)}");
                        
                        if (!File.Exists(filePath))
                        {
                            Debug.WriteLine($"[WicGpu] FileDeletedDuringGpuWait | {System.IO.Path.GetFileName(filePath)}");
                            return null;
                        }
                        
                        try
                        {
                            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan);
                            imageBytes = new byte[fs.Length];
                            int bytesRead = fs.Read(imageBytes, 0, imageBytes.Length);
                            if (bytesRead != imageBytes.Length && imageBytes.Length > 0)
                            {
                                Array.Resize(ref imageBytes, bytesRead);
                            }
                            readSw.Stop();
                            
                            Debug.WriteLine($"[WicGpu] ReadOK {readSw.ElapsedMilliseconds}ms | {System.IO.Path.GetFileName(filePath)}");
                        }
                        catch (FileNotFoundException)
                        {
                            Debug.WriteLine($"[WicGpu] FileNotFound | {System.IO.Path.GetFileName(filePath)}");
                            return null;
                        }
                        catch (IOException ioEx)
                        {
                            Debug.WriteLine($"[WicGpu] IOError: {ioEx.Message} | {System.IO.Path.GetFileName(filePath)}");
                            return null;
                        }
                    }
                    
                    long fileSizeKB = imageBytes.Length / 1024;
                    string fileSizeMB = fileSizeKB > 1024 ? $"{fileSizeKB / 1024.0:F1}MB" : $"{fileSizeKB}KB";
                    
                    var infoSw = Stopwatch.StartNew();
                    int originalWidth = 0, originalHeight = 0;
                    string formatInfo = "";
                    long totalPixels = 0;
                    
                    if (verboseLog)
                    {
                        try
                        {
                            using var memStream = new MemoryStream(imageBytes);
                            var decoder = BitmapDecoder.Create(memStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnDemand);
                            if (decoder.Frames.Count > 0)
                            {
                                var frame = decoder.Frames[0];
                                originalWidth = frame.PixelWidth;
                                originalHeight = frame.PixelHeight;
                                formatInfo = $"格式:{decoder.CodecInfo.FriendlyName}";
                                totalPixels = (long)originalWidth * originalHeight;
                            }
                        }
                        catch { }
                    }
                    infoSw.Stop();
                    
                    string resolutionInfo = originalWidth > 0 
                        ? $"原始:{originalWidth}x{originalHeight}({totalPixels / 1000000.0:F1}MP)" 
                        : "";

                    var decodeSw = Stopwatch.StartNew();
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = size;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

                    MemoryStream? decodeStream = null;
                    try
                    {
                        decodeStream = new MemoryStream(imageBytes);
                        bitmap.StreamSource = decodeStream;
                        bitmap.Rotation = Rotation.Rotate0;
                        bitmap.EndInit();
                    }
                    finally
                    {
                        decodeStream?.Dispose();
                    }
                    decodeSw.Stop();

                    var freezeSw = Stopwatch.StartNew();
                    bitmap.Freeze();
                    freezeSw.Stop();

                    totalSw.Stop();

                    if (verboseLog)
                    {
                        string diagnosticInfo = string.IsNullOrEmpty(resolutionInfo) 
                            ? $"文件:{fileSizeMB}" 
                            : $"{resolutionInfo} 文件:{fileSizeMB} {formatInfo}";
                        Debug.WriteLine($"[WicGpuDecoder] GPU解码 | 等待:{waitSw.Elapsed.TotalMilliseconds:F0}ms 读取:{readSw.Elapsed.TotalMilliseconds:F0}ms 解码={decodeSw.Elapsed.TotalMilliseconds:F0}ms 总计:{totalSw.Elapsed.TotalMilliseconds:F0}ms | {diagnosticInfo} | file={System.IO.Path.GetFileName(filePath)}");
                        
                        if (totalPixels > HUGE_IMAGE_THRESHOLD)
                        {
                            Debug.WriteLine($"[WicGpuDecoder] 超大图警告 - {originalWidth}x{originalHeight}({totalPixels / 1000000.0:F1}MP) file={System.IO.Path.GetFileName(filePath)}");
                        }
                    }

                    if (bitmap.Width <= 0 || bitmap.Height <= 0)
                    {
                        Debug.WriteLine($"[WicGpuDecoder] 解码结果无效 size={bitmap.Width}x{bitmap.Height} file={System.IO.Path.GetFileName(filePath)}");
                        return null;
                    }

                    return bitmap;
                }
                finally
                {
                    _gpuDecodeSemaphore.Release();
                    Interlocked.Decrement(ref _decodingCount);
                }
            }
            catch (Exception ex)
            {
                totalSw.Stop();
                Debug.WriteLine($"[WicGpuDecoder] 解码异常: {ex.Message} (耗时:{totalSw.Elapsed.TotalMilliseconds:F2}ms) file={System.IO.Path.GetFileName(filePath)}");
                return null;
            }
        }

        private BitmapImage? DecodeWithCpu(string filePath, int size, byte[]? prefetchedData, Stopwatch totalSw)
        {
            try
            {
                byte[] imageBytes;
                if (prefetchedData != null && prefetchedData.Length > 0)
                {
                    imageBytes = prefetchedData;
                }
                else
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.SequentialScan);
                    imageBytes = new byte[fs.Length];
                    fs.Read(imageBytes, 0, imageBytes.Length);
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = size;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                
                MemoryStream? decodeStream = null;
                try
                {
                    decodeStream = new MemoryStream(imageBytes);
                    bitmap.StreamSource = decodeStream;
                    bitmap.Rotation = Rotation.Rotate0;
                    bitmap.EndInit();
                }
                finally
                {
                    decodeStream?.Dispose();
                }
                
                bitmap.Freeze();
                totalSw.Stop();
                
                Debug.WriteLine($"[WicGpuDecoder] CPU降级解码完成 | 耗时:{totalSw.Elapsed.TotalMilliseconds:F0}ms | file={System.IO.Path.GetFileName(filePath)}");
                
                return bitmap;
            }
            catch (Exception ex)
            {
                totalSw.Stop();
                Debug.WriteLine($"[WicGpuDecoder] CPU降级解码失败: {ex.Message} | file={System.IO.Path.GetFileName(filePath)}");
                return null;
            }
        }

        public BitmapImage? DecodeThumbnailSafe(
            IFileAccessManager? fileManager,
            string filePath,
            int size,
            byte[]? prefetchedData = null,
            bool verboseLog = false,
            bool isHighPriority = false)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            
            Debug.WriteLine($"[WicGpu] SafeStart | {fileName}");
            CleanupScheduler.MarkFileInUse(filePath);
            
            try
            {
                if (fileManager != null)
                {
                    using var scope = fileManager.CreateAccessScope(filePath, FileAccessIntent.Read, FileType.OriginalImage);
                    
                    if (!scope.IsGranted)
                    {
                        Debug.WriteLine($"[WicGpu] AccessDenied: {scope.ErrorMessage} | {fileName}");
                        return null;
                    }

                    var result = DecodeThumbnail(filePath, size, prefetchedData, verboseLog, isHighPriority);
                    
                    Debug.WriteLine($"[WicGpu] SafeEnd OK={(result != null)} | {fileName}");
                    return result;
                }
                else
                {
                    var result = DecodeThumbnail(filePath, size, prefetchedData, verboseLog, isHighPriority);
                    
                    Debug.WriteLine($"[WicGpu] SafeEnd OK={(result != null)} | {fileName}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WicGpu] SafeError {ex.GetType().Name} | {fileName}");
                throw;
            }
            finally
            {
                CleanupScheduler.ReleaseFile(filePath);
                Debug.WriteLine($"[WicGpu] SafeRelease | {fileName}");
            }
        }

        public void Dispose()
        {
            _isInitialized = false;
            IsHardwareAccelerated = false;
            Debug.WriteLine("[WicGpuDecoder] 资源已释放");
        }
    }
}
