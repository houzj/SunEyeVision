# 修复 CreateWorkflowTab 方法 - 添加工作流注册

$filePath = 'd:\MyWork\SunEyeVision_Dev\src\UI\ViewModels\MainWindowViewModel.cs'
$content = [System.IO.File]::ReadAllText($filePath, [System.Text.UTF8Encoding]::new($false))

# 修改：在 LogInfo 后面添加工作流注册代码
$oldText = '        private void CreateWorkflowTab(SunEyeVision.Workflow.Workflow workflow, string? filePath)
        {
            LogInfo($"创建标签页: {workflow.Name}");

            // 创建 WorkflowTabViewModel'

$newText = '        private void CreateWorkflowTab(SunEyeVision.Workflow.Workflow workflow, string? filePath)
        {
            LogInfo($"创建标签页: {workflow.Name}");

            // ✅ 将工作流注册到 WorkflowEngine.Workflows 字典
            // 确保节点可以获取父节点数据
            _workflowEngine.Workflows[workflow.Id] = workflow;
            LogInfo($"工作流已注册到 WorkflowEngine: {workflow.Name} (ID: {workflow.Id})");

            // 创建 WorkflowTabViewModel'

if ($content -match [regex]::Escape($oldText)) {
    $content = $content.Replace($oldText, $newText)
    [System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))
    Write-Host "✅ 修改1完成：已添加工作流注册代码"
} else {
    Write-Host "❌ 未找到匹配的文本"
}
