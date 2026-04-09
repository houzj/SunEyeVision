using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SunEyeVision.DeviceDriver.Cameras;
using SunEyeVision.DeviceDriver.Events;
using SunEyeVision.DeviceDriver.Models;
using SunEyeVision.Plugin.SDK.Logging;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 相机管理器 ViewModel（重构版本，集成DeviceDriver层）
    /// </summary>
    public class CameraManagerViewModel : ViewModelBase
    {
        private readonly SolutionManager _solutionManager;
        private readonly CameraPoolManager _cameraPoolManager;
        private readonly ILogger _logger;

        /// <summary>
        /// 相机设备列表
        /// </summary>
        public ObservableCollection<CameraDevice> Cameras { get; }

        /// <summary>
        /// 相机详情 ViewModel
        /// </summary>
        public CameraDetailViewModel CameraDetailViewModel { get; }

        /// <summary>
        /// 状态消息
        /// </summary>
        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 已连接数量
        /// </summary>
        public int ConnectedCount => Cameras.Count(c => c.IsEnabled);

        /// <summary>
        /// 已禁用数量
        /// </summary>
        public int DisabledCount => Cameras.Count(c => !c.IsEnabled);

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount => Cameras.Count;

        /// <summary>
        /// 选中的相机
        /// </summary>
        private CameraDevice? _selectedCamera;
        public CameraDevice? SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                SetProperty(ref _selectedCamera, value);
                if (value != null)
                {
                    CameraDetailViewModel.UpdateCamera(value);
                }
            }
        }

        /// <summary>
        /// 添加相机命令
        /// </summary>
        public ICommand AddCameraCommand { get; private set; }

        /// <summary>
        /// 删除相机命令
        /// </summary>
        public ICommand DeleteCameraCommand { get; private set; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; private set; }

        /// <summary>
        /// 导出命令
        /// </summary>
        public ICommand ExportCommand { get; private set; }

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 全局设置命令
        /// </summary>
        public ICommand GlobalSettingsCommand { get; private set; }

        /// <summary>
        /// 批量连接命令
        /// </summary>
        public ICommand BatchConnectCommand { get; private set; }

        /// <summary>
        /// 批量断开命令
        /// </summary>
        public ICommand BatchDisconnectCommand { get; private set; }

        /// <summary>
        /// 批量启用命令
        /// </summary>
        public ICommand BatchEnableCommand { get; private set; }

        /// <summary>
        /// 批量禁用命令
        /// </summary>
        public ICommand BatchDisableCommand { get; private set; }

        /// <summary>
        /// 发现相机命令
        /// </summary>
        public ICommand DiscoverCamerasCommand { get; private set; }

        public CameraManagerViewModel(SolutionManager solutionManager, CameraPoolManager cameraPoolManager, ILogger logger)
        {
            _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));
            _cameraPoolManager = cameraPoolManager ?? throw new ArgumentNullException(nameof(cameraPoolManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Cameras = new ObservableCollection<CameraDevice>();
            CameraDetailViewModel = new CameraDetailViewModel();

            // 订阅相机池管理器事件
            _cameraPoolManager.CameraConnectionStateChanged += OnCameraConnectionStateChanged;
            _cameraPoolManager.CameraExceptionOccurred += OnCameraExceptionOccurred;

            LoadCamerasFromSolution();

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            AddCameraCommand = new RelayCommand(AddCamera);
            DeleteCameraCommand = new RelayCommand(DeleteCamera, CanDeleteCamera);
            RefreshCommand = new RelayCommand(Refresh);
            ExportCommand = new RelayCommand(Export);
            SaveCommand = new RelayCommand(Save);
            GlobalSettingsCommand = new RelayCommand(GlobalSettings);

            BatchConnectCommand = new RelayCommand(BatchConnect);
            BatchDisconnectCommand = new RelayCommand(BatchDisconnect);
            BatchEnableCommand = new RelayCommand(BatchEnable);
            BatchDisableCommand = new RelayCommand(BatchDisable);

            DiscoverCamerasCommand = new RelayCommand(async () => await DiscoverCamerasAsync());
        }

        private void LoadCamerasFromSolution()
        {
            var currentSolution = _solutionManager.CurrentSolution;
            if (currentSolution != null)
            {
                var devices = currentSolution.Devices.Where(d => d.Type == DeviceType.Camera);
                foreach (var device in devices)
                {
                    Cameras.Add(new CameraDevice
                    {
                        Name = device.Name,
                        CameraType = device.Manufacturer ?? "Generic",
                        IpAddress = device.IpAddress,
                        Port = device.Port.ToString(),
                        Manufacturer = device.Manufacturer,
                        Model = device.Model,
                        Description = device.Description,
                        IsEnabled = device.IsEnabled,
                        StatusIcon = device.Status == DeviceStatus.Connected ? "🟢" : "🔴",
                        StatusText = device.Status == DeviceStatus.Connected ? "已连接" : "未连接",
                        LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            OnPropertyChanged(nameof(ConnectedCount));
            OnPropertyChanged(nameof(DisabledCount));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 发现相机
        /// </summary>
        private async Task DiscoverCamerasAsync()
        {
            try
            {
                StatusMessage = "正在扫描相机...";
                _logger.LogInfo("Starting camera discovery...");

                var discoveredCameras = await _cameraPoolManager.DiscoverAllCamerasAsync();

                if (discoveredCameras.Count == 0)
                {
                    StatusMessage = "未发现相机";
                    _logger.LogWarning("No cameras discovered");
                    return;
                }

                // 添加发现的相机到现有列表（不清空）
                foreach (var cameraInfo in discoveredCameras)
                {
                    var cameraDevice = new CameraDevice
                    {
                        Name = cameraInfo.DeviceName,
                        CameraType = cameraInfo.Manufacturer,
                        IpAddress = cameraInfo.IpAddress,
                        Port = cameraInfo.Port.ToString(),
                        Manufacturer = cameraInfo.Manufacturer,
                        Model = cameraInfo.Model,
                        Description = $"{cameraInfo.Model} ({cameraInfo.SerialNumber})",
                        SerialNumber = cameraInfo.SerialNumber,
                        DeviceId = cameraInfo.DeviceId,
                        StatusIcon = "🔴",
                        StatusText = "未连接",
                        IsEnabled = false,
                        LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    Cameras.Add(cameraDevice);

                    // 同时添加到解决方案的设备列表
                    var device = new Device
                    {
                        Name = cameraInfo.DeviceName,
                        Type = DeviceType.Camera,
                        IpAddress = cameraInfo.IpAddress,
                        Port = cameraInfo.Port,
                        Manufacturer = cameraInfo.Manufacturer,
                        Model = cameraInfo.Model,
                        SerialNumber = cameraInfo.SerialNumber,
                        Description = cameraDevice.Description,
                        IsEnabled = false,
                        Status = DeviceStatus.Disconnected
                    };
                    _solutionManager.CurrentSolution?.Devices.Add(device);

                    // 创建相机服务
                    _cameraPoolManager.CreateCameraService(cameraInfo);
                }

                StatusMessage = $"扫描完成，发现 {discoveredCameras.Count} 台相机";
                _logger.LogSuccess($"Camera discovery completed, found {discoveredCameras.Count} camera(s)");

                OnPropertyChanged(nameof(ConnectedCount));
                OnPropertyChanged(nameof(DisabledCount));
                OnPropertyChanged(nameof(TotalCount));
            }
            catch (Exception ex)
            {
                StatusMessage = $"扫描失败: {ex.Message}";
                _logger.LogError("Error during camera discovery", ex);
            }
        }

        /// <summary>
        /// 添加相机
        /// </summary>
        private void AddCamera()
        {
            var camera = new CameraDevice
            {
                Name = $"相机{Cameras.Count + 1}",
                CameraType = "Generic",
                IpAddress = "192.168.1.100",
                Port = "5000",
                Manufacturer = "",
                Model = "",
                Description = "",
                StatusIcon = "🔴",
                StatusText = "未连接",
                IsEnabled = false,
                LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Cameras.Add(camera);

            // 同时添加到解决方案的设备列表
            var device = new Device
            {
                Name = camera.Name,
                Type = DeviceType.Camera,
                IpAddress = camera.IpAddress,
                Port = int.Parse(camera.Port),
                Manufacturer = camera.Manufacturer,
                Model = camera.Model,
                Description = camera.Description,
                IsEnabled = camera.IsEnabled
            };
            _solutionManager.CurrentSolution?.Devices.Add(device);

            SelectedCamera = camera;
            LogInfo($"添加相机: {camera.Name}");

            OnPropertyChanged(nameof(ConnectedCount));
            OnPropertyChanged(nameof(DisabledCount));
            OnPropertyChanged(nameof(TotalCount));
        }

        /// <summary>
        /// 删除相机
        /// </summary>
        private void DeleteCamera()
        {
            if (SelectedCamera == null) return;

            // 从相机池管理器中移除
            if (!string.IsNullOrEmpty(SelectedCamera.DeviceId))
            {
                _cameraPoolManager.RemoveCameraService(SelectedCamera.DeviceId);
            }

            Cameras.Remove(SelectedCamera);

            // 从解决方案的设备列表中删除
            var deviceToRemove = _solutionManager.CurrentSolution?.Devices
                .FirstOrDefault(d => d.Name == SelectedCamera.Name);
            if (deviceToRemove != null)
            {
                _solutionManager.CurrentSolution?.Devices.Remove(deviceToRemove);
            }

            LogInfo($"删除相机: {SelectedCamera.Name}");
            SelectedCamera = null;
        }

        /// <summary>
        /// 是否可以删除相机
        /// </summary>
        private bool CanDeleteCamera()
        {
            return SelectedCamera != null;
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        private async void TestConnection()
        {
            if (SelectedCamera == null) return;

            if (string.IsNullOrEmpty(SelectedCamera.DeviceId))
            {
                LogWarning("相机设备ID为空，无法连接");
                return;
            }

            LogInfo($"测试相机连接: {SelectedCamera.Name}");
            LogInfo($"  地址: {SelectedCamera.IpAddress}:{SelectedCamera.Port}");

            try
            {
                SelectedCamera.StatusIcon = "🟡";
                SelectedCamera.StatusText = "连接中...";
                OnPropertyChanged(nameof(SelectedCamera));

                var cameraService = _cameraPoolManager.GetCameraService(SelectedCamera.DeviceId);
                if (cameraService == null)
                {
                    SelectedCamera.StatusIcon = "🔴";
                    SelectedCamera.StatusText = "连接失败";
                    OnPropertyChanged(nameof(SelectedCamera));
                    LogError($"相机服务不存在: {SelectedCamera.Name}");
                    return;
                }

                bool result = await cameraService.ConnectAsync();

                if (result)
                {
                    SelectedCamera.StatusIcon = "🟢";
                    SelectedCamera.StatusText = "已连接";
                    SelectedCamera.IsEnabled = true;
                    OnPropertyChanged(nameof(SelectedCamera));

                    LogSuccess($"相机连接成功: {SelectedCamera.Name}");
                    OnPropertyChanged(nameof(ConnectedCount));
                    OnPropertyChanged(nameof(DisabledCount));
                }
                else
                {
                    SelectedCamera.StatusIcon = "🔴";
                    SelectedCamera.StatusText = "连接失败";
                    OnPropertyChanged(nameof(SelectedCamera));

                    LogError($"相机连接失败: {SelectedCamera.Name}");
                }
            }
            catch (Exception ex)
            {
                SelectedCamera.StatusIcon = "🔴";
                SelectedCamera.StatusText = "连接失败";
                OnPropertyChanged(nameof(SelectedCamera));

                LogError($"相机连接测试失败: {SelectedCamera.Name}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        private void Save()
        {
            var currentSolution = _solutionManager.CurrentSolution;
            if (currentSolution?.FilePath != null)
            {
                try
                {
                    _solutionManager.SaveSolution(currentSolution.FilePath);
                    LogSuccess("相机配置保存成功");
                }
                catch (Exception ex)
                {
                    LogError($"保存相机配置失败: {ex.Message}");
                }
            }
            else
            {
                LogWarning("解决方案未保存，无法保存相机配置");
            }
        }

        /// <summary>
        /// 是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return _solutionManager.CurrentSolution?.FilePath != null;
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private void Refresh()
        {
            StatusMessage = "正在刷新...";
            LoadCamerasFromSolution();
            StatusMessage = "刷新完成";
        }

        /// <summary>
        /// 导出
        /// </summary>
        private void Export()
        {
            StatusMessage = "导出功能开发中...";
        }

        /// <summary>
        /// 全局设置
        /// </summary>
        private void GlobalSettings()
        {
            StatusMessage = "全局设置功能开发中...";
        }

        /// <summary>
        /// 批量连接
        /// </summary>
        private async void BatchConnect()
        {
            var selectedCameras = Cameras.Where(c => c.IsSelected && !string.IsNullOrEmpty(c.DeviceId)).ToList();

            foreach (var camera in selectedCameras)
            {
                var cameraService = _cameraPoolManager.GetCameraService(camera.DeviceId);
                if (cameraService != null)
                {
                    try
                    {
                        await cameraService.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error connecting camera: {camera.Name}", ex);
                    }
                }
            }

            StatusMessage = $"已连接 {selectedCameras.Count} 个相机";
            OnPropertyChanged(nameof(ConnectedCount));
            OnPropertyChanged(nameof(DisabledCount));
        }

        /// <summary>
        /// 批量断开
        /// </summary>
        private async void BatchDisconnect()
        {
            var selectedCameras = Cameras.Where(c => c.IsSelected && !string.IsNullOrEmpty(c.DeviceId)).ToList();

            foreach (var camera in selectedCameras)
            {
                var cameraService = _cameraPoolManager.GetCameraService(camera.DeviceId);
                if (cameraService != null)
                {
                    try
                    {
                        await cameraService.DisconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error disconnecting camera: {camera.Name}", ex);
                    }
                }
            }

            StatusMessage = $"已断开 {selectedCameras.Count} 个相机";
            OnPropertyChanged(nameof(ConnectedCount));
            OnPropertyChanged(nameof(DisabledCount));
        }

        /// <summary>
        /// 批量启用
        /// </summary>
        private void BatchEnable()
        {
            foreach (var camera in Cameras.Where(c => c.IsSelected))
            {
                camera.IsEnabled = true;
            }
            StatusMessage = $"已启用 {Cameras.Count(c => c.IsSelected)} 个相机";
            OnPropertyChanged(nameof(DisabledCount));
        }

        /// <summary>
        /// 批量禁用
        /// </summary>
        private void BatchDisable()
        {
            foreach (var camera in Cameras.Where(c => c.IsSelected))
            {
                camera.IsEnabled = false;
            }
            StatusMessage = $"已禁用 {Cameras.Count(c => c.IsSelected)} 个相机";
            OnPropertyChanged(nameof(DisabledCount));
        }

        /// <summary>
        /// 处理相机连接状态变更事件
        /// </summary>
        private void OnCameraConnectionStateChanged(object? sender, CameraConnectionEvent e)
        {
            var camera = Cameras.FirstOrDefault(c => c.DeviceId == e.DeviceId);
            if (camera != null)
            {
                switch (e.ConnectionState)
                {
                    case CameraConnectionState.Connected:
                        camera.StatusIcon = "🟢";
                        camera.StatusText = "已连接";
                        camera.IsEnabled = true;
                        camera.LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case CameraConnectionState.Disconnected:
                        camera.StatusIcon = "🔴";
                        camera.StatusText = "未连接";
                        camera.IsEnabled = false;
                        camera.LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case CameraConnectionState.Connecting:
                        camera.StatusIcon = "🟡";
                        camera.StatusText = "连接中...";
                        break;
                    case CameraConnectionState.Error:
                        camera.StatusIcon = "🔴";
                        camera.StatusText = "错误";
                        camera.LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                }

                OnPropertyChanged(nameof(SelectedCamera));
                OnPropertyChanged(nameof(ConnectedCount));
                OnPropertyChanged(nameof(DisabledCount));
            }
        }

        /// <summary>
        /// 处理相机异常事件
        /// </summary>
        private void OnCameraExceptionOccurred(object? sender, CameraExceptionEvent e)
        {
            _logger.LogError($"Camera exception: {e.DeviceId} - {e.Message}");

            var camera = Cameras.FirstOrDefault(c => c.DeviceId == e.DeviceId);
            if (camera != null)
            {
                camera.StatusIcon = "🔴";
                camera.StatusText = $"错误: {e.ExceptionType}";
                camera.LastOperationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                OnPropertyChanged(nameof(SelectedCamera));
            }

            StatusMessage = $"相机异常: {e.Message}";
        }

        private void LogInfo(string message)
        {
            StatusMessage = message;
            _logger.LogInfo(message);
        }

        private void LogSuccess(string message)
        {
            StatusMessage = $"✓ {message}";
            _logger.LogSuccess(message);
        }

        private void LogWarning(string message)
        {
            StatusMessage = $"⚠ {message}";
            _logger.LogWarning(message);
        }

        private void LogError(string message)
        {
            StatusMessage = $"✗ {message}";
            _logger.LogError(message);
        }
    }

    /// <summary>
    /// 相机设备模型（扩展版本，包含DeviceId）
    /// </summary>
    public class CameraDevice : INotifyPropertyChanged
    {
        private string _name;
        private string _cameraType;
        private string _statusText;
        private string _statusIcon;
        private string _ipAddress;
        private string _lastOperationTime;
        private bool _isSelected;
        private bool _isEnabled;
        private string _manufacturer;
        private string _model;
        private string _description;
        private string _port;
        private string _latency;
        private string _frameRate;
        private string _deviceId;
        private string _serialNumber;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public string CameraType
        {
            get => _cameraType;
            set
            {
                _cameraType = value;
                OnPropertyChanged(nameof(CameraType));
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusIcon
        {
            get => _statusIcon;
            set
            {
                _statusIcon = value;
                OnPropertyChanged(nameof(StatusIcon));
            }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }

        public string LastOperationTime
        {
            get => _lastOperationTime;
            set
            {
                _lastOperationTime = value;
                OnPropertyChanged(nameof(LastOperationTime));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public string Manufacturer
        {
            get => _manufacturer;
            set
            {
                _manufacturer = value;
                OnPropertyChanged(nameof(Manufacturer));
            }
        }

        public string Model
        {
            get => _model;
            set
            {
                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        public string Latency
        {
            get => _latency;
            set
            {
                _latency = value;
                OnPropertyChanged(nameof(Latency));
            }
        }

        public string FrameRate
        {
            get => _frameRate;
            set
            {
                _frameRate = value;
                OnPropertyChanged(nameof(FrameRate));
            }
        }

        public string DeviceId
        {
            get => _deviceId;
            set
            {
                _deviceId = value;
                OnPropertyChanged(nameof(DeviceId));
            }
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set
            {
                _serialNumber = value;
                OnPropertyChanged(nameof(SerialNumber));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
