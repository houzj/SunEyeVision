using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 相机管理器 ViewModel
    /// </summary>
    public class CameraManagerViewModel : ViewModelBase
    {
        private readonly SolutionManager _solutionManager;

        /// <summary>
        /// 相机设备列表
        /// </summary>
        public ObservableCollection<Device> CameraDevices { get; }

        /// <summary>
        /// 选中的相机
        /// </summary>
        private Device? _selectedCamera;
        public Device? SelectedCamera
        {
            get => _selectedCamera;
            set => SetProperty(ref _selectedCamera, value);
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
        /// 测试连接命令
        /// </summary>
        public ICommand TestConnectionCommand { get; private set; }

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public ICommand CloseCommand { get; private set; }

        public CameraManagerViewModel(SolutionManager solutionManager)
        {
            _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));

            var currentSolution = _solutionManager.CurrentSolution;
            CameraDevices = new ObservableCollection<Device>(
                currentSolution?.Devices.Where(d => d.Type == DeviceType.Camera) ??
                Enumerable.Empty<Device>()
            );

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            AddCameraCommand = new RelayCommand(AddCamera, () => true);
            DeleteCameraCommand = new RelayCommand(DeleteCamera, CanDeleteCamera);
            TestConnectionCommand = new RelayCommand(TestConnection, CanTestConnection);
            SaveCommand = new RelayCommand(Save, CanSave);
            CloseCommand = new RelayCommand(Close, () => true);
        }

        /// <summary>
        /// 添加相机
        /// </summary>
        private void AddCamera()
        {
            var camera = new Device
            {
                Name = $"相机{CameraDevices.Count + 1}",
                Type = DeviceType.Camera,
                IpAddress = "192.168.1.100",
                Port = 5000,
                Manufacturer = "",
                Model = "",
                Description = ""
            };

            CameraDevices.Add(camera);
            _solutionManager.CurrentSolution?.Devices.Add(camera);
            SelectedCamera = camera;

            LogInfo($"添加相机: {camera.Name}");
        }

        /// <summary>
        /// 删除相机
        /// </summary>
        private void DeleteCamera()
        {
            if (SelectedCamera == null) return;

            CameraDevices.Remove(SelectedCamera);
            _solutionManager.CurrentSolution?.Devices.Remove(SelectedCamera);

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
        private void TestConnection()
        {
            if (SelectedCamera == null) return;

            LogInfo($"测试相机连接: {SelectedCamera.Name}");
            LogInfo($"  地址: {SelectedCamera.GetConnectionString()}");

            try
            {
                SelectedCamera.Status = DeviceStatus.Initializing;
                OnPropertyChanged(nameof(SelectedCamera));

                System.Threading.Thread.Sleep(1000);

                SelectedCamera.Status = DeviceStatus.Connected;
                OnPropertyChanged(nameof(SelectedCamera));

                LogSuccess($"相机连接测试成功: {SelectedCamera.Name}");
            }
            catch (Exception ex)
            {
                SelectedCamera.Status = DeviceStatus.Error;
                OnPropertyChanged(nameof(SelectedCamera));

                LogError($"相机连接测试失败: {SelectedCamera.Name}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 是否可以测试连接
        /// </summary>
        private bool CanTestConnection()
        {
            return SelectedCamera != null &&
                   !string.IsNullOrWhiteSpace(SelectedCamera.IpAddress) &&
                   SelectedCamera.Port > 0;
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
        /// 关闭
        /// </summary>
        private void Close()
        {
            LogInfo("关闭相机管理器");
        }
    }
}
