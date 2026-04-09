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
    /// GigE 相机发现服务
    /// </summary>
    public class GigeCameraDiscoveryService : ICameraDiscoveryService
    {
        public CameraType CameraType => CameraType.GigE;
        
        private CancellationTokenSource? _cancellationTokenSource;
        
        public async Task<List<DiscoveredCamera>> DiscoverAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            var cameras = new List<DiscoveredCamera>();
            
            try
            {
                // 创建 UDP 客户端
                using var udpClient = new UdpClient();
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 3956));
                
                // 加入多播组
                udpClient.JoinMulticastGroup(IPAddress.Parse("239.192.168.10"));
                
                // 发送 GVCP Discovery 命令
                var discoveryCommand = CreateGvcpDiscoveryCommand();
                udpClient.Send(discoveryCommand, discoveryCommand.Length, new IPEndPoint(IPAddress.Parse("239.192.168.10"), 3956));
                
                VisionLogger.Instance.Log(LogLevel.Info, "开始 GigE 相机发现", "GigeCameraDiscoveryService");
                
                // 接收响应（超时 5 秒）
                var endTime = DateTime.Now.AddSeconds(5);
                
                while (DateTime.Now < endTime && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    udpClient.Client.ReceiveTimeout = 1000;
                    
                    try
                    {
                        var receiveResult = await udpClient.ReceiveAsync();
                        
                        if (receiveResult.Buffer.Length > 0)
                        {
                            var camera = ParseGvcpDiscoveryResponse(receiveResult.Buffer);
                            if (camera != null)
                            {
                                cameras.Add(camera);
                                VisionLogger.Instance.Log(LogLevel.Success, $"发现相机: {camera.Name} ({camera.IpAddress})", "GigeCameraDiscoveryService");
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        // 超时，继续等待
                    }
                }
                
                VisionLogger.Instance.Log(LogLevel.Success, $"GigE 相机发现完成，共发现 {cameras.Count} 台相机", "GigeCameraDiscoveryService");
            }
            catch (OperationCanceledException)
            {
                VisionLogger.Instance.Log(LogLevel.Info, "GigE 相机发现已取消", "GigeCameraDiscoveryService");
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Error, $"GigE 相机发现失败: {ex.Message}", "GigeCameraDiscoveryService", ex);
            }
            
            return cameras;
        }
        
        public void StopDiscovery()
        {
            _cancellationTokenSource?.Cancel();
        }
        
        /// <summary>
        /// 创建 GVCP Discovery 命令
        /// </summary>
        private byte[] CreateGvcpDiscoveryCommand()
        {
            // GVCP Discovery 命令格式
            // 命令码: 0x0002 (Discovery)
            // 强制 IP: 0x00000001
            var command = new byte[16];
            command[0] = 0x42; // Magic Key
            command[1] = 0x01;
            command[2] = 0x00;  // Flag
            command[3] = 0x00;
            command[4] = 0x00;  // Command High
            command[5] = 0x02;  // Command Low (Discovery)
            command[6] = 0x00;  // Length High
            command[7] = 0x04;  // Length Low
            command[8] = 0x00;  // Reserved
            command[9] = 0x00;
            command[10] = 0x00;
            command[11] = 0x01; // Force IP
            command[12] = 0x00;
            command[13] = 0x00;
            command[14] = 0x00;
            command[15] = 0x00;
            
            return command;
        }
        
        /// <summary>
        /// 解析 GVCP Discovery 响应
        /// </summary>
        private DiscoveredCamera? ParseGvcpDiscoveryResponse(byte[] buffer)
        {
            try
            {
                // 检查 Magic Key
                if (buffer.Length < 128 || buffer[0] != 0x42 || buffer[1] != 0x01)
                {
                    return null;
                }
                
                // 解析 Manufacturer（字符串从偏移 44 开始，长度 32）
                var manufacturer = Encoding.ASCII.GetString(buffer, 44, 32).TrimEnd('\0');
                
                // 解析 Model（字符串从偏移 76 开始，长度 32）
                var model = Encoding.ASCII.GetString(buffer, 76, 32).TrimEnd('\0');
                
                // 解析 Serial Number（字符串从偏移 108 开始，长度 16）
                var serialNumber = Encoding.ASCII.GetString(buffer, 108, 16).TrimEnd('\0');
                
                // 解析 IP 地址
                var ipAddress = $"{buffer[124]}.{buffer[125]}.{buffer[126]}.{buffer[127]}";
                
                // 解析 MAC 地址
                var macAddress = $"{buffer[128]:X2}:{buffer[129]:X2}:{buffer[130]:X2}:{buffer[131]:X2}:{buffer[132]:X2}:{buffer[133]:X2}";
                
                return new DiscoveredCamera
                {
                    Name = $"{manufacturer} {model}",
                    Manufacturer = manufacturer,
                    Model = model,
                    IpAddress = ipAddress,
                    SerialNumber = serialNumber,
                    CameraType = CameraType.GigE,
                    Port = 3956,
                    Status = CameraStatus.Available,
                    MacAddress = macAddress
                };
            }
            catch (Exception ex)
            {
                VisionLogger.Instance.Log(LogLevel.Warning, $"解析 GVCP 响应失败: {ex.Message}", "GigeCameraDiscoveryService");
                return null;
            }
        }
    }
}
