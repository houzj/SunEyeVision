using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.DeviceDriver.Events;
using SunEyeVision.DeviceDriver.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.DeviceDriver.Cameras
{
    /// <summary>
    /// 相机服务基类
    /// </summary>
    public abstract class CameraServiceBase : ICameraService, IDisposable
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        public string DeviceId => DeviceInfo.DeviceId;

        /// <summary>
        /// 设备信息
        /// </summary>
        public CameraDeviceInfo DeviceInfo { get; protected set; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; protected set; }

        /// <summary>
        /// 是否正在采集
        /// </summary>
        protected bool IsCapturing { get; set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// 帧计数器
        /// </summary>
        protected long FrameCounter { get; private set; }

        /// <summary>
        /// 取消采集令牌
        /// </summary>
        protected CancellationTokenSource? CaptureCancellationTokenSource { get; private set; }

        /// <summary>
        /// 相机帧接收事件
        /// </summary>
        public event EventHandler<CameraFrameReceivedEvent>? FrameReceived;

        /// <summary>
        /// 相机异常事件
        /// </summary>
        public event EventHandler<CameraExceptionEvent>? ExceptionOccurred;

        /// <summary>
        /// 相机连接事件
        /// </summary>
        public event EventHandler<CameraConnectionEvent>? ConnectionStateChanged;

        protected bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected CameraServiceBase(CameraDeviceInfo deviceInfo, ILogger logger)
        {
            DeviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            IsConnected = false;
            IsCapturing = false;
            FrameCounter = 0;
        }

        /// <summary>
        /// 连接相机（由子类实现）
        /// </summary>
        protected abstract Task<bool> ConnectCoreAsync();

        /// <summary>
        /// 断开相机（由子类实现）
        /// </summary>
        protected abstract Task<bool> DisconnectCoreAsync();

        /// <summary>
        /// 开始采集（由子类实现）
        /// </summary>
        protected abstract Task<bool> StartCaptureCoreAsync();

        /// <summary>
        /// 停止采集（由子类实现）
        /// </summary>
        protected abstract Task<bool> StopCaptureCoreAsync();

        /// <summary>
        /// 触发一次采集（由子类实现）
        /// </summary>
        protected abstract Task<CameraFrameInfo?> TriggerCaptureCoreAsync();

        /// <summary>
        /// 设置触发模式（由子类实现）
        /// </summary>
        protected abstract Task<bool> SetTriggerModeCoreAsync(CameraTriggerMode mode);

        /// <summary>
        /// 设置采集参数（由子类实现）
        /// </summary>
        protected abstract Task<bool> SetCaptureSettingsCoreAsync(CameraCaptureSettings settings);

        /// <summary>
        /// 获取采集参数（由子类实现）
        /// </summary>
        protected abstract Task<CameraCaptureSettings?> GetCaptureSettingsCoreAsync();

        /// <summary>
        /// 获取相机属性（由子类实现）
        /// </summary>
        protected abstract Task<Dictionary<string, object>> GetPropertiesCoreAsync();

        /// <summary>
        /// 设置相机属性（由子类实现）
        /// </summary>
        protected abstract Task<bool> SetPropertiesCoreAsync(Dictionary<string, object> properties);

        /// <summary>
        /// 连接相机
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                OnConnectionStateChanged(CameraConnectionState.Connecting, "正在连接相机...");

                Logger.LogInfo($"Connecting to camera: {DeviceInfo.DeviceName} ({DeviceInfo.IpAddress}:{DeviceInfo.Port})");

                bool result = await ConnectCoreAsync();

                if (result)
                {
                    IsConnected = true;
                    OnConnectionStateChanged(CameraConnectionState.Connected, "相机已连接");
                    Logger.LogSuccess($"Camera connected: {DeviceInfo.DeviceName}");
                }
                else
                {
                    IsConnected = false;
                    OnConnectionStateChanged(CameraConnectionState.Error, "相机连接失败");
                    OnExceptionOccurred(CameraExceptionType.ConnectionFailed, "Failed to connect to camera");
                    Logger.LogError($"Failed to connect to camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnConnectionStateChanged(CameraConnectionState.Error, $"连接异常: {ex.Message}");
                OnExceptionOccurred(CameraExceptionType.ConnectionFailed, ex.Message, ex);
                Logger.LogError($"Error connecting to camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 断开相机
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            try
            {
                // 如果正在采集，先停止采集
                if (IsCapturing)
                {
                    await StopCaptureAsync();
                }

                OnConnectionStateChanged(CameraConnectionState.Disconnecting, "正在断开相机...");

                Logger.LogInfo($"Disconnecting from camera: {DeviceInfo.DeviceName}");

                bool result = await DisconnectCoreAsync();

                IsConnected = false;

                if (result)
                {
                    OnConnectionStateChanged(CameraConnectionState.Disconnected, "相机已断开");
                    Logger.LogSuccess($"Camera disconnected: {DeviceInfo.DeviceName}");
                }
                else
                {
                    OnConnectionStateChanged(CameraConnectionState.Error, "相机断开失败");
                    Logger.LogError($"Failed to disconnect from camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnConnectionStateChanged(CameraConnectionState.Error, $"断开异常: {ex.Message}");
                OnExceptionOccurred(CameraExceptionType.Disconnected, ex.Message, ex);
                Logger.LogError($"Error disconnecting from camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 开始采集
        /// </summary>
        public async Task<bool> StartCaptureAsync()
        {
            if (!IsConnected)
            {
                Logger.LogError("Cannot start capture: camera not connected");
                return false;
            }

            if (IsCapturing)
            {
                Logger.LogWarning("Camera is already capturing");
                return true;
            }

            try
            {
                Logger.LogInfo($"Starting capture on camera: {DeviceInfo.DeviceName}");

                bool result = await StartCaptureCoreAsync();

                if (result)
                {
                    IsCapturing = true;
                    CaptureCancellationTokenSource = new CancellationTokenSource();
                    Logger.LogSuccess($"Capture started on camera: {DeviceInfo.DeviceName}");
                }
                else
                {
                    Logger.LogError($"Failed to start capture on camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                IsCapturing = false;
                OnExceptionOccurred(CameraExceptionType.CaptureFailed, ex.Message, ex);
                Logger.LogError($"Error starting capture on camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        public async Task<bool> StopCaptureAsync()
        {
            if (!IsCapturing)
            {
                Logger.LogWarning("Camera is not capturing");
                return true;
            }

            try
            {
                Logger.LogInfo($"Stopping capture on camera: {DeviceInfo.DeviceName}");

                // 取消采集
                CaptureCancellationTokenSource?.Cancel();

                bool result = await StopCaptureCoreAsync();

                IsCapturing = false;
                CaptureCancellationTokenSource?.Dispose();
                CaptureCancellationTokenSource = null;

                if (result)
                {
                    Logger.LogSuccess($"Capture stopped on camera: {DeviceInfo.DeviceName}");
                }
                else
                {
                    Logger.LogError($"Failed to stop capture on camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                IsCapturing = false;
                OnExceptionOccurred(CameraExceptionType.CaptureFailed, ex.Message, ex);
                Logger.LogError($"Error stopping capture on camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 触发一次采集
        /// </summary>
        public async Task<CameraFrameInfo?> TriggerCaptureAsync()
        {
            if (!IsConnected)
            {
                Logger.LogError("Cannot trigger capture: camera not connected");
                return null;
            }

            try
            {
                Logger.LogInfo($"Triggering capture on camera: {DeviceInfo.DeviceName}");

                CameraFrameInfo? frame = await TriggerCaptureCoreAsync();

                if (frame != null)
                {
                    FrameCounter++;
                    OnFrameReceived(frame);
                    Logger.LogSuccess($"Frame captured: {DeviceInfo.DeviceName}, Frame #{FrameCounter}");
                }
                else
                {
                    Logger.LogError($"Failed to trigger capture on camera: {DeviceInfo.DeviceName}");
                }

                return frame;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(CameraExceptionType.CaptureFailed, ex.Message, ex);
                Logger.LogError($"Error triggering capture on camera: {DeviceInfo.DeviceName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 设置触发模式
        /// </summary>
        public async Task<bool> SetTriggerModeAsync(CameraTriggerMode mode)
        {
            if (!IsConnected)
            {
                Logger.LogError("Cannot set trigger mode: camera not connected");
                return false;
            }

            try
            {
                Logger.LogInfo($"Setting trigger mode on camera: {DeviceInfo.DeviceName} to {mode}");

                bool result = await SetTriggerModeCoreAsync(mode);

                if (result)
                {
                    Logger.LogSuccess($"Trigger mode set to {mode} on camera: {DeviceInfo.DeviceName}");
                }
                else
                {
                    Logger.LogError($"Failed to set trigger mode on camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(CameraExceptionType.SettingFailed, ex.Message, ex);
                Logger.LogError($"Error setting trigger mode on camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 设置采集参数
        /// </summary>
        public async Task<bool> SetCaptureSettingsAsync(CameraCaptureSettings settings)
        {
            if (!IsConnected)
            {
                Logger.LogError("Cannot set capture settings: camera not connected");
                return false;
            }

            try
            {
                Logger.LogInfo($"Setting capture settings on camera: {DeviceInfo.DeviceName}");

                bool result = await SetCaptureSettingsCoreAsync(settings);

                if (result)
                {
                    Logger.LogSuccess($"Capture settings set on camera: {DeviceInfo.DeviceName}");
                }
                else
                {
                    Logger.LogError($"Failed to set capture settings on camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(CameraExceptionType.SettingFailed, ex.Message, ex);
                Logger.LogError($"Error setting capture settings on camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取采集参数
        /// </summary>
        public async Task<CameraCaptureSettings?> GetCaptureSettingsAsync()
        {
            if (!IsConnected)
            {
                Logger.LogError("Cannot get capture settings: camera not connected");
                return null;
            }

            try
            {
                CameraCaptureSettings? settings = await GetCaptureSettingsCoreAsync();
                return settings;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting capture settings on camera: {DeviceInfo.DeviceName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取相机属性
        /// </summary>
        public async Task<Dictionary<string, object>> GetPropertiesAsync()
        {
            if (!IsConnected)
            {
                Logger.LogWarning("Cannot get properties: camera not connected");
                return new Dictionary<string, object>();
            }

            try
            {
                return await GetPropertiesCoreAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting properties on camera: {DeviceInfo.DeviceName}", ex);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 设置相机属性
        /// </summary>
        public async Task<bool> SetPropertiesAsync(Dictionary<string, object> properties)
        {
            if (!IsConnected)
            {
                Logger.LogError("Cannot set properties: camera not connected");
                return false;
            }

            try
            {
                Logger.LogInfo($"Setting properties on camera: {DeviceInfo.DeviceName}");

                bool result = await SetPropertiesCoreAsync(properties);

                if (result)
                {
                    Logger.LogSuccess($"Properties set on camera: {DeviceInfo.DeviceName}");
                }
                else
                {
                    Logger.LogError($"Failed to set properties on camera: {DeviceInfo.DeviceName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                OnExceptionOccurred(CameraExceptionType.SettingFailed, ex.Message, ex);
                Logger.LogError($"Error setting properties on camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 触发帧接收事件
        /// </summary>
        protected virtual void OnFrameReceived(CameraFrameInfo frameInfo)
        {
            FrameReceived?.Invoke(this, new CameraFrameReceivedEvent
            {
                DeviceId = DeviceId,
                FrameInfo = frameInfo
            });
        }

        /// <summary>
        /// 触发异常事件
        /// </summary>
        protected virtual void OnExceptionOccurred(CameraExceptionType exceptionType, string message, Exception? exception = null)
        {
            ExceptionOccurred?.Invoke(this, new CameraExceptionEvent
            {
                DeviceId = DeviceId,
                ExceptionType = exceptionType,
                Message = message,
                Exception = exception,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 触发连接状态变更事件
        /// </summary>
        protected virtual void OnConnectionStateChanged(CameraConnectionState state, string? message = null)
        {
            ConnectionStateChanged?.Invoke(this, new CameraConnectionEvent
            {
                DeviceId = DeviceId,
                ConnectionState = state,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    if (IsConnected)
                    {
                        DisconnectAsync().GetAwaiter().GetResult();
                    }

                    CaptureCancellationTokenSource?.Dispose();
                }

                // 释放非托管资源
                _disposed = true;
            }
        }

        ~CameraServiceBase()
        {
            Dispose(false);
        }
    }
}
