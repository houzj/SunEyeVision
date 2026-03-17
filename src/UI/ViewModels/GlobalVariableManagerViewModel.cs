using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 全局变量管理器 ViewModel
    /// </summary>
    public class GlobalVariableManagerViewModel : ViewModelBase
    {
        private readonly SolutionManager _solutionManager;

        /// <summary>
        /// 全局变量列表
        /// </summary>
        public ObservableCollection<GlobalVariable> Variables { get; }

        /// <summary>
        /// 选中的变量
        /// </summary>
        private GlobalVariable? _selectedVariable;
        public GlobalVariable? SelectedVariable
        {
            get => _selectedVariable;
            set => SetProperty(ref _selectedVariable, value);
        }

        /// <summary>
        /// 添加变量命令
        /// </summary>
        public ICommand AddVariableCommand { get; private set; }

        /// <summary>
        /// 删除变量命令
        /// </summary>
        public ICommand DeleteVariableCommand { get; private set; }

        /// <summary>
        /// 清空变量命令
        /// </summary>
        public ICommand ClearVariablesCommand { get; private set; }

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; private set; }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public ICommand CloseCommand { get; private set; }

        public GlobalVariableManagerViewModel(SolutionManager solutionManager)
        {
            _solutionManager = solutionManager ?? throw new ArgumentNullException(nameof(solutionManager));

            var currentSolution = _solutionManager.CurrentSolution;
            Variables = new ObservableCollection<GlobalVariable>(
                currentSolution?.GlobalVariables ?? Enumerable.Empty<GlobalVariable>()
            );

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            AddVariableCommand = new RelayCommand(AddVariable, () => true);
            DeleteVariableCommand = new RelayCommand(DeleteVariable, CanDeleteVariable);
            ClearVariablesCommand = new RelayCommand(ClearVariables, CanClearVariables);
            SaveCommand = new RelayCommand(Save, CanSave);
            CloseCommand = new RelayCommand(Close, () => true);
        }

        /// <summary>
        /// 添加变量
        /// </summary>
        private void AddVariable()
        {
            var variable = new GlobalVariable
            {
                Name = $"变量{Variables.Count + 1}",
                Type = "String",
                Value = "",
                Description = ""
            };

            Variables.Add(variable);
            _solutionManager.CurrentSolution?.GlobalVariables.Add(variable);
            SelectedVariable = variable;

            LogInfo($"添加全局变量: {variable.Name}");
        }

        /// <summary>
        /// 删除变量
        /// </summary>
        private void DeleteVariable()
        {
            if (SelectedVariable == null) return;

            Variables.Remove(SelectedVariable);
            _solutionManager.CurrentSolution?.GlobalVariables.Remove(SelectedVariable);

            LogInfo($"删除全局变量: {SelectedVariable.Name}");
            SelectedVariable = null;
        }

        /// <summary>
        /// 是否可以删除变量
        /// </summary>
        private bool CanDeleteVariable()
        {
            return SelectedVariable != null;
        }

        /// <summary>
        /// 清空所有变量
        /// </summary>
        private void ClearVariables()
        {
            Variables.Clear();
            _solutionManager.CurrentSolution?.GlobalVariables.Clear();

            LogInfo("清空所有全局变量");
            SelectedVariable = null;
        }

        /// <summary>
        /// 是否可以清空变量
        /// </summary>
        private bool CanClearVariables()
        {
            return Variables.Count > 0;
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
                    LogSuccess("全局变量保存成功");
                }
                catch (Exception ex)
                {
                    LogError($"保存全局变量失败: {ex.Message}");
                }
            }
            else
            {
                LogWarning("解决方案未保存，无法保存全局变量");
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
            LogInfo("关闭全局变量管理器");
        }
    }
}
