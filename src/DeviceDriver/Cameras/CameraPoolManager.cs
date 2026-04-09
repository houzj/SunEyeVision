using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SunEyeVision.DeviceDriver.Events;
using SunEyeVision.DeviceDriver.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.DeviceDriver.Cameras
{
    /// <summary>
    /// 相机池管理器
    /// </summary>
    public class CameraPoolManager : IDisposable
    {
        /// <summary>
        /// 已注册的相机工厂
        /// </summary>
        private readonly List<ICameraFactory> _factories;

        /// <summary>
        /// 相机服务字典（Key: DeviceId, Value: ICameraService）
        /// </summary>
        private readonly Dictionary<string, ICameraService> _cameraServices;

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// 是否已释放
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CameraPoolManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _factories = new List<ICameraFactory>();
            _cameraServices = new Dictionary<string, ICameraService>();
            _disposed = false;
        }

        /// <summary>
        /// 注册相机工厂
        /// </summary>
        public void RegisterFactory(ICameraFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            // 检查是否已经注册
            if (_factories.Any(f => f.Manufacturer.Equals(factory.Manufacturer, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning($"Factory for manufacturer '{factory.Manufacturer}' is already registered");
                return;
            }

            _factories.Add(factory);
            _logger.LogSuccess($"Camera factory registered: {factory.Manufacturer}");
        }

        /// <summary>
        /// 注销相机工厂
        /// </summary>
        public bool UnregisterFactory(string manufacturer)
        {
            if (string.IsNullOrEmpty(manufacturer))
            {
                return false;
            }

            var factory = _factories.FirstOrDefault(f => f.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));
            if (factory == null)
            {
                _logger.LogWarning($"Factory for manufacturer '{manufacturer}' not found");
                return false;
            }

            // 断开所有使用该工厂创建的相机
            var camerasToDisconnect = _cameraServices.Values
                .Where(c => c.DeviceInfo.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var camera in camerasToDisconnect)
            {
                try
                {
                    if (camera.IsConnected)
                    {
                        camera.DisconnectAsync().GetAwaiter().GetResult();
                    }
                    camera.Dispose();
                    _cameraServices.Remove(camera.DeviceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disconnecting camera: {camera.DeviceInfo.DeviceName}", ex);
                }
            }

            _factories.Remove(factory);
            _logger.LogSuccess($"Camera factory unregistered: {manufacturer}");
            return true;
        }

        /// <summary>
        /// 发现所有相机
        /// </summary>
        public async Task<List<CameraDeviceInfo>> DiscoverAllCamerasAsync()
        {
            var allCameras = new List<CameraDeviceInfo>();

            _logger.LogInfo("Discovering cameras from all registered factories...");

            foreach (var factory in _factories)
            {
                try
                {
                    _logger.LogInfo($"Discovering cameras from {factory.Manufacturer} factory...");

                    var cameras = await factory.DiscoverDevicesAsync();
                    allCameras.AddRange(cameras);

                    _logger.LogSuccess($"Found {cameras.Count} camera(s) from {factory.Manufacturer} factory");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error discovering cameras from {factory.Manufacturer} factory", ex);
                }
            }

            _logger.LogSuccess($"Total discovered cameras: {allCameras.Count}");
            return allCameras;
        }

        /// <summary>
        /// 创建相机服务
        /// </summary>
        public ICameraService? CreateCameraService(CameraDeviceInfo deviceInfo)
        {
            if (deviceInfo == null)
            {
                throw new ArgumentNullException(nameof(deviceInfo));
            }

            // 检查是否已经存在
            if (_cameraServices.ContainsKey(deviceInfo.DeviceId))
            {
                _logger.LogWarning($"Camera service already exists: {deviceInfo.DeviceName}");
                return _cameraServices[deviceInfo.DeviceId];
            }

            // 查找合适的工厂
            var factory = _factories.FirstOrDefault(f => f.IsSupported(deviceInfo));
            if (factory == null)
            {
                _logger.LogError($"No factory found for camera: {deviceInfo.DeviceName}");
                return null;
            }

            try
            {
                var cameraService = factory.CreateCameraService(deviceInfo);

                // 订阅事件
                cameraService.ExceptionOccurred += OnCameraExceptionOccurred;
                cameraService.ConnectionStateChanged += OnCameraConnectionStateChanged;

                _cameraServices[deviceInfo.DeviceId] = cameraService;
                _logger.LogSuccess($"Camera service created: {deviceInfo.DeviceName}");

                return cameraService;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating camera service: {deviceInfo.DeviceName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取相机服务
        /// </summary>
        public ICameraService? GetCameraService(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return null;
            }

            _cameraServices.TryGetValue(deviceId, out var cameraService);
            return cameraService;
        }

        /// <summary>
        /// 获取所有相机服务
        /// </summary>
        public List<ICameraService> GetAllCameraServices()
        {
            return _cameraServices.Values.ToList();
        }

        /// <summary>
        /// 获取已连接的相机服务
        /// </summary>
        public List<ICameraService> GetConnectedCameras()
        {
            return _cameraServices.Values.Where(c => c.IsConnected).ToList();
        }

        /// <summary>
        /// 移除相机服务
        /// </summary>
        public bool RemoveCameraService(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                return false;
            }

            if (!_cameraServices.TryGetValue(deviceId, out var cameraService))
            {
                _logger.LogWarning($"Camera service not found: {deviceId}");
                return false;
            }

            try
            {
                // 断开连接
                if (cameraService.IsConnected)
                {
                    cameraService.DisconnectAsync().GetAwaiter().GetResult();
                }

                // 取消订阅事件
                cameraService.ExceptionOccurred -= OnCameraExceptionOccurred;
                cameraService.ConnectionStateChanged -= OnCameraConnectionStateChanged;

                // 释放资源
                cameraService.Dispose();

                _cameraServices.Remove(deviceId);
                _logger.LogSuccess($"Camera service removed: {deviceId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing camera service: {deviceId}", ex);
                return false;
            }
        }

        /// <summary>
        /// 连接所有相机
        /// </summary>
        public async Task<List<string>> ConnectAllCamerasAsync()
        {
            var connectedDevices = new List<string>();

            foreach (var cameraService in _cameraServices.Values)
            {
                try
                {
                    if (!cameraService.IsConnected)
                    {
                        bool result = await cameraService.ConnectAsync();
                        if (result)
                        {
                            connectedDevices.Add(cameraService.DeviceId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error connecting camera: {cameraService.DeviceInfo.DeviceName}", ex);
                }
            }

            return connectedDevices;
        }

        /// <summary>
        /// 断开所有相机
        /// </summary>
        public async Task<List<string>> DisconnectAllCamerasAsync()
        {
            var disconnectedDevices = new List<string>();

            foreach (var cameraService in _cameraServices.Values)
            {
                try
                {
                    if (cameraService.IsConnected)
                    {
                        bool result = await cameraService.DisconnectAsync();
                        if (result)
                        {
                            disconnectedDevices.Add(cameraService.DeviceId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disconnecting camera: {cameraService.DeviceInfo.DeviceName}", ex);
                }
            }

            return disconnectedDevices;
        }

        /// <summary>
        /// 开始所有相机的采集
        /// </summary>
        public async Task<List<string>> StartAllCaptureAsync()
        {
            var startedDevices = new List<string>();

            foreach (var cameraService in _cameraServices.Values)
            {
                try
                {
                    if (cameraService.IsConnected)
                    {
                        bool result = await cameraService.StartCaptureAsync();
                        if (result)
                        {
                            startedDevices.Add(cameraService.DeviceId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error starting capture on camera: {cameraService.DeviceInfo.DeviceName}", ex);
                }
            }

            return startedDevices;
        }

        /// <summary>
        /// 停止所有相机的采集
        /// </summary>
        public async Task<List<string>> StopAllCaptureAsync()
        {
            var stoppedDevices = new List<string>();

            foreach (var cameraService in _cameraServices.Values)
            {
                try
                {
                    bool result = await cameraService.StopCaptureAsync();
                    if (result)
                    {
                        stoppedDevices.Add(cameraService.DeviceId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error stopping capture on camera: {cameraService.DeviceInfo.DeviceName}", ex);
                }
            }

            return stoppedDevices;
        }

        /// <summary>
        /// 相机异常事件
        /// </summary>
        public event EventHandler<CameraExceptionEvent>? CameraExceptionOccurred;

        /// <summary>
        /// 相机连接状态变更事件
        /// </summary>
        public event EventHandler<CameraConnectionEvent>? CameraConnectionStateChanged;

        /// <summary>
        /// 处理相机异常事件
        /// </summary>
        private void OnCameraExceptionOccurred(object? sender, CameraExceptionEvent e)
        {
            _logger.LogError($"Camera exception: {e.DeviceId} - {e.Message}");
            CameraExceptionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// 处理相机连接状态变更事件
        /// </summary>
        private void OnCameraConnectionStateChanged(object? sender, CameraConnectionEvent e)
        {
            _logger.LogInfo($"Camera connection state changed: {e.DeviceId} - {e.ConnectionState}");
            CameraConnectionStateChanged?.Invoke(this, e);
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
                    // 释放所有相机服务
                    foreach (var cameraService in _cameraServices.Values.ToList())
                    {
                        try
                        {
                            RemoveCameraService(cameraService.DeviceId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error disposing camera service: {cameraService.DeviceInfo.DeviceName}", ex);
                        }
                    }

                    _cameraServices.Clear();
                    _factories.Clear();
                }

                _disposed = true;
            }
        }

        ~CameraPoolManager()
        {
            Dispose(false);
        }
    }
}
