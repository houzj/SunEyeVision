using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 设备面板视图模型
    /// </summary>
    public class DevicePanelViewModel : ViewModelBase
    {
        private DeviceItem? _selectedDevice;
        private string _connectionStatus = "未连接设�?;

        public ObservableCollection<DeviceItem> Devices { get; }

        public DeviceItem? SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public ICommand ConnectDeviceCommand { get; }
        public ICommand DisconnectDeviceCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand CaptureImageCommand { get; }

        public DevicePanelViewModel()
        {
            Devices = new ObservableCollection<DeviceItem>();

            ConnectDeviceCommand = new RelayCommand<DeviceItem>(ExecuteConnectDevice, CanConnectDevice);
            DisconnectDeviceCommand = new RelayCommand<DeviceItem>(ExecuteDisconnectDevice, CanDisconnectDevice);
            RefreshDevicesCommand = new RelayCommand(ExecuteRefreshDevices);
            CaptureImageCommand = new RelayCommand<DeviceItem>(ExecuteCaptureImage, CanCaptureImage);

            InitializeDevices();
        }

        private void InitializeDevices()
        {
            Devices.Add(new DeviceItem("CAM001", "相机1", "Camera"));
            Devices.Add(new DeviceItem("CAM002", "相机2", "Camera"));
            Devices.Add(new DeviceItem("IO001", "数字IO", "IO"));
            Devices.Add(new DeviceItem("PLC001", "PLC控制�?, "PLC"));
        }

        private bool CanConnectDevice(DeviceItem? device)
        {
            return device != null && !device.IsConnected;
        }

        private bool CanDisconnectDevice(DeviceItem? device)
        {
            return device != null && device.IsConnected;
        }

        private bool CanCaptureImage(DeviceItem? device)
        {
            return device != null && device.IsConnected && device.Type == "Camera";
        }

        private async void ExecuteConnectDevice(DeviceItem? device)
        {
            if (device == null) return;

            await Task.Delay(500);

            device.IsConnected = true;
            device.Status = "已连�?;
            ConnectionStatus = $"{device.Name} 连接成功";
        }

        private void ExecuteDisconnectDevice(DeviceItem? device)
        {
            if (device == null) return;

            device.IsConnected = false;
            device.Status = "未连�?;
            ConnectionStatus = $"{device.Name} 已断开";
        }

        private async void ExecuteRefreshDevices()
        {
            await Task.Delay(300);

            var connectedCount = Devices.Count(d => d.IsConnected);
            ConnectionStatus = connectedCount > 0
                ? $"{connectedCount} 个设备已连接"
                : "未连接设�?;
        }

        private async void ExecuteCaptureImage(DeviceItem? device)
        {
            if (device == null) return;

            await Task.Delay(200);

            ConnectionStatus = $"已从 {device.Name} 采集图像";
            // TODO: 处理采集的图�?
        }
    }
}
