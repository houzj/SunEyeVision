using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 配方另存为对话框 ViewModel
    /// </summary>
    public class SaveRecipeAsDialogViewModel : INotifyPropertyChanged
    {
        private readonly List<Project> _projects;
        private string _selectedProjectId = string.Empty;
        private string _recipeName = string.Empty;
        private string _description = string.Empty;

        /// <summary>
        /// 项目列表
        /// </summary>
        public List<Project> Projects => _projects;

        /// <summary>
        /// 选中的目标项目ID
        /// </summary>
        public string SelectedProjectId
        {
            get => _selectedProjectId;
            set
            {
                if (_selectedProjectId != value)
                {
                    _selectedProjectId = value;
                    OnPropertyChanged(nameof(SelectedProjectId));
                }
            }
        }

        /// <summary>
        /// 配方名称
        /// </summary>
        public string RecipeName
        {
            get => _recipeName;
            set
            {
                if (_recipeName != value)
                {
                    _recipeName = value;
                    OnPropertyChanged(nameof(RecipeName));
                }
            }
        }

        /// <summary>
        /// 配方描述
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SaveRecipeAsDialogViewModel(string currentProjectId, string currentRecipeName, 
                                        string? currentDescription, List<Project> projects)
        {
            _projects = projects;

            // 设置默认值
            SelectedProjectId = currentProjectId;
            RecipeName = currentRecipeName + "_副本";
            Description = currentDescription ?? string.Empty;
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(SelectedProjectId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(RecipeName))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// PropertyChanged 事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发 PropertyChanged 事件
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
