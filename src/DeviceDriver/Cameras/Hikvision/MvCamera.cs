using System;
using System.Runtime.InteropServices;

namespace SunEyeVision.DeviceDriver.Cameras.Hikvision
{
    /// <summary>
    /// 海康MVS SDK封装类
    /// </summary>
    public class MvCamera
    {
        // 常量定义
        public const int MV_OK = 0;
        public const int MV_GIGE_DEVICE = 0x00000001;
        public const int MV_USB_DEVICE = 0x00000002;

        // 像素类型枚举
        public enum MvGvspPixelType
        {
            PixelType_Gvsp_Mono8 = 0x01080001,
            PixelType_Gvsp_RGB8_Packed = 0x02180014,
            PixelType_Gvsp_BGR8_Packed = 0x02180015
        }

        // 设备信息结构体
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MV_CC_DEVICE_INFO
        {
            public uint nMajorVer;
            public uint nMinorVer;
            public uint nMacAddrHigh;
            public uint nMacAddrLow;
            public uint nTLayerType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] SpecialInfo;
        }

        // 设备信息列表结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct MV_CC_DEVICE_INFO_LIST
        {
            public uint nDeviceNum;
            public IntPtr pDeviceInfo;
        }

        // 帧信息结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct MV_FRAME_OUT_INFO_EX
        {
            public uint nWidth;
            public uint nHeight;
            public uint enPixelType;
            public IntPtr pData;
            public ulong nFrameNum;
            public double fExposureTime;
            public double fGain;
            public double fFrameRate;
            public uint nFrameLen;
            public ulong nDevTimeStampHigh;
            public ulong nDevTimeStampLow;
        }

        // GigE设备信息结构体
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MV_GIGE_DEVICE_INFO
        {
            public uint nCurrentIp;
            public uint nCurrentSubNetMask;
            public uint nDefaultGateWay;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] ManufacturerInfo;
            public uint ManufacturerSpecificInfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ModelName;
            public uint DeviceVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ManufacturerName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] SerialNumber;
            public uint UserDefinedName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] DeviceUserID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Reserved;
        }

        // USB设备信息结构体
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MV_USB3_DEVICE_INFO
        {
            public ushort cchVendorName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] VendorName;
            public ushort cchModelName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ModelName;
            public ushort cchFamilyName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] FamilyName;
            public ushort cchDeviceVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] DeviceVersion;
            public ushort cchManufacturerName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ManufacturerName;
            public ushort cchSerialNumber;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SerialNumber;
            public ushort cchUserDefinedName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] DeviceGUID;
            public uint nDeviceNumber;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Reserved;
        }

        // 图像回调函数委托
        public delegate void CameraImageCallback(IntPtr pData, ref MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser);

        // DLL导入
        private const string DLL_NAME = "MvCameraControl_NET.dll";

        #region SDK初始化与清理

        /// <summary>
        /// 初始化SDK
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_Initialize();

        /// <summary>
        /// 反初始化SDK
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_Finalize();

        #endregion

        #region 设备枚举

        /// <summary>
        /// 枚举设备
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_EnumDevices_NET(ref MV_CC_DEVICE_INFO_LIST pDevList, uint nTLayerType);

        #endregion

        #region 设备管理

        /// <summary>
        /// 创建设备句柄
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MV_CC_CreateHandle_NET(IntPtr pDevInfo);

        /// <summary>
        /// 销毁设备句柄
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_DestroyHandle_NET(IntPtr handle);

        /// <summary>
        /// 打开设备
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_OpenDevice_NET(IntPtr handle, uint nAccessMode = 0, IntPtr pReserved = default);

        /// <summary>
        /// 关闭设备
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_CloseDevice_NET(IntPtr handle);

        /// <summary>
        /// 获取设备信息
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetDeviceInfo_NET(IntPtr handle, ref MV_CC_DEVICE_INFO pDevInfo);

        #endregion

        #region 图像采集

        /// <summary>
        /// 开始采集
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_StartGrabbing_NET(IntPtr handle);

        /// <summary>
        /// 停止采集
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_StopGrabbing_NET(IntPtr handle);

        /// <summary>
        /// 获取一帧图像（带超时）
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetOneFrameTimeout_NET(IntPtr handle, ref IntPtr pData, ref int pnDataLen, ref MV_FRAME_OUT_INFO_EX pFrameInfo, uint nMsec);

        /// <summary>
        /// 释放图像缓冲区
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_FreeImageBuffer_NET(IntPtr handle, ref IntPtr pData);

        /// <summary>
        /// 注册图像回调
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_RegisterImageCallbackEx_NET(IntPtr handle, CameraImageCallback cbImageCallback, IntPtr pUser);

        #endregion

        #region 参数设置

        /// <summary>
        /// 设置整数参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_SetIntValue_NET(IntPtr handle, string strKey, long nValue);

        /// <summary>
        /// 获取整数参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetIntValue_NET(IntPtr handle, string strKey, ref long pnValue);

        /// <summary>
        /// 设置浮点数参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_SetFloatValue_NET(IntPtr handle, string strKey, float fValue);

        /// <summary>
        /// 获取浮点数参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetFloatValue_NET(IntPtr handle, string strKey, ref float pfValue);

        /// <summary>
        /// 设置枚举参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_SetEnumValue_NET(IntPtr handle, string strKey, uint nValue);

        /// <summary>
        /// 获取枚举参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetEnumValue_NET(IntPtr handle, string strKey, ref uint pnValue);

        /// <summary>
        /// 设置布尔参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_SetBoolValue_NET(IntPtr handle, string strKey, bool bValue);

        /// <summary>
        /// 获取布尔参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetBoolValue_NET(IntPtr handle, string strKey, ref bool pbValue);

        /// <summary>
        /// 设置字符串参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_SetStringValue_NET(IntPtr handle, string strKey, string sValue);

        /// <summary>
        /// 获取字符串参数
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_GetStringValue_NET(IntPtr handle, string strKey, ref string psValue);

        /// <summary>
        /// 执行命令
        /// </summary>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MV_CC_SetCommandValue_NET(IntPtr handle, string strKey);

        #endregion
    }
}
