#!/usr/bin/env pwsh
# 诊断脚本：查找日志方法使用情况

Write-Host "正在搜索所有使用日志方法的文件..." -ForegroundColor Cyan

$files = @(
    "src/UI/ViewModels/ParamSettingViewModel.cs",
    "src/UI/ViewModels/ImageParamSettingViewModel.cs",
    "src/UI/Services/ParameterSetting/*.cs"
)

foreach ($pattern in @("LogInfo", "LogSuccess", "LogWarning", "LogError")) {
    Write-Host "`n查找 $pattern 使用:" -ForegroundColor Yellow
    
    $matches = Select-String -Path $files -Pattern "$pattern\(" -AllMatches
    
    if ($matches) {
        Write-Host "找到 $($matches.Count) 处使用:" -ForegroundColor Green
        
        foreach ($match in $matches) {
            Write-Host "  $($match.Path):$($match.LineNumber)" -ForegroundColor White
            
            # 显示上下文
            $startLine = [Math]::Max(1, $match.LineNumber - 2)
            $endLine = [Math]::Min((Get-Content $match.Path).Count, $match.LineNumber + 2)
            Get-Content $match.Path | Select-Object -Index ($startLine-1)..($endLine-1) | ForEach-Object {
                Write-Host "    $_" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "未找到 $pattern 的使用" -ForegroundColor Red
    }
}

Write-Host "`n检查基类继承关系..." -ForegroundColor Cyan

# 检查 ParamSettingViewModelBase
$baseClassFile = "src/UI/ViewModels/ToolViewModelBase.cs"
Write-Host "检查 $baseClassFile 中的 ParamSettingViewModelBase 定义" -ForegroundColor Yellow

Select-String -Path $baseClassFile -Pattern "class ParamSettingViewModelBase" -Context 0,2 | ForEach-Object {
    Write-Host "  找到定义: $($_.Line.Trim())" -ForegroundColor Green
    Write-Host "  上下文:" -ForegroundColor Gray
    $_.Context.PostContext | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
}

# 检查 ParamSettingViewModel
$viewModelFile = "src/UI/ViewModels/ParamSettingViewModel.cs"
Write-Host "检查 $viewModelFile 中的类定义" -ForegroundColor Yellow

Select-String -Path $viewModelFile -Pattern "class ParamSettingViewModel" -Context 0,0 | ForEach-Object {
    Write-Host "  找到定义: $($_.Line.Trim())" -ForegroundColor Green
}

Write-Host "`n诊断完成!" -ForegroundColor Green
