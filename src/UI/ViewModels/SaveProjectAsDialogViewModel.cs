using System;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 项目另存为对话框 ViewModel
    /// </summary>
    public class SaveProjectAsDialogViewModel : ViewModelBase
    {
        private string _projectName = string.Empty;
        private string _projectPath = string.Empty;
        private string _description = string.Empty;

        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value, "项目名称");
        }

        /// <summary>
        /// 项目路径
        /// </summary>
        public string ProjectPath
        {
            get => _projectPath;
            set => SetProperty(ref _projectPath, value, "保存路径");
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value, "描述");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SaveProjectAsDialogViewModel(string defaultName, string defaultPath, string? defaultDescription)
        {
            ProjectName = defaultName;
            ProjectPath = defaultPath;
            Description = defaultDescription ?? string.Empty;
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                LogWarning("项目名称不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ProjectPath))
            {
                LogWarning("项目路径不能为空");
                return false;
            }

            return true;
        }
    }
}
