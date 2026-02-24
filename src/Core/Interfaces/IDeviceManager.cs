using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCvSharp;

namespace SunEyeVision.Core.Interfaces
{
    /// <summary>
    /// 璁惧绠＄悊鍣ㄦ帴鍙?
    /// </summary>
    public interface IDeviceManager
    {
        /// <summary>
        /// 妫€娴嬪彲鐢ㄨ澶?
        /// </summary>
        /// <returns>璁惧鍒楄〃</returns>
        Task<List<DeviceInfo>> DetectDevicesAsync();

        /// <summary>
        /// 杩炴帴璁惧
        /// </summary>
        /// <param name="deviceId">璁惧ID</param>
        /// <returns>杩炴帴缁撴灉</returns>
        Task<bool> ConnectDeviceAsync(string deviceId);

        /// <summary>
        /// 鏂紑璁惧
        /// </summary>
        /// <param name="deviceId">璁惧ID</param>
        /// <returns>鏂紑缁撴灉</returns>
        Task<bool> DisconnectDeviceAsync(string deviceId);

        /// <summary>
        /// 浠庤澶囪幏鍙栧浘鍍?
        /// </summary>
        /// <param name="deviceId">璁惧ID</param>
        /// <returns>鑾峰彇鐨勫浘鍍?/returns>
        Task<Mat> CaptureImageAsync(string deviceId);

        /// <summary>
        /// 鑾峰彇宸茶繛鎺ョ殑璁惧鍒楄〃
        /// </summary>
        /// <returns>宸茶繛鎺ョ殑璁惧鍒楄〃</returns>
        List<string> GetConnectedDevices();
    }
}
