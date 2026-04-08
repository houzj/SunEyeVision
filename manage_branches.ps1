# 分支管理脚本
# 用法：.\manage_branches.ps1 -Branch [camera|tool|main]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("camera", "tool", "main")]
    [string]$Branch
)

$branchMap = @{
    "camera" = "feature/camera-type-support"
    "tool" = "feature/tool-improvement"
    "main" = "main"
}

$targetBranch = $branchMap[$Branch]

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "切换到分支: $targetBranch" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# 切换分支
git checkout $targetBranch 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ 切换成功" -ForegroundColor Green
} else {
    Write-Host "✗ 切换失败" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "当前分支:" -ForegroundColor Yellow
$currentBranch = git branch --show-current
Write-Host "  $currentBranch" -ForegroundColor White

Write-Host ""
Write-Host "最近3次提交:" -ForegroundColor Yellow
git log -3 --oneline --decorate | ForEach-Object {
    Write-Host "  $_" -ForegroundColor White
}

Write-Host ""
Write-Host "当前修改状态:" -ForegroundColor Yellow
$gitStatus = git status --short
if ($gitStatus) {
    $gitStatus | ForEach-Object {
        $status = $_.Substring(0, 2)
        $file = $_.Substring(3)
        if ($status -like "M*") {
            Write-Host "  $status - 修改: $file" -ForegroundColor Yellow
        } elseif ($status -like "A*") {
            Write-Host "  $status - 新增: $file" -ForegroundColor Green
        } elseif ($status -like "D*") {
            Write-Host "  $status - 删除: $file" -ForegroundColor Red
        } else {
            Write-Host "  $status - $file" -ForegroundColor White
        }
    }
} else {
    Write-Host "  (无修改)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
