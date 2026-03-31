using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SunEyeVision.Core.Events;
using SunEyeVision.Plugin.SDK.Models;
using SunEyeVision.UI.Models;

namespace SunEyeVision.UI.ViewModels
{
    /// <summary>
    /// 工作流标签页管理ViewModel
    /// </summary>
    public class WorkflowTabControlViewModel : ObservableObject
    {
        private ObservableCollection<WorkflowTabViewModel> _tabs = new ObservableCollection<WorkflowTabViewModel>();
        private WorkflowTabViewModel? _selectedTab;
        private int _workflowCounter = 1;
        private SortedSet<int> _usedWorkflowNumbers = new SortedSet<int>();

        /// <summary>
        /// 选中画布变化事件
        /// </summary>
        public event EventHandler? SelectionChanged;

        /// <summary>
        /// 工作流状态变化事件。
        /// </summary>
        public event EventHandler? WorkflowStatusChanged;

        public WorkflowTabControlViewModel()
        {
            // 创建默认工作流。
            CreateDefaultWorkflow();
        }

        /// <summary>
        /// 标签页集合。
        /// </summary>
        public ObservableCollection<WorkflowTabViewModel> Tabs
        {
            get => _tabs;
            set => SetProperty(ref _tabs, value);
        }

        /// <summary>
        /// 当前选中的标签页
        /// </summary>
        public WorkflowTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    // 触发选中变化事件
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #region 工作流管理。

        /// <summary>
        /// 创建默认工作流。
        /// </summary>
        private void CreateDefaultWorkflow()
        {
            if (Tabs.Count > 0)
                return;

            var defaultWorkflow = new WorkflowTabViewModel
            {
                Name = "工作流1"
            };
            // ✅ 为没有绑定 Solution 的工作流创建独立的连接集合
            defaultWorkflow.SetConnections(new ObservableCollection<WorkflowConnection>());
            Tabs.Add(defaultWorkflow);
            SelectedTab = defaultWorkflow;
            _workflowCounter = 1;
            _usedWorkflowNumbers.Add(1);
        }

        /// <summary>
        /// 重置为默认状态（清空所有标签页并创建新的默认工作流）
        /// </summary>
        /// <remarks>
        /// 用于删除解决方案后恢复到启动时的初始状态。
        /// 与启动时保持一致，彻底清空节点和连接。
        /// </remarks>
        public void ResetToDefault()
        {
            // 重置所有工作流的节点序号管理器（防止全局索引污染）
            foreach (var tab in Tabs)
            {
                if (tab.SequenceManager != null)
                {
                    tab.SequenceManager.Reset();
                }
            }

            // 清空现有标签页
            Tabs.Clear();

            // 重置计数器和已使用编号
            _workflowCounter = 1;
            _usedWorkflowNumbers.Clear();

            // 创建新的默认工作流（新对象，彻底清空节点和连接）
            var defaultWorkflow = new WorkflowTabViewModel
            {
                Name = "工作流1"
            };
            // ✅ 为没有绑定 Solution 的工作流创建独立的连接集合
            defaultWorkflow.SetConnections(new ObservableCollection<WorkflowConnection>());
            Tabs.Add(defaultWorkflow);
            SelectedTab = defaultWorkflow;
            _usedWorkflowNumbers.Add(1);
        }

