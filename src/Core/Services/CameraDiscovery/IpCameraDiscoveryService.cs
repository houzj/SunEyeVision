using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.Core.Services.Logging;

namespace SunEyeVision.Core.Services.CameraDiscovery
{
    /// <summary>
    /// IP 相机发现服务
    /// </summary>
    public class IpCameraDiscoveryService : ICameraDiscoveryService
    {
        public CameraType CameraType => CameraType.IP;
        
        private CancellationTokenSource? _cancellationTokenSource;
        
        public async Task<List<DiscoveredCamera>> DiscoverAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            var cameras = new List<DiscoveredCamera>();
            
            try
            {
                // 获取本地网段
                var localIp = GetLocalIpAddress();
                if (string.IsNullOrEmpty(localIp))
                {
                    VisionLogger.Instance.Log(LogLevel.Warning, "无法获取本地 IP 地址", "IpCameraDiscoveryService");
                    return cameras;
                }
                
                var networkSegments = GetNetworkSegments(localIp);
                VisionLogger.Instance.Log(LogLevel.Info, $"开始 IP 相机发现，扫描网段: {string.Join(", ", networkSegments)}", "IpCameraDiscoveryService");
                
                await Task.Run(() =>
                {
                    foreach (var segment in networkSegments)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            break;
                        }
                        
                        ScanSegment(segment, cameras, _cancellationTokenSource.Token);
                    }
                }, _cancellationTokenSource.Token);
                
                VisionLogger.Instance.Log(LogLevel.Success, $"IP 相机发现完成，共发现 {cameras.Count} 台相机", "IpCameraDiscoveryService");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"IP 相机发现失败: {ex.Message}", "IpCameraDiscoveryService", ex);
            }
            
            return cameras;
        }
        
        public void StopDiscovery()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        private void ScanSegment(string segment, List<DiscoveredCamera> cameras, CancellationToken cancellationToken)
        {
            // 扫描网段（RTSP 默认端口 554, ONVIF 默认端口 80）
            var ports = new[] { 554, 80 };
            
            foreach (var port in ports)
            {
                for (int i = 1; i <= 254; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    var ipAddress = $"{segment}.{i}";
                    
                    if (TryConnect(ipAddress, port))
                    {
                        var camera = new DiscoveredCamera
                        {
                            Name = $"IP Camera {ipAddress}",
                            Manufacturer = "Generic",
                            Model = "IP Camera",
                            IpAddress = ipAddress,
                            SerialNumber = ipAddress,
                            CameraType = CameraType.IP,
                            Port = port,
                            Status = CameraStatus.Available
                        };
                        
                        cameras.Add(camera);
                        VisionLogger.Instance.Log(LogLevel.Success, $"发现 IP 相机: {ipAddress}:{port}", "IpCameraDiscoveryService");
                    }
                }
            }
        }
        
        private bool TryConnect(string ipAddress, int port)
        {
            try
            {
                using var client = new TcpClient();
                client.ReceiveTimeout = 500;
                client.Connect(ipAddress, port);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private string? GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"获取本地 IP 失败: {ex.Message}", "IpCameraDiscoveryService");
            }
            return null;
        }
        
        private List<string> GetNetworkSegments(string localIp)
        {
            var segments = new List<string>();
            var parts = localIp.Split('.');
            if (parts.Length == 4)
            {
                var segment = $"{parts[0]}.{parts[1]}.{parts[2]}";
                segments.Add(segment);
            }
            return segments;
        }
    }
}
