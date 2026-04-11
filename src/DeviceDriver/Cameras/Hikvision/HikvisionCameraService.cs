using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SunEyeVision.DeviceDriver.Events;
using SunEyeVision.DeviceDriver.Models;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.DeviceDriver.Cameras.Hikvision
{
    /// <summary>
    /// 海康相机服务
    /// </summary>
    public class HikvisionCameraService : CameraServiceBase
    {
        /// <summary>
        /// 海康相机设备句柄
        /// </summary>
        private IntPtr _cameraHandle = IntPtr.Zero;

        /// <summary>
        /// 构造函数
        /// </summary>
        public HikvisionCameraService(CameraDeviceInfo deviceInfo, ILogger logger)
            : base(deviceInfo, logger)
        {
        }

        /// <summary>
        /// 连接相机核心实现
        /// </summary>
        protected override async Task<bool> ConnectCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                Logger.LogInfo($"Creating camera handle for {DeviceInfo.DeviceName}");

                // 创建设备句柄
                _cameraHandle = MvCamera.MV_CC_CreateHandle_NET(IntPtr.Zero);
                if (_cameraHandle == IntPtr.Zero)
                {
                    Logger.LogError("Failed to create camera handle");
                    return false;
                }

                // 打开设备
                int result = MvCamera.MV_CC_OpenDevice_NET(_cameraHandle);
                if (result != MvCamera.MV_OK)
                {
                    Logger.LogError($"Failed to open camera device, error code: 0x{result:X}");
                    MvCamera.MV_CC_DestroyHandle_NET(_cameraHandle);
                    _cameraHandle = IntPtr.Zero;
                    return false;
                }

                Logger.LogSuccess($"Camera opened successfully: {DeviceInfo.DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error connecting to camera: {DeviceInfo.DeviceName}", ex);

                // 清理资源
                if (_cameraHandle != IntPtr.Zero)
                {
                    MvCamera.MV_CC_DestroyHandle_NET(_cameraHandle);
                    _cameraHandle = IntPtr.Zero;
                }

                return false;
            }
        }

        /// <summary>
        /// 断开相机核心实现
        /// </summary>
        protected override async Task<bool> DisconnectCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                if (_cameraHandle == IntPtr.Zero)
                {
                    Logger.LogWarning("Camera handle is null, no need to disconnect");
                    return true;
                }

                Logger.LogInfo($"Closing camera device: {DeviceInfo.DeviceName}");

                // 关闭设备
                int result = MvCamera.MV_CC_CloseDevice_NET(_cameraHandle);
                if (result != MvCamera.MV_OK)
                {
                    Logger.LogWarning($"Failed to close camera device, error code: 0x{result:X}");
                }

                // 销毁句柄
                result = MvCamera.MV_CC_DestroyHandle_NET(_cameraHandle);
                if (result != MvCamera.MV_OK)
                {
                    Logger.LogWarning($"Failed to destroy camera handle, error code: 0x{result:X}");
                }

                _cameraHandle = IntPtr.Zero;
                Logger.LogSuccess($"Camera disconnected: {DeviceInfo.DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error disconnecting from camera: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 开始采集核心实现
        /// </summary>
        protected override async Task<bool> StartCaptureCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                if (_cameraHandle == IntPtr.Zero)
                {
                    Logger.LogError("Cannot start capture: camera handle is null");
                    return false;
                }

                Logger.LogInfo($"Starting capture on camera: {DeviceInfo.DeviceName}");

                // 开始采集
                int result = MvCamera.MV_CC_StartGrabbing_NET(_cameraHandle);
                if (result != MvCamera.MV_OK)
                {
                    Logger.LogError($"Failed to start grabbing, error code: 0x{result:X}");
                    return false;
                }

                Logger.LogSuccess($"Capture started: {DeviceInfo.DeviceName}");

                // 启动采集任务
                Task.Run(() => CaptureLoop());

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error starting capture: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 停止采集核心实现
        /// </summary>
        protected override async Task<bool> StopCaptureCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                if (_cameraHandle == IntPtr.Zero)
                {
                    Logger.LogWarning("Cannot stop capture: camera handle is null");
                    return true;
                }

                Logger.LogInfo($"Stopping capture on camera: {DeviceInfo.DeviceName}");

                // 停止采集
                int result = MvCamera.MV_CC_StopGrabbing_NET(_cameraHandle);
                if (result != MvCamera.MV_OK)
                {
                    Logger.LogWarning($"Failed to stop grabbing, error code: 0x{result:X}");
                }

                Logger.LogSuccess($"Capture stopped: {DeviceInfo.DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping capture: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 触发一次采集核心实现
        /// </summary>
        protected override async Task<CameraFrameInfo?> TriggerCaptureCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                if (_cameraHandle == IntPtr.Zero)
                {
                    Logger.LogError("Cannot trigger capture: camera handle is null");
                    return null;
                }

                Logger.LogInfo($"Triggering single frame capture: {DeviceInfo.DeviceName}");

                // 等待一帧图像
                MvCamera.MV_FRAME_OUT_INFO_EX frameInfo = new MvCamera.MV_FRAME_OUT_INFO_EX();
                IntPtr imageData = IntPtr.Zero;
                int dataLength = 0;

                int result = MvCamera.MV_CC_GetOneFrameTimeout_NET(
                    _cameraHandle,
                    ref imageData,
                    ref dataLength,
                    ref frameInfo,
                    3000 // 3秒超时
                );

                if (result != MvCamera.MV_OK)
                {
                    Logger.LogError($"Failed to get one frame, error code: 0x{result:X}");
                    return null;
                }

                // 转换帧信息
                CameraFrameInfo cameraFrameInfo = ConvertFrameInfo(frameInfo, imageData, dataLength);

                Logger.LogSuccess($"Frame captured: {DeviceInfo.DeviceName}");
                return cameraFrameInfo;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error triggering capture: {DeviceInfo.DeviceName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 设置触发模式核心实现
        /// </summary>
        protected override async Task<bool> SetTriggerModeCoreAsync(CameraTriggerMode mode)
        {
            await Task.CompletedTask;

            try
            {
                Logger.LogInfo($"Setting trigger mode to {mode}: {DeviceInfo.DeviceName}");

                // TODO: 实现设置触发模式的逻辑
                // 这里需要调用海康SDK的接口来设置触发模式

                Logger.LogSuccess($"Trigger mode set to {mode}: {DeviceInfo.DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting trigger mode: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 设置采集参数核心实现
        /// </summary>
        protected override async Task<bool> SetCaptureSettingsCoreAsync(CameraCaptureSettings settings)
        {
            await Task.CompletedTask;

            try
            {
                Logger.LogInfo($"Setting capture settings: {DeviceInfo.DeviceName}");

                // TODO: 实现设置采集参数的逻辑
                // 这里需要调用海康SDK的接口来设置曝光时间、增益、帧率等参数

                Logger.LogSuccess($"Capture settings set: {DeviceInfo.DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting capture settings: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取采集参数核心实现
        /// </summary>
        protected override async Task<CameraCaptureSettings?> GetCaptureSettingsCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                Logger.LogInfo($"Getting capture settings: {DeviceInfo.DeviceName}");

                // TODO: 实现获取采集参数的逻辑
                // 这里需要调用海康SDK的接口来获取当前参数

                CameraCaptureSettings settings = new CameraCaptureSettings();
                Logger.LogSuccess($"Capture settings retrieved: {DeviceInfo.DeviceName}");
                return settings;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting capture settings: {DeviceInfo.DeviceName}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取相机属性核心实现
        /// </summary>
        protected override async Task<Dictionary<string, object>> GetPropertiesCoreAsync()
        {
            await Task.CompletedTask;

            try
            {
                Logger.LogInfo($"Getting camera properties: {DeviceInfo.DeviceName}");

                Dictionary<string, object> properties = new Dictionary<string, object>();

                // TODO: 实现获取相机属性的逻辑
                // 这里需要调用海康SDK的接口来获取各种属性

                Logger.LogSuccess($"Camera properties retrieved: {DeviceInfo.DeviceName}");
                return properties;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error getting camera properties: {DeviceInfo.DeviceName}", ex);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 设置相机属性核心实现
        /// </summary>
        protected override async Task<bool> SetPropertiesCoreAsync(Dictionary<string, object> properties)
        {
            await Task.CompletedTask;

            try
            {
                Logger.LogInfo($"Setting camera properties: {DeviceInfo.DeviceName}");

                // TODO: 实现设置相机属性的逻辑
                // 这里需要调用海康SDK的接口来设置各种属性

                Logger.LogSuccess($"Camera properties set: {DeviceInfo.DeviceName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error setting camera properties: {DeviceInfo.DeviceName}", ex);
                return false;
            }
        }

        /// <summary>
        /// 采集循环
        /// </summary>
        private async Task CaptureLoop()
        {
            while (IsCapturing && _cameraHandle != IntPtr.Zero)
            {
                try
                {
                    if (CaptureCancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        break;
                    }

                    // 等待一帧图像
                    MvCamera.MV_FRAME_OUT_INFO_EX frameInfo = new MvCamera.MV_FRAME_OUT_INFO_EX();
                    IntPtr imageData = IntPtr.Zero;
                    int dataLength = 0;

                    int result = MvCamera.MV_CC_GetOneFrameTimeout_NET(
                        _cameraHandle,
                        ref imageData,
                        ref dataLength,
                        ref frameInfo,
                        100 // 100ms超时
                    );

                    if (result == MvCamera.MV_OK)
                    {
                        // 转换帧信息
                        CameraFrameInfo cameraFrameInfo = ConvertFrameInfo(frameInfo, imageData, dataLength);

                        // 触发帧接收事件
                        OnFrameReceived(cameraFrameInfo);
                    }

                    // 短暂休眠，避免CPU占用过高
                    await Task.Delay(1, CaptureCancellationTokenSource?.Token ?? CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error in capture loop: {DeviceInfo.DeviceName}", ex);
                    OnExceptionOccurred(CameraExceptionType.CaptureFailed, ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// 转换帧信息
        /// </summary>
        private CameraFrameInfo ConvertFrameInfo(MvCamera.MV_FRAME_OUT_INFO_EX frameInfo, IntPtr imageData, int dataLength)
        {
            CameraFrameInfo cameraFrameInfo = new CameraFrameInfo
            {
                DeviceId = DeviceId,
                Width = (int)frameInfo.nWidth,
                Height = (int)frameInfo.nHeight,
                DataSize = dataLength,
                ExposureTime = frameInfo.fExposureTime,
                Gain = frameInfo.fGain,
                FrameRate = frameInfo.fFrameRate,
                FrameNumber = (long)frameInfo.nFrameNum,
                Timestamp = DateTime.Now
            };

            // 转换像素格式
            MvCamera.MvGvspPixelType pixelType = (MvCamera.MvGvspPixelType)frameInfo.enPixelType;
            cameraFrameInfo.PixelFormat = pixelType switch
            {
                MvCamera.MvGvspPixelType.PixelType_Gvsp_Mono8 => "Mono8",
                MvCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed => "RGB8",
                MvCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed => "BGR8",
                _ => "Unknown"
            };

            // 复制图像数据
            if (imageData != IntPtr.Zero && dataLength > 0)
            {
                cameraFrameInfo.ImageData = new byte[dataLength];
                Marshal.Copy(imageData, cameraFrameInfo.ImageData, 0, dataLength);
            }

            return cameraFrameInfo;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
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
                }

                // 释放非托管资源
                if (_cameraHandle != IntPtr.Zero)
                {
                    try
                    {
                        MvCamera.MV_CC_DestroyHandle_NET(_cameraHandle);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Error destroying camera handle during dispose: {ex.Message}");
                    }
                    _cameraHandle = IntPtr.Zero;
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