        /// <summary>
        /// 重置工作流编号计数器（防止方案切换时的编号污染）
        /// </summary>
        /// <remarks>
        /// 用于加载新方案时重置工作流编号，确保新方案的工作流从1开始编号。
        /// 与 ResetToDefault() 不同，此方法只重置编号计数器，不创建默认工作流。
        /// </remarks>
        public void ResetWorkflowNumberCounter()
        {
            // 重置计数器和已使用编号
            _workflowCounter = 0;
            _usedWorkflowNumbers.Clear();

            // 从已加载的标签页中恢复编号（在加载方案后调用）
            foreach (var tab in Tabs)
            {
                var match = System.Text.RegularExpressions.Regex.Match(tab.Name, @"工作流(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
                {
                    _usedWorkflowNumbers.Add(number);
                    _workflowCounter = Math.Max(_workflowCounter, number);
                }
            }
        }

        /// <summary>
        /// 添加新工作流
        /// </summary>
        public void AddWorkflow()
        {
            int nextNumber = GetNextWorkflowNumber();
            var newWorkflow = new WorkflowTabViewModel
            {
                Name = $"工作流{nextNumber}"
            };
            // ✅ 为没有绑定 Solution 的工作流创建独立的连接集合
            newWorkflow.SetConnections(new ObservableCollection<WorkflowConnection>());
            Tabs.Add(newWorkflow);
            _usedWorkflowNumbers.Add(nextNumber);
            _workflowCounter = Math.Max(_workflowCounter, nextNumber);
            SelectedTab = newWorkflow;
        }

        /// <summary>
        /// 删除工作流。
        /// </summary>
        public bool DeleteWorkflow(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                return false;

            if (Tabs.Count <= 1)
            {
                return false; // 至少保留一个工作流
            }

            if (workflow.IsRunning)
            {
                return false; // 运行中不能删除。
            }

            var index = Tabs.IndexOf(workflow);
            Tabs.Remove(workflow);

            // 从已使用的编号集合中移除
            var match = System.Text.RegularExpressions.Regex.Match(workflow.Name, @"工作流(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                _usedWorkflowNumbers.Remove(number);
            }

            // 选择其他标签页。
            if (SelectedTab == workflow)
            {
                if (Tabs.Count > 0)
                {
                    var newIndex = Math.Min(index, Tabs.Count - 1);
                    SelectedTab = Tabs[newIndex];
                }
                else
                {
                    SelectedTab = null;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取下一个可用的工作流编号。
        /// </summary>
        private int GetNextWorkflowNumber()
        {
            if (_usedWorkflowNumbers.Count == 0)
            {
                return 1;
            }

            // 查找第一个未被使用的编号
            int expectedNumber = 1;
            foreach (var number in _usedWorkflowNumbers)
            {
                if (number != expectedNumber)
                {
                    return expectedNumber;
                }
                expectedNumber++;
            }

            return expectedNumber;
        }

        #endregion

        #region 工作流运行控制。

        /// <summary>
        /// 单次运行工作流。
        /// </summary>
        public void RunSingle(WorkflowTabViewModel workflow)
        {
            if (workflow == null || workflow.IsRunning)
                return;

            workflow.IsRunning = true;
            
            // 模拟单次运行
            Task.Delay(500).ContinueWith(_ =>
            {
                workflow.IsRunning = false;
                WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// 开始连续运行工作流
        /// </summary>
        public void StartContinuous(WorkflowTabViewModel workflow)
        {
            if (workflow == null || workflow.IsRunning)
                return;

            workflow.IsRunning = true;
            WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 停止工作流运行。
        /// </summary>
        public void StopWorkflow(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                return;

            workflow.IsRunning = false;
            WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 切换连续运行/停止
        /// </summary>
        public void ToggleContinuous(WorkflowTabViewModel workflow)
        {
            if (workflow == null)
                return;

            if (workflow.IsRunning)
            {
                StopWorkflow(workflow);
            }
            else
            {
                StartContinuous(workflow);
            }
        }

        /// <summary>
        /// 单次运行所有工作流
        /// </summary>
        public async Task RunAllWorkflowsAsync()
        {
            var runningWorkflows = new List<WorkflowTabViewModel>();
            
            foreach (var workflow in Tabs)
            {
                if (!workflow.IsRunning)
                {
                    workflow.IsRunning = true;
                    runningWorkflows.Add(workflow);
                }
            }

            WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);

            // 模拟执行所有工作流
            await Task.Delay(500);

            foreach (var workflow in runningWorkflows)
            {
                workflow.IsRunning = false;
            }

            WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 开始连续运行所有工作流
        /// </summary>
        public void StartAllWorkflows()
        {
            foreach (var workflow in Tabs)
            {
                if (!workflow.IsRunning)
                {
                    workflow.IsRunning = true;
                }
            }
            WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 停止所有工作流
        /// </summary>
        public void StopAllWorkflows()
        {
            foreach (var workflow in Tabs)
            {
                workflow.IsRunning = false;
            }
            WorkflowStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 切换所有工作流的连续运行/停止。
        /// </summary>
        public void ToggleAllWorkflows()
        {
            var anyRunning = Tabs.Any(w => w.IsRunning);
            
            if (anyRunning)
            {
                StopAllWorkflows();
            }
            else
            {
                StartAllWorkflows();
            }
        }

        /// <summary>
        /// 判断是否有任何工作流正在运行
        /// </summary>
        public bool IsAnyWorkflowRunning
        {
            get => Tabs.Any(w => w.IsRunning);
        }

        #endregion
    }
}
