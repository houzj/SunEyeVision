# 修复 CreateWorkflowTab 和 OnSelectedTabChanged 方法
$filePath = 'd:\MyWork\SunEyeVision_Dev\src\UI\ViewModels\MainWindowViewModel.cs'
$content = [System.IO.File]::ReadAllText($filePath, [System.Text.UTF8Encoding]::new($false))

# 修改1：在 CreateWorkflowTab 方法中添加工作流注册
$old1 = @'
        private void CreateWorkflowTab(SunEyeVision.Workflow.Workflow workflow, string? filePath)
        {
            LogInfo($"创建标签页: {workflow.Name}");

            // 创建 WorkflowTabViewModel
            var tabViewModel = new ViewModels.WorkflowTabViewModel();
'@
$new1 = @'
        private void CreateWorkflowTab(SunEyeVision.Workflow.Workflow workflow, string? filePath)
        {
            LogInfo($"创建标签页: {workflow.Name}");

            // 将工作流注册到 WorkflowEngine.Workflows 字典
            // 确保节点可以获取父节点数据
            _workflowEngine.Workflows[workflow.Id] = workflow;
            LogInfo($"工作流已注册到 WorkflowEngine: {workflow.Name} (ID: {workflow.Id})");

            // 创建 WorkflowTabViewModel
            var tabViewModel = new ViewModels.WorkflowTabViewModel();
'@
$content = $content.Replace($old1, $new1)

# 修改2：移除 OnSelectedTabChanged 中的容错逻辑
$old2 = @'
                catch (ArgumentException ex)
                {
                    // 工作流不在 WorkflowEngine.Workflows 字典中，尝试添加
                    LogInfo($"工作流 {WorkflowTabViewModel.SelectedTab.Name} 不在 WorkflowEngine 中，尝试添加");
                    
                    // 从 Solution 中获取对应的工作流
                    if (_solutionManager?.CurrentSolution != null)
                    {
                        var workflow = _solutionManager.CurrentSolution.Workflows.FirstOrDefault(w => w.Id == WorkflowTabViewModel.SelectedTab.Id);
                        if (workflow != null)
                        {
                            _workflowEngine.Workflows[workflow.Id] = workflow;
                            _workflowEngine.SetCurrentWorkflow(workflow.Id);
                            LogInfo($"工作流 {WorkflowTabViewModel.SelectedTab.Name} 已添加到 WorkflowEngine");
                        }
                    }
                }
'@
$new2 = @'
                catch (ArgumentException ex)
                {
                    // 工作流不在 WorkflowEngine.Workflows 字典中，这是严重错误！
                    LogError($"严重错误：工作流 {WorkflowTabViewModel.SelectedTab.Name} 不在 WorkflowEngine.Workflows 字典中！");
                    LogError($"错误详情: {ex.Message}");
                    LogError($"工作流ID: {WorkflowTabViewModel.SelectedTab.Id}");
                    LogError($"这表明在创建标签页时，工作流没有被正确注册到 WorkflowEngine.Workflows 字典中");
                }
'@
$content = $content.Replace($old2, $new2)

[System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))
Write-Host "修复完成"
