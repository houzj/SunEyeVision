using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using SunEyeVision.UI.Controls.Helpers;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 解决方案路径配置对话框 ViewModel
    /// </summary>
    public class SolutionPathDialogViewModel : ViewModelBase
    {
        private string _solutionsPath = string.Empty;
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 解决方案存储路径
        /// </summary>
        public string SolutionsPath
        {
            get => _solutionsPath;
            set => SetProperty(ref _solutionsPath, value, "解决方案路径");
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否路径有效
        /// </summary>
        public bool IsValidPath => !string.IsNullOrWhiteSpace(SolutionsPath) && 
                                   (Directory.Exists(SolutionsPath) || CanCreateDirectory(SolutionsPath));

        /// <summary>
        /// 确认命令
        /// </summary>
        public ICommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 浏览命令
        /// </summary>
        public ICommand BrowseCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SolutionPathDialogViewModel(string currentPath)
        {
            SolutionsPath = currentPath;
            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(Cancel);
            BrowseCommand = new RelayCommand(Browse);

            ValidatePath();
        }

        /// <summary>
        /// 确认
        /// </summary>
        private void Confirm()
        {
            if (!IsValidPath)
            {
                StatusMessage = "请选择有效的解决方案路径";
                return;
            }

            // 确保目录存在
            if (!Directory.Exists(SolutionsPath))
            {
                try
                {
                    Directory.CreateDirectory(SolutionsPath);
                    LogInfo($"创建解决方案目录: {SolutionsPath}");
                }
                catch (Exception ex)
                {
                    LogError($"创建目录失败: {ex.Message}", null, ex);
                    StatusMessage = $"创建目录失败: {ex.Message}";
                    return;
                }
            }

            // 触发确认事件
            OnConfirmed?.Invoke(SolutionsPath);
            LogSuccess($"确认解决方案路径: {SolutionsPath}");
        }

        /// <summary>
        /// 是否可以确认
        /// </summary>
        private bool CanConfirm() => IsValidPath;

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            OnCancelled?.Invoke();
            LogInfo("取消路径配置");
        }

        /// <summary>
        /// 浏览
        /// </summary>
        private void Browse()
        {
            var selectedPath = FolderBrowserHelper.BrowseForFolder(
                description: "选择解决方案存储路径",
                initialPath: SolutionsPath,
                showNewFolderButton: true);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                SolutionsPath = selectedPath;
                ValidatePath();
                LogInfo($"浏览并选择路径: {SolutionsPath}");
            }
        }

        /// <summary>
        /// 验证路径
        /// </summary>
        private void ValidatePath()
        {
            if (string.IsNullOrWhiteSpace(SolutionsPath))
            {
                StatusMessage = "请选择解决方案路径";
                OnPropertyChanged(nameof(IsValidPath));
                return;
            }

            if (!Directory.Exists(SolutionsPath))
            {
                StatusMessage = $"将创建新目录: {SolutionsPath}";
                OnPropertyChanged(nameof(IsValidPath));
                return;
            }

            // 检查目录是否为空
            var files = Directory.GetFiles(SolutionsPath);
            var dirs = Directory.GetDirectories(SolutionsPath);
            if (files.Length > 0 || dirs.Length > 0)
            {
                StatusMessage = $"警告：目录不为空，将使用现有数据";
            }
            else
            {
                StatusMessage = "路径有效";
            }

            OnPropertyChanged(nameof(IsValidPath));
        }

        /// <summary>
        /// 检查是否可以创建目录
        /// </summary>
        private bool CanCreateDirectory(string path)
        {
            try
            {
                var testPath = Path.Combine(path, ".test");
                Directory.CreateDirectory(testPath);
                if (Directory.Exists(testPath))
                {
                    Directory.Delete(testPath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 确认事件
        /// </summary>
        public event Action<string>? OnConfirmed;

        /// <summary>
        /// 取消事件
        /// </summary>
        public event Action? OnCancelled;
    }
}
