using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.Core.Services.Logging;

namespace SunEyeVision.Core.Services.CameraDiscovery
{
    /// <summary>
    /// 相机发现聚合服务
    /// </summary>
    public class CameraDiscoveryAggregator
    {
        private readonly Dictionary<CameraType, ICameraDiscoveryService> _discoveryServices;
        private readonly ICameraDiscoveryService _gigeDiscoveryService;
        private readonly ICameraDiscoveryService _usbDiscoveryService;
        private readonly ICameraDiscoveryService _ipDiscoveryService;
        
        public CameraDiscoveryAggregator(
            GigeCameraDiscoveryService gigeDiscoveryService,
            UsbCameraDiscoveryService usbDiscoveryService,
            IpCameraDiscoveryService ipDiscoveryService)
        {
            _gigeDiscoveryService = gigeDiscoveryService;
            _usbDiscoveryService = usbDiscoveryService;
            _ipDiscoveryService = ipDiscoveryService;
            
            _discoveryServices = new Dictionary<CameraType, ICameraDiscoveryService>
            {
                { CameraType.GigE, _gigeDiscoveryService },
                { CameraType.USB, _usbDiscoveryService },
                { CameraType.IP, _ipDiscoveryService }
            };
            
            VisionLogger.Instance.Log(LogLevel.Info, "相机发现聚合服务初始化完成", "CameraDiscoveryAggregator");
        }
        
        /// <summary>
        /// 异步发现指定类型的相机
        /// </summary>
        public async Task<List<DiscoveredCamera>> DiscoverAsync(CameraType cameraType, CancellationToken cancellationToken)
        {
            if (_discoveryServices.TryGetValue(cameraType, out var discoveryService))
            {
                VisionLogger.Instance.Log(LogLevel.Info, $"开始发现 {cameraType} 相机", "CameraDiscoveryAggregator");
                return await discoveryService.DiscoverAsync(cancellationToken);
            }
            
            VisionLogger.Instance.Log(LogLevel.Warning, $"未找到相机类型 {cameraType} 的发现服务", "CameraDiscoveryAggregator");
            return new List<DiscoveredCamera>();
        }
        
        /// <summary>
        /// 异步发现所有类型的相机
        /// </summary>
        public async Task<List<DiscoveredCamera>> DiscoverAllAsync(CancellationToken cancellationToken)
        {
            VisionLogger.Instance.Log(LogLevel.Info, "开始发现所有类型的相机", "CameraDiscoveryAggregator");
            
            var allCameras = new List<DiscoveredCamera>();
            var tasks = new List<Task<List<DiscoveredCamera>>>();
            
            foreach (var service in _discoveryServices.Values)
            {
                tasks.Add(service.DiscoverAsync(cancellationToken));
            }
            
            var results = await Task.WhenAll(tasks);
            
            foreach (var cameras in results)
            {
                allCameras.AddRange(cameras);
            }
            
            VisionLogger.Instance.Log(LogLevel.Success, $"所有相机发现完成，共发现 {allCameras.Count} 台相机", "CameraDiscoveryAggregator");
            return allCameras;
        }
        
        /// <summary>
        /// 停止所有发现服务
        /// </summary>
        public void StopAllDiscovery()
        {
            VisionLogger.Instance.Log(LogLevel.Info, "停止所有发现服务", "CameraDiscoveryAggregator");
            
            foreach (var service in _discoveryServices.Values)
            {
                service.StopDiscovery();
            }
        }
        
        /// <summary>
        /// 停止指定类型的发现服务
        /// </summary>
        public void StopDiscovery(CameraType cameraType)
        {
            if (_discoveryServices.TryGetValue(cameraType, out var discoveryService))
            {
                VisionLogger.Instance.Log(LogLevel.Info, $"停止 {cameraType} 发现服务", "CameraDiscoveryAggregator");
                discoveryService.StopDiscovery();
            }
        }
    }
}
